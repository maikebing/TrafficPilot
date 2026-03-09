using System.ComponentModel;
using System.Net;
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
	private string _activeConfigPath;

	private LogBuffer? _logBuffer;
	private readonly AutoUpdater _autoUpdater;
	private ReleaseInfo? _availableRelease;

	private bool _isStarting = false;
	private bool _initialAutoStartHandled;

	private CancellationTokenSource? _ipFetchCts;
	private CancellationTokenSource? _autoFetchCts;
	private Task? _autoFetchTask;

	public MainForm()
	{
		_configManager = new ProxyConfigManager();
		_activeConfigPath = _configManager.GetConfigPath();
		_currentConfig = _configManager.Load(_activeConfigPath);
		_logBuffer = new LogBuffer(BatchAppendLogs);
		_autoUpdater = new AutoUpdater();
		InitializeComponent();
		InitIpResultsColumns();
		LoadApplicationIcon();
		LoadVersionLabel();
		CenterToScreen();
		LoadConfigToUI();
		RefreshConfigShortcutButtons();
		UpdateTrayMenuState();
	}

	/// <summary>
	/// Initializes ListView columns for IP results.
	/// Must be called from the constructor — the WinForms designer regenerates
	/// InitializeComponent and removes any Columns.Add calls placed there.
	/// </summary>
	private void InitIpResultsColumns()
	{
		_lvIpResults!.Columns.Clear();
		_lvIpResults.Columns.Add("Domain",     220, HorizontalAlignment.Left);
		_lvIpResults.Columns.Add("IP Address", 130, HorizontalAlignment.Left);
		_lvIpResults.Columns.Add("Latency",     90, HorizontalAlignment.Right);
		_lvIpResults.Columns.Add("Via Proxy",  100, HorizontalAlignment.Right);
		_lvIpResults.Columns.Add("Source",     100, HorizontalAlignment.Left);
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
		if (_trayStartProxyMenuItem is null || _trayStopProxyMenuItem is null)
			return;

		var isRunning = _engine?.IsRunning ?? false;
		_trayStartProxyMenuItem.Enabled = !isRunning;
		_trayStopProxyMenuItem.Enabled = isRunning;

		if (_trayStartOnBootMenuItem is not null && _chkStartOnBoot is not null)
			_trayStartOnBootMenuItem.Checked = _chkStartOnBoot.Checked;

		if (_trayAutoStartProxyMenuItem is not null && _chkAutoStartProxy is not null)
			_trayAutoStartProxyMenuItem.Checked = _chkAutoStartProxy.Checked;
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
		SaveConfig(_activeConfigPath, "Save Config");
	}

	private void BtnLoadConfig_Click(object? sender, EventArgs e)
	{
		using OpenFileDialog dialog = new();
		dialog.InitialDirectory = GetConfigDialogDirectory();
		dialog.FileName = Path.GetFileName(_activeConfigPath);
		dialog.DefaultExt = "json";
		dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		LoadConfigFromPath(dialog.FileName, "Load Config");
	}

	private void BtnSaveConfigAs_Click(object? sender, EventArgs e)
	{
		using SaveFileDialog dialog = new();
		dialog.InitialDirectory = GetConfigDialogDirectory();
		dialog.FileName = Path.GetFileName(_activeConfigPath);
		dialog.DefaultExt = "json";
		dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		SaveConfig(dialog.FileName, "Save Config");
	}

	private ProxyConfigModel BuildConfigModel()
	{
		return new ProxyConfigModel
		{
			ConfigName = _txtConfigName!.Text.Trim(),
			Proxy = new ProxySettings
			{
				Enabled = _chkProxyEnabled!.Checked,
				Host = _txtProxyHost!.Text,
				Port = (ushort)_numProxyPort!.Value,
				Scheme = _cmbProxyScheme!.SelectedItem?.ToString() ?? "socks4"
			},
			Targeting = new TargetingSettings
			{
				ProcessNames = GetProcessNamesFromUi(),
				DomainRules = GetDomainRulesFromUi()
			},
			HostsRedirect = new HostsRedirectSettings
			{
				Enabled = _chkDNSRedirectEnabled!.Checked,
				HostsUrl = _txtHostsUrl!.Text.Trim(),
				RefreshDomains = GetRefreshDomainsFromUi()
			},
			StartOnBoot = _chkStartOnBoot!.Checked,
			AutoStartProxy = _chkAutoStartProxy!.Checked
		};
	}

	private ProxyOptions BuildProxyOptions()
	{
		return new ProxyOptions(
			GetProcessNamesFromUi(),
			GetDomainRulesFromUi(),
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
		_lblConfigFileValue!.Text = _activeConfigPath;

		SetProcessNamesToUi(_currentConfig.Targeting?.ProcessNames);
		SetDomainRulesToUi(_currentConfig.Targeting?.DomainRules);

		_chkDNSRedirectEnabled!.Checked = _currentConfig.HostsRedirect?.Enabled ?? false;
		_txtHostsUrl!.Text = _currentConfig.HostsRedirect?.HostsUrl ?? GitHub520HostsProvider.DefaultUrl;
		SetRefreshDomainsToUi(_currentConfig.HostsRedirect?.RefreshDomains);
		_chkStartOnBoot!.Checked = StartupManager.IsEnabled();
		_chkAutoStartProxy!.Checked = _currentConfig.AutoStartProxy;
		_txtConfigName!.Text = _currentConfig.ConfigName;
	}

	private List<string> GetProcessNamesFromUi()
	{
		return _txtProcesses!.Lines
			.Select(static line => TargetRuleNormalizer.NormalizeProcessName(line))
			.Where(static line => !string.IsNullOrWhiteSpace(line))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void SetProcessNamesToUi(IEnumerable<string>? rules)
	{
		_txtProcesses!.Lines = rules is null
			? []
			: rules
				.Select(static line => TargetRuleNormalizer.NormalizeProcessName(line))
				.Where(static line => !string.IsNullOrWhiteSpace(line))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
	}

	private List<string> GetDomainRulesFromUi()
	{
		return _txtDomainRules!.Lines
			.Select(static line => TargetRuleNormalizer.NormalizeDomain(line))
			.Where(static line => !string.IsNullOrWhiteSpace(line))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void SetDomainRulesToUi(IEnumerable<string>? rules)
	{
		_txtDomainRules!.Lines = rules is null
			? []
			: rules
				.Select(static line => TargetRuleNormalizer.NormalizeDomain(line))
				.Where(static line => !string.IsNullOrWhiteSpace(line))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
	}

	private List<string> GetRefreshDomainsFromUi()
	{
		return _txtRefreshDomains!.Lines
			.Select(static line => TargetRuleNormalizer.NormalizeDomain(line))
			.Where(static line => !string.IsNullOrWhiteSpace(line)
							   && !line.Contains('*') && !line.Contains('?'))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private void SetRefreshDomainsToUi(IEnumerable<string>? domains)
	{
		var list = domains?.ToList();
		_txtRefreshDomains!.Lines = list is null || list.Count == 0
			? [.. ProxyOptions.DefaultRefreshDomains]
			: list
				.Select(static line => TargetRuleNormalizer.NormalizeDomain(line))
				.Where(static line => !string.IsNullOrWhiteSpace(line))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();
	}

	private void LoadConfigFromPath(string configPath, string dialogTitle)
	{
		if (string.IsNullOrWhiteSpace(configPath))
			throw new ArgumentException("Config path cannot be empty.", nameof(configPath));

		_activeConfigPath = Path.GetFullPath(configPath);
		_currentConfig = _configManager.Load(_activeConfigPath);
		LoadConfigToUI();
		RefreshConfigShortcutButtons();

		MessageBox.Show(
			$"Configuration loaded successfully.\n{_activeConfigPath}",
			dialogTitle,
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private void SaveConfig(string configPath, string dialogTitle)
	{
		if (string.IsNullOrWhiteSpace(configPath))
			throw new ArgumentException("Config path cannot be empty.", nameof(configPath));

		_currentConfig = BuildConfigModel();
		_activeConfigPath = Path.GetFullPath(configPath);
		_configManager.Save(_currentConfig, _activeConfigPath);
		LoadConfigToUI();
		RefreshConfigShortcutButtons();

		MessageBox.Show(
			$"Configuration saved successfully.\n{_activeConfigPath}",
			dialogTitle,
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private void RefreshConfigShortcutButtons()
	{
		_quickConfigPanel!.Controls.Clear();

		foreach (var configPath in GetQuickConfigPaths())
		{
			_quickConfigPanel.Controls.Add(CreateQuickConfigButton(configPath));
		}

		RefreshTrayConfigMenuItems();
	}

	private void RefreshTrayConfigMenuItems()
	{
		if (_trayConfigMenuItem is null || _trayConfigSeparator is null)
			return;

		var separatorIndex = _trayConfigMenuItem.DropDownItems.IndexOf(_trayConfigSeparator);
		if (separatorIndex < 0)
			return;

		while (_trayConfigMenuItem.DropDownItems.Count > separatorIndex + 1)
		{
			var lastIndex = _trayConfigMenuItem.DropDownItems.Count - 1;
			_trayConfigMenuItem.DropDownItems.RemoveAt(lastIndex);
		}

		var configPaths = GetQuickConfigPaths();
		_trayConfigSeparator.Visible = configPaths.Count > 0;

		foreach (var configPath in configPaths)
		{
			ToolStripMenuItem quickConfigMenuItem = new();
			quickConfigMenuItem.Name = $"_trayQuickConfig{_trayConfigMenuItem.DropDownItems.Count - separatorIndex}";
			quickConfigMenuItem.Tag = configPath;
			quickConfigMenuItem.Text = GetQuickConfigButtonText(configPath);
			quickConfigMenuItem.Click += TrayQuickConfigMenuItem_Click;
			_trayConfigMenuItem.DropDownItems.Add(quickConfigMenuItem);
		}
	}

	private IReadOnlyList<string> GetQuickConfigPaths()
	{
		List<string> configPaths = [];

		if (File.Exists(_activeConfigPath))
			configPaths.Add(_activeConfigPath);

		foreach (var configPath in _configManager.GetConfigPaths(5))
		{
			if (configPaths.Contains(configPath, StringComparer.OrdinalIgnoreCase))
				continue;

			configPaths.Add(configPath);
			if (configPaths.Count == 5)
				break;
		}

		return configPaths;
	}

	private Button CreateQuickConfigButton(string configPath)
	{
		Button button = new();
		button.Margin = new Padding(2);
		button.Name = $"_btnQuickConfig{_quickConfigPanel!.Controls.Count + 1}";
		button.Size = new Size(72, 30);
		button.TabIndex = _quickConfigPanel.Controls.Count;
		button.Tag = configPath;
		button.AutoEllipsis = true;
		button.Text = GetQuickConfigButtonText(configPath);
		button.UseVisualStyleBackColor = true;
		button.Click += BtnQuickConfig_Click;
 
		return button;
	}

	private string GetQuickConfigButtonText(string configPath)
	{
		return _configManager.GetConfigDisplayName(configPath);
	}

	private string GetConfigDialogDirectory()
	{
		if (!string.IsNullOrWhiteSpace(_activeConfigPath))
		{
			var activeConfigDirectory = Path.GetDirectoryName(_activeConfigPath);
			if (!string.IsNullOrWhiteSpace(activeConfigDirectory) && Directory.Exists(activeConfigDirectory))
				return activeConfigDirectory;
		}

		return _configManager.GetConfigDirectory();
	}

	private void BtnQuickConfig_Click(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.Tag is not string configPath || string.IsNullOrWhiteSpace(configPath))
			return;

		LoadConfigFromPath(configPath, "Load Config");
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

	private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
	{
		ShowWindow();
	}

	private void TrayShowMenuItem_Click(object? sender, EventArgs e)
	{
		ShowWindow();
	}

	private void TrayHideMenuItem_Click(object? sender, EventArgs e)
	{
		HideWindow();
	}

	private void TrayStartProxyMenuItem_Click(object? sender, EventArgs e)
	{
		BtnStartStop_Click(_btnStartStop, e);
	}

	private void TrayStopProxyMenuItem_Click(object? sender, EventArgs e)
	{
		BtnStartStop_Click(_btnStartStop, e);
	}

	private void TrayStartOnBootMenuItem_Click(object? sender, EventArgs e)
	{
		if (_trayStartOnBootMenuItem is null || _chkStartOnBoot is null)
			return;

		_chkStartOnBoot.Checked = _trayStartOnBootMenuItem.Checked;
	}

	private void TrayAutoStartProxyMenuItem_Click(object? sender, EventArgs e)
	{
		if (_trayAutoStartProxyMenuItem is null || _chkAutoStartProxy is null)
			return;

		_chkAutoStartProxy.Checked = _trayAutoStartProxyMenuItem.Checked;
	}

	private void TrayLoadConfigMenuItem_Click(object? sender, EventArgs e)
	{
		BtnLoadConfig_Click(_btnLoadConfig, e);
	}

	private void TraySaveConfigAsMenuItem_Click(object? sender, EventArgs e)
	{
		BtnSaveConfigAs_Click(_btnSaveConfigAs, e);
	}

	private void TraySaveConfigMenuItem_Click(object? sender, EventArgs e)
	{
		BtnSaveConfig_Click(_btnSaveConfig, e);
	}

	private void TrayQuickConfigMenuItem_Click(object? sender, EventArgs e)
	{
		if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not string configPath || string.IsNullOrWhiteSpace(configPath))
			return;

		LoadConfigFromPath(configPath, "Load Config");
	}

	private void TrayExitMenuItem_Click(object? sender, EventArgs e)
	{
		ExitApplication();
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
			components?.Dispose();
			_autoUpdater.Dispose();
			_ipFetchCts?.Cancel();
			_ipFetchCts?.Dispose();
			StopAutoFetch();
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

	protected override async void OnShown(EventArgs e)
	{
		base.OnShown(e);

		if (_initialAutoStartHandled || !_currentConfig.AutoStartProxy)
			return;

		_initialAutoStartHandled = true;

		if (!_chkProxyEnabled!.Checked && !_chkDNSRedirectEnabled!.Checked)
			return;

		try
		{
			await StartProxyAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error: {ex.Message}", "Proxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_btnStartStop!.Text = "Start Proxy";
			_btnStartStop.BackColor = Color.LimeGreen;
			_lblStatus!.Text = "Status: Stopped";
			_engine?.Dispose();
			_engine = null;
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

	private async void BtnFetchIps_Click(object? sender, EventArgs e)
	{
		// Toggle: clicking again cancels an in-progress fetch
		if (_ipFetchCts is not null)
		{
			_ipFetchCts.Cancel();
			_btnFetchIps!.Text = "Fetch IPs via DoH";
			return;
		}

		var domains = GetRefreshDomainsFromUi();

		if (domains.Count == 0)
		{
			MessageBox.Show(
				"No domains in the Refresh Domains list.",
				"No Domains", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		_ipFetchCts = new CancellationTokenSource();
		_btnFetchIps!.Text = "Cancel";

		try
		{
			await RunFetchCycleAsync(_ipFetchCts.Token);
		}
		catch (OperationCanceledException)
		{
			_lblHostsStatus!.Text = "Cancelled.";
		}
		finally
		{
			_btnFetchIps!.Text = "Fetch IPs via DoH";
			_ipFetchCts.Dispose();
			_ipFetchCts = null;
		}
	}

	/// <summary>Resolves concrete domain IPs via DoH, updates the ListView, and pushes results to the running engine.</summary>
	private async Task RunFetchCycleAsync(CancellationToken ct)
	{
		var domains = GetRefreshDomainsFromUi();

		if (domains.Count == 0)
			return;

		// Pre-populate every domain as "Pending" so the list is visible before any result arrives
		_lvIpResults!.Items.Clear();
		var itemMap = new Dictionary<string, ListViewItem>(domains.Count, StringComparer.OrdinalIgnoreCase);
		foreach (var domain in domains)
		{
			var pending = new ListViewItem(domain);
				pending.SubItems.Add("-");
				pending.SubItems.Add("Pending...");
				pending.SubItems.Add("-");
				pending.SubItems.Add("-");
				pending.ForeColor = SystemColors.GrayText;
			_lvIpResults.Items.Add(pending);
			itemMap[domain] = pending;
		}

		_lblHostsStatus!.Text = $"Resolving {domains.Count} domains...";

		var updates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		int resolved = 0, failed = 0;

		// No BeginUpdate/EndUpdate — each result streams in immediately as it completes
		using var service = new IpFetchService(
				proxyHost: _currentConfig.Proxy?.Host,
				proxyPort: (int)(_currentConfig.Proxy?.Port ?? 0),
				proxyScheme: _currentConfig.Proxy?.Scheme ?? "socks4");
			await foreach (var result in service.FetchAllAsync(domains, ct))
		{
			if (result.Ip is not null && IPAddress.TryParse(result.Ip, out var addr))
				updates[result.Domain] = addr.GetAddressBytes();

			// Update the pre-populated row in place
			if (itemMap.TryGetValue(result.Domain, out var item))
				ApplyIpResultToItem(item, result);
			else
				_lvIpResults.Items.Add(CreateIpListViewItem(result));

			if (result.Ip is not null && result.LatencyMs is >= 0 and < 800)
				resolved++;
			else
				failed++;

			_lblHostsStatus.Text = $"Resolved: {resolved} | Failed: {failed} | Remaining: {domains.Count - resolved - failed}";
		}

		_lblHostsStatus.Text = $"Done \u2014 Resolved: {resolved} | Failed: {failed} | Total: {domains.Count}";
		_engine?.UpdateHostsEntries(updates);
	}

	private static ListViewItem CreateIpListViewItem(DomainIpResult result)
	{
		var item = new ListViewItem(result.Domain);
		item.SubItems.Add("-");
		item.SubItems.Add("-");
		item.SubItems.Add("-");
		item.SubItems.Add("-");
		ApplyIpResultToItem(item, result);

		return item;
	}

	private static void ApplyIpResultToItem(ListViewItem item, DomainIpResult result)
	{
		item.SubItems[1].Text = result.Ip ?? "-";
		item.SubItems[2].Text = result.LatencyMs >= 0 ? $"{result.LatencyMs} ms" : result.Error ?? "Failed";
		item.SubItems[3].Text = result.ProxyLatencyMs >= 0
			? $"{result.ProxyLatencyMs} ms"
			: result.ProxyError ?? "-";
		item.SubItems[4].Text = result.DohSource ?? "-";

		if (result.Ip is null)
			item.ForeColor = Color.Red;
		else if (result.LatencyMs is >= 0 and < 200)
			item.ForeColor = Color.Green;
		else if (result.LatencyMs is >= 200 and < 800)
			item.ForeColor = Color.DarkOrange;
		else
			item.ForeColor = Color.Red;
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

			UpdateTrayMenuState();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Failed to update startup setting: {ex.Message}", "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_chkStartOnBoot!.CheckedChanged -= ChkStartOnBoot_CheckedChanged;
			_chkStartOnBoot.Checked = StartupManager.IsEnabled();
			_chkStartOnBoot.CheckedChanged += ChkStartOnBoot_CheckedChanged;
			UpdateTrayMenuState();
		}
	}

	private void ChkAutoStartProxy_CheckedChanged(object? sender, EventArgs e)
	{
		UpdateTrayMenuState();
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

	private void ChkAutoFetch_CheckedChanged(object? sender, EventArgs e)
	{
		if (_chkAutoFetch!.Checked)
			StartAutoFetch();
		else
			StopAutoFetch();
	}

	private void StartAutoFetch()
	{
		StopAutoFetch();
		_autoFetchCts = new CancellationTokenSource();
		_autoFetchTask = RunAutoFetchLoopAsync((int)_numAutoFetchInterval!.Value, _autoFetchCts.Token);
	}

	private void StopAutoFetch()
	{
		_autoFetchCts?.Cancel();
		_autoFetchCts?.Dispose();
		_autoFetchCts = null;
		_autoFetchTask = null;
	}

	private async Task RunAutoFetchLoopAsync(int intervalMinutes, CancellationToken ct)
	{
		try
		{
			using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
			while (true)
			{
				await _lvIpResults!.InvokeAsync(async ct2 => await RunFetchCycleAsync(ct2), ct);
				if (!await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
					break;
			}
		}
		catch (OperationCanceledException) { }
	}
}
