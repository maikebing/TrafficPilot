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
	private bool _isLoadingGatewayProviderFields;
	private bool _isLoadingGatewayProviderOverview;
	private bool _isRebuildingGatewayProviderTabs;
	private string? _activeGatewayProviderId;

	private Dictionary<string, string> _localApiModelNameMap = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, GatewayProviderSettingsControl> _gatewayProviderEditors = new(StringComparer.OrdinalIgnoreCase);
	private CancellationTokenSource? _statusHintCts;

	public MainForm(bool startMinimized = false)
	{
		_startMinimized = startMinimized;
		_configManager = new ProxyConfigManager();
		_activeConfigPath = _configManager.GetConfigPath();
		_currentConfig = _configManager.Load(_activeConfigPath);
		_logBuffer = new LogBuffer(BatchAppendLogs);
		_autoUpdater = new AutoUpdater();
		InitializeComponent();
		InitializeGatewayProviderEditors();
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
		var gatewaySettings = BuildOllamaGatewaySettings();
		ApplyGatewayProviderEditorChanges(gatewaySettings);
		ApplyGatewayRouteEditorChanges(gatewaySettings);

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
			OllamaGateway = gatewaySettings,
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
		var gatewaySettings = BuildOllamaGatewaySettings();
		ApplyGatewayProviderEditorChanges(gatewaySettings);
		ApplyGatewayRouteEditorChanges(gatewaySettings);

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
			gatewaySettings,
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
		LoadGatewayProviderEditor(_currentConfig.OllamaGateway);
		UpdateGatewayRoutesPreview(_currentConfig.OllamaGateway);
		LoadGatewayRouteEditor(_currentConfig.OllamaGateway);

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

		var localApi = config.OllamaGateway ?? ProxyConfigManager.BuildGatewaySettingsFromLegacy(config.LocalApiForwarder);
		if (localApi?.Enabled != true)
			return;

		var provider = localApi.GetDefaultProvider();
		if (provider is null)
			return;
		var defaultModel = string.IsNullOrWhiteSpace(provider.DefaultModel)
			? "<empty>"
			: provider.DefaultModel;
		var embeddingModel = string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModel)
			? "<empty>"
			: provider.DefaultEmbeddingModel;
		AppendLog(
			$"[{DateTime.Now:HH:mm:ss.fff}] [Config] Local API provider='{provider.Name}', protocol={provider.Protocol}, baseUrl={provider.BaseUrl}, defaultModel={defaultModel}, embeddingModel={embeddingModel}");
	}

	private OllamaGatewaySettings? BuildOllamaGatewaySettings()
	{
		var legacy = BuildLocalApiForwarderSettings();
		return ProxyConfigManager.BuildGatewaySettingsFromLegacy(legacy);
	}

	private void InitializeGatewayProviderEditors()
	{
		if (_gatewayTabControl is null
			|| _gatewayOpenAiProviderControl is null
			|| _gatewayAnthropicProviderControl is null
			|| _gatewayGeminiProviderControl is null)
		{
			return;
		}

		_gatewayProviderEditors.Clear();
		_gatewayProviderEditors["openai"] = _gatewayOpenAiProviderControl;
		_gatewayProviderEditors["anthropic"] = _gatewayAnthropicProviderControl;
		_gatewayProviderEditors["google"] = _gatewayGeminiProviderControl;

		foreach (var editor in _gatewayProviderEditors.Values)
			WireGatewayProviderEditor(editor);

		BindGatewayProviderEditor("openai");
		_gatewayTabControl.SelectedIndexChanged += GatewayProviderTabs_SelectedIndexChanged;
	}

	private void WireGatewayProviderEditor(GatewayProviderSettingsControl editor)
	{
		editor.ProtocolComboBox.SelectedIndexChanged += CmbGatewayProviderProtocol2_SelectedIndexChanged;
		editor.DisplayNameTextBox.TextChanged += TxtGatewayProviderName2_TextChanged;
		editor.BaseUrlTextBox.TextChanged += TxtGatewayProviderBaseUrl2_TextChanged;
		editor.SupportsEmbeddingsCheckBox.CheckedChanged += ChkGatewaySupportsEmbeddings_CheckedChanged;
		editor.ShowAdvancedCheckBox.CheckedChanged += ChkGatewayProviderShowAdvanced_CheckedChanged;
		editor.DetectButton.Click += BtnGatewayDetectProvider_Click;
		editor.RefreshModelsButton.Click += BtnGatewayRefreshProviderModels_Click;
		editor.RefreshModelsApplyButton.Click += BtnGatewayRefreshProviderModelsApply_Click;
		editor.ModelPreviewListBox.SelectedIndexChanged += GatewayProviderModelPreview_SelectedIndexChanged;
		editor.ModelPreviewListBox.DoubleClick += GatewayProviderModelPreview_DoubleClick;
		editor.ModelPreviewListBox.MouseDown += GatewayProviderModelPreview_MouseDown;
		editor.CopyModelMetadataButton.Click += BtnGatewayCopyModelMetadata_Click;

		if (_gatewayModelPreviewContextMenu is not null)
			editor.ModelPreviewListBox.ContextMenuStrip = _gatewayModelPreviewContextMenu;
	}

	private void BindGatewayProviderEditor(string? providerId)
	{
		var editor = GetGatewayProviderEditor(providerId);
		if (editor is null)
			return;

		_activeGatewayProviderId = providerId;
		_txtGatewayProviderId = editor.ProviderIdTextBox;
		_cmbGatewayProviderProtocol2 = editor.ProtocolComboBox;
		_txtGatewayProviderName2 = editor.DisplayNameTextBox;
		_txtGatewayProviderBaseUrl2 = editor.BaseUrlTextBox;
		_txtGatewayProviderDefaultModel2 = editor.DefaultModelComboBox;
		_txtGatewayProviderEmbeddingModel2 = editor.DefaultEmbeddingModelComboBox;
		_txtLocalApiApiKey = editor.ApiKeyTextBox;
		_cmbGatewayProviderAuthType = editor.AuthTypeComboBox;
		_txtGatewayProviderAuthHeader = editor.AuthHeaderTextBox;
		_txtGatewayProviderChatEndpoint = editor.ChatEndpointTextBox;
		_txtGatewayProviderEmbeddingsEndpoint = editor.EmbeddingsEndpointTextBox;
		_txtGatewayProviderResponsesEndpoint = editor.ResponsesEndpointTextBox;
		_txtGatewayProviderAdditionalHeaders = editor.AdditionalHeadersTextBox;
		_chkGatewaySupportsChat = editor.SupportsChatCheckBox;
		_chkGatewaySupportsEmbeddings = editor.SupportsEmbeddingsCheckBox;
		_chkGatewaySupportsResponses = editor.SupportsResponsesCheckBox;
		_chkGatewaySupportsStreaming = editor.SupportsStreamingCheckBox;
		_chkGatewayProviderShowAdvanced = editor.ShowAdvancedCheckBox;
		_grpGatewayProviderAdvanced = editor.AdvancedGroupBox;
		_btnGatewayDetectProvider = editor.DetectButton;
		_btnGatewayRefreshProviderModels = editor.RefreshModelsButton;
		_btnGatewayRefreshProviderModelsApply = editor.RefreshModelsApplyButton;
		_txtGatewayProviderModelPreview = editor.ModelPreviewListBox;
		_txtGatewayProviderModelMetadata = editor.ModelMetadataTextBox;
		_btnGatewayCopyModelMetadata = editor.CopyModelMetadataButton;
	}

	private GatewayProviderSettingsControl? GetGatewayProviderEditor(string? providerId)
	{
		if (string.IsNullOrWhiteSpace(providerId))
			return null;

		return _gatewayProviderEditors.GetValueOrDefault(providerId.Trim());
	}

	private string? GetGatewayProviderId(TabPage? tabPage)
	{
		return tabPage?.Tag?.ToString() switch
		{
			"openai" => "openai",
			"anthropic" => "anthropic",
			"google" => "google",
			_ => null
		};
	}

	private TabPage? GetGatewayProviderTabPage(string? providerId)
	{
		return providerId?.Trim().ToLowerInvariant() switch
		{
			"openai" => _gatewayOpenAiProviderTab,
			"anthropic" => _gatewayAnthropicProviderTab,
			"google" => _gatewayGeminiProviderTab,
			_ => null
		};
	}

	private static string GetGatewayProviderTabTitle(string providerId)
	{
		return providerId.Trim().ToLowerInvariant() switch
		{
			"openai" => "OpenAI",
			"anthropic" => "Anthropic",
			"google" => "Gemini",
			_ => providerId
		};
	}

	private void UpdateGatewayRoutesPreview(OllamaGatewaySettings? gatewaySettings)
	{
		if (_txtGatewayRoutesPreview is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		if (gatewaySettings is null)
		{
			_txtGatewayRoutesPreview.Text = string.Empty;
			return;
		}

		var lines = new List<string>();
		foreach (var provider in gatewaySettings.Providers.Where(static provider => provider.Enabled))
		{
			var providerId = string.IsNullOrWhiteSpace(provider.Id) ? "default" : provider.Id.Trim();
			var providerName = string.IsNullOrWhiteSpace(provider.Name) ? providerId : provider.Name.Trim();
			lines.Add($"[{providerId}] {providerName} ({provider.Protocol})");

			var providerRoutes = gatewaySettings.Routes
				.Where(route => string.Equals(route.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
				.Where(static route => !string.IsNullOrWhiteSpace(route.LocalModel) || !string.IsNullOrWhiteSpace(route.UpstreamModel))
				.ToList();

			if (providerRoutes.Count == 0)
			{
				lines.Add("  (no explicit routes)");
			}
			else
			{
				foreach (var route in providerRoutes)
					lines.Add($"  {route.LocalModel} -> {route.UpstreamModel}");
			}

			if (!string.IsNullOrWhiteSpace(provider.DefaultModel))
				lines.Add($"  default chat: {provider.DefaultModel}");
			if (!string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModel))
				lines.Add($"  default embedding: {provider.DefaultEmbeddingModel}");
			lines.Add(string.Empty);
		}

		_txtGatewayRoutesPreview.Lines = lines.Count == 0 ? [] : lines.ToArray();
	}

	private void GatewayProviderTabs_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_isRebuildingGatewayProviderTabs)
			return;

		ApplyGatewayProviderEditorChanges(_currentConfig.OllamaGateway);
		LoadGatewayRouteEditor(_currentConfig.OllamaGateway);
		UpdateGatewayRoutesPreview(_currentConfig.OllamaGateway);
		UpdateGatewayOverviewProviderList(_currentConfig.OllamaGateway);

		var selectedProviderId = GetGatewayProviderId(_gatewayTabControl?.SelectedTab);
		if (selectedProviderId is null)
			return;

		BindGatewayProviderEditor(selectedProviderId);
		UpdateGatewayProviderEditorFields(_currentConfig.OllamaGateway, selectedProviderId);
		LoadGatewayProviderApiKey(_currentConfig.OllamaGateway, selectedProviderId);
		UpdateGatewayProviderModelPreview(_currentConfig.OllamaGateway, selectedProviderId);
		ApplyGatewayProviderBasicFieldPolicy();
		ApplyGatewayProviderAdvancedVisibility();
	}

	private void CmbGatewayProviderProtocol2_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (_isLoadingGatewayProviderFields)
			return;

		ApplyGatewayProviderProtocolTemplate();
	}

	private void TxtGatewayProviderBaseUrl2_TextChanged(object? sender, EventArgs e)
	{
		if (_isLoadingGatewayProviderFields)
			return;

		ApplyGatewayProviderEditorChanges(_currentConfig.OllamaGateway);
		UpdateGatewayRoutesPreview(_currentConfig.OllamaGateway);
		UpdateGatewayOverviewProviderList(_currentConfig.OllamaGateway);
	}

	private void ChkGatewaySupportsEmbeddings_CheckedChanged(object? sender, EventArgs e)
	{
		ApplyGatewayProviderCapabilityUiState();
	}

	private void ChkGatewayProviderShowAdvanced_CheckedChanged(object? sender, EventArgs e)
	{
		ApplyGatewayProviderAdvancedVisibility();
	}

	private async void BtnGatewayDetectProvider_Click(object? sender, EventArgs e)
	{
		if (_txtGatewayProviderBaseUrl2 is null)
			return;

		var baseUrl = _txtGatewayProviderBaseUrl2.Text.Trim();
		PersistGatewayProviderApiKey();
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			MessageBox.Show("Please enter Provider Base URL first.", "Detect Provider", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		if (_btnGatewayDetectProvider is not null)
			_btnGatewayDetectProvider.Enabled = false;

		try
		{
			ApplyDetectedProviderDefaults(baseUrl);
			await TryProbeProviderAsync(baseUrl);
			await TryPopulateGatewayProviderModelsAsync(baseUrl, autoApplySuggestedDefaults: false, showPrompt: true);
			ApplyGatewayProviderBasicFieldPolicy();
			ApplyGatewayProviderAdvancedVisibility();
			MessageBox.Show("Provider defaults detected and filled. Review them if needed.", "Detect Provider", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Detection failed: {ex.Message}", "Detect Provider", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		finally
		{
			if (_btnGatewayDetectProvider is not null)
				_btnGatewayDetectProvider.Enabled = true;
		}
	}

	private void BtnAddGatewayProvider_Click(object? sender, EventArgs e)
	{
		MessageBox.Show("当前不再支持新增 provider。请直接使用内置的 OpenAI、Anthropic、Gemini。", "Providers", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void LoadGatewayProviderEditor(OllamaGatewaySettings? gatewaySettings)
	{
		if (_gatewayTabControl is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var providers = gatewaySettings?.Providers ?? [];
		var selectedProviderId = GetSelectedGatewayProviderId() ?? "openai";

		_isRebuildingGatewayProviderTabs = true;
		foreach (var provider in providers)
		{
			var providerId = string.IsNullOrWhiteSpace(provider.Id) ? "default" : provider.Id.Trim();
			var tabPage = GetGatewayProviderTabPage(providerId);
			if (tabPage is not null)
				tabPage.Text = GetGatewayProviderTabTitle(providerId);

			BindGatewayProviderEditor(providerId);
			UpdateGatewayProviderEditorFields(gatewaySettings, providerId);
			LoadGatewayProviderApiKey(gatewaySettings, providerId);
			UpdateGatewayProviderModelPreview(gatewaySettings, providerId);
		}
		_isRebuildingGatewayProviderTabs = false;

		BindGatewayProviderEditor(selectedProviderId);
		UpdateGatewayOverviewProviderList(gatewaySettings);
		ApplyGatewayProviderBasicFieldPolicy();
		ApplyGatewayProviderAdvancedVisibility();
	}

	private string? GetSelectedGatewayProviderId()
	{
		return _activeGatewayProviderId ?? GetGatewayProviderId(_gatewayTabControl?.SelectedTab);
	}

	private void SelectGatewayProviderTab(string providerId)
	{
		var tabPage = GetGatewayProviderTabPage(providerId);
		if (_gatewayTabControl is null || tabPage is null)
			return;

		_gatewayTabControl.SelectedTab = tabPage;
	}

	private void UpdateGatewayOverviewProviderList(OllamaGatewaySettings? gatewaySettings)
	{
		if (_txtGatewayOverviewProviders is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var lines = new List<string>();
		foreach (var provider in gatewaySettings?.Providers ?? [])
		{
			var providerId = string.IsNullOrWhiteSpace(provider.Id) ? "default" : provider.Id.Trim();
			var name = string.IsNullOrWhiteSpace(provider.Name) ? providerId : provider.Name.Trim();
			lines.Add($"- {name} [{providerId}] ({provider.Protocol})");
			if (!string.IsNullOrWhiteSpace(provider.DefaultModel))
				lines.Add($"  chat: {provider.DefaultModel}");
			if (!string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModel))
				lines.Add($"  embedding: {provider.DefaultEmbeddingModel}");
		}

		_txtGatewayOverviewProviders.Lines = lines.Count == 0 ? ["<no providers>"] : lines.ToArray();
	}

	private CheckBox? GetGatewayProviderEnabledCheckBox(string? providerId)
	{
		return providerId?.Trim().ToLowerInvariant() switch
		{
			"openai" => _chkGatewayOpenAiEnabled,
			"anthropic" => _chkGatewayAnthropicEnabled,
			"google" => _chkGatewayGeminiEnabled,
			_ => null
		};
	}

	private void LoadGatewayProviderEnabledStates(OllamaGatewaySettings? gatewaySettings)
	{
		_isLoadingGatewayProviderOverview = true;
		try
		{
			gatewaySettings ??= BuildOllamaGatewaySettings();

			foreach (var providerId in new[] { "openai", "anthropic", "google" })
			{
				var checkBox = GetGatewayProviderEnabledCheckBox(providerId);
				if (checkBox is null)
					continue;

				var provider = gatewaySettings?.Providers.FirstOrDefault(p =>
					string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), providerId, StringComparison.OrdinalIgnoreCase));
				checkBox.Checked = provider?.Enabled == true;
			}
		}
		finally
		{
			_isLoadingGatewayProviderOverview = false;
		}
	}

	private void ChkGatewayProviderEnabled_CheckedChanged(object? sender, EventArgs e)
	{
		if (_isLoadingGatewayProviderOverview
			|| sender is not CheckBox checkBox
			|| checkBox.Tag is not string providerId)
		{
			return;
		}

		var gatewaySettings = _currentConfig.OllamaGateway ?? BuildOllamaGatewaySettings();
		if (gatewaySettings is null)
			return;

		ApplyGatewayProviderEditorChanges(gatewaySettings);
		ApplyGatewayRouteEditorChanges(gatewaySettings);

		var provider = gatewaySettings.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), providerId, StringComparison.OrdinalIgnoreCase));
		if (provider is null)
			return;

		provider.Enabled = checkBox.Checked;
		_currentConfig.OllamaGateway = gatewaySettings;
		LoadGatewayProviderEnabledStates(gatewaySettings);
		LoadGatewayProviderEditor(gatewaySettings);
		SelectGatewayProviderTab(providerId);
		LoadGatewayRouteEditor(gatewaySettings);
		UpdateGatewayRoutesPreview(gatewaySettings);
		UpdateGatewayOverviewProviderList(gatewaySettings);
	}

	private void TxtGatewayProviderName2_TextChanged(object? sender, EventArgs e)
	{
		if (_txtGatewayProviderName2 is null)
			return;

		if (_currentConfig.OllamaGateway is not null)
			UpdateGatewayOverviewProviderList(_currentConfig.OllamaGateway);
	}

	private void TxtGatewayOverviewProviders_DoubleClick(object? sender, EventArgs e)
	{
		JumpToProviderFromOverview(switchToProvidersTab: true);
	}

	private void TxtGatewayOverviewProviders_Click(object? sender, EventArgs e)
	{
		JumpToProviderFromOverview(switchToProvidersTab: false);
	}

	private void JumpToProviderFromOverview(bool switchToProvidersTab)
	{
		if (_txtGatewayOverviewProviders is null || _gatewayTabControl is null)
			return;

		var lineIndex = _txtGatewayOverviewProviders.GetLineFromCharIndex(_txtGatewayOverviewProviders.SelectionStart);
		if (lineIndex < 0 || lineIndex >= _txtGatewayOverviewProviders.Lines.Length)
			return;

		var line = _txtGatewayOverviewProviders.Lines[lineIndex].Trim();
		if (!line.StartsWith("- ") || !line.Contains("[", StringComparison.Ordinal) || !line.Contains("]", StringComparison.Ordinal))
			return;

		var start = line.IndexOf('[', StringComparison.Ordinal);
		var end = line.IndexOf(']', start + 1);
		if (start < 0 || end <= start)
			return;

		var providerId = line.Substring(start + 1, end - start - 1).Trim();
		if (string.IsNullOrWhiteSpace(providerId))
			return;

		SelectGatewayProviderTab(providerId);
	}

	private void UpdateGatewayProviderEditorFields(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
		if (_txtGatewayProviderId is null
			|| _cmbGatewayProviderProtocol2 is null
			|| _cmbGatewayProviderAuthType is null
			|| _txtGatewayProviderName2 is null
			|| _txtGatewayProviderBaseUrl2 is null
			|| _txtGatewayProviderDefaultModel2 is null
			|| _txtGatewayProviderEmbeddingModel2 is null
			|| _txtGatewayProviderAuthHeader is null
			|| _txtGatewayProviderChatEndpoint is null
			|| _txtGatewayProviderEmbeddingsEndpoint is null
			|| _txtGatewayProviderResponsesEndpoint is null
			|| _txtGatewayProviderAdditionalHeaders is null
			|| _chkGatewaySupportsChat is null
			|| _chkGatewaySupportsEmbeddings is null
			|| _chkGatewaySupportsResponses is null
			|| _chkGatewaySupportsStreaming is null)
		{
			return;
		}

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var provider = gatewaySettings?.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), providerId, StringComparison.OrdinalIgnoreCase));

		if (provider is null)
		{
			_isLoadingGatewayProviderFields = true;
			_txtGatewayProviderId.Text = string.Empty;
			_cmbGatewayProviderProtocol2.SelectedItem = null;
			_txtGatewayProviderName2.Text = string.Empty;
			_txtGatewayProviderBaseUrl2.Text = string.Empty;
			RebindEditableComboBox(_txtGatewayProviderDefaultModel2, [], string.Empty);
			RebindEditableComboBox(_txtGatewayProviderEmbeddingModel2, [], string.Empty);
			_cmbGatewayProviderAuthType.SelectedItem = null;
			_txtGatewayProviderAuthHeader.Text = string.Empty;
			_txtGatewayProviderChatEndpoint.Text = string.Empty;
			_txtGatewayProviderEmbeddingsEndpoint.Text = string.Empty;
			_txtGatewayProviderResponsesEndpoint.Text = string.Empty;
			_txtGatewayProviderAdditionalHeaders.Lines = [];
			_chkGatewaySupportsChat.Checked = false;
			_chkGatewaySupportsEmbeddings.Checked = false;
			_chkGatewaySupportsResponses.Checked = false;
			_chkGatewaySupportsStreaming.Checked = false;
			_isLoadingGatewayProviderFields = false;
			return;
		}

		_isLoadingGatewayProviderFields = true;
		_txtGatewayProviderId.Text = provider.Id ?? string.Empty;
		_cmbGatewayProviderProtocol2.SelectedItem = _cmbGatewayProviderProtocol2.Items.Contains(provider.Protocol)
			? provider.Protocol
			: "OpenAICompatible";
		_txtGatewayProviderName2.Text = provider.Name ?? string.Empty;
		_txtGatewayProviderBaseUrl2.Text = provider.BaseUrl ?? string.Empty;
		_txtGatewayProviderDefaultModel2.Text = provider.DefaultModel ?? string.Empty;
		_txtGatewayProviderEmbeddingModel2.Text = provider.DefaultEmbeddingModel ?? string.Empty;
		RebindEditableComboBox(_txtGatewayProviderDefaultModel2, provider.CachedModels ?? [], provider.DefaultModel ?? string.Empty);
		RebindEditableComboBox(_txtGatewayProviderEmbeddingModel2, provider.CachedEmbeddingModels ?? provider.CachedModels ?? [], provider.DefaultEmbeddingModel ?? string.Empty);
		UpdateGatewayProviderModelPreview(gatewaySettings, providerId);
		_cmbGatewayProviderAuthType.SelectedItem = _cmbGatewayProviderAuthType.Items.Contains(provider.AuthType)
			? provider.AuthType
			: "Bearer";
		_txtGatewayProviderAuthHeader.Text = provider.AuthHeaderName ?? string.Empty;
		_txtGatewayProviderChatEndpoint.Text = provider.ChatEndpoint ?? string.Empty;
		_txtGatewayProviderEmbeddingsEndpoint.Text = provider.EmbeddingsEndpoint ?? string.Empty;
		_txtGatewayProviderResponsesEndpoint.Text = provider.ResponsesEndpoint ?? string.Empty;
		_txtGatewayProviderAdditionalHeaders.Lines = provider.AdditionalHeaders?.Count > 0
			? provider.AdditionalHeaders
				.Where(static header => !string.IsNullOrWhiteSpace(header.Name))
				.Select(static header => $"{header.Name.Trim()}={header.Value}")
				.ToArray()
			: [];
		_chkGatewaySupportsChat.Checked = provider.Capabilities?.SupportsChat ?? true;
		_chkGatewaySupportsEmbeddings.Checked = provider.Capabilities?.SupportsEmbeddings ?? true;
		_chkGatewaySupportsResponses.Checked = provider.Capabilities?.SupportsResponses ?? true;
		_chkGatewaySupportsStreaming.Checked = provider.Capabilities?.SupportsStreaming ?? true;
		_isLoadingGatewayProviderFields = false;
		ApplyGatewayProviderCapabilityUiState();
	}

	private void ApplyGatewayProviderAdvancedVisibility()
	{
		if (_grpGatewayProviderAdvanced is null || _chkGatewayProviderShowAdvanced is null)
			return;

		_grpGatewayProviderAdvanced.Visible = _chkGatewayProviderShowAdvanced.Checked;
	}

	private void ApplyGatewayProviderBasicFieldPolicy()
	{
		if (_txtGatewayProviderName2 is null
			|| _lblGatewayProviderDisplayName is null
			|| _txtGatewayProviderId is null
			|| _cmbGatewayProviderProtocol2 is null
			|| _chkGatewaySupportsChat is null
			|| _chkGatewaySupportsEmbeddings is null
			|| _chkGatewaySupportsResponses is null
			|| _chkGatewaySupportsStreaming is null)
		{
			return;
		}

		_txtGatewayProviderId.ReadOnly = true;
		_cmbGatewayProviderProtocol2.Enabled = false;
		_chkGatewaySupportsChat.Enabled = false;
		_chkGatewaySupportsEmbeddings.Enabled = false;
		_chkGatewaySupportsResponses.Enabled = false;
		_chkGatewaySupportsStreaming.Enabled = false;
		_lblGatewayProviderDisplayName.Text = "Display Name:";
	}

	private void ApplyGatewayProviderProtocolTemplate()
	{
		if (_cmbGatewayProviderProtocol2 is null
			|| _cmbGatewayProviderAuthType is null
			|| _txtGatewayProviderBaseUrl2 is null
			|| _txtGatewayProviderAuthHeader is null
			|| _txtGatewayProviderChatEndpoint is null
			|| _txtGatewayProviderEmbeddingsEndpoint is null
			|| _txtGatewayProviderResponsesEndpoint is null
			|| _chkGatewaySupportsChat is null
			|| _chkGatewaySupportsEmbeddings is null
			|| _chkGatewaySupportsResponses is null
			|| _chkGatewaySupportsStreaming is null)
		{
			return;
		}

		var protocol = _cmbGatewayProviderProtocol2.SelectedItem?.ToString() ?? "OpenAICompatible";
		var isAnthropic = protocol.Equals("Anthropic", StringComparison.OrdinalIgnoreCase);

		if (string.IsNullOrWhiteSpace(_txtGatewayProviderBaseUrl2.Text)
			|| _txtGatewayProviderBaseUrl2.Text.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase)
			|| _txtGatewayProviderBaseUrl2.Text.Contains("api.anthropic.com", StringComparison.OrdinalIgnoreCase))
		{
			_txtGatewayProviderBaseUrl2.Text = isAnthropic
				? "https://api.anthropic.com/v1/"
				: "https://api.openai.com/v1/";
		}

		_cmbGatewayProviderAuthType.SelectedItem = isAnthropic ? "Header" : "Bearer";
		_txtGatewayProviderAuthHeader.Text = isAnthropic ? "x-api-key" : "Authorization";
		_txtGatewayProviderChatEndpoint.Text = isAnthropic ? "messages" : "chat/completions";
		_txtGatewayProviderEmbeddingsEndpoint.Text = isAnthropic ? string.Empty : "embeddings";
		_txtGatewayProviderResponsesEndpoint.Text = "responses";
		_chkGatewaySupportsChat.Checked = true;
		_chkGatewaySupportsEmbeddings.Checked = !isAnthropic;
		_chkGatewaySupportsResponses.Checked = true;
		_chkGatewaySupportsStreaming.Checked = true;
		ApplyGatewayProviderCapabilityUiState();
	}

	private void ApplyDetectedProviderDefaults(string baseUrl)
	{
		if (_cmbGatewayProviderProtocol2 is null)
			return;

		var normalized = baseUrl.Trim();
		var isAnthropic = normalized.Contains("anthropic", StringComparison.OrdinalIgnoreCase);
		var detectedProtocol = isAnthropic ? "Anthropic" : "OpenAICompatible";
		_cmbGatewayProviderProtocol2.SelectedItem = detectedProtocol;

		if (_txtGatewayProviderName2 is not null && string.IsNullOrWhiteSpace(_txtGatewayProviderName2.Text))
		{
			_txtGatewayProviderName2.Text = isAnthropic ? "Anthropic" : "OpenAI Compatible";
		}
	}

	private async Task TryProbeProviderAsync(string baseUrl)
	{
		using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
		var probeTargets = BuildProviderProbeTargets(baseUrl);
		var apiKey = _txtLocalApiApiKey?.Text.Trim();

		foreach (var probe in probeTargets)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, probe.Uri);
				ApplyProbeAuthentication(request, probe.Kind, apiKey);
				using var response = await client.SendAsync(request);
				if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
					continue;

				ApplyProbeResult(probe.Kind);
				return;
			}
			catch
			{
				// ignore and try next probe
			}
		}
	}

	private async Task TryPopulateGatewayProviderModelsAsync(string baseUrl, bool autoApplySuggestedDefaults, bool showPrompt)
	{
		if (_txtGatewayProviderDefaultModel2 is null
			|| _txtGatewayProviderEmbeddingModel2 is null
			|| _cmbGatewayProviderProtocol2 is null)
		{
			return;
		}

		var protocol = _cmbGatewayProviderProtocol2.SelectedItem?.ToString() ?? "OpenAICompatible";
		var apiKey = _txtLocalApiApiKey?.Text.Trim();
		var modelCatalog = await FetchGatewayProviderModelCatalogAsync(baseUrl, protocol, apiKey);
		if (modelCatalog.AllModels.Count == 0)
			return;

		CacheGatewayProviderModels(modelCatalog, protocol);

		var suggestedChat = modelCatalog.MessageModels.FirstOrDefault() ?? modelCatalog.AllModels[0];
		var suggestedEmbedding = modelCatalog.EmbeddingModels.FirstOrDefault();
		var preview = string.Join(", ", modelCatalog.AllModels.Take(6)) + (modelCatalog.AllModels.Count > 6 ? ", ..." : string.Empty);

		if (showPrompt)
		{
			var result = MessageBox.Show(
				$"检测成功，发现 {modelCatalog.AllModels.Count} 个模型。\r\n\r\n示例：{preview}\r\n\r\n是否自动填入默认 Chat 模型{(string.IsNullOrWhiteSpace(suggestedEmbedding) ? string.Empty : "和 Embedding 模型")}？",
				"Detect Models",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Information);

			if (result != DialogResult.Yes)
				return;
		}
		else if (!autoApplySuggestedDefaults)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(_txtGatewayProviderDefaultModel2.Text) || protocol.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
			_txtGatewayProviderDefaultModel2.Text = suggestedChat;

		if (!string.IsNullOrWhiteSpace(suggestedEmbedding)
			&& string.IsNullOrWhiteSpace(_txtGatewayProviderEmbeddingModel2.Text)
			&& (_chkGatewaySupportsEmbeddings?.Checked ?? false))
		{
			_txtGatewayProviderEmbeddingModel2.Text = suggestedEmbedding;
		}
	}

	private static async Task<GatewayProviderModelCatalog> FetchGatewayProviderModelCatalogAsync(string baseUrl, string protocol, string? apiKey)
	{
		using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
		var normalized = baseUrl.Trim().TrimEnd('/');
		var isAnthropic = protocol.Equals("Anthropic", StringComparison.OrdinalIgnoreCase);
		var uri = normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
			? $"{normalized}/models"
			: $"{normalized}/v1/models";

		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		ApplyProbeAuthentication(request, protocol, apiKey);
		if (isAnthropic)
			request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
		using var response = await client.SendAsync(request);
		if (!response.IsSuccessStatusCode)
			return GatewayProviderModelCatalog.Empty;

		var json = await response.Content.ReadAsStringAsync();
		using var doc = System.Text.Json.JsonDocument.Parse(json);
		System.Text.Json.JsonElement data;
		if (doc.RootElement.TryGetProperty("data", out var openAiData) && openAiData.ValueKind == System.Text.Json.JsonValueKind.Array)
		{
			data = openAiData;
		}
		else if (isAnthropic && doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
		{
			data = doc.RootElement;
		}
		else
		{
			return GatewayProviderModelCatalog.Empty;
		}

		var results = new List<string>();
		var messageModels = new List<string>();
		var otherModels = new List<string>();
		var moderationModels = new List<string>();
		var unknownModels = new List<string>();
		var summaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in data.EnumerateArray())
		{
			string? modelName = null;
			if (item.TryGetProperty("id", out var id) && id.ValueKind == System.Text.Json.JsonValueKind.String)
			{
				modelName = id.GetString();
			}
			else if (isAnthropic && item.TryGetProperty("name", out var name) && name.ValueKind == System.Text.Json.JsonValueKind.String)
			{
				modelName = name.GetString();
			}

			if (string.IsNullOrWhiteSpace(modelName))
				continue;

			var normalizedName = modelName.Trim();
			results.Add(normalizedName);
			summaries[normalizedName] = BuildModelSummary(item);
			if (isAnthropic)
			{
				var modelType = item.TryGetProperty("type", out var typeValue) && typeValue.ValueKind == System.Text.Json.JsonValueKind.String
					? typeValue.GetString()
					: null;
				var supportsMessages = string.Equals(modelType, "messages", StringComparison.OrdinalIgnoreCase)
					|| (item.TryGetProperty("capabilities", out var caps)
						&& caps.ValueKind == System.Text.Json.JsonValueKind.Object
						&& caps.TryGetProperty("messages", out var messagesCap)
						&& messagesCap.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False
						&& messagesCap.GetBoolean());

				var looksLikeEmbedding = normalizedName.Contains("embed", StringComparison.OrdinalIgnoreCase) || normalizedName.Contains("embedding", StringComparison.OrdinalIgnoreCase);
				var looksLikeModeration = normalizedName.Contains("moderation", StringComparison.OrdinalIgnoreCase) || string.Equals(modelType, "moderation", StringComparison.OrdinalIgnoreCase);

				if (supportsMessages)
					messageModels.Add(normalizedName);
				else if (looksLikeEmbedding)
					otherModels.Add(normalizedName);
				else if (looksLikeModeration)
					moderationModels.Add(normalizedName);
				else
					unknownModels.Add(normalizedName);
			}
		}

		var allModels = results.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		var embeddingModels = allModels.Where(static m => m.Contains("embed", StringComparison.OrdinalIgnoreCase) || m.Contains("embedding", StringComparison.OrdinalIgnoreCase)).ToList();
		return new GatewayProviderModelCatalog(
			allModels,
			embeddingModels,
			messageModels.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			otherModels.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			moderationModels.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			unknownModels.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
			summaries);
	}

	private static string BuildModelSummary(System.Text.Json.JsonElement item)
	{
		var fields = new List<string>();
		foreach (var property in item.EnumerateObject())
		{
			string value = property.Value.ValueKind switch
			{
				System.Text.Json.JsonValueKind.String => property.Value.GetString() ?? string.Empty,
				System.Text.Json.JsonValueKind.Number => property.Value.ToString(),
				System.Text.Json.JsonValueKind.True => "true",
				System.Text.Json.JsonValueKind.False => "false",
				System.Text.Json.JsonValueKind.Object => "{...}",
				System.Text.Json.JsonValueKind.Array => "[...]",
				_ => string.Empty
			};
			if (!string.IsNullOrWhiteSpace(value))
				fields.Add($"{property.Name}={value}");
		}
		return string.Join(", ", fields.Take(8));
	}

	private void CacheGatewayProviderModels(GatewayProviderModelCatalog catalog, string protocol)
	{
		if (_currentConfig.OllamaGateway is null)
			return;

		var selectedProviderId = GetSelectedGatewayProviderId();
		if (string.IsNullOrWhiteSpace(selectedProviderId))
			return;

		var provider = _currentConfig.OllamaGateway.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), selectedProviderId, StringComparison.OrdinalIgnoreCase));
		if (provider is null)
			return;

		provider.CachedModels = catalog.AllModels;
		provider.CachedEmbeddingModels = protocol.Equals("Anthropic", StringComparison.OrdinalIgnoreCase) ? catalog.EmbeddingModels : catalog.EmbeddingModels;
		provider.CachedMessageModels = catalog.MessageModels;
		provider.CachedOtherModels = catalog.OtherModels;
		provider.CachedModerationModels = catalog.ModerationModels;
		provider.CachedUnknownModels = catalog.UnknownModels;
		provider.CachedModelSummaries = catalog.Summaries;

		RebindEditableComboBox(_txtGatewayProviderDefaultModel2!, provider.CachedModels, _txtGatewayProviderDefaultModel2!.Text);
		RebindEditableComboBox(_txtGatewayProviderEmbeddingModel2!, provider.CachedEmbeddingModels.Count > 0 ? provider.CachedEmbeddingModels : provider.CachedModels, _txtGatewayProviderEmbeddingModel2!.Text);
		UpdateGatewayProviderModelPreview(_currentConfig.OllamaGateway, selectedProviderId);
	}

	private void UpdateGatewayProviderModelPreview(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
		if (_txtGatewayProviderModelPreview is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var provider = gatewaySettings?.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), providerId, StringComparison.OrdinalIgnoreCase));

		if (provider is null)
		{
			_txtGatewayProviderModelPreview.Items.Clear();
			if (_txtGatewayProviderModelMetadata is not null)
				_txtGatewayProviderModelMetadata.Text = string.Empty;
			return;
		}

		_txtGatewayProviderModelPreview.BeginUpdate();
		try
		{
			_txtGatewayProviderModelPreview.Items.Clear();

		if (string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase))
		{
			AddPreviewSection("Messages", provider.CachedMessageModels, provider.DefaultModel, null);
			AddPreviewSection("Embeddings", provider.CachedOtherModels, null, provider.DefaultEmbeddingModel);
			AddPreviewSection("Moderation", provider.CachedModerationModels, null, null);
			AddPreviewSection("Unknown", provider.CachedUnknownModels, null, null);
			return;
		}

			AddPreviewSection("Models", provider.CachedModels, provider.DefaultModel, provider.DefaultEmbeddingModel);
		}
		finally
		{
			_txtGatewayProviderModelPreview.EndUpdate();
		}

		void AddPreviewSection(string title, IEnumerable<string> items, string? defaultChat, string? defaultEmbedding)
		{
			_txtGatewayProviderModelPreview.Items.Add($"[{title}]");
			var hasItem = false;
			foreach (var item in items.DefaultIfEmpty("<none>"))
			{
				hasItem = true;
				var display = item;
				if (string.Equals(item, defaultChat, StringComparison.OrdinalIgnoreCase))
					display = $"* {item}  [default]";
				else if (string.Equals(item, defaultEmbedding, StringComparison.OrdinalIgnoreCase))
					display = $"* {item}  [embedding]";
				_txtGatewayProviderModelPreview.Items.Add(display);
			}
			if (!hasItem)
				_txtGatewayProviderModelPreview.Items.Add("<none>");
			_txtGatewayProviderModelPreview.Items.Add(string.Empty);
		}
	}

	private async void BtnGatewayRefreshProviderModels_Click(object? sender, EventArgs e)
	{
		await RefreshGatewayProviderModelsAsync(autoApplySuggestedDefaults: false, showPrompt: false);
	}

	private async void BtnGatewayRefreshProviderModelsApply_Click(object? sender, EventArgs e)
	{
		await RefreshGatewayProviderModelsAsync(autoApplySuggestedDefaults: true, showPrompt: false);
	}

	private async Task RefreshGatewayProviderModelsAsync(bool autoApplySuggestedDefaults, bool showPrompt)
	{
		if (_txtGatewayProviderBaseUrl2 is null || _btnGatewayRefreshProviderModels is null || _btnGatewayRefreshProviderModelsApply is null)
			return;

		var baseUrl = _txtGatewayProviderBaseUrl2.Text.Trim();
		if (string.IsNullOrWhiteSpace(baseUrl))
		{
			MessageBox.Show("Please enter Provider Base URL first.", "Refresh Models", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}

		PersistGatewayProviderApiKey();
		_btnGatewayRefreshProviderModels.Enabled = false;
		_btnGatewayRefreshProviderModelsApply.Enabled = false;
		try
		{
			var chatBefore = _txtGatewayProviderDefaultModel2?.Text.Trim() ?? string.Empty;
			var embeddingBefore = _txtGatewayProviderEmbeddingModel2?.Text.Trim() ?? string.Empty;
			await TryPopulateGatewayProviderModelsAsync(baseUrl, autoApplySuggestedDefaults, showPrompt);
			if (autoApplySuggestedDefaults)
			{
				var providerId = GetSelectedGatewayProviderId();
				var provider = _currentConfig.OllamaGateway?.Providers.FirstOrDefault(p =>
					string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), providerId, StringComparison.OrdinalIgnoreCase));
				var count = provider?.CachedModels.Count ?? 0;
				var chatAfter = _txtGatewayProviderDefaultModel2?.Text.Trim() ?? string.Empty;
				var embeddingAfter = _txtGatewayProviderEmbeddingModel2?.Text.Trim() ?? string.Empty;
				MessageBox.Show(
					$"已刷新 {count} 个模型，已应用默认 Chat={(string.IsNullOrWhiteSpace(chatAfter) ? "<none>" : chatAfter)}{(string.IsNullOrWhiteSpace(embeddingAfter) ? string.Empty : $"，Embedding={embeddingAfter}")}",
					"Refresh + Apply",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
				AppendLog($"[{DateTime.Now:HH:mm:ss.fff}] [Gateway] Refreshed {count} models and applied defaults: Chat={(string.IsNullOrWhiteSpace(chatAfter) ? "<none>" : chatAfter)}{(string.IsNullOrWhiteSpace(embeddingAfter) ? string.Empty : $", Embedding={embeddingAfter}")}");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Refresh failed: {ex.Message}", "Refresh Models", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
		finally
		{
			_btnGatewayRefreshProviderModels.Enabled = true;
			_btnGatewayRefreshProviderModelsApply.Enabled = true;
		}
	}

	private void GatewayProviderModelPreview_DoubleClick(object? sender, EventArgs e)
	{
		ApplySelectedPreviewModelByCategory();
	}

	private void GatewayProviderModelPreview_MouseDown(object? sender, MouseEventArgs e)
	{
		if (_txtGatewayProviderModelPreview is null)
			return;

		var index = _txtGatewayProviderModelPreview.IndexFromPoint(e.Location);
		if (index >= 0)
			_txtGatewayProviderModelPreview.SelectedIndex = index;
	}

	private void GatewayProviderModelPreview_SelectedIndexChanged(object? sender, EventArgs e)
	{
		UpdateSelectedGatewayModelMetadata();
	}

	private void GatewaySetAsChatModelMenuItem_Click(object? sender, EventArgs e)
	{
		ApplySelectedPreviewModel(forceEmbedding: false);
	}

	private void GatewaySetAsEmbeddingModelMenuItem_Click(object? sender, EventArgs e)
	{
		ApplySelectedPreviewModel(forceEmbedding: true);
	}

	private void ApplySelectedPreviewModelByCategory()
	{
		ApplySelectedPreviewModel(forceEmbedding: null);
	}

	private void ApplySelectedPreviewModel(bool? forceEmbedding)
	{
		if (_txtGatewayProviderModelPreview is null)
			return;

		var selected = _txtGatewayProviderModelPreview.SelectedItem?.ToString();
		if (string.IsNullOrWhiteSpace(selected) || selected.StartsWith("[") || selected.Equals("<none>", StringComparison.OrdinalIgnoreCase))
			return;

		var normalized = selected.Replace("* ", string.Empty).Replace("  [default]", string.Empty).Replace("  [embedding]", string.Empty).Trim();
		var selectedProviderId = GetSelectedGatewayProviderId();
		var provider = _currentConfig.OllamaGateway?.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), selectedProviderId, StringComparison.OrdinalIgnoreCase));
		if (provider is null)
			return;

		if (forceEmbedding == true)
		{
			_txtGatewayProviderEmbeddingModel2!.Text = normalized;
		}
		else if (forceEmbedding == false)
		{
			_txtGatewayProviderDefaultModel2!.Text = normalized;
		}
		else if (string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase))
		{
			if (provider.CachedMessageModels.Contains(normalized, StringComparer.OrdinalIgnoreCase))
				_txtGatewayProviderDefaultModel2!.Text = normalized;
			else if (provider.CachedOtherModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) || provider.CachedEmbeddingModels.Contains(normalized, StringComparer.OrdinalIgnoreCase))
				_txtGatewayProviderEmbeddingModel2!.Text = normalized;
		}
		else
		{
			if (provider.CachedEmbeddingModels.Contains(normalized, StringComparer.OrdinalIgnoreCase))
				_txtGatewayProviderEmbeddingModel2!.Text = normalized;
			else
				_txtGatewayProviderDefaultModel2!.Text = normalized;
		}

		ApplyGatewayProviderEditorChanges(_currentConfig.OllamaGateway);
		UpdateGatewayProviderModelPreview(_currentConfig.OllamaGateway, selectedProviderId);
	}

	private void UpdateSelectedGatewayModelMetadata()
	{
		if (_txtGatewayProviderModelPreview is null || _txtGatewayProviderModelMetadata is null)
			return;

		var selected = _txtGatewayProviderModelPreview.SelectedItem?.ToString();
		if (string.IsNullOrWhiteSpace(selected) || selected.StartsWith("[") || selected.Equals("<none>", StringComparison.OrdinalIgnoreCase))
		{
			_txtGatewayProviderModelMetadata.Text = string.Empty;
			if (_lblStatus is not null && _lblStatus.Text.StartsWith("Model:", StringComparison.OrdinalIgnoreCase))
				_lblStatus.Text = _engine?.IsRunning == true ? "Status: Running" : "Status: Stopped";
			return;
		}

		var normalized = selected.Replace("* ", string.Empty).Replace("  [default]", string.Empty).Replace("  [embedding]", string.Empty).Trim();
		var selectedProviderId = GetSelectedGatewayProviderId();
		var provider = _currentConfig.OllamaGateway?.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), selectedProviderId, StringComparison.OrdinalIgnoreCase));
		if (provider is null)
		{
			_txtGatewayProviderModelMetadata.Text = string.Empty;
			return;
		}

		var lines = new List<string>
		{
			$"Model: {normalized}",
			$"Provider: {provider.Name} ({provider.Id})",
			$"Protocol: {provider.Protocol}"
		};

		if (string.Equals(provider.DefaultModel, normalized, StringComparison.OrdinalIgnoreCase))
			lines.Add("Role: Default Chat Model");
		else if (string.Equals(provider.DefaultEmbeddingModel, normalized, StringComparison.OrdinalIgnoreCase))
			lines.Add("Role: Default Embedding Model");

		if (string.Equals(provider.Protocol, "Anthropic", StringComparison.OrdinalIgnoreCase))
		{
			var category = provider.CachedMessageModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? "messages"
				: provider.CachedOtherModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? "embeddings"
				: provider.CachedModerationModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? "moderation"
				: provider.CachedUnknownModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? "unknown"
				: "uncategorized";
			lines.Add($"Category: {category}");
		}
		else
		{
			lines.Add($"Category: {(provider.CachedEmbeddingModels.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? "embedding-capable" : "general")}");
		}

		if (provider.CachedModelSummaries.TryGetValue(normalized, out var summary) && !string.IsNullOrWhiteSpace(summary))
			lines.Add($"Raw Fields: {summary}");

		_txtGatewayProviderModelMetadata.Text = string.Join(Environment.NewLine, lines);
		if (_lblStatus is not null)
			ShowTemporaryStatusHint($"Model: {normalized} ({lines.LastOrDefault(static line => line.StartsWith("Category:"))?.Replace("Category: ", string.Empty) ?? "selected"})");
	}

	private void BtnGatewayCopyModelMetadata_Click(object? sender, EventArgs e)
	{
		if (_txtGatewayProviderModelMetadata is null || string.IsNullOrWhiteSpace(_txtGatewayProviderModelMetadata.Text))
			return;

		Clipboard.SetText(_txtGatewayProviderModelMetadata.Text);
		ShowTemporaryStatusHint("Model metadata copied");
	}

	private void ShowTemporaryStatusHint(string message, int delayMilliseconds = 3000)
	{
		if (_lblStatus is null)
			return;

		_statusHintCts?.Cancel();
		_statusHintCts?.Dispose();
		_statusHintCts = new CancellationTokenSource();
		var token = _statusHintCts.Token;

		_lblStatus.Text = message;
		_ = RestoreStatusLabelAsync(delayMilliseconds, token);
	}

	private async Task RestoreStatusLabelAsync(int delayMilliseconds, CancellationToken cancellationToken)
	{
		try
		{
			await Task.Delay(delayMilliseconds, cancellationToken);
			if (cancellationToken.IsCancellationRequested || _lblStatus is null || _lblStatus.IsDisposed)
				return;

			if (_lblStatus.InvokeRequired)
			{
				_lblStatus.BeginInvoke(() => RestoreStatusLabel());
			}
			else
			{
				RestoreStatusLabel();
			}
		}
		catch (TaskCanceledException)
		{
		}
	}

	private void RestoreStatusLabel()
	{
		if (_lblStatus is null)
			return;

		if (_engine?.IsRunning == true)
		{
			_lblStatus.Text = "Status: Running";
			_lblStatus.ForeColor = Color.Green;
		}
		else
		{
			_lblStatus.Text = "Status: Stopped";
			_lblStatus.ForeColor = Color.Black;
		}
	}

	private sealed record GatewayProviderModelCatalog(
		List<string> AllModels,
		List<string> EmbeddingModels,
		List<string> MessageModels,
		List<string> OtherModels,
		List<string> ModerationModels,
		List<string> UnknownModels,
		Dictionary<string, string> Summaries)
	{
		public static GatewayProviderModelCatalog Empty { get; } = new([], [], [], [], [], [], new(StringComparer.OrdinalIgnoreCase));
	}

	private IEnumerable<(string Kind, string Uri)> BuildProviderProbeTargets(string baseUrl)
	{
		var normalized = baseUrl.Trim().TrimEnd('/');
		yield return ("Anthropic", normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? $"{normalized}/messages" : $"{normalized}/v1/messages");
		yield return ("OpenAICompatible", normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? $"{normalized}/models" : $"{normalized}/v1/models");
	}

	private void ApplyProbeResult(string kind)
	{
		if (_cmbGatewayProviderProtocol2 is null)
			return;

		_cmbGatewayProviderProtocol2.SelectedItem = kind;
	}

	private static void ApplyProbeAuthentication(HttpRequestMessage request, string kind, string? apiKey)
	{
		if (string.IsNullOrWhiteSpace(apiKey))
			return;

		if (kind.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
		{
			request.Headers.TryAddWithoutValidation("x-api-key", apiKey.Trim());
			request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
			return;
		}

		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey.Trim());
	}

	private void PersistGatewayProviderApiKey()
	{
		if (_txtLocalApiApiKey is null)
			return;

		var providerKey = GetSelectedGatewayProviderId();
		if (string.IsNullOrWhiteSpace(providerKey))
			return;

		var targetName = CredentialManager.GetLocalApiTargetName(providerKey.Trim());
		var apiKey = _txtLocalApiApiKey.Text.Trim();
		if (string.IsNullOrWhiteSpace(apiKey))
			CredentialManager.DeleteToken(targetName);
		else
			CredentialManager.SaveToken(targetName, apiKey);
	}

	private void LoadGatewayProviderApiKey(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
		if (_txtLocalApiApiKey is null)
			return;

		if (string.IsNullOrWhiteSpace(providerId))
		{
			_txtLocalApiApiKey.Text = string.Empty;
			return;
		}

		_txtLocalApiApiKey.Text = CredentialManager.LoadToken(CredentialManager.GetLocalApiTargetName(providerId.Trim())) ?? string.Empty;
	}

	private void ApplyGatewayProviderCapabilityUiState()
	{
		if (_chkGatewaySupportsEmbeddings is null
			|| _txtGatewayProviderEmbeddingModel2 is null
			|| _txtGatewayProviderEmbeddingsEndpoint is null)
		{
			return;
		}

		var supportsEmbeddings = _chkGatewaySupportsEmbeddings.Checked;
		_txtGatewayProviderEmbeddingModel2.Enabled = supportsEmbeddings;
		_txtGatewayProviderEmbeddingsEndpoint.Enabled = supportsEmbeddings;
		if (!supportsEmbeddings)
		{
			_txtGatewayProviderEmbeddingModel2.Text = string.Empty;
			_txtGatewayProviderEmbeddingsEndpoint.Text = string.Empty;
		}
	}

	private void ApplyGatewayProviderEditorChanges(OllamaGatewaySettings? gatewaySettings)
	{
		if (gatewaySettings is null
			|| _txtGatewayProviderId is null
			|| _cmbGatewayProviderProtocol2 is null
			|| _cmbGatewayProviderAuthType is null
			|| _txtGatewayProviderName2 is null
			|| _txtGatewayProviderBaseUrl2 is null
			|| _txtGatewayProviderDefaultModel2 is null
			|| _txtGatewayProviderEmbeddingModel2 is null
			|| _txtGatewayProviderAuthHeader is null
			|| _txtGatewayProviderChatEndpoint is null
			|| _txtGatewayProviderEmbeddingsEndpoint is null
			|| _txtGatewayProviderResponsesEndpoint is null
			|| _txtGatewayProviderAdditionalHeaders is null
			|| _chkGatewaySupportsChat is null
			|| _chkGatewaySupportsEmbeddings is null
			|| _chkGatewaySupportsResponses is null
			|| _chkGatewaySupportsStreaming is null)
		{
			return;
		}

		var selectedProviderId = GetSelectedGatewayProviderId();
		if (string.IsNullOrWhiteSpace(selectedProviderId))
			return;

		var provider = gatewaySettings.Providers.FirstOrDefault(p =>
			string.Equals(string.IsNullOrWhiteSpace(p.Id) ? "default" : p.Id.Trim(), selectedProviderId, StringComparison.OrdinalIgnoreCase));
		if (provider is null)
			return;

		provider.Id = string.IsNullOrWhiteSpace(_txtGatewayProviderId.Text) ? selectedProviderId.Trim() : _txtGatewayProviderId.Text.Trim();
		provider.Protocol = _cmbGatewayProviderProtocol2.SelectedItem?.ToString() ?? "OpenAICompatible";
		provider.Name = _txtGatewayProviderName2.Text.Trim();
		provider.BaseUrl = _txtGatewayProviderBaseUrl2.Text.Trim();
		provider.DefaultModel = _txtGatewayProviderDefaultModel2.Text.Trim();
		provider.DefaultEmbeddingModel = _txtGatewayProviderEmbeddingModel2.Text.Trim();
		provider.AuthType = _cmbGatewayProviderAuthType.SelectedItem?.ToString() ?? "Bearer";
		provider.AuthHeaderName = _txtGatewayProviderAuthHeader.Text.Trim();
		provider.ChatEndpoint = _txtGatewayProviderChatEndpoint.Text.Trim();
		provider.EmbeddingsEndpoint = _txtGatewayProviderEmbeddingsEndpoint.Text.Trim();
		provider.ResponsesEndpoint = _txtGatewayProviderResponsesEndpoint.Text.Trim();
		provider.AdditionalHeaders = ParseLocalApiHeaders(_txtGatewayProviderAdditionalHeaders.Lines);
		provider.Capabilities = new GatewayProviderCapabilitySettings
		{
			SupportsChat = _chkGatewaySupportsChat.Checked,
			SupportsEmbeddings = _chkGatewaySupportsEmbeddings.Checked,
			SupportsResponses = _chkGatewaySupportsResponses.Checked,
			SupportsStreaming = _chkGatewaySupportsStreaming.Checked
		};

		gatewaySettings.Routes = gatewaySettings.Routes
			.Select(route => string.Equals(route.ProviderId, selectedProviderId, StringComparison.OrdinalIgnoreCase)
				? new GatewayRouteSettings
				{
					ProviderId = provider.Id,
					LocalModel = route.LocalModel,
					UpstreamModel = route.UpstreamModel
				}
				: route)
			.ToList();

		LoadGatewayProviderEditor(gatewaySettings);
		LoadGatewayRouteEditor(gatewaySettings);
	}

	private static string BuildNextGatewayProviderId(OllamaGatewaySettings gatewaySettings)
	{
		var existing = gatewaySettings.Providers
			.Select(static provider => string.IsNullOrWhiteSpace(provider.Id) ? "default" : provider.Id.Trim())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		for (var index = 1; index <= 999; index++)
		{
			var candidate = $"provider-{index}";
			if (!existing.Contains(candidate))
				return candidate;
		}

		return $"provider-{Guid.NewGuid():N}";
	}

	private void CmbGatewayRouteProvider_SelectedIndexChanged(object? sender, EventArgs e)
	{
		ApplyGatewayRouteEditorChanges(_currentConfig.OllamaGateway);
		UpdateGatewayRoutesPreview(_currentConfig.OllamaGateway);
		UpdateGatewayRouteEditorText(_currentConfig.OllamaGateway, _cmbGatewayRouteProvider?.SelectedItem?.ToString());
	}

	private void TxtGatewayRouteMappings_TextChanged(object? sender, EventArgs e)
	{
		UpdateGatewayRouteValidationState();
	}

	private void LoadGatewayRouteEditor(OllamaGatewaySettings? gatewaySettings)
	{
		if (_cmbGatewayRouteProvider is null || _txtGatewayRouteMappings is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		var providers = gatewaySettings?.Providers ?? [];

		var selectedProviderId = _cmbGatewayRouteProvider.SelectedItem?.ToString();
		_cmbGatewayRouteProvider.SelectedIndexChanged -= CmbGatewayRouteProvider_SelectedIndexChanged;
		_cmbGatewayRouteProvider.Items.Clear();
		foreach (var provider in providers.Where(static provider => provider.Enabled))
		{
			var providerId = string.IsNullOrWhiteSpace(provider.Id) ? "default" : provider.Id.Trim();
			_cmbGatewayRouteProvider.Items.Add(providerId);
			if (string.Equals(providerId, selectedProviderId, StringComparison.OrdinalIgnoreCase))
				_cmbGatewayRouteProvider.SelectedItem = providerId;
		}

		if (_cmbGatewayRouteProvider.SelectedItem is null && _cmbGatewayRouteProvider.Items.Count > 0)
			_cmbGatewayRouteProvider.SelectedIndex = 0;
		_cmbGatewayRouteProvider.SelectedIndexChanged += CmbGatewayRouteProvider_SelectedIndexChanged;

		UpdateGatewayRouteEditorText(gatewaySettings, _cmbGatewayRouteProvider.SelectedItem?.ToString());
	}

	private void UpdateGatewayRouteEditorText(OllamaGatewaySettings? gatewaySettings, string? providerId)
	{
		if (_txtGatewayRouteMappings is null)
			return;

		gatewaySettings ??= BuildOllamaGatewaySettings();
		if (gatewaySettings is null || string.IsNullOrWhiteSpace(providerId))
		{
			_txtGatewayRouteMappings.Lines = [];
			return;
		}

		var lines = gatewaySettings.Routes
			.Where(route => string.Equals(route.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
			.Where(static route => !string.IsNullOrWhiteSpace(route.LocalModel) || !string.IsNullOrWhiteSpace(route.UpstreamModel))
			.Select(static route => $"{route.LocalModel}={route.UpstreamModel}")
			.ToArray();

		_txtGatewayRouteMappings.Lines = lines;
		UpdateGatewayRouteValidationState();
	}

	private void ApplyGatewayRouteEditorChanges(OllamaGatewaySettings? gatewaySettings)
	{
		if (gatewaySettings is null
			|| _cmbGatewayRouteProvider is null
			|| _txtGatewayRouteMappings is null)
		{
			return;
		}

		var providerId = _cmbGatewayRouteProvider.SelectedItem?.ToString();
		if (string.IsNullOrWhiteSpace(providerId))
			return;

		if (!TryValidateGatewayRouteMappings(_txtGatewayRouteMappings.Lines, out _, out _, out _))
			return;

		var parsedRoutes = ParseGatewayRouteMappings(_txtGatewayRouteMappings.Lines, providerId);
		gatewaySettings.Routes = gatewaySettings.Routes
			.Where(route => !string.Equals(route.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
			.Concat(parsedRoutes)
			.GroupBy(static route => $"{route.ProviderId}\n{route.LocalModel}", StringComparer.OrdinalIgnoreCase)
			.Select(static group => group.Last())
			.ToList();
	}

	private void UpdateGatewayRouteValidationState()
	{
		if (_txtGatewayRouteMappings is null || _lblGatewayRouteValidation is null)
			return;

		if (TryValidateGatewayRouteMappings(_txtGatewayRouteMappings.Lines, out var total, out var invalidLines, out var duplicateKeys))
		{
			_txtGatewayRouteMappings.BackColor = SystemColors.Window;
			_lblGatewayRouteValidation.ForeColor = duplicateKeys.Count > 0 ? Color.DarkOrange : SystemColors.GrayText;
			_lblGatewayRouteValidation.Text = duplicateKeys.Count > 0
				? $"{total} routes, duplicate local models: {string.Join(", ", duplicateKeys)} (last one wins)"
				: $"{total} routes valid. Format: local=upstream";
			return;
		}

		_txtGatewayRouteMappings.BackColor = Color.MistyRose;
		_lblGatewayRouteValidation.ForeColor = Color.Firebrick;
		_lblGatewayRouteValidation.Text = $"Invalid route lines: {string.Join(", ", invalidLines)}. Use local=upstream.";
	}

	private static bool TryValidateGatewayRouteMappings(
		IEnumerable<string> lines,
		out int totalRoutes,
		out List<int> invalidLines,
		out List<string> duplicateKeys)
	{
		totalRoutes = 0;
		invalidLines = [];
		duplicateKeys = [];
		var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var duplicateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var lineNumber = 0;

		foreach (var rawLine in lines)
		{
			lineNumber++;
			var line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith('#'))
				continue;

			var separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
			{
				invalidLines.Add(lineNumber);
				continue;
			}

			var localModel = line[..separatorIndex].Trim();
			var upstreamModel = line[(separatorIndex + 1)..].Trim();
			if (localModel.Length == 0 || upstreamModel.Length == 0)
			{
				invalidLines.Add(lineNumber);
				continue;
			}

			totalRoutes++;
			if (!seenKeys.Add(localModel))
				duplicateSet.Add(localModel);
		}

		duplicateKeys = duplicateSet.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToList();
		return invalidLines.Count == 0;
	}

	private static List<GatewayRouteSettings> ParseGatewayRouteMappings(IEnumerable<string> lines, string providerId)
	{
		var routes = new List<GatewayRouteSettings>();

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

			routes.Add(new GatewayRouteSettings
			{
				ProviderId = providerId.Trim(),
				LocalModel = localModel,
				UpstreamModel = upstreamModel
			});
		}

		return routes;
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
