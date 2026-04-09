using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

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
	private readonly List<AppLogEntry> _logEntries = [];
	private readonly object _logEntriesLock = new();
	private readonly object _logSettingsLock = new();
	private readonly AppLogFileWriter _logFileWriter;
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
	private bool _isLoadingGatewayProviderFields;
	private bool _isRebuildingGatewayProviderTabs;
	private bool _isLoadingSyncCredentials;
	private bool _isLoadingLogSettings;

	private FlowLayoutPanel? _logFilterPanel;
	private CheckBox? _chkLogDebug;
	private CheckBox? _chkLogInformation;
	private CheckBox? _chkLogWarning;
	private CheckBox? _chkLogError;
	private CheckBox? _chkLogWriteToDirectory;
	private Label? _lblLogDirectory;
	private Button? _btnOpenLogDirectory;
	private AppLoggingSettings _activeLogSettings = new();

	private const int MaxLogHistoryEntries = 20000;
	private const int MaxRenderedLogEntries = 10000;

    private readonly Dictionary<string, GatewayProviderSettingsControl> _gatewayProviderControls = new(StringComparer.OrdinalIgnoreCase);

	public MainForm(bool startMinimized = false)
	{
		_startMinimized = startMinimized;
		_configManager = new ProxyConfigManager();
		_activeConfigPath = _configManager.GetConfigPath();
        _currentConfig = new ProxyConfigModel();
		_logFileWriter = new AppLogFileWriter(Path.Combine(_configManager.GetConfigDirectory(), "logs"));
		_autoUpdater = new AutoUpdater();
		InitializeComponent();

		if (IsInDesignMode())
			return;

		_currentConfig = _configManager.Load(_activeConfigPath);
		_logBuffer = new LogBuffer(BatchAppendLogs);
		InitializeLogUi();
     InitializeGatewayProviderControls();
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

	private static bool IsInDesignMode()
	{
		return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
	}

	private void InitializeLogUi()
	{
		if (_logPanel is null || _rtbLogs is null || _btnClearPanel is null)
			return;

		_logFilterPanel = new FlowLayoutPanel
		{
			AutoSize = true,
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = new Padding(5, 5, 5, 0),
			Name = "_logFilterPanel",
			WrapContents = true
		};

		var levelsLabel = new Label
		{
			AutoSize = true,
			Margin = new Padding(3, 7, 6, 3),
			Text = "Levels:"
		};
		_logFilterPanel.Controls.Add(levelsLabel);

		_chkLogDebug = CreateLogLevelCheckBox("_chkLogDebug", "Debug");
		_chkLogInformation = CreateLogLevelCheckBox("_chkLogInformation", "Info");
		_chkLogWarning = CreateLogLevelCheckBox("_chkLogWarning", "Warning");
		_chkLogError = CreateLogLevelCheckBox("_chkLogError", "Error");
		_chkLogWriteToDirectory = CreateLogLevelCheckBox("_chkLogWriteToDirectory", "Write To Directory");
		_chkLogWriteToDirectory.Margin = new Padding(20, 4, 12, 3);

		_lblLogDirectory = new Label
		{
			AutoSize = true,
			Margin = new Padding(3, 7, 8, 3),
			Name = "_lblLogDirectory",
			Text = string.Empty
		};

		_btnOpenLogDirectory = new Button
		{
			AutoSize = true,
			Margin = new Padding(0, 2, 3, 2),
			Name = "_btnOpenLogDirectory",
			Text = "Open Folder",
			UseVisualStyleBackColor = true
		};
		_btnOpenLogDirectory.Click += BtnOpenLogDirectory_Click;

		_logFilterPanel.Controls.Add(_chkLogDebug);
		_logFilterPanel.Controls.Add(_chkLogInformation);
		_logFilterPanel.Controls.Add(_chkLogWarning);
		_logFilterPanel.Controls.Add(_chkLogError);
		_logFilterPanel.Controls.Add(_chkLogWriteToDirectory);
		_logFilterPanel.Controls.Add(_lblLogDirectory);
		_logFilterPanel.Controls.Add(_btnOpenLogDirectory);

		_rtbLogs.HideSelection = false;
		_rtbLogs.HandleCreated += (_, _) => RebuildLogView();

		_logPanel.SuspendLayout();
		_logPanel.Controls.Remove(_rtbLogs);
		_logPanel.Controls.Remove(_btnClearPanel);
		_logPanel.RowCount = 3;
		_logPanel.RowStyles.Clear();
		_logPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
		_logPanel.Controls.Add(_logFilterPanel, 0, 0);
		_logPanel.Controls.Add(_rtbLogs, 0, 1);
		_logPanel.Controls.Add(_btnClearPanel, 0, 2);
		_logPanel.ResumeLayout();
	}

	private CheckBox CreateLogLevelCheckBox(string name, string text)
	{
		var checkBox = new CheckBox
		{
			AutoSize = true,
			Margin = new Padding(3, 4, 12, 3),
			Name = name,
			Text = text,
			UseVisualStyleBackColor = true
		};
		checkBox.CheckedChanged += LogFilterControl_Changed;
		return checkBox;
	}

	private void LogFilterControl_Changed(object? sender, EventArgs e)
	{
		if (_isLoadingLogSettings)
			return;

		var settings = BuildAppLoggingSettings();
		ApplyActiveLogSettings(settings);
		UpdateLogDirectoryHint();
		_currentConfig.Logging = settings;
		RebuildLogView();
	}

	private void BtnOpenLogDirectory_Click(object? sender, EventArgs e)
	{
		try
		{
			Directory.CreateDirectory(_logFileWriter.LogDirectory);
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = _logFileWriter.LogDirectory,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show(
				$"Failed to open log directory: {ex.Message}",
				"Logs",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
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
			_engine = new ProxyEngine(opts, Path.Combine(_logFileWriter.LogDirectory, "requests"));
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

	private async void BtnSaveConfig_Click(object? sender, EventArgs e)
	{
		await SaveConfigAsync(_activeConfigPath, "Save Config");
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

	private async void BtnLoadConfig_Click(object? sender, EventArgs e)
	{
		using OpenFileDialog dialog = new();
		dialog.InitialDirectory = GetConfigDialogDirectory();
		dialog.FileName = Path.GetFileName(_activeConfigPath);
		dialog.DefaultExt = "json";
		dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		await LoadConfigFromPathAsync(dialog.FileName, "Load Config");
	}

	private async void BtnSaveConfigAs_Click(object? sender, EventArgs e)
	{
		using SaveFileDialog dialog = new();
		dialog.InitialDirectory = GetConfigDialogDirectory();
		dialog.FileName = Path.GetFileName(_activeConfigPath);
		dialog.DefaultExt = "json";
		dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;

		await SaveConfigAsync(dialog.FileName, "Save Config");
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
			var json = System.Text.Json.JsonSerializer.Serialize(model, TrafficPilotConfigJsonContext.Default.ProxyConfigModel);

			using var syncProvider = ConfigSyncProviderFactory.Create(provider, token);
			string newId = await syncProvider.PushAsync(json, gistId).ConfigureAwait(true);

			// Persist the returned gist ID back to the config
			_txtGistId.Text = newId;
			SaveConfigSyncCredentials(provider, token, newId);
			_currentConfig.ConfigSync = new ConfigSyncSettings { Provider = provider };
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

			var model = System.Text.Json.JsonSerializer.Deserialize(json, TrafficPilotJsonContext.Default.ProxyConfigModel);
			if (model is null)
				throw new InvalidOperationException("Remote config could not be deserialized.");

			// Preserve sync settings from the current session
			SaveConfigSyncCredentials(provider, token, gistId);
			model.ConfigSync = new ConfigSyncSettings { Provider = provider };
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
		var model = CloneConfigModel(_currentConfig) ?? new ProxyConfigModel();
		var gatewaySettings = BuildOllamaGatewaySettings();
		gatewaySettings ??= model.OllamaGateway ?? new OllamaGatewaySettings();
      ApplyAllGatewayProviderChanges(gatewaySettings);

		model.ConfigName = _txtConfigName!.Text.Trim();
		model.Proxy ??= new ProxySettings();
		model.Proxy.Enabled = _chkProxyEnabled!.Checked;
		model.Proxy.Host = _cmbProxyHost!.Text;
		model.Proxy.Port = (ushort)_numProxyPort!.Value;
		model.Proxy.Scheme = _cmbProxyScheme!.SelectedItem?.ToString() ?? "socks5";
		model.Proxy.IsLocalProxy = _chkLocalProxy!.Checked;

		model.Targeting ??= new TargetingSettings();
		model.Targeting.ProcessNames = GetProcessNamesFromUi();
		model.Targeting.DomainRules = GetDomainRulesFromUi();

		model.HostsRedirect ??= new HostsRedirectSettings();
		model.HostsRedirect.Enabled = _chkDNSRedirectEnabled!.Checked;
		model.HostsRedirect.Mode = _rdoHostsFile?.Checked == true ? "HostsFile" : "DnsInterception";
		model.HostsRedirect.HostsUrl = _txtHostsUrl!.Text.Trim();
		model.HostsRedirect.RefreshDomains = GetRefreshDomainsFromUi();

		model.StartOnBoot = _chkStartOnBoot!.Checked;
		model.AutoStartProxy = _chkAutoStartProxy!.Checked;
		model.ConfigSync = BuildConfigSyncSettings();
		model.OllamaGateway = gatewaySettings;
		model.Logging = BuildAppLoggingSettings();

		return model;
	}

	private ConfigSyncSettings? BuildConfigSyncSettings()
	{
		string provider = _cmbSyncProvider!.SelectedItem?.ToString() ?? "GitHub";
		string token = _txtSyncToken!.Text.Trim();
		string? gistId = _txtGistId!.Text.Trim();
		if (string.IsNullOrEmpty(gistId)) gistId = null;

		SaveConfigSyncCredentials(provider, token, gistId);

		if (string.IsNullOrEmpty(token) && gistId is null)
			return null;
		return new ConfigSyncSettings { Provider = provider };
	}

	private ProxyOptions BuildProxyOptions()
	{
		bool hostsEnabled = _chkDNSRedirectEnabled?.Checked ?? 
			(_rdoDnsInterception?.Checked == true || _rdoHostsFile?.Checked == true);
		string hostsMode = _rdoHostsFile?.Checked == true ? "HostsFile" : "DnsInterception";
		var gatewaySettings = BuildOllamaGatewaySettings();
      ApplyAllGatewayProviderChanges(gatewaySettings);

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
			gatewaySettings
		);
	}

	private void LoadConfigToUI()
	{
		_currentConfig.OllamaGateway ??= new OllamaGatewaySettings();
		GatewayProviderModelHelpers.Normalize(_currentConfig.OllamaGateway);

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
		LoadGatewayOverviewToUi(_currentConfig.OllamaGateway);
        LoadGatewayProviderTabs(_currentConfig.OllamaGateway);

		// Load sync settings
		string syncProvider = _currentConfig.ConfigSync?.Provider ?? "GitHub";
		_cmbSyncProvider!.SelectedItem = _cmbSyncProvider.Items.Contains(syncProvider) ? syncProvider : "GitHub";
		_cmbSyncProvider.SelectedIndexChanged -= CmbSyncProvider_SelectedIndexChanged;
		_cmbSyncProvider.SelectedIndexChanged += CmbSyncProvider_SelectedIndexChanged;
		LoadConfigSyncCredentials(syncProvider);
		LoadLogSettingsToUi(_currentConfig.Logging);

		// 加载hosts redirect模式
		string mode = _currentConfig.HostsRedirect?.Mode ?? "DnsInterception";
		if (_rdoHostsFile is not null && _rdoDnsInterception is not null)
		{
			if (mode.Equals("HostsFile", StringComparison.OrdinalIgnoreCase))
				_rdoHostsFile.Checked = true;
			else
				_rdoDnsInterception.Checked = true;
		}

		ApplySimpleGatewayUi();
	}

	private void CmbSyncProvider_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_isLoadingSyncCredentials)
			return;

		var provider = _cmbSyncProvider?.SelectedItem?.ToString() ?? "GitHub";
		LoadConfigSyncCredentials(provider);
	}

	private void LoadConfigSyncCredentials(string provider)
	{
		if (_txtSyncToken is null || _txtGistId is null)
			return;

		_isLoadingSyncCredentials = true;
		try
		{
			_txtSyncToken.Text = CredentialManager.LoadConfigSyncToken(provider) ?? string.Empty;
			_txtGistId.Text = CredentialManager.LoadConfigSyncRemoteId(provider) ?? string.Empty;
		}
		finally
		{
			_isLoadingSyncCredentials = false;
		}
	}

	private static void SaveConfigSyncCredentials(string provider, string? token, string? remoteId)
	{
		var normalizedProvider = string.IsNullOrWhiteSpace(provider) ? "GitHub" : provider.Trim();

		if (string.IsNullOrWhiteSpace(token))
			CredentialManager.DeleteConfigSyncToken(normalizedProvider);
		else
			CredentialManager.SaveConfigSyncToken(normalizedProvider, token.Trim());

		if (string.IsNullOrWhiteSpace(remoteId))
			CredentialManager.DeleteConfigSyncRemoteId(normalizedProvider);
		else
			CredentialManager.SaveConfigSyncRemoteId(normalizedProvider, remoteId.Trim());
	}

	private AppLoggingSettings BuildAppLoggingSettings()
	{
		return new AppLoggingSettings
		{
			EnableDebug = _chkLogDebug?.Checked ?? false,
			EnableInformation = _chkLogInformation?.Checked ?? true,
			EnableWarning = _chkLogWarning?.Checked ?? true,
			EnableError = _chkLogError?.Checked ?? true,
			WriteToDirectory = _chkLogWriteToDirectory?.Checked ?? false
		};
	}

	private static AppLoggingSettings CloneAppLoggingSettings(AppLoggingSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		return new AppLoggingSettings
		{
			EnableDebug = settings.EnableDebug,
			EnableInformation = settings.EnableInformation,
			EnableWarning = settings.EnableWarning,
			EnableError = settings.EnableError,
			WriteToDirectory = settings.WriteToDirectory
		};
	}

	private void ApplyActiveLogSettings(AppLoggingSettings settings)
	{
		lock (_logSettingsLock)
			_activeLogSettings = CloneAppLoggingSettings(settings);
	}

	private AppLoggingSettings GetActiveLogSettings()
	{
		lock (_logSettingsLock)
			return CloneAppLoggingSettings(_activeLogSettings);
	}

	private void LoadLogSettingsToUi(AppLoggingSettings? settings)
	{
		if (_chkLogDebug is null
			|| _chkLogInformation is null
			|| _chkLogWarning is null
			|| _chkLogError is null
			|| _chkLogWriteToDirectory is null)
		{
			return;
		}

		settings ??= new AppLoggingSettings();

		_isLoadingLogSettings = true;
		try
		{
			_chkLogDebug.Checked = settings.EnableDebug;
			_chkLogInformation.Checked = settings.EnableInformation;
			_chkLogWarning.Checked = settings.EnableWarning;
			_chkLogError.Checked = settings.EnableError;
			_chkLogWriteToDirectory.Checked = settings.WriteToDirectory;
			ApplyActiveLogSettings(settings);
			UpdateLogDirectoryHint();
		}
		finally
		{
			_isLoadingLogSettings = false;
		}

		RebuildLogView();
	}

	private void UpdateLogDirectoryHint()
	{
		if (_lblLogDirectory is null || _chkLogWriteToDirectory is null)
			return;

		var modeText = _chkLogWriteToDirectory.Checked ? "Writing" : "Directory";
		_lblLogDirectory.Text = $"{modeText}: {_logFileWriter.LogDirectory}";
	}

	private bool IsLogLevelEnabled(AppLogLevel level)
	{
		return GetActiveLogSettings().IsEnabled(level);
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

	private async Task LoadConfigFromPathAsync(string configPath, string dialogTitle)
	{
		if (string.IsNullOrWhiteSpace(configPath))
			throw new ArgumentException("Config path cannot be empty.", nameof(configPath));

		_activeConfigPath = Path.GetFullPath(configPath);
		_currentConfig = _configManager.Load(_activeConfigPath);
		LoadConfigToUI();
		RefreshConfigShortcutButtons();
		await ApplyGatewayConfigToRunningEngineAsync(_currentConfig, $"{dialogTitle} applied");

		MessageBox.Show(
			$"Configuration loaded successfully.\n{_activeConfigPath}",
			dialogTitle,
			MessageBoxButtons.OK,
			MessageBoxIcon.Information);
	}

	private async Task SaveConfigAsync(string configPath, string dialogTitle)
	{
		if (string.IsNullOrWhiteSpace(configPath))
			throw new ArgumentException("Config path cannot be empty.", nameof(configPath));

		PersistCurrentConfig(configPath);
		await ApplyGatewayConfigToRunningEngineAsync(_currentConfig, $"{dialogTitle} applied");
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

	private async Task ApplyGatewayConfigToRunningEngineAsync(
		ProxyConfigModel config,
		string actionLabel,
		bool showFailureDialog = true)
	{
		if (_engine?.IsRunning != true)
			return;

		try
		{
			await _engine.ApplyGatewaySettingsAsync(config.OllamaGateway);
			AppendLog(AppLogLevel.Information, $"[Config] {actionLabel}: running Ollama Gateway updated.");
		}
		catch (Exception ex)
		{
			AppendLog(AppLogLevel.Error, $"[Config] Failed to update running Ollama Gateway: {ex}");
			if (showFailureDialog)
			{
				MessageBox.Show(
					$"Configuration changed, but the running Ollama Gateway could not be updated: {ex.Message}",
					"Ollama Gateway",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}
	}

	private void LogRuntimeConfigSnapshot(ProxyConfigModel config)
	{
		AppendLog(AppLogLevel.Information, $"[Config] Active config: {_activeConfigPath}");

		var localApi = config.OllamaGateway;
		if (localApi?.Enabled != true)
			return;

		var provider = GatewayProviderModelHelpers.GetDefault(localApi);
		var defaultModel = string.IsNullOrWhiteSpace(provider.DefaultModel)
			? "<empty>"
			: provider.DefaultModel;
		var embeddingModel = string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModel)
			? "<empty>"
			: provider.DefaultEmbeddingModel;
		AppendLog(
			AppLogLevel.Information,
			$"[Config] Local API provider='{provider.Name}', protocol={provider.Protocol}, baseUrl={provider.BaseUrl}, defaultModel={defaultModel}, embeddingModel={embeddingModel}");
	}

	private OllamaGatewaySettings? BuildOllamaGatewaySettings()
	{
		var cloned = _currentConfig.OllamaGateway is not null
			? CloneGatewaySettings(_currentConfig.OllamaGateway)
			: new OllamaGatewaySettings();
		if (cloned is null)
			return null;

		ApplyGatewayOverviewSettings(cloned);
		return cloned;
	}

	private void ApplyGatewayOverviewSettings(OllamaGatewaySettings gatewaySettings)
	{
		ArgumentNullException.ThrowIfNull(gatewaySettings);

		if (_chkLocalApiForwarderEnabled is not null)
			gatewaySettings.Enabled = _chkLocalApiForwarderEnabled.Checked;
		if (_numOllamaPort is not null)
			gatewaySettings.OllamaPort = (ushort)_numOllamaPort.Value;

		gatewaySettings.RequestResponseLogging ??= new LocalApiRequestResponseLoggingSettings();
		if (_chkLocalApiRequestResponseLogging is not null)
			gatewaySettings.RequestResponseLogging.Enabled = _chkLocalApiRequestResponseLogging.Checked;
		if (_chkLocalApiIncludeBodies is not null)
			gatewaySettings.RequestResponseLogging.IncludeBodies = _chkLocalApiIncludeBodies.Checked;
		if (_numLocalApiMaxBodyChars is not null)
			gatewaySettings.RequestResponseLogging.MaxBodyCharacters = (int)_numLocalApiMaxBodyChars.Value;
		if (_chkLocalApiIncludeErrorDiagnostics is not null)
			gatewaySettings.IncludeErrorDiagnostics = _chkLocalApiIncludeErrorDiagnostics.Checked;
	}

	private void LoadGatewayOverviewToUi(OllamaGatewaySettings? gatewaySettings)
	{
		gatewaySettings ??= new OllamaGatewaySettings();
		GatewayProviderModelHelpers.Normalize(gatewaySettings);

		if (_chkLocalApiForwarderEnabled is not null)
			_chkLocalApiForwarderEnabled.Checked = gatewaySettings.Enabled;
		if (_numOllamaPort is not null)
			_numOllamaPort.Value = gatewaySettings.OllamaPort == 0 ? 11434 : gatewaySettings.OllamaPort;
		if (_chkLocalApiRequestResponseLogging is not null)
			_chkLocalApiRequestResponseLogging.Checked = gatewaySettings.RequestResponseLogging?.Enabled ?? false;
		if (_chkLocalApiIncludeBodies is not null)
			_chkLocalApiIncludeBodies.Checked = gatewaySettings.RequestResponseLogging?.IncludeBodies ?? false;
		if (_chkLocalApiIncludeErrorDiagnostics is not null)
			_chkLocalApiIncludeErrorDiagnostics.Checked = gatewaySettings.IncludeErrorDiagnostics;
		if (_numLocalApiMaxBodyChars is not null)
		{
			var maxBodyChars = gatewaySettings.RequestResponseLogging?.MaxBodyCharacters ?? 4000;
			_numLocalApiMaxBodyChars.Value = Math.Clamp(
				maxBodyChars,
				(int)_numLocalApiMaxBodyChars.Minimum,
				(int)_numLocalApiMaxBodyChars.Maximum);
		}
	}

	private static OllamaGatewaySettings? CloneGatewaySettings(OllamaGatewaySettings? settings)
	{
		return DeepClone(settings);
	}

	private static ProxyConfigModel? CloneConfigModel(ProxyConfigModel? model)
	{
		return DeepClone(model);
	}

	private static T? DeepClone<T>(T? value)
	{
		if (value is null)
			return default;

		if (typeof(T) == typeof(ProxyConfigModel))
		{
			var json = System.Text.Json.JsonSerializer.Serialize((ProxyConfigModel)(object)value, TrafficPilotJsonContext.Default.ProxyConfigModel);
			return (T?)(object?)System.Text.Json.JsonSerializer.Deserialize(json, TrafficPilotJsonContext.Default.ProxyConfigModel);
		}

		if (typeof(T) == typeof(OllamaGatewaySettings))
		{
			var json = System.Text.Json.JsonSerializer.Serialize((OllamaGatewaySettings)(object)value, TrafficPilotJsonContext.Default.OllamaGatewaySettings);
			return (T?)(object?)System.Text.Json.JsonSerializer.Deserialize(json, TrafficPilotJsonContext.Default.OllamaGatewaySettings);
		}

		throw new NotSupportedException($"AOT clone is not configured for type '{typeof(T).FullName}'.");
	}

	[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = false)]
	[JsonSerializable(typeof(ProxyConfigModel))]
	[JsonSerializable(typeof(OllamaGatewaySettings))]
	private sealed partial class TrafficPilotJsonContext : JsonSerializerContext
	{
	}

   private void ApplyAllGatewayProviderChanges(OllamaGatewaySettings? gatewaySettings)
	{
		if (gatewaySettings is null)
			return;

       foreach (var pair in _gatewayProviderControls)
		{
			var previousProvider = FindGatewayProvider(gatewaySettings, pair.Key);
			var previousProviderName = previousProvider?.Name;
			var previousBaseUrl = previousProvider?.BaseUrl;
           ApplyGatewayProviderChanges(gatewaySettings, pair.Key, pair.Value);
			var provider = FindGatewayProvider(gatewaySettings, pair.Key);
			if (provider is not null)
				PersistGatewayProviderApiKey(previousProviderName, previousBaseUrl, provider.Name, provider.BaseUrl, pair.Value.ApiKeyValue);
		}
	}

 private void InitializeGatewayProviderControls()
	{
		if (_gatewayTabControl is null
			|| _gatewayOpenAiProviderControl is null
			|| _gatewayAnthropicProviderControl is null
			|| _gatewayGeminiProviderControl is null
			|| _gatewayXAiProviderControl is null)
		{
			return;
		}

        _gatewayProviderControls.Clear();
		_gatewayProviderControls["openai"] = _gatewayOpenAiProviderControl;
		_gatewayProviderControls["anthropic"] = _gatewayAnthropicProviderControl;
		_gatewayProviderControls["google"] = _gatewayGeminiProviderControl;
		_gatewayProviderControls["xai"] = _gatewayXAiProviderControl;

       foreach (var pair in _gatewayProviderControls)
		{
            var control = pair.Value;
			WireGatewayProviderControl(control);
			ConfigureGatewayProviderControl(pair.Key, control);
		}

		_gatewayTabControl.SelectedIndexChanged += GatewayProviderTabs_SelectedIndexChanged;
		ApplySimpleGatewayUi();
	}

	private void ApplySimpleGatewayUi()
	{
	 
	}

 private void ConfigureGatewayProviderControl(string providerId, GatewayProviderSettingsControl control)
	{
		var normalizedProviderId = GatewayProviderModelHelpers.NormalizeProviderId(providerId);
		control.Tag = normalizedProviderId;
		control.EnabledInput.Tag = normalizedProviderId;
		control.BaseUrlInput.Tag = normalizedProviderId;
		control.ApiKeyInput.Tag = normalizedProviderId;
     control.ApplyProviderPreset(
			GetSimpleGatewayProviderDisplayName(normalizedProviderId),
			GetSimpleGatewayProviderSuffixHint(normalizedProviderId),
			GetSimpleGatewayProviderDefaultBaseUrl(normalizedProviderId));
	}

	private static string GetSimpleGatewayProviderDisplayName(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"openai" => "OpenAI",
			"anthropic" => "Anthropic",
			"google" => "Gemini",
			"xai" => "xAI",
			_ => providerId
		};
	}

	private static string GetSimpleGatewayProviderDefaultBaseUrl(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"openai" => "https://api.openai.com/v1/",
			"anthropic" => "https://api.anthropic.com/v1/",
			"google" => "https://generativelanguage.googleapis.com/v1beta/openai/",
			"xai" => "https://api.x.ai/v1/",
			_ => string.Empty
		};
	}

	private static string GetSimpleGatewayProviderSuffixHint(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"openai" => "@openai / @oai",
			"anthropic" => "@anthropic / @claude",
			"google" => "@gemini / @google",
			"xai" => "@xai / @grok",
			_ => "@provider"
		};
	}

	private static string GetSimpleGatewayProviderProtocol(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"anthropic" => "Anthropic",
			_ => "OpenAICompatible"
		};
	}

	private static string GetSimpleGatewayProviderAuthType(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"anthropic" => "Header",
			_ => "Bearer"
		};
	}

	private static string GetSimpleGatewayProviderAuthHeader(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"anthropic" => "x-api-key",
			_ => "Authorization"
		};
	}

	private static string GetSimpleGatewayProviderChatEndpoint(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"anthropic" => "messages",
			_ => "chat/completions"
		};
	}

	private static string GetSimpleGatewayProviderEmbeddingsEndpoint(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"anthropic" => string.Empty,
			"xai" => string.Empty,
			_ => "embeddings"
		};
	}

	private static bool GetSimpleGatewayProviderSupportsEmbeddings(string providerId)
	{
		return !string.IsNullOrWhiteSpace(GetSimpleGatewayProviderEmbeddingsEndpoint(providerId));
	}

   private void WireGatewayProviderControl(GatewayProviderSettingsControl control)
	{
        control.BaseUrlInput.TextChanged += GatewayProviderBaseUrl_TextChanged;
		control.EnabledInput.CheckedChanged += ChkGatewayProviderEnabled_CheckedChanged;
	}

    private GatewayProviderSettingsControl? GetGatewayProviderControl(string? providerId)
	{
		if (string.IsNullOrWhiteSpace(providerId))
			return null;

		var normalizedProviderId = GatewayProviderModelHelpers.NormalizeProviderId(providerId);
        return _gatewayProviderControls.GetValueOrDefault(normalizedProviderId);
	}

	private static string? GetGatewayProviderIdFromControl(Control? control)
	{
		if (control?.Tag is not string providerId || string.IsNullOrWhiteSpace(providerId))
			return null;

		return GatewayProviderModelHelpers.NormalizeProviderId(providerId);
	}

	private string? GetGatewayProviderId(TabPage? tabPage)
	{
		return GetGatewayProviderIdFromControl(tabPage);
	}

	private TabPage? GetGatewayProviderTabPage(string? providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"openai" => _gatewayOpenAiProviderTab,
			"anthropic" => _gatewayAnthropicProviderTab,
			"google" => _gatewayGeminiProviderTab,
			"xai" => _gatewayXAiProviderTab,
			_ => null
		};
	}

	private static string GetGatewayProviderTabTitle(string providerId)
	{
		return GatewayProviderModelHelpers.NormalizeProviderId(providerId) switch
		{
			"openai" => "OpenAI",
			"anthropic" => "Anthropic",
			"google" => "Gemini",
			"xai" => "xAI",
			_ => providerId
		};
	}

	private static IEnumerable<IGatewayProviderModel> EnumerateGatewayProviders(OllamaGatewaySettings? gatewaySettings)
	{
		return gatewaySettings is null ? [] : GatewayProviderModelHelpers.Enumerate(gatewaySettings);
	}

	private static IGatewayProviderModel? FindGatewayProvider(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
		return GatewayProviderModelHelpers.Find(gatewaySettings, providerId);
	}

	private void GatewayProviderTabs_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_isRebuildingGatewayProviderTabs)
			return;

		ApplyAllGatewayProviderChanges(_currentConfig.OllamaGateway);

		var selectedProviderId = GetGatewayProviderId(_gatewayTabControl?.SelectedTab);
		if (selectedProviderId is null)
			return;

      LoadGatewayProviderControlValues(_currentConfig.OllamaGateway, selectedProviderId);
		LoadGatewayProviderApiKey(_currentConfig.OllamaGateway, selectedProviderId);
	}

    private void GatewayProviderBaseUrl_TextChanged(object? sender, EventArgs e)
	{
		if (_isLoadingGatewayProviderFields || sender is not Control inputControl)
			return;

		var providerId = GetGatewayProviderIdFromControl(inputControl);
		var control = GetGatewayProviderControl(providerId);
		if (string.IsNullOrWhiteSpace(providerId) || control is null)
			return;

		ApplyGatewayProviderChanges(_currentConfig.OllamaGateway, providerId, control);
	}

  private void LoadGatewayProviderTabs(OllamaGatewaySettings? gatewaySettings)
	{
		if (_gatewayTabControl is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var selectedProviderId = GetSelectedGatewayProviderId() ?? "openai";

		_isRebuildingGatewayProviderTabs = true;
		foreach (var provider in EnumerateGatewayProviders(gatewaySettings))
		{
			var providerId = string.IsNullOrWhiteSpace(provider.Id) ? "openai" : provider.Id.Trim();
			var tabPage = GetGatewayProviderTabPage(providerId);
			if (tabPage is not null)
				tabPage.Text = GetGatewayProviderTabTitle(providerId);

          LoadGatewayProviderControlValues(gatewaySettings, providerId);
			LoadGatewayProviderApiKey(gatewaySettings, providerId);
         if (GetGatewayProviderControl(providerId) is { } control)
				ConfigureGatewayProviderControl(providerId, control);
		}
		_isRebuildingGatewayProviderTabs = false;
		SelectGatewayProviderTab(selectedProviderId);

	}

	private string? GetSelectedGatewayProviderId()
	{
       return GetGatewayProviderId(_gatewayTabControl?.SelectedTab);
	}

	private void SelectGatewayProviderTab(string providerId)
	{
		var tabPage = GetGatewayProviderTabPage(providerId);
		if (_gatewayTabControl is null || tabPage is null)
			return;

		_gatewayTabControl.SelectedTab = tabPage;
	}
	 
	private void ChkGatewayProviderEnabled_CheckedChanged(object? sender, EventArgs e)
	{
		if (_isLoadingGatewayProviderFields
			|| sender is not CheckBox checkBox
			|| checkBox.Tag is not string providerId)
		{
			return;
		}

		var gatewaySettings = _currentConfig.OllamaGateway ?? BuildOllamaGatewaySettings();
		if (gatewaySettings is null)
			return;

     ApplySelectedGatewayProviderChanges(gatewaySettings);

		var provider = FindGatewayProvider(gatewaySettings, providerId);
		if (provider is null)
			return;

		provider.Enabled = checkBox.Checked;
		_currentConfig.OllamaGateway = gatewaySettings;
     LoadGatewayProviderTabs(gatewaySettings);
		SelectGatewayProviderTab(providerId);
	}

 
 

  private void LoadGatewayProviderControlValues(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
       var control = GetGatewayProviderControl(providerId);
		if (control is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var provider = FindGatewayProvider(gatewaySettings, providerId);

		_isLoadingGatewayProviderFields = true;
      try
		{
 			control.EnabledInput.Checked = provider?.Enabled == true;
         control.BaseUrlInput.Text = provider?.BaseUrl ?? string.Empty;
		}
		finally
		{
			_isLoadingGatewayProviderFields = false;
		}
	}


	private static void PersistGatewayProviderApiKey(string? previousProviderName, string? previousBaseUrl, string providerName, string? baseUrl, string? rawApiKey)
	{
		if (string.IsNullOrWhiteSpace(providerName))
			return;

		var previousTargetName = string.IsNullOrWhiteSpace(previousProviderName)
			? null
			: CredentialManager.GetLocalApiTargetName(previousProviderName.Trim(), previousBaseUrl);
		var targetName = CredentialManager.GetLocalApiTargetName(providerName.Trim(), baseUrl);
		var apiKey = rawApiKey?.Trim() ?? string.Empty;

		if (!string.IsNullOrWhiteSpace(previousTargetName)
			&& !previousTargetName.Equals(targetName, StringComparison.Ordinal))
		{
			CredentialManager.DeleteToken(previousTargetName);
		}

		CredentialManager.DeleteToken(targetName);
		if (string.IsNullOrWhiteSpace(apiKey))
			return;

		CredentialManager.SaveToken(targetName, apiKey);
	}

	private void LoadGatewayProviderApiKey(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
     var control = GetGatewayProviderControl(providerId);
		if (control is null)
			return;

		if (string.IsNullOrWhiteSpace(providerId))
		{
			control.SetApiKey(string.Empty);
			return;
		}

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var provider = FindGatewayProvider(gatewaySettings, providerId);
		if (provider is null)
		{
			control.SetApiKey(string.Empty);
			return;
		}

		control.SetApiKey(CredentialManager.LoadToken(CredentialManager.GetLocalApiTargetName(provider.Name, provider.BaseUrl)) ?? string.Empty);
	}

  private void ApplySelectedGatewayProviderChanges(OllamaGatewaySettings? gatewaySettings)
	{
		var selectedProviderId = GetSelectedGatewayProviderId();
		if (string.IsNullOrWhiteSpace(selectedProviderId))
			return;

        var selectedControl = GetGatewayProviderControl(selectedProviderId);
		if (selectedControl is null)
			return;

       ApplyGatewayProviderChanges(gatewaySettings, selectedProviderId, selectedControl);
	}

    private void ApplyGatewayProviderChanges(OllamaGatewaySettings? gatewaySettings, string providerId, GatewayProviderSettingsControl control)
	{
		if (gatewaySettings is null
			|| string.IsNullOrWhiteSpace(providerId))
		{
			return;
		}

		var normalizedProviderId = GatewayProviderModelHelpers.NormalizeProviderId(providerId);

		var provider = FindGatewayProvider(gatewaySettings, normalizedProviderId);
		if (provider is null)
			return;

		provider.Id = normalizedProviderId;
		provider.Enabled = control.EnabledInput.Checked;
		provider.Protocol = GetSimpleGatewayProviderProtocol(normalizedProviderId);
		provider.Name = GetSimpleGatewayProviderDisplayName(normalizedProviderId);
        provider.BaseUrl = string.IsNullOrWhiteSpace(control.BaseUrlInput.Text)
			? GetSimpleGatewayProviderDefaultBaseUrl(normalizedProviderId)
            : control.BaseUrlInput.Text.Trim();
		provider.DefaultModel = string.Empty;
		provider.DefaultEmbeddingModel = string.Empty;
		provider.AuthType = GetSimpleGatewayProviderAuthType(normalizedProviderId);
		provider.AuthHeaderName = GetSimpleGatewayProviderAuthHeader(normalizedProviderId);
		provider.ChatEndpoint = GetSimpleGatewayProviderChatEndpoint(normalizedProviderId);
		provider.EmbeddingsEndpoint = GetSimpleGatewayProviderEmbeddingsEndpoint(normalizedProviderId);
		provider.ResponsesEndpoint = "responses";
		provider.AdditionalHeaders = [];
		provider.Routes = [];
		provider.Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = true,
			SupportsEmbeddings = GetSimpleGatewayProviderSupportsEmbeddings(normalizedProviderId),
			SupportsResponses = true,
			SupportsStreaming = true
		};
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

	private async void BtnQuickConfig_Click(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.Tag is not string configPath || string.IsNullOrWhiteSpace(configPath))
			return;

		await LoadConfigFromPathAsync(configPath, "Load Config");
	}

	private void AppendLog(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
			return;

		_logBuffer?.Enqueue(AppLogFormatting.Decode(message));
	}

	private void AppendLog(AppLogLevel level, string message)
	{
		if (string.IsNullOrWhiteSpace(message))
			return;

		_logBuffer?.Enqueue(new AppLogEntry(DateTime.Now, level, message.Trim()));
	}

	private void BatchAppendLogs(List<AppLogEntry> entries)
	{
		if (entries.Count == 0)
			return;

		lock (_logEntriesLock)
		{
			_logEntries.AddRange(entries);
			if (_logEntries.Count > MaxLogHistoryEntries)
				_logEntries.RemoveRange(0, _logEntries.Count - MaxLogHistoryEntries);
		}

		WriteLogsToDirectory(entries);
		AppendLogBatchToUi(entries);
	}

	private void WriteLogsToDirectory(IEnumerable<AppLogEntry> entries)
	{
		var activeSettings = GetActiveLogSettings();
		if (!activeSettings.WriteToDirectory)
			return;

		var materialized = entries
			.Where(entry => activeSettings.IsEnabled(entry.Level))
			.ToList();
		if (materialized.Count == 0)
			return;

		try
		{
			_logFileWriter.WriteEntries(materialized);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to write app logs to '{_logFileWriter.LogDirectory}': {ex}");
		}
	}

	private void AppendLogBatchToUi(List<AppLogEntry> entries)
	{
		if (_rtbLogs is null || _rtbLogs.IsDisposed || !_rtbLogs.IsHandleCreated)
			return;

		if (_rtbLogs.InvokeRequired)
		{
			try
			{
				_rtbLogs.BeginInvoke(() => AppendLogBatchToUi(entries));
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

		foreach (var entry in entries)
		{
			if (!IsLogLevelEnabled(entry.Level))
				continue;

			AppendLogEntryToRichTextBox(entry);
		}

		TrimRenderedLogsIfNeeded();
	}

	private void RebuildLogView()
	{
		if (_rtbLogs is null || _rtbLogs.IsDisposed || !_rtbLogs.IsHandleCreated)
			return;

		if (_rtbLogs.InvokeRequired)
		{
			try
			{
				_rtbLogs.BeginInvoke(RebuildLogView);
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

		List<AppLogEntry> snapshot;
		lock (_logEntriesLock)
		{
			snapshot = _logEntries
				.Where(entry => IsLogLevelEnabled(entry.Level))
				.TakeLast(MaxRenderedLogEntries)
				.ToList();
		}

		_rtbLogs.SuspendLayout();
		try
		{
			_rtbLogs.Clear();
			foreach (var entry in snapshot)
				AppendLogEntryToRichTextBox(entry);
		}
		finally
		{
			_rtbLogs.ResumeLayout();
		}
	}

	private void AppendLogEntryToRichTextBox(AppLogEntry entry)
	{
		if (_rtbLogs is null)
			return;

		_rtbLogs.SelectionStart = _rtbLogs.TextLength;
		_rtbLogs.SelectionLength = 0;
		_rtbLogs.SelectionColor = GetLogEntryColor(entry.Level);
		_rtbLogs.AppendText(entry.FormatForDisplay() + Environment.NewLine);
		_rtbLogs.SelectionColor = _rtbLogs.ForeColor;
		_rtbLogs.SelectionStart = _rtbLogs.TextLength;
		_rtbLogs.ScrollToCaret();
	}

	private void TrimRenderedLogsIfNeeded()
	{
		if (_rtbLogs is null)
			return;

		if (_rtbLogs.Lines.Length > MaxRenderedLogEntries)
			RebuildLogView();
	}

	private static Color GetLogEntryColor(AppLogLevel level)
	{
		return level switch
		{
			AppLogLevel.Debug => Color.LightGray,
			AppLogLevel.Information => Color.Lime,
			AppLogLevel.Warning => Color.Gold,
			AppLogLevel.Error => Color.OrangeRed,
			_ => Color.Lime
		};
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

	private async void TrayQuickConfigMenuItem_Click(object? sender, EventArgs e)
	{
		if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not string configPath || string.IsNullOrWhiteSpace(configPath))
			return;

		await LoadConfigFromPathAsync(configPath, "Load Config");
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
		lock (_logEntriesLock)
			_logEntries.Clear();

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

}
