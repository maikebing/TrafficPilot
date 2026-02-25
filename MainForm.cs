using System.ComponentModel;

namespace VSifier;

// ════════════════════════════════════════════════════════════════
//  Main WinForms Window
// ════════════════════════════════════════════════════════════════

internal class MainForm : Form
{
	private readonly ProxyConfigManager _configManager;
	private ProxyEngine? _engine;
	private ProxyConfigModel _currentConfig;
	private NotifyIcon? _notifyIcon;
	private ContextMenuStrip? _contextMenu;

	// UI Controls
	private TabControl? _tabControl;
	private TabPage? _configTab;
	private TabPage? _logsTab;

	// Config tab
	private TextBox? _txtProxyHost;
	private NumericUpDown? _numProxyPort;
	private ComboBox? _cmbProxyScheme;
	private ListBox? _lstProcesses;
	private TextBox? _txtNewProcess;
	private Button? _btnAddProcess;
	private Button? _btnRemoveProcess;
	private ListBox? _lstExtraPids;
	private TextBox? _txtNewPid;
	private Button? _btnAddPid;
	private Button? _btnRemovePid;
	private Button? _btnSaveConfig;
	private Button? _btnLoadConfig;
	private Label? _lblConfigFile;

	// Logs tab
	private RichTextBox? _rtbLogs;
	private Button? _btnClearLogs;

	// Status bar
	private Button? _btnStartStop;
	private Label? _lblStatus;
	private Label? _lblStats;

	private bool _isStarting = false;

	public MainForm()
	{
		_configManager = new ProxyConfigManager();
		_currentConfig = _configManager.Load();
		InitializeComponent();
		InitializeNotifyIcon();
		CenterToScreen();
	}

	private void InitializeComponent()
	{
		SuspendLayout();

		// Form settings
		Text = "VSifier - Proxy Manager";
		Size = new Size(800, 600);
		MinimumSize = new Size(600, 400);
		StartPosition = FormStartPosition.CenterScreen;
		Icon = SystemIcons.Application;

		// Main layout
		var mainPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 3,
			ColumnCount = 1,
			Margin = new Padding(0),
			Padding = new Padding(5)
		};
		mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
		mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

		// Tab Control
		_tabControl = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 5) };

		// Config Tab
		_configTab = new TabPage { Text = "Configuration", AutoScroll = true };
		CreateConfigTab();
		_tabControl.TabPages.Add(_configTab);

		// Logs Tab
		_logsTab = new TabPage { Text = "Logs", Padding = new Padding(5) };
		CreateLogsTab();
		_tabControl.TabPages.Add(_logsTab);

		mainPanel.Controls.Add(_tabControl, 0, 0);

		// Status Bar
		var statusPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0),
			WrapContents = false,
			AutoScroll = true
		};
		_lblStatus = new Label { Text = "Status: Stopped", Width = 200, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
		_lblStats = new Label { Text = "Stats: -", AutoSize = true, Height = 40, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(20, 0, 0, 0) };
		statusPanel.Controls.Add(_lblStatus);
		statusPanel.Controls.Add(_lblStats);
		mainPanel.Controls.Add(statusPanel, 0, 1);

		// Control Bar
		var controlPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			Margin = new Padding(0),
			WrapContents = false,
			FlowDirection = FlowDirection.RightToLeft
		};
		_btnStartStop = new Button { Text = "Start Proxy", Width = 100, Height = 40, BackColor = Color.LimeGreen };
		_btnStartStop.Click += BtnStartStop_Click;
		controlPanel.Controls.Add(_btnStartStop);
		mainPanel.Controls.Add(controlPanel, 0, 2);

		Controls.Add(mainPanel);

		ResumeLayout(false);
		PerformLayout();
	}

	private void CreateConfigTab()
	{
		var panel = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			ColumnCount = 2,
			RowCount = 12,
			Padding = new Padding(10),
			AutoScroll = true
		};

		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		int row = 0;

		// Proxy settings
		panel.Controls.Add(new Label { Text = "Proxy Host:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
		_txtProxyHost = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(5) };
		_txtProxyHost.Text = _currentConfig.Proxy?.Host ?? "host.docker.internal";
		panel.Controls.Add(_txtProxyHost, 1, row);
		row++;

		panel.Controls.Add(new Label { Text = "Proxy Port:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
        _numProxyPort = new NumericUpDown { Dock = DockStyle.Fill, Margin = new Padding(5),  Maximum=65535,Value = _currentConfig.Proxy?.Port ?? 7890 };
	
		panel.Controls.Add(_numProxyPort, 1, row);
		row++;

		panel.Controls.Add(new Label { Text = "Proxy Scheme:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
		_cmbProxyScheme = new ComboBox { Dock = DockStyle.Fill, Margin = new Padding(5), DropDownStyle = ComboBoxStyle.DropDownList };
		_cmbProxyScheme.Items.AddRange(["socks4", "socks5", "http"]);
		_cmbProxyScheme.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks4";
		panel.Controls.Add(_cmbProxyScheme, 1, row);
		row++;

		// Process Names
		panel.Controls.Add(new Label { Text = "Process Names:", TextAlign = ContentAlignment.TopRight }, 0, row);
		var procPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
		_lstProcesses = new ListBox { Dock = DockStyle.Fill, Height = 80 };
		foreach (var proc in _currentConfig.Targeting?.ProcessNames ?? [])
			_lstProcesses.Items.Add(proc);
		procPanel.Controls.Add(_lstProcesses, 0, 0);
		_btnRemoveProcess = new Button { Text = "Remove", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnRemoveProcess.Click += (s, e) => { if (_lstProcesses.SelectedIndex >= 0) _lstProcesses.Items.RemoveAt(_lstProcesses.SelectedIndex); };
		procPanel.Controls.Add(_btnRemoveProcess, 1, 0);
		panel.Controls.Add(procPanel, 1, row);
		row++;

		panel.Controls.Add(new Label { Text = "Add Process:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
		var addProcPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
		_txtNewProcess = new TextBox { Dock = DockStyle.Fill };
		addProcPanel.Controls.Add(_txtNewProcess, 0, 0);
		_btnAddProcess = new Button { Text = "Add", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnAddProcess.Click += (s, e) =>
		{
			if (!string.IsNullOrWhiteSpace(_txtNewProcess.Text))
			{
				_lstProcesses.Items.Add(_txtNewProcess.Text);
				_txtNewProcess.Clear();
			}
		};
		addProcPanel.Controls.Add(_btnAddProcess, 1, 0);
		panel.Controls.Add(addProcPanel, 1, row);
		row++;

		// Extra PIDs
		panel.Controls.Add(new Label { Text = "Extra PIDs:", TextAlign = ContentAlignment.TopRight }, 0, row);
		var pidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
		_lstExtraPids = new ListBox { Dock = DockStyle.Fill, Height = 80 };
		foreach (var pid in _currentConfig.Targeting?.ExtraPids ?? [])
			_lstExtraPids.Items.Add(pid);
		pidPanel.Controls.Add(_lstExtraPids, 0, 0);
		_btnRemovePid = new Button { Text = "Remove", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnRemovePid.Click += (s, e) => { if (_lstExtraPids.SelectedIndex >= 0) _lstExtraPids.Items.RemoveAt(_lstExtraPids.SelectedIndex); };
		pidPanel.Controls.Add(_btnRemovePid, 1, 0);
		panel.Controls.Add(pidPanel, 1, row);
		row++;

		panel.Controls.Add(new Label { Text = "Add PID:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
		var addPidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
		_txtNewPid = new TextBox { Dock = DockStyle.Fill };
		addPidPanel.Controls.Add(_txtNewPid, 0, 0);
		_btnAddPid = new Button { Text = "Add", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnAddPid.Click += (s, e) =>
		{
			if (int.TryParse(_txtNewPid.Text, out int pid))
			{
				_lstExtraPids.Items.Add(pid);
				_txtNewPid.Clear();
			}
		};
		addPidPanel.Controls.Add(_btnAddPid, 1, 0);
		panel.Controls.Add(addPidPanel, 1, row);
		row++;

		// Config file info
		panel.Controls.Add(new Label { Text = "Config File:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
		_lblConfigFile = new Label { Text = _configManager.GetConfigPath(), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Dock = DockStyle.Fill, Margin = new Padding(5) };
		panel.Controls.Add(_lblConfigFile, 1, row);
		row++;

		// Buttons
		var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(5), FlowDirection = FlowDirection.RightToLeft };
		_btnSaveConfig = new Button { Text = "Save Config", Width = 100, Height = 30, Margin = new Padding(2) };
		_btnSaveConfig.Click += BtnSaveConfig_Click;
		btnPanel.Controls.Add(_btnSaveConfig);
		_btnLoadConfig = new Button { Text = "Load Config", Width = 100, Height = 30, Margin = new Padding(2) };
		_btnLoadConfig.Click += BtnLoadConfig_Click;
		btnPanel.Controls.Add(_btnLoadConfig);
		panel.Controls.Add(btnPanel, 0, row);

		for (int i = 0; i < row; i++)
			panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

		_configTab!.Controls.Add(panel);
	}

	private void CreateLogsTab()
	{
		var logPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 2,
			Padding = new Padding(0),
			Margin = new Padding(0)
		};
		logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

		_rtbLogs = new RichTextBox
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			BackColor = Color.Black,
			ForeColor = Color.Lime,
			Font = new Font("Courier New", 9)
		};
		logPanel.Controls.Add(_rtbLogs, 0, 0);

		var btnClearPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(5) };
		_btnClearLogs = new Button { Text = "Clear Logs", Width = 80, Height = 30 };
		_btnClearLogs.Click += (s, e) => _rtbLogs.Clear();
		btnClearPanel.Controls.Add(_btnClearLogs);
		logPanel.Controls.Add(btnClearPanel, 0, 1);

		_logsTab!.Controls.Add(logPanel);
	}

	private void InitializeNotifyIcon()
	{
		_contextMenu = new ContextMenuStrip();
		_contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
		_contextMenu.Items.Add("Hide", null, (s, e) => HideWindow());
		_contextMenu.Items.Add("-");
		_contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

		_notifyIcon = new NotifyIcon
		{
			Icon = SystemIcons.Application,
			Text = "VSifier - Proxy Manager",
			ContextMenuStrip = _contextMenu,
			Visible = true
		};
		_notifyIcon.DoubleClick += (s, e) => ShowWindow();
	}

	private async void BtnStartStop_Click(object? sender, EventArgs e)
	{
		if (_isStarting) return;
		_isStarting = true;

		try
		{
			if (_engine == null || !_engine.IsRunning)
			{
				var opts = BuildProxyOptions();
				_engine = new ProxyEngine(opts);
				_engine.OnLog += (msg) => AppendLog(msg);
				_engine.OnStatsUpdated += (stats) => UpdateStats(stats);

				await _engine.StartAsync();
				_btnStartStop!.Text = "Stop Proxy";
				_btnStartStop.BackColor = Color.Red;
				_lblStatus!.Text = "Status: Running";
				_lblStatus.ForeColor = Color.Green;
			}
			else
			{
				await _engine.StopAsync();
				_engine.Dispose();
				_engine = null;
				_btnStartStop!.Text = "Start Proxy";
				_btnStartStop.BackColor = Color.LimeGreen;
				_lblStatus!.Text = "Status: Stopped";
				_lblStatus.ForeColor = Color.Black;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error: {ex.Message}", "Proxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_btnStartStop!.Text = "Start Proxy";
			_btnStartStop.BackColor = Color.LimeGreen;
		}
		finally
		{
			_isStarting = false;
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
		ReloadConfigToUI();
		MessageBox.Show("Configuration loaded successfully.", "Load Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private ProxyConfigModel BuildConfigModel()
	{
		return new ProxyConfigModel
		{
			Proxy = new ProxySettings
			{
				Host = _txtProxyHost!.Text,
				Port = (ushort)_numProxyPort!.Value,
				Scheme = _cmbProxyScheme!.SelectedItem?.ToString() ?? "socks4"
			},
			Targeting = new TargetingSettings
			{
				ProcessNames = _lstProcesses!.Items.Cast<string>().ToList(),
				ExtraPids = _lstExtraPids!.Items.Cast<int>().ToList()
			}
		};
	}

	private ProxyOptions BuildProxyOptions()
	{
		return new ProxyOptions(
			_lstExtraPids!.Items.Cast<int>().ToList(),
			_lstProcesses!.Items.Cast<string>().ToList(),
			_txtProxyHost!.Text,
			(ushort)_numProxyPort!.Value,
			_cmbProxyScheme!.SelectedItem?.ToString() ?? "socks4"
		);
	}

	private void ReloadConfigToUI()
	{
		_txtProxyHost!.Text = _currentConfig.Proxy?.Host ?? "";
		_numProxyPort!.Value = _currentConfig.Proxy?.Port ?? 7890;
		_cmbProxyScheme!.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks4";

		_lstProcesses!.Items.Clear();
		foreach (var proc in _currentConfig.Targeting?.ProcessNames ?? [])
			_lstProcesses.Items.Add(proc);

		_lstExtraPids!.Items.Clear();
		foreach (var pid in _currentConfig.Targeting?.ExtraPids ?? [])
			_lstExtraPids.Items.Add(pid);
	}

	private void AppendLog(string message)
	{
		if (_rtbLogs!.InvokeRequired)
		{
			_rtbLogs.Invoke(() => AppendLog(message));
			return;
		}

		_rtbLogs.AppendText(message + Environment.NewLine);
		_rtbLogs.SelectionStart = _rtbLogs.TextLength;
		_rtbLogs.ScrollToCaret();

		if (_rtbLogs.Lines.Length > 10000)
			_rtbLogs.Clear();
	}

	private void UpdateStats(RedirectStats stats)
	{
		if (_lblStats!.InvokeRequired)
		{
			_lblStats.Invoke(() => UpdateStats(stats));
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
			_engine?.Dispose();
			_notifyIcon?.Dispose();
			_contextMenu?.Dispose();
		}
		base.Dispose(disposing);
	}
}
