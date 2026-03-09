using System.ComponentModel;
using System.Reflection;
using TrafficPilot.Properties;

namespace TrafficPilot;

// ════════════════════════════════════════════════════════════════
//  Main WinForms Window
// ════════════════════════════════════════════════════════════════

internal partial class MainForm : Form
{
	private readonly ProxyConfigManager _configManager;
	private ProxyEngine? _engine;
	private ProxyConfigModel _currentConfig;

	private LogBuffer? _logBuffer;
	private readonly AutoUpdater _autoUpdater;
	private ReleaseInfo? _availableRelease;

	private bool _isStarting = false;

	public MainForm()
	{
		_configManager = new ProxyConfigManager();
		_currentConfig = _configManager.Load();
		_logBuffer = new LogBuffer(BatchAppendLogs);
		_autoUpdater = new AutoUpdater();
		InitializeComponent();
		InitializeNotifyIcon();
		LoadApplicationIcon();
		LoadVersionLabel();
		CenterToScreen();
		LoadConfigToUI();
	}

	private void LoadApplicationIcon()
	{
		_notifyIcon!.Icon = Resources.favicon;
		Icon = Resources.favicon;
	}

	private void LoadVersionLabel()
	{
		var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
		versionLabel.Text = $"Version: {version}";
		_lblLatestVersion!.Text = " | Online latest: querying...";
		_btnCheckUpdate!.Visible = false;
		_lblUpdateStatus!.Text = string.Empty;
		Text = $"TrafficPilot {version}";
	}

	private static Version GetCurrentVersion()
	{
		var version = Assembly.GetEntryAssembly()?.GetName().Version;
		if (version is null)
			return new Version(0, 0, 0);

		return new Version(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build));
	}

	private void ShowLatestVersion(ReleaseInfo? latestRelease)
	{
		_availableRelease = latestRelease;
		_lblLatestVersion!.Text = latestRelease is null
			? " | Online latest: unavailable"
			: $" | Online latest: {latestRelease.Version}";

		var hasUpdate = latestRelease is not null && latestRelease.Version > GetCurrentVersion();
		_btnCheckUpdate!.Visible = hasUpdate;
		_lblUpdateStatus!.Text = hasUpdate ? $" | Update available from {latestRelease!.Source}" : string.Empty;
	}

	private async Task<ReleaseInfo?> RefreshLatestVersionAsync()
	{
		var latestRelease = await _autoUpdater.GetLatestReleaseAsync();

		ShowLatestVersion(latestRelease);
		return latestRelease;
	}

	private void InitializeNotifyIcon()
	{
		_contextMenu = new ContextMenuStrip();
		_contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
		_contextMenu.Items.Add("Hide", null, (s, e) => HideWindow());
		_contextMenu.Items.Add("-");
		_contextMenu.Items.Add("Start Proxy", null, async (s, e) => await StartProxyAsync());
		_contextMenu.Items.Add("Stop Proxy", null, async (s, e) => await StopProxyAsync());
		_contextMenu.Items.Add("-");
		_contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

		_notifyIcon = new NotifyIcon
		{
			Text = "TrafficPilot",
			ContextMenuStrip = _contextMenu,
			Visible = true
		};
		_notifyIcon.DoubleClick += (s, e) => ShowWindow();
		
		UpdateTrayMenuState();
	}

	private async void BtnStartStop_Click(object? sender, EventArgs e)
	{
		if (_isStarting) return;
	
		_btnStartStop!.Enabled = false;

		try
		{
			if (_engine == null || !_engine.IsRunning)
			{
				if (!_chkProxyEnabled!.Checked && !_chkDNSRedirectEnabled!.Checked)
				{
					MessageBox.Show(
						"Neither Proxy nor Hosts Redirect is enabled. Please enable at least one before starting.",
						"Nothing to Start",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					return;
				}
				_btnStartStop.Text = "Starting...";
				await StartProxyAsync();
				_isStarting = true;
			}
			else
			{
				_btnStartStop.Text = "Stopping...";
				await StopProxyAsync();
				_isStarting = false;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error: {ex.Message}", "Proxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_btnStartStop!.Text = "Start Proxy";
			_btnStartStop.BackColor = Color.LimeGreen;
			_lblStatus!.Text = "Status: Stopped";
			_engine?.Dispose();
			_engine = null;
			_isStarting = false;
		}
		finally
		{
			_btnStartStop!.Enabled = true;
			_isStarting = false;
		}
	}

	private void UpdateTrayMenuState()
	{
		if (_contextMenu?.Items.Count < 7)
			return;

		bool isRunning = _engine?.IsRunning ?? false;
		_contextMenu?.Items[3].Enabled = !isRunning;  // Start Proxy
		_contextMenu?.Items[4].Enabled = isRunning;   // Stop Proxy
	}

	private async Task StartProxyAsync()
	{
		if (_engine?.IsRunning == true || _isStarting)
			return;

		try
		{
			var opts = BuildProxyOptions();
			_engine = new ProxyEngine(opts);
			_engine.OnLog += (msg) => AppendLog(msg);
			_engine.OnStatsUpdated += (stats) => UpdateStats(stats);

			await Task.Run(async () => await _engine.StartAsync());
			
			_btnStartStop!.Text = "Stop Proxy";
			_btnStartStop.BackColor = Color.Red;
			_lblStatus!.Text = "Status: Running";
			_lblStatus.ForeColor = Color.Green;
			
			UpdateTrayMenuState();
		}
		catch (Exception )
		{
			_engine?.Dispose();
			_engine = null;
			throw;
		}
	}

	private async Task StopProxyAsync()
	{
		if (_engine?.IsRunning != true)
			return;

		try
		{
			await _engine.StopAsync();
			_engine.Dispose();
			_engine = null;
			
			_btnStartStop!.Text = "Start Proxy";
			_btnStartStop.BackColor = Color.LimeGreen;
			_lblStatus!.Text = "Status: Stopped";
			_lblStatus.ForeColor = Color.Black;
			
			UpdateTrayMenuState();
		}
		catch (Exception )
		{
			throw;
		}
	}

	private void BtnSaveConfig_Click(object? sender, EventArgs e)
	{
		_currentConfig = BuildConfigModel();
		_configManager.Save(_currentConfig);
		MessageBox.Show("Configuration saved successfully.", "Save Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void BtnLoadConfig_Click(object? sender, EventArgs e)
	{
		_currentConfig = _configManager.Load();
		LoadConfigToUI();
		MessageBox.Show("Configuration loaded successfully.", "Load Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private ProxyConfigModel BuildConfigModel()
	{
		return new ProxyConfigModel
		{
			Proxy = new ProxySettings
			{
				Enabled = _chkProxyEnabled!.Checked,
				Host = _txtProxyHost!.Text,
				Port = (ushort)_numProxyPort!.Value,
				Scheme = _cmbProxyScheme!.SelectedItem?.ToString() ?? "socks4"
			},
			Targeting = new TargetingSettings
			{
				ProcessNames = _lstProcesses!.Items.Cast<string>().ToList(),
				ExtraPids = _lstExtraPids!.Items.Cast<int>().ToList()
			},
			HostsRedirect = new HostsRedirectSettings
			{
				Enabled = _chkDNSRedirectEnabled!.Checked,
				HostsUrl = _txtHostsUrl!.Text.Trim()
			},
			StartOnBoot = _chkStartOnBoot!.Checked
		};
	}

	private ProxyOptions BuildProxyOptions()
	{
		return new ProxyOptions(
			_lstExtraPids!.Items.Cast<int>().ToList(),
			_lstProcesses!.Items.Cast<string>().ToList(),
			_txtProxyHost!.Text,
			(ushort)_numProxyPort!.Value,
			_cmbProxyScheme!.SelectedItem?.ToString() ?? "socks4",
			_chkProxyEnabled!.Checked,
			_chkDNSRedirectEnabled!.Checked,
			_txtHostsUrl!.Text.Trim()
		);
	}

	private void LoadConfigToUI()
	{
		_chkProxyEnabled!.Checked = _currentConfig.Proxy?.Enabled ?? true;
		_txtProxyHost!.Text = _currentConfig.Proxy?.Host ?? "";
		_numProxyPort!.Value = _currentConfig.Proxy?.Port ?? 7890;
		_cmbProxyScheme!.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks4";
		_lblConfigFileValue!.Text = _configManager.GetConfigPath();

		_lstProcesses!.Items.Clear();
		foreach (var proc in _currentConfig.Targeting?.ProcessNames ?? [])
			_lstProcesses.Items.Add(proc);

		_lstExtraPids!.Items.Clear();
		foreach (var pid in _currentConfig.Targeting?.ExtraPids ?? [])
			_lstExtraPids.Items.Add(pid);

		_chkDNSRedirectEnabled!.Checked = _currentConfig.HostsRedirect?.Enabled ?? false;
		_txtHostsUrl!.Text = _currentConfig.HostsRedirect?.HostsUrl ?? GitHub520HostsProvider.DefaultUrl;
		_chkStartOnBoot!.Checked = StartupManager.IsEnabled();
	}

	private void AppendLog(string message)
	{
		_logBuffer?.Enqueue(message);
	}

	private void BatchAppendLogs(List<string> messages)
	{
		if (_rtbLogs!.InvokeRequired)
		{
			_rtbLogs.Invoke(() => BatchAppendLogs(messages));
			return;
		}

		foreach (var msg in messages)
		{
			_rtbLogs.AppendText(msg + Environment.NewLine);
		}

		_rtbLogs.SelectionStart = _rtbLogs.TextLength;
		_rtbLogs.ScrollToCaret();

		if (_rtbLogs.Lines.Length > 10000)
			_rtbLogs.Clear();
	}

	private void UpdateStats(RedirectStats stats)
	{
		if (_lblStats!.InvokeRequired)
		{
			_lblStats.BeginInvoke(() => UpdateStats(stats));
			return;
		}

		_lblStats.Text = $"Redirected: {stats.Redirected} | Proxied: {stats.ProxiedOk} | Failed: {stats.ProxiedFail}";
	}

	private void ShowWindow()
	{
		Show();
		WindowState = FormWindowState.Normal;
		BringToFront();
		Activate();
	}

	private void HideWindow()
	{
		Hide();
		WindowState = FormWindowState.Minimized;
	}

	private void ExitApplication()
	{
		if (_engine?.IsRunning == true)
		{
			MessageBox.Show("Please stop the proxy before exiting.", "Proxy Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		_engine?.Dispose();
		_notifyIcon?.Dispose();
		Application.Exit();
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		if (e.CloseReason == CloseReason.UserClosing)
		{
			e.Cancel = true;
			HideWindow();
		}
		base.OnFormClosing(e);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_logBuffer?.Dispose();
			_engine?.Dispose();
			_notifyIcon?.Dispose();
			_contextMenu?.Dispose();
			_autoUpdater.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override async void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		try
		{
			await RefreshLatestVersionAsync();
		}
		catch (HttpRequestException)
		{
			ShowLatestVersion(null);
			_lblUpdateStatus!.Text = string.Empty;
		}
	}

	private async void BtnCheckUpdate_Click(object? sender, EventArgs e)
	{
		_btnCheckUpdate!.Enabled = false;
		_lblUpdateStatus!.Text = "Checking for updates...";

		try
		{
			_lblUpdateStatus.Text = " | Checking for updates...";

			var latestRelease = _availableRelease ?? await RefreshLatestVersionAsync();
			var current = GetCurrentVersion();

			if (latestRelease is null)
			{
				_lblUpdateStatus.Text = " | Unable to determine the online latest version.";
				return;
			}

			if (latestRelease.Version <= current)
			{
				_btnCheckUpdate.Visible = false;
				_lblUpdateStatus.Text = string.Empty;
				return;
			}

			_lblUpdateStatus.Text = $" | Update available from {latestRelease.Source}";

			var confirm = MessageBox.Show(
				$"Version {latestRelease.Version} is available from {latestRelease.Source}.\n\nDownload and install it now?",
				"Update Available",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Information);

			if (confirm != DialogResult.Yes)
			{
				_lblUpdateStatus.Text = $" | Update {latestRelease.Version} available";
				return;
			}

			var progress = new Progress<(int Percent, string Message)>(report =>
				_lblUpdateStatus.Text = $" | {report.Message} ({report.Percent}%)");

			await _autoUpdater.DownloadAndApplyUpdateAsync(latestRelease, progress);

			MessageBox.Show(
				"The update has been downloaded. The application will now close and restart to apply it.",
				"Restart Required",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);

			_engine?.Dispose();
			_notifyIcon?.Dispose();
			Application.Exit();
		}
		catch (HttpRequestException ex)
		{
			ShowLatestVersion(null);
			_lblUpdateStatus.Text = $" | Update failed: {ex.Message}";
		}
		catch (InvalidOperationException ex)
		{
			_lblUpdateStatus.Text = $" | Update failed: {ex.Message}";
		}
		catch (IOException ex)
		{
			_lblUpdateStatus.Text = $" | Update failed: {ex.Message}";
		}
		catch (UnauthorizedAccessException ex)
		{
			_lblUpdateStatus.Text = $" | Update failed: {ex.Message}";
		}
		finally
		{
			_btnCheckUpdate.Enabled = true;
		}
	}

	private void BtnRemoveProcess_Click(object? sender, EventArgs e)
	{
		if (_lstProcesses?.SelectedIndex >= 0)
			_lstProcesses.Items.RemoveAt(_lstProcesses.SelectedIndex);
	}

	private void BtnAddProcess_Click(object? sender, EventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(_txtNewProcess?.Text))
		{
			_lstProcesses?.Items.Add(_txtNewProcess.Text);
			_txtNewProcess.Clear();
		}
	}

	private void BtnRemovePid_Click(object? sender, EventArgs e)
	{
		if (_lstExtraPids?.SelectedIndex >= 0)
			_lstExtraPids.Items.RemoveAt(_lstExtraPids.SelectedIndex);
	}

	private void BtnAddPid_Click(object? sender, EventArgs e)
	{
		if (int.TryParse(_txtNewPid?.Text, out int pid))
		{
			_lstExtraPids?.Items.Add(pid);
			_txtNewPid!.Clear();
		}
	}

	private void BtnClearLogs_Click(object? sender, EventArgs e)
	{
		_rtbLogs?.Clear();
	}

	private void ChkStartOnBoot_CheckedChanged(object? sender, EventArgs e)
	{
		try
		{
			if (_chkStartOnBoot!.Checked)
				StartupManager.Enable();
			else
				StartupManager.Disable();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to update startup setting: {ex.Message}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_chkStartOnBoot!.CheckedChanged -= ChkStartOnBoot_CheckedChanged;
			_chkStartOnBoot.Checked = StartupManager.IsEnabled();
			_chkStartOnBoot.CheckedChanged += ChkStartOnBoot_CheckedChanged;
		}
	}

	private async void BtnRefreshHosts_Click(object? sender, EventArgs e)
	{
		_btnRefreshHosts!.Enabled = false;
		_lblHostsStatus!.Text = "Status: Downloading...";
		try
		{
			using var provider = new GitHub520HostsProvider(_txtHostsUrl!.Text.Trim());
			await provider.RefreshAsync();
			_lblHostsStatus.Text = $"Status: Loaded {provider.HostCount} entries (last updated {provider.LastRefresh.ToLocalTime():HH:mm:ss})";
		}
		catch (Exception ex)
		{
			_lblHostsStatus.Text = $"Status: Error - {ex.Message}";
		}
		finally
		{
			_btnRefreshHosts.Enabled = true;
		}
	}
}
