using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace VSifier;

// ════════════════════════════════════════════════════════════════
//  Proxy Engine
// ════════════════════════════════════════════════════════════════

internal sealed class ProxyEngine : IDisposable
{
	private readonly ProxyOptions _options;
	private readonly IPAddress _proxyIp;
	private TcpRelayServer? _relay;
	private ProcessAllowListMatcher? _processMatcher;
	private LocalTrafficBypass? _localBypass;
	private SkipLogDedup? _skipLogDedup;
	private RedirectNatTable? _redirectNat;
	private ConnectionInfoCache? _connInfoCache;
	private ProcessNameResolver? _processNameResolver;
	private TcpOwnerPidResolver? _tcpTableResolver;
	private RedirectStats? _stats;
	private IntPtr _winDivertHandle = IntPtr.Zero;
	private CancellationTokenSource? _cts;
	private Task? _packetLoopTask;
	private bool _isRunning = false;

	public event Action<string>? OnLog;
	public event Action<RedirectStats>? OnStatsUpdated;

	public bool IsRunning => _isRunning;
	public ushort RelayPort { get; private set; }

	public ProxyEngine(ProxyOptions options)
	{
		_options = options;
		_proxyIp = ResolveProxyIpv4(_options.ProxyHost);
	}

	public async Task StartAsync()
	{
		if (_isRunning) return;
		if (_proxyIp.AddressFamily != AddressFamily.InterNetwork)
			throw new InvalidOperationException("Only IPv4 proxy address is supported.");

		_processMatcher = new ProcessAllowListMatcher(_options.ProcessNames, _options.ExtraPids, TimeSpan.FromSeconds(1));
		if (!_processMatcher.HasAnyRule)
			throw new InvalidOperationException("No target process rule found.");

		_localBypass = LocalTrafficBypass.CreateDefault();
		_skipLogDedup = new SkipLogDedup(TimeSpan.FromSeconds(30));
		_redirectNat = new RedirectNatTable(TimeSpan.FromMinutes(10));
		_connInfoCache = new ConnectionInfoCache(TimeSpan.FromMinutes(5));
		_processNameResolver = new ProcessNameResolver(TimeSpan.FromSeconds(3));
		_relay = new TcpRelayServer(_proxyIp, _options.ProxyPort, _options.ProxyScheme, _redirectNat, _connInfoCache);
		RelayPort = (ushort)_relay.Start();

		LogInfo($"Relay started at 0.0.0.0:{RelayPort}");
		LogInfo($"Proxy target: {_proxyIp}:{_options.ProxyPort} ({_options.ProxyScheme})");
		LogInfo($"Process rules: {_processMatcher.Describe()}");

		_winDivertHandle = WinDivertNative.WinDivertOpen("ip and tcp", WinDivertLayer.Network, 0, 0UL);
		if (_winDivertHandle == IntPtr.Zero || _winDivertHandle == new IntPtr(-1))
		{
			var err = Marshal.GetLastWin32Error();
			throw new Win32Exception(err, $"WinDivertOpen failed (err={err})");
		}

		_tcpTableResolver = new TcpOwnerPidResolver(TimeSpan.FromMilliseconds(500));
		_stats = new RedirectStats();
		_cts = new CancellationTokenSource();
		_isRunning = true;
		_packetLoopTask = PacketProcessingLoopAsync(_cts.Token);
		await Task.Delay(100);
	}

	public async Task StopAsync()
	{
		if (!_isRunning) return;
		_isRunning = false;
		_cts?.Cancel();
		if (_packetLoopTask != null)
			await _packetLoopTask;
	}

	public void Dispose()
	{
		_cts?.Dispose();
		_relay?.Stop();
		_tcpTableResolver?.Dispose();
		_processMatcher?.Dispose();
		_processNameResolver?.Dispose();
		_redirectNat?.Dispose();
		_connInfoCache?.Dispose();
		if (_winDivertHandle != IntPtr.Zero && _winDivertHandle != new IntPtr(-1))
			WinDivertNative.WinDivertClose(_winDivertHandle);
	}

	private async Task PacketProcessingLoopAsync(CancellationToken ct)
	{
		var selfPid = Environment.ProcessId;
		var packet = new byte[0xFFFF];

		while (!ct.IsCancellationRequested)
		{
			try
			{
				var addr = new WinDivertAddress();
				if (!WinDivertNative.WinDivertRecv(_winDivertHandle, packet, (uint)packet.Length, out var recvLen, ref addr))
					continue;

				_stats!.ReceivedPackets++;

				if (!PacketInspector.TryParseIpv4Tcp(packet, (int)recvLen, out var pkt))
				{
					_stats.NonIpv4Tcp++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (pkt.SrcPort == RelayPort)
				{
					if (_redirectNat!.TryGetOrigDest(pkt.DstAddr, pkt.DstPort, out var origDst, out var origPort))
					{
						PacketInspector.RewriteSource(packet, pkt, origDst, origPort);
						WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);
						_stats.InboundRewritten++;
					}
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (!addr.IsOutbound)
				{
					_stats.InboundPackets++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				_stats.OutboundPackets++;

				if (_localBypass!.ShouldBypass(pkt.DstAddr))
				{
					_stats.LocalBypassed++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				var pid = _tcpTableResolver!.FindOwnerPid(pkt.SrcAddr, pkt.SrcPort, pkt.DstAddr, pkt.DstPort);

				if (pid == selfPid)
				{
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (pid is null || !_processMatcher!.ContainsPid(pid.Value))
				{
					if (pid is null) _stats.PidMiss++;
					else _stats.ProcessSkipped++;

					bool isSyn = PacketInspector.IsSyn(packet, pkt);
					if (isSyn)
					{
						var dstIpStr = FormatIp(pkt.DstAddr);
						if (_skipLogDedup!.ShouldLog(dstIpStr, pkt.DstPort))
						{
							var skipProcName = pid is not null ? _processNameResolver!.Resolve(pid.Value) : "?";
							var reason = pid is null ? "PID not found" : "Process not in allow-list";
							LogInfo($"[skip] {skipProcName}({pid?.ToString() ?? "?"}) {FormatIp(pkt.SrcAddr)}:{pkt.SrcPort} -> {dstIpStr}:{pkt.DstPort} ({reason})");
						}
					}

					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (pkt.DstPort == RelayPort && pkt.DstAddr.SequenceEqual(pkt.SrcAddr))
				{
					_stats.AlreadyProxy++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				var connKey = (ToU32(pkt.SrcAddr), pkt.SrcPort, ToU32(pkt.DstAddr), pkt.DstPort);
				var httpInfo = PayloadExtractor.TryExtract(packet, (int)recvLen, pkt);
				if (httpInfo is not null)
					_connInfoCache!.Record(connKey, httpInfo);

				bool isSynForLog = PacketInspector.IsSyn(packet, pkt);
				if (isSynForLog || httpInfo is not null)
				{
					var displayInfo = httpInfo ?? _connInfoCache!.TryGet(connKey);
					var procName = _processNameResolver!.Resolve(pid.Value);
					var infoTag = displayInfo is not null ? $"  [{displayInfo}]" : "";
					LogInfo($"{procName}({pid.Value}) {FormatIp(pkt.SrcAddr)}:{pkt.SrcPort} -> {FormatIp(pkt.DstAddr)}:{pkt.DstPort} >> relay >> {_options.ProxyScheme}://{_proxyIp}:{_options.ProxyPort}{infoTag}");
				}

				_redirectNat!.RecordRedirect(pkt.SrcAddr, pkt.SrcPort, pkt.DstAddr, pkt.DstPort);
				PacketInspector.RewriteDestination(packet, pkt, pkt.SrcAddr, RelayPort);
				WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);
				WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
				_stats.Redirected++;
				_stats.ProxiedOk = Interlocked.Read(ref _relay!.ProxiedSuccess);
				_stats.ProxiedFail = Interlocked.Read(ref _relay.ProxiedFailed);

				if (DateTime.UtcNow.Second % 5 == 0)
					OnStatsUpdated?.Invoke(_stats);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				LogInfo($"Error in packet loop: {ex.Message}");
			}

			await Task.Yield();
		}
	}

	private void LogInfo(string message)
	{
		OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
	}

	private static IPAddress ResolveProxyIpv4(string host)
	{
		if (IPAddress.TryParse(host, out var ip)) return ip;
		var addrs = Dns.GetHostAddresses(host);
		return addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
			?? throw new InvalidOperationException($"Cannot resolve IPv4 for '{host}'.");
	}

	private static string FormatIp(byte[] a) => a.Length == 4 ? $"{a[0]}.{a[1]}.{a[2]}.{a[3]}" : "?";
	private static uint ToU32(byte[] a) => (uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));
}
