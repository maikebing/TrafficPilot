using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace TrafficPilot;

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
	private RedirectNatTable? _redirectNat;
	private ConnectionInfoCache? _connInfoCache;
	private TcpOwnerPidResolver? _tcpTableResolver;
	private RedirectStats? _stats;
	private IntPtr _winDivertHandle = IntPtr.Zero;
	private CancellationTokenSource? _cts;
	private Task? _packetLoopTask;
	private bool _isRunning = false;
	private GitHub520HostsProvider? _hostsProvider;
	private DnsInterceptor? _dnsInterceptor;
	private LocalApiForwarder? _localApiForwarder;

	public event Action<string>? OnLog;
	public event Action<RedirectStats>? OnStatsUpdated;

	public bool IsRunning => _isRunning;
	public bool IsLocalApiForwarderRunning => _localApiForwarder is not null;
	public ushort RelayPort { get; private set; }

	public ProxyEngine(ProxyOptions options)
	{
		_options = options;
		_proxyIp = options.ProxyEnabled
			? ResolveProxyIpv4(_options.ProxyHost)
			: IPAddress.Loopback;
	}

	public async Task StartAsync()
	{
		if (_isRunning) return;

		if (!_options.ProxyEnabled && !_options.HostsRedirectEnabled && _options.LocalApiForwarder?.Enabled != true)
			throw new InvalidOperationException("Proxy, hosts redirect, and local API forwarding are all disabled.");

		if (_options.LocalApiForwarder?.Enabled == true)
		{
			var providerName = _options.LocalApiForwarder.Provider?.Name ?? "Default";
			var apiKey = CredentialManager.LoadToken(CredentialManager.GetLocalApiTargetName(providerName));
			_localApiForwarder = new LocalApiForwarder(_options.LocalApiForwarder, apiKey);
			_localApiForwarder.OnLog += LogInfo;
			await _localApiForwarder.StartAsync();
		}

		// Start hosts redirect in appropriate mode
		if (_options.HostsRedirectEnabled)
		{
			_hostsProvider = new GitHub520HostsProvider(_options.HostsRedirectUrl);
			_hostsProvider.OnLog += (msg) => OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
			await _hostsProvider.RefreshAsync();

			bool useHostsFile = (_options.HostsRedirectMode ?? "DnsInterception")
				.Equals("HostsFile", StringComparison.OrdinalIgnoreCase);

			if (useHostsFile)
			{
				// Hosts file mode
				try
				{
					SystemHostsFileManager.WriteHostsFile(_hostsProvider.GetHostsMap());
					SystemHostsFileManager.FlushDnsCache();
					LogInfo($"System hosts file updated ({_hostsProvider.HostCount} hosts)");
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(
						$"Failed to write hosts file: {ex.Message}. Please run as Administrator.", ex);
				}
			}
			else
			{
				// DNS interception mode
				_dnsInterceptor = new DnsInterceptor(_hostsProvider);
				_dnsInterceptor.OnLog += (msg) => OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
				await _dnsInterceptor.StartAsync();
				LogInfo($"DNS interception started ({_hostsProvider.HostCount} hosts)");
			}
		}

		if (!_options.ProxyEnabled)
		{
			// Hosts redirect and/or local API forwarder only mode: no TCP relay or packet loop needed.
			_isRunning = true;
			return;
		}

		if (_proxyIp.AddressFamily != AddressFamily.InterNetwork)
			throw new InvalidOperationException("Only IPv4 proxy address is supported.");

		_processMatcher = new ProcessAllowListMatcher(_options.ProcessNames, TimeSpan.FromSeconds(1));
		var domainRules = new DomainRuleMatcher(_options.DomainRules);
		_localBypass = LocalTrafficBypass.CreateDefault();
		_redirectNat = new RedirectNatTable(TimeSpan.FromMinutes(10));
		_connInfoCache = new ConnectionInfoCache(TimeSpan.FromMinutes(5));
		_relay = new TcpRelayServer(_proxyIp, _options.ProxyPort, _options.ProxyScheme, _redirectNat, _connInfoCache, domainRules);
		_relay.OnLog += LogInfo;
		RelayPort = (ushort)_relay.Start();

		LogInfo($"Relay started at 0.0.0.0:{RelayPort}");
		LogInfo($"Proxy target: {_proxyIp}:{_options.ProxyPort} ({_options.ProxyScheme})");
		LogInfo($"Process rules: {_processMatcher.Describe()}");
		LogInfo($"Domain rules: {domainRules.Describe()}");

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
		{
			try
			{
				await _packetLoopTask;
			}
			catch (OperationCanceledException)
			{
				// Expected when cancelling
			}
		}
		if (_dnsInterceptor != null)
			await _dnsInterceptor.StopAsync();
		if (_localApiForwarder != null)
			await _localApiForwarder.StopAsync();

        // Clean up hosts file entries if in hosts file mode
		if (_options.HostsRedirectEnabled && 
			(_options.HostsRedirectMode ?? "DnsInterception").Equals("HostsFile", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				SystemHostsFileManager.RemoveTrafficPilotEntries();
				SystemHostsFileManager.FlushDnsCache();
             LogInfo("Removed TrafficPilot entries from Windows/WSL hosts files");
			}
			catch (Exception ex)
			{
               LogInfo($"Warning: Failed to clean up Windows/WSL hosts files: {ex.Message}");
			}
		}
	}

	public async Task<IReadOnlyList<string>> RefreshLocalApiModelCatalogAsync(CancellationToken ct = default)
	{
		if (_options.LocalApiForwarder?.Enabled != true || _localApiForwarder is null)
			throw new InvalidOperationException("Local API forwarder is not running.");

		return await _localApiForwarder.RefreshModelCatalogAsync(ct);
	}

	/// <summary>Pushes freshly resolved IPs into the live DNS interceptor without restarting it.</summary>
	public void UpdateHostsEntries(IReadOnlyDictionary<string, byte[]> updates)
	{
		_hostsProvider?.BatchUpdate(updates);

     // If in hosts file mode, also update the Windows and WSL hosts files
		if (_hostsProvider != null && 
			(_options.HostsRedirectMode ?? "DnsInterception").Equals("HostsFile", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				SystemHostsFileManager.WriteHostsFile(_hostsProvider.GetHostsMap());
				SystemHostsFileManager.FlushDnsCache();
             LogInfo($"Windows/WSL hosts files refreshed ({_hostsProvider.HostCount} hosts)");
			}
			catch (Exception ex)
			{
             LogInfo($"Warning: Failed to update Windows/WSL hosts files: {ex.Message}");
			}
		}
	}

	public void Dispose()
	{
		_packetLoopTask?.Dispose();
		_cts?.Dispose();
		_relay?.Stop();
		_processMatcher?.Dispose();
		_tcpTableResolver?.Dispose();
		_redirectNat?.Dispose();
		_connInfoCache?.Dispose();
		_dnsInterceptor?.Dispose();
		_hostsProvider?.Dispose();
		_localApiForwarder?.Dispose();
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

				if (pid is null)
				{
					_stats.PidMiss++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (_processMatcher!.HasAnyRule && !_processMatcher.ContainsPid(pid.Value))
				{
					_stats.ProcessSkipped++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				if (pkt.DstPort == RelayPort && pkt.DstAddr.SequenceEqual(pkt.SrcAddr))
				{
					_stats.AlreadyProxy++;
					WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
					continue;
				}

				_redirectNat!.RecordRedirect(pkt.SrcAddr, pkt.SrcPort, pkt.DstAddr, pkt.DstPort);
				PacketInspector.RewriteDestination(packet, pkt, pkt.SrcAddr, RelayPort);
				WinDivertNative.WinDivertHelperCalcChecksums(packet, recvLen, ref addr, 0UL);
				WinDivertNative.WinDivertSend(_winDivertHandle, packet, recvLen, out _, ref addr);
				_stats.Redirected++;
				_stats.Bytes += recvLen;
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

	private static uint ToU32(byte[] a) => (uint)(a[0] | (a[1] << 8) | (a[2] << 16) | (a[3] << 24));
}
