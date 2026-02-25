using System.ComponentModel;
using TrafficPilot.Properties;

namespace TrafficPilot;

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
	private LogBuffer? _logBuffer;

	// UI Controls
	private TabControl? _tabControl;
	private TabPage? _configTab;
	private TabPage? _logsTab;
	private TabPage? _aboutTab;

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
		_logBuffer = new LogBuffer(BatchAppendLogs);
		InitializeComponent();
		InitializeNotifyIcon();
		LoadApplicationIcon();
		CenterToScreen();
	}

	private void LoadApplicationIcon()
	{
        _notifyIcon.Icon = Resources.favicon;
        Icon = Resources.favicon;
    }

	private void InitializeComponent()
	{
		SuspendLayout();
	
		// Form settings
		Text = "TrafficPilot - Proxy Manager";
		Size = new Size(800, 600);
		MinimumSize = new Size(600, 400);
		StartPosition = FormStartPosition.CenterScreen;
		// Icon will be set by LoadApplicationIcon()

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

		// About Tab
		_aboutTab = new TabPage { Text = "About", Padding = new Padding(10) };
		CreateAboutTab();
		_tabControl.TabPages.Add(_aboutTab);

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
			Dock = DockStyle.Fill,  // 改为 Fill，填充整个标签页
			AutoSize = false,        // 改为 false，避免固定大小
			ColumnCount = 2,
			RowCount = 12,
			Padding = new Padding(10)
		};

		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		int row = 0;

		// Proxy settings
		panel.Controls.Add(new Label { Text = "Proxy Host:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		_txtProxyHost = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(5) };
		_txtProxyHost.Text = _currentConfig.Proxy?.Host ?? "host.docker.internal";
		panel.Controls.Add(_txtProxyHost, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		panel.Controls.Add(new Label { Text = "Proxy Port:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		_numProxyPort = new NumericUpDown { Dock = DockStyle.Fill, Margin = new Padding(5), Maximum = 65535, Value = _currentConfig.Proxy?.Port ?? 7890 };
		panel.Controls.Add(_numProxyPort, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		panel.Controls.Add(new Label { Text = "Proxy Scheme:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		_cmbProxyScheme = new ComboBox { Dock = DockStyle.Fill, Margin = new Padding(5), DropDownStyle = ComboBoxStyle.DropDownList };
		_cmbProxyScheme.Items.AddRange(["socks4", "socks5", "http"]);
		_cmbProxyScheme.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks4";
		panel.Controls.Add(_cmbProxyScheme, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		// Process Names - 使用百分比高度，随窗口缩放
		panel.Controls.Add(new Label { Text = "Process Names:", TextAlign = ContentAlignment.TopRight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		var procPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		procPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
		_lstProcesses = new ListBox { Dock = DockStyle.Fill };
		foreach (var proc in _currentConfig.Targeting?.ProcessNames ?? [])
			_lstProcesses.Items.Add(proc);
		procPanel.Controls.Add(_lstProcesses, 0, 0);
		_btnRemoveProcess = new Button { Text = "Remove", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnRemoveProcess.Click += (s, e) => { if (_lstProcesses.SelectedIndex >= 0) _lstProcesses.Items.RemoveAt(_lstProcesses.SelectedIndex); };
		procPanel.Controls.Add(_btnRemoveProcess, 1, 0);
		panel.Controls.Add(procPanel, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
		row++;

		panel.Controls.Add(new Label { Text = "Add Process:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		var addProcPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		addProcPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
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
		panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		row++;

		// Extra PIDs - 使用百分比高度，随窗口缩放
		panel.Controls.Add(new Label { Text = "Extra PIDs:", TextAlign = ContentAlignment.TopRight, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		var pidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		pidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
		_lstExtraPids = new ListBox { Dock = DockStyle.Fill };
		foreach (var pid in _currentConfig.Targeting?.ExtraPids ?? [])
			_lstExtraPids.Items.Add(pid);
		pidPanel.Controls.Add(_lstExtraPids, 0, 0);
		_btnRemovePid = new Button { Text = "Remove", Dock = DockStyle.Fill, Margin = new Padding(2) };
		_btnRemovePid.Click += (s, e) => { if (_lstExtraPids.SelectedIndex >= 0) _lstExtraPids.Items.RemoveAt(_lstExtraPids.SelectedIndex); };
		pidPanel.Controls.Add(_btnRemovePid, 1, 0);
		panel.Controls.Add(pidPanel, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
		row++;

		panel.Controls.Add(new Label { Text = "Add PID:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		var addPidPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(5) };
		addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		addPidPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
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
		panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
		row++;

		// Config file info
		panel.Controls.Add(new Label { Text = "Config File:", TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Left | AnchorStyles.Right }, 0, row);
		_lblConfigFile = new Label { Text = _configManager.GetConfigPath(), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Dock = DockStyle.Fill, Margin = new Padding(5) };
		panel.Controls.Add(_lblConfigFile, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		// Buttons
		var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(5), FlowDirection = FlowDirection.RightToLeft };
		_btnSaveConfig = new Button { Text = "Save Config", Width = 100, Height = 30, Margin = new Padding(2) };
		_btnSaveConfig.Click += BtnSaveConfig_Click;
		btnPanel.Controls.Add(_btnSaveConfig);
		_btnLoadConfig = new Button { Text = "Load Config", Width = 100, Height = 30, Margin = new Padding(2) };
		_btnLoadConfig.Click += BtnLoadConfig_Click;
		btnPanel.Controls.Add(_btnLoadConfig);
		panel.Controls.Add(btnPanel, 1, row);
		panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// 最后一行使用 Percent 填充剩余空间，避免控件挤在一起
		panel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

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
			Font = new Font("Courier New", 9),
			WordWrap = false
		};
		logPanel.Controls.Add(_rtbLogs, 0, 0);

		var btnClearPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(5) };
		_btnClearLogs = new Button { Text = "Clear Logs", Width = 80, Height = 30 };
		_btnClearLogs.Click += (s, e) => _rtbLogs.Clear();
		btnClearPanel.Controls.Add(_btnClearLogs);
		logPanel.Controls.Add(btnClearPanel, 0, 1);

		_logsTab!.Controls.Add(logPanel);
	}

	private void CreateAboutTab()
	{
		var scrollPanel = new Panel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			Margin = new Padding(0),
			Padding = new Padding(10)
		};

		var contentPanel = new TableLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			ColumnCount = 1,
			RowCount = 7,
			Padding = new Padding(10),
			Margin = new Padding(0),
			Dock = DockStyle.Top
		};
		contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

		int row = 0;

		// Title
		var titleLabel = new Label
		{
			Text = "TrafficPilot - Proxy Manager",
			Font = new Font("Segoe UI", 16, FontStyle.Bold),
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 10)
		};
		contentPanel.Controls.Add(titleLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		// Version
		var versionLabel = new Label
		{
			Text = "Version: 1.0.0\nRelease Date: 2024",
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 15),
			ForeColor = SystemColors.GrayText
		};
		contentPanel.Controls.Add(versionLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
	 row++;

		// Description
		var descLabel = new Label
		{
			Text = "Project Description",
			Font = new Font("Segoe UI", 11, FontStyle.Bold),
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 5)
		};
		contentPanel.Controls.Add(descLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		var descContentLabel = new Label
		{
			Text = "TrafficPilot is a Windows proxy manager that allows you to route network traffic from specific processes through a proxy server.\n\n" +
				   "Features:\n" +
				   "• Intercept and redirect traffic from specified processes\n" +
				   "• Support for wildcard process matching\n" +
				   "• Support for SOCKS4, SOCKS5, and HTTP proxies\n" +
				   "• Real-time logging and statistics\n" +
				   "• Configuration save/load functionality\n" +
				   "• Process filtering and PID-based targeting",
			AutoSize = true,
			Margin = new Padding(10, 0, 0, 15),
			MaximumSize = new Size(450, 0)
		};
		contentPanel.Controls.Add(descContentLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		// Contributors
		var contribLabel = new Label
		{
			Text = "Contributors & Acknowledgments",
			Font = new Font("Segoe UI", 11, FontStyle.Bold),
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 5)
		};
		contentPanel.Controls.Add(contribLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		var contribContentLabel = new Label
		{
			Text = "• Original Author: maikebing\n" +
				   "• Repository: github.com/maikebing/TrafficPilot\n" +
				   "• WinDivert: Windows Packet Divert library\n" +
				   "• Contributors: Community members and testers",
			AutoSize = true,
			Margin = new Padding(10, 0, 0, 15),
			MaximumSize = new Size(450, 0)
		};
		contentPanel.Controls.Add(contribContentLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		// Tech Info
		var techLabel = new Label
		{
			Text = "Technical Information",
			Font = new Font("Segoe UI", 11, FontStyle.Bold),
			AutoSize = true,
			Margin = new Padding(0, 0, 0, 5)
		};
		contentPanel.Controls.Add(techLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		row++;

		var techContentLabel = new Label
		{
			Text = $"Platform: Windows\n" +
				   $".NET Version: .NET 10\n" +
				   $"C# Version: 14.0\n" +
				   $"Architecture: x64\n" +
				   $"License: Open Source",
			AutoSize = true,
			Margin = new Padding(10, 0, 0, 0),
			MaximumSize = new Size(450, 0)
		};
		contentPanel.Controls.Add(techContentLabel, 0, row);
		contentPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		scrollPanel.Controls.Add(contentPanel);
		_aboutTab!.Controls.Add(scrollPanel);
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
			// Icon will be set by LoadApplicationIcon() after this method
			Text = "TrafficPilot - Proxy Manager",
			ContextMenuStrip = _contextMenu,
			Visible = true
		};
		_notifyIcon.DoubleClick += (s, e) => ShowWindow();
		
		UpdateTrayMenuState();
	}

	private async void BtnStartStop_Click(object? sender, EventArgs e)
	{
		if (_isStarting) return;
		_isStarting = true;
		_btnStartStop!.Enabled = false;

		try
		{
			if (_engine == null || !_engine.IsRunning)
			{
				_btnStartStop.Text = "Starting...";
				await StartProxyAsync();
			}
			else
			{
				_btnStartStop.Text = "Stopping...";
				await StopProxyAsync();
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
		_contextMenu.Items[3].Enabled = !isRunning;  // Start Proxy
		_contextMenu.Items[4].Enabled = isRunning;   // Stop Proxy
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
		catch (Exception ex)
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
		catch (Exception ex)
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
		}
		base.Dispose(disposing);
	}
}
