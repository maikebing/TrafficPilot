namespace TrafficPilot;

internal partial class GatewayProviderSettingsControl : UserControl
{
	public GatewayProviderSettingsControl()
	{
		InitializeComponent();
	}

	public TextBox BaseUrlTextBox => _txtBaseUrlEditor;
	public TextBox ApiKeyTextBox => _txtApiKey;

	public void ApplySimpleMode(string providerLabel, string suffixHint, string baseUrlPlaceholder)
	{
      _grpBasic.Text = $"{providerLabel} Connection";
		_lblProviderUrl.Text = "Provider Base URL:";
		_txtBaseUrlEditor.PlaceholderText = baseUrlPlaceholder;

		_lblDisplayNameEditor.Text = "Model Suffix:";
		_txtDisplayNameEditor.ReadOnly = true;
		_txtDisplayNameEditor.TabStop = false;
		_txtDisplayNameEditor.Text = suffixHint;
		_txtDisplayNameEditor.BackColor = SystemColors.Control;
	}
}
