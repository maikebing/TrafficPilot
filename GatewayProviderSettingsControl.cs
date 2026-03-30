namespace TrafficPilot;

internal partial class GatewayProviderSettingsControl : UserControl
{
	public GatewayProviderSettingsControl()
	{
		InitializeComponent();
	}

  public TextBox BaseUrlInput => _txtBaseUrl;
	public TextBox ApiKeyInput => _txtApiKey;

 public void ApplyProviderPreset(string providerLabel, string suffixHint, string baseUrlPlaceholder)
	{
       _grpBasic.Text = $"{providerLabel} Connection";
		_lblProviderUrl.Text = "Provider Base URL:";
     _txtBaseUrl.PlaceholderText = baseUrlPlaceholder;

       _lblModelSuffix.Text = "Model Suffix:";
		_txtModelSuffix.ReadOnly = true;
		_txtModelSuffix.TabStop = false;
		_txtModelSuffix.Text = suffixHint;
		_txtModelSuffix.BackColor = SystemColors.Control;
	}
}
