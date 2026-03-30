namespace TrafficPilot;

internal partial class GatewayProviderSettingsControl : UserControl
{
	private bool _isUpdatingApiKeyPresentation;
	private bool _isShowingMaskedApiKey;
	private string _apiKeyValue = string.Empty;

	public GatewayProviderSettingsControl()
	{
		InitializeComponent();
		_txtApiKey.PasswordChar = '\0';
		_txtApiKey.Enter += TxtApiKey_Enter;
		_txtApiKey.Leave += TxtApiKey_Leave;
	}

 	public CheckBox EnabledInput => _chkEnabled;
  public TextBox BaseUrlInput => _txtBaseUrl;
	public TextBox ApiKeyInput => _txtApiKey;
	public string ApiKeyValue => GetApiKeyValue();

	public void SetApiKey(string? apiKey)
	{
		_apiKeyValue = apiKey?.Trim() ?? string.Empty;
		ApplyMaskedApiKeyPresentation();
	}

 public void ApplyProviderPreset(string providerLabel, string suffixHint, string baseUrlPlaceholder)
	{
		_grpBasic.Text = $"{providerLabel} Provider";
		_chkEnabled.Text = $"Enable {providerLabel}";
		_lblProviderUrl.Text = "Provider Base URL:";
     _txtBaseUrl.PlaceholderText = baseUrlPlaceholder;

       _lblModelSuffix.Text = "Model Suffix:";
		_txtModelSuffix.ReadOnly = true;
		_txtModelSuffix.TabStop = false;
		_txtModelSuffix.Text = suffixHint;
		_txtModelSuffix.BackColor = SystemColors.Control;
	}

	private string GetApiKeyValue()
	{
		if (!_isShowingMaskedApiKey)
			_apiKeyValue = _txtApiKey.Text.Trim();

		return _apiKeyValue;
	}

	private void TxtApiKey_Enter(object? sender, EventArgs e)
	{
		if (_isUpdatingApiKeyPresentation || !_isShowingMaskedApiKey)
			return;

		_isUpdatingApiKeyPresentation = true;
		try
		{
			_isShowingMaskedApiKey = false;
			_txtApiKey.Text = _apiKeyValue;
			_txtApiKey.SelectionStart = 0;
			_txtApiKey.SelectionLength = _txtApiKey.TextLength;
		}
		finally
		{
			_isUpdatingApiKeyPresentation = false;
		}
	}

	private void TxtApiKey_Leave(object? sender, EventArgs e)
	{
		if (_isUpdatingApiKeyPresentation)
			return;

		_apiKeyValue = _txtApiKey.Text.Trim();
		ApplyMaskedApiKeyPresentation();
	}

	private void ApplyMaskedApiKeyPresentation()
	{
		_isUpdatingApiKeyPresentation = true;
		try
		{
			_isShowingMaskedApiKey = !string.IsNullOrEmpty(_apiKeyValue);
			_txtApiKey.Text = MaskApiKey(_apiKeyValue);
			_txtApiKey.SelectionStart = 0;
			_txtApiKey.SelectionLength = 0;
		}
		finally
		{
			_isUpdatingApiKeyPresentation = false;
		}
	}

	private static string MaskApiKey(string? apiKey)
	{
		if (string.IsNullOrWhiteSpace(apiKey))
			return string.Empty;

		var trimmed = apiKey.Trim();
		if (trimmed.Length <= 8)
			return trimmed;

		return $"{trimmed[..4]}{new string('*', trimmed.Length - 8)}{trimmed[^4..]}";
	}
}
