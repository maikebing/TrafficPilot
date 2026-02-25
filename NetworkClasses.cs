using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  WinDivert P/Invoke
// ════════════════════════════════════════════════════════════════

internal enum WinDivertLayer : uint { Network = 0 }

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal struct WinDivertAddress
{
	[FieldOffset(0)] public long Timestamp;
	[FieldOffset(8)] public uint Flags;
	[FieldOffset(12)] public uint Reserved;
	[FieldOffset(16)] public uint IfIdx;
	[FieldOffset(20)] public uint SubIfIdx;

	public readonly byte Layer => (byte)(Flags & 0xFFu);
	public readonly byte Event => (byte)((Flags >> 8) & 0xFFu);
	public readonly bool IsOutbound => (Flags & (1u << 17)) != 0;
	public readonly bool IsLoopback => (Flags & (1u << 18)) != 0;
}

internal static class WinDivertNative
{
	private const string Dll = "WinDivert.dll";

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
	public static extern IntPtr WinDivertOpen(
		[MarshalAs(UnmanagedType.LPStr)] string filter,
		WinDivertLayer layer, short priority, ulong flags);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertRecv(
		IntPtr handle, byte[] pPacket, uint packetLen,
		out uint readLen, ref WinDivertAddress pAddr);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertSend(
		IntPtr handle, byte[] pPacket, uint packetLen,
		out uint writeLen, ref WinDivertAddress pAddr);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertClose(IntPtr handle);

	[DllImport(Dll, CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool WinDivertHelperCalcChecksums(
		byte[] pPacket, uint packetLen, ref WinDivertAddress pAddr, ulong flags);
}

// ════════════════════════════════════════════════════════════════
//  Packet Inspector
// ════════════════════════════════════════════════════════════════

internal readonly record struct ParsedPacket(
	byte[] SrcAddr, ushort SrcPort,
	byte[] DstAddr, ushort DstPort,
	int IpHeaderLen);

internal static class PacketInspector
{
	public static bool TryParseIpv4Tcp(byte[] buf, int len, out ParsedPacket pkt)
	{
		pkt = default;
		if (len < 40) return false;
		if (((buf[0] >> 4) & 0xF) != 4) return false;
		int ihl = (buf[0] & 0xF) * 4;
		if (ihl < 20 || len < ihl + 20) return false;
		if (buf[9] != 6) return false;

		var src = new byte[4]; Buffer.BlockCopy(buf, 12, src, 0, 4);
		var dst = new byte[4]; Buffer.BlockCopy(buf, 16, dst, 0, 4);
		ushort srcPort = (ushort)((buf[ihl] << 8) | buf[ihl + 1]);
		ushort dstPort = (ushort)((buf[ihl + 2] << 8) | buf[ihl + 3]);

		pkt = new ParsedPacket(src, srcPort, dst, dstPort, ihl);
		return true;
	}

	public static void RewriteDestination(byte[] buf, ParsedPacket pkt, byte[] newDst, ushort newPort)
	{
		Buffer.BlockCopy(newDst, 0, buf, 16, 4);
		buf[pkt.IpHeaderLen + 2] = (byte)(newPort >> 8);
		buf[pkt.IpHeaderLen + 3] = (byte)(newPort & 0xFF);
	}

	public static void RewriteSource(byte[] buf, ParsedPacket pkt, byte[] newSrc, ushort newPort)
	{
		Buffer.BlockCopy(newSrc, 0, buf, 12, 4);
		buf[pkt.IpHeaderLen] = (byte)(newPort >> 8);
		buf[pkt.IpHeaderLen + 1] = (byte)(newPort & 0xFF);
	}

	public static bool IsSyn(byte[] buf, ParsedPacket pkt)
	{
		int flagsOffset = pkt.IpHeaderLen + 13;
		if (flagsOffset >= buf.Length) return false;
		byte flags = buf[flagsOffset];
		return (flags & 0x02) != 0 && (flags & 0x10) == 0;
	}
}

// ════════════════════════════════════════════════════════════════
//  Payload Extractor (HTTP / TLS)
// ════════════════════════════════════════════════════════════════

internal static class PayloadExtractor
{
	public static string? TryExtract(byte[] buf, int totalLen, ParsedPacket pkt)
	{
		int tcpOff = pkt.IpHeaderLen;
		if (totalLen < tcpOff + 20) return null;
		int dataOff = tcpOff + (((buf[tcpOff + 12] >> 4) & 0xF) * 4);
		int payloadLen = totalLen - dataOff;
		if (payloadLen <= 0) return null;

		if (buf[dataOff] == 0x16 && payloadLen > 5)
		{
			var sni = TryParseTlsSni(buf, dataOff, payloadLen);
			if (sni is not null) return $"TLS {sni}";
		}

		var http = TryParseHttpRequestInfo(buf, dataOff, payloadLen);
		return http;
	}

	public static string? ExtractDomain(byte[] payload, int len)
	{
		if (len <= 0) return null;

		if (payload[0] == 0x16 && len > 5)
		{
			var sni = TryParseTlsSni(payload, 0, len);
			if (sni is not null) return sni;
		}

		return TryParseHttpHostOnly(payload, 0, len);
	}

	private static string? TryParseHttpRequestInfo(byte[] buf, int offset, int len)
	{
		ReadOnlySpan<byte> data = buf.AsSpan(offset, Math.Min(len, 4096));
		int lineEnd = data.IndexOf((byte)'\n');
		if (lineEnd < 0) lineEnd = data.Length;
		if (lineEnd > 0 && data[lineEnd - 1] == '\r') lineEnd--;
		if (lineEnd < 10) return null;

		var firstLine = data[..lineEnd];
		if (firstLine.IndexOf("HTTP/"u8) < 0) return null;

		int sp1 = firstLine.IndexOf((byte)' ');
		if (sp1 < 0 || sp1 > 10) return null;
		var method = Encoding.ASCII.GetString(firstLine[..sp1]);

		var afterMethod = firstLine[(sp1 + 1)..];
		int sp2 = afterMethod.IndexOf((byte)' ');
		var uri = sp2 > 0
			? Encoding.ASCII.GetString(afterMethod[..sp2])
			: Encoding.ASCII.GetString(afterMethod);

		var host = FindHostHeader(data);

		if (uri.StartsWith("http://") || uri.StartsWith("https://"))
			return $"{method} {uri}";
		if (host is not null)
			return $"{method} http://{host}{uri}";
		return $"{method} {uri}";
	}

	private static string? TryParseHttpHostOnly(byte[] buf, int offset, int len)
	{
		ReadOnlySpan<byte> data = buf.AsSpan(offset, Math.Min(len, 4096));

		if (data.IndexOf("HTTP/"u8) < 0) return null;

		var host = FindHostHeader(data);
		if (host is null) return null;

		int colon = host.LastIndexOf(':');
		if (colon > 0 && !host.Contains('['))
			host = host[..colon];

		return host.Length > 0 ? host : null;
	}

	private static string? FindHostHeader(ReadOnlySpan<byte> data)
	{
		ReadOnlySpan<byte> hostPrefix = "Host: "u8;
		ReadOnlySpan<byte> hostLower = "host: "u8;
		int searchStart = 0;
		while (searchStart < data.Length - 6)
		{
			int nl = data[searchStart..].IndexOf((byte)'\n');
			if (nl < 0) break;
			int headerStart = searchStart + nl + 1;
			if (headerStart >= data.Length) break;

			var remaining = data[headerStart..];
			if (remaining.Length >= hostPrefix.Length &&
				(remaining.StartsWith(hostPrefix) || remaining.StartsWith(hostLower)))
			{
				var afterHost = remaining[hostPrefix.Length..];
				int end = afterHost.IndexOf((byte)'\r');
				if (end < 0) end = afterHost.IndexOf((byte)'\n');
				if (end < 0) end = Math.Min(afterHost.Length, 256);
				return Encoding.ASCII.GetString(afterHost[..end]).Trim();
			}
			searchStart = headerStart;
		}
		return null;
	}

	private static string? TryParseTlsSni(byte[] buf, int offset, int len)
	{
		if (len < 6) return null;
		int hsStart = offset + 5;
		int hsLen = (buf[offset + 3] << 8) | buf[offset + 4];
		if (hsLen < 1) return null;
		int hsEnd = Math.Min(offset + 5 + hsLen, offset + len);

		if (hsStart >= hsEnd || buf[hsStart] != 0x01) return null;

		int pos = hsStart + 1 + 3 + 2 + 32;
		if (pos >= hsEnd) return null;

		int sidLen = buf[pos]; pos++; pos += sidLen;
		if (pos + 2 >= hsEnd) return null;

		int csLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2; pos += csLen;
		if (pos + 1 >= hsEnd) return null;

		int cmLen = buf[pos]; pos++; pos += cmLen;
		if (pos + 2 >= hsEnd) return null;

		int extTotalLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
		int extEnd = Math.Min(pos + extTotalLen, hsEnd);

		while (pos + 4 <= extEnd)
		{
			int extType = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			int extLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			if (pos + extLen > extEnd) break;

			if (extType == 0x0000)
				return ParseSniList(buf, pos, extLen);
			pos += extLen;
		}
		return null;
	}

	private static string? ParseSniList(byte[] buf, int offset, int extLen)
	{
		if (extLen < 5) return null;
		int listLen = (buf[offset] << 8) | buf[offset + 1];
		int pos = offset + 2;
		int listEnd = Math.Min(offset + 2 + listLen, offset + extLen);

		while (pos + 3 <= listEnd)
		{
			int nameType = buf[pos]; pos++;
			int nameLen = (buf[pos] << 8) | buf[pos + 1]; pos += 2;
			if (nameType == 0 && nameLen > 0 && pos + nameLen <= listEnd)
				return Encoding.ASCII.GetString(buf, pos, nameLen);
			pos += nameLen;
		}
		return null;
	}
}

// ════════════════════════════════════════════════════════════════
//  Redirect NAT Table
// ════════════════════════════════════════════════════════════════

internal sealed class RedirectNatTable : IDisposable
{
	private readonly TimeSpan _ttl;
	private readonly ConcurrentDictionary<(uint Ip, ushort Port), NatEntry> _map = new();
	private long _lastCleanup = Environment.TickCount64;

	public RedirectNatTable(TimeSpan ttl) => _ttl = ttl;

	public void RecordRedirect(byte[] clientIp, ushort clientPort,
		byte[] origDstIp, ushort origDstPort)
	{
		var key = (ToU32(clientIp), clientPort);
		_map[key] = new NatEntry(
			Clone4(origDstIp), origDstPort,
			Environment.TickCount64 + (long)_ttl.TotalMilliseconds);
		TryCleanup();
	}

	public bool TryGetOrigDest(byte[] clientIp, ushort clientPort,
		out byte[] origDstIp, out ushort origDstPort)
	{
		var key = (ToU32(clientIp), clientPort);
		if (_map.TryGetValue(key, out var entry) && entry.Expire > Environment.TickCount64)
		{
			origDstIp = entry.OrigDstAddr;
			origDstPort = entry.OrigDstPort;
			return true;
		}
		origDstIp = [];
		origDstPort = 0;
		return false;
	}

	public void Dispose() => _map.Clear();

	private void TryCleanup()
	{
		var now = Environment.TickCount64;
		if (now - Interlocked.Read(ref _lastCleanup) < 15_000) return;
		Interlocked.Exchange(ref _lastCleanup, now);
		foreach (var kv in _map)
			if (kv.Value.Expire <= now)
				_map.TryRemove(kv.Key, out _);
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));

	private static byte[] Clone4(byte[] a) { var c = new byte[4]; Buffer.BlockCopy(a, 0, c, 0, 4); return c; }

	private readonly record struct NatEntry(byte[] OrigDstAddr, ushort OrigDstPort, long Expire);
}

// ════════════════════════════════════════════════════════════════
//  Connection info cache
// ════════════════════════════════════════════════════════════════

internal sealed class ConnectionInfoCache : IDisposable
{
	private readonly TimeSpan _ttl;
	private readonly ConcurrentDictionary<(uint, ushort, uint, ushort), (string Info, long Expire)> _map = new();
	private long _lastCleanup = Environment.TickCount64;

	public ConnectionInfoCache(TimeSpan ttl) => _ttl = ttl;

	public void Record((uint, ushort, uint, ushort) key, string info)
	{
		_map[key] = (info, Environment.TickCount64 + (long)_ttl.TotalMilliseconds);
		TryCleanup();
	}

	public string? TryGet((uint, ushort, uint, ushort) key)
	{
		if (_map.TryGetValue(key, out var entry) && entry.Expire > Environment.TickCount64)
			return entry.Info;
		return null;
	}

	public void Dispose() => _map.Clear();

	private void TryCleanup()
	{
		var now = Environment.TickCount64;
		if (now - Interlocked.Read(ref _lastCleanup) < 15_000) return;
		Interlocked.Exchange(ref _lastCleanup, now);
		foreach (var kv in _map)
			if (kv.Value.Expire <= now)
				_map.TryRemove(kv.Key, out _);
	}
}

// ════════════════════════════════════════════════════════════════
//  TCP table → PID resolver
// ════════════════════════════════════════════════════════════════

internal sealed class TcpOwnerPidResolver : IDisposable
{
	private readonly TimeSpan _cacheTtl;
	private DateTime _lastRefresh = DateTime.MinValue;
	private readonly Dictionary<ConnKey, int> _cache = new();
	private readonly object _lock = new();

	public TcpOwnerPidResolver(TimeSpan cacheTtl) => _cacheTtl = cacheTtl;

	public int? FindOwnerPid(byte[] localAddr, ushort localPort, byte[] remoteAddr, ushort remotePort)
	{
		lock (_lock)
		{
			if (DateTime.UtcNow - _lastRefresh > _cacheTtl)
				Refresh();
			var key = new ConnKey(ToU32(localAddr), localPort, ToU32(remoteAddr), remotePort);
			return _cache.TryGetValue(key, out var pid) ? pid : null;
		}
	}

	public void Dispose() { lock (_lock) _cache.Clear(); }

	private void Refresh()
	{
		int size = 0;
		_ = IpHelperNative.GetExtendedTcpTable(IntPtr.Zero, ref size, true, 2, TcpTableClass.OwnerPidAll, 0);
		var buf = Marshal.AllocHGlobal(size);
		try
		{
			if (IpHelperNative.GetExtendedTcpTable(buf, ref size, true, 2, TcpTableClass.OwnerPidAll, 0) != 0)
				return;
			int count = Marshal.ReadInt32(buf);
			int rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
			var ptr = IntPtr.Add(buf, 4);
			_cache.Clear();
			for (int i = 0; i < count; i++)
			{
				var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(IntPtr.Add(ptr, i * rowSize));
				ushort lp = (ushort)((row.localPort[0] << 8) | row.localPort[1]);
				ushort rp = (ushort)((row.remotePort[0] << 8) | row.remotePort[1]);
				_cache[new ConnKey(row.localAddr, lp, row.remoteAddr, rp)] = (int)row.owningPid;
			}
			_lastRefresh = DateTime.UtcNow;
		}
		finally { Marshal.FreeHGlobal(buf); }
	}

	private static uint ToU32(byte[] a) =>
		(uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));

	private readonly record struct ConnKey(uint LocalAddr, ushort LocalPort, uint RemoteAddr, ushort RemotePort);
}

internal enum TcpTableClass { OwnerPidAll = 5 }

[StructLayout(LayoutKind.Sequential)]
internal struct MibTcpRowOwnerPid
{
	public uint state;
	public uint localAddr;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
	public uint remoteAddr;
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
	public uint owningPid;
}

internal static class IpHelperNative
{
	[DllImport("iphlpapi.dll", SetLastError = true)]
	public static extern uint GetExtendedTcpTable(
		IntPtr pTcpTable, ref int dwOutBufLen, bool sort,
		int ipVersion, TcpTableClass tableClass, uint reserved);
}
