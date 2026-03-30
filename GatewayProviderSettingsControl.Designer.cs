namespace TrafficPilot;

partial class GatewayProviderSettingsControl
{
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}

		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		_rootPanel = new TableLayoutPanel();
		_grpBasic = new GroupBox();
		_basicPanel = new TableLayoutPanel();
		_chkEnabled = new CheckBox();
		_lblProviderUrl = new Label();
      _txtBaseUrl = new TextBox();
		_lblApiKey = new Label();
		_txtApiKey = new TextBox();
        _lblModelSuffix = new Label();
		_txtModelSuffix = new TextBox();
		_rootPanel.SuspendLayout();
		_grpBasic.SuspendLayout();
		_basicPanel.SuspendLayout();
		SuspendLayout();
		// 
		// _rootPanel
		// 
		_rootPanel.ColumnCount = 1;
		_rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_rootPanel.Controls.Add(_grpBasic, 0, 0);
		_rootPanel.Dock = DockStyle.Fill;
		_rootPanel.Location = new Point(0, 0);
		_rootPanel.Margin = new Padding(0);
		_rootPanel.Name = "_rootPanel";
		_rootPanel.RowCount = 1;
		_rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_rootPanel.Size = new Size(719, 172);
		_rootPanel.TabIndex = 0;
		// 
		// _grpBasic
		// 
		_grpBasic.Controls.Add(_basicPanel);
		_grpBasic.Dock = DockStyle.Fill;
		_grpBasic.Location = new Point(3, 3);
		_grpBasic.Name = "_grpBasic";
		_grpBasic.Size = new Size(713, 166);
		_grpBasic.TabIndex = 0;
		_grpBasic.TabStop = false;
		_grpBasic.Text = "Connection";
		// 
		// _basicPanel
		// 
		_basicPanel.ColumnCount = 2;
		_basicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
		_basicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_basicPanel.Controls.Add(_chkEnabled, 0, 0);
		_basicPanel.Controls.Add(_lblProviderUrl, 0, 1);
	      _basicPanel.Controls.Add(_txtBaseUrl, 1, 1);
		_basicPanel.Controls.Add(_lblApiKey, 0, 2);
		_basicPanel.Controls.Add(_txtApiKey, 1, 2);
	      _basicPanel.Controls.Add(_lblModelSuffix, 0, 3);
		_basicPanel.Controls.Add(_txtModelSuffix, 1, 3);
		_basicPanel.Dock = DockStyle.Fill;
		_basicPanel.Location = new Point(3, 19);
		_basicPanel.Name = "_basicPanel";
		_basicPanel.RowCount = 4;
		_basicPanel.RowStyles.Add(new RowStyle());
		_basicPanel.RowStyles.Add(new RowStyle());
		_basicPanel.RowStyles.Add(new RowStyle());
		_basicPanel.RowStyles.Add(new RowStyle());
		_basicPanel.Size = new Size(707, 144);
		_basicPanel.TabIndex = 0;
		_basicPanel.SetColumnSpan(_chkEnabled, 2);
		// 
		// _chkEnabled
		// 
		_chkEnabled.AutoSize = true;
		_chkEnabled.Location = new Point(3, 3);
		_chkEnabled.Margin = new Padding(3, 3, 3, 8);
		_chkEnabled.Name = "_chkEnabled";
		_chkEnabled.Size = new Size(119, 21);
		_chkEnabled.TabIndex = 0;
		_chkEnabled.Text = "Enable Provider";
		_chkEnabled.UseVisualStyleBackColor = true;
		// 
		// _lblProviderUrl
		// 
		_lblProviderUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_lblProviderUrl.Location = new Point(3, 39);
		_lblProviderUrl.Name = "_lblProviderUrl";
       _lblProviderUrl.Size = new Size(144, 23);
		_lblProviderUrl.Text = "Provider Base URL:";
		_lblProviderUrl.TextAlign = ContentAlignment.MiddleRight;
		// 
        // _txtBaseUrl
		// 
        _txtBaseUrl.Dock = DockStyle.Fill;
		_txtBaseUrl.Location = new Point(153, 38);
		_txtBaseUrl.Margin = new Padding(3, 5, 3, 5);
		_txtBaseUrl.Name = "_txtBaseUrl";
		_txtBaseUrl.PlaceholderText = "https://api.openai.com/v1/";
		_txtBaseUrl.Size = new Size(551, 23);
		_txtBaseUrl.TabIndex = 1;
		// 
		// _lblApiKey
		// 
     _lblApiKey.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_lblApiKey.Location = new Point(3, 73);
		_lblApiKey.Name = "_lblApiKey";
		_lblApiKey.Size = new Size(144, 23);
        _lblApiKey.Text = "Provider API Key:";
		_lblApiKey.TextAlign = ContentAlignment.MiddleRight;
		// 
		// _txtApiKey
		// 
		_txtApiKey.Dock = DockStyle.Fill;
		_txtApiKey.Location = new Point(153, 72);
		_txtApiKey.Name = "_txtApiKey";
		_txtApiKey.PasswordChar = '●';
		_txtApiKey.PlaceholderText = "Stored in Windows Credential Manager, not in config JSON";
		_txtApiKey.Size = new Size(551, 23);
		_txtApiKey.TabIndex = 3;
		// 
        // _lblModelSuffix
		// 
      _lblModelSuffix.Anchor = AnchorStyles.Left | AnchorStyles.Right;
		_lblModelSuffix.Location = new Point(3, 102);
		_lblModelSuffix.Name = "_lblModelSuffix";
		_lblModelSuffix.Size = new Size(144, 23);
		_lblModelSuffix.Text = "Model Suffix:";
        _lblModelSuffix.TextAlign = ContentAlignment.MiddleRight;
		// 
        // _txtModelSuffix
		// 
        _txtModelSuffix.Dock = DockStyle.Fill;
		_txtModelSuffix.Location = new Point(153, 101);
		_txtModelSuffix.Name = "_txtModelSuffix";
		_txtModelSuffix.ReadOnly = true;
		_txtModelSuffix.Size = new Size(551, 23);
		_txtModelSuffix.TabIndex = 5;
		// 
		// GatewayProviderSettingsControl
		// 
		AutoScaleDimensions = new SizeF(7F, 17F);
		AutoScaleMode = AutoScaleMode.Font;
		Controls.Add(_rootPanel);
		Name = "GatewayProviderSettingsControl";
		Size = new Size(719, 172);
		_rootPanel.ResumeLayout(false);
		_grpBasic.ResumeLayout(false);
		_basicPanel.ResumeLayout(false);
		_basicPanel.PerformLayout();
		ResumeLayout(false);
	}

	private TableLayoutPanel _rootPanel;
	private GroupBox _grpBasic;
	private TableLayoutPanel _basicPanel;
	private CheckBox _chkEnabled;
	private Label _lblProviderUrl;
  private TextBox _txtBaseUrl;
	private Label _lblApiKey;
	private TextBox _txtApiKey;
    private Label _lblModelSuffix;
	private TextBox _txtModelSuffix;
}
