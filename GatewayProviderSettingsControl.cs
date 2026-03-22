namespace TrafficPilot;

internal partial class GatewayProviderSettingsControl : UserControl
{
	private bool _isSynchronizingFields;

	public GatewayProviderSettingsControl()
	{
		InitializeComponent();
		_txtBaseUrlEditor.TextChanged += BaseUrlEditor_TextChanged;
		_txtBaseUrl.TextChanged += BaseUrlAdvanced_TextChanged;
		_txtDisplayNameEditor.TextChanged += DisplayNameEditor_TextChanged;
		_txtDisplayName.TextChanged += DisplayNameAdvanced_TextChanged;
	}

	public CheckBox ShowAdvancedCheckBox => _chkShowAdvanced;
	public GroupBox AdvancedGroupBox => _grpAdvanced;
	public TextBox ProviderIdTextBox => _txtProviderId;
	public ComboBox ProtocolComboBox => _cmbProtocol;
	public TextBox DisplayNameTextBox => _txtDisplayNameEditor;
	public TextBox BaseUrlTextBox => _txtBaseUrlEditor;
	public ComboBox DefaultModelComboBox => _cmbDefaultModel;
	public ComboBox DefaultEmbeddingModelComboBox => _cmbDefaultEmbeddingModel;
	public TextBox ApiKeyTextBox => _txtApiKey;
	public ComboBox AuthTypeComboBox => _cmbAuthType;
	public TextBox AuthHeaderTextBox => _txtAuthHeader;
	public TextBox ChatEndpointTextBox => _txtChatEndpoint;
	public TextBox EmbeddingsEndpointTextBox => _txtEmbeddingsEndpoint;
	public TextBox ResponsesEndpointTextBox => _txtResponsesEndpoint;
	public TextBox AdditionalHeadersTextBox => _txtAdditionalHeaders;
	public CheckBox SupportsChatCheckBox => _chkSupportsChat;
	public CheckBox SupportsEmbeddingsCheckBox => _chkSupportsEmbeddings;
	public CheckBox SupportsResponsesCheckBox => _chkSupportsResponses;
	public CheckBox SupportsStreamingCheckBox => _chkSupportsStreaming;
	public Button DetectButton => _btnDetectProvider;
	public Button RefreshModelsButton => _btnRefreshModels;
	public Button RefreshModelsApplyButton => _btnRefreshModelsApply;
	public ListBox ModelPreviewListBox => _lstModelPreview;
	public TextBox ModelMetadataTextBox => _txtModelMetadata;
	public Button CopyModelMetadataButton => _btnCopyModelMetadata;

	private void BaseUrlEditor_TextChanged(object? sender, EventArgs e)
	{
		SyncText(_txtBaseUrlEditor, _txtBaseUrl);
	}

	private void BaseUrlAdvanced_TextChanged(object? sender, EventArgs e)
	{
		SyncText(_txtBaseUrl, _txtBaseUrlEditor);
	}

	private void DisplayNameEditor_TextChanged(object? sender, EventArgs e)
	{
		SyncText(_txtDisplayNameEditor, _txtDisplayName);
	}

	private void DisplayNameAdvanced_TextChanged(object? sender, EventArgs e)
	{
		SyncText(_txtDisplayName, _txtDisplayNameEditor);
	}

	private void SyncText(TextBox source, TextBox target)
	{
		if (_isSynchronizingFields || target.Text == source.Text)
			return;

		_isSynchronizingFields = true;
		try
		{
			target.Text = source.Text;
		}
		finally
		{
			_isSynchronizingFields = false;
		}
	}
}
