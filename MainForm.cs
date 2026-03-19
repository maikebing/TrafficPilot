using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
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
	private bool _startMinimized;

	private CancellationTokenSource? _ipFetchCts;
	private CancellationTokenSource? _autoFetchCts;
	private Task? _autoFetchTask;

	private bool _localProxySubscribed;
	private CancellationTokenSource? _networkChangeCts;

	private TabPage? _localApiTab;
	private TableLayoutPanel? _localApiPanel;
	private CheckBox? _chkLocalApiForwarderEnabled;
	private NumericUpDown? _numOllamaPort;
	private NumericUpDown? _numFoundryPort;
	private ComboBox? _cmbLocalApiProviderProtocol;
	private TextBox? _txtLocalApiProviderName;
	private TextBox? _txtLocalApiProviderUrl;
	private ComboBox? _cmbLocalApiDefaultModel;
	private ComboBox? _cmbLocalApiDefaultEmbeddingModel;
	private ComboBox? _cmbLocalApiAuthType;
	private TextBox? _txtLocalApiAuthHeaderName;
	private TextBox? _txtLocalApiApiKey;
	private Button? _btnRefreshLocalApiModels;
	private TextBox? _txtLocalApiAdditionalHeaders;
	private TextBox? _txtLocalApiModelMappings;
	private CheckBox? _chkLocalApiRequestResponseLogging;
	private CheckBox? _chkLocalApiIncludeBodies;
	private CheckBox? _chkLocalApiIncludeErrorDiagnostics;
	private NumericUpDown? _numLocalApiMaxBodyChars;
	private Dictionary<string, string> _localApiModelNameMap = new(StringComparer.OrdinalIgnoreCase);

	public MainForm(bool startMinimized = false)
	{
		_startMinimized = startMinimized;
		_configManager = new ProxyConfigManager();
		_activeConfigPath = _configManager.GetConfigPath();
		_currentConfig = _configManager.Load(_activeConfigPath);
		_logBuffer = new LogBuffer(BatchAppendLogs);
		_autoUpdater = new AutoUpdater();
		InitializeComponent();
		InitializeLocalApiForwarderTab();
		InitIpResultsColumns();
		InitProxyHostComboBox();
		InitializeHostsRedirectModeUI(); // 添加hosts模式UI
		LoadApplicationIcon();
		LoadVersionLabel();
		CenterToScreen();
		LoadConfigToUI();
		RefreshConfigShortcutButtons();
		UpdateTrayMenuState();
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

		return new Version(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build), Math.Max(0, version.Revision));
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
				if (!_chkProxyEnabled!.Checked && !_chkDNSRedirectEnabled!.Checked && !(_chkLocalApiForwarderEnabled?.Checked ?? false))
				{
					MessageBox.Show(
						"Proxy, Hosts Redirect, and Local API Forwarding are all disabled. Please enable at least one before starting.",
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
			var persistedConfig = PersistCurrentConfig();
			LogRuntimeConfigSnapshot(persistedConfig);

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

	private void BtnResetConfig_Click(object? sender, EventArgs e)
	{
		var result = MessageBox.Show(
			"This will reset process rules, domain rules, and DNS refresh domains to their defaults. Continue?",
			"Reset to Defaults",
			MessageBoxButtons.YesNo,
			MessageBoxIcon.Warning);

		if (result != DialogResult.Yes)
			return;

		SetProcessNamesToUi(ProxyOptions.DefaultProcessNames);
		SetDomainRulesToUi(ProxyOptions.DefaultDomainRules);
		SetRefreshDomainsToUi(ProxyOptions.DefaultRefreshDomains);
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

	private async void BtnSyncPush_Click(object? sender, EventArgs e)
	{
		string provider = _cmbSyncProvider!.SelectedItem?.ToString() ?? "GitHub";
		string token = _txtSyncToken!.Text.Trim();
		string? gistId = _txtGistId!.Text.Trim();
		if (string.IsNullOrEmpty(gistId)) gistId = null;

		if (string.IsNullOrEmpty(token))
		{
			MessageBox.Show("Please enter a personal access token before pushing.", "Sync Error",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnSyncPush!.Enabled = false;
		_btnSyncPull!.Enabled = false;

		try
		{
			// Save current config to a JSON string
			var model = BuildConfigModel();
			var json = System.Text.Json.JsonSerializer.Serialize(model,
				new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

			using var syncProvider = ConfigSyncProviderFactory.Create(provider, token);
			string newId = await syncProvider.PushAsync(json, gistId).ConfigureAwait(true);

			// Persist the returned gist ID back to the config
			_txtGistId.Text = newId;
			_currentConfig.ConfigSync = new ConfigSyncSettings { Provider = provider, GistId = newId };
			_configManager.Save(_currentConfig, _activeConfigPath);

			MessageBox.Show($"Config pushed successfully to {provider}.\nGist/Snippet ID: {newId}",
				"Sync Push", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Push failed: {ex.Message}", "Sync Error",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_btnSyncPush.Enabled = true;
			_btnSyncPull!.Enabled = true;
		}
	}

	private async void BtnSyncPull_Click(object? sender, EventArgs e)
	{
		string provider = _cmbSyncProvider!.SelectedItem?.ToString() ?? "GitHub";
		string token = _txtSyncToken!.Text.Trim();
		string gistId = _txtGistId!.Text.Trim();

		if (string.IsNullOrEmpty(token))
		{
			MessageBox.Show("Please enter a personal access token before pulling.", "Sync Error",
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_btnSyncPush!.Enabled = false;
		_btnSyncPull!.Enabled = false;

		try
		{
			using var syncProvider = ConfigSyncProviderFactory.Create(provider, token);

			// Auto-discover the TrafficPilot gist when no ID is supplied
			if (string.IsNullOrEmpty(gistId))
			{
				string? discoveredId = await syncProvider.FindGistIdAsync().ConfigureAwait(true);
				if (discoveredId is null)
				{
					MessageBox.Show(
						$"No TrafficPilot configuration found in your {provider} gists.\nPlease push your config first.",
						"Sync Pull", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				gistId = discoveredId;
			}

			string json = await syncProvider.PullAsync(gistId).ConfigureAwait(true);

			var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var model = System.Text.Json.JsonSerializer.Deserialize<ProxyConfigModel>(json, options);
			if (model is null)
				throw new InvalidOperationException("Remote config could not be deserialized.");

			// Preserve sync settings from the current session
			model.ConfigSync = new ConfigSyncSettings { Provider = provider, GistId = gistId };
			_currentConfig = model;
			_configManager.Save(_currentConfig, _activeConfigPath);
			LoadConfigToUI();

			MessageBox.Show("Config pulled and applied successfully.", "Sync Pull",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Pull failed: {ex.Message}", "Sync Error",
				MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		finally
		{
			_btnSyncPush.Enabled = true;
			_btnSyncPull!.Enabled = true;
		}
	}

	private ProxyConfigModel BuildConfigModel()
	{
		return new ProxyConfigModel
		{
			ConfigName = _txtConfigName!.Text.Trim(),
			Proxy = new ProxySettings
			{
				Enabled = _chkProxyEnabled!.Checked,
				Host = _cmbProxyHost!.Text,
				Port = (ushort)_numProxyPort!.Value,
				Scheme = _cmbProxyScheme!.SelectedItem?.ToString() ?? "socks5",
				IsLocalProxy = _chkLocalProxy!.Checked
			},
			Targeting = new TargetingSettings
			{
				ProcessNames = GetProcessNamesFromUi(),
				DomainRules = GetDomainRulesFromUi()
			},
			HostsRedirect = new HostsRedirectSettings
			{
				Enabled = _chkDNSRedirectEnabled!.Checked,
				Mode = _rdoHostsFile?.Checked == true ? "HostsFile" : "DnsInterception",
				HostsUrl = _txtHostsUrl!.Text.Trim(),
				RefreshDomains = GetRefreshDomainsFromUi()
			},
			StartOnBoot = _chkStartOnBoot!.Checked,
			AutoStartProxy = _chkAutoStartProxy!.Checked,
			ConfigSync = BuildConfigSyncSettings(),
			LocalApiForwarder = BuildLocalApiForwarderSettings()
		};
	}

	private ConfigSyncSettings? BuildConfigSyncSettings()
	{
		string provider = _cmbSyncProvider!.SelectedItem?.ToString() ?? "GitHub";
		string token = _txtSyncToken!.Text.Trim();
		string? gistId = _txtGistId!.Text.Trim();
		if (string.IsNullOrEmpty(gistId)) gistId = null;

		// Save token to Windows Credential Manager (never in config file)
		string targetName = CredentialManager.GetTargetName(provider);
		if (string.IsNullOrEmpty(token))
			CredentialManager.DeleteToken(targetName);
		else
			CredentialManager.SaveToken(targetName, token);

		if (string.IsNullOrEmpty(token) && gistId is null)
			return null;
		return new ConfigSyncSettings { Provider = provider, GistId = gistId };
	}

	private ProxyOptions BuildProxyOptions()
	{
		bool hostsEnabled = _chkDNSRedirectEnabled?.Checked ?? 
			(_rdoDnsInterception?.Checked == true || _rdoHostsFile?.Checked == true);
		string hostsMode = _rdoHostsFile?.Checked == true ? "HostsFile" : "DnsInterception";

		return new ProxyOptions(
			GetProcessNamesFromUi(),
			GetDomainRulesFromUi(),
			_cmbProxyHost!.Text,
			(ushort)_numProxyPort!.Value,
			_cmbProxyScheme!.SelectedItem?.ToString() ?? "socks5",
			_chkProxyEnabled!.Checked,
			hostsEnabled,
			_txtHostsUrl!.Text.Trim(),
			hostsMode,
			BuildLocalApiForwarderSettings()
		);
	}

	private void LoadConfigToUI()
	{
		_chkProxyEnabled!.Checked = _currentConfig.Proxy?.Enabled ?? true;
		_cmbProxyHost!.Text = _currentConfig.Proxy?.Host ?? "";
		_numProxyPort!.Value = _currentConfig.Proxy?.Port ?? 7890;
		_cmbProxyScheme!.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks5";
		_chkLocalProxy!.Checked = _currentConfig.Proxy?.IsLocalProxy ?? false;
		_lblConfigFileValue!.Text = _activeConfigPath;

		SetProcessNamesToUi(_currentConfig.Targeting?.ProcessNames);
		SetDomainRulesToUi(_currentConfig.Targeting?.DomainRules);

		_chkDNSRedirectEnabled!.Checked = _currentConfig.HostsRedirect?.Enabled ?? false;
		_txtHostsUrl!.Text = _currentConfig.HostsRedirect?.HostsUrl ?? GitHub520HostsProvider.DefaultUrl;
		SetRefreshDomainsToUi(_currentConfig.HostsRedirect?.RefreshDomains);
		_chkStartOnBoot!.Checked = StartupManager.IsEnabled();
		_chkAutoStartProxy!.Checked = _currentConfig.AutoStartProxy;
		_txtConfigName!.Text = _currentConfig.ConfigName;
		LoadLocalApiForwarderToUi(_currentConfig.LocalApiForwarder);

		// Load sync settings
		string syncProvider = _currentConfig.ConfigSync?.Provider ?? "GitHub";
		_cmbSyncProvider!.SelectedItem = _cmbSyncProvider.Items.Contains(syncProvider) ? syncProvider : "GitHub";
		_txtSyncToken!.Text = CredentialManager.LoadToken(CredentialManager.GetTargetName(syncProvider)) ?? string.Empty;
		_txtGistId!.Text = _currentConfig.ConfigSync?.GistId ?? string.Empty;

		// 加载hosts redirect模式
		string mode = _currentConfig.HostsRedirect?.Mode ?? "DnsInterception";
		if (_rdoHostsFile is not null && _rdoDnsInterception is not null)
		{
			if (mode.Equals("HostsFile", StringComparison.OrdinalIgnoreCase))
				_rdoHostsFile.Checked = true;
			else
				_rdoDnsInterception.Checked = true;
		}
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

		PersistCurrentConfig(configPath);
		LoadConfigToUI();
		RefreshConfigShortcutButtons();

		MessageBox.Show(
			$"Configuration saved successfully.\n{_activeConfigPath}",
			dialogTitle,
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private ProxyConfigModel PersistCurrentConfig(string? configPath = null)
	{
		var path = Path.GetFullPath(configPath ?? _activeConfigPath);
		_currentConfig = BuildConfigModel();
		_activeConfigPath = path;
		_configManager.Save(_currentConfig, _activeConfigPath);
		return _currentConfig;
	}

	private void LogRuntimeConfigSnapshot(ProxyConfigModel config)
	{
		AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [Config] Active config: {_activeConfigPath}");

		var localApi = config.LocalApiForwarder;
		if (localApi?.Enabled != true)
			return;

		var provider = localApi.Provider ?? new LocalApiProviderSettings();
		var defaultModel = string.IsNullOrWhiteSpace(provider.DefaultModel)
			? "<empty>"
			: provider.DefaultModel;
		var embeddingModel = string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModel)
			? "<empty>"
			: provider.DefaultEmbeddingModel;
		AppendLog(
			$"[{DateTime.Now:HH:mm:ss.fff}] [Config] Local API provider='{provider.Name}', protocol={provider.Protocol}, baseUrl={provider.BaseUrl}, defaultModel={defaultModel}, embeddingModel={embeddingModel}");
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
		if (_rtbLogs is null || _rtbLogs.IsDisposed || !_rtbLogs.IsHandleCreated)
			return;

		if (_rtbLogs!.InvokeRequired)
		{
			try
			{
				_rtbLogs.BeginInvoke(() => BatchAppendLogs(messages));
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (InvalidOperationException)
			{
				return;
			}
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
		lblBytes.Text = RedirectStats.FormatBytes(stats.Bytes);
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

		PersistCurrentConfig();
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
			if (_localProxySubscribed)
			{
				NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
				_localProxySubscribed = false;
			}
			_networkChangeCts?.Cancel();
			_networkChangeCts?.Dispose();
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

		// Handle minimized startup (e.g., from Windows startup)
		if (_startMinimized)
		{
			HideWindow();
		}

		if (_initialAutoStartHandled || !_currentConfig.AutoStartProxy)
			return;

		_initialAutoStartHandled = true;

		if (!_chkProxyEnabled!.Checked && !_chkDNSRedirectEnabled!.Checked && !(_chkLocalApiForwarderEnabled?.Checked ?? false))
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

		var retryOnly = _chkRetestSlowOrTimeoutOnly?.Checked == true;
		var domains = retryOnly
			? GetDomainsForRetryFromResults()
			: GetRefreshDomainsFromUi();

		if (domains.Count == 0)
		{
			MessageBox.Show(
				retryOnly
					? "No timeout/high-latency domains available to retest."
					: "No domains in the Refresh Domains list.",
				retryOnly ? "No Retry Targets" : "No Domains",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
			return;
		}

		_ipFetchCts = new CancellationTokenSource();
		_btnFetchIps!.Text = "Cancel";

		try
		{
			await RunFetchCycleAsync(domains, _ipFetchCts.Token);
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
	private Task RunFetchCycleAsync(CancellationToken ct)
	{
		var domains = GetRefreshDomainsFromUi();
		return RunFetchCycleAsync(domains, ct);
	}

	private async Task RunFetchCycleAsync(IReadOnlyList<string> domains, CancellationToken ct)
	{
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
				proxyScheme: _currentConfig.Proxy?.Scheme ?? "socks5");
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

	private void ChkLocalProxy_CheckedChanged(object? sender, EventArgs e)
	{
		if (_chkLocalProxy!.Checked)
		{
			UpdateProxyHostToLocalIp();
			if (!_localProxySubscribed)
			{
				NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
				_localProxySubscribed = true;
			}
		}
		else
		{
			if (_localProxySubscribed)
			{
				NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
				_localProxySubscribed = false;
			}
			_networkChangeCts?.Cancel();
		}
	}

	private void OnNetworkAddressChanged(object? sender, EventArgs e)
	{
		if (!(_chkLocalProxy?.Checked ?? false))
			return;

		// Cancel any pending update and schedule a fresh one after a short delay
		// so the OS finishes reconfiguring the interface before we query IP addresses
		_networkChangeCts?.Cancel();
		_networkChangeCts?.Dispose();
		var cts = new CancellationTokenSource();
		_networkChangeCts = cts;

		Task.Delay(1000, cts.Token).ContinueWith(t =>
		{
			if (t.IsCanceled || IsDisposed) return;
			try
			{
				Invoke(UpdateProxyHostToLocalIp);
			}
			catch (ObjectDisposedException) { }
			catch (InvalidOperationException) { }
		}, TaskScheduler.Default);
	}

	private void UpdateProxyHostToLocalIp()
	{
		var ips = LocalNetworkHelper.GetLocalIpsWithGateway();
		if (ips.Count == 0)
			return;

		var firstIp = ips[0];
		_cmbProxyHost!.Items.Clear();
		foreach (var ip in ips)
			_cmbProxyHost.Items.Add(ip);
		_cmbProxyHost.Text = firstIp;
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

	private async void BtnRefreshLocalApiModels_Click(object? sender, EventArgs e)
	{
		if (_btnRefreshLocalApiModels is null)
			return;

		var originalText = _btnRefreshLocalApiModels.Text;
		_btnRefreshLocalApiModels.Enabled = false;
		_btnRefreshLocalApiModels.Text = "Refreshing...";

		try
		{
			var persistedConfig = PersistCurrentConfig();
			LogRuntimeConfigSnapshot(persistedConfig);

			var localApiSettings = persistedConfig.LocalApiForwarder;

			IReadOnlyList<LocalApiAdvertisedModel> models;
			if (_engine?.IsRunning == true && _engine.IsLocalApiForwarderRunning)
			{
				AppendLog(
					$"[{DateTime.Now:HH:mm:ss.fff}] [Config] Refreshing upstream model catalog for the running Local API forwarder. Restart the proxy to apply any provider changes currently shown in the UI.");
				models = await _engine.RefreshLocalApiModelCatalogEntriesAsync();
			}
			else
			{
				if (localApiSettings?.Enabled != true)
					throw new InvalidOperationException("Enable local Ollama / Foundry forwarding before refreshing models.");

				AppendLog(
					$"[{DateTime.Now:HH:mm:ss.fff}] [Config] Refreshing upstream model catalog using the current UI settings (proxy not running).");
				using var forwarder = CreateDetachedLocalApiForwarder(localApiSettings);
				forwarder.OnLog += AppendLog;
				models = await forwarder.RefreshModelCatalogEntriesAsync();
			}

			ReplaceLocalApiModelNameMap(models);
			var selectedDefaultModel = NormalizeLocalApiModelDisplayName(_cmbLocalApiDefaultModel?.Text ?? string.Empty);
			var selectedEmbeddingModel = NormalizeLocalApiModelDisplayName(_cmbLocalApiDefaultEmbeddingModel?.Text ?? string.Empty);
			var preview = models.Count == 0
				? "<none>"
				: string.Join(", ", models.Select(static model => model.LocalName).Take(5)) + (models.Count > 5 ? ", ..." : string.Empty);
			UpdateLocalApiModelSelectors(
				models.Select(static model => model.LocalName),
				selectedDefaultModel,
				selectedEmbeddingModel);
			AppendLog(
				$"[{DateTime.Now:HH:mm:ss.fff}] [Config] Upstream model refresh completed: {models.Count} models ({preview})");
		}
		catch (Exception ex)
		{
			AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [Config] Upstream model refresh failed: {ex}");
			MessageBox.Show(
				$"Failed to refresh upstream models: {ex.Message}",
				"Local API Models",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
		finally
		{
			_btnRefreshLocalApiModels.Text = originalText;
			_btnRefreshLocalApiModels.Enabled = true;
		}
	}

	private static LocalApiForwarder CreateDetachedLocalApiForwarder(LocalApiForwarderSettings settings)
	{
		var providerName = settings.Provider?.Name ?? "Default";
		var apiKey = CredentialManager.LoadToken(CredentialManager.GetLocalApiTargetName(providerName));
		return new LocalApiForwarder(settings, apiKey);
	}

	private void UpdateLocalApiModelSelectors(
		IEnumerable<string>? models,
		string selectedDefaultModel,
		string selectedEmbeddingModel)
	{
		if (_cmbLocalApiDefaultModel is null || _cmbLocalApiDefaultEmbeddingModel is null)
			return;

		var normalizedItems = BuildLocalApiSelectorDisplayOrder(
			models,
			selectedDefaultModel,
			selectedEmbeddingModel);

		RebindEditableComboBox(_cmbLocalApiDefaultModel, normalizedItems, selectedDefaultModel);
		RebindEditableComboBox(_cmbLocalApiDefaultEmbeddingModel, normalizedItems, selectedEmbeddingModel);
	}

	private static string[] BuildLocalApiSelectorDisplayOrder(
		IEnumerable<string>? models,
		string selectedDefaultModel,
		string selectedEmbeddingModel)
	{
		var orderedItems = new List<string>();

		if (!string.IsNullOrWhiteSpace(selectedDefaultModel))
			orderedItems.Add(selectedDefaultModel.Trim());
		if (!string.IsNullOrWhiteSpace(selectedEmbeddingModel))
			orderedItems.Add(selectedEmbeddingModel.Trim());

		orderedItems.AddRange((models ?? Enumerable.Empty<string>())
			.Select(static model => model?.Trim() ?? string.Empty)
			.Where(static model => !string.IsNullOrWhiteSpace(model))
			.Reverse());

		return orderedItems
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private IEnumerable<string> BuildLocalApiSelectorItems(LocalApiForwarderSettings settings)
	{
		var items = new List<string>();
		ReplaceLocalApiModelNameMap(BuildConfiguredLocalApiSelectorModels(settings));

		if (!string.IsNullOrWhiteSpace(settings.Provider?.DefaultModel))
			items.Add(NormalizeLocalApiModelDisplayName(settings.Provider.DefaultModel));
		if (!string.IsNullOrWhiteSpace(settings.Provider?.DefaultEmbeddingModel))
			items.Add(NormalizeLocalApiModelDisplayName(settings.Provider.DefaultEmbeddingModel));

		items.AddRange(settings.ModelMappings
			.Where(static mapping => !string.IsNullOrWhiteSpace(mapping.UpstreamModel))
			.Select(mapping => NormalizeLocalApiModelDisplayName(mapping.UpstreamModel)));

		return items;
	}

	private static void RebindEditableComboBox(ComboBox comboBox, IEnumerable<string> items, string selectedText)
	{
		comboBox.BeginUpdate();
		try
		{
			comboBox.Items.Clear();
			comboBox.Items.AddRange(items.Cast<object>().ToArray());
			comboBox.Text = selectedText ?? string.Empty;
		}
		finally
		{
			comboBox.EndUpdate();
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

	private void RdoDnsInterception_CheckedChanged(object? sender, EventArgs e)
	{
		if (_rdoDnsInterception?.Checked == true)
		{
			if (_engine?.IsRunning == true)
			{
				_lblHostsStatus!.Text = "Status: Mode changed. Please Stop and Start again to apply.";
				_lblHostsStatus.ForeColor = Color.Orange;
			}
		}
	}

	private void RdoHostsFile_CheckedChanged(object? sender, EventArgs e)
	{
		if (_rdoHostsFile?.Checked == true)
		{
			if (_engine?.IsRunning == true)
			{
				_lblHostsStatus!.Text = "Status: Mode changed. Please Stop and Start again to apply.";
				_lblHostsStatus.ForeColor = Color.Orange;
			}
		}
	}

	/// <summary>
	/// Loads hosts redirect mode from configuration and updates UI
	/// </summary>
	private void LoadHostsRedirectMode()
	{
		if (_rdoDnsInterception is null || _rdoHostsFile is null)
			return;

		string mode = _currentConfig.HostsRedirect?.Mode ?? "DnsInterception";

		if (mode.Equals("HostsFile", StringComparison.OrdinalIgnoreCase))
			_rdoHostsFile.Checked = true;
		else
			_rdoDnsInterception.Checked = true;
	}

	/// <summary>
	/// Returns the currently selected hosts redirect mode string from UI
	/// </summary>
	private string GetHostsRedirectModeFromUI()
	{
		if (_rdoHostsFile?.Checked == true)
			return "HostsFile";
		return "DnsInterception";
	}

	/// <summary>
	/// Creates ProxyOptions with hosts redirect mode from UI
	/// </summary>
	private ProxyOptions CreateProxyOptionsFromUI()
	{
		return new ProxyOptions(
			GetProcessNamesFromUi(),
			GetDomainRulesFromUi(),
			_cmbProxyHost!.Text.Trim(),
			(ushort)_numProxyPort!.Value,
			_cmbProxyScheme!.SelectedItem?.ToString() ?? "socks5",
			ProxyEnabled: _chkProxyEnabled!.Checked,
			HostsRedirectEnabled: _chkDNSRedirectEnabled?.Checked ?? (_rdoDnsInterception?.Checked == true || _rdoHostsFile?.Checked == true),
			HostsRedirectUrl: _txtHostsUrl!.Text.Trim(),
			HostsRedirectMode: GetHostsRedirectModeFromUI()
		);
	}

	private List<string> GetDomainsForRetryFromResults()
	{
		if (_lvIpResults is null)
			return [];

		var domains = new List<string>();
		foreach (ListViewItem item in _lvIpResults.Items)
		{
			if (item.SubItems.Count < 3)
				continue;

			var domain = TargetRuleNormalizer.NormalizeDomain(item.Text);
			if (string.IsNullOrWhiteSpace(domain))
				continue;

			var latencyText = item.SubItems[2].Text.Trim();
			var shouldRetry = item.ForeColor == Color.Red;

			if (!shouldRetry && TryParseLatencyMs(latencyText, out var latencyMs))
				shouldRetry = latencyMs >= 800;

			if (!shouldRetry && (latencyText.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
				|| latencyText.Contains("Failed", StringComparison.OrdinalIgnoreCase)))
				shouldRetry = true;

			if (shouldRetry)
				domains.Add(domain);
		}

		return domains
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static bool TryParseLatencyMs(string text, out long latencyMs)
	{
		latencyMs = -1;
		if (!text.EndsWith(" ms", StringComparison.OrdinalIgnoreCase))
			return false;

		return long.TryParse(text[..^3].Trim(), out latencyMs);
	}

	private void InitializeLocalApiForwarderTab()
	{
		if (_tabControl is null)
			return;

		_localApiTab = new TabPage("Local API");
		_localApiPanel = new TableLayoutPanel
		{
			ColumnCount = 2,
			RowCount = 11,
			Dock = DockStyle.Fill,
			Padding = new Padding(10)
		};
		_localApiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
		_localApiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		for (var i = 0; i < 10; i++)
			_localApiPanel.RowStyles.Add(new RowStyle());
		_localApiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

		var header = new FlowLayoutPanel
		{
			AutoSize = true,
			Dock = DockStyle.Top,
			WrapContents = false
		};
		_chkLocalApiForwarderEnabled = new CheckBox
		{
			AutoSize = true,
			Text = "Enable local Ollama / Foundry forwarding",
			Margin = new Padding(3, 3, 12, 3)
		};
		var headerLabel = new Label
		{
			AutoSize = true,
			Text = "TrafficPilot keeps these listeners on loopback only for safety.",
			Margin = new Padding(3, 5, 3, 3)
		};
		header.Controls.Add(_chkLocalApiForwarderEnabled);
		header.Controls.Add(headerLabel);
		_localApiPanel.Controls.Add(header, 0, 0);
		_localApiPanel.SetColumnSpan(header, 2);

		var portPanel = new TableLayoutPanel
		{
			ColumnCount = 4,
			Dock = DockStyle.Fill
		};
		portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
		portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
		portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
		portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
		portPanel.Controls.Add(new Label
		{
			Text = "Ollama Port:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 0, 0);
		_numOllamaPort = CreatePortEditor(11434);
		portPanel.Controls.Add(_numOllamaPort, 1, 0);
		portPanel.Controls.Add(new Label
		{
			Text = "Foundry Port:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 2, 0);
		_numFoundryPort = CreatePortEditor(5273);
		portPanel.Controls.Add(_numFoundryPort, 3, 0);
		_localApiPanel.Controls.Add(new Label
		{
			Text = "Listen Ports:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 0, 1);
		_localApiPanel.Controls.Add(portPanel, 1, 1);

		var providerPanel = new TableLayoutPanel
		{
			ColumnCount = 4,
			Dock = DockStyle.Fill
		};
		providerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
		providerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		providerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
		providerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		providerPanel.Controls.Add(new Label
		{
			Text = "Protocol:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 0, 0);
		_cmbLocalApiProviderProtocol = new ComboBox
		{
			Dock = DockStyle.Fill,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		_cmbLocalApiProviderProtocol.Items.AddRange(["OpenAICompatible", "Anthropic"]);
		providerPanel.Controls.Add(_cmbLocalApiProviderProtocol, 1, 0);
		providerPanel.Controls.Add(new Label
		{
			Text = "Provider Name:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 2, 0);
		_txtLocalApiProviderName = CreateFillTextBox("Third-party provider display name");
		providerPanel.Controls.Add(_txtLocalApiProviderName, 3, 0);
		AddLocalApiRow(2, "Provider:", providerPanel);

		var providerUrlPanel = new TableLayoutPanel
		{
			ColumnCount = 2,
			Dock = DockStyle.Fill,
			Margin = Padding.Empty
		};
		providerUrlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		providerUrlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_txtLocalApiProviderUrl = CreateFillTextBox("https://api.openai.com/v1/");
		providerUrlPanel.Controls.Add(_txtLocalApiProviderUrl, 0, 0);
		_btnRefreshLocalApiModels = new Button
		{
			AutoSize = true,
			Text = "Refresh Models",
			Margin = new Padding(8, 0, 0, 0)
		};
		_btnRefreshLocalApiModels.Click += BtnRefreshLocalApiModels_Click;
		providerUrlPanel.Controls.Add(_btnRefreshLocalApiModels, 1, 0);
		AddLocalApiRow(3, "Provider Base URL:", providerUrlPanel);

		_cmbLocalApiDefaultModel = CreateEditableComboBox();
		_cmbLocalApiDefaultModel.Text = string.Empty;
		AddLocalApiRow(4, "Default Model:", _cmbLocalApiDefaultModel);

		_cmbLocalApiDefaultEmbeddingModel = CreateEditableComboBox();
		_cmbLocalApiDefaultEmbeddingModel.Text = string.Empty;
		AddLocalApiRow(5, "Embedding Model:", _cmbLocalApiDefaultEmbeddingModel);

		var authPanel = new TableLayoutPanel
		{
			ColumnCount = 4,
			Dock = DockStyle.Fill
		};
		authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
		authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
		authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
		authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		authPanel.Controls.Add(new Label
		{
			Text = "Auth Type:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 0, 0);
		_cmbLocalApiAuthType = new ComboBox
		{
			Dock = DockStyle.Fill,
			DropDownStyle = ComboBoxStyle.DropDownList
		};
		_cmbLocalApiAuthType.Items.AddRange(["Bearer", "Header", "Query"]);
		authPanel.Controls.Add(_cmbLocalApiAuthType, 1, 0);
		authPanel.Controls.Add(new Label
		{
			Text = "Header / Query Name:",
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 2, 0);
		_txtLocalApiAuthHeaderName = CreateFillTextBox("Authorization / x-api-key / key");
		authPanel.Controls.Add(_txtLocalApiAuthHeaderName, 3, 0);
		AddLocalApiRow(6, "Authentication:", authPanel);

		_txtLocalApiApiKey = CreateFillTextBox("Stored in Windows Credential Manager, not in config JSON");
		_txtLocalApiApiKey.PasswordChar = '●';
		AddLocalApiRow(7, "Provider API Key:", _txtLocalApiApiKey);

		_txtLocalApiAdditionalHeaders = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			WordWrap = false,
			PlaceholderText = "Optional additional upstream headers, one per line:\r\nanthropic-version=2023-06-01\r\nX-Agent-ID=my-agent"
		};
		AddLocalApiRow(8, "Extra Headers:", _txtLocalApiAdditionalHeaders);

		_txtLocalApiModelMappings = new TextBox
		{
			Dock = DockStyle.Fill,
			Multiline = true,
			ScrollBars = ScrollBars.Vertical,
			WordWrap = false,
			PlaceholderText = "One mapping per line:\r\nqwen2.5:7b=gpt-4.1-mini\r\nllama3.2=claude-3-7-sonnet"
		};
		AddLocalApiRow(9, "Model Mappings:", _txtLocalApiModelMappings);

		var loggingPanel = new FlowLayoutPanel
		{
			AutoSize = true,
			Dock = DockStyle.Top,
			WrapContents = true
		};
		_chkLocalApiRequestResponseLogging = new CheckBox
		{
			AutoSize = true,
			Text = "Enable request/response logging",
			Margin = new Padding(3, 3, 16, 3)
		};
		_chkLocalApiIncludeBodies = new CheckBox
		{
			AutoSize = true,
			Text = "Include bodies",
			Margin = new Padding(3, 3, 16, 3)
		};
		_chkLocalApiIncludeErrorDiagnostics = new CheckBox
		{
			AutoSize = true,
			Text = "Include error diagnostics in responses",
			Margin = new Padding(3, 3, 16, 3)
		};
		_numLocalApiMaxBodyChars = new NumericUpDown
		{
			Minimum = 256,
			Maximum = 20000,
			Value = 4000,
			Width = 90
		};
		loggingPanel.Controls.Add(_chkLocalApiRequestResponseLogging);
		loggingPanel.Controls.Add(_chkLocalApiIncludeBodies);
		loggingPanel.Controls.Add(_chkLocalApiIncludeErrorDiagnostics);
		loggingPanel.Controls.Add(new Label
		{
			AutoSize = true,
			Text = "Max body chars:",
			Margin = new Padding(3, 6, 3, 3)
		});
		loggingPanel.Controls.Add(_numLocalApiMaxBodyChars);
		AddLocalApiRow(10, "Diagnostics:", loggingPanel);

		_localApiTab.Controls.Add(_localApiPanel);
		_tabControl.TabPages.Insert(1, _localApiTab);
	}

	private void AddLocalApiRow(int rowIndex, string labelText, Control control)
	{
		if (_localApiPanel is null)
			return;

		_localApiPanel.Controls.Add(new Label
		{
			Text = labelText,
			TextAlign = ContentAlignment.MiddleRight,
			Dock = DockStyle.Fill
		}, 0, rowIndex);
		_localApiPanel.Controls.Add(control, 1, rowIndex);
	}

	private static NumericUpDown CreatePortEditor(decimal defaultValue)
	{
		return new NumericUpDown
		{
			Dock = DockStyle.Fill,
			Minimum = 1,
			Maximum = 65535,
			Value = defaultValue
		};
	}

	private static TextBox CreateFillTextBox(string placeholderText)
	{
		return new TextBox
		{
			Dock = DockStyle.Fill,
			PlaceholderText = placeholderText
		};
	}

	private static ComboBox CreateEditableComboBox()
	{
		return new ComboBox
		{
			Dock = DockStyle.Fill,
			DropDownStyle = ComboBoxStyle.DropDown,
			MaxDropDownItems = 20,
			AutoCompleteMode = AutoCompleteMode.SuggestAppend,
			AutoCompleteSource = AutoCompleteSource.ListItems
		};
	}

	private LocalApiForwarderSettings? BuildLocalApiForwarderSettings()
	{
		if (_chkLocalApiForwarderEnabled is null
			|| _numOllamaPort is null
			|| _numFoundryPort is null
			|| _cmbLocalApiProviderProtocol is null
			|| _txtLocalApiProviderName is null
			|| _txtLocalApiProviderUrl is null
			|| _cmbLocalApiDefaultModel is null
			|| _cmbLocalApiDefaultEmbeddingModel is null
			|| _cmbLocalApiAuthType is null
			|| _txtLocalApiAuthHeaderName is null
			|| _txtLocalApiApiKey is null
			|| _txtLocalApiAdditionalHeaders is null
			|| _chkLocalApiRequestResponseLogging is null
			|| _chkLocalApiIncludeBodies is null
			|| _chkLocalApiIncludeErrorDiagnostics is null
			|| _numLocalApiMaxBodyChars is null
			|| _txtLocalApiModelMappings is null)
		{
			return null;
		}

		var providerName = _txtLocalApiProviderName.Text.Trim();
		var targetName = CredentialManager.GetLocalApiTargetName(providerName);
		var apiKey = _txtLocalApiApiKey.Text.Trim();
		if (string.IsNullOrWhiteSpace(apiKey))
			CredentialManager.DeleteToken(targetName);
		else
			CredentialManager.SaveToken(targetName, apiKey);

		return new LocalApiForwarderSettings
		{
			Enabled = _chkLocalApiForwarderEnabled.Checked,
			OllamaPort = (ushort)_numOllamaPort.Value,
			FoundryPort = (ushort)_numFoundryPort.Value,
			Provider = new LocalApiProviderSettings
			{
				Protocol = _cmbLocalApiProviderProtocol.SelectedItem?.ToString() ?? "OpenAICompatible",
				Name = providerName,
				BaseUrl = _txtLocalApiProviderUrl.Text.Trim(),
				DefaultModel = ResolveLocalApiStoredModelName(_cmbLocalApiDefaultModel.Text),
				DefaultEmbeddingModel = ResolveLocalApiStoredModelName(_cmbLocalApiDefaultEmbeddingModel.Text),
				AuthType = _cmbLocalApiAuthType.SelectedItem?.ToString() ?? "Bearer",
				AuthHeaderName = _txtLocalApiAuthHeaderName.Text.Trim(),
				AdditionalHeaders = ParseLocalApiHeaders(_txtLocalApiAdditionalHeaders.Lines)
			},
			ModelMappings = ParseLocalApiModelMappings(_txtLocalApiModelMappings.Lines),
			RequestResponseLogging = new LocalApiRequestResponseLoggingSettings
			{
				Enabled = _chkLocalApiRequestResponseLogging.Checked,
				IncludeBodies = _chkLocalApiIncludeBodies.Checked,
				MaxBodyCharacters = (int)_numLocalApiMaxBodyChars.Value
			},
			IncludeErrorDiagnostics = _chkLocalApiIncludeErrorDiagnostics.Checked
		};
	}

	private void LoadLocalApiForwarderToUi(LocalApiForwarderSettings? settings)
	{
		if (_chkLocalApiForwarderEnabled is null
			|| _numOllamaPort is null
			|| _numFoundryPort is null
			|| _cmbLocalApiProviderProtocol is null
			|| _txtLocalApiProviderName is null
			|| _txtLocalApiProviderUrl is null
			|| _cmbLocalApiDefaultModel is null
			|| _cmbLocalApiDefaultEmbeddingModel is null
			|| _cmbLocalApiAuthType is null
			|| _txtLocalApiAuthHeaderName is null
			|| _txtLocalApiApiKey is null
			|| _txtLocalApiAdditionalHeaders is null
			|| _chkLocalApiRequestResponseLogging is null
			|| _chkLocalApiIncludeBodies is null
			|| _chkLocalApiIncludeErrorDiagnostics is null
			|| _numLocalApiMaxBodyChars is null
			|| _txtLocalApiModelMappings is null)
		{
			return;
		}

		settings ??= new LocalApiForwarderSettings();
		_chkLocalApiForwarderEnabled.Checked = settings.Enabled;
		ReplaceLocalApiModelNameMap(BuildConfiguredLocalApiSelectorModels(settings));
		_numOllamaPort.Value = settings.OllamaPort == 0 ? 11434 : settings.OllamaPort;
		_numFoundryPort.Value = settings.FoundryPort == 0 ? 5273 : settings.FoundryPort;
		_cmbLocalApiProviderProtocol.SelectedItem = _cmbLocalApiProviderProtocol.Items.Contains(settings.Provider?.Protocol ?? "OpenAICompatible")
			? settings.Provider?.Protocol ?? "OpenAICompatible"
			: "OpenAICompatible";
		_txtLocalApiProviderName.Text = settings.Provider?.Name ?? string.Empty;
		_txtLocalApiProviderUrl.Text = settings.Provider?.BaseUrl ?? string.Empty;
		UpdateLocalApiModelSelectors(
			BuildLocalApiSelectorItems(settings),
			NormalizeLocalApiModelDisplayName(settings.Provider?.DefaultModel ?? string.Empty),
			NormalizeLocalApiModelDisplayName(settings.Provider?.DefaultEmbeddingModel ?? string.Empty));
		_cmbLocalApiAuthType.SelectedItem = _cmbLocalApiAuthType.Items.Contains(settings.Provider?.AuthType ?? "Bearer")
			? settings.Provider?.AuthType ?? "Bearer"
			: "Bearer";
		_txtLocalApiAuthHeaderName.Text = settings.Provider?.AuthHeaderName ?? "Authorization";
		_txtLocalApiApiKey.Text = CredentialManager.LoadToken(
			CredentialManager.GetLocalApiTargetName(settings.Provider?.Name ?? string.Empty)) ?? string.Empty;
		_txtLocalApiAdditionalHeaders.Lines = settings.Provider?.AdditionalHeaders?.Count > 0
			? settings.Provider.AdditionalHeaders
				.Where(static header => !string.IsNullOrWhiteSpace(header.Name))
				.Select(static header => $"{header.Name.Trim()}={header.Value}")
				.ToArray()
			: [];
		_chkLocalApiRequestResponseLogging.Checked = settings.RequestResponseLogging?.Enabled ?? false;
		_chkLocalApiIncludeBodies.Checked = settings.RequestResponseLogging?.IncludeBodies ?? false;
		_chkLocalApiIncludeErrorDiagnostics.Checked = settings.IncludeErrorDiagnostics;
		var maxBodyChars = settings.RequestResponseLogging?.MaxBodyCharacters ?? 4000;
		_numLocalApiMaxBodyChars.Value = Math.Clamp(maxBodyChars, (int)_numLocalApiMaxBodyChars.Minimum, (int)_numLocalApiMaxBodyChars.Maximum);
		_txtLocalApiModelMappings.Lines = settings.ModelMappings.Count == 0
			? []
			: settings.ModelMappings
				.Where(static mapping => !string.IsNullOrWhiteSpace(mapping.LocalModel)
					&& !string.IsNullOrWhiteSpace(mapping.UpstreamModel))
				.Select(static mapping => $"{mapping.LocalModel.Trim()}={mapping.UpstreamModel.Trim()}")
				.ToArray();
	}

	private IEnumerable<LocalApiAdvertisedModel> BuildConfiguredLocalApiSelectorModels(LocalApiForwarderSettings settings)
	{
		var providerBaseUrl = settings.Provider?.BaseUrl;

		if (!string.IsNullOrWhiteSpace(settings.Provider?.DefaultModel))
		{
			var upstreamModel = settings.Provider.DefaultModel.Trim();
			yield return new LocalApiAdvertisedModel(FormatLocalApiDisplayName(upstreamModel, providerBaseUrl), upstreamModel);
		}

		if (!string.IsNullOrWhiteSpace(settings.Provider?.DefaultEmbeddingModel))
		{
			var upstreamModel = settings.Provider.DefaultEmbeddingModel.Trim();
			yield return new LocalApiAdvertisedModel(FormatLocalApiDisplayName(upstreamModel, providerBaseUrl), upstreamModel);
		}

		foreach (var mapping in settings.ModelMappings.Where(static mapping => !string.IsNullOrWhiteSpace(mapping.UpstreamModel)))
		{
			var upstreamModel = mapping.UpstreamModel.Trim();
			yield return new LocalApiAdvertisedModel(FormatLocalApiDisplayName(upstreamModel, providerBaseUrl), upstreamModel);
		}
	}

	private void ReplaceLocalApiModelNameMap(IEnumerable<LocalApiAdvertisedModel> models)
	{
		_localApiModelNameMap = models
			.Where(static model => !string.IsNullOrWhiteSpace(model.LocalName) && !string.IsNullOrWhiteSpace(model.UpstreamModel))
			.GroupBy(static model => model.LocalName.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				static group => group.Key,
				static group => group.First().UpstreamModel.Trim(),
				StringComparer.OrdinalIgnoreCase);
	}

	private string NormalizeLocalApiModelDisplayName(string? modelName)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return string.Empty;

		var trimmed = modelName.Trim();
		if (_localApiModelNameMap.ContainsKey(trimmed))
			return trimmed;

		var upstreamMatch = _localApiModelNameMap.FirstOrDefault(entry =>
			entry.Value.Equals(trimmed, StringComparison.OrdinalIgnoreCase)
			|| entry.Value.Equals(RemoveLocalApiModelSuffix(trimmed), StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(upstreamMatch.Key))
			return upstreamMatch.Key;

		return FormatLocalApiDisplayName(trimmed, _txtLocalApiProviderUrl?.Text ?? _currentConfig.LocalApiForwarder?.Provider?.BaseUrl);
	}

	private string ResolveLocalApiStoredModelName(string? displayOrModelName)
	{
		if (string.IsNullOrWhiteSpace(displayOrModelName))
			return string.Empty;

		var trimmed = displayOrModelName.Trim();
		if (_localApiModelNameMap.TryGetValue(trimmed, out var upstreamModel))
			return upstreamModel;

		foreach (var suffix in GetLocalApiProviderSuffixCandidates(_txtLocalApiProviderUrl?.Text ?? _currentConfig.LocalApiForwarder?.Provider?.BaseUrl))
		{
			if (!trimmed.EndsWith($"@{suffix}", StringComparison.OrdinalIgnoreCase))
				continue;

			return trimmed[..^(suffix.Length + 1)].Trim();
		}

		return trimmed;
	}

	private static string FormatLocalApiDisplayName(string? modelName, string? providerBaseUrl)
	{
		if (string.IsNullOrWhiteSpace(modelName))
			return string.Empty;

		var trimmed = modelName.Trim();
		var providerSuffix = TryGetLocalApiProviderDisplaySuffix(providerBaseUrl);
		if (string.IsNullOrWhiteSpace(providerSuffix))
			return trimmed;

		var baseModelName = RemoveLocalApiModelSuffix(trimmed);
		return $"{baseModelName}@{providerSuffix}";
	}

	private static string RemoveLocalApiModelSuffix(string modelName)
	{
		var trimmed = modelName.Trim();
		var atIndex = trimmed.LastIndexOf('@');
		return atIndex > 0 ? trimmed[..atIndex].Trim() : trimmed;
	}

	private static string TryGetLocalApiProviderDomain(string? baseUrl)
	{
		if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
			return string.Empty;

		return uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
	}

	private static IEnumerable<string> GetLocalApiProviderSuffixCandidates(string? baseUrl)
	{
		var domain = TryGetLocalApiProviderDomain(baseUrl);
		if (string.IsNullOrWhiteSpace(domain))
			yield break;

		yield return domain;

		var compact = domain.Replace(".", string.Empty, StringComparison.Ordinal);
		if (!compact.Equals(domain, StringComparison.OrdinalIgnoreCase))
			yield return compact;
	}

	private static string TryGetLocalApiProviderDisplaySuffix(string? baseUrl)
	{
		var domain = TryGetLocalApiProviderDomain(baseUrl);
		if (string.IsNullOrWhiteSpace(domain))
			return string.Empty;

		return domain.Replace(".", string.Empty, StringComparison.Ordinal);
	}

	private static List<LocalApiModelMapping> ParseLocalApiModelMappings(IEnumerable<string> lines)
	{
		var mappings = new List<LocalApiModelMapping>();

		foreach (var rawLine in lines)
		{
			var line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith('#'))
				continue;

			var separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
				continue;

			var localModel = line[..separatorIndex].Trim();
			var upstreamModel = line[(separatorIndex + 1)..].Trim();
			if (localModel.Length == 0 || upstreamModel.Length == 0)
				continue;

			mappings.Add(new LocalApiModelMapping
			{
				LocalModel = localModel,
				UpstreamModel = upstreamModel
			});
		}

		return mappings
			.GroupBy(static mapping => mapping.LocalModel, StringComparer.OrdinalIgnoreCase)
			.Select(static group => group.Last())
			.ToList();
	}

	private static List<LocalApiHeaderSetting> ParseLocalApiHeaders(IEnumerable<string> lines)
	{
		return lines
			.Select(static rawLine => rawLine.Trim())
			.Where(static line => line.Length > 0 && !line.StartsWith('#'))
			.Select(static line =>
			{
				var separatorIndex = line.IndexOf('=');
				return separatorIndex <= 0
					? null
					: new LocalApiHeaderSetting
					{
						Name = line[..separatorIndex].Trim(),
						Value = line[(separatorIndex + 1)..].Trim()
					};
			})
			.Where(static header => header is not null && !string.IsNullOrWhiteSpace(header.Name))
			.Select(static header => header!)
			.ToList();
	}
}
