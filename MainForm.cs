using System.ComponentModel;
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

	private LogBuffer? _logBuffer;

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
		LoadConfigToUI();
	}

	private void LoadApplicationIcon()
	{
		_notifyIcon!.Icon = Resources.favicon;
		Icon = Resources.favicon;
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
	
		_btnStartStop!.Enabled = false;

		try
		{
			if (_engine == null || !_engine.IsRunning)
			{
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
		if (_contextMenu?.Items.Count < 7)
			return;

		bool isRunning = _engine?.IsRunning ?? false;
		_contextMenu?.Items[3].Enabled = !isRunning;  // Start Proxy
		_contextMenu?.Items[4].Enabled = isRunning;   // Stop Proxy
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
		_currentConfig = BuildConfigModel();
		_configManager.Save(_currentConfig);
		MessageBox.Show("Configuration saved successfully.", "Save Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void BtnLoadConfig_Click(object? sender, EventArgs e)
	{
		_currentConfig = _configManager.Load();
		LoadConfigToUI();
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

	private void LoadConfigToUI()
	{
		_txtProxyHost!.Text = _currentConfig.Proxy?.Host ?? "";
		_numProxyPort!.Value = _currentConfig.Proxy?.Port ?? 7890;
		_cmbProxyScheme!.SelectedItem = _currentConfig.Proxy?.Scheme ?? "socks4";
		_lblConfigFileValue!.Text = _configManager.GetConfigPath();

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

	private void BtnRemoveProcess_Click(object? sender, EventArgs e)
	{
		if (_lstProcesses?.SelectedIndex >= 0)
			_lstProcesses.Items.RemoveAt(_lstProcesses.SelectedIndex);
	}

	private void BtnAddProcess_Click(object? sender, EventArgs e)
	{
		if (!string.IsNullOrWhiteSpace(_txtNewProcess?.Text))
		{
			_lstProcesses?.Items.Add(_txtNewProcess.Text);
			_txtNewProcess.Clear();
		}
	}

	private void BtnRemovePid_Click(object? sender, EventArgs e)
	{
		if (_lstExtraPids?.SelectedIndex >= 0)
			_lstExtraPids.Items.RemoveAt(_lstExtraPids.SelectedIndex);
	}

	private void BtnAddPid_Click(object? sender, EventArgs e)
	{
		if (int.TryParse(_txtNewPid?.Text, out int pid))
		{
			_lstExtraPids?.Items.Add(pid);
			_txtNewPid!.Clear();
		}
	}

	private void BtnClearLogs_Click(object? sender, EventArgs e)
	{
		_rtbLogs?.Clear();
	}
}
