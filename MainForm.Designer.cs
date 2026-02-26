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
		components = new Container();

		// Main form settings
		Text = "TrafficPilot - Proxy Manager";
		Size = new Size(800, 600);
		MinimumSize = new Size(600, 400);
		StartPosition = FormStartPosition.CenterScreen;
		SuspendLayout();

		// ==================== MAIN LAYOUT ====================
		_mainPanel = new TableLayoutPanel();
		_mainPanel.Dock = DockStyle.Fill;
		_mainPanel.RowCount = 3;
		_mainPanel.ColumnCount = 1;
		_mainPanel.Margin = new Padding(0);
		_mainPanel.Padding = new Padding(5);
		_mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
		_mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
		_mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		// Tab Control
		_tabControl = new TabControl();
		_tabControl.Dock = DockStyle.Fill;
		_tabControl.Margin = new Padding(0, 0, 0, 5);

		// ==================== CONFIG TAB ====================
		_configTab = new TabPage();
		_configTab.Text = "Configuration";
		_configTab.Padding = new Padding(0);

		_configPanel = new TableLayoutPanel();
		_configPanel.Dock = DockStyle.Fill;
		_configPanel.ColumnCount = 2;
		_configPanel.RowCount = 12;
		_configPanel.Padding = new Padding(10);
		_configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		_configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		int configRow = 0;

		// Proxy Host Row
		_lblProxyHost = new Label();
		_lblProxyHost.Text = "Proxy Host:";
		_lblProxyHost.TextAlign = ContentAlignment.MiddleRight;
		_lblProxyHost.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblProxyHost, 0, configRow);

		_txtProxyHost = new TextBox();
		_txtProxyHost.Dock = DockStyle.Fill;
		_txtProxyHost.Margin = new Padding(5);
		_txtProxyHost.Text = "host.docker.internal";
		_configPanel.Controls.Add(_txtProxyHost, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		configRow++;

		// Proxy Port Row
		_lblProxyPort = new Label();
		_lblProxyPort.Text = "Proxy Port:";
		_lblProxyPort.TextAlign = ContentAlignment.MiddleRight;
		_lblProxyPort.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblProxyPort, 0, configRow);

		_numProxyPort = new NumericUpDown();
		_numProxyPort.Dock = DockStyle.Fill;
		_numProxyPort.Margin = new Padding(5);
		_numProxyPort.Maximum = 65535;
		_numProxyPort.Value = 7890;
		_configPanel.Controls.Add(_numProxyPort, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		configRow++;

		// Proxy Scheme Row
		_lblProxyScheme = new Label();
		_lblProxyScheme.Text = "Proxy Scheme:";
		_lblProxyScheme.TextAlign = ContentAlignment.MiddleRight;
		_lblProxyScheme.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblProxyScheme, 0, configRow);

		_cmbProxyScheme = new ComboBox();
		_cmbProxyScheme.Dock = DockStyle.Fill;
		_cmbProxyScheme.Margin = new Padding(5);
		_cmbProxyScheme.DropDownStyle = ComboBoxStyle.DropDownList;
		_cmbProxyScheme.Items.AddRange(["socks4", "socks5", "http"]);
		_cmbProxyScheme.SelectedItem = "socks4";
		_configPanel.Controls.Add(_cmbProxyScheme, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		configRow++;

		// Process Names Row
		_lblProcesses = new Label();
		_lblProcesses.Text = "Process Names:";
		_lblProcesses.TextAlign = ContentAlignment.TopRight;
		_lblProcesses.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblProcesses, 0, configRow);

		_procPanel = new TableLayoutPanel();
		_procPanel.Dock = DockStyle.Fill;
		_procPanel.ColumnCount = 2;
		_procPanel.RowCount = 1;
		_procPanel.Margin = new Padding(5);
		_procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

		_lstProcesses = new ListBox();
		_lstProcesses.Dock = DockStyle.Fill;
		_procPanel.Controls.Add(_lstProcesses, 0, 0);

		_btnRemoveProcess = new Button();
		_btnRemoveProcess.Text = "Remove";
		_btnRemoveProcess.Dock = DockStyle.Fill;
		_btnRemoveProcess.Margin = new Padding(2);
		_btnRemoveProcess.Click += BtnRemoveProcess_Click;
		_procPanel.Controls.Add(_btnRemoveProcess, 1, 0);

		_configPanel.Controls.Add(_procPanel, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
		configRow++;

		// Add Process Row
		_lblAddProcess = new Label();
		_lblAddProcess.Text = "Add Process:";
		_lblAddProcess.TextAlign = ContentAlignment.MiddleRight;
		_lblAddProcess.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblAddProcess, 0, configRow);

		_addProcPanel = new TableLayoutPanel();
		_addProcPanel.Dock = DockStyle.Fill;
		_addProcPanel.ColumnCount = 2;
		_addProcPanel.RowCount = 1;
		_addProcPanel.Margin = new Padding(5);
		_addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

		_txtNewProcess = new TextBox();
		_txtNewProcess.Dock = DockStyle.Fill;
		_addProcPanel.Controls.Add(_txtNewProcess, 0, 0);

		_btnAddProcess = new Button();
		_btnAddProcess.Text = "Add";
		_btnAddProcess.Dock = DockStyle.Fill;
		_btnAddProcess.Margin = new Padding(2);
		_btnAddProcess.Click += BtnAddProcess_Click;
		_addProcPanel.Controls.Add(_btnAddProcess, 1, 0);

		_configPanel.Controls.Add(_addProcPanel, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		configRow++;

		// Extra PIDs Row
		_lblExtraPids = new Label();
		_lblExtraPids.Text = "Extra PIDs:";
		_lblExtraPids.TextAlign = ContentAlignment.TopRight;
		_lblExtraPids.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblExtraPids, 0, configRow);

		_pidPanel = new TableLayoutPanel();
		_pidPanel.Dock = DockStyle.Fill;
		_pidPanel.ColumnCount = 2;
		_pidPanel.RowCount = 1;
		_pidPanel.Margin = new Padding(5);
		_pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

		_lstExtraPids = new ListBox();
		_lstExtraPids.Dock = DockStyle.Fill;
		_pidPanel.Controls.Add(_lstExtraPids, 0, 0);

		_btnRemovePid = new Button();
		_btnRemovePid.Text = "Remove";
		_btnRemovePid.Dock = DockStyle.Fill;
		_btnRemovePid.Margin = new Padding(2);
		_btnRemovePid.Click += BtnRemovePid_Click;
		_pidPanel.Controls.Add(_btnRemovePid, 1, 0);

		_configPanel.Controls.Add(_pidPanel, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
		configRow++;

		// Add PID Row
		_lblAddPid = new Label();
		_lblAddPid.Text = "Add PID:";
		_lblAddPid.TextAlign = ContentAlignment.MiddleRight;
		_lblAddPid.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblAddPid, 0, configRow);

		_addPidPanel = new TableLayoutPanel();
		_addPidPanel.Dock = DockStyle.Fill;
		_addPidPanel.ColumnCount = 2;
		_addPidPanel.RowCount = 1;
		_addPidPanel.Margin = new Padding(5);
		_addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));

		_txtNewPid = new TextBox();
		_txtNewPid.Dock = DockStyle.Fill;
		_addPidPanel.Controls.Add(_txtNewPid, 0, 0);

		_btnAddPid = new Button();
		_btnAddPid.Text = "Add";
		_btnAddPid.Dock = DockStyle.Fill;
		_btnAddPid.Margin = new Padding(2);
		_btnAddPid.Click += BtnAddPid_Click;
		_addPidPanel.Controls.Add(_btnAddPid, 1, 0);

		_configPanel.Controls.Add(_addPidPanel, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		configRow++;

		// Config File Row
		_lblConfigFile = new Label();
		_lblConfigFile.Text = "Config File:";
		_lblConfigFile.TextAlign = ContentAlignment.MiddleRight;
		_lblConfigFile.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_configPanel.Controls.Add(_lblConfigFile, 0, configRow);

		_lblConfigFileValue = new Label();
		_lblConfigFileValue.Text = "";
		_lblConfigFileValue.TextAlign = ContentAlignment.MiddleLeft;
		_lblConfigFileValue.AutoEllipsis = true;
		_lblConfigFileValue.Dock = DockStyle.Fill;
		_lblConfigFileValue.Margin = new Padding(5);
		_configPanel.Controls.Add(_lblConfigFileValue, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		configRow++;

		// Config Buttons Row
		_configBtnPanel = new FlowLayoutPanel();
		_configBtnPanel.Dock = DockStyle.Top;
		_configBtnPanel.AutoSize = true;
		_configBtnPanel.Margin = new Padding(5);
		_configBtnPanel.FlowDirection = FlowDirection.RightToLeft;

		_btnSaveConfig = new Button();
		_btnSaveConfig.Text = "Save Config";
		_btnSaveConfig.Width = 100;
		_btnSaveConfig.Height = 30;
		_btnSaveConfig.Margin = new Padding(2);
		_btnSaveConfig.Click += BtnSaveConfig_Click;
		_configBtnPanel.Controls.Add(_btnSaveConfig);

		_btnLoadConfig = new Button();
		_btnLoadConfig.Text = "Load Config";
		_btnLoadConfig.Width = 100;
		_btnLoadConfig.Height = 30;
		_btnLoadConfig.Margin = new Padding(2);
		_btnLoadConfig.Click += BtnLoadConfig_Click;
		_configBtnPanel.Controls.Add(_btnLoadConfig);

		_configPanel.Controls.Add(_configBtnPanel, 1, configRow);
		_configPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		configRow++;

		// Filler row
		_configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

		_configTab.Controls.Add(_configPanel);
		_tabControl.TabPages.Add(_configTab);

		// ==================== LOGS TAB ====================
		_logsTab = new TabPage();
		_logsTab.Text = "Logs";
		_logsTab.Padding = new Padding(0);

		_logPanel = new TableLayoutPanel();
		_logPanel.Dock = DockStyle.Fill;
		_logPanel.ColumnCount = 1;
		_logPanel.RowCount = 2;
		_logPanel.Padding = new Padding(0);
		_logPanel.Margin = new Padding(0);
		_logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
		_logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		_rtbLogs = new RichTextBox();
		_rtbLogs.Dock = DockStyle.Fill;
		_rtbLogs.ReadOnly = true;
		_rtbLogs.BackColor = Color.Black;
		_rtbLogs.ForeColor = Color.Lime;
		_rtbLogs.Font = new Font("Courier New", 9);
		_rtbLogs.WordWrap = false;
		_logPanel.Controls.Add(_rtbLogs, 0, 0);

		_btnClearPanel = new FlowLayoutPanel();
		_btnClearPanel.Dock = DockStyle.Fill;
		_btnClearPanel.FlowDirection = FlowDirection.RightToLeft;
		_btnClearPanel.Margin = new Padding(5);

		_btnClearLogs = new Button();
		_btnClearLogs.Text = "Clear Logs";
		_btnClearLogs.Width = 80;
		_btnClearLogs.Height = 30;
		_btnClearLogs.Click += BtnClearLogs_Click;
		_btnClearPanel.Controls.Add(_btnClearLogs);

		_logPanel.Controls.Add(_btnClearPanel, 0, 1);

		_logsTab.Controls.Add(_logPanel);
		_tabControl.TabPages.Add(_logsTab);

		// ==================== ABOUT TAB ====================
		_aboutTab = new TabPage();
		_aboutTab.Text = "About";
		_aboutTab.Padding = new Padding(0);

		_aboutScrollPanel = new Panel();
		_aboutScrollPanel.Dock = DockStyle.Fill;
		_aboutScrollPanel.AutoScroll = true;
		_aboutScrollPanel.Margin = new Padding(0);
		_aboutScrollPanel.Padding = new Padding(10);

		_aboutContentPanel = new TableLayoutPanel();
		_aboutContentPanel.AutoSize = true;
		_aboutContentPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_aboutContentPanel.ColumnCount = 1;
		_aboutContentPanel.RowCount = 8;
		_aboutContentPanel.Padding = new Padding(10);
		_aboutContentPanel.Margin = new Padding(0);
		_aboutContentPanel.Dock = DockStyle.Top;
		_aboutContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		int aboutRow = 0;

		// Title
		var titleLabel = new Label();
		titleLabel.Text = "TrafficPilot - Proxy Manager";
		titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
		titleLabel.AutoSize = true;
		titleLabel.Margin = new Padding(0, 0, 0, 10);
		_aboutContentPanel.Controls.Add(titleLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Version
		var versionLabel = new Label();
		versionLabel.Text = "Version: 1.0.0\nRelease Date: 2024";
		versionLabel.AutoSize = true;
		versionLabel.Margin = new Padding(0, 0, 0, 15);
		versionLabel.ForeColor = SystemColors.GrayText;
		_aboutContentPanel.Controls.Add(versionLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Description Title
		var descLabel = new Label();
		descLabel.Text = "Project Description";
		descLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
		descLabel.AutoSize = true;
		descLabel.Margin = new Padding(0, 0, 0, 5);
		_aboutContentPanel.Controls.Add(descLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Description Content
		var descContentLabel = new Label();
		descContentLabel.Text = "TrafficPilot is a Windows proxy manager that allows you to route network traffic from specific processes through a proxy server.\n\n" +
								"Features:\n" +
								"• Intercept and redirect traffic from specified processes\n" +
								"• Support for wildcard process matching\n" +
								"• Support for SOCKS4, SOCKS5, and HTTP proxies\n" +
								"• Real-time logging and statistics\n" +
								"• Configuration save/load functionality\n" +
								"• Process filtering and PID-based targeting";
		descContentLabel.AutoSize = true;
		descContentLabel.Margin = new Padding(10, 0, 0, 15);
		descContentLabel.MaximumSize = new Size(450, 0);
		_aboutContentPanel.Controls.Add(descContentLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Contributors Title
		var contribLabel = new Label();
		contribLabel.Text = "Contributors & Acknowledgments";
		contribLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
		contribLabel.AutoSize = true;
		contribLabel.Margin = new Padding(0, 0, 0, 5);
		_aboutContentPanel.Controls.Add(contribLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Contributors Content
		var contribContentLabel = new Label();
		contribContentLabel.Text = "• Original Author: maikebing\n" +
									"• Repository: github.com/maikebing/TrafficPilot\n" +
									"• WinDivert: Windows Packet Divert library\n" +
									"• Contributors: Community members and testers";
		contribContentLabel.AutoSize = true;
		contribContentLabel.Margin = new Padding(10, 0, 0, 15);
		contribContentLabel.MaximumSize = new Size(450, 0);
		_aboutContentPanel.Controls.Add(contribContentLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Tech Info Title
		var techLabel = new Label();
		techLabel.Text = "Technical Information";
		techLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
		techLabel.AutoSize = true;
		techLabel.Margin = new Padding(0, 0, 0, 5);
		_aboutContentPanel.Controls.Add(techLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		aboutRow++;

		// Tech Info Content
		var techContentLabel = new Label();
		techContentLabel.Text = "Platform: Windows\n" +
								".NET Version: .NET 10\n" +
								"C# Version: 14.0\n" +
								"Architecture: x64\n" +
								"License: Open Source";
		techContentLabel.AutoSize = true;
		techContentLabel.Margin = new Padding(10, 0, 0, 0);
		techContentLabel.MaximumSize = new Size(450, 0);
		_aboutContentPanel.Controls.Add(techContentLabel, 0, aboutRow);
		_aboutContentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		_aboutScrollPanel.Controls.Add(_aboutContentPanel);
		_aboutTab.Controls.Add(_aboutScrollPanel);
		_tabControl.TabPages.Add(_aboutTab);

		// ==================== ADD TAB CONTROL TO MAIN ====================
		_mainPanel.Controls.Add(_tabControl, 0, 0);

		// ==================== STATUS BAR ====================
		_statusPanel = new FlowLayoutPanel();
		_statusPanel.Dock = DockStyle.Fill;
		_statusPanel.Margin = new Padding(0);
		_statusPanel.WrapContents = false;
		_statusPanel.AutoScroll = true;

		_lblStatus = new Label();
		_lblStatus.Text = "Status: Stopped";
		_lblStatus.Width = 200;
		_lblStatus.Height = 40;
		_lblStatus.TextAlign = ContentAlignment.MiddleLeft;
		_statusPanel.Controls.Add(_lblStatus);

		_lblStats = new Label();
		_lblStats.Text = "Stats: -";
		_lblStats.AutoSize = true;
		_lblStats.Height = 40;
		_lblStats.TextAlign = ContentAlignment.MiddleLeft;
		_lblStats.Margin = new Padding(20, 0, 0, 0);
		_statusPanel.Controls.Add(_lblStats);

		_mainPanel.Controls.Add(_statusPanel, 0, 1);

		// ==================== CONTROL BAR ====================
		_controlPanel = new FlowLayoutPanel();
		_controlPanel.Dock = DockStyle.Fill;
		_controlPanel.Margin = new Padding(0);
		_controlPanel.WrapContents = false;
		_controlPanel.FlowDirection = FlowDirection.RightToLeft;

		_btnStartStop = new Button();
		_btnStartStop.Text = "Start Proxy";
		_btnStartStop.Width = 100;
		_btnStartStop.Height = 40;
		_btnStartStop.BackColor = Color.LimeGreen;
		_btnStartStop.Click += BtnStartStop_Click;
		_controlPanel.Controls.Add(_btnStartStop);

		_mainPanel.Controls.Add(_controlPanel, 0, 2);

		// ==================== ADD MAIN PANEL TO FORM ====================
		Controls.Add(_mainPanel);

		ResumeLayout(false);
		PerformLayout();
	}
}
