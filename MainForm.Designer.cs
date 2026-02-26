#nullable enable
using System.ComponentModel;

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
	private TabPage? _aboutTab;

	// Tray
	private NotifyIcon? _notifyIcon;
	private ContextMenuStrip? _contextMenu;

	// Config tab - main panel
	private TableLayoutPanel? _configPanel;

	// Config tab - Proxy Host
	private Label? _lblProxyHost;
	private TextBox? _txtProxyHost;

	// Config tab - Proxy Port
	private Label? _lblProxyPort;
	private NumericUpDown? _numProxyPort;

	// Config tab - Proxy Scheme
	private Label? _lblProxyScheme;
	private ComboBox? _cmbProxyScheme;

	// Config tab - Process Names
	private Label? _lblProcesses;
	private TableLayoutPanel? _procPanel;
	private ListBox? _lstProcesses;
	private Button? _btnRemoveProcess;

	// Config tab - Add Process
	private Label? _lblAddProcess;
	private TableLayoutPanel? _addProcPanel;
	private TextBox? _txtNewProcess;
	private Button? _btnAddProcess;

	// Config tab - Extra PIDs
	private Label? _lblExtraPids;
	private TableLayoutPanel? _pidPanel;
	private ListBox? _lstExtraPids;
	private Button? _btnRemovePid;

	// Config tab - Add PID
	private Label? _lblAddPid;
	private TableLayoutPanel? _addPidPanel;
	private TextBox? _txtNewPid;
	private Button? _btnAddPid;

	// Config tab - Config File
	private Label? _lblConfigFile;
	private Label? _lblConfigFileValue;

	// Config tab - Buttons
	private FlowLayoutPanel? _configBtnPanel;
	private Button? _btnSaveConfig;
	private Button? _btnLoadConfig;

	// Logs tab
	private TableLayoutPanel? _logPanel;
	private RichTextBox? _rtbLogs;
	private FlowLayoutPanel? _btnClearPanel;
	private Button? _btnClearLogs;

	// About tab
	private Panel? _aboutScrollPanel;
	private TableLayoutPanel? _aboutContentPanel;

	// Status and control
	private Button? _btnStartStop;
	private Label? _lblStatus;
	private Label? _lblStats;

    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
        _mainPanel = new TableLayoutPanel();
        _tabControl = new TabControl();
        _configTab = new TabPage();
        _configPanel = new TableLayoutPanel();
        _lblProxyHost = new Label();
        _txtProxyHost = new TextBox();
        _lblProxyPort = new Label();
        _numProxyPort = new NumericUpDown();
        _lblProxyScheme = new Label();
        _cmbProxyScheme = new ComboBox();
        _lblProcesses = new Label();
        _procPanel = new TableLayoutPanel();
        _lstProcesses = new ListBox();
        _btnRemoveProcess = new Button();
        _lblAddProcess = new Label();
        _addProcPanel = new TableLayoutPanel();
        _txtNewProcess = new TextBox();
        _btnAddProcess = new Button();
        _lblExtraPids = new Label();
        _pidPanel = new TableLayoutPanel();
        _lstExtraPids = new ListBox();
        _btnRemovePid = new Button();
        _lblAddPid = new Label();
        _addPidPanel = new TableLayoutPanel();
        _txtNewPid = new TextBox();
        _btnAddPid = new Button();
        _lblConfigFile = new Label();
        _lblConfigFileValue = new Label();
        _configBtnPanel = new FlowLayoutPanel();
        _btnSaveConfig = new Button();
        _btnLoadConfig = new Button();
        _logsTab = new TabPage();
        _logPanel = new TableLayoutPanel();
        _rtbLogs = new RichTextBox();
        _btnClearPanel = new FlowLayoutPanel();
        _btnClearLogs = new Button();
        _aboutTab = new TabPage();
        _aboutScrollPanel = new Panel();
        _aboutContentPanel = new TableLayoutPanel();
        titleLabel = new Label();
        versionLabel = new Label();
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
        _mainPanel.SuspendLayout();
        _tabControl.SuspendLayout();
        _configTab.SuspendLayout();
        _configPanel.SuspendLayout();
        ((ISupportInitialize)_numProxyPort).BeginInit();
        _procPanel.SuspendLayout();
        _addProcPanel.SuspendLayout();
        _pidPanel.SuspendLayout();
        _addPidPanel.SuspendLayout();
        _configBtnPanel.SuspendLayout();
        _logsTab.SuspendLayout();
        _logPanel.SuspendLayout();
        _btnClearPanel.SuspendLayout();
        _aboutTab.SuspendLayout();
        _aboutScrollPanel.SuspendLayout();
        _aboutContentPanel.SuspendLayout();
        _statusPanel.SuspendLayout();
        _controlPanel.SuspendLayout();
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
        _configPanel.Controls.Add(_lblProxyHost, 0, 0);
        _configPanel.Controls.Add(_txtProxyHost, 1, 0);
        _configPanel.Controls.Add(_lblProxyPort, 0, 1);
        _configPanel.Controls.Add(_numProxyPort, 1, 1);
        _configPanel.Controls.Add(_lblProxyScheme, 0, 2);
        _configPanel.Controls.Add(_cmbProxyScheme, 1, 2);
        _configPanel.Controls.Add(_lblProcesses, 0, 3);
        _configPanel.Controls.Add(_procPanel, 1, 3);
        _configPanel.Controls.Add(_lblAddProcess, 0, 4);
        _configPanel.Controls.Add(_addProcPanel, 1, 4);
        _configPanel.Controls.Add(_lblExtraPids, 0, 5);
        _configPanel.Controls.Add(_pidPanel, 1, 5);
        _configPanel.Controls.Add(_lblAddPid, 0, 6);
        _configPanel.Controls.Add(_addPidPanel, 1, 6);
        _configPanel.Controls.Add(_lblConfigFile, 0, 7);
        _configPanel.Controls.Add(_lblConfigFileValue, 1, 7);
        _configPanel.Controls.Add(_configBtnPanel, 1, 8);
        _configPanel.Dock = DockStyle.Fill;
        _configPanel.Location = new Point(0, 0);
        _configPanel.Name = "_configPanel";
        _configPanel.Padding = new Padding(10);
        _configPanel.RowCount = 12;
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle());
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _configPanel.Size = new Size(769, 530);
        _configPanel.TabIndex = 0;
        // 
        // _lblProxyHost
        // 
        _lblProxyHost.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyHost.Location = new Point(13, 15);
        _lblProxyHost.Name = "_lblProxyHost";
        _lblProxyHost.Size = new Size(144, 23);
        _lblProxyHost.TabIndex = 0;
        _lblProxyHost.Text = "Proxy Host:";
        _lblProxyHost.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtProxyHost
        // 
        _txtProxyHost.Dock = DockStyle.Fill;
        _txtProxyHost.Location = new Point(165, 15);
        _txtProxyHost.Margin = new Padding(5);
        _txtProxyHost.Name = "_txtProxyHost";
        _txtProxyHost.Size = new Size(589, 23);
        _txtProxyHost.TabIndex = 1;
        _txtProxyHost.Text = "host.docker.internal";
        // 
        // _lblProxyPort
        // 
        _lblProxyPort.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProxyPort.Location = new Point(13, 48);
        _lblProxyPort.Name = "_lblProxyPort";
        _lblProxyPort.Size = new Size(144, 23);
        _lblProxyPort.TabIndex = 2;
        _lblProxyPort.Text = "Proxy Port:";
        _lblProxyPort.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _numProxyPort
        // 
        _numProxyPort.Dock = DockStyle.Fill;
        _numProxyPort.Location = new Point(165, 48);
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
        _lblProxyScheme.Location = new Point(13, 82);
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
        _cmbProxyScheme.Items.AddRange(new object[] { "socks4", "socks5", "http", "https" });
        _cmbProxyScheme.Location = new Point(165, 81);
        _cmbProxyScheme.Margin = new Padding(5);
        _cmbProxyScheme.Name = "_cmbProxyScheme";
        _cmbProxyScheme.Size = new Size(589, 25);
        _cmbProxyScheme.TabIndex = 5;
        // 
        // _lblProcesses
        // 
        _lblProcesses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblProcesses.Location = new Point(13, 111);
        _lblProcesses.Name = "_lblProcesses";
        _lblProcesses.Size = new Size(144, 23);
        _lblProcesses.TabIndex = 6;
        _lblProcesses.Text = "Process Names:";
        _lblProcesses.TextAlign = ContentAlignment.TopRight;
        // 
        // _procPanel
        // 
        _procPanel.ColumnCount = 2;
        _procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        _procPanel.Controls.Add(_lstProcesses, 0, 0);
        _procPanel.Controls.Add(_btnRemoveProcess, 1, 0);
        _procPanel.Dock = DockStyle.Fill;
        _procPanel.Location = new Point(165, 116);
        _procPanel.Margin = new Padding(5);
        _procPanel.Name = "_procPanel";
        _procPanel.RowCount = 1;
        _procPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _procPanel.Size = new Size(589, 72);
        _procPanel.TabIndex = 7;
        // 
        // _lstProcesses
        // 
        _lstProcesses.Dock = DockStyle.Fill;
        _lstProcesses.Location = new Point(3, 3);
        _lstProcesses.Name = "_lstProcesses";
        _lstProcesses.Size = new Size(513, 66);
        _lstProcesses.TabIndex = 0;
        // 
        // _btnRemoveProcess
        // 
        _btnRemoveProcess.Dock = DockStyle.Fill;
        _btnRemoveProcess.Location = new Point(521, 2);
        _btnRemoveProcess.Margin = new Padding(2);
        _btnRemoveProcess.Name = "_btnRemoveProcess";
        _btnRemoveProcess.Size = new Size(66, 68);
        _btnRemoveProcess.TabIndex = 1;
        _btnRemoveProcess.Text = "Remove";
        _btnRemoveProcess.Click += BtnRemoveProcess_Click;
        // 
        // _lblAddProcess
        // 
        _lblAddProcess.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblAddProcess.Location = new Point(13, 196);
        _lblAddProcess.Name = "_lblAddProcess";
        _lblAddProcess.Size = new Size(144, 23);
        _lblAddProcess.TabIndex = 8;
        _lblAddProcess.Text = "Add Process:";
        _lblAddProcess.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _addProcPanel
        // 
        _addProcPanel.ColumnCount = 2;
        _addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        _addProcPanel.Controls.Add(_txtNewProcess, 0, 0);
        _addProcPanel.Controls.Add(_btnAddProcess, 1, 0);
        _addProcPanel.Dock = DockStyle.Fill;
        _addProcPanel.Location = new Point(165, 198);
        _addProcPanel.Margin = new Padding(5);
        _addProcPanel.Name = "_addProcPanel";
        _addProcPanel.RowCount = 1;
        _addProcPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _addProcPanel.Size = new Size(589, 20);
        _addProcPanel.TabIndex = 9;
        // 
        // _txtNewProcess
        // 
        _txtNewProcess.Dock = DockStyle.Fill;
        _txtNewProcess.Location = new Point(3, 3);
        _txtNewProcess.Name = "_txtNewProcess";
        _txtNewProcess.Size = new Size(513, 23);
        _txtNewProcess.TabIndex = 0;
        // 
        // _btnAddProcess
        // 
        _btnAddProcess.Dock = DockStyle.Fill;
        _btnAddProcess.Location = new Point(521, 2);
        _btnAddProcess.Margin = new Padding(2);
        _btnAddProcess.Name = "_btnAddProcess";
        _btnAddProcess.Size = new Size(66, 16);
        _btnAddProcess.TabIndex = 1;
        _btnAddProcess.Text = "Add";
        _btnAddProcess.Click += BtnAddProcess_Click;
        // 
        // _lblExtraPids
        // 
        _lblExtraPids.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblExtraPids.Location = new Point(13, 223);
        _lblExtraPids.Name = "_lblExtraPids";
        _lblExtraPids.Size = new Size(144, 23);
        _lblExtraPids.TabIndex = 10;
        _lblExtraPids.Text = "Extra PIDs:";
        _lblExtraPids.TextAlign = ContentAlignment.TopRight;
        // 
        // _pidPanel
        // 
        _pidPanel.ColumnCount = 2;
        _pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        _pidPanel.Controls.Add(_lstExtraPids, 0, 0);
        _pidPanel.Controls.Add(_btnRemovePid, 1, 0);
        _pidPanel.Dock = DockStyle.Fill;
        _pidPanel.Location = new Point(165, 228);
        _pidPanel.Margin = new Padding(5);
        _pidPanel.Name = "_pidPanel";
        _pidPanel.RowCount = 1;
        _pidPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _pidPanel.Size = new Size(589, 72);
        _pidPanel.TabIndex = 11;
        // 
        // _lstExtraPids
        // 
        _lstExtraPids.Dock = DockStyle.Fill;
        _lstExtraPids.Location = new Point(3, 3);
        _lstExtraPids.Name = "_lstExtraPids";
        _lstExtraPids.Size = new Size(513, 66);
        _lstExtraPids.TabIndex = 0;
        // 
        // _btnRemovePid
        // 
        _btnRemovePid.Dock = DockStyle.Fill;
        _btnRemovePid.Location = new Point(521, 2);
        _btnRemovePid.Margin = new Padding(2);
        _btnRemovePid.Name = "_btnRemovePid";
        _btnRemovePid.Size = new Size(66, 68);
        _btnRemovePid.TabIndex = 1;
        _btnRemovePid.Text = "Remove";
        _btnRemovePid.Click += BtnRemovePid_Click;
        // 
        // _lblAddPid
        // 
        _lblAddPid.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblAddPid.Location = new Point(13, 308);
        _lblAddPid.Name = "_lblAddPid";
        _lblAddPid.Size = new Size(144, 23);
        _lblAddPid.TabIndex = 12;
        _lblAddPid.Text = "Add PID:";
        _lblAddPid.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _addPidPanel
        // 
        _addPidPanel.ColumnCount = 2;
        _addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        _addPidPanel.Controls.Add(_txtNewPid, 0, 0);
        _addPidPanel.Controls.Add(_btnAddPid, 1, 0);
        _addPidPanel.Dock = DockStyle.Fill;
        _addPidPanel.Location = new Point(165, 310);
        _addPidPanel.Margin = new Padding(5);
        _addPidPanel.Name = "_addPidPanel";
        _addPidPanel.RowCount = 1;
        _addPidPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        _addPidPanel.Size = new Size(589, 20);
        _addPidPanel.TabIndex = 13;
        // 
        // _txtNewPid
        // 
        _txtNewPid.Dock = DockStyle.Fill;
        _txtNewPid.Location = new Point(3, 3);
        _txtNewPid.Name = "_txtNewPid";
        _txtNewPid.Size = new Size(513, 23);
        _txtNewPid.TabIndex = 0;
        // 
        // _btnAddPid
        // 
        _btnAddPid.Dock = DockStyle.Fill;
        _btnAddPid.Location = new Point(521, 2);
        _btnAddPid.Margin = new Padding(2);
        _btnAddPid.Name = "_btnAddPid";
        _btnAddPid.Size = new Size(66, 16);
        _btnAddPid.TabIndex = 1;
        _btnAddPid.Text = "Add";
        _btnAddPid.Click += BtnAddPid_Click;
        // 
        // _lblConfigFile
        // 
        _lblConfigFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblConfigFile.Location = new Point(13, 340);
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
        _lblConfigFileValue.Location = new Point(165, 340);
        _lblConfigFileValue.Margin = new Padding(5);
        _lblConfigFileValue.Name = "_lblConfigFileValue";
        _lblConfigFileValue.Size = new Size(589, 23);
        _lblConfigFileValue.TabIndex = 15;
        _lblConfigFileValue.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _configBtnPanel
        // 
        _configBtnPanel.AutoSize = true;
        _configBtnPanel.Controls.Add(_btnSaveConfig);
        _configBtnPanel.Controls.Add(_btnLoadConfig);
        _configBtnPanel.Dock = DockStyle.Top;
        _configBtnPanel.FlowDirection = FlowDirection.RightToLeft;
        _configBtnPanel.Location = new Point(165, 373);
        _configBtnPanel.Margin = new Padding(5);
        _configBtnPanel.Name = "_configBtnPanel";
        _configBtnPanel.Size = new Size(589, 34);
        _configBtnPanel.TabIndex = 16;
        // 
        // _btnSaveConfig
        // 
        _btnSaveConfig.Location = new Point(487, 2);
        _btnSaveConfig.Margin = new Padding(2);
        _btnSaveConfig.Name = "_btnSaveConfig";
        _btnSaveConfig.Size = new Size(100, 30);
        _btnSaveConfig.TabIndex = 0;
        _btnSaveConfig.Text = "Save Config";
        _btnSaveConfig.Click += BtnSaveConfig_Click;
        // 
        // _btnLoadConfig
        // 
        _btnLoadConfig.Location = new Point(383, 2);
        _btnLoadConfig.Margin = new Padding(2);
        _btnLoadConfig.Name = "_btnLoadConfig";
        _btnLoadConfig.Size = new Size(100, 30);
        _btnLoadConfig.TabIndex = 1;
        _btnLoadConfig.Text = "Load Config";
        _btnLoadConfig.Click += BtnLoadConfig_Click;
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
        _aboutTab.TabIndex = 2;
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
        _aboutContentPanel.Controls.Add(versionLabel, 0, 1);
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
        _aboutContentPanel.Size = new Size(732, 537);
        _aboutContentPanel.TabIndex = 0;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        titleLabel.Location = new Point(10, 10);
        titleLabel.Margin = new Padding(0, 0, 0, 10);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(306, 30);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "TrafficPilot - Proxy Manager";
        // 
        // versionLabel
        // 
        versionLabel.AutoSize = true;
        versionLabel.ForeColor = SystemColors.GrayText;
        versionLabel.Location = new Point(10, 50);
        versionLabel.Margin = new Padding(0, 0, 0, 15);
        versionLabel.Name = "versionLabel";
        versionLabel.Size = new Size(119, 34);
        versionLabel.TabIndex = 1;
        versionLabel.Text = "Version: 1.0.0\nRelease Date: 2024";
        // 
        // descLabel
        // 
        descLabel.AutoSize = true;
        descLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        descLabel.Location = new Point(10, 99);
        descLabel.Margin = new Padding(0, 0, 0, 5);
        descLabel.Name = "descLabel";
        descLabel.Size = new Size(142, 20);
        descLabel.TabIndex = 2;
        descLabel.Text = "Project Description";
        // 
        // descContentLabel
        // 
        descContentLabel.AutoSize = true;
        descContentLabel.Location = new Point(20, 124);
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
        contribLabel.Location = new Point(10, 309);
        contribLabel.Margin = new Padding(0, 0, 0, 5);
        contribLabel.Name = "contribLabel";
        contribLabel.Size = new Size(236, 20);
        contribLabel.TabIndex = 4;
        contribLabel.Text = "Contributors & Acknowledgments";
        // 
        // contribContentLabel
        // 
        contribContentLabel.AutoSize = true;
        contribContentLabel.Location = new Point(20, 334);
        contribContentLabel.Margin = new Padding(10, 0, 0, 15);
        contribContentLabel.MaximumSize = new Size(450, 0);
        contribContentLabel.Name = "contribContentLabel";
        contribContentLabel.Size = new Size(290, 68);
        contribContentLabel.TabIndex = 5;
        contribContentLabel.Text = "• Original Author: maikebing\n• Repository: github.com/maikebing/TrafficPilot\n• WinDivert: Windows Packet Divert library\n• Contributors: Community members and testers";
        // 
        // techLabel
        // 
        techLabel.AutoSize = true;
        techLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        techLabel.Location = new Point(10, 417);
        techLabel.Margin = new Padding(0, 0, 0, 5);
        techLabel.Name = "techLabel";
        techLabel.Size = new Size(162, 20);
        techLabel.TabIndex = 6;
        techLabel.Text = "Technical Information";
        // 
        // techContentLabel
        // 
        techContentLabel.AutoSize = true;
        techContentLabel.Location = new Point(20, 442);
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
        // MainForm
        // 
        ClientSize = new Size(787, 675);
        Controls.Add(_mainPanel);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(600, 400);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "TrafficPilot - Proxy Manager";
        _mainPanel.ResumeLayout(false);
        _tabControl.ResumeLayout(false);
        _configTab.ResumeLayout(false);
        _configPanel.ResumeLayout(false);
        _configPanel.PerformLayout();
        ((ISupportInitialize)_numProxyPort).EndInit();
        _procPanel.ResumeLayout(false);
        _addProcPanel.ResumeLayout(false);
        _addProcPanel.PerformLayout();
        _pidPanel.ResumeLayout(false);
        _addPidPanel.ResumeLayout(false);
        _addPidPanel.PerformLayout();
        _configBtnPanel.ResumeLayout(false);
        _logsTab.ResumeLayout(false);
        _logPanel.ResumeLayout(false);
        _btnClearPanel.ResumeLayout(false);
        _aboutTab.ResumeLayout(false);
        _aboutScrollPanel.ResumeLayout(false);
        _aboutScrollPanel.PerformLayout();
        _aboutContentPanel.ResumeLayout(false);
        _aboutContentPanel.PerformLayout();
        _statusPanel.ResumeLayout(false);
        _statusPanel.PerformLayout();
        _controlPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private Label titleLabel;
    private Label versionLabel;
    private Label descLabel;
    private Label descContentLabel;
    private Label contribLabel;
    private Label contribContentLabel;
    private Label techLabel;
    private Label techContentLabel;
}
