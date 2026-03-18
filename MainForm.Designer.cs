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
	private FlowLayoutPanel? _controlPanel;

	// Tab and tab pages
	private TabControl? _tabControl;
	private TabPage? _configTab;
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
        _lblConfigFile = new Label();
        _lblConfigFileValue = new Label();
        _lblSyncProvider = new Label();
        _cmbSyncProvider = new ComboBox();
        _lblSyncToken = new Label();
        _txtSyncToken = new TextBox();
        _lblGistId = new Label();
        _txtGistId = new TextBox();
        _lblSyncActions = new Label();
        _syncActionsPanel = new FlowLayoutPanel();
        _btnSyncPush = new Button();
        _btnSyncPull = new Button();
        _startupOptionsPanel = new FlowLayoutPanel();
        _chkStartOnBoot = new CheckBox();
        _chkAutoStartProxy = new CheckBox();
        _configActionPanel = new TableLayoutPanel();
        _quickConfigPanel = new FlowLayoutPanel();
        _configBtnPanel = new FlowLayoutPanel();
        _btnLoadConfig = new Button();
        _btnSaveConfigAs = new Button();
        _btnSaveConfig = new Button();
        _dnsRedirectTab = new TabPage();
        _dnsRedirectPanel = new TableLayoutPanel();
        _chkDNSRedirectEnabled = new CheckBox();
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
        _controlPanel = new FlowLayoutPanel();
        _btnStartStop = new Button();
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
        ((ISupportInitialize)_numProxyPort).BeginInit();
        _procPanel.SuspendLayout();
        _domainRulesPanel.SuspendLayout();
        _startupOptionsPanel.SuspendLayout();
        _configActionPanel.SuspendLayout();
        _configBtnPanel.SuspendLayout();
        _dnsRedirectTab.SuspendLayout();
        _dnsRedirectPanel.SuspendLayout();
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
        _controlPanel.SuspendLayout();
        _contextMenu.SuspendLayout();
        SuspendLayout();
        // 
        // _mainPanel
        // 
        _mainPanel.ColumnCount = 1;
        _mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mainPanel.Controls.Add(_tabControl, 0, 0);
        _mainPanel.Controls.Add(_statusPanel, 0, 1);
        _mainPanel.Controls.Add(_controlPanel, 0, 2);
        _mainPanel.Dock = DockStyle.Fill;
        _mainPanel.Location = new Point(0, 0);
        _mainPanel.Margin = new Padding(0);
        _mainPanel.Name = "_mainPanel";
        _mainPanel.Padding = new Padding(5);
        _mainPanel.RowCount = 3;
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        _mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        _mainPanel.Size = new Size(787, 675);
        _mainPanel.TabIndex = 0;
        // 
        // _tabControl
        // 
        _tabControl.Controls.Add(_configTab);
        _tabControl.Controls.Add(_dnsRedirectTab);
        _tabControl.Controls.Add(_logsTab);
        _tabControl.Controls.Add(_aboutTab);
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Location = new Point(5, 5);
        _tabControl.Margin = new Padding(0, 0, 0, 5);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new Size(777, 560);
        _tabControl.TabIndex = 0;
        // 
        // _configTab
        // 
        _configTab.Controls.Add(_configPanel);
        _configTab.Location = new Point(4, 26);
        _configTab.Name = "_configTab";
        _configTab.Size = new Size(769, 530);
        _configTab.TabIndex = 0;
        _configTab.Text = "Configuration";
        // 
        // _configPanel
        // 
        _configPanel.ColumnCount = 2;
        _configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configPanel.Controls.Add(_proxyHeaderPanel, 0, 0);
        _configPanel.Controls.Add(_lblProxyHost, 0, 1);
        _configPanel.Controls.Add(_cmbProxyHost, 1, 1);
        _configPanel.Controls.Add(_lblProxyPort, 0, 2);
        _configPanel.Controls.Add(_numProxyPort, 1, 2);
        _configPanel.Controls.Add(_lblProxyScheme, 0, 3);
        _configPanel.Controls.Add(_cmbProxyScheme, 1, 3);
        _configPanel.Controls.Add(_lblProcesses, 0, 4);
        _configPanel.Controls.Add(_procPanel, 1, 4);
        _configPanel.Controls.Add(_lblDomainRules, 0, 5);
        _configPanel.Controls.Add(_domainRulesPanel, 1, 5);
        _configPanel.Controls.Add(_lblConfigFile, 0, 6);
        _configPanel.Controls.Add(_lblConfigFileValue, 1, 6);
        _configPanel.Controls.Add(_lblSyncProvider, 0, 7);
        _configPanel.Controls.Add(_cmbSyncProvider, 1, 7);
        _configPanel.Controls.Add(_lblSyncToken, 0, 8);
        _configPanel.Controls.Add(_txtSyncToken, 1, 8);
        _configPanel.Controls.Add(_lblGistId, 0, 9);
        _configPanel.Controls.Add(_txtGistId, 1, 9);
        _configPanel.Controls.Add(_lblSyncActions, 0, 10);
        _configPanel.Controls.Add(_syncActionsPanel, 1, 10);
        _configPanel.Controls.Add(_startupOptionsPanel, 0, 11);
        _configPanel.Controls.Add(_configActionPanel, 0, 12);
        _configPanel.Dock = DockStyle.Fill;
        _configPanel.Location = new Point(0, 0);
        _configPanel.Name = "_configPanel";
        _configPanel.Padding = new Padding(10);
        _configPanel.RowCount = 13;
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.Size = new Size(769, 530);
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
        // _lblProxyHost
        // 
        _lblProxyHost.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyHost.Location = new Point(13, 50);
        _lblProxyHost.Name = "_lblProxyHost";
        _lblProxyHost.Size = new Size(144, 23);
        _lblProxyHost.TabIndex = 0;
        _lblProxyHost.Text = "Proxy Host:";
        _lblProxyHost.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbProxyHost
        // 
        _cmbProxyHost.Dock = DockStyle.Fill;
        _cmbProxyHost.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbProxyHost.Location = new Point(165, 50);
        _cmbProxyHost.Margin = new Padding(5);
        _cmbProxyHost.Name = "_cmbProxyHost";
        _cmbProxyHost.Size = new Size(589, 23);
        _cmbProxyHost.TabIndex = 1;
        // 
        // _lblProxyPort
        // 
        _lblProxyPort.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyPort.Location = new Point(13, 83);
        _lblProxyPort.Name = "_lblProxyPort";
        _lblProxyPort.Size = new Size(144, 23);
        _lblProxyPort.TabIndex = 2;
        _lblProxyPort.Text = "Proxy Port:";
        _lblProxyPort.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _numProxyPort
        // 
        _numProxyPort.Dock = DockStyle.Fill;
        _numProxyPort.Location = new Point(165, 83);
        _numProxyPort.Margin = new Padding(5);
        _numProxyPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        _numProxyPort.Name = "_numProxyPort";
        _numProxyPort.Size = new Size(589, 23);
        _numProxyPort.TabIndex = 3;
        _numProxyPort.Value = new decimal(new int[] { 7890, 0, 0, 0 });
        // 
        // _lblProxyScheme
        // 
        _lblProxyScheme.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyScheme.Location = new Point(13, 117);
        _lblProxyScheme.Name = "_lblProxyScheme";
        _lblProxyScheme.Size = new Size(144, 23);
        _lblProxyScheme.TabIndex = 4;
        _lblProxyScheme.Text = "Proxy Scheme:";
        _lblProxyScheme.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbProxyScheme
        // 
        _cmbProxyScheme.Dock = DockStyle.Fill;
        _cmbProxyScheme.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbProxyScheme.Items.AddRange(new object[] { "socks5", "socks4", "http", "https" });
        _cmbProxyScheme.Location = new Point(165, 116);
        _cmbProxyScheme.Margin = new Padding(5);
        _cmbProxyScheme.Name = "_cmbProxyScheme";
        _cmbProxyScheme.Size = new Size(589, 25);
        _cmbProxyScheme.TabIndex = 5;
        // 
        // _lblProcesses
        // 
        _lblProcesses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblProcesses.Location = new Point(13, 146);
        _lblProcesses.Name = "_lblProcesses";
        _lblProcesses.Size = new Size(144, 23);
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
        _procPanel.Location = new Point(165, 151);
        _procPanel.Margin = new Padding(5);
        _procPanel.Name = "_procPanel";
        _procPanel.RowCount = 1;
        _procPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _procPanel.Size = new Size(589, 120);
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
        _txtProcesses.PlaceholderText = "姣忚涓€涓繘绋嬪悕锛屼緥濡?\r\ndevenv.exe\r\nservicehub*.exe";
        _txtProcesses.ScrollBars = ScrollBars.Vertical;
        _txtProcesses.Size = new Size(583, 114);
        _txtProcesses.TabIndex = 0;
        _txtProcesses.WordWrap = false;
        // 
        // _lblDomainRules
        // 
        _lblDomainRules.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblDomainRules.Location = new Point(13, 276);
        _lblDomainRules.Name = "_lblDomainRules";
        _lblDomainRules.Size = new Size(144, 23);
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
        _domainRulesPanel.Location = new Point(165, 281);
        _domainRulesPanel.Margin = new Padding(5);
        _domainRulesPanel.Name = "_domainRulesPanel";
        _domainRulesPanel.RowCount = 1;
        _domainRulesPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _domainRulesPanel.Size = new Size(589, 120);
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
        _txtDomainRules.PlaceholderText = "姣忚涓€涓煙鍚嶈鍒欙紝渚嬪:\r\n*.github.com\r\nraw.githubusercontent.com";
        _txtDomainRules.ScrollBars = ScrollBars.Vertical;
        _txtDomainRules.Size = new Size(583, 114);
        _txtDomainRules.TabIndex = 0;
        _txtDomainRules.WordWrap = false;
        // 
        // _lblConfigFile
        // 
        _lblConfigFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblConfigFile.Location = new Point(13, 411);
        _lblConfigFile.Name = "_lblConfigFile";
        _lblConfigFile.Size = new Size(144, 23);
        _lblConfigFile.TabIndex = 14;
        _lblConfigFile.Text = "Config File:";
        _lblConfigFile.TextAlign = ContentAlignment.MiddleRight;
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
        // _lblSyncProvider
        // 
        _lblSyncProvider.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblSyncProvider.Name = "_lblSyncProvider";
        _lblSyncProvider.Size = new Size(144, 23);
        _lblSyncProvider.TabIndex = 16;
        _lblSyncProvider.Text = "Sync Provider:";
        _lblSyncProvider.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbSyncProvider
        // 
        _cmbSyncProvider.Dock = DockStyle.Fill;
        _cmbSyncProvider.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSyncProvider.Items.AddRange(new object[] { "GitHub", "Gitee" });
        _cmbSyncProvider.Margin = new Padding(5);
        _cmbSyncProvider.Name = "_cmbSyncProvider";
        _cmbSyncProvider.Size = new Size(589, 25);
        _cmbSyncProvider.TabIndex = 17;
        // 
        // _lblSyncToken
        // 
        _lblSyncToken.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblSyncToken.Name = "_lblSyncToken";
        _lblSyncToken.Size = new Size(144, 23);
        _lblSyncToken.TabIndex = 18;
        _lblSyncToken.Text = "Sync Token:";
        _lblSyncToken.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtSyncToken
        // 
        _txtSyncToken.Dock = DockStyle.Fill;
        _txtSyncToken.Margin = new Padding(5);
        _txtSyncToken.Name = "_txtSyncToken";
        _txtSyncToken.PasswordChar = '●';
        _txtSyncToken.PlaceholderText = "Enter GitHub or Gitee personal access token";
        _txtSyncToken.Size = new Size(589, 23);
        _txtSyncToken.TabIndex = 19;
        // 
        // _lblGistId
        // 
        _lblGistId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblGistId.Name = "_lblGistId";
        _lblGistId.Size = new Size(144, 23);
        _lblGistId.TabIndex = 20;
        _lblGistId.Text = "Gist / Snippet ID:";
        _lblGistId.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtGistId
        // 
        _txtGistId.Dock = DockStyle.Fill;
        _txtGistId.Margin = new Padding(5);
        _txtGistId.Name = "_txtGistId";
        _txtGistId.PlaceholderText = "Optional – auto-discovered or filled automatically after first push";
        _txtGistId.Size = new Size(589, 23);
        _txtGistId.TabIndex = 21;
        // 
        // _lblSyncActions
        // 
        _lblSyncActions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblSyncActions.Name = "_lblSyncActions";
        _lblSyncActions.Size = new Size(144, 23);
        _lblSyncActions.TabIndex = 22;
        _lblSyncActions.Text = "Sync:";
        _lblSyncActions.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _syncActionsPanel
        // 
        _syncActionsPanel.AutoSize = true;
        _syncActionsPanel.Controls.Add(_btnSyncPush);
        _syncActionsPanel.Controls.Add(_btnSyncPull);
        _syncActionsPanel.Dock = DockStyle.Fill;
        _syncActionsPanel.Margin = new Padding(5);
        _syncActionsPanel.Name = "_syncActionsPanel";
        _syncActionsPanel.TabIndex = 23;
        _syncActionsPanel.WrapContents = false;
        // 
        // _btnSyncPush
        // 
        _btnSyncPush.AutoSize = true;
        _btnSyncPush.Margin = new Padding(0, 0, 6, 0);
        _btnSyncPush.Name = "_btnSyncPush";
        _btnSyncPush.Size = new Size(120, 27);
        _btnSyncPush.TabIndex = 0;
        _btnSyncPush.Text = "⬆ Push Config";
        _btnSyncPush.UseVisualStyleBackColor = true;
        _btnSyncPush.Click += BtnSyncPush_Click;
        // 
        // _btnSyncPull
        // 
        _btnSyncPull.AutoSize = true;
        _btnSyncPull.Margin = new Padding(0, 0, 6, 0);
        _btnSyncPull.Name = "_btnSyncPull";
        _btnSyncPull.Size = new Size(120, 27);
        _btnSyncPull.TabIndex = 1;
        _btnSyncPull.Text = "⬇ Pull Config";
        _btnSyncPull.UseVisualStyleBackColor = true;
        _btnSyncPull.Click += BtnSyncPull_Click;
        // 
        // _startupOptionsPanel
        // 
        _startupOptionsPanel.AutoSize = true;
        _configPanel.SetColumnSpan(_startupOptionsPanel, 2);
        _startupOptionsPanel.Controls.Add(_chkStartOnBoot);
        _startupOptionsPanel.Controls.Add(_chkAutoStartProxy);
        _startupOptionsPanel.Dock = DockStyle.Top;
        _startupOptionsPanel.Location = new Point(13, 442);
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
        // _configActionPanel
        // 
        _configActionPanel.AutoSize = true;
        _configActionPanel.ColumnCount = 2;
        _configPanel.SetColumnSpan(_configActionPanel, 2);
        _configActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configActionPanel.ColumnStyles.Add(new ColumnStyle());
        _configActionPanel.Controls.Add(_quickConfigPanel, 0, 0);
        _configActionPanel.Controls.Add(_configBtnPanel, 1, 0);
        _configActionPanel.Dock = DockStyle.Top;
        _configActionPanel.Location = new Point(13, 472);
        _configActionPanel.Margin = new Padding(3, 0, 3, 3);
        _configActionPanel.Name = "_configActionPanel";
        _configActionPanel.RowCount = 1;
        _configActionPanel.RowStyles.Add(new RowStyle());
        _configActionPanel.Size = new Size(743, 44);
        _configActionPanel.TabIndex = 18;
        // 
        // _quickConfigPanel
        // 
        _quickConfigPanel.AutoSize = true;
        _quickConfigPanel.Dock = DockStyle.Fill;
        _quickConfigPanel.Location = new Point(0, 0);
        _quickConfigPanel.Margin = new Padding(0);
        _quickConfigPanel.Name = "_quickConfigPanel";
        _quickConfigPanel.Size = new Size(423, 44);
        _quickConfigPanel.TabIndex = 0;
        _quickConfigPanel.WrapContents = false;
        // 
        // _configBtnPanel
        // 
        _configBtnPanel.AutoSize = true;
        _configBtnPanel.Controls.Add(_btnLoadConfig);
        _configBtnPanel.Controls.Add(_btnSaveConfigAs);
        _configBtnPanel.Controls.Add(_btnSaveConfig);
        _configBtnPanel.Dock = DockStyle.Top;
        _configBtnPanel.FlowDirection = FlowDirection.RightToLeft;
        _configBtnPanel.Location = new Point(428, 5);
        _configBtnPanel.Margin = new Padding(5);
        _configBtnPanel.Name = "_configBtnPanel";
        _configBtnPanel.Size = new Size(310, 34);
        _configBtnPanel.TabIndex = 18;
        _configBtnPanel.WrapContents = false;
        // 
        // _btnLoadConfig
        // 
        _btnLoadConfig.Location = new Point(208, 2);
        _btnLoadConfig.Margin = new Padding(2);
        _btnLoadConfig.Name = "_btnLoadConfig";
        _btnLoadConfig.Size = new Size(100, 30);
        _btnLoadConfig.TabIndex = 1;
        _btnLoadConfig.Text = "Load Config";
        _btnLoadConfig.Click += BtnLoadConfig_Click;
        // 
        // _btnSaveConfigAs
        // 
        _btnSaveConfigAs.Location = new Point(106, 2);
        _btnSaveConfigAs.Margin = new Padding(2);
        _btnSaveConfigAs.Name = "_btnSaveConfigAs";
        _btnSaveConfigAs.Size = new Size(98, 30);
        _btnSaveConfigAs.TabIndex = 2;
        _btnSaveConfigAs.Text = "Save As";
        _btnSaveConfigAs.Click += BtnSaveConfigAs_Click;
        // 
        // _btnSaveConfig
        // 
        _btnSaveConfig.Location = new Point(2, 2);
        _btnSaveConfig.Margin = new Padding(2);
        _btnSaveConfig.Name = "_btnSaveConfig";
        _btnSaveConfig.Size = new Size(100, 30);
        _btnSaveConfig.TabIndex = 3;
        _btnSaveConfig.Text = "Save Config";
        _btnSaveConfig.Click += BtnSaveConfig_Click;
        // 
        // _dnsRedirectTab
        // 
        _dnsRedirectTab.Controls.Add(_dnsRedirectPanel);
        _dnsRedirectTab.Location = new Point(4, 26);
        _dnsRedirectTab.Name = "_dnsRedirectTab";
        _dnsRedirectTab.Size = new Size(769, 530);
        _dnsRedirectTab.TabIndex = 3;
        _dnsRedirectTab.Text = "DNS Redirect";
        // 
        // _dnsRedirectPanel
        // 
        _dnsRedirectPanel.ColumnCount = 2;
        _dnsRedirectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _dnsRedirectPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _dnsRedirectPanel.Controls.Add(_chkDNSRedirectEnabled, 0, 0);
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
        _dnsRedirectPanel.Size = new Size(769, 530);
        _dnsRedirectPanel.TabIndex = 0;
        // 
        // _chkDNSRedirectEnabled
        // 
        _chkDNSRedirectEnabled.AutoSize = true;
        _dnsRedirectPanel.SetColumnSpan(_chkDNSRedirectEnabled, 2);
        _chkDNSRedirectEnabled.Location = new Point(13, 13);
        _chkDNSRedirectEnabled.Margin = new Padding(3, 3, 3, 10);
        _chkDNSRedirectEnabled.Name = "_chkDNSRedirectEnabled";
        _chkDNSRedirectEnabled.Size = new Size(263, 21);
        _chkDNSRedirectEnabled.TabIndex = 0;
        _chkDNSRedirectEnabled.Text = "Enable  DNS Redirect (DNS interception)";
        // 
        // _lblHostsUrl
        // 
        _lblHostsUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblHostsUrl.Location = new Point(13, 49);
        _lblHostsUrl.Name = "_lblHostsUrl";
        _lblHostsUrl.Size = new Size(144, 23);
        _lblHostsUrl.TabIndex = 1;
        _lblHostsUrl.Text = "Hosts URL:";
        _lblHostsUrl.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtHostsUrl
        // 
        _txtHostsUrl.Dock = DockStyle.Fill;
        _txtHostsUrl.Location = new Point(165, 49);
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
        _hostsRedirectBtnPanel.Location = new Point(165, 82);
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
        _chkRetestSlowOrTimeoutOnly.Size = new Size(245, 21);
        _chkRetestSlowOrTimeoutOnly.TabIndex = 2;
        _chkRetestSlowOrTimeoutOnly.Text = "Only retest timeout/high-latency domains";
        // 
        // _lblHostsStatus
        // 
        _lblHostsStatus.AutoSize = true;
        _dnsRedirectPanel.SetColumnSpan(_lblHostsStatus, 2);
        _lblHostsStatus.Location = new Point(13, 128);
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
        _autoFetchPanel.Location = new Point(13, 151);
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
        _lblRefreshDomains.Location = new Point(13, 183);
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
        _txtRefreshDomains.Location = new Point(165, 188);
        _txtRefreshDomains.Margin = new Padding(5);
        _txtRefreshDomains.Multiline = true;
        _txtRefreshDomains.Name = "_txtRefreshDomains";
        _txtRefreshDomains.PlaceholderText = "姣忚涓€涓煙鍚嶏紝渚嬪:\nalive.github.com\ngithub.com";
        _txtRefreshDomains.ScrollBars = ScrollBars.Vertical;
        _txtRefreshDomains.Size = new Size(589, 92);
        _txtRefreshDomains.TabIndex = 7;
        _txtRefreshDomains.WordWrap = false;
        // 
        // _lvIpResults
        // 
        _dnsRedirectPanel.SetColumnSpan(_lvIpResults, 2);
        _lvIpResults.Dock = DockStyle.Fill;
        _lvIpResults.FullRowSelect = true;
        _lvIpResults.GridLines = true;
        _lvIpResults.Location = new Point(13, 288);
        _lvIpResults.Name = "_lvIpResults";
        _lvIpResults.Size = new Size(743, 229);
        _lvIpResults.TabIndex = 8;
        _lvIpResults.UseCompatibleStateImageBehavior = false;
        _lvIpResults.View = View.Details;
        // 
        // _logsTab
        // 
        _logsTab.Controls.Add(_logPanel);
        _logsTab.Location = new Point(4, 26);
        _logsTab.Name = "_logsTab";
        _logsTab.Size = new Size(769, 530);
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
        _logPanel.Size = new Size(769, 530);
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
        _rtbLogs.Size = new Size(763, 484);
        _rtbLogs.TabIndex = 0;
        _rtbLogs.Text = "";
        _rtbLogs.WordWrap = false;
        // 
        // _btnClearPanel
        // 
        _btnClearPanel.Controls.Add(_btnClearLogs);
        _btnClearPanel.Dock = DockStyle.Fill;
        _btnClearPanel.FlowDirection = FlowDirection.RightToLeft;
        _btnClearPanel.Location = new Point(5, 495);
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
        _aboutTab.Size = new Size(769, 530);
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
        _aboutScrollPanel.Size = new Size(769, 530);
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
        _aboutContentPanel.Size = new Size(732, 531);
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
        descContentLabel.Size = new Size(440, 170);
        descContentLabel.TabIndex = 3;
        descContentLabel.Text = resources.GetString("descContentLabel.Text");
        // 
        // contribLabel
        // 
        contribLabel.AutoSize = true;
        contribLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        contribLabel.Location = new Point(10, 303);
        contribLabel.Margin = new Padding(0, 0, 0, 5);
        contribLabel.Name = "contribLabel";
        contribLabel.Size = new Size(236, 20);
        contribLabel.TabIndex = 4;
        contribLabel.Text = "Contributors & Acknowledgments";
        // 
        // contribContentLabel
        // 
        contribContentLabel.AutoSize = true;
        contribContentLabel.Location = new Point(20, 328);
        contribContentLabel.Margin = new Padding(10, 0, 0, 15);
        contribContentLabel.MaximumSize = new Size(450, 0);
        contribContentLabel.Name = "contribContentLabel";
        contribContentLabel.Size = new Size(290, 68);
        contribContentLabel.TabIndex = 5;
        contribContentLabel.Text = "鈥?Original Author: maikebing\n鈥?Repository: github.com/maikebing/TrafficPilot\n鈥?WinDivert: Windows Packet Divert library\n鈥?Contributors: Community members and testers";
        // 
        // techLabel
        // 
        techLabel.AutoSize = true;
        techLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        techLabel.Location = new Point(10, 411);
        techLabel.Margin = new Padding(0, 0, 0, 5);
        techLabel.Name = "techLabel";
        techLabel.Size = new Size(162, 20);
        techLabel.TabIndex = 6;
        techLabel.Text = "Technical Information";
        // 
        // techContentLabel
        // 
        techContentLabel.AutoSize = true;
        techContentLabel.Location = new Point(20, 436);
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
        _statusPanel.Dock = DockStyle.Fill;
        _statusPanel.Location = new Point(5, 570);
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
        _lblStatus.Size = new Size(200, 40);
        _lblStatus.TabIndex = 0;
        _lblStatus.Text = "Status: Stopped";
        _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblStats
        // 
        _lblStats.AutoSize = true;
        _lblStats.Location = new Point(226, 0);
        _lblStats.Margin = new Padding(20, 0, 0, 0);
        _lblStats.Name = "_lblStats";
        _lblStats.Size = new Size(48, 17);
        _lblStats.TabIndex = 1;
        _lblStats.Text = "Stats: -";
        _lblStats.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _controlPanel
        // 
        _controlPanel.Controls.Add(_btnStartStop);
        _controlPanel.Dock = DockStyle.Fill;
        _controlPanel.FlowDirection = FlowDirection.RightToLeft;
        _controlPanel.Location = new Point(5, 620);
        _controlPanel.Margin = new Padding(0);
        _controlPanel.Name = "_controlPanel";
        _controlPanel.Size = new Size(777, 50);
        _controlPanel.TabIndex = 2;
        _controlPanel.WrapContents = false;
        // 
        // _btnStartStop
        // 
        _btnStartStop.BackColor = Color.LimeGreen;
        _btnStartStop.Location = new Point(674, 3);
        _btnStartStop.Name = "_btnStartStop";
        _btnStartStop.Size = new Size(100, 40);
        _btnStartStop.TabIndex = 0;
        _btnStartStop.Text = "Start Proxy";
        _btnStartStop.UseVisualStyleBackColor = false;
        _btnStartStop.Click += BtnStartStop_Click;
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
        ClientSize = new Size(787, 675);
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
        ((ISupportInitialize)_numProxyPort).EndInit();
        _procPanel.ResumeLayout(false);
        _procPanel.PerformLayout();
        _domainRulesPanel.ResumeLayout(false);
        _domainRulesPanel.PerformLayout();
        _startupOptionsPanel.ResumeLayout(false);
        _startupOptionsPanel.PerformLayout();
        _configActionPanel.ResumeLayout(false);
        _configActionPanel.PerformLayout();
        _configBtnPanel.ResumeLayout(false);
        _dnsRedirectTab.ResumeLayout(false);
        _dnsRedirectPanel.ResumeLayout(false);
        _dnsRedirectPanel.PerformLayout();
        _hostsRedirectBtnPanel.ResumeLayout(false);
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
        _statusPanel.PerformLayout();
        _controlPanel.ResumeLayout(false);
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

    // Hosts Redirect Mode Selection (dynamically created)
    private GroupBox? _grpRedirectMode;
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
        _grpRedirectMode = new GroupBox
        {
            Text = "Redirect Mode",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(3, 3, 3, 10),
            Padding = new Padding(8, 5, 8, 8)
        };

        var modePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        _rdoDnsInterception = new RadioButton
        {
            Text = "DNS Interception (packet-level, no file modification)",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(3, 3, 3, 5)
        };
        _rdoDnsInterception.CheckedChanged += RdoDnsInterception_CheckedChanged;

        _rdoHostsFile = new RadioButton
        {
            Text = "System Hosts File (write to C:\\Windows\\System32\\drivers\\etc\\hosts)",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 3)
        };
        _rdoHostsFile.CheckedChanged += RdoHostsFile_CheckedChanged;

        _lblHostsFileWarning = new Label
        {
            Text = "鈿?Requires Administrator privileges. Creates backup before modifying.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(20, 0, 3, 3)
        };

        modePanel.Controls.Add(_rdoDnsInterception);
        modePanel.Controls.Add(_rdoHostsFile);
        modePanel.Controls.Add(_lblHostsFileWarning);
        _grpRedirectMode.Controls.Add(modePanel);

        if (_dnsRedirectPanel is not null)
        {
            _dnsRedirectPanel.Controls.Remove(_chkDNSRedirectEnabled);
            _dnsRedirectPanel.Controls.Add(_grpRedirectMode, 0, 0);
            _dnsRedirectPanel.SetColumnSpan(_grpRedirectMode, 2);
        }

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
}

















