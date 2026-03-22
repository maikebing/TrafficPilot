#nullable enable
using System.ComponentModel;
using TrafficPilot.Properties;

namespace TrafficPilot;

partial class MainForm
{
	private IContainer? components;

	// Main panels
	private TableLayoutPanel? _mainPanel;
	private FlowLayoutPanel? _statusPanel;

	// Tab and tab pages
	private TabControl? _tabControl;
	private TabPage? _configTab;
  private TabPage? _localApiTab;
	private TabPage? _logsTab;
	private TabPage? _dnsRedirectTab;
	private TabPage? _aboutTab;

	// Tray
	private NotifyIcon? _notifyIcon;
	private ContextMenuStrip? _contextMenu;
	private ToolStripMenuItem? _trayShowMenuItem;
	private ToolStripMenuItem? _trayHideMenuItem;
	private ToolStripSeparator? _trayTopSeparator;
	private ToolStripMenuItem? _trayStartProxyMenuItem;
	private ToolStripMenuItem? _trayStopProxyMenuItem;
	private ToolStripSeparator? _trayMiddleSeparator;
	private ToolStripMenuItem? _trayOptionsMenuItem;
	private ToolStripMenuItem? _trayStartOnBootMenuItem;
	private ToolStripMenuItem? _trayAutoStartProxyMenuItem;
	private ToolStripMenuItem? _trayConfigMenuItem;
	private ToolStripMenuItem? _trayLoadConfigMenuItem;
	private ToolStripMenuItem? _traySaveConfigAsMenuItem;
	private ToolStripMenuItem? _traySaveConfigMenuItem;
	private ToolStripSeparator? _trayConfigSeparator;
	private ToolStripSeparator? _trayBottomSeparator;
	private ToolStripMenuItem? _trayExitMenuItem;

	// Config tab - main panel
	private TableLayoutPanel? _configPanel;

	// Config tab - Proxy Enable
	private FlowLayoutPanel? _proxyHeaderPanel;
	private CheckBox? _chkProxyEnabled;
	private Label? _lblConfigName;
	private TextBox? _txtConfigName;

	// Config tab - Proxy Host
	private CheckBox? _chkLocalProxy;
	private Label? _lblProxyHost;
	private ComboBox? _cmbProxyHost;

	// Config tab - Proxy Port
	private Label? _lblProxyPort;
	private NumericUpDown? _numProxyPort;

	// Config tab - Proxy Scheme
	private Label? _lblProxyScheme;
	private ComboBox? _cmbProxyScheme;

	// Config tab - Process Rules
	private Label? _lblProcesses;
	private TableLayoutPanel? _procPanel;
	private TextBox? _txtProcesses;

	// Config tab - Domain Rules
	private Label? _lblDomainRules;
	private TableLayoutPanel? _domainRulesPanel;
	private TextBox? _txtDomainRules;

	// Config tab - Config File
	private Label? _lblConfigFile;
	private Label? _lblConfigFileValue;

	// Config tab - Startup
	private FlowLayoutPanel? _startupOptionsPanel;
	private CheckBox? _chkStartOnBoot;
	private CheckBox? _chkAutoStartProxy;

	// Config tab - Sync Settings
	private Label? _lblSyncProvider;
	private ComboBox? _cmbSyncProvider;
	private Label? _lblSyncToken;
	private TextBox? _txtSyncToken;
	private Label? _lblGistId;
	private TextBox? _txtGistId;
	private Label? _lblSyncActions;
	private FlowLayoutPanel? _syncActionsPanel;
	private Button? _btnSyncPush;
	private Button? _btnSyncPull;

	// Config tab - Proxy Settings combined row
	private TableLayoutPanel? _proxySettingsPanel;

	// Config tab - Sync combined rows
	private TableLayoutPanel? _syncProviderTokenPanel;
	private TableLayoutPanel? _gistIdActionPanel;

	// Config tab - Buttons
	private TableLayoutPanel? _configActionPanel;
	private FlowLayoutPanel? _configBtnPanel;
	private FlowLayoutPanel? _quickConfigPanel;
	private Button? _btnSaveConfigAs;
	private Button? _btnSaveConfig;
	private Button? _btnLoadConfig;

	// Logs tab
	private TableLayoutPanel? _logPanel;
	private RichTextBox? _rtbLogs;
	private FlowLayoutPanel? _btnClearPanel;
	private Button? _btnClearLogs;

	// Hosts Redirect tab
	private TableLayoutPanel? _dnsRedirectPanel;
	private CheckBox? _chkDNSRedirectEnabled;
	private Label? _lblHostsUrl;
	private TextBox? _txtHostsUrl;
	private FlowLayoutPanel? _hostsRedirectBtnPanel;
	private Button? _btnRefreshHosts;
	private Button? _btnFetchIps;
	private CheckBox? _chkRetestSlowOrTimeoutOnly;
	private Label? _lblHostsStatus;
	private ListView? _lvIpResults;
	private Label? _lblRefreshDomains;
	private TextBox? _txtRefreshDomains;
	private FlowLayoutPanel? _autoFetchPanel;
	private CheckBox? _chkAutoFetch;
	private NumericUpDown? _numAutoFetchInterval;
	private Label? _lblAutoFetchMinutes;

	// About tab
	private Panel? _aboutScrollPanel;
	private TableLayoutPanel? _aboutContentPanel;

    // Local API tab
    private TableLayoutPanel? _localApiPanel;
    private TabControl? _gatewayTabControl;
    private TabPage? _gatewayOverviewTab;
    private TabPage? _gatewayProviderTab;
    private TabPage? _gatewayRouteTab;
    private TabPage? _gatewayDiagnosticsTab;
    private TableLayoutPanel? _gatewayOverviewPanel;
    private Label? _lblGatewayProviderEnabled;
    private FlowLayoutPanel? _gatewayProviderEnabledPanel;
    private CheckBox? _chkGatewayOpenAiEnabled;
    private CheckBox? _chkGatewayAnthropicEnabled;
    private CheckBox? _chkGatewayGeminiEnabled;
    private Label? _lblGatewayOverviewSummary;
    private TextBox? _txtGatewayOverviewProviders;
    private GroupBox? _grpGatewayProviderBasic;
    private GroupBox? _grpGatewayProviderAdvanced;
    private TableLayoutPanel? _gatewayProviderAdvancedLayout;
    private GroupBox? _grpGatewayProviderAuth;
    private GroupBox? _grpGatewayProviderEndpoints;
    private GroupBox? _grpGatewayProviderCapabilities;
    private TableLayoutPanel? _gatewayProviderBasicPanel;
    private CheckBox? _chkGatewayProviderShowAdvanced;
    private Button? _btnGatewayDetectProvider;
    private FlowLayoutPanel? _gatewayProviderModelActionsPanel;
    private Button? _btnGatewayRefreshProviderModels;
    private Button? _btnGatewayRefreshProviderModelsApply;
    private Label? _lblGatewayDetectedProtocol;
    private TextBox? _txtGatewayDetectedProtocol;
    private Label? _lblGatewayProviderDisplayName;
    private Label? _lblGatewayProviderModelPreview;
    private ListBox? _txtGatewayProviderModelPreview;
    private Label? _lblGatewayProviderModelMetadata;
    private TextBox? _txtGatewayProviderModelMetadata;
    private Button? _btnGatewayCopyModelMetadata;
    private ContextMenuStrip? _gatewayModelPreviewContextMenu;
    private ToolStripMenuItem? _gatewaySetAsChatModelMenuItem;
    private ToolStripMenuItem? _gatewaySetAsEmbeddingModelMenuItem;
    private FlowLayoutPanel? _localApiHeaderPanel;
    private CheckBox? _chkLocalApiForwarderEnabled;
    private Label? _lblLocalApiHeaderHelp;
    private Label? _lblLocalApiListenPorts;
    private TableLayoutPanel? _localApiPortPanel;
    private Label? _lblOllamaPort;
    private NumericUpDown? _numOllamaPort;
    private Label? _lblFoundryPort;
    private NumericUpDown? _numFoundryPort;
    private Label? _lblLocalApiProvider;
    private TableLayoutPanel? _localApiProviderPanel;
    private Label? _lblLocalApiProtocol;
    private ComboBox? _cmbLocalApiProviderProtocol;
    private Label? _lblLocalApiProviderName;
    private TextBox? _txtLocalApiProviderName;
    private Label? _lblLocalApiProviderUrl;
    private TableLayoutPanel? _localApiProviderUrlPanel;
    private TextBox? _txtLocalApiProviderUrl;
    private Button? _btnRefreshLocalApiModels;
    private Label? _lblLocalApiDefaultModel;
    private ComboBox? _cmbLocalApiDefaultModel;
    private Label? _lblLocalApiDefaultEmbeddingModel;
    private ComboBox? _cmbLocalApiDefaultEmbeddingModel;
    private Label? _lblLocalApiAuthentication;
    private TableLayoutPanel? _localApiAuthPanel;
    private Label? _lblLocalApiAuthType;
    private ComboBox? _cmbLocalApiAuthType;
    private Label? _lblLocalApiAuthHeaderName;
    private TextBox? _txtLocalApiAuthHeaderName;
    private Label? _lblLocalApiApiKey;
    private TextBox? _txtLocalApiApiKey;
    private Label? _lblLocalApiExtraHeaders;
    private TextBox? _txtLocalApiAdditionalHeaders;
    private Label? _lblLocalApiModelMappings;
    private TextBox? _txtLocalApiModelMappings;
    private Label? _lblGatewayRoutesPreview;
    private TextBox? _txtGatewayRoutesPreview;
    private Label? _lblGatewayRouteEditor;
    private TableLayoutPanel? _gatewayRouteEditorPanel;
    private Label? _lblGatewayRouteProvider;
    private ComboBox? _cmbGatewayRouteProvider;
    private Label? _lblGatewayRouteMappings;
    private TextBox? _txtGatewayRouteMappings;
    private Label? _lblGatewayRouteValidation;
    private Label? _lblGatewayProviderEditor;
    private TableLayoutPanel? _gatewayProviderEditorPanel;
    private Label? _lblGatewayProviderSelection;
    private TabControl? _gatewayProviderTabs;
    private TabPage? _gatewayProviderOpenAiTab;
    private TabPage? _gatewayProviderAnthropicTab;
    private TabPage? _gatewayProviderGeminiTab;
    private FlowLayoutPanel? _gatewayProviderButtonsPanel;
    private Panel? _gatewayProviderContentScrollPanel;
    private Button? _btnAddGatewayProvider;
    private Button? _btnDuplicateGatewayProvider;
    private Button? _btnRemoveGatewayProvider;
    private Label? _lblGatewayProviderDetails;
    private TableLayoutPanel? _gatewayProviderDetailsPanel;
    private TableLayoutPanel? _gatewayProviderAuthPanel;
    private TableLayoutPanel? _gatewayProviderEndpointsPanel;
    private Label? _lblGatewayProviderId;
    private TextBox? _txtGatewayProviderId;
    private Label? _lblGatewayProviderProtocol2;
    private ComboBox? _cmbGatewayProviderProtocol2;
    private Label? _lblGatewayProviderName2;
    private TextBox? _txtGatewayProviderName2;
    private Label? _lblGatewayProviderBaseUrl2;
    private TextBox? _txtGatewayProviderBaseUrl2;
    private Label? _lblGatewayProviderDefaultModel2;
    private ComboBox? _txtGatewayProviderDefaultModel2;
    private Label? _lblGatewayProviderEmbeddingModel2;
    private ComboBox? _txtGatewayProviderEmbeddingModel2;
    private Label? _lblGatewayProviderAuthType;
    private ComboBox? _cmbGatewayProviderAuthType;
    private Label? _lblGatewayProviderAuthHeader;
    private TextBox? _txtGatewayProviderAuthHeader;
    private Label? _lblGatewayProviderChatEndpoint;
    private TextBox? _txtGatewayProviderChatEndpoint;
    private Label? _lblGatewayProviderEmbeddingsEndpoint;
    private TextBox? _txtGatewayProviderEmbeddingsEndpoint;
    private Label? _lblGatewayProviderResponsesEndpoint;
    private TextBox? _txtGatewayProviderResponsesEndpoint;
    private Label? _lblGatewayProviderAdditionalHeaders;
    private TextBox? _txtGatewayProviderAdditionalHeaders;
    private FlowLayoutPanel? _gatewayProviderCapabilitiesPanel;
    private CheckBox? _chkGatewaySupportsChat;
    private CheckBox? _chkGatewaySupportsEmbeddings;
    private CheckBox? _chkGatewaySupportsResponses;
    private CheckBox? _chkGatewaySupportsStreaming;
    private Label? _lblLocalApiDiagnostics;
    private FlowLayoutPanel? _localApiLoggingPanel;
    private CheckBox? _chkLocalApiRequestResponseLogging;
    private CheckBox? _chkLocalApiIncludeBodies;
    private CheckBox? _chkLocalApiIncludeErrorDiagnostics;
    private Label? _lblLocalApiMaxBodyChars;
    private NumericUpDown? _numLocalApiMaxBodyChars;

	// Status and control
	private Button? _btnStartStop;
	private Label? _lblStatus;
	private Label? _lblStats;

    private void InitializeComponent()
    {
        components = new Container();
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
        _mainPanel = new TableLayoutPanel();
        _tabControl = new TabControl();
        _configTab = new TabPage();
        _configPanel = new TableLayoutPanel();
        _proxyHeaderPanel = new FlowLayoutPanel();
        _chkProxyEnabled = new CheckBox();
        _lblConfigName = new Label();
        _txtConfigName = new TextBox();
        _proxySettingsPanel = new TableLayoutPanel();
        _chkLocalProxy = new CheckBox();
        _lblProxyHost = new Label();
        _cmbProxyHost = new ComboBox();
        _lblProxyPort = new Label();
        _numProxyPort = new NumericUpDown();
        _lblProxyScheme = new Label();
        _cmbProxyScheme = new ComboBox();
        _lblProcesses = new Label();
        _procPanel = new TableLayoutPanel();
        _txtProcesses = new TextBox();
        _lblDomainRules = new Label();
        _domainRulesPanel = new TableLayoutPanel();
        _txtDomainRules = new TextBox();
        _startupOptionsPanel = new FlowLayoutPanel();
        _chkStartOnBoot = new CheckBox();
        _chkAutoStartProxy = new CheckBox();
        _lblSyncProvider = new Label();
        _syncProviderTokenPanel = new TableLayoutPanel();
        _cmbSyncProvider = new ComboBox();
        _lblSyncToken = new Label();
        _txtSyncToken = new TextBox();
        _btnSyncPull = new Button();
        _lblGistId = new Label();
        _gistIdActionPanel = new TableLayoutPanel();
        _txtGistId = new TextBox();
        _btnSyncPush = new Button();
        _lblConfigFile = new Label();
        _configActionPanel = new TableLayoutPanel();
        _quickConfigPanel = new FlowLayoutPanel();
        _configBtnPanel = new FlowLayoutPanel();
        btnResetConfig = new Button();
        _btnLoadConfig = new Button();
        _btnSaveConfigAs = new Button();
        _btnSaveConfig = new Button();
        _localApiTab = new TabPage();
        _localApiPanel = new TableLayoutPanel();
        _localApiHeaderPanel = new FlowLayoutPanel();
        _chkLocalApiForwarderEnabled = new CheckBox();
        _lblLocalApiHeaderHelp = new Label();
        _gatewayTabControl = new TabControl();
        _gatewayOverviewTab = new TabPage();
        _gatewayOverviewPanel = new TableLayoutPanel();
        _lblLocalApiListenPorts = new Label();
        _localApiPortPanel = new TableLayoutPanel();
        _lblOllamaPort = new Label();
        _numOllamaPort = new NumericUpDown();
        _lblFoundryPort = new Label();
        _numFoundryPort = new NumericUpDown();
        _lblGatewayProviderEnabled = new Label();
        _gatewayProviderEnabledPanel = new FlowLayoutPanel();
        _chkGatewayOpenAiEnabled = new CheckBox();
        _chkGatewayAnthropicEnabled = new CheckBox();
        _chkGatewayGeminiEnabled = new CheckBox();
        _lblGatewayOverviewSummary = new Label();
        _txtGatewayOverviewProviders = new TextBox();
        _gatewayOpenAiProviderTab = new TabPage();
        _gatewayOpenAiProviderControl = new GatewayProviderSettingsControl();
        _gatewayAnthropicProviderTab = new TabPage();
        _gatewayAnthropicProviderControl = new GatewayProviderSettingsControl();
        _gatewayGeminiProviderTab = new TabPage();
        _gatewayGeminiProviderControl = new GatewayProviderSettingsControl();
        _gatewayProviderTab = new TabPage();
        _gatewayProviderEditorPanel = new TableLayoutPanel();
        _lblGatewayProviderSelection = new Label();
        _gatewayProviderTabs = new TabControl();
        _gatewayProviderOpenAiTab = new TabPage();
        _gatewayProviderAnthropicTab = new TabPage();
        _gatewayProviderGeminiTab = new TabPage();
        _gatewayProviderButtonsPanel = new FlowLayoutPanel();
        _btnDuplicateGatewayProvider = new Button();
        _btnRemoveGatewayProvider = new Button();
        _gatewayProviderContentScrollPanel = new Panel();
        _grpGatewayProviderAdvanced = new GroupBox();
        _gatewayProviderAdvancedLayout = new TableLayoutPanel();
        _gatewayProviderDetailsPanel = new TableLayoutPanel();
        _lblGatewayProviderId = new Label();
        _txtGatewayProviderId = new TextBox();
        _lblGatewayProviderProtocol2 = new Label();
        _cmbGatewayProviderProtocol2 = new ComboBox();
        _lblGatewayProviderName2 = new Label();
        _txtGatewayProviderName2 = new TextBox();
        _lblGatewayProviderBaseUrl2 = new Label();
        _txtGatewayProviderBaseUrl2 = new TextBox();
        _lblGatewayProviderDefaultModel2 = new Label();
        _txtGatewayProviderDefaultModel2 = new ComboBox();
        _lblGatewayProviderEmbeddingModel2 = new Label();
        _txtGatewayProviderEmbeddingModel2 = new ComboBox();
        _grpGatewayProviderAuth = new GroupBox();
        _gatewayProviderAuthPanel = new TableLayoutPanel();
        _lblGatewayProviderAuthType = new Label();
        _cmbGatewayProviderAuthType = new ComboBox();
        _lblGatewayProviderAuthHeader = new Label();
        _txtGatewayProviderAuthHeader = new TextBox();
        _grpGatewayProviderEndpoints = new GroupBox();
        _gatewayProviderEndpointsPanel = new TableLayoutPanel();
        _lblGatewayProviderChatEndpoint = new Label();
        _txtGatewayProviderChatEndpoint = new TextBox();
        _lblGatewayProviderEmbeddingsEndpoint = new Label();
        _txtGatewayProviderEmbeddingsEndpoint = new TextBox();
        _lblGatewayProviderResponsesEndpoint = new Label();
        _txtGatewayProviderResponsesEndpoint = new TextBox();
        _lblGatewayProviderAdditionalHeaders = new Label();
        _txtGatewayProviderAdditionalHeaders = new TextBox();
        _grpGatewayProviderCapabilities = new GroupBox();
        _gatewayProviderCapabilitiesPanel = new FlowLayoutPanel();
        _chkGatewaySupportsChat = new CheckBox();
        _chkGatewaySupportsEmbeddings = new CheckBox();
        _chkGatewaySupportsResponses = new CheckBox();
        _chkGatewaySupportsStreaming = new CheckBox();
        _chkGatewayProviderShowAdvanced = new CheckBox();
        _grpGatewayProviderBasic = new GroupBox();
        _gatewayProviderBasicPanel = new TableLayoutPanel();
        _lblLocalApiProviderUrl = new Label();
        _localApiProviderUrlPanel = new TableLayoutPanel();
        _txtLocalApiProviderUrl = new TextBox();
        _btnRefreshLocalApiModels = new Button();
        _lblLocalApiApiKey = new Label();
        _txtLocalApiApiKey = new TextBox();
        _lblGatewayDetectedProtocol = new Label();
        _txtGatewayDetectedProtocol = new TextBox();
        _lblGatewayProviderDisplayName = new Label();
        _btnGatewayDetectProvider = new Button();
        _gatewayProviderModelActionsPanel = new FlowLayoutPanel();
        _btnGatewayRefreshProviderModels = new Button();
        _btnGatewayRefreshProviderModelsApply = new Button();
        _lblGatewayProviderModelPreview = new Label();
        _txtGatewayProviderModelPreview = new ListBox();
        _gatewayModelPreviewContextMenu = new ContextMenuStrip(components);
        _gatewaySetAsChatModelMenuItem = new ToolStripMenuItem();
        _gatewaySetAsEmbeddingModelMenuItem = new ToolStripMenuItem();
        _lblGatewayProviderModelMetadata = new Label();
        _txtGatewayProviderModelMetadata = new TextBox();
        _btnGatewayCopyModelMetadata = new Button();
        _gatewayRouteTab = new TabPage();
        _gatewayRouteEditorPanel = new TableLayoutPanel();
        _lblGatewayRouteProvider = new Label();
        _cmbGatewayRouteProvider = new ComboBox();
        _lblGatewayRouteMappings = new Label();
        _txtGatewayRouteMappings = new TextBox();
        _lblGatewayRouteValidation = new Label();
        _txtGatewayRoutesPreview = new TextBox();
        _lblGatewayRoutesPreview = new Label();
        _gatewayDiagnosticsTab = new TabPage();
        _localApiLoggingPanel = new FlowLayoutPanel();
        _chkLocalApiRequestResponseLogging = new CheckBox();
        _chkLocalApiIncludeBodies = new CheckBox();
        _chkLocalApiIncludeErrorDiagnostics = new CheckBox();
        _lblLocalApiMaxBodyChars = new Label();
        _numLocalApiMaxBodyChars = new NumericUpDown();
        _dnsRedirectTab = new TabPage();
        _dnsRedirectPanel = new TableLayoutPanel();
        _grpRedirectMode = new GroupBox();
        _modePanelRedirectMode = new FlowLayoutPanel();
        _rdoDnsInterception = new RadioButton();
        _rdoHostsFile = new RadioButton();
        _lblHostsFileWarning = new Label();
        _lblHostsUrl = new Label();
        _txtHostsUrl = new TextBox();
        _hostsRedirectBtnPanel = new FlowLayoutPanel();
        _btnRefreshHosts = new Button();
        _btnFetchIps = new Button();
        _chkRetestSlowOrTimeoutOnly = new CheckBox();
        _lblHostsStatus = new Label();
        _autoFetchPanel = new FlowLayoutPanel();
        _chkAutoFetch = new CheckBox();
        _numAutoFetchInterval = new NumericUpDown();
        _lblAutoFetchMinutes = new Label();
        _lblRefreshDomains = new Label();
        _txtRefreshDomains = new TextBox();
        _lvIpResults = new ListView();
        _logsTab = new TabPage();
        _logPanel = new TableLayoutPanel();
        _rtbLogs = new RichTextBox();
        _btnClearPanel = new FlowLayoutPanel();
        _btnClearLogs = new Button();
        _aboutTab = new TabPage();
        _aboutScrollPanel = new Panel();
        _aboutContentPanel = new TableLayoutPanel();
        titleLabel = new Label();
        _versionPanel = new FlowLayoutPanel();
        versionLabel = new Label();
        _lblLatestVersion = new Label();
        _btnCheckUpdate = new Button();
        _lblUpdateStatus = new Label();
        descLabel = new Label();
        descContentLabel = new Label();
        contribLabel = new Label();
        contribContentLabel = new Label();
        techLabel = new Label();
        techContentLabel = new Label();
        _statusPanel = new FlowLayoutPanel();
        _lblStatus = new Label();
        _lblStats = new Label();
        lblBytes = new Label();
        label1 = new Label();
        _btnStartStop = new Button();
        _btnAddGatewayProvider = new Button();
        _lblLocalApiProvider = new Label();
        _localApiProviderPanel = new TableLayoutPanel();
        _lblLocalApiProtocol = new Label();
        _cmbLocalApiProviderProtocol = new ComboBox();
        _lblLocalApiProviderName = new Label();
        _txtLocalApiProviderName = new TextBox();
        _lblLocalApiDefaultModel = new Label();
        _cmbLocalApiDefaultModel = new ComboBox();
        _lblLocalApiDefaultEmbeddingModel = new Label();
        _cmbLocalApiDefaultEmbeddingModel = new ComboBox();
        _lblLocalApiAuthentication = new Label();
        _localApiAuthPanel = new TableLayoutPanel();
        _lblLocalApiAuthType = new Label();
        _cmbLocalApiAuthType = new ComboBox();
        _lblLocalApiAuthHeaderName = new Label();
        _txtLocalApiAuthHeaderName = new TextBox();
        _lblLocalApiExtraHeaders = new Label();
        _txtLocalApiAdditionalHeaders = new TextBox();
        _lblLocalApiModelMappings = new Label();
        _txtLocalApiModelMappings = new TextBox();
        _lblGatewayProviderEditor = new Label();
        _lblGatewayProviderDetails = new Label();
        _lblGatewayRouteEditor = new Label();
        _lblLocalApiDiagnostics = new Label();
        _chkDNSRedirectEnabled = new CheckBox();
        _lblConfigFileValue = new Label();
        _lblSyncActions = new Label();
        _syncActionsPanel = new FlowLayoutPanel();
        _contextMenu = new ContextMenuStrip(components);
        _trayShowMenuItem = new ToolStripMenuItem();
        _trayHideMenuItem = new ToolStripMenuItem();
        _trayTopSeparator = new ToolStripSeparator();
        _trayStartProxyMenuItem = new ToolStripMenuItem();
        _trayStopProxyMenuItem = new ToolStripMenuItem();
        _trayMiddleSeparator = new ToolStripSeparator();
        _trayOptionsMenuItem = new ToolStripMenuItem();
        _trayStartOnBootMenuItem = new ToolStripMenuItem();
        _trayAutoStartProxyMenuItem = new ToolStripMenuItem();
        _trayConfigMenuItem = new ToolStripMenuItem();
        _trayLoadConfigMenuItem = new ToolStripMenuItem();
        _traySaveConfigAsMenuItem = new ToolStripMenuItem();
        _traySaveConfigMenuItem = new ToolStripMenuItem();
        _trayConfigSeparator = new ToolStripSeparator();
        _trayBottomSeparator = new ToolStripSeparator();
        _trayExitMenuItem = new ToolStripMenuItem();
        _notifyIcon = new NotifyIcon(components);
        _mainPanel.SuspendLayout();
        _tabControl.SuspendLayout();
        _configTab.SuspendLayout();
        _configPanel.SuspendLayout();
        _proxyHeaderPanel.SuspendLayout();
        _proxySettingsPanel.SuspendLayout();
        ((ISupportInitialize)_numProxyPort).BeginInit();
        _procPanel.SuspendLayout();
        _domainRulesPanel.SuspendLayout();
        _startupOptionsPanel.SuspendLayout();
        _syncProviderTokenPanel.SuspendLayout();
        _gistIdActionPanel.SuspendLayout();
        _configActionPanel.SuspendLayout();
        _configBtnPanel.SuspendLayout();
        _localApiTab.SuspendLayout();
        _localApiPanel.SuspendLayout();
        _localApiHeaderPanel.SuspendLayout();
        _gatewayTabControl.SuspendLayout();
        _gatewayOverviewTab.SuspendLayout();
        _gatewayOverviewPanel.SuspendLayout();
        _localApiPortPanel.SuspendLayout();
        ((ISupportInitialize)_numOllamaPort).BeginInit();
        ((ISupportInitialize)_numFoundryPort).BeginInit();
        _gatewayProviderTab.SuspendLayout();
        _gatewayProviderEditorPanel.SuspendLayout();
        _gatewayProviderTabs.SuspendLayout();
        _gatewayProviderButtonsPanel.SuspendLayout();
        _gatewayProviderContentScrollPanel.SuspendLayout();
        _grpGatewayProviderAdvanced.SuspendLayout();
        _gatewayProviderAdvancedLayout.SuspendLayout();
        _gatewayProviderDetailsPanel.SuspendLayout();
        _grpGatewayProviderAuth.SuspendLayout();
        _gatewayProviderAuthPanel.SuspendLayout();
        _grpGatewayProviderEndpoints.SuspendLayout();
        _gatewayProviderEndpointsPanel.SuspendLayout();
        _grpGatewayProviderCapabilities.SuspendLayout();
        _gatewayProviderCapabilitiesPanel.SuspendLayout();
        _grpGatewayProviderBasic.SuspendLayout();
        _gatewayProviderBasicPanel.SuspendLayout();
        _localApiProviderUrlPanel.SuspendLayout();
        _gatewayProviderModelActionsPanel.SuspendLayout();
        _gatewayModelPreviewContextMenu.SuspendLayout();
        _gatewayRouteTab.SuspendLayout();
        _gatewayRouteEditorPanel.SuspendLayout();
        _gatewayDiagnosticsTab.SuspendLayout();
        _localApiLoggingPanel.SuspendLayout();
        ((ISupportInitialize)_numLocalApiMaxBodyChars).BeginInit();
        _dnsRedirectTab.SuspendLayout();
        _dnsRedirectPanel.SuspendLayout();
        _grpRedirectMode.SuspendLayout();
        _modePanelRedirectMode.SuspendLayout();
        _hostsRedirectBtnPanel.SuspendLayout();
        _autoFetchPanel.SuspendLayout();
        ((ISupportInitialize)_numAutoFetchInterval).BeginInit();
        _logsTab.SuspendLayout();
        _logPanel.SuspendLayout();
        _btnClearPanel.SuspendLayout();
        _aboutTab.SuspendLayout();
        _aboutScrollPanel.SuspendLayout();
        _aboutContentPanel.SuspendLayout();
        _versionPanel.SuspendLayout();
        _statusPanel.SuspendLayout();
        _localApiProviderPanel.SuspendLayout();
        _localApiAuthPanel.SuspendLayout();
        _contextMenu.SuspendLayout();
        SuspendLayout();
        // 
        // _mainPanel
        // 
        _mainPanel.ColumnCount = 1;
        _mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mainPanel.Controls.Add(_tabControl, 0, 0);
        _mainPanel.Controls.Add(_statusPanel, 0, 1);
        _mainPanel.Dock = DockStyle.Fill;
        _mainPanel.Location = new Point(0, 0);
        _mainPanel.Margin = new Padding(0);
        _mainPanel.Name = "_mainPanel";
        _mainPanel.Padding = new Padding(5);
        _mainPanel.RowCount = 2;
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        _mainPanel.Size = new Size(787, 691);
        _mainPanel.TabIndex = 0;
        // 
        // _tabControl
        // 
        _tabControl.Controls.Add(_configTab);
        _tabControl.Controls.Add(_localApiTab);
        _tabControl.Controls.Add(_dnsRedirectTab);
        _tabControl.Controls.Add(_logsTab);
        _tabControl.Controls.Add(_aboutTab);
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Location = new Point(5, 5);
        _tabControl.Margin = new Padding(0, 0, 0, 5);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new Size(777, 626);
        _tabControl.TabIndex = 0;
        // 
        // _configTab
        // 
        _configTab.Controls.Add(_configPanel);
        _configTab.Location = new Point(4, 26);
        _configTab.Name = "_configTab";
        _configTab.Size = new Size(769, 596);
        _configTab.TabIndex = 0;
        _configTab.Text = "Configuration";
        // 
        // _configPanel
        // 
        _configPanel.ColumnCount = 2;
        _configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        _configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configPanel.Controls.Add(_proxyHeaderPanel, 0, 0);
        _configPanel.Controls.Add(_proxySettingsPanel, 0, 1);
        _configPanel.Controls.Add(_lblProcesses, 0, 2);
        _configPanel.Controls.Add(_procPanel, 1, 2);
        _configPanel.Controls.Add(_lblDomainRules, 0, 3);
        _configPanel.Controls.Add(_domainRulesPanel, 1, 3);
        _configPanel.Controls.Add(_startupOptionsPanel, 0, 4);
        _configPanel.Controls.Add(_lblSyncProvider, 0, 5);
        _configPanel.Controls.Add(_syncProviderTokenPanel, 1, 5);
        _configPanel.Controls.Add(_lblGistId, 0, 6);
        _configPanel.Controls.Add(_gistIdActionPanel, 1, 6);
        _configPanel.Controls.Add(_lblConfigFile, 0, 7);
        _configPanel.Controls.Add(_configActionPanel, 1, 7);
        _configPanel.Dock = DockStyle.Fill;
        _configPanel.Location = new Point(0, 0);
        _configPanel.Name = "_configPanel";
        _configPanel.Padding = new Padding(10);
        _configPanel.RowCount = 8;
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 143F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        _configPanel.Size = new Size(769, 596);
        _configPanel.TabIndex = 0;
        // 
        // _proxyHeaderPanel
        // 
        _proxyHeaderPanel.AutoSize = true;
        _configPanel.SetColumnSpan(_proxyHeaderPanel, 2);
        _proxyHeaderPanel.Controls.Add(_chkProxyEnabled);
        _proxyHeaderPanel.Controls.Add(_lblConfigName);
        _proxyHeaderPanel.Controls.Add(_txtConfigName);
        _proxyHeaderPanel.Dock = DockStyle.Top;
        _proxyHeaderPanel.Location = new Point(13, 13);
        _proxyHeaderPanel.Name = "_proxyHeaderPanel";
        _proxyHeaderPanel.Size = new Size(743, 29);
        _proxyHeaderPanel.TabIndex = 0;
        _proxyHeaderPanel.WrapContents = false;
        // 
        // _chkProxyEnabled
        // 
        _chkProxyEnabled.AutoSize = true;
        _chkProxyEnabled.Checked = true;
        _chkProxyEnabled.CheckState = CheckState.Checked;
        _chkProxyEnabled.Location = new Point(3, 5);
        _chkProxyEnabled.Margin = new Padding(3, 5, 18, 3);
        _chkProxyEnabled.Name = "_chkProxyEnabled";
        _chkProxyEnabled.Size = new Size(222, 21);
        _chkProxyEnabled.TabIndex = 0;
        _chkProxyEnabled.Text = "Enable Proxy (TCP traffic redirect)";
        // 
        // _lblConfigName
        // 
        _lblConfigName.Anchor = AnchorStyles.Left;
        _lblConfigName.AutoSize = true;
        _lblConfigName.Location = new Point(246, 6);
        _lblConfigName.Name = "_lblConfigName";
        _lblConfigName.Size = new Size(88, 17);
        _lblConfigName.TabIndex = 1;
        _lblConfigName.Text = "Config Name:";
        // 
        // _txtConfigName
        // 
        _txtConfigName.Location = new Point(340, 3);
        _txtConfigName.Name = "_txtConfigName";
        _txtConfigName.PlaceholderText = "Default";
        _txtConfigName.Size = new Size(220, 23);
        _txtConfigName.TabIndex = 2;
        // 
        // _proxySettingsPanel
        // 
        _proxySettingsPanel.ColumnCount = 7;
        _configPanel.SetColumnSpan(_proxySettingsPanel, 2);
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68F));
        _proxySettingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
        _proxySettingsPanel.Controls.Add(_chkLocalProxy, 0, 0);
        _proxySettingsPanel.Controls.Add(_lblProxyHost, 1, 0);
        _proxySettingsPanel.Controls.Add(_cmbProxyHost, 2, 0);
        _proxySettingsPanel.Controls.Add(_lblProxyPort, 3, 0);
        _proxySettingsPanel.Controls.Add(_numProxyPort, 4, 0);
        _proxySettingsPanel.Controls.Add(_lblProxyScheme, 5, 0);
        _proxySettingsPanel.Controls.Add(_cmbProxyScheme, 6, 0);
        _proxySettingsPanel.Dock = DockStyle.Fill;
        _proxySettingsPanel.Location = new Point(13, 45);
        _proxySettingsPanel.Margin = new Padding(3, 0, 3, 3);
        _proxySettingsPanel.Name = "_proxySettingsPanel";
        _proxySettingsPanel.RowCount = 1;
        _proxySettingsPanel.RowStyles.Add(new RowStyle());
        _proxySettingsPanel.Size = new Size(743, 33);
        _proxySettingsPanel.TabIndex = 1;
        // 
        // _chkLocalProxy
        // 
        _chkLocalProxy.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _chkLocalProxy.Location = new Point(3, 8);
        _chkLocalProxy.Margin = new Padding(3, 6, 3, 3);
        _chkLocalProxy.Name = "_chkLocalProxy";
        _chkLocalProxy.Size = new Size(89, 21);
        _chkLocalProxy.TabIndex = 0;
        _chkLocalProxy.Text = "Local Proxy";
        _chkLocalProxy.CheckedChanged += ChkLocalProxy_CheckedChanged;
        // 
        // _lblProxyHost
        // 
        _lblProxyHost.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyHost.Location = new Point(98, 6);
        _lblProxyHost.Name = "_lblProxyHost";
        _lblProxyHost.Size = new Size(39, 23);
        _lblProxyHost.TabIndex = 1;
        _lblProxyHost.Text = "Host:";
        _lblProxyHost.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbProxyHost
        // 
        _cmbProxyHost.Dock = DockStyle.Fill;
        _cmbProxyHost.Location = new Point(143, 5);
        _cmbProxyHost.Margin = new Padding(3, 5, 3, 5);
        _cmbProxyHost.Name = "_cmbProxyHost";
        _cmbProxyHost.Size = new Size(283, 25);
        _cmbProxyHost.TabIndex = 2;
        // 
        // _lblProxyPort
        // 
        _lblProxyPort.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyPort.Location = new Point(432, 6);
        _lblProxyPort.Name = "_lblProxyPort";
        _lblProxyPort.Size = new Size(44, 23);
        _lblProxyPort.TabIndex = 3;
        _lblProxyPort.Text = "Port:";
        _lblProxyPort.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _numProxyPort
        // 
        _numProxyPort.Dock = DockStyle.Fill;
        _numProxyPort.Location = new Point(482, 5);
        _numProxyPort.Margin = new Padding(3, 5, 3, 5);
        _numProxyPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _numProxyPort.Name = "_numProxyPort";
        _numProxyPort.Size = new Size(72, 23);
        _numProxyPort.TabIndex = 4;
        _numProxyPort.Value = new decimal(new int[] { 7890, 0, 0, 0 });
        // 
        // _lblProxyScheme
        // 
        _lblProxyScheme.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyScheme.Location = new Point(560, 6);
        _lblProxyScheme.Name = "_lblProxyScheme";
        _lblProxyScheme.Size = new Size(62, 23);
        _lblProxyScheme.TabIndex = 5;
        _lblProxyScheme.Text = "Scheme:";
        _lblProxyScheme.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbProxyScheme
        // 
        _cmbProxyScheme.Dock = DockStyle.Fill;
        _cmbProxyScheme.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbProxyScheme.Items.AddRange(new object[] { "socks5", "socks4", "http", "https" });
        _cmbProxyScheme.Location = new Point(628, 5);
        _cmbProxyScheme.Margin = new Padding(3, 5, 5, 5);
        _cmbProxyScheme.Name = "_cmbProxyScheme";
        _cmbProxyScheme.Size = new Size(110, 25);
        _cmbProxyScheme.TabIndex = 6;
        // 
        // _lblProcesses
        // 
        _lblProcesses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblProcesses.Location = new Point(13, 81);
        _lblProcesses.Name = "_lblProcesses";
        _lblProcesses.Size = new Size(102, 23);
        _lblProcesses.TabIndex = 6;
        _lblProcesses.Text = "Process Name List:";
        _lblProcesses.TextAlign = ContentAlignment.TopRight;
        // 
        // _procPanel
        // 
        _procPanel.ColumnCount = 1;
        _procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _procPanel.Controls.Add(_txtProcesses, 0, 0);
        _procPanel.Dock = DockStyle.Fill;
        _procPanel.Location = new Point(123, 86);
        _procPanel.Margin = new Padding(5);
        _procPanel.Name = "_procPanel";
        _procPanel.RowCount = 1;
        _procPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _procPanel.Size = new Size(631, 122);
        _procPanel.TabIndex = 7;
        // 
        // _txtProcesses
        // 
        _txtProcesses.AcceptsReturn = true;
        _txtProcesses.AcceptsTab = true;
        _txtProcesses.Dock = DockStyle.Fill;
        _txtProcesses.Location = new Point(3, 3);
        _txtProcesses.Multiline = true;
        _txtProcesses.Name = "_txtProcesses";
        _txtProcesses.PlaceholderText = "\r\ndevenv.exe\r\nservicehub*.exe";
        _txtProcesses.ScrollBars = ScrollBars.Vertical;
        _txtProcesses.Size = new Size(625, 116);
        _txtProcesses.TabIndex = 0;
        _txtProcesses.WordWrap = false;
        // 
        // _lblDomainRules
        // 
        _lblDomainRules.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblDomainRules.Location = new Point(13, 213);
        _lblDomainRules.Name = "_lblDomainRules";
        _lblDomainRules.Size = new Size(102, 23);
        _lblDomainRules.TabIndex = 8;
        _lblDomainRules.Text = "Domain Rule List:";
        _lblDomainRules.TextAlign = ContentAlignment.TopRight;
        // 
        // _domainRulesPanel
        // 
        _domainRulesPanel.ColumnCount = 1;
        _domainRulesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _domainRulesPanel.Controls.Add(_txtDomainRules, 0, 0);
        _domainRulesPanel.Dock = DockStyle.Fill;
        _domainRulesPanel.Location = new Point(123, 218);
        _domainRulesPanel.Margin = new Padding(5);
        _domainRulesPanel.Name = "_domainRulesPanel";
        _domainRulesPanel.RowCount = 1;
        _domainRulesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _domainRulesPanel.Size = new Size(631, 133);
        _domainRulesPanel.TabIndex = 9;
        // 
        // _txtDomainRules
        // 
        _txtDomainRules.AcceptsReturn = true;
        _txtDomainRules.AcceptsTab = true;
        _txtDomainRules.Dock = DockStyle.Fill;
        _txtDomainRules.Location = new Point(3, 3);
        _txtDomainRules.Multiline = true;
        _txtDomainRules.Name = "_txtDomainRules";
        _txtDomainRules.PlaceholderText = ":\r\n*.github.com\r\nraw.githubusercontent.com";
        _txtDomainRules.ScrollBars = ScrollBars.Vertical;
        _txtDomainRules.Size = new Size(625, 127);
        _txtDomainRules.TabIndex = 0;
        _txtDomainRules.WordWrap = false;
        // 
        // _startupOptionsPanel
        // 
        _startupOptionsPanel.AutoSize = true;
        _configPanel.SetColumnSpan(_startupOptionsPanel, 2);
        _startupOptionsPanel.Controls.Add(_chkStartOnBoot);
        _startupOptionsPanel.Controls.Add(_chkAutoStartProxy);
        _startupOptionsPanel.Dock = DockStyle.Top;
        _startupOptionsPanel.Location = new Point(13, 359);
        _startupOptionsPanel.Name = "_startupOptionsPanel";
        _startupOptionsPanel.Size = new Size(743, 27);
        _startupOptionsPanel.TabIndex = 17;
        _startupOptionsPanel.WrapContents = false;
        // 
        // _chkStartOnBoot
        // 
        _chkStartOnBoot.AutoSize = true;
        _chkStartOnBoot.Location = new Point(3, 3);
        _chkStartOnBoot.Margin = new Padding(3, 3, 18, 3);
        _chkStartOnBoot.Name = "_chkStartOnBoot";
        _chkStartOnBoot.Size = new Size(175, 21);
        _chkStartOnBoot.TabIndex = 0;
        _chkStartOnBoot.Text = "Start on Windows startup";
        _chkStartOnBoot.CheckedChanged += ChkStartOnBoot_CheckedChanged;
        // 
        // _chkAutoStartProxy
        // 
        _chkAutoStartProxy.AutoSize = true;
        _chkAutoStartProxy.Location = new Point(199, 3);
        _chkAutoStartProxy.Name = "_chkAutoStartProxy";
        _chkAutoStartProxy.Size = new Size(162, 21);
        _chkAutoStartProxy.TabIndex = 1;
        _chkAutoStartProxy.Text = "Start Proxy after launch";
        _chkAutoStartProxy.CheckedChanged += ChkAutoStartProxy_CheckedChanged;
        // 
        // _lblSyncProvider
        // 
        _lblSyncProvider.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblSyncProvider.Location = new Point(13, 400);
        _lblSyncProvider.Name = "_lblSyncProvider";
        _lblSyncProvider.Size = new Size(102, 23);
        _lblSyncProvider.TabIndex = 16;
        _lblSyncProvider.Text = "Sync Provider:";
        _lblSyncProvider.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _syncProviderTokenPanel
        // 
        _syncProviderTokenPanel.ColumnCount = 4;
        _syncProviderTokenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 87F));
        _syncProviderTokenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.4117641F));
        _syncProviderTokenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78.5882339F));
        _syncProviderTokenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        _syncProviderTokenPanel.Controls.Add(_cmbSyncProvider, 0, 0);
        _syncProviderTokenPanel.Controls.Add(_lblSyncToken, 1, 0);
        _syncProviderTokenPanel.Controls.Add(_txtSyncToken, 2, 0);
        _syncProviderTokenPanel.Controls.Add(_btnSyncPull, 3, 0);
        _syncProviderTokenPanel.Dock = DockStyle.Fill;
        _syncProviderTokenPanel.Location = new Point(123, 395);
        _syncProviderTokenPanel.Margin = new Padding(5);
        _syncProviderTokenPanel.Name = "_syncProviderTokenPanel";
        _syncProviderTokenPanel.RowCount = 1;
        _syncProviderTokenPanel.RowStyles.Add(new RowStyle());
        _syncProviderTokenPanel.Size = new Size(631, 34);
        _syncProviderTokenPanel.TabIndex = 17;
        // 
        // _cmbSyncProvider
        // 
        _cmbSyncProvider.Dock = DockStyle.Fill;
        _cmbSyncProvider.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSyncProvider.Items.AddRange(new object[] { "GitHub", "Gitee" });
        _cmbSyncProvider.Location = new Point(3, 5);
        _cmbSyncProvider.Margin = new Padding(3, 5, 3, 5);
        _cmbSyncProvider.Name = "_cmbSyncProvider";
        _cmbSyncProvider.Size = new Size(81, 25);
        _cmbSyncProvider.TabIndex = 17;
        // 
        // _lblSyncToken
        // 
        _lblSyncToken.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblSyncToken.Location = new Point(90, 6);
        _lblSyncToken.Name = "_lblSyncToken";
        _lblSyncToken.Size = new Size(87, 23);
        _lblSyncToken.TabIndex = 18;
        _lblSyncToken.Text = "Sync Token:";
        _lblSyncToken.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // _txtSyncToken
        // 
        _txtSyncToken.Dock = DockStyle.Fill;
        _txtSyncToken.Location = new Point(183, 5);
        _txtSyncToken.Margin = new Padding(3, 5, 3, 5);
        _txtSyncToken.Name = "_txtSyncToken";
        _txtSyncToken.PasswordChar = '●';
        _txtSyncToken.PlaceholderText = "Enter GitHub or Gitee personal access token";
        _txtSyncToken.Size = new Size(339, 23);
        _txtSyncToken.TabIndex = 19;
        // 
        // _btnSyncPull
        // 
        _btnSyncPull.Dock = DockStyle.Fill;
        _btnSyncPull.Location = new Point(528, 2);
        _btnSyncPull.Margin = new Padding(3, 2, 3, 2);
        _btnSyncPull.Name = "_btnSyncPull";
        _btnSyncPull.Size = new Size(100, 31);
        _btnSyncPull.TabIndex = 20;
        _btnSyncPull.Text = "⬇ Pull Config";
        _btnSyncPull.UseVisualStyleBackColor = true;
        _btnSyncPull.Click += BtnSyncPull_Click;
        // 
        // _lblGistId
        // 
        _lblGistId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGistId.Location = new Point(13, 444);
        _lblGistId.Name = "_lblGistId";
        _lblGistId.Size = new Size(102, 23);
        _lblGistId.TabIndex = 21;
        _lblGistId.Text = "Gist / Snippet ID:";
        _lblGistId.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _gistIdActionPanel
        // 
        _gistIdActionPanel.ColumnCount = 2;
        _gistIdActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gistIdActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        _gistIdActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        _gistIdActionPanel.Controls.Add(_txtGistId, 0, 0);
        _gistIdActionPanel.Controls.Add(_btnSyncPush, 1, 0);
        _gistIdActionPanel.Dock = DockStyle.Fill;
        _gistIdActionPanel.Location = new Point(123, 439);
        _gistIdActionPanel.Margin = new Padding(5);
        _gistIdActionPanel.Name = "_gistIdActionPanel";
        _gistIdActionPanel.RowCount = 1;
        _gistIdActionPanel.RowStyles.Add(new RowStyle());
        _gistIdActionPanel.Size = new Size(631, 34);
        _gistIdActionPanel.TabIndex = 18;
        // 
        // _txtGistId
        // 
        _txtGistId.Dock = DockStyle.Fill;
        _txtGistId.Location = new Point(3, 5);
        _txtGistId.Margin = new Padding(3, 5, 3, 5);
        _txtGistId.Name = "_txtGistId";
        _txtGistId.PlaceholderText = "Optional – auto-discovered or filled automatically after first push";
        _txtGistId.Size = new Size(520, 23);
        _txtGistId.TabIndex = 22;
        // 
        // _btnSyncPush
        // 
        _btnSyncPush.Dock = DockStyle.Fill;
        _btnSyncPush.Location = new Point(529, 2);
        _btnSyncPush.Margin = new Padding(3, 2, 3, 2);
        _btnSyncPush.Name = "_btnSyncPush";
        _btnSyncPush.Size = new Size(99, 30);
        _btnSyncPush.TabIndex = 23;
        _btnSyncPush.Text = "⬆ Push Config";
        _btnSyncPush.UseVisualStyleBackColor = true;
        _btnSyncPush.Click += BtnSyncPush_Click;
        // 
        // _lblConfigFile
        // 
        _lblConfigFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblConfigFile.Location = new Point(13, 520);
        _lblConfigFile.Name = "_lblConfigFile";
        _lblConfigFile.Size = new Size(102, 23);
        _lblConfigFile.TabIndex = 14;
        _lblConfigFile.Text = "Config File:";
        _lblConfigFile.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _configActionPanel
        // 
        _configActionPanel.AutoSize = true;
        _configActionPanel.ColumnCount = 2;
        _configActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configActionPanel.ColumnStyles.Add(new ColumnStyle());
        _configActionPanel.Controls.Add(_quickConfigPanel, 0, 0);
        _configActionPanel.Controls.Add(_configBtnPanel, 1, 0);
        _configActionPanel.Dock = DockStyle.Fill;
        _configActionPanel.Location = new Point(121, 478);
        _configActionPanel.Margin = new Padding(3, 0, 3, 3);
        _configActionPanel.Name = "_configActionPanel";
        _configActionPanel.RowCount = 1;
        _configActionPanel.RowStyles.Add(new RowStyle());
        _configActionPanel.Size = new Size(635, 105);
        _configActionPanel.TabIndex = 18;
        // 
        // _quickConfigPanel
        // 
        _quickConfigPanel.AutoSize = true;
        _quickConfigPanel.Dock = DockStyle.Fill;
        _quickConfigPanel.Location = new Point(0, 0);
        _quickConfigPanel.Margin = new Padding(0);
        _quickConfigPanel.Name = "_quickConfigPanel";
        _quickConfigPanel.Size = new Size(347, 105);
        _quickConfigPanel.TabIndex = 0;
        _quickConfigPanel.WrapContents = false;
        // 
        // _configBtnPanel
        // 
        _configBtnPanel.AutoSize = true;
        _configBtnPanel.Controls.Add(btnResetConfig);
        _configBtnPanel.Controls.Add(_btnLoadConfig);
        _configBtnPanel.Controls.Add(_btnSaveConfigAs);
        _configBtnPanel.Controls.Add(_btnSaveConfig);
        _configBtnPanel.Dock = DockStyle.Top;
        _configBtnPanel.FlowDirection = FlowDirection.RightToLeft;
        _configBtnPanel.Location = new Point(352, 5);
        _configBtnPanel.Margin = new Padding(5);
        _configBtnPanel.Name = "_configBtnPanel";
        _configBtnPanel.Size = new Size(278, 36);
        _configBtnPanel.TabIndex = 18;
        _configBtnPanel.WrapContents = false;
        // 
        // btnResetConfig
        // 
        btnResetConfig.Location = new Point(210, 3);
        btnResetConfig.Name = "btnResetConfig";
        btnResetConfig.Size = new Size(65, 30);
        btnResetConfig.TabIndex = 4;
        btnResetConfig.Text = "Reset";
        btnResetConfig.UseVisualStyleBackColor = true;
        btnResetConfig.Click += BtnResetConfig_Click;
        // 
        // _btnLoadConfig
        // 
        _btnLoadConfig.Location = new Point(140, 2);
        _btnLoadConfig.Margin = new Padding(2);
        _btnLoadConfig.Name = "_btnLoadConfig";
        _btnLoadConfig.Size = new Size(65, 30);
        _btnLoadConfig.TabIndex = 1;
        _btnLoadConfig.Text = "Load";
        _btnLoadConfig.Click += BtnLoadConfig_Click;
        // 
        // _btnSaveConfigAs
        // 
        _btnSaveConfigAs.Location = new Point(71, 2);
        _btnSaveConfigAs.Margin = new Padding(2);
        _btnSaveConfigAs.Name = "_btnSaveConfigAs";
        _btnSaveConfigAs.Size = new Size(65, 30);
        _btnSaveConfigAs.TabIndex = 2;
        _btnSaveConfigAs.Text = "Save As";
        _btnSaveConfigAs.Click += BtnSaveConfigAs_Click;
        // 
        // _btnSaveConfig
        // 
        _btnSaveConfig.Location = new Point(2, 2);
        _btnSaveConfig.Margin = new Padding(2);
        _btnSaveConfig.Name = "_btnSaveConfig";
        _btnSaveConfig.Size = new Size(65, 30);
        _btnSaveConfig.TabIndex = 3;
        _btnSaveConfig.Text = "Save";
        _btnSaveConfig.Click += BtnSaveConfig_Click;
        // 
        // _localApiTab
        // 
        _localApiTab.Controls.Add(_localApiPanel);
        _localApiTab.Location = new Point(4, 26);
        _localApiTab.Name = "_localApiTab";
        _localApiTab.Size = new Size(769, 596);
        _localApiTab.TabIndex = 2;
        _localApiTab.Text = "Ollama Gateway";
        // 
        // _localApiPanel
        // 
        _localApiPanel.ColumnCount = 2;
        _localApiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        _localApiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _localApiPanel.Controls.Add(_localApiHeaderPanel, 0, 0);
        _localApiPanel.Controls.Add(_gatewayTabControl, 0, 1);
        _localApiPanel.Dock = DockStyle.Fill;
        _localApiPanel.Location = new Point(0, 0);
        _localApiPanel.Name = "_localApiPanel";
        _localApiPanel.Padding = new Padding(10);
        _localApiPanel.RowCount = 2;
        _localApiPanel.RowStyles.Add(new RowStyle());
        _localApiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _localApiPanel.Size = new Size(769, 596);
        _localApiPanel.TabIndex = 0;
        // 
        // _localApiHeaderPanel
        // 
        _localApiHeaderPanel.AutoSize = true;
        _localApiPanel.SetColumnSpan(_localApiHeaderPanel, 2);
        _localApiHeaderPanel.Controls.Add(_chkLocalApiForwarderEnabled);
        _localApiHeaderPanel.Controls.Add(_lblLocalApiHeaderHelp);
        _localApiHeaderPanel.Dock = DockStyle.Top;
        _localApiHeaderPanel.Location = new Point(13, 13);
        _localApiHeaderPanel.Name = "_localApiHeaderPanel";
        _localApiHeaderPanel.Size = new Size(743, 29);
        _localApiHeaderPanel.TabIndex = 0;
        _localApiHeaderPanel.WrapContents = false;
        // 
        // _chkLocalApiForwarderEnabled
        // 
        _chkLocalApiForwarderEnabled.AutoSize = true;
        _chkLocalApiForwarderEnabled.Location = new Point(3, 5);
        _chkLocalApiForwarderEnabled.Margin = new Padding(3, 5, 12, 3);
        _chkLocalApiForwarderEnabled.Name = "_chkLocalApiForwarderEnabled";
        _chkLocalApiForwarderEnabled.Size = new Size(195, 21);
        _chkLocalApiForwarderEnabled.TabIndex = 0;
        _chkLocalApiForwarderEnabled.Text = "Enable local Ollama Gateway";
        // 
        // _lblLocalApiHeaderHelp
        // 
        _lblLocalApiHeaderHelp.Anchor = AnchorStyles.Left;
        _lblLocalApiHeaderHelp.AutoSize = true;
        _lblLocalApiHeaderHelp.Location = new Point(213, 6);
        _lblLocalApiHeaderHelp.Name = "_lblLocalApiHeaderHelp";
        _lblLocalApiHeaderHelp.Size = new Size(380, 17);
        _lblLocalApiHeaderHelp.TabIndex = 1;
        _lblLocalApiHeaderHelp.Text = "TrafficPilot keeps gateway listeners on loopback only for safety.";
        // 
        // _gatewayTabControl
        // 
        _localApiPanel.SetColumnSpan(_gatewayTabControl, 2);
        _gatewayTabControl.Controls.Add(_gatewayOverviewTab);
        _gatewayTabControl.Controls.Add(_gatewayOpenAiProviderTab);
        _gatewayTabControl.Controls.Add(_gatewayAnthropicProviderTab);
        _gatewayTabControl.Controls.Add(_gatewayGeminiProviderTab);
        _gatewayTabControl.Controls.Add(_gatewayRouteTab);
        _gatewayTabControl.Controls.Add(_gatewayDiagnosticsTab);
        _gatewayTabControl.Dock = DockStyle.Fill;
        _gatewayTabControl.Location = new Point(13, 48);
        _gatewayTabControl.Name = "_gatewayTabControl";
        _gatewayTabControl.SelectedIndex = 0;
        _gatewayTabControl.Size = new Size(743, 535);
        _gatewayTabControl.TabIndex = 2;
        // 
        // _gatewayOverviewTab
        // 
        _gatewayOverviewTab.Controls.Add(_gatewayOverviewPanel);
        _gatewayOverviewTab.Location = new Point(4, 26);
        _gatewayOverviewTab.Name = "_gatewayOverviewTab";
        _gatewayOverviewTab.Padding = new Padding(8);
        _gatewayOverviewTab.Size = new Size(735, 505);
        _gatewayOverviewTab.TabIndex = 0;
        _gatewayOverviewTab.Text = "Overview";
        // 
        // _gatewayOverviewPanel
        // 
        _gatewayOverviewPanel.ColumnCount = 2;
        _gatewayOverviewPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _gatewayOverviewPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gatewayOverviewPanel.Controls.Add(_lblLocalApiListenPorts, 0, 0);
        _gatewayOverviewPanel.Controls.Add(_localApiPortPanel, 1, 0);
        _gatewayOverviewPanel.Controls.Add(_lblGatewayProviderEnabled, 0, 1);
        _gatewayOverviewPanel.Controls.Add(_gatewayProviderEnabledPanel, 1, 1);
        _gatewayOverviewPanel.Controls.Add(_lblGatewayOverviewSummary, 0, 2);
        _gatewayOverviewPanel.Controls.Add(_txtGatewayOverviewProviders, 1, 2);
        _gatewayOverviewPanel.Dock = DockStyle.Fill;
        _gatewayOverviewPanel.Location = new Point(8, 8);
        _gatewayOverviewPanel.Name = "_gatewayOverviewPanel";
        _gatewayOverviewPanel.RowCount = 3;
        _gatewayOverviewPanel.RowStyles.Add(new RowStyle());
        _gatewayOverviewPanel.RowStyles.Add(new RowStyle());
        _gatewayOverviewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _gatewayOverviewPanel.Size = new Size(719, 489);
        _gatewayOverviewPanel.TabIndex = 0;
        // 
        // _lblLocalApiListenPorts
        // 
        _lblLocalApiListenPorts.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiListenPorts.Location = new Point(3, 9);
        _lblLocalApiListenPorts.Name = "_lblLocalApiListenPorts";
        _lblLocalApiListenPorts.Size = new Size(144, 23);
        _lblLocalApiListenPorts.TabIndex = 1;
        _lblLocalApiListenPorts.Text = "Listen Ports:";
        _lblLocalApiListenPorts.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _localApiPortPanel
        // 
        _localApiPortPanel.ColumnCount = 4;
        _localApiPortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _localApiPortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _localApiPortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        _localApiPortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _localApiPortPanel.Controls.Add(_lblOllamaPort, 0, 0);
        _localApiPortPanel.Controls.Add(_numOllamaPort, 1, 0);
        _localApiPortPanel.Controls.Add(_lblFoundryPort, 2, 0);
        _localApiPortPanel.Controls.Add(_numFoundryPort, 3, 0);
        _localApiPortPanel.Dock = DockStyle.Fill;
        _localApiPortPanel.Location = new Point(153, 3);
        _localApiPortPanel.Name = "_localApiPortPanel";
        _localApiPortPanel.RowCount = 1;
        _localApiPortPanel.RowStyles.Add(new RowStyle());
        _localApiPortPanel.Size = new Size(563, 36);
        _localApiPortPanel.TabIndex = 2;
        // 
        // _lblOllamaPort
        // 
        _lblOllamaPort.Dock = DockStyle.Fill;
        _lblOllamaPort.Location = new Point(3, 0);
        _lblOllamaPort.Name = "_lblOllamaPort";
        _lblOllamaPort.Size = new Size(104, 36);
        _lblOllamaPort.TabIndex = 0;
        _lblOllamaPort.Text = "Ollama Port:";
        _lblOllamaPort.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _numOllamaPort
        // 
        _numOllamaPort.Dock = DockStyle.Fill;
        _numOllamaPort.Location = new Point(113, 6);
        _numOllamaPort.Margin = new Padding(3, 6, 3, 6);
        _numOllamaPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _numOllamaPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _numOllamaPort.Name = "_numOllamaPort";
        _numOllamaPort.Size = new Size(94, 23);
        _numOllamaPort.TabIndex = 1;
        _numOllamaPort.Value = new decimal(new int[] { 11434, 0, 0, 0 });
        // 
        // _lblFoundryPort
        // 
        _lblFoundryPort.Dock = DockStyle.Fill;
        _lblFoundryPort.Location = new Point(213, 0);
        _lblFoundryPort.Name = "_lblFoundryPort";
        _lblFoundryPort.Size = new Size(119, 36);
        _lblFoundryPort.TabIndex = 2;
        _lblFoundryPort.Text = "Foundry Port:";
        _lblFoundryPort.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _numFoundryPort
        // 
        _numFoundryPort.Dock = DockStyle.Fill;
        _numFoundryPort.Location = new Point(338, 6);
        _numFoundryPort.Margin = new Padding(3, 6, 3, 6);
        _numFoundryPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _numFoundryPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _numFoundryPort.Name = "_numFoundryPort";
        _numFoundryPort.Size = new Size(222, 23);
        _numFoundryPort.TabIndex = 3;
        _numFoundryPort.Value = new decimal(new int[] { 5273, 0, 0, 0 });
        // 
        // _lblGatewayProviderEnabled
        // 
        _lblGatewayProviderEnabled.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderEnabled.Location = new Point(3, 45);
        _lblGatewayProviderEnabled.Name = "_lblGatewayProviderEnabled";
        _lblGatewayProviderEnabled.Size = new Size(144, 23);
        _lblGatewayProviderEnabled.TabIndex = 3;
        _lblGatewayProviderEnabled.Text = "Providers:";
        _lblGatewayProviderEnabled.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _gatewayProviderEnabledPanel
        // 
        _gatewayProviderEnabledPanel.AutoSize = true;
        _gatewayProviderEnabledPanel.Controls.Add(_chkGatewayOpenAiEnabled);
        _gatewayProviderEnabledPanel.Controls.Add(_chkGatewayAnthropicEnabled);
        _gatewayProviderEnabledPanel.Controls.Add(_chkGatewayGeminiEnabled);
        _gatewayProviderEnabledPanel.Dock = DockStyle.Fill;
        _gatewayProviderEnabledPanel.Location = new Point(153, 42);
        _gatewayProviderEnabledPanel.Name = "_gatewayProviderEnabledPanel";
        _gatewayProviderEnabledPanel.Size = new Size(563, 29);
        _gatewayProviderEnabledPanel.TabIndex = 4;
        _gatewayProviderEnabledPanel.WrapContents = false;
        // 
        // _chkGatewayOpenAiEnabled
        // 
        _chkGatewayOpenAiEnabled.AutoSize = true;
        _chkGatewayOpenAiEnabled.Location = new Point(3, 4);
        _chkGatewayOpenAiEnabled.Margin = new Padding(3, 4, 12, 3);
        _chkGatewayOpenAiEnabled.Name = "_chkGatewayOpenAiEnabled";
        _chkGatewayOpenAiEnabled.Size = new Size(75, 21);
        _chkGatewayOpenAiEnabled.TabIndex = 0;
        _chkGatewayOpenAiEnabled.Tag = "openai";
        _chkGatewayOpenAiEnabled.Text = "OpenAI";
        _chkGatewayOpenAiEnabled.UseVisualStyleBackColor = true;
        _chkGatewayOpenAiEnabled.CheckedChanged += ChkGatewayProviderEnabled_CheckedChanged;
        // 
        // _chkGatewayAnthropicEnabled
        // 
        _chkGatewayAnthropicEnabled.AutoSize = true;
        _chkGatewayAnthropicEnabled.Location = new Point(93, 4);
        _chkGatewayAnthropicEnabled.Margin = new Padding(3, 4, 12, 3);
        _chkGatewayAnthropicEnabled.Name = "_chkGatewayAnthropicEnabled";
        _chkGatewayAnthropicEnabled.Size = new Size(90, 21);
        _chkGatewayAnthropicEnabled.TabIndex = 1;
        _chkGatewayAnthropicEnabled.Tag = "anthropic";
        _chkGatewayAnthropicEnabled.Text = "Anthropic";
        _chkGatewayAnthropicEnabled.UseVisualStyleBackColor = true;
        _chkGatewayAnthropicEnabled.CheckedChanged += ChkGatewayProviderEnabled_CheckedChanged;
        // 
        // _chkGatewayGeminiEnabled
        // 
        _chkGatewayGeminiEnabled.AutoSize = true;
        _chkGatewayGeminiEnabled.Location = new Point(198, 4);
        _chkGatewayGeminiEnabled.Margin = new Padding(3, 4, 12, 3);
        _chkGatewayGeminiEnabled.Name = "_chkGatewayGeminiEnabled";
        _chkGatewayGeminiEnabled.Size = new Size(73, 21);
        _chkGatewayGeminiEnabled.TabIndex = 2;
        _chkGatewayGeminiEnabled.Tag = "google";
        _chkGatewayGeminiEnabled.Text = "Gemini";
        _chkGatewayGeminiEnabled.UseVisualStyleBackColor = true;
        _chkGatewayGeminiEnabled.CheckedChanged += ChkGatewayProviderEnabled_CheckedChanged;
        // 
        // _lblGatewayOverviewSummary
        // 
        _lblGatewayOverviewSummary.AutoSize = true;
        _lblGatewayOverviewSummary.Dock = DockStyle.Fill;
        _lblGatewayOverviewSummary.Location = new Point(3, 74);
        _lblGatewayOverviewSummary.Name = "_lblGatewayOverviewSummary";
        _lblGatewayOverviewSummary.Padding = new Padding(0, 8, 0, 0);
        _lblGatewayOverviewSummary.Size = new Size(144, 415);
        _lblGatewayOverviewSummary.TabIndex = 5;
        _lblGatewayOverviewSummary.Text = "Overview 只保留全局项。\r\n在 Providers 里先填写提供者地址和 API Key；协议、鉴权和端点默认自动判断。\r\n只有在需要覆盖默认行为时，再展开高级设置。";
        // 
        // _txtGatewayOverviewProviders
        // 
        _txtGatewayOverviewProviders.BackColor = SystemColors.Window;
        _txtGatewayOverviewProviders.Cursor = Cursors.Hand;
        _txtGatewayOverviewProviders.Dock = DockStyle.Fill;
        _txtGatewayOverviewProviders.Location = new Point(153, 77);
        _txtGatewayOverviewProviders.Multiline = true;
        _txtGatewayOverviewProviders.Name = "_txtGatewayOverviewProviders";
        _txtGatewayOverviewProviders.ReadOnly = true;
        _txtGatewayOverviewProviders.ScrollBars = ScrollBars.Vertical;
        _txtGatewayOverviewProviders.ShortcutsEnabled = false;
        _txtGatewayOverviewProviders.Size = new Size(563, 409);
        _txtGatewayOverviewProviders.TabIndex = 6;
        _txtGatewayOverviewProviders.WordWrap = false;
        _txtGatewayOverviewProviders.Click += TxtGatewayOverviewProviders_Click;
        _txtGatewayOverviewProviders.DoubleClick += TxtGatewayOverviewProviders_DoubleClick;
        // 
        // _gatewayOpenAiProviderTab
        // 
        _gatewayOpenAiProviderTab.Controls.Add(_gatewayOpenAiProviderControl);
        _gatewayOpenAiProviderTab.Location = new Point(4, 26);
        _gatewayOpenAiProviderTab.Name = "_gatewayOpenAiProviderTab";
        _gatewayOpenAiProviderTab.Padding = new Padding(8);
        _gatewayOpenAiProviderTab.Size = new Size(735, 505);
        _gatewayOpenAiProviderTab.TabIndex = 1;
        _gatewayOpenAiProviderTab.Tag = "openai";
        _gatewayOpenAiProviderTab.Text = "OpenAI";
        // 
        // _gatewayOpenAiProviderControl
        // 
        _gatewayOpenAiProviderControl.Dock = DockStyle.Fill;
        _gatewayOpenAiProviderControl.Location = new Point(8, 8);
        _gatewayOpenAiProviderControl.Name = "_gatewayOpenAiProviderControl";
        _gatewayOpenAiProviderControl.Size = new Size(719, 489);
        _gatewayOpenAiProviderControl.TabIndex = 0;
        // 
        // _gatewayAnthropicProviderTab
        // 
        _gatewayAnthropicProviderTab.Controls.Add(_gatewayAnthropicProviderControl);
        _gatewayAnthropicProviderTab.Location = new Point(4, 26);
        _gatewayAnthropicProviderTab.Name = "_gatewayAnthropicProviderTab";
        _gatewayAnthropicProviderTab.Padding = new Padding(8);
        _gatewayAnthropicProviderTab.Size = new Size(735, 505);
        _gatewayAnthropicProviderTab.TabIndex = 2;
        _gatewayAnthropicProviderTab.Tag = "anthropic";
        _gatewayAnthropicProviderTab.Text = "Anthropic";
        // 
        // _gatewayAnthropicProviderControl
        // 
        _gatewayAnthropicProviderControl.Dock = DockStyle.Fill;
        _gatewayAnthropicProviderControl.Location = new Point(8, 8);
        _gatewayAnthropicProviderControl.Name = "_gatewayAnthropicProviderControl";
        _gatewayAnthropicProviderControl.Size = new Size(719, 489);
        _gatewayAnthropicProviderControl.TabIndex = 0;
        // 
        // _gatewayGeminiProviderTab
        // 
        _gatewayGeminiProviderTab.Controls.Add(_gatewayGeminiProviderControl);
        _gatewayGeminiProviderTab.Location = new Point(4, 26);
        _gatewayGeminiProviderTab.Name = "_gatewayGeminiProviderTab";
        _gatewayGeminiProviderTab.Padding = new Padding(8);
        _gatewayGeminiProviderTab.Size = new Size(735, 505);
        _gatewayGeminiProviderTab.TabIndex = 3;
        _gatewayGeminiProviderTab.Tag = "google";
        _gatewayGeminiProviderTab.Text = "Gemini";
        // 
        // _gatewayGeminiProviderControl
        // 
        _gatewayGeminiProviderControl.Dock = DockStyle.Fill;
        _gatewayGeminiProviderControl.Location = new Point(8, 8);
        _gatewayGeminiProviderControl.Name = "_gatewayGeminiProviderControl";
        _gatewayGeminiProviderControl.Size = new Size(719, 489);
        _gatewayGeminiProviderControl.TabIndex = 0;
        // 
        // _grpGatewayProviderAdvanced
        // 
        _grpGatewayProviderAdvanced.Controls.Add(_gatewayProviderAdvancedLayout);
        _grpGatewayProviderAdvanced.Dock = DockStyle.Fill;
        _grpGatewayProviderAdvanced.Location = new Point(0, 430);
        _grpGatewayProviderAdvanced.Name = "_grpGatewayProviderAdvanced";
        _grpGatewayProviderAdvanced.Size = new Size(694, 29);
        _grpGatewayProviderAdvanced.TabIndex = 5;
        _grpGatewayProviderAdvanced.TabStop = false;
        _grpGatewayProviderAdvanced.Text = "Advanced Settings";
        // 
        // _gatewayProviderAdvancedLayout
        // 
        _gatewayProviderAdvancedLayout.ColumnCount = 1;
        _gatewayProviderAdvancedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gatewayProviderAdvancedLayout.Controls.Add(_gatewayProviderDetailsPanel, 0, 0);
        _gatewayProviderAdvancedLayout.Controls.Add(_grpGatewayProviderAuth, 0, 1);
        _gatewayProviderAdvancedLayout.Controls.Add(_grpGatewayProviderEndpoints, 0, 2);
        _gatewayProviderAdvancedLayout.Controls.Add(_grpGatewayProviderCapabilities, 0, 3);
        _gatewayProviderAdvancedLayout.Dock = DockStyle.Fill;
        _gatewayProviderAdvancedLayout.Location = new Point(3, 19);
        _gatewayProviderAdvancedLayout.Name = "_gatewayProviderAdvancedLayout";
        _gatewayProviderAdvancedLayout.RowCount = 4;
        _gatewayProviderAdvancedLayout.RowStyles.Add(new RowStyle());
        _gatewayProviderAdvancedLayout.RowStyles.Add(new RowStyle());
        _gatewayProviderAdvancedLayout.RowStyles.Add(new RowStyle());
        _gatewayProviderAdvancedLayout.RowStyles.Add(new RowStyle());
        _gatewayProviderAdvancedLayout.Size = new Size(688, 7);
        _gatewayProviderAdvancedLayout.TabIndex = 0;
        // 
        // _gatewayProviderDetailsPanel
        // 
        _gatewayProviderDetailsPanel.ColumnCount = 4;
        _gatewayProviderDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _gatewayProviderDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _gatewayProviderDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderId, 0, 0);
        _gatewayProviderDetailsPanel.Controls.Add(_txtGatewayProviderId, 1, 0);
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderProtocol2, 2, 0);
        _gatewayProviderDetailsPanel.Controls.Add(_cmbGatewayProviderProtocol2, 3, 0);
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderName2, 0, 1);
        _gatewayProviderDetailsPanel.Controls.Add(_txtGatewayProviderName2, 1, 1);
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderBaseUrl2, 2, 1);
        _gatewayProviderDetailsPanel.Controls.Add(_txtGatewayProviderBaseUrl2, 3, 1);
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderDefaultModel2, 0, 2);
        _gatewayProviderDetailsPanel.Controls.Add(_txtGatewayProviderDefaultModel2, 1, 2);
        _gatewayProviderDetailsPanel.Controls.Add(_lblGatewayProviderEmbeddingModel2, 2, 2);
        _gatewayProviderDetailsPanel.Controls.Add(_txtGatewayProviderEmbeddingModel2, 3, 2);
        _gatewayProviderDetailsPanel.Dock = DockStyle.Top;
        _gatewayProviderDetailsPanel.Location = new Point(3, 0);
        _gatewayProviderDetailsPanel.Margin = new Padding(3, 0, 3, 6);
        _gatewayProviderDetailsPanel.Name = "_gatewayProviderDetailsPanel";
        _gatewayProviderDetailsPanel.RowCount = 3;
        _gatewayProviderDetailsPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderDetailsPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderDetailsPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderDetailsPanel.Size = new Size(682, 90);
        _gatewayProviderDetailsPanel.TabIndex = 4;
        // 
        // _lblGatewayProviderId
        // 
        _lblGatewayProviderId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderId.Location = new Point(3, 4);
        _lblGatewayProviderId.Name = "_lblGatewayProviderId";
        _lblGatewayProviderId.Size = new Size(84, 23);
        _lblGatewayProviderId.TabIndex = 0;
        _lblGatewayProviderId.Text = "Id:";
        _lblGatewayProviderId.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderId
        // 
        _txtGatewayProviderId.Dock = DockStyle.Fill;
        _txtGatewayProviderId.Location = new Point(93, 3);
        _txtGatewayProviderId.Name = "_txtGatewayProviderId";
        _txtGatewayProviderId.Size = new Size(235, 23);
        _txtGatewayProviderId.TabIndex = 1;
        // 
        // _lblGatewayProviderProtocol2
        // 
        _lblGatewayProviderProtocol2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderProtocol2.Location = new Point(334, 4);
        _lblGatewayProviderProtocol2.Name = "_lblGatewayProviderProtocol2";
        _lblGatewayProviderProtocol2.Size = new Size(104, 23);
        _lblGatewayProviderProtocol2.TabIndex = 2;
        _lblGatewayProviderProtocol2.Text = "Protocol:";
        _lblGatewayProviderProtocol2.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbGatewayProviderProtocol2
        // 
        _cmbGatewayProviderProtocol2.Dock = DockStyle.Fill;
        _cmbGatewayProviderProtocol2.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbGatewayProviderProtocol2.FormattingEnabled = true;
        _cmbGatewayProviderProtocol2.Items.AddRange(new object[] { "OpenAICompatible", "Anthropic" });
        _cmbGatewayProviderProtocol2.Location = new Point(444, 3);
        _cmbGatewayProviderProtocol2.Name = "_cmbGatewayProviderProtocol2";
        _cmbGatewayProviderProtocol2.Size = new Size(235, 25);
        _cmbGatewayProviderProtocol2.TabIndex = 3;
        _cmbGatewayProviderProtocol2.SelectedIndexChanged += CmbGatewayProviderProtocol2_SelectedIndexChanged;
        // 
        // _lblGatewayProviderName2
        // 
        _lblGatewayProviderName2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderName2.Location = new Point(3, 34);
        _lblGatewayProviderName2.Name = "_lblGatewayProviderName2";
        _lblGatewayProviderName2.Size = new Size(84, 23);
        _lblGatewayProviderName2.TabIndex = 4;
        _lblGatewayProviderName2.Text = "Name:";
        _lblGatewayProviderName2.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderName2
        // 
        _txtGatewayProviderName2.Dock = DockStyle.Fill;
        _txtGatewayProviderName2.Location = new Point(93, 34);
        _txtGatewayProviderName2.Name = "_txtGatewayProviderName2";
        _txtGatewayProviderName2.Size = new Size(235, 23);
        _txtGatewayProviderName2.TabIndex = 5;
        _txtGatewayProviderName2.TextChanged += TxtGatewayProviderName2_TextChanged;
        // 
        // _lblGatewayProviderBaseUrl2
        // 
        _lblGatewayProviderBaseUrl2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderBaseUrl2.Location = new Point(334, 34);
        _lblGatewayProviderBaseUrl2.Name = "_lblGatewayProviderBaseUrl2";
        _lblGatewayProviderBaseUrl2.Size = new Size(104, 23);
        _lblGatewayProviderBaseUrl2.TabIndex = 6;
        _lblGatewayProviderBaseUrl2.Text = "Base URL:";
        _lblGatewayProviderBaseUrl2.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderBaseUrl2
        // 
        _txtGatewayProviderBaseUrl2.Dock = DockStyle.Fill;
        _txtGatewayProviderBaseUrl2.Location = new Point(444, 34);
        _txtGatewayProviderBaseUrl2.Name = "_txtGatewayProviderBaseUrl2";
        _txtGatewayProviderBaseUrl2.Size = new Size(235, 23);
        _txtGatewayProviderBaseUrl2.TabIndex = 7;
        _txtGatewayProviderBaseUrl2.TextChanged += TxtGatewayProviderBaseUrl2_TextChanged;
        // 
        // _lblGatewayProviderDefaultModel2
        // 
        _lblGatewayProviderDefaultModel2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderDefaultModel2.Location = new Point(3, 64);
        _lblGatewayProviderDefaultModel2.Name = "_lblGatewayProviderDefaultModel2";
        _lblGatewayProviderDefaultModel2.Size = new Size(84, 23);
        _lblGatewayProviderDefaultModel2.TabIndex = 8;
        _lblGatewayProviderDefaultModel2.Text = "Chat Model:";
        _lblGatewayProviderDefaultModel2.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderDefaultModel2
        // 
        _txtGatewayProviderDefaultModel2.Dock = DockStyle.Fill;
        _txtGatewayProviderDefaultModel2.FormattingEnabled = true;
        _txtGatewayProviderDefaultModel2.Location = new Point(93, 63);
        _txtGatewayProviderDefaultModel2.Name = "_txtGatewayProviderDefaultModel2";
        _txtGatewayProviderDefaultModel2.Size = new Size(235, 25);
        _txtGatewayProviderDefaultModel2.TabIndex = 9;
        // 
        // _lblGatewayProviderEmbeddingModel2
        // 
        _lblGatewayProviderEmbeddingModel2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderEmbeddingModel2.Location = new Point(334, 64);
        _lblGatewayProviderEmbeddingModel2.Name = "_lblGatewayProviderEmbeddingModel2";
        _lblGatewayProviderEmbeddingModel2.Size = new Size(104, 23);
        _lblGatewayProviderEmbeddingModel2.TabIndex = 10;
        _lblGatewayProviderEmbeddingModel2.Text = "Embedding:";
        _lblGatewayProviderEmbeddingModel2.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderEmbeddingModel2
        // 
        _txtGatewayProviderEmbeddingModel2.Dock = DockStyle.Fill;
        _txtGatewayProviderEmbeddingModel2.FormattingEnabled = true;
        _txtGatewayProviderEmbeddingModel2.Location = new Point(444, 63);
        _txtGatewayProviderEmbeddingModel2.Name = "_txtGatewayProviderEmbeddingModel2";
        _txtGatewayProviderEmbeddingModel2.Size = new Size(235, 25);
        _txtGatewayProviderEmbeddingModel2.TabIndex = 11;
        // 
        // _grpGatewayProviderAuth
        // 
        _grpGatewayProviderAuth.Controls.Add(_gatewayProviderAuthPanel);
        _grpGatewayProviderAuth.Dock = DockStyle.Top;
        _grpGatewayProviderAuth.Location = new Point(3, 99);
        _grpGatewayProviderAuth.Name = "_grpGatewayProviderAuth";
        _grpGatewayProviderAuth.Size = new Size(682, 58);
        _grpGatewayProviderAuth.TabIndex = 1;
        _grpGatewayProviderAuth.TabStop = false;
        _grpGatewayProviderAuth.Text = "Auth";
        // 
        // _gatewayProviderAuthPanel
        // 
        _gatewayProviderAuthPanel.ColumnCount = 4;
        _gatewayProviderAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _gatewayProviderAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _gatewayProviderAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderAuthPanel.Controls.Add(_lblGatewayProviderAuthType, 0, 0);
        _gatewayProviderAuthPanel.Controls.Add(_cmbGatewayProviderAuthType, 1, 0);
        _gatewayProviderAuthPanel.Controls.Add(_lblGatewayProviderAuthHeader, 2, 0);
        _gatewayProviderAuthPanel.Controls.Add(_txtGatewayProviderAuthHeader, 3, 0);
        _gatewayProviderAuthPanel.Dock = DockStyle.Fill;
        _gatewayProviderAuthPanel.Location = new Point(3, 19);
        _gatewayProviderAuthPanel.Name = "_gatewayProviderAuthPanel";
        _gatewayProviderAuthPanel.RowCount = 1;
        _gatewayProviderAuthPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderAuthPanel.Size = new Size(676, 36);
        _gatewayProviderAuthPanel.TabIndex = 0;
        // 
        // _lblGatewayProviderAuthType
        // 
        _lblGatewayProviderAuthType.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderAuthType.Location = new Point(3, 6);
        _lblGatewayProviderAuthType.Name = "_lblGatewayProviderAuthType";
        _lblGatewayProviderAuthType.Size = new Size(84, 23);
        _lblGatewayProviderAuthType.TabIndex = 12;
        _lblGatewayProviderAuthType.Text = "Auth Type:";
        _lblGatewayProviderAuthType.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbGatewayProviderAuthType
        // 
        _cmbGatewayProviderAuthType.Dock = DockStyle.Fill;
        _cmbGatewayProviderAuthType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbGatewayProviderAuthType.FormattingEnabled = true;
        _cmbGatewayProviderAuthType.Items.AddRange(new object[] { "Bearer", "Header", "Query" });
        _cmbGatewayProviderAuthType.Location = new Point(93, 3);
        _cmbGatewayProviderAuthType.Name = "_cmbGatewayProviderAuthType";
        _cmbGatewayProviderAuthType.Size = new Size(232, 25);
        _cmbGatewayProviderAuthType.TabIndex = 13;
        // 
        // _lblGatewayProviderAuthHeader
        // 
        _lblGatewayProviderAuthHeader.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderAuthHeader.Location = new Point(331, 6);
        _lblGatewayProviderAuthHeader.Name = "_lblGatewayProviderAuthHeader";
        _lblGatewayProviderAuthHeader.Size = new Size(104, 23);
        _lblGatewayProviderAuthHeader.TabIndex = 14;
        _lblGatewayProviderAuthHeader.Text = "Auth Name:";
        _lblGatewayProviderAuthHeader.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderAuthHeader
        // 
        _txtGatewayProviderAuthHeader.Dock = DockStyle.Fill;
        _txtGatewayProviderAuthHeader.Location = new Point(441, 3);
        _txtGatewayProviderAuthHeader.Name = "_txtGatewayProviderAuthHeader";
        _txtGatewayProviderAuthHeader.Size = new Size(232, 23);
        _txtGatewayProviderAuthHeader.TabIndex = 15;
        // 
        // _grpGatewayProviderEndpoints
        // 
        _grpGatewayProviderEndpoints.Controls.Add(_gatewayProviderEndpointsPanel);
        _grpGatewayProviderEndpoints.Dock = DockStyle.Top;
        _grpGatewayProviderEndpoints.Location = new Point(3, 163);
        _grpGatewayProviderEndpoints.Name = "_grpGatewayProviderEndpoints";
        _grpGatewayProviderEndpoints.Size = new Size(682, 91);
        _grpGatewayProviderEndpoints.TabIndex = 2;
        _grpGatewayProviderEndpoints.TabStop = false;
        _grpGatewayProviderEndpoints.Text = "Endpoints";
        // 
        // _gatewayProviderEndpointsPanel
        // 
        _gatewayProviderEndpointsPanel.ColumnCount = 4;
        _gatewayProviderEndpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _gatewayProviderEndpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderEndpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _gatewayProviderEndpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gatewayProviderEndpointsPanel.Controls.Add(_lblGatewayProviderChatEndpoint, 0, 0);
        _gatewayProviderEndpointsPanel.Controls.Add(_txtGatewayProviderChatEndpoint, 1, 0);
        _gatewayProviderEndpointsPanel.Controls.Add(_lblGatewayProviderEmbeddingsEndpoint, 2, 0);
        _gatewayProviderEndpointsPanel.Controls.Add(_txtGatewayProviderEmbeddingsEndpoint, 3, 0);
        _gatewayProviderEndpointsPanel.Controls.Add(_lblGatewayProviderResponsesEndpoint, 0, 1);
        _gatewayProviderEndpointsPanel.Controls.Add(_txtGatewayProviderResponsesEndpoint, 1, 1);
        _gatewayProviderEndpointsPanel.Controls.Add(_lblGatewayProviderAdditionalHeaders, 2, 1);
        _gatewayProviderEndpointsPanel.Controls.Add(_txtGatewayProviderAdditionalHeaders, 3, 1);
        _gatewayProviderEndpointsPanel.Dock = DockStyle.Fill;
        _gatewayProviderEndpointsPanel.Location = new Point(3, 19);
        _gatewayProviderEndpointsPanel.Name = "_gatewayProviderEndpointsPanel";
        _gatewayProviderEndpointsPanel.RowCount = 2;
        _gatewayProviderEndpointsPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderEndpointsPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderEndpointsPanel.Size = new Size(676, 69);
        _gatewayProviderEndpointsPanel.TabIndex = 0;
        // 
        // _lblGatewayProviderChatEndpoint
        // 
        _lblGatewayProviderChatEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderChatEndpoint.Location = new Point(3, 3);
        _lblGatewayProviderChatEndpoint.Name = "_lblGatewayProviderChatEndpoint";
        _lblGatewayProviderChatEndpoint.Size = new Size(84, 23);
        _lblGatewayProviderChatEndpoint.TabIndex = 16;
        _lblGatewayProviderChatEndpoint.Text = "Chat API:";
        _lblGatewayProviderChatEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderChatEndpoint
        // 
        _txtGatewayProviderChatEndpoint.Dock = DockStyle.Fill;
        _txtGatewayProviderChatEndpoint.Location = new Point(93, 3);
        _txtGatewayProviderChatEndpoint.Name = "_txtGatewayProviderChatEndpoint";
        _txtGatewayProviderChatEndpoint.Size = new Size(232, 23);
        _txtGatewayProviderChatEndpoint.TabIndex = 17;
        // 
        // _lblGatewayProviderEmbeddingsEndpoint
        // 
        _lblGatewayProviderEmbeddingsEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderEmbeddingsEndpoint.Location = new Point(331, 3);
        _lblGatewayProviderEmbeddingsEndpoint.Name = "_lblGatewayProviderEmbeddingsEndpoint";
        _lblGatewayProviderEmbeddingsEndpoint.Size = new Size(104, 23);
        _lblGatewayProviderEmbeddingsEndpoint.TabIndex = 18;
        _lblGatewayProviderEmbeddingsEndpoint.Text = "Embed API:";
        _lblGatewayProviderEmbeddingsEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderEmbeddingsEndpoint
        // 
        _txtGatewayProviderEmbeddingsEndpoint.Dock = DockStyle.Fill;
        _txtGatewayProviderEmbeddingsEndpoint.Location = new Point(441, 3);
        _txtGatewayProviderEmbeddingsEndpoint.Name = "_txtGatewayProviderEmbeddingsEndpoint";
        _txtGatewayProviderEmbeddingsEndpoint.Size = new Size(232, 23);
        _txtGatewayProviderEmbeddingsEndpoint.TabIndex = 19;
        // 
        // _lblGatewayProviderResponsesEndpoint
        // 
        _lblGatewayProviderResponsesEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderResponsesEndpoint.Location = new Point(3, 37);
        _lblGatewayProviderResponsesEndpoint.Name = "_lblGatewayProviderResponsesEndpoint";
        _lblGatewayProviderResponsesEndpoint.Size = new Size(84, 23);
        _lblGatewayProviderResponsesEndpoint.TabIndex = 20;
        _lblGatewayProviderResponsesEndpoint.Text = "Resp API:";
        _lblGatewayProviderResponsesEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderResponsesEndpoint
        // 
        _txtGatewayProviderResponsesEndpoint.Dock = DockStyle.Fill;
        _txtGatewayProviderResponsesEndpoint.Location = new Point(93, 32);
        _txtGatewayProviderResponsesEndpoint.Name = "_txtGatewayProviderResponsesEndpoint";
        _txtGatewayProviderResponsesEndpoint.Size = new Size(232, 23);
        _txtGatewayProviderResponsesEndpoint.TabIndex = 21;
        // 
        // _lblGatewayProviderAdditionalHeaders
        // 
        _lblGatewayProviderAdditionalHeaders.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderAdditionalHeaders.Location = new Point(331, 37);
        _lblGatewayProviderAdditionalHeaders.Name = "_lblGatewayProviderAdditionalHeaders";
        _lblGatewayProviderAdditionalHeaders.Size = new Size(104, 23);
        _lblGatewayProviderAdditionalHeaders.TabIndex = 22;
        _lblGatewayProviderAdditionalHeaders.Text = "Extra Headers:";
        _lblGatewayProviderAdditionalHeaders.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayProviderAdditionalHeaders
        // 
        _txtGatewayProviderAdditionalHeaders.Dock = DockStyle.Fill;
        _txtGatewayProviderAdditionalHeaders.Location = new Point(441, 32);
        _txtGatewayProviderAdditionalHeaders.Multiline = true;
        _txtGatewayProviderAdditionalHeaders.Name = "_txtGatewayProviderAdditionalHeaders";
        _txtGatewayProviderAdditionalHeaders.ScrollBars = ScrollBars.Vertical;
        _txtGatewayProviderAdditionalHeaders.Size = new Size(232, 34);
        _txtGatewayProviderAdditionalHeaders.TabIndex = 23;
        _txtGatewayProviderAdditionalHeaders.WordWrap = false;
        // 
        // _grpGatewayProviderCapabilities
        // 
        _grpGatewayProviderCapabilities.Controls.Add(_gatewayProviderCapabilitiesPanel);
        _grpGatewayProviderCapabilities.Dock = DockStyle.Top;
        _grpGatewayProviderCapabilities.Location = new Point(3, 260);
        _grpGatewayProviderCapabilities.Name = "_grpGatewayProviderCapabilities";
        _grpGatewayProviderCapabilities.Size = new Size(682, 52);
        _grpGatewayProviderCapabilities.TabIndex = 3;
        _grpGatewayProviderCapabilities.TabStop = false;
        _grpGatewayProviderCapabilities.Text = "Capabilities";
        // 
        // _gatewayProviderCapabilitiesPanel
        // 
        _gatewayProviderCapabilitiesPanel.AutoSize = true;
        _gatewayProviderCapabilitiesPanel.Controls.Add(_chkGatewaySupportsChat);
        _gatewayProviderCapabilitiesPanel.Controls.Add(_chkGatewaySupportsEmbeddings);
        _gatewayProviderCapabilitiesPanel.Controls.Add(_chkGatewaySupportsResponses);
        _gatewayProviderCapabilitiesPanel.Controls.Add(_chkGatewaySupportsStreaming);
        _gatewayProviderCapabilitiesPanel.Dock = DockStyle.Fill;
        _gatewayProviderCapabilitiesPanel.Location = new Point(3, 19);
        _gatewayProviderCapabilitiesPanel.Name = "_gatewayProviderCapabilitiesPanel";
        _gatewayProviderCapabilitiesPanel.Size = new Size(676, 30);
        _gatewayProviderCapabilitiesPanel.TabIndex = 24;
        _gatewayProviderCapabilitiesPanel.WrapContents = false;
        // 
        // _chkGatewaySupportsChat
        // 
        _chkGatewaySupportsChat.AutoSize = true;
        _chkGatewaySupportsChat.Location = new Point(3, 3);
        _chkGatewaySupportsChat.Name = "_chkGatewaySupportsChat";
        _chkGatewaySupportsChat.Size = new Size(110, 21);
        _chkGatewaySupportsChat.TabIndex = 0;
        _chkGatewaySupportsChat.Text = "Supports Chat";
        // 
        // _chkGatewaySupportsEmbeddings
        // 
        _chkGatewaySupportsEmbeddings.AutoSize = true;
        _chkGatewaySupportsEmbeddings.Location = new Point(119, 3);
        _chkGatewaySupportsEmbeddings.Name = "_chkGatewaySupportsEmbeddings";
        _chkGatewaySupportsEmbeddings.Size = new Size(151, 21);
        _chkGatewaySupportsEmbeddings.TabIndex = 1;
        _chkGatewaySupportsEmbeddings.Text = "Supports Embedding";
        _chkGatewaySupportsEmbeddings.CheckedChanged += ChkGatewaySupportsEmbeddings_CheckedChanged;
        // 
        // _chkGatewaySupportsResponses
        // 
        _chkGatewaySupportsResponses.AutoSize = true;
        _chkGatewaySupportsResponses.Location = new Point(276, 3);
        _chkGatewaySupportsResponses.Name = "_chkGatewaySupportsResponses";
        _chkGatewaySupportsResponses.Size = new Size(147, 21);
        _chkGatewaySupportsResponses.TabIndex = 2;
        _chkGatewaySupportsResponses.Text = "Supports Responses";
        // 
        // _chkGatewaySupportsStreaming
        // 
        _chkGatewaySupportsStreaming.AutoSize = true;
        _chkGatewaySupportsStreaming.Location = new Point(429, 3);
        _chkGatewaySupportsStreaming.Name = "_chkGatewaySupportsStreaming";
        _chkGatewaySupportsStreaming.Size = new Size(143, 21);
        _chkGatewaySupportsStreaming.TabIndex = 3;
        _chkGatewaySupportsStreaming.Text = "Supports Streaming";
        // 
        // _chkGatewayProviderShowAdvanced
        // 
        _chkGatewayProviderShowAdvanced.AutoSize = true;
        _chkGatewayProviderShowAdvanced.Location = new Point(3, 438);
        _chkGatewayProviderShowAdvanced.Name = "_chkGatewayProviderShowAdvanced";
        _chkGatewayProviderShowAdvanced.Size = new Size(119, 21);
        _chkGatewayProviderShowAdvanced.TabIndex = 4;
        _chkGatewayProviderShowAdvanced.Text = "Show Advanced";
        _chkGatewayProviderShowAdvanced.UseVisualStyleBackColor = true;
        _chkGatewayProviderShowAdvanced.CheckedChanged += ChkGatewayProviderShowAdvanced_CheckedChanged;
        // 
        // _grpGatewayProviderBasic
        // 
        _grpGatewayProviderBasic.Controls.Add(_gatewayProviderBasicPanel);
        _grpGatewayProviderBasic.Dock = DockStyle.Top;
        _grpGatewayProviderBasic.Location = new Point(0, 0);
        _grpGatewayProviderBasic.Name = "_grpGatewayProviderBasic";
        _grpGatewayProviderBasic.Size = new Size(694, 430);
        _grpGatewayProviderBasic.TabIndex = 3;
        _grpGatewayProviderBasic.TabStop = false;
        _grpGatewayProviderBasic.Text = "Basic Settings";
        // 
        // _gatewayProviderBasicPanel
        // 
        _gatewayProviderBasicPanel.ColumnCount = 2;
        _gatewayProviderBasicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _gatewayProviderBasicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gatewayProviderBasicPanel.Controls.Add(_lblLocalApiProviderUrl, 0, 0);
        _gatewayProviderBasicPanel.Controls.Add(_localApiProviderUrlPanel, 1, 0);
        _gatewayProviderBasicPanel.Controls.Add(_lblLocalApiApiKey, 0, 1);
        _gatewayProviderBasicPanel.Controls.Add(_txtLocalApiApiKey, 1, 1);
        _gatewayProviderBasicPanel.Controls.Add(_lblGatewayDetectedProtocol, 0, 2);
        _gatewayProviderBasicPanel.Controls.Add(_txtGatewayDetectedProtocol, 1, 2);
        _gatewayProviderBasicPanel.Controls.Add(_lblGatewayProviderDisplayName, 0, 3);
        _gatewayProviderBasicPanel.Controls.Add(_btnGatewayDetectProvider, 1, 4);
        _gatewayProviderBasicPanel.Controls.Add(_gatewayProviderModelActionsPanel, 1, 5);
        _gatewayProviderBasicPanel.Controls.Add(_lblGatewayProviderModelPreview, 0, 6);
        _gatewayProviderBasicPanel.Controls.Add(_txtGatewayProviderModelPreview, 1, 6);
        _gatewayProviderBasicPanel.Controls.Add(_lblGatewayProviderModelMetadata, 0, 7);
        _gatewayProviderBasicPanel.Controls.Add(_txtGatewayProviderModelMetadata, 1, 7);
        _gatewayProviderBasicPanel.Controls.Add(_btnGatewayCopyModelMetadata, 1, 8);
        _gatewayProviderBasicPanel.Dock = DockStyle.Fill;
        _gatewayProviderBasicPanel.Location = new Point(3, 19);
        _gatewayProviderBasicPanel.Name = "_gatewayProviderBasicPanel";
        _gatewayProviderBasicPanel.RowCount = 8;
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle());
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        _gatewayProviderBasicPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        _gatewayProviderBasicPanel.Size = new Size(688, 408);
        _gatewayProviderBasicPanel.TabIndex = 0;
        // 
        // _lblLocalApiProviderUrl
        // 
        _lblLocalApiProviderUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiProviderUrl.Location = new Point(3, 6);
        _lblLocalApiProviderUrl.Name = "_lblLocalApiProviderUrl";
        _lblLocalApiProviderUrl.Size = new Size(144, 23);
        _lblLocalApiProviderUrl.TabIndex = 5;
        _lblLocalApiProviderUrl.Text = "Provider Base URL:";
        _lblLocalApiProviderUrl.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _localApiProviderUrlPanel
        // 
        _localApiProviderUrlPanel.ColumnCount = 2;
        _localApiProviderUrlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _localApiProviderUrlPanel.ColumnStyles.Add(new ColumnStyle());
        _localApiProviderUrlPanel.Controls.Add(_txtLocalApiProviderUrl, 0, 0);
        _localApiProviderUrlPanel.Controls.Add(_btnRefreshLocalApiModels, 1, 0);
        _localApiProviderUrlPanel.Dock = DockStyle.Fill;
        _localApiProviderUrlPanel.Location = new Point(153, 0);
        _localApiProviderUrlPanel.Margin = new Padding(3, 0, 3, 3);
        _localApiProviderUrlPanel.Name = "_localApiProviderUrlPanel";
        _localApiProviderUrlPanel.RowCount = 1;
        _localApiProviderUrlPanel.RowStyles.Add(new RowStyle());
        _localApiProviderUrlPanel.Size = new Size(532, 33);
        _localApiProviderUrlPanel.TabIndex = 6;
        // 
        // _txtLocalApiProviderUrl
        // 
        _txtLocalApiProviderUrl.Dock = DockStyle.Fill;
        _txtLocalApiProviderUrl.Location = new Point(3, 5);
        _txtLocalApiProviderUrl.Margin = new Padding(3, 5, 3, 5);
        _txtLocalApiProviderUrl.Name = "_txtLocalApiProviderUrl";
        _txtLocalApiProviderUrl.PlaceholderText = "https://api.openai.com/v1/";
        _txtLocalApiProviderUrl.Size = new Size(410, 23);
        _txtLocalApiProviderUrl.TabIndex = 0;
        // 
        // _btnRefreshLocalApiModels
        // 
        _btnRefreshLocalApiModels.AutoSize = true;
        _btnRefreshLocalApiModels.Location = new Point(419, 3);
        _btnRefreshLocalApiModels.Name = "_btnRefreshLocalApiModels";
        _btnRefreshLocalApiModels.Size = new Size(110, 27);
        _btnRefreshLocalApiModels.TabIndex = 1;
        _btnRefreshLocalApiModels.Text = "Refresh Models";
        _btnRefreshLocalApiModels.UseVisualStyleBackColor = true;
        _btnRefreshLocalApiModels.Click += BtnRefreshLocalApiModels_Click;
        // 
        // _lblLocalApiApiKey
        // 
        _lblLocalApiApiKey.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiApiKey.Location = new Point(3, 39);
        _lblLocalApiApiKey.Name = "_lblLocalApiApiKey";
        _lblLocalApiApiKey.Size = new Size(144, 23);
        _lblLocalApiApiKey.TabIndex = 13;
        _lblLocalApiApiKey.Text = "Provider API Key:";
        _lblLocalApiApiKey.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtLocalApiApiKey
        // 
        _txtLocalApiApiKey.Dock = DockStyle.Fill;
        _txtLocalApiApiKey.Location = new Point(153, 39);
        _txtLocalApiApiKey.Name = "_txtLocalApiApiKey";
        _txtLocalApiApiKey.PasswordChar = '●';
        _txtLocalApiApiKey.PlaceholderText = "Stored in Windows Credential Manager, not in config JSON";
        _txtLocalApiApiKey.Size = new Size(532, 23);
        _txtLocalApiApiKey.TabIndex = 14;
        // 
        // _lblGatewayDetectedProtocol
        // 
        _lblGatewayDetectedProtocol.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayDetectedProtocol.Location = new Point(3, 68);
        _lblGatewayDetectedProtocol.Name = "_lblGatewayDetectedProtocol";
        _lblGatewayDetectedProtocol.Size = new Size(144, 23);
        _lblGatewayDetectedProtocol.TabIndex = 2;
        _lblGatewayDetectedProtocol.Text = "Detected Protocol:";
        _lblGatewayDetectedProtocol.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGatewayDetectedProtocol
        // 
        _txtGatewayDetectedProtocol.BorderStyle = BorderStyle.FixedSingle;
        _txtGatewayDetectedProtocol.Dock = DockStyle.Fill;
        _txtGatewayDetectedProtocol.Location = new Point(153, 68);
        _txtGatewayDetectedProtocol.Name = "_txtGatewayDetectedProtocol";
        _txtGatewayDetectedProtocol.ReadOnly = true;
        _txtGatewayDetectedProtocol.Size = new Size(532, 23);
        _txtGatewayDetectedProtocol.TabIndex = 3;
        // 
        // _lblGatewayProviderDisplayName
        // 
        _lblGatewayProviderDisplayName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderDisplayName.Location = new Point(3, 94);
        _lblGatewayProviderDisplayName.Name = "_lblGatewayProviderDisplayName";
        _lblGatewayProviderDisplayName.Size = new Size(144, 23);
        _lblGatewayProviderDisplayName.TabIndex = 4;
        _lblGatewayProviderDisplayName.Text = "Display Name (optional):";
        _lblGatewayProviderDisplayName.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _btnGatewayDetectProvider
        // 
        _btnGatewayDetectProvider.AutoSize = true;
        _btnGatewayDetectProvider.Location = new Point(153, 120);
        _btnGatewayDetectProvider.Name = "_btnGatewayDetectProvider";
        _btnGatewayDetectProvider.Size = new Size(143, 27);
        _btnGatewayDetectProvider.TabIndex = 6;
        _btnGatewayDetectProvider.Text = "Test / Detect Defaults";
        _btnGatewayDetectProvider.UseVisualStyleBackColor = true;
        _btnGatewayDetectProvider.Click += BtnGatewayDetectProvider_Click;
        // 
        // _gatewayProviderModelActionsPanel
        // 
        _gatewayProviderModelActionsPanel.AutoSize = true;
        _gatewayProviderModelActionsPanel.Controls.Add(_btnGatewayRefreshProviderModels);
        _gatewayProviderModelActionsPanel.Controls.Add(_btnGatewayRefreshProviderModelsApply);
        _gatewayProviderModelActionsPanel.Dock = DockStyle.Fill;
        _gatewayProviderModelActionsPanel.Location = new Point(153, 153);
        _gatewayProviderModelActionsPanel.Name = "_gatewayProviderModelActionsPanel";
        _gatewayProviderModelActionsPanel.Size = new Size(532, 92);
        _gatewayProviderModelActionsPanel.TabIndex = 7;
        _gatewayProviderModelActionsPanel.WrapContents = false;
        // 
        // _btnGatewayRefreshProviderModels
        // 
        _btnGatewayRefreshProviderModels.AutoSize = true;
        _btnGatewayRefreshProviderModels.Location = new Point(0, 0);
        _btnGatewayRefreshProviderModels.Margin = new Padding(0, 0, 6, 0);
        _btnGatewayRefreshProviderModels.Name = "_btnGatewayRefreshProviderModels";
        _btnGatewayRefreshProviderModels.Size = new Size(111, 27);
        _btnGatewayRefreshProviderModels.TabIndex = 0;
        _btnGatewayRefreshProviderModels.Text = "Refresh Only";
        _btnGatewayRefreshProviderModels.UseVisualStyleBackColor = true;
        _btnGatewayRefreshProviderModels.Click += BtnGatewayRefreshProviderModels_Click;
        // 
        // _btnGatewayRefreshProviderModelsApply
        // 
        _btnGatewayRefreshProviderModelsApply.AutoSize = true;
        _btnGatewayRefreshProviderModelsApply.Location = new Point(117, 0);
        _btnGatewayRefreshProviderModelsApply.Margin = new Padding(0);
        _btnGatewayRefreshProviderModelsApply.Name = "_btnGatewayRefreshProviderModelsApply";
        _btnGatewayRefreshProviderModelsApply.Size = new Size(149, 27);
        _btnGatewayRefreshProviderModelsApply.TabIndex = 1;
        _btnGatewayRefreshProviderModelsApply.Text = "Refresh + Apply";
        _btnGatewayRefreshProviderModelsApply.UseVisualStyleBackColor = true;
        _btnGatewayRefreshProviderModelsApply.Click += BtnGatewayRefreshProviderModelsApply_Click;
        // 
        // _lblGatewayProviderModelPreview
        // 
        _lblGatewayProviderModelPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderModelPreview.Location = new Point(3, 248);
        _lblGatewayProviderModelPreview.Name = "_lblGatewayProviderModelPreview";
        _lblGatewayProviderModelPreview.Size = new Size(144, 23);
        _lblGatewayProviderModelPreview.TabIndex = 8;
        _lblGatewayProviderModelPreview.Text = "Detected Models:";
        _lblGatewayProviderModelPreview.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtGatewayProviderModelPreview
        // 
        _txtGatewayProviderModelPreview.ContextMenuStrip = _gatewayModelPreviewContextMenu;
        _txtGatewayProviderModelPreview.Dock = DockStyle.Fill;
        _txtGatewayProviderModelPreview.FormattingEnabled = true;
        _txtGatewayProviderModelPreview.IntegralHeight = false;
        _txtGatewayProviderModelPreview.Location = new Point(153, 251);
        _txtGatewayProviderModelPreview.Name = "_txtGatewayProviderModelPreview";
        _txtGatewayProviderModelPreview.Size = new Size(532, 90);
        _txtGatewayProviderModelPreview.TabIndex = 9;
        _txtGatewayProviderModelPreview.SelectedIndexChanged += GatewayProviderModelPreview_SelectedIndexChanged;
        _txtGatewayProviderModelPreview.DoubleClick += GatewayProviderModelPreview_DoubleClick;
        _txtGatewayProviderModelPreview.MouseDown += GatewayProviderModelPreview_MouseDown;
        // 
        // _gatewayModelPreviewContextMenu
        // 
        _gatewayModelPreviewContextMenu.Items.AddRange(new ToolStripItem[] { _gatewaySetAsChatModelMenuItem, _gatewaySetAsEmbeddingModelMenuItem });
        _gatewayModelPreviewContextMenu.Name = "_gatewayModelPreviewContextMenu";
        _gatewayModelPreviewContextMenu.Size = new Size(225, 48);
        // 
        // _gatewaySetAsChatModelMenuItem
        // 
        _gatewaySetAsChatModelMenuItem.Name = "_gatewaySetAsChatModelMenuItem";
        _gatewaySetAsChatModelMenuItem.Size = new Size(224, 22);
        _gatewaySetAsChatModelMenuItem.Text = "Set as Chat Model";
        _gatewaySetAsChatModelMenuItem.Click += GatewaySetAsChatModelMenuItem_Click;
        // 
        // _gatewaySetAsEmbeddingModelMenuItem
        // 
        _gatewaySetAsEmbeddingModelMenuItem.Name = "_gatewaySetAsEmbeddingModelMenuItem";
        _gatewaySetAsEmbeddingModelMenuItem.Size = new Size(224, 22);
        _gatewaySetAsEmbeddingModelMenuItem.Text = "Set as Embedding Model";
        _gatewaySetAsEmbeddingModelMenuItem.Click += GatewaySetAsEmbeddingModelMenuItem_Click;
        // 
        // _lblGatewayProviderModelMetadata
        // 
        _lblGatewayProviderModelMetadata.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderModelMetadata.Location = new Point(3, 344);
        _lblGatewayProviderModelMetadata.Name = "_lblGatewayProviderModelMetadata";
        _lblGatewayProviderModelMetadata.Size = new Size(144, 20);
        _lblGatewayProviderModelMetadata.TabIndex = 10;
        _lblGatewayProviderModelMetadata.Text = "Model Info:";
        _lblGatewayProviderModelMetadata.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtGatewayProviderModelMetadata
        // 
        _txtGatewayProviderModelMetadata.Dock = DockStyle.Fill;
        _txtGatewayProviderModelMetadata.Location = new Point(153, 347);
        _txtGatewayProviderModelMetadata.Multiline = true;
        _txtGatewayProviderModelMetadata.Name = "_txtGatewayProviderModelMetadata";
        _txtGatewayProviderModelMetadata.ReadOnly = true;
        _txtGatewayProviderModelMetadata.ScrollBars = ScrollBars.Vertical;
        _txtGatewayProviderModelMetadata.Size = new Size(532, 22);
        _txtGatewayProviderModelMetadata.TabIndex = 11;
        // 
        // _btnGatewayCopyModelMetadata
        // 
        _btnGatewayCopyModelMetadata.AutoSize = true;
        _btnGatewayCopyModelMetadata.Location = new Point(153, 375);
        _btnGatewayCopyModelMetadata.Name = "_btnGatewayCopyModelMetadata";
        _btnGatewayCopyModelMetadata.Size = new Size(134, 27);
        _btnGatewayCopyModelMetadata.TabIndex = 12;
        _btnGatewayCopyModelMetadata.Text = "Copy Raw Summary";
        _btnGatewayCopyModelMetadata.UseVisualStyleBackColor = true;
        _btnGatewayCopyModelMetadata.Click += BtnGatewayCopyModelMetadata_Click;
        // 
        // _gatewayRouteTab
        // 
        _gatewayRouteTab.Controls.Add(_gatewayRouteEditorPanel);
        _gatewayRouteTab.Controls.Add(_txtGatewayRoutesPreview);
        _gatewayRouteTab.Controls.Add(_lblGatewayRoutesPreview);
        _gatewayRouteTab.Location = new Point(4, 26);
        _gatewayRouteTab.Name = "_gatewayRouteTab";
        _gatewayRouteTab.Padding = new Padding(8);
        _gatewayRouteTab.Size = new Size(735, 505);
        _gatewayRouteTab.TabIndex = 2;
        _gatewayRouteTab.Text = "Routes";
        // 
        // _gatewayRouteEditorPanel
        // 
        _gatewayRouteEditorPanel.ColumnCount = 2;
        _gatewayRouteEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _gatewayRouteEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gatewayRouteEditorPanel.Controls.Add(_lblGatewayRouteProvider, 0, 0);
        _gatewayRouteEditorPanel.Controls.Add(_cmbGatewayRouteProvider, 1, 0);
        _gatewayRouteEditorPanel.Controls.Add(_lblGatewayRouteMappings, 0, 1);
        _gatewayRouteEditorPanel.Controls.Add(_txtGatewayRouteMappings, 1, 1);
        _gatewayRouteEditorPanel.Controls.Add(_lblGatewayRouteValidation, 1, 2);
        _gatewayRouteEditorPanel.Dock = DockStyle.Top;
        _gatewayRouteEditorPanel.Location = new Point(8, 8);
        _gatewayRouteEditorPanel.Margin = new Padding(0);
        _gatewayRouteEditorPanel.Name = "_gatewayRouteEditorPanel";
        _gatewayRouteEditorPanel.RowCount = 3;
        _gatewayRouteEditorPanel.RowStyles.Add(new RowStyle());
        _gatewayRouteEditorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _gatewayRouteEditorPanel.RowStyles.Add(new RowStyle());
        _gatewayRouteEditorPanel.Size = new Size(719, 170);
        _gatewayRouteEditorPanel.TabIndex = 22;
        // 
        // _lblGatewayRouteProvider
        // 
        _lblGatewayRouteProvider.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayRouteProvider.Location = new Point(3, 4);
        _lblGatewayRouteProvider.Name = "_lblGatewayRouteProvider";
        _lblGatewayRouteProvider.Size = new Size(114, 23);
        _lblGatewayRouteProvider.TabIndex = 0;
        _lblGatewayRouteProvider.Text = "Provider:";
        _lblGatewayRouteProvider.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbGatewayRouteProvider
        // 
        _cmbGatewayRouteProvider.Dock = DockStyle.Fill;
        _cmbGatewayRouteProvider.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbGatewayRouteProvider.FormattingEnabled = true;
        _cmbGatewayRouteProvider.Location = new Point(123, 3);
        _cmbGatewayRouteProvider.Name = "_cmbGatewayRouteProvider";
        _cmbGatewayRouteProvider.Size = new Size(593, 25);
        _cmbGatewayRouteProvider.TabIndex = 1;
        _cmbGatewayRouteProvider.SelectedIndexChanged += CmbGatewayRouteProvider_SelectedIndexChanged;
        // 
        // _lblGatewayRouteMappings
        // 
        _lblGatewayRouteMappings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayRouteMappings.Location = new Point(3, 31);
        _lblGatewayRouteMappings.Name = "_lblGatewayRouteMappings";
        _lblGatewayRouteMappings.Size = new Size(114, 1);
        _lblGatewayRouteMappings.TabIndex = 2;
        _lblGatewayRouteMappings.Text = "Routes:";
        _lblGatewayRouteMappings.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtGatewayRouteMappings
        // 
        _txtGatewayRouteMappings.AcceptsReturn = true;
        _txtGatewayRouteMappings.Dock = DockStyle.Fill;
        _txtGatewayRouteMappings.Location = new Point(123, 31);
        _txtGatewayRouteMappings.Margin = new Padding(3, 0, 3, 0);
        _txtGatewayRouteMappings.Multiline = true;
        _txtGatewayRouteMappings.Name = "_txtGatewayRouteMappings";
        _txtGatewayRouteMappings.PlaceholderText = "One route per line:\r\nqwen2.5:7b=gpt-4.1-mini\r\nclaude-local=claude-sonnet-4";
        _txtGatewayRouteMappings.ScrollBars = ScrollBars.Vertical;
        _txtGatewayRouteMappings.Size = new Size(593, 122);
        _txtGatewayRouteMappings.TabIndex = 3;
        _txtGatewayRouteMappings.WordWrap = false;
        _txtGatewayRouteMappings.TextChanged += TxtGatewayRouteMappings_TextChanged;
        // 
        // _lblGatewayRouteValidation
        // 
        _lblGatewayRouteValidation.AutoSize = true;
        _lblGatewayRouteValidation.Dock = DockStyle.Fill;
        _lblGatewayRouteValidation.ForeColor = SystemColors.GrayText;
        _lblGatewayRouteValidation.Location = new Point(123, 153);
        _lblGatewayRouteValidation.Name = "_lblGatewayRouteValidation";
        _lblGatewayRouteValidation.Size = new Size(593, 17);
        _lblGatewayRouteValidation.TabIndex = 4;
        _lblGatewayRouteValidation.Text = "Route format: local=upstream";
        // 
        // _txtGatewayRoutesPreview
        // 
        _txtGatewayRoutesPreview.AcceptsReturn = true;
        _txtGatewayRoutesPreview.Dock = DockStyle.Fill;
        _txtGatewayRoutesPreview.Location = new Point(8, 8);
        _txtGatewayRoutesPreview.Margin = new Padding(0);
        _txtGatewayRoutesPreview.Multiline = true;
        _txtGatewayRoutesPreview.Name = "_txtGatewayRoutesPreview";
        _txtGatewayRoutesPreview.PlaceholderText = "Gateway routes preview (design-time visible, runtime generated):\r\ndefault -> gpt-4.1-mini\r\nanthropic -> claude-sonnet";
        _txtGatewayRoutesPreview.ReadOnly = true;
        _txtGatewayRoutesPreview.ScrollBars = ScrollBars.Vertical;
        _txtGatewayRoutesPreview.Size = new Size(719, 489);
        _txtGatewayRoutesPreview.TabIndex = 24;
        _txtGatewayRoutesPreview.WordWrap = false;
        // 
        // _lblGatewayRoutesPreview
        // 
        _lblGatewayRoutesPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayRoutesPreview.Location = new Point(16, 190);
        _lblGatewayRoutesPreview.Name = "_lblGatewayRoutesPreview";
        _lblGatewayRoutesPreview.Size = new Size(653, 23);
        _lblGatewayRoutesPreview.TabIndex = 23;
        _lblGatewayRoutesPreview.Text = "Gateway Routes:";
        _lblGatewayRoutesPreview.TextAlign = ContentAlignment.TopRight;
        // 
        // _gatewayDiagnosticsTab
        // 
        _gatewayDiagnosticsTab.Controls.Add(_localApiLoggingPanel);
        _gatewayDiagnosticsTab.Location = new Point(4, 26);
        _gatewayDiagnosticsTab.Name = "_gatewayDiagnosticsTab";
        _gatewayDiagnosticsTab.Padding = new Padding(8);
        _gatewayDiagnosticsTab.Size = new Size(735, 505);
        _gatewayDiagnosticsTab.TabIndex = 3;
        _gatewayDiagnosticsTab.Text = "Diagnostics";
        // 
        // _localApiLoggingPanel
        // 
        _localApiLoggingPanel.AutoSize = true;
        _localApiLoggingPanel.Controls.Add(_chkLocalApiRequestResponseLogging);
        _localApiLoggingPanel.Controls.Add(_chkLocalApiIncludeBodies);
        _localApiLoggingPanel.Controls.Add(_chkLocalApiIncludeErrorDiagnostics);
        _localApiLoggingPanel.Controls.Add(_lblLocalApiMaxBodyChars);
        _localApiLoggingPanel.Controls.Add(_numLocalApiMaxBodyChars);
        _localApiLoggingPanel.Dock = DockStyle.Top;
        _localApiLoggingPanel.Location = new Point(8, 8);
        _localApiLoggingPanel.Name = "_localApiLoggingPanel";
        _localApiLoggingPanel.Size = new Size(719, 58);
        _localApiLoggingPanel.TabIndex = 26;
        // 
        // _chkLocalApiRequestResponseLogging
        // 
        _chkLocalApiRequestResponseLogging.AutoSize = true;
        _chkLocalApiRequestResponseLogging.Location = new Point(3, 5);
        _chkLocalApiRequestResponseLogging.Margin = new Padding(3, 5, 16, 3);
        _chkLocalApiRequestResponseLogging.Name = "_chkLocalApiRequestResponseLogging";
        _chkLocalApiRequestResponseLogging.Size = new Size(222, 21);
        _chkLocalApiRequestResponseLogging.TabIndex = 0;
        _chkLocalApiRequestResponseLogging.Text = "Enable request/response logging";
        // 
        // _chkLocalApiIncludeBodies
        // 
        _chkLocalApiIncludeBodies.AutoSize = true;
        _chkLocalApiIncludeBodies.Location = new Point(244, 5);
        _chkLocalApiIncludeBodies.Margin = new Padding(3, 5, 16, 3);
        _chkLocalApiIncludeBodies.Name = "_chkLocalApiIncludeBodies";
        _chkLocalApiIncludeBodies.Size = new Size(113, 21);
        _chkLocalApiIncludeBodies.TabIndex = 1;
        _chkLocalApiIncludeBodies.Text = "Include bodies";
        // 
        // _chkLocalApiIncludeErrorDiagnostics
        // 
        _chkLocalApiIncludeErrorDiagnostics.AutoSize = true;
        _chkLocalApiIncludeErrorDiagnostics.Location = new Point(376, 5);
        _chkLocalApiIncludeErrorDiagnostics.Margin = new Padding(3, 5, 16, 3);
        _chkLocalApiIncludeErrorDiagnostics.Name = "_chkLocalApiIncludeErrorDiagnostics";
        _chkLocalApiIncludeErrorDiagnostics.Size = new Size(251, 21);
        _chkLocalApiIncludeErrorDiagnostics.TabIndex = 2;
        _chkLocalApiIncludeErrorDiagnostics.Text = "Include error diagnostics in responses";
        // 
        // _lblLocalApiMaxBodyChars
        // 
        _lblLocalApiMaxBodyChars.Anchor = AnchorStyles.Left;
        _lblLocalApiMaxBodyChars.AutoSize = true;
        _lblLocalApiMaxBodyChars.Location = new Point(3, 36);
        _lblLocalApiMaxBodyChars.Margin = new Padding(3, 6, 3, 3);
        _lblLocalApiMaxBodyChars.Name = "_lblLocalApiMaxBodyChars";
        _lblLocalApiMaxBodyChars.Size = new Size(105, 17);
        _lblLocalApiMaxBodyChars.TabIndex = 3;
        _lblLocalApiMaxBodyChars.Text = "Max body chars:";
        // 
        // _numLocalApiMaxBodyChars
        // 
        _numLocalApiMaxBodyChars.Location = new Point(114, 32);
        _numLocalApiMaxBodyChars.Maximum = new decimal(new int[] { 20000, 0, 0, 0 });
        _numLocalApiMaxBodyChars.Minimum = new decimal(new int[] { 256, 0, 0, 0 });
        _numLocalApiMaxBodyChars.Name = "_numLocalApiMaxBodyChars";
        _numLocalApiMaxBodyChars.Size = new Size(90, 23);
        _numLocalApiMaxBodyChars.TabIndex = 4;
        _numLocalApiMaxBodyChars.Value = new decimal(new int[] { 4000, 0, 0, 0 });
        // 
        // _dnsRedirectTab
        // 
        _dnsRedirectTab.Controls.Add(_dnsRedirectPanel);
        _dnsRedirectTab.Location = new Point(4, 26);
        _dnsRedirectTab.Name = "_dnsRedirectTab";
        _dnsRedirectTab.Size = new Size(769, 596);
        _dnsRedirectTab.TabIndex = 3;
        _dnsRedirectTab.Text = "DNS Redirect";
        // 
        // _dnsRedirectPanel
        // 
        _dnsRedirectPanel.ColumnCount = 2;
        _dnsRedirectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _dnsRedirectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _dnsRedirectPanel.Controls.Add(_grpRedirectMode, 0, 0);
        _dnsRedirectPanel.Controls.Add(_lblHostsUrl, 0, 1);
        _dnsRedirectPanel.Controls.Add(_txtHostsUrl, 1, 1);
        _dnsRedirectPanel.Controls.Add(_hostsRedirectBtnPanel, 1, 2);
        _dnsRedirectPanel.Controls.Add(_lblHostsStatus, 0, 3);
        _dnsRedirectPanel.Controls.Add(_autoFetchPanel, 0, 4);
        _dnsRedirectPanel.Controls.Add(_lblRefreshDomains, 0, 5);
        _dnsRedirectPanel.Controls.Add(_txtRefreshDomains, 1, 5);
        _dnsRedirectPanel.Controls.Add(_lvIpResults, 0, 6);
        _dnsRedirectPanel.Dock = DockStyle.Fill;
        _dnsRedirectPanel.Location = new Point(0, 0);
        _dnsRedirectPanel.Name = "_dnsRedirectPanel";
        _dnsRedirectPanel.Padding = new Padding(10);
        _dnsRedirectPanel.RowCount = 7;
        _dnsRedirectPanel.RowStyles.Add(new RowStyle());
        _dnsRedirectPanel.RowStyles.Add(new RowStyle());
        _dnsRedirectPanel.RowStyles.Add(new RowStyle());
        _dnsRedirectPanel.RowStyles.Add(new RowStyle());
        _dnsRedirectPanel.RowStyles.Add(new RowStyle());
        _dnsRedirectPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30.2670631F));
        _dnsRedirectPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 69.73294F));
        _dnsRedirectPanel.Size = new Size(769, 596);
        _dnsRedirectPanel.TabIndex = 0;
        // 
        // _grpRedirectMode
        // 
        _grpRedirectMode.AutoSize = true;
        _grpRedirectMode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _dnsRedirectPanel.SetColumnSpan(_grpRedirectMode, 2);
        _grpRedirectMode.Controls.Add(_modePanelRedirectMode);
        _grpRedirectMode.Dock = DockStyle.Fill;
        _grpRedirectMode.Location = new Point(13, 13);
        _grpRedirectMode.Margin = new Padding(3, 3, 3, 10);
        _grpRedirectMode.Name = "_grpRedirectMode";
        _grpRedirectMode.Padding = new Padding(8, 5, 8, 8);
        _grpRedirectMode.Size = new Size(743, 105);
        _grpRedirectMode.TabIndex = 0;
        _grpRedirectMode.TabStop = false;
        _grpRedirectMode.Text = "Redirect Mode";
        // 
        // _modePanelRedirectMode
        // 
        _modePanelRedirectMode.AutoSize = true;
        _modePanelRedirectMode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _modePanelRedirectMode.Controls.Add(_rdoDnsInterception);
        _modePanelRedirectMode.Controls.Add(_rdoHostsFile);
        _modePanelRedirectMode.Controls.Add(_lblHostsFileWarning);
        _modePanelRedirectMode.Dock = DockStyle.Fill;
        _modePanelRedirectMode.FlowDirection = FlowDirection.TopDown;
        _modePanelRedirectMode.Location = new Point(8, 21);
        _modePanelRedirectMode.Name = "_modePanelRedirectMode";
        _modePanelRedirectMode.Size = new Size(727, 76);
        _modePanelRedirectMode.TabIndex = 0;
        _modePanelRedirectMode.WrapContents = false;
        // 
        // _rdoDnsInterception
        // 
        _rdoDnsInterception.AutoSize = true;
        _rdoDnsInterception.Checked = true;
        _rdoDnsInterception.Location = new Point(3, 3);
        _rdoDnsInterception.Margin = new Padding(3, 3, 3, 5);
        _rdoDnsInterception.Name = "_rdoDnsInterception";
        _rdoDnsInterception.Size = new Size(327, 21);
        _rdoDnsInterception.TabIndex = 0;
        _rdoDnsInterception.TabStop = true;
        _rdoDnsInterception.Text = "DNS Interception (packet-level, no file modification)";
        // 
        // _rdoHostsFile
        // 
        _rdoHostsFile.AutoSize = true;
        _rdoHostsFile.Location = new Point(3, 32);
        _rdoHostsFile.Name = "_rdoHostsFile";
        _rdoHostsFile.Size = new Size(419, 21);
        _rdoHostsFile.TabIndex = 1;
        _rdoHostsFile.Text = "System Hosts File (write to C:\\Windows\\System32\\drivers\\etc\\hosts)";
        // 
        // _lblHostsFileWarning
        // 
        _lblHostsFileWarning.AutoSize = true;
        _lblHostsFileWarning.ForeColor = SystemColors.GrayText;
        _lblHostsFileWarning.Location = new Point(20, 56);
        _lblHostsFileWarning.Margin = new Padding(20, 0, 3, 3);
        _lblHostsFileWarning.Name = "_lblHostsFileWarning";
        _lblHostsFileWarning.Size = new Size(428, 17);
        _lblHostsFileWarning.TabIndex = 2;
        _lblHostsFileWarning.Text = "⚠ Requires Administrator privileges. Creates backup before modifying.";
        _lblHostsFileWarning.Visible = false;
        // 
        // _lblHostsUrl
        // 
        _lblHostsUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblHostsUrl.Location = new Point(13, 133);
        _lblHostsUrl.Name = "_lblHostsUrl";
        _lblHostsUrl.Size = new Size(144, 23);
        _lblHostsUrl.TabIndex = 1;
        _lblHostsUrl.Text = "Hosts URL:";
        _lblHostsUrl.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtHostsUrl
        // 
        _txtHostsUrl.Dock = DockStyle.Fill;
        _txtHostsUrl.Location = new Point(165, 133);
        _txtHostsUrl.Margin = new Padding(5);
        _txtHostsUrl.Name = "_txtHostsUrl";
        _txtHostsUrl.Size = new Size(589, 23);
        _txtHostsUrl.TabIndex = 2;
        _txtHostsUrl.Text = "https://raw.hellogithub.com/hosts";
        // 
        // _hostsRedirectBtnPanel
        // 
        _hostsRedirectBtnPanel.AutoSize = true;
        _hostsRedirectBtnPanel.Controls.Add(_btnRefreshHosts);
        _hostsRedirectBtnPanel.Controls.Add(_btnFetchIps);
        _hostsRedirectBtnPanel.Controls.Add(_chkRetestSlowOrTimeoutOnly);
        _hostsRedirectBtnPanel.Dock = DockStyle.Fill;
        _hostsRedirectBtnPanel.Location = new Point(165, 166);
        _hostsRedirectBtnPanel.Margin = new Padding(5);
        _hostsRedirectBtnPanel.Name = "_hostsRedirectBtnPanel";
        _hostsRedirectBtnPanel.Size = new Size(589, 36);
        _hostsRedirectBtnPanel.TabIndex = 3;
        // 
        // _btnRefreshHosts
        // 
        _btnRefreshHosts.Location = new Point(3, 3);
        _btnRefreshHosts.Name = "_btnRefreshHosts";
        _btnRefreshHosts.Size = new Size(120, 30);
        _btnRefreshHosts.TabIndex = 0;
        _btnRefreshHosts.Text = "Refresh Hosts Now";
        _btnRefreshHosts.Click += BtnRefreshHosts_Click;
        // 
        // _btnFetchIps
        // 
        _btnFetchIps.Location = new Point(129, 3);
        _btnFetchIps.Name = "_btnFetchIps";
        _btnFetchIps.Size = new Size(130, 30);
        _btnFetchIps.TabIndex = 1;
        _btnFetchIps.Text = "Fetch IPs via DoH";
        _btnFetchIps.Click += BtnFetchIps_Click;
        // 
        // _chkRetestSlowOrTimeoutOnly
        // 
        _chkRetestSlowOrTimeoutOnly.AutoSize = true;
        _chkRetestSlowOrTimeoutOnly.Location = new Point(265, 7);
        _chkRetestSlowOrTimeoutOnly.Margin = new Padding(3, 7, 3, 3);
        _chkRetestSlowOrTimeoutOnly.Name = "_chkRetestSlowOrTimeoutOnly";
        _chkRetestSlowOrTimeoutOnly.Size = new Size(267, 21);
        _chkRetestSlowOrTimeoutOnly.TabIndex = 2;
        _chkRetestSlowOrTimeoutOnly.Text = "Only retest timeout/high-latency domains";
        // 
        // _lblHostsStatus
        // 
        _lblHostsStatus.AutoSize = true;
        _dnsRedirectPanel.SetColumnSpan(_lblHostsStatus, 2);
        _lblHostsStatus.Location = new Point(13, 212);
        _lblHostsStatus.Margin = new Padding(3, 5, 3, 3);
        _lblHostsStatus.Name = "_lblHostsStatus";
        _lblHostsStatus.Size = new Size(117, 17);
        _lblHostsStatus.TabIndex = 4;
        _lblHostsStatus.Text = "Status: Not loaded";
        // 
        // _autoFetchPanel
        // 
        _autoFetchPanel.AutoSize = true;
        _dnsRedirectPanel.SetColumnSpan(_autoFetchPanel, 2);
        _autoFetchPanel.Controls.Add(_chkAutoFetch);
        _autoFetchPanel.Controls.Add(_numAutoFetchInterval);
        _autoFetchPanel.Controls.Add(_lblAutoFetchMinutes);
        _autoFetchPanel.Dock = DockStyle.Fill;
        _autoFetchPanel.Location = new Point(13, 235);
        _autoFetchPanel.Name = "_autoFetchPanel";
        _autoFetchPanel.Size = new Size(743, 29);
        _autoFetchPanel.TabIndex = 6;
        _autoFetchPanel.WrapContents = false;
        // 
        // _chkAutoFetch
        // 
        _chkAutoFetch.AutoSize = true;
        _chkAutoFetch.Location = new Point(3, 5);
        _chkAutoFetch.Margin = new Padding(3, 5, 5, 3);
        _chkAutoFetch.Name = "_chkAutoFetch";
        _chkAutoFetch.Size = new Size(135, 21);
        _chkAutoFetch.TabIndex = 0;
        _chkAutoFetch.Text = "Auto-refresh every";
        _chkAutoFetch.CheckedChanged += ChkAutoFetch_CheckedChanged;
        // 
        // _numAutoFetchInterval
        // 
        _numAutoFetchInterval.Location = new Point(146, 3);
        _numAutoFetchInterval.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
        _numAutoFetchInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _numAutoFetchInterval.Name = "_numAutoFetchInterval";
        _numAutoFetchInterval.Size = new Size(65, 23);
        _numAutoFetchInterval.TabIndex = 1;
        _numAutoFetchInterval.Value = new decimal(new int[] { 60, 0, 0, 0 });
        // 
        // _lblAutoFetchMinutes
        // 
        _lblAutoFetchMinutes.AutoSize = true;
        _lblAutoFetchMinutes.Location = new Point(217, 6);
        _lblAutoFetchMinutes.Margin = new Padding(3, 6, 3, 0);
        _lblAutoFetchMinutes.Name = "_lblAutoFetchMinutes";
        _lblAutoFetchMinutes.Size = new Size(53, 17);
        _lblAutoFetchMinutes.TabIndex = 2;
        _lblAutoFetchMinutes.Text = "minutes";
        // 
        // _lblRefreshDomains
        // 
        _lblRefreshDomains.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblRefreshDomains.Location = new Point(13, 267);
        _lblRefreshDomains.Name = "_lblRefreshDomains";
        _lblRefreshDomains.Size = new Size(144, 23);
        _lblRefreshDomains.TabIndex = 6;
        _lblRefreshDomains.Text = "Refresh Domains:";
        _lblRefreshDomains.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtRefreshDomains
        // 
        _txtRefreshDomains.AcceptsReturn = true;
        _txtRefreshDomains.Dock = DockStyle.Fill;
        _txtRefreshDomains.Location = new Point(165, 272);
        _txtRefreshDomains.Margin = new Padding(5);
        _txtRefreshDomains.Multiline = true;
        _txtRefreshDomains.Name = "_txtRefreshDomains";
        _txtRefreshDomains.PlaceholderText = "\nalive.github.com\ngithub.com";
        _txtRefreshDomains.ScrollBars = ScrollBars.Vertical;
        _txtRefreshDomains.Size = new Size(589, 86);
        _txtRefreshDomains.TabIndex = 7;
        _txtRefreshDomains.WordWrap = false;
        // 
        // _lvIpResults
        // 
        _dnsRedirectPanel.SetColumnSpan(_lvIpResults, 2);
        _lvIpResults.Dock = DockStyle.Fill;
        _lvIpResults.FullRowSelect = true;
        _lvIpResults.GridLines = true;
        _lvIpResults.Location = new Point(13, 366);
        _lvIpResults.Name = "_lvIpResults";
        _lvIpResults.Size = new Size(743, 217);
        _lvIpResults.TabIndex = 8;
        _lvIpResults.UseCompatibleStateImageBehavior = false;
        _lvIpResults.View = View.Details;
        // 
        // _logsTab
        // 
        _logsTab.Controls.Add(_logPanel);
        _logsTab.Location = new Point(4, 26);
        _logsTab.Name = "_logsTab";
        _logsTab.Size = new Size(769, 596);
        _logsTab.TabIndex = 1;
        _logsTab.Text = "Logs";
        // 
        // _logPanel
        // 
        _logPanel.ColumnCount = 1;
        _logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _logPanel.Controls.Add(_rtbLogs, 0, 0);
        _logPanel.Controls.Add(_btnClearPanel, 0, 1);
        _logPanel.Dock = DockStyle.Fill;
        _logPanel.Location = new Point(0, 0);
        _logPanel.Margin = new Padding(0);
        _logPanel.Name = "_logPanel";
        _logPanel.RowCount = 2;
        _logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _logPanel.Size = new Size(769, 596);
        _logPanel.TabIndex = 0;
        // 
        // _rtbLogs
        // 
        _rtbLogs.BackColor = Color.Black;
        _rtbLogs.Dock = DockStyle.Fill;
        _rtbLogs.Font = new Font("Courier New", 9F);
        _rtbLogs.ForeColor = Color.Lime;
        _rtbLogs.Location = new Point(3, 3);
        _rtbLogs.Name = "_rtbLogs";
        _rtbLogs.ReadOnly = true;
        _rtbLogs.Size = new Size(763, 550);
        _rtbLogs.TabIndex = 0;
        _rtbLogs.Text = "";
        _rtbLogs.WordWrap = false;
        // 
        // _btnClearPanel
        // 
        _btnClearPanel.Controls.Add(_btnClearLogs);
        _btnClearPanel.Dock = DockStyle.Fill;
        _btnClearPanel.FlowDirection = FlowDirection.RightToLeft;
        _btnClearPanel.Location = new Point(5, 561);
        _btnClearPanel.Margin = new Padding(5);
        _btnClearPanel.Name = "_btnClearPanel";
        _btnClearPanel.Size = new Size(759, 30);
        _btnClearPanel.TabIndex = 1;
        // 
        // _btnClearLogs
        // 
        _btnClearLogs.Location = new Point(676, 3);
        _btnClearLogs.Name = "_btnClearLogs";
        _btnClearLogs.Size = new Size(80, 30);
        _btnClearLogs.TabIndex = 0;
        _btnClearLogs.Text = "Clear Logs";
        _btnClearLogs.Click += BtnClearLogs_Click;
        // 
        // _aboutTab
        // 
        _aboutTab.Controls.Add(_aboutScrollPanel);
        _aboutTab.Location = new Point(4, 26);
        _aboutTab.Name = "_aboutTab";
        _aboutTab.Size = new Size(769, 596);
        _aboutTab.TabIndex = 4;
        _aboutTab.Text = "About";
        // 
        // _aboutScrollPanel
        // 
        _aboutScrollPanel.AutoScroll = true;
        _aboutScrollPanel.Controls.Add(_aboutContentPanel);
        _aboutScrollPanel.Dock = DockStyle.Fill;
        _aboutScrollPanel.Location = new Point(0, 0);
        _aboutScrollPanel.Margin = new Padding(0);
        _aboutScrollPanel.Name = "_aboutScrollPanel";
        _aboutScrollPanel.Padding = new Padding(10);
        _aboutScrollPanel.Size = new Size(769, 596);
        _aboutScrollPanel.TabIndex = 0;
        // 
        // _aboutContentPanel
        // 
        _aboutContentPanel.AutoSize = true;
        _aboutContentPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _aboutContentPanel.ColumnCount = 1;
        _aboutContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _aboutContentPanel.Controls.Add(titleLabel, 0, 0);
        _aboutContentPanel.Controls.Add(_versionPanel, 0, 1);
        _aboutContentPanel.Controls.Add(descLabel, 0, 2);
        _aboutContentPanel.Controls.Add(descContentLabel, 0, 3);
        _aboutContentPanel.Controls.Add(contribLabel, 0, 4);
        _aboutContentPanel.Controls.Add(contribContentLabel, 0, 5);
        _aboutContentPanel.Controls.Add(techLabel, 0, 6);
        _aboutContentPanel.Controls.Add(techContentLabel, 0, 7);
        _aboutContentPanel.Dock = DockStyle.Top;
        _aboutContentPanel.Location = new Point(10, 10);
        _aboutContentPanel.Margin = new Padding(0);
        _aboutContentPanel.Name = "_aboutContentPanel";
        _aboutContentPanel.Padding = new Padding(10);
        _aboutContentPanel.RowCount = 8;
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.RowStyles.Add(new RowStyle());
        _aboutContentPanel.Size = new Size(732, 667);
        _aboutContentPanel.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Location = new Point(10, 10);
        titleLabel.Margin = new Padding(0, 0, 0, 10);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(126, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "TrafficPilot";
        // 
        // _versionPanel
        // 
        _versionPanel.AutoSize = true;
        _versionPanel.Controls.Add(versionLabel);
        _versionPanel.Controls.Add(_lblLatestVersion);
        _versionPanel.Controls.Add(_btnCheckUpdate);
        _versionPanel.Controls.Add(_lblUpdateStatus);
        _versionPanel.Location = new Point(10, 50);
        _versionPanel.Margin = new Padding(0, 0, 0, 15);
        _versionPanel.Name = "_versionPanel";
        _versionPanel.Size = new Size(210, 28);
        _versionPanel.TabIndex = 1;
        _versionPanel.WrapContents = false;
        // 
        // versionLabel
        // 
        versionLabel.AutoSize = true;
        versionLabel.ForeColor = SystemColors.GrayText;
        versionLabel.Location = new Point(0, 0);
        versionLabel.Margin = new Padding(0);
        versionLabel.Name = "versionLabel";
        versionLabel.Size = new Size(86, 17);
        versionLabel.TabIndex = 1;
        versionLabel.Text = "Version: 1.0.0";
        // 
        // _lblLatestVersion
        // 
        _lblLatestVersion.AutoSize = true;
        _lblLatestVersion.ForeColor = SystemColors.GrayText;
        _lblLatestVersion.Location = new Point(86, 0);
        _lblLatestVersion.Margin = new Padding(0, 0, 8, 0);
        _lblLatestVersion.Name = "_lblLatestVersion";
        _lblLatestVersion.Size = new Size(12, 17);
        _lblLatestVersion.TabIndex = 2;
        _lblLatestVersion.Text = " ";
        // 
        // _btnCheckUpdate
        // 
        _btnCheckUpdate.Location = new Point(106, 0);
        _btnCheckUpdate.Margin = new Padding(0, 0, 8, 0);
        _btnCheckUpdate.Name = "_btnCheckUpdate";
        _btnCheckUpdate.Size = new Size(96, 28);
        _btnCheckUpdate.TabIndex = 8;
        _btnCheckUpdate.Text = "Update Now";
        _btnCheckUpdate.Visible = false;
        _btnCheckUpdate.Click += BtnCheckUpdate_Click;
        // 
        // _lblUpdateStatus
        // 
        _lblUpdateStatus.AutoSize = true;
        _lblUpdateStatus.ForeColor = SystemColors.GrayText;
        _lblUpdateStatus.Location = new Point(210, 5);
        _lblUpdateStatus.Margin = new Padding(0, 5, 0, 0);
        _lblUpdateStatus.Name = "_lblUpdateStatus";
        _lblUpdateStatus.Size = new Size(0, 17);
        _lblUpdateStatus.TabIndex = 9;
        // 
        // descLabel
        // 
        descLabel.AutoSize = true;
        descLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        descLabel.Location = new Point(10, 93);
        descLabel.Margin = new Padding(0, 0, 0, 5);
        descLabel.Name = "descLabel";
        descLabel.Size = new Size(142, 20);
        descLabel.TabIndex = 2;
        descLabel.Text = "Project Description";
        // 
        // descContentLabel
        // 
        descContentLabel.AutoSize = true;
        descContentLabel.Location = new Point(20, 118);
        descContentLabel.Margin = new Padding(10, 0, 0, 15);
        descContentLabel.MaximumSize = new Size(450, 0);
        descContentLabel.Name = "descContentLabel";
        descContentLabel.Size = new Size(450, 306);
        descContentLabel.TabIndex = 3;
        descContentLabel.Text = resources.GetString("descContentLabel.Text");
        // 
        // contribLabel
        // 
        contribLabel.AutoSize = true;
        contribLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        contribLabel.Location = new Point(10, 439);
        contribLabel.Margin = new Padding(0, 0, 0, 5);
        contribLabel.Name = "contribLabel";
        contribLabel.Size = new Size(236, 20);
        contribLabel.TabIndex = 4;
        contribLabel.Text = "Contributors & Acknowledgments";
        // 
        // contribContentLabel
        // 
        contribContentLabel.AutoSize = true;
        contribContentLabel.Location = new Point(20, 464);
        contribContentLabel.Margin = new Padding(10, 0, 0, 15);
        contribContentLabel.MaximumSize = new Size(450, 0);
        contribContentLabel.Name = "contribContentLabel";
        contribContentLabel.Size = new Size(299, 68);
        contribContentLabel.TabIndex = 5;
        contribContentLabel.Text = "鈥?Original Author: maikebing\n鈥?Repository: github.com/maikebing/TrafficPilot\n鈥?WinDivert: Windows Packet Divert library\n鈥?Contributors: Community members and testers";
        // 
        // techLabel
        // 
        techLabel.AutoSize = true;
        techLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        techLabel.Location = new Point(10, 547);
        techLabel.Margin = new Padding(0, 0, 0, 5);
        techLabel.Name = "techLabel";
        techLabel.Size = new Size(162, 20);
        techLabel.TabIndex = 6;
        techLabel.Text = "Technical Information";
        // 
        // techContentLabel
        // 
        techContentLabel.AutoSize = true;
        techContentLabel.Location = new Point(20, 572);
        techContentLabel.Margin = new Padding(10, 0, 0, 0);
        techContentLabel.MaximumSize = new Size(450, 0);
        techContentLabel.Name = "techContentLabel";
        techContentLabel.Size = new Size(135, 85);
        techContentLabel.TabIndex = 7;
        techContentLabel.Text = "Platform: Windows\n.NET Version: .NET 10\nC# Version: 14.0\nArchitecture: x64\nLicense: Open Source";
        // 
        // _statusPanel
        // 
        _statusPanel.AutoScroll = true;
        _statusPanel.Controls.Add(_lblStatus);
        _statusPanel.Controls.Add(_lblStats);
        _statusPanel.Controls.Add(lblBytes);
        _statusPanel.Controls.Add(label1);
        _statusPanel.Controls.Add(_btnStartStop);
        _statusPanel.Dock = DockStyle.Fill;
        _statusPanel.Location = new Point(5, 636);
        _statusPanel.Margin = new Padding(0);
        _statusPanel.Name = "_statusPanel";
        _statusPanel.Size = new Size(777, 50);
        _statusPanel.TabIndex = 1;
        _statusPanel.WrapContents = false;
        // 
        // _lblStatus
        // 
        _lblStatus.Location = new Point(3, 0);
        _lblStatus.Name = "_lblStatus";
        _lblStatus.Size = new Size(211, 40);
        _lblStatus.TabIndex = 0;
        _lblStatus.Text = "Status: Stopped";
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblStats
        // 
        _lblStats.Location = new Point(237, 0);
        _lblStats.Margin = new Padding(20, 0, 0, 0);
        _lblStats.Name = "_lblStats";
        _lblStats.Size = new Size(129, 40);
        _lblStats.TabIndex = 1;
        _lblStats.Text = "Stats: -";
        _lblStats.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblBytes
        // 
        lblBytes.Location = new Point(369, 0);
        lblBytes.Name = "lblBytes";
        lblBytes.Size = new Size(100, 43);
        lblBytes.TabIndex = 3;
        lblBytes.Text = "bytes";
        lblBytes.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // label1
        // 
        label1.Location = new Point(475, 0);
        label1.Name = "label1";
        label1.Size = new Size(175, 40);
        label1.TabIndex = 2;
        // 
        // _btnStartStop
        // 
        _btnStartStop.BackColor = Color.LimeGreen;
        _btnStartStop.Location = new Point(656, 3);
        _btnStartStop.Name = "_btnStartStop";
        _btnStartStop.Size = new Size(100, 40);
        _btnStartStop.TabIndex = 0;
        _btnStartStop.Text = "Start Proxy";
        _btnStartStop.UseVisualStyleBackColor = false;
        _btnStartStop.Click += BtnStartStop_Click;
        // 
        // _btnAddGatewayProvider
        // 
        _btnAddGatewayProvider.Location = new Point(0, 0);
        _btnAddGatewayProvider.Name = "_btnAddGatewayProvider";
        _btnAddGatewayProvider.Size = new Size(75, 23);
        _btnAddGatewayProvider.TabIndex = 0;
        // 
        // _lblLocalApiProvider
        // 
        _lblLocalApiProvider.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiProvider.Location = new Point(13, 93);
        _lblLocalApiProvider.Name = "_lblLocalApiProvider";
        _lblLocalApiProvider.Size = new Size(134, 23);
        _lblLocalApiProvider.TabIndex = 3;
        _lblLocalApiProvider.Text = "Default Provider:";
        _lblLocalApiProvider.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _localApiProviderPanel
        // 
        _localApiProviderPanel.ColumnCount = 4;
        _localApiProviderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        _localApiProviderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 153F));
        _localApiProviderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122F));
        _localApiProviderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _localApiProviderPanel.Controls.Add(_lblLocalApiProtocol, 0, 0);
        _localApiProviderPanel.Controls.Add(_cmbLocalApiProviderProtocol, 1, 0);
        _localApiProviderPanel.Controls.Add(_lblLocalApiProviderName, 2, 0);
        _localApiProviderPanel.Controls.Add(_txtLocalApiProviderName, 3, 0);
        _localApiProviderPanel.Dock = DockStyle.Fill;
        _localApiProviderPanel.Location = new Point(153, 87);
        _localApiProviderPanel.Margin = new Padding(3, 0, 3, 3);
        _localApiProviderPanel.Name = "_localApiProviderPanel";
        _localApiProviderPanel.RowCount = 1;
        _localApiProviderPanel.RowStyles.Add(new RowStyle());
        _localApiProviderPanel.Size = new Size(603, 33);
        _localApiProviderPanel.TabIndex = 4;
        // 
        // _lblLocalApiProtocol
        // 
        _lblLocalApiProtocol.Dock = DockStyle.Fill;
        _lblLocalApiProtocol.Location = new Point(3, 0);
        _lblLocalApiProtocol.Name = "_lblLocalApiProtocol";
        _lblLocalApiProtocol.Size = new Size(99, 33);
        _lblLocalApiProtocol.TabIndex = 0;
        _lblLocalApiProtocol.Text = "Protocol:";
        _lblLocalApiProtocol.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbLocalApiProviderProtocol
        // 
        _cmbLocalApiProviderProtocol.Dock = DockStyle.Fill;
        _cmbLocalApiProviderProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbLocalApiProviderProtocol.FormattingEnabled = true;
        _cmbLocalApiProviderProtocol.Items.AddRange(new object[] { "OpenAICompatible", "Anthropic" });
        _cmbLocalApiProviderProtocol.Location = new Point(108, 4);
        _cmbLocalApiProviderProtocol.Margin = new Padding(3, 4, 3, 4);
        _cmbLocalApiProviderProtocol.Name = "_cmbLocalApiProviderProtocol";
        _cmbLocalApiProviderProtocol.Size = new Size(147, 25);
        _cmbLocalApiProviderProtocol.TabIndex = 1;
        // 
        // _lblLocalApiProviderName
        // 
        _lblLocalApiProviderName.Dock = DockStyle.Fill;
        _lblLocalApiProviderName.Location = new Point(261, 0);
        _lblLocalApiProviderName.Name = "_lblLocalApiProviderName";
        _lblLocalApiProviderName.Size = new Size(116, 33);
        _lblLocalApiProviderName.TabIndex = 2;
        _lblLocalApiProviderName.Text = "Provider Name:";
        _lblLocalApiProviderName.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtLocalApiProviderName
        // 
        _txtLocalApiProviderName.Dock = DockStyle.Fill;
        _txtLocalApiProviderName.Location = new Point(383, 5);
        _txtLocalApiProviderName.Margin = new Padding(3, 5, 3, 5);
        _txtLocalApiProviderName.Name = "_txtLocalApiProviderName";
        _txtLocalApiProviderName.PlaceholderText = "Third-party provider display name";
        _txtLocalApiProviderName.Size = new Size(217, 23);
        _txtLocalApiProviderName.TabIndex = 3;
        // 
        // _lblLocalApiDefaultModel
        // 
        _lblLocalApiDefaultModel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiDefaultModel.Location = new Point(13, 163);
        _lblLocalApiDefaultModel.Name = "_lblLocalApiDefaultModel";
        _lblLocalApiDefaultModel.Size = new Size(134, 23);
        _lblLocalApiDefaultModel.TabIndex = 7;
        _lblLocalApiDefaultModel.Text = "Default Model:";
        _lblLocalApiDefaultModel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbLocalApiDefaultModel
        // 
        _cmbLocalApiDefaultModel.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbLocalApiDefaultModel.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbLocalApiDefaultModel.Dock = DockStyle.Fill;
        _cmbLocalApiDefaultModel.FormattingEnabled = true;
        _cmbLocalApiDefaultModel.Location = new Point(153, 162);
        _cmbLocalApiDefaultModel.Name = "_cmbLocalApiDefaultModel";
        _cmbLocalApiDefaultModel.Size = new Size(603, 25);
        _cmbLocalApiDefaultModel.TabIndex = 8;
        // 
        // _lblLocalApiDefaultEmbeddingModel
        // 
        _lblLocalApiDefaultEmbeddingModel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiDefaultEmbeddingModel.Location = new Point(13, 194);
        _lblLocalApiDefaultEmbeddingModel.Name = "_lblLocalApiDefaultEmbeddingModel";
        _lblLocalApiDefaultEmbeddingModel.Size = new Size(134, 23);
        _lblLocalApiDefaultEmbeddingModel.TabIndex = 9;
        _lblLocalApiDefaultEmbeddingModel.Text = "Embedding Model:";
        _lblLocalApiDefaultEmbeddingModel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbLocalApiDefaultEmbeddingModel
        // 
        _cmbLocalApiDefaultEmbeddingModel.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cmbLocalApiDefaultEmbeddingModel.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cmbLocalApiDefaultEmbeddingModel.Dock = DockStyle.Fill;
        _cmbLocalApiDefaultEmbeddingModel.FormattingEnabled = true;
        _cmbLocalApiDefaultEmbeddingModel.Location = new Point(153, 193);
        _cmbLocalApiDefaultEmbeddingModel.Name = "_cmbLocalApiDefaultEmbeddingModel";
        _cmbLocalApiDefaultEmbeddingModel.Size = new Size(603, 25);
        _cmbLocalApiDefaultEmbeddingModel.TabIndex = 10;
        // 
        // _lblLocalApiAuthentication
        // 
        _lblLocalApiAuthentication.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiAuthentication.Location = new Point(13, 227);
        _lblLocalApiAuthentication.Name = "_lblLocalApiAuthentication";
        _lblLocalApiAuthentication.Size = new Size(134, 23);
        _lblLocalApiAuthentication.TabIndex = 11;
        _lblLocalApiAuthentication.Text = "Authentication:";
        _lblLocalApiAuthentication.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _localApiAuthPanel
        // 
        _localApiAuthPanel.ColumnCount = 4;
        _localApiAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _localApiAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _localApiAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _localApiAuthPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _localApiAuthPanel.Controls.Add(_lblLocalApiAuthType, 0, 0);
        _localApiAuthPanel.Controls.Add(_cmbLocalApiAuthType, 1, 0);
        _localApiAuthPanel.Controls.Add(_lblLocalApiAuthHeaderName, 2, 0);
        _localApiAuthPanel.Controls.Add(_txtLocalApiAuthHeaderName, 3, 0);
        _localApiAuthPanel.Dock = DockStyle.Fill;
        _localApiAuthPanel.Location = new Point(153, 221);
        _localApiAuthPanel.Margin = new Padding(3, 0, 3, 3);
        _localApiAuthPanel.Name = "_localApiAuthPanel";
        _localApiAuthPanel.RowCount = 1;
        _localApiAuthPanel.RowStyles.Add(new RowStyle());
        _localApiAuthPanel.Size = new Size(603, 33);
        _localApiAuthPanel.TabIndex = 12;
        // 
        // _lblLocalApiAuthType
        // 
        _lblLocalApiAuthType.Dock = DockStyle.Fill;
        _lblLocalApiAuthType.Location = new Point(3, 0);
        _lblLocalApiAuthType.Name = "_lblLocalApiAuthType";
        _lblLocalApiAuthType.Size = new Size(94, 33);
        _lblLocalApiAuthType.TabIndex = 0;
        _lblLocalApiAuthType.Text = "Auth Type:";
        _lblLocalApiAuthType.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbLocalApiAuthType
        // 
        _cmbLocalApiAuthType.Dock = DockStyle.Fill;
        _cmbLocalApiAuthType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbLocalApiAuthType.FormattingEnabled = true;
        _cmbLocalApiAuthType.Items.AddRange(new object[] { "Bearer", "Header", "Query" });
        _cmbLocalApiAuthType.Location = new Point(103, 4);
        _cmbLocalApiAuthType.Margin = new Padding(3, 4, 3, 4);
        _cmbLocalApiAuthType.Name = "_cmbLocalApiAuthType";
        _cmbLocalApiAuthType.Size = new Size(114, 25);
        _cmbLocalApiAuthType.TabIndex = 1;
        // 
        // _lblLocalApiAuthHeaderName
        // 
        _lblLocalApiAuthHeaderName.Dock = DockStyle.Fill;
        _lblLocalApiAuthHeaderName.Location = new Point(223, 0);
        _lblLocalApiAuthHeaderName.Name = "_lblLocalApiAuthHeaderName";
        _lblLocalApiAuthHeaderName.Size = new Size(114, 33);
        _lblLocalApiAuthHeaderName.TabIndex = 2;
        _lblLocalApiAuthHeaderName.Text = "Header / Query Name:";
        _lblLocalApiAuthHeaderName.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtLocalApiAuthHeaderName
        // 
        _txtLocalApiAuthHeaderName.Dock = DockStyle.Fill;
        _txtLocalApiAuthHeaderName.Location = new Point(343, 5);
        _txtLocalApiAuthHeaderName.Margin = new Padding(3, 5, 3, 5);
        _txtLocalApiAuthHeaderName.Name = "_txtLocalApiAuthHeaderName";
        _txtLocalApiAuthHeaderName.PlaceholderText = "Authorization / x-api-key / key";
        _txtLocalApiAuthHeaderName.Size = new Size(257, 23);
        _txtLocalApiAuthHeaderName.TabIndex = 3;
        // 
        // _lblLocalApiExtraHeaders
        // 
        _lblLocalApiExtraHeaders.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiExtraHeaders.Location = new Point(13, 286);
        _lblLocalApiExtraHeaders.Name = "_lblLocalApiExtraHeaders";
        _lblLocalApiExtraHeaders.Size = new Size(134, 23);
        _lblLocalApiExtraHeaders.TabIndex = 15;
        _lblLocalApiExtraHeaders.Text = "Extra Headers:";
        _lblLocalApiExtraHeaders.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtLocalApiAdditionalHeaders
        // 
        _txtLocalApiAdditionalHeaders.AcceptsReturn = true;
        _txtLocalApiAdditionalHeaders.Dock = DockStyle.Fill;
        _txtLocalApiAdditionalHeaders.Location = new Point(153, 286);
        _txtLocalApiAdditionalHeaders.Margin = new Padding(3, 0, 3, 3);
        _txtLocalApiAdditionalHeaders.Multiline = true;
        _txtLocalApiAdditionalHeaders.Name = "_txtLocalApiAdditionalHeaders";
        _txtLocalApiAdditionalHeaders.PlaceholderText = "Optional additional upstream headers, one per line:\r\nanthropic-version=2023-06-01\r\nX-Agent-ID=my-agent";
        _txtLocalApiAdditionalHeaders.ScrollBars = ScrollBars.Vertical;
        _txtLocalApiAdditionalHeaders.Size = new Size(603, 44);
        _txtLocalApiAdditionalHeaders.TabIndex = 16;
        _txtLocalApiAdditionalHeaders.WordWrap = false;
        // 
        // _lblLocalApiModelMappings
        // 
        _lblLocalApiModelMappings.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiModelMappings.Location = new Point(13, 333);
        _lblLocalApiModelMappings.Name = "_lblLocalApiModelMappings";
        _lblLocalApiModelMappings.Size = new Size(134, 23);
        _lblLocalApiModelMappings.TabIndex = 17;
        _lblLocalApiModelMappings.Text = "Legacy Mappings:";
        _lblLocalApiModelMappings.TextAlign = ContentAlignment.TopRight;
        // 
        // _txtLocalApiModelMappings
        // 
        _txtLocalApiModelMappings.AcceptsReturn = true;
        _txtLocalApiModelMappings.Dock = DockStyle.Fill;
        _txtLocalApiModelMappings.Location = new Point(153, 333);
        _txtLocalApiModelMappings.Margin = new Padding(3, 0, 3, 3);
        _txtLocalApiModelMappings.Multiline = true;
        _txtLocalApiModelMappings.Name = "_txtLocalApiModelMappings";
        _txtLocalApiModelMappings.PlaceholderText = "One mapping per line:\r\nqwen2.5:7b=gpt-4.1-mini\r\nllama3.2=claude-3-7-sonnet";
        _txtLocalApiModelMappings.ScrollBars = ScrollBars.Vertical;
        _txtLocalApiModelMappings.Size = new Size(603, 44);
        _txtLocalApiModelMappings.TabIndex = 18;
        _txtLocalApiModelMappings.WordWrap = false;
        // 
        // _lblGatewayProviderEditor
        // 
        _lblGatewayProviderEditor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderEditor.Location = new Point(13, 380);
        _lblGatewayProviderEditor.Name = "_lblGatewayProviderEditor";
        _lblGatewayProviderEditor.Size = new Size(134, 23);
        _lblGatewayProviderEditor.TabIndex = 19;
        _lblGatewayProviderEditor.Text = "Provider Editor:";
        _lblGatewayProviderEditor.TextAlign = ContentAlignment.TopRight;
        // 
        // _lblGatewayProviderDetails
        // 
        _lblGatewayProviderDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayProviderDetails.Location = new Point(3, 33);
        _lblGatewayProviderDetails.Name = "_lblGatewayProviderDetails";
        _lblGatewayProviderDetails.Size = new Size(114, 11);
        _lblGatewayProviderDetails.TabIndex = 3;
        _lblGatewayProviderDetails.Text = "Details:";
        _lblGatewayProviderDetails.TextAlign = ContentAlignment.TopRight;
        // 
        // _lblGatewayRouteEditor
        // 
        _lblGatewayRouteEditor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblGatewayRouteEditor.Location = new Point(13, 427);
        _lblGatewayRouteEditor.Name = "_lblGatewayRouteEditor";
        _lblGatewayRouteEditor.Size = new Size(134, 23);
        _lblGatewayRouteEditor.TabIndex = 21;
        _lblGatewayRouteEditor.Text = "Route Editor:";
        _lblGatewayRouteEditor.TextAlign = ContentAlignment.TopRight;
        // 
        // _lblLocalApiDiagnostics
        // 
        _lblLocalApiDiagnostics.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblLocalApiDiagnostics.Location = new Point(13, 542);
        _lblLocalApiDiagnostics.Name = "_lblLocalApiDiagnostics";
        _lblLocalApiDiagnostics.Size = new Size(134, 23);
        _lblLocalApiDiagnostics.TabIndex = 25;
        _lblLocalApiDiagnostics.Text = "Diagnostics:";
        _lblLocalApiDiagnostics.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _chkDNSRedirectEnabled
        // 
        _chkDNSRedirectEnabled.Location = new Point(0, 0);
        _chkDNSRedirectEnabled.Name = "_chkDNSRedirectEnabled";
        _chkDNSRedirectEnabled.Size = new Size(104, 24);
        _chkDNSRedirectEnabled.TabIndex = 0;
        // 
        // _lblConfigFileValue
        // 
        _lblConfigFileValue.AutoEllipsis = true;
        _lblConfigFileValue.Dock = DockStyle.Fill;
        _lblConfigFileValue.Location = new Point(165, 411);
        _lblConfigFileValue.Margin = new Padding(5);
        _lblConfigFileValue.Name = "_lblConfigFileValue";
        _lblConfigFileValue.Size = new Size(589, 23);
        _lblConfigFileValue.TabIndex = 15;
        _lblConfigFileValue.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblSyncActions
        // 
        _lblSyncActions.Location = new Point(0, 0);
        _lblSyncActions.Name = "_lblSyncActions";
        _lblSyncActions.Size = new Size(100, 23);
        _lblSyncActions.TabIndex = 0;
        // 
        // _syncActionsPanel
        // 
        _syncActionsPanel.Location = new Point(0, 0);
        _syncActionsPanel.Name = "_syncActionsPanel";
        _syncActionsPanel.Size = new Size(200, 100);
        _syncActionsPanel.TabIndex = 0;
        // 
        // _contextMenu
        // 
        _contextMenu.Items.AddRange(new ToolStripItem[] { _trayShowMenuItem, _trayHideMenuItem, _trayTopSeparator, _trayStartProxyMenuItem, _trayStopProxyMenuItem, _trayMiddleSeparator, _trayOptionsMenuItem, _trayConfigMenuItem, _trayBottomSeparator, _trayExitMenuItem });
        _contextMenu.Name = "_contextMenu";
        _contextMenu.Size = new Size(140, 176);
        // 
        // _trayShowMenuItem
        // 
        _trayShowMenuItem.Name = "_trayShowMenuItem";
        _trayShowMenuItem.Size = new Size(139, 22);
        _trayShowMenuItem.Text = "Show";
        _trayShowMenuItem.Click += TrayShowMenuItem_Click;
        // 
        // _trayHideMenuItem
        // 
        _trayHideMenuItem.Name = "_trayHideMenuItem";
        _trayHideMenuItem.Size = new Size(139, 22);
        _trayHideMenuItem.Text = "Hide";
        _trayHideMenuItem.Click += TrayHideMenuItem_Click;
        // 
        // _trayTopSeparator
        // 
        _trayTopSeparator.Name = "_trayTopSeparator";
        _trayTopSeparator.Size = new Size(136, 6);
        // 
        // _trayStartProxyMenuItem
        // 
        _trayStartProxyMenuItem.Name = "_trayStartProxyMenuItem";
        _trayStartProxyMenuItem.Size = new Size(139, 22);
        _trayStartProxyMenuItem.Text = "Start Proxy";
        _trayStartProxyMenuItem.Click += TrayStartProxyMenuItem_Click;
        // 
        // _trayStopProxyMenuItem
        // 
        _trayStopProxyMenuItem.Name = "_trayStopProxyMenuItem";
        _trayStopProxyMenuItem.Size = new Size(139, 22);
        _trayStopProxyMenuItem.Text = "Stop Proxy";
        _trayStopProxyMenuItem.Click += TrayStopProxyMenuItem_Click;
        // 
        // _trayMiddleSeparator
        // 
        _trayMiddleSeparator.Name = "_trayMiddleSeparator";
        _trayMiddleSeparator.Size = new Size(136, 6);
        // 
        // _trayOptionsMenuItem
        // 
        _trayOptionsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _trayStartOnBootMenuItem, _trayAutoStartProxyMenuItem });
        _trayOptionsMenuItem.Name = "_trayOptionsMenuItem";
        _trayOptionsMenuItem.Size = new Size(139, 22);
        _trayOptionsMenuItem.Text = "Options";
        // 
        // _trayStartOnBootMenuItem
        // 
        _trayStartOnBootMenuItem.CheckOnClick = true;
        _trayStartOnBootMenuItem.Name = "_trayStartOnBootMenuItem";
        _trayStartOnBootMenuItem.Size = new Size(224, 22);
        _trayStartOnBootMenuItem.Text = "Start on Windows startup";
        _trayStartOnBootMenuItem.Click += TrayStartOnBootMenuItem_Click;
        // 
        // _trayAutoStartProxyMenuItem
        // 
        _trayAutoStartProxyMenuItem.CheckOnClick = true;
        _trayAutoStartProxyMenuItem.Name = "_trayAutoStartProxyMenuItem";
        _trayAutoStartProxyMenuItem.Size = new Size(224, 22);
        _trayAutoStartProxyMenuItem.Text = "Start Proxy after launch";
        _trayAutoStartProxyMenuItem.Click += TrayAutoStartProxyMenuItem_Click;
        // 
        // _trayConfigMenuItem
        // 
        _trayConfigMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _trayLoadConfigMenuItem, _traySaveConfigAsMenuItem, _traySaveConfigMenuItem, _trayConfigSeparator });
        _trayConfigMenuItem.Name = "_trayConfigMenuItem";
        _trayConfigMenuItem.Size = new Size(139, 22);
        _trayConfigMenuItem.Text = "Config";
        // 
        // _trayLoadConfigMenuItem
        // 
        _trayLoadConfigMenuItem.Name = "_trayLoadConfigMenuItem";
        _trayLoadConfigMenuItem.Size = new Size(147, 22);
        _trayLoadConfigMenuItem.Text = "Load Config";
        _trayLoadConfigMenuItem.Click += TrayLoadConfigMenuItem_Click;
        // 
        // _traySaveConfigAsMenuItem
        // 
        _traySaveConfigAsMenuItem.Name = "_traySaveConfigAsMenuItem";
        _traySaveConfigAsMenuItem.Size = new Size(147, 22);
        _traySaveConfigAsMenuItem.Text = "Save As";
        _traySaveConfigAsMenuItem.Click += TraySaveConfigAsMenuItem_Click;
        // 
        // _traySaveConfigMenuItem
        // 
        _traySaveConfigMenuItem.Name = "_traySaveConfigMenuItem";
        _traySaveConfigMenuItem.Size = new Size(147, 22);
        _traySaveConfigMenuItem.Text = "Save Config";
        _traySaveConfigMenuItem.Click += TraySaveConfigMenuItem_Click;
        // 
        // _trayConfigSeparator
        // 
        _trayConfigSeparator.Name = "_trayConfigSeparator";
        _trayConfigSeparator.Size = new Size(144, 6);
        // 
        // _trayBottomSeparator
        // 
        _trayBottomSeparator.Name = "_trayBottomSeparator";
        _trayBottomSeparator.Size = new Size(136, 6);
        // 
        // _trayExitMenuItem
        // 
        _trayExitMenuItem.Name = "_trayExitMenuItem";
        _trayExitMenuItem.Size = new Size(139, 22);
        _trayExitMenuItem.Text = "Exit";
        _trayExitMenuItem.Click += TrayExitMenuItem_Click;
        // 
        // _notifyIcon
        // 
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.Text = "TrafficPilot";
        _notifyIcon.Visible = true;
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        // 
        // MainForm
        // 
        ClientSize = new Size(787, 691);
        Controls.Add(_mainPanel);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(600, 400);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "TrafficPilot";
        _mainPanel.ResumeLayout(false);
        _tabControl.ResumeLayout(false);
        _configTab.ResumeLayout(false);
        _configPanel.ResumeLayout(false);
        _configPanel.PerformLayout();
        _proxyHeaderPanel.ResumeLayout(false);
        _proxyHeaderPanel.PerformLayout();
        _proxySettingsPanel.ResumeLayout(false);
        ((ISupportInitialize)_numProxyPort).EndInit();
        _procPanel.ResumeLayout(false);
        _procPanel.PerformLayout();
        _domainRulesPanel.ResumeLayout(false);
        _domainRulesPanel.PerformLayout();
        _startupOptionsPanel.ResumeLayout(false);
        _startupOptionsPanel.PerformLayout();
        _syncProviderTokenPanel.ResumeLayout(false);
        _syncProviderTokenPanel.PerformLayout();
        _gistIdActionPanel.ResumeLayout(false);
        _gistIdActionPanel.PerformLayout();
        _configActionPanel.ResumeLayout(false);
        _configActionPanel.PerformLayout();
        _configBtnPanel.ResumeLayout(false);
        _localApiTab.ResumeLayout(false);
        _localApiPanel.ResumeLayout(false);
        _localApiPanel.PerformLayout();
        _localApiHeaderPanel.ResumeLayout(false);
        _localApiHeaderPanel.PerformLayout();
        _gatewayTabControl.ResumeLayout(false);
        _gatewayOverviewTab.ResumeLayout(false);
        _gatewayOverviewPanel.ResumeLayout(false);
        _gatewayOverviewPanel.PerformLayout();
        _localApiPortPanel.ResumeLayout(false);
        ((ISupportInitialize)_numOllamaPort).EndInit();
        ((ISupportInitialize)_numFoundryPort).EndInit();
        _gatewayOpenAiProviderTab.ResumeLayout(false);
        _gatewayAnthropicProviderTab.ResumeLayout(false);
        _gatewayGeminiProviderTab.ResumeLayout(false);
        _gatewayOpenAiProviderControl.ResumeLayout(false);
        _gatewayAnthropicProviderControl.ResumeLayout(false);
        _gatewayGeminiProviderControl.ResumeLayout(false);
        _gatewayProviderTab.ResumeLayout(false);
        _gatewayProviderEditorPanel.ResumeLayout(false);
        _gatewayProviderEditorPanel.PerformLayout();
        _gatewayProviderTabs.ResumeLayout(false);
        _gatewayProviderButtonsPanel.ResumeLayout(false);
        _gatewayProviderButtonsPanel.PerformLayout();
        _gatewayProviderContentScrollPanel.ResumeLayout(false);
        _gatewayProviderContentScrollPanel.PerformLayout();
        _grpGatewayProviderAdvanced.ResumeLayout(false);
        _gatewayProviderAdvancedLayout.ResumeLayout(false);
        _gatewayProviderDetailsPanel.ResumeLayout(false);
        _gatewayProviderDetailsPanel.PerformLayout();
        _grpGatewayProviderAuth.ResumeLayout(false);
        _gatewayProviderAuthPanel.ResumeLayout(false);
        _gatewayProviderAuthPanel.PerformLayout();
        _grpGatewayProviderEndpoints.ResumeLayout(false);
        _gatewayProviderEndpointsPanel.ResumeLayout(false);
        _gatewayProviderEndpointsPanel.PerformLayout();
        _grpGatewayProviderCapabilities.ResumeLayout(false);
        _grpGatewayProviderCapabilities.PerformLayout();
        _gatewayProviderCapabilitiesPanel.ResumeLayout(false);
        _gatewayProviderCapabilitiesPanel.PerformLayout();
        _grpGatewayProviderBasic.ResumeLayout(false);
        _gatewayProviderBasicPanel.ResumeLayout(false);
        _gatewayProviderBasicPanel.PerformLayout();
        _localApiProviderUrlPanel.ResumeLayout(false);
        _localApiProviderUrlPanel.PerformLayout();
        _gatewayProviderModelActionsPanel.ResumeLayout(false);
        _gatewayProviderModelActionsPanel.PerformLayout();
        _gatewayModelPreviewContextMenu.ResumeLayout(false);
        _gatewayRouteTab.ResumeLayout(false);
        _gatewayRouteTab.PerformLayout();
        _gatewayRouteEditorPanel.ResumeLayout(false);
        _gatewayRouteEditorPanel.PerformLayout();
        _gatewayDiagnosticsTab.ResumeLayout(false);
        _gatewayDiagnosticsTab.PerformLayout();
        _localApiLoggingPanel.ResumeLayout(false);
        _localApiLoggingPanel.PerformLayout();
        ((ISupportInitialize)_numLocalApiMaxBodyChars).EndInit();
        _dnsRedirectTab.ResumeLayout(false);
        _dnsRedirectPanel.ResumeLayout(false);
        _dnsRedirectPanel.PerformLayout();
        _grpRedirectMode.ResumeLayout(false);
        _grpRedirectMode.PerformLayout();
        _modePanelRedirectMode.ResumeLayout(false);
        _modePanelRedirectMode.PerformLayout();
        _hostsRedirectBtnPanel.ResumeLayout(false);
        _hostsRedirectBtnPanel.PerformLayout();
        _autoFetchPanel.ResumeLayout(false);
        _autoFetchPanel.PerformLayout();
        ((ISupportInitialize)_numAutoFetchInterval).EndInit();
        _logsTab.ResumeLayout(false);
        _logPanel.ResumeLayout(false);
        _btnClearPanel.ResumeLayout(false);
        _aboutTab.ResumeLayout(false);
        _aboutScrollPanel.ResumeLayout(false);
        _aboutScrollPanel.PerformLayout();
        _aboutContentPanel.ResumeLayout(false);
        _aboutContentPanel.PerformLayout();
        _versionPanel.ResumeLayout(false);
        _versionPanel.PerformLayout();
        _statusPanel.ResumeLayout(false);
        _localApiProviderPanel.ResumeLayout(false);
        _localApiProviderPanel.PerformLayout();
        _localApiAuthPanel.ResumeLayout(false);
        _localApiAuthPanel.PerformLayout();
        _contextMenu.ResumeLayout(false);
        ResumeLayout(false);
    }

    private FlowLayoutPanel? _versionPanel;
    private Label titleLabel;
    private Label versionLabel;
    private Label? _lblLatestVersion;
    private Label descLabel;
    private Label descContentLabel;
    private Label contribLabel;
    private Label contribContentLabel;
    private Label techLabel;
    private Label techContentLabel;
    private Button? _btnCheckUpdate;
    private Label? _lblUpdateStatus;
    private TabPage _gatewayOpenAiProviderTab;
    private TabPage _gatewayAnthropicProviderTab;
    private TabPage _gatewayGeminiProviderTab;
    private GatewayProviderSettingsControl _gatewayOpenAiProviderControl;
    private GatewayProviderSettingsControl _gatewayAnthropicProviderControl;
    private GatewayProviderSettingsControl _gatewayGeminiProviderControl;

    // Hosts Redirect Mode Selection (dynamically created)
    private GroupBox? _grpRedirectMode;
    private FlowLayoutPanel? _modePanelRedirectMode;
    private RadioButton? _rdoDnsInterception;
    private RadioButton? _rdoHostsFile;
    private Label? _lblHostsFileWarning;

    // 鈹€鈹€ Control initialization helpers 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€

    private void InitIpResultsColumns()
    {
        _lvIpResults!.Columns.Clear();
        _lvIpResults.Columns.Add("Domain",     220, HorizontalAlignment.Left);
        _lvIpResults.Columns.Add("IP Address", 130, HorizontalAlignment.Left);
        _lvIpResults.Columns.Add("Latency",     90, HorizontalAlignment.Right);
        _lvIpResults.Columns.Add("Via Proxy",  100, HorizontalAlignment.Right);
        _lvIpResults.Columns.Add("Source",     100, HorizontalAlignment.Left);
    }

    private void InitProxyHostComboBox()
    {
        _cmbProxyHost!.Items.Clear();
        foreach (var ip in LocalNetworkHelper.GetLocalIpsWithGateway())
            _cmbProxyHost.Items.Add(ip);

        if (_cmbProxyHost.Items.Count > 0 && string.IsNullOrEmpty(_cmbProxyHost.Text))
            _cmbProxyHost.SelectedIndex = 0;
    }

    private void LoadApplicationIcon()
    {
        _notifyIcon!.Icon = Resources.favicon;
        Icon = Resources.favicon;
    }

    private void InitializeHostsRedirectModeUI()
    {
        _rdoDnsInterception!.CheckedChanged += RdoDnsInterception_CheckedChanged;
        _rdoHostsFile!.CheckedChanged += RdoHostsFile_CheckedChanged;
        UpdateHostsFileModeUI();
    }

    private void UpdateHostsFileModeUI()
    {
        if (_lblHostsFileWarning is null || _rdoHostsFile is null)
            return;

        _lblHostsFileWarning.Visible = _rdoHostsFile.Checked;

        if (_rdoHostsFile.Checked && !SystemHostsFileManager.HasWriteAccess())
        {
            _lblHostsFileWarning.Text = "鈿?Administrator privileges required! Please restart TrafficPilot as Administrator.";
            _lblHostsFileWarning.ForeColor = Color.OrangeRed;
        }
        else if (_rdoHostsFile.Checked)
        {
            _lblHostsFileWarning.Text = "鉁?Administrator access confirmed. Backups will be created automatically.";
            _lblHostsFileWarning.ForeColor = Color.Green;
        }
    }

    private Button CreateQuickConfigButton(String configPath)
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

    private Label label1;
    private Label lblBytes;
    private Button btnResetConfig;
}

















