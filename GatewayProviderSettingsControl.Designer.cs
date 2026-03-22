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
        _scrollPanel = new Panel();
        _grpAdvanced = new GroupBox();
        _advancedLayout = new TableLayoutPanel();
        _providerDetailsPanel = new TableLayoutPanel();
        _lblProviderId = new Label();
        _txtProviderId = new TextBox();
        _lblProtocol = new Label();
        _cmbProtocol = new ComboBox();
        _lblDisplayName = new Label();
        _txtDisplayName = new TextBox();
        _lblBaseUrl = new Label();
        _txtBaseUrl = new TextBox();
        _lblDefaultModel = new Label();
        _cmbDefaultModel = new ComboBox();
        _lblDefaultEmbeddingModel = new Label();
        _cmbDefaultEmbeddingModel = new ComboBox();
        _grpAuth = new GroupBox();
        _authPanel = new TableLayoutPanel();
        _lblAuthType = new Label();
        _cmbAuthType = new ComboBox();
        _lblAuthHeader = new Label();
        _txtAuthHeader = new TextBox();
        _grpEndpoints = new GroupBox();
        _endpointsPanel = new TableLayoutPanel();
        _lblChatEndpoint = new Label();
        _txtChatEndpoint = new TextBox();
        _lblEmbeddingsEndpoint = new Label();
        _txtEmbeddingsEndpoint = new TextBox();
        _lblResponsesEndpoint = new Label();
        _txtResponsesEndpoint = new TextBox();
        _lblAdditionalHeaders = new Label();
        _txtAdditionalHeaders = new TextBox();
        _grpCapabilities = new GroupBox();
        _capabilitiesPanel = new FlowLayoutPanel();
        _chkSupportsChat = new CheckBox();
        _chkSupportsEmbeddings = new CheckBox();
        _chkSupportsResponses = new CheckBox();
        _chkSupportsStreaming = new CheckBox();
        _chkShowAdvanced = new CheckBox();
        _grpBasic = new GroupBox();
        _basicPanel = new TableLayoutPanel();
        _lblProviderUrl = new Label();
        _providerUrlPanel = new TableLayoutPanel();
        _txtBaseUrlEditor = new TextBox();
        _btnRefreshModels = new Button();
        _lblApiKey = new Label();
        _txtApiKey = new TextBox();
        _lblDisplayNameEditor = new Label();
        _txtDisplayNameEditor = new TextBox();
        _modelActionsPanel = new FlowLayoutPanel();
        _btnRefreshModelsApply = new Button();
        _btnCopyModelMetadata = new Button();
        _btnDetectProvider = new Button();
        _lblModelPreview = new Label();
        _lstModelPreview = new ListBox();
        _txtModelMetadata = new TextBox();
        _lblModelMetadata = new Label();
        _rootPanel.SuspendLayout();
        _scrollPanel.SuspendLayout();
        _grpAdvanced.SuspendLayout();
        _advancedLayout.SuspendLayout();
        _providerDetailsPanel.SuspendLayout();
        _grpAuth.SuspendLayout();
        _authPanel.SuspendLayout();
        _grpEndpoints.SuspendLayout();
        _endpointsPanel.SuspendLayout();
        _grpCapabilities.SuspendLayout();
        _capabilitiesPanel.SuspendLayout();
        _grpBasic.SuspendLayout();
        _basicPanel.SuspendLayout();
        _providerUrlPanel.SuspendLayout();
        _modelActionsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _rootPanel
        // 
        _rootPanel.ColumnCount = 1;
        _rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootPanel.Controls.Add(_scrollPanel, 0, 0);
        _rootPanel.Dock = DockStyle.Fill;
        _rootPanel.Location = new Point(0, 0);
        _rootPanel.Margin = new Padding(0);
        _rootPanel.Name = "_rootPanel";
        _rootPanel.RowCount = 1;
        _rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootPanel.Size = new Size(719, 683);
        _rootPanel.TabIndex = 0;
        // 
        // _scrollPanel
        // 
        _scrollPanel.AutoScroll = true;
        _scrollPanel.BorderStyle = BorderStyle.FixedSingle;
        _scrollPanel.Controls.Add(_grpAdvanced);
        _scrollPanel.Controls.Add(_chkShowAdvanced);
        _scrollPanel.Controls.Add(_grpBasic);
        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.Location = new Point(3, 3);
        _scrollPanel.Name = "_scrollPanel";
        _scrollPanel.Size = new Size(713, 677);
        _scrollPanel.TabIndex = 0;
        // 
        // _grpAdvanced
        // 
        _grpAdvanced.Controls.Add(_advancedLayout);
        _grpAdvanced.Dock = DockStyle.Top;
        _grpAdvanced.Location = new Point(0, 319);
        _grpAdvanced.Name = "_grpAdvanced";
        _grpAdvanced.Size = new Size(711, 345);
        _grpAdvanced.TabIndex = 2;
        _grpAdvanced.TabStop = false;
        _grpAdvanced.Text = "Advanced Settings";
        // 
        // _advancedLayout
        // 
        _advancedLayout.ColumnCount = 1;
        _advancedLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _advancedLayout.Controls.Add(_providerDetailsPanel, 0, 0);
        _advancedLayout.Controls.Add(_grpAuth, 0, 1);
        _advancedLayout.Controls.Add(_grpEndpoints, 0, 2);
        _advancedLayout.Controls.Add(_grpCapabilities, 0, 3);
        _advancedLayout.Dock = DockStyle.Fill;
        _advancedLayout.Location = new Point(3, 19);
        _advancedLayout.Name = "_advancedLayout";
        _advancedLayout.RowCount = 4;
        _advancedLayout.RowStyles.Add(new RowStyle());
        _advancedLayout.RowStyles.Add(new RowStyle());
        _advancedLayout.RowStyles.Add(new RowStyle());
        _advancedLayout.RowStyles.Add(new RowStyle());
        _advancedLayout.Size = new Size(705, 323);
        _advancedLayout.TabIndex = 0;
        // 
        // _providerDetailsPanel
        // 
        _providerDetailsPanel.ColumnCount = 4;
        _providerDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _providerDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _providerDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _providerDetailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _providerDetailsPanel.Controls.Add(_lblProviderId, 0, 0);
        _providerDetailsPanel.Controls.Add(_txtProviderId, 1, 0);
        _providerDetailsPanel.Controls.Add(_lblProtocol, 2, 0);
        _providerDetailsPanel.Controls.Add(_cmbProtocol, 3, 0);
        _providerDetailsPanel.Controls.Add(_lblDisplayName, 0, 1);
        _providerDetailsPanel.Controls.Add(_txtDisplayName, 1, 1);
        _providerDetailsPanel.Controls.Add(_lblBaseUrl, 2, 1);
        _providerDetailsPanel.Controls.Add(_txtBaseUrl, 3, 1);
        _providerDetailsPanel.Controls.Add(_lblDefaultModel, 0, 2);
        _providerDetailsPanel.Controls.Add(_cmbDefaultModel, 1, 2);
        _providerDetailsPanel.Controls.Add(_lblDefaultEmbeddingModel, 2, 2);
        _providerDetailsPanel.Controls.Add(_cmbDefaultEmbeddingModel, 3, 2);
        _providerDetailsPanel.Dock = DockStyle.Top;
        _providerDetailsPanel.Location = new Point(3, 0);
        _providerDetailsPanel.Margin = new Padding(3, 0, 3, 6);
        _providerDetailsPanel.Name = "_providerDetailsPanel";
        _providerDetailsPanel.RowCount = 3;
        _providerDetailsPanel.RowStyles.Add(new RowStyle());
        _providerDetailsPanel.RowStyles.Add(new RowStyle());
        _providerDetailsPanel.RowStyles.Add(new RowStyle());
        _providerDetailsPanel.Size = new Size(699, 90);
        _providerDetailsPanel.TabIndex = 0;
        // 
        // _lblProviderId
        // 
        _lblProviderId.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProviderId.Location = new Point(3, 4);
        _lblProviderId.Name = "_lblProviderId";
        _lblProviderId.Size = new Size(84, 23);
        _lblProviderId.TabIndex = 0;
        _lblProviderId.Text = "Id:";
        _lblProviderId.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtProviderId
        // 
        _txtProviderId.Dock = DockStyle.Fill;
        _txtProviderId.Location = new Point(93, 3);
        _txtProviderId.Name = "_txtProviderId";
        _txtProviderId.Size = new Size(243, 23);
        _txtProviderId.TabIndex = 1;
        // 
        // _lblProtocol
        // 
        _lblProtocol.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProtocol.Location = new Point(342, 4);
        _lblProtocol.Name = "_lblProtocol";
        _lblProtocol.Size = new Size(104, 23);
        _lblProtocol.TabIndex = 2;
        _lblProtocol.Text = "Protocol:";
        _lblProtocol.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbProtocol
        // 
        _cmbProtocol.Dock = DockStyle.Fill;
        _cmbProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbProtocol.FormattingEnabled = true;
        _cmbProtocol.Items.AddRange(new object[] { "OpenAICompatible", "Anthropic" });
        _cmbProtocol.Location = new Point(452, 3);
        _cmbProtocol.Name = "_cmbProtocol";
        _cmbProtocol.Size = new Size(244, 25);
        _cmbProtocol.TabIndex = 3;
        // 
        // _lblDisplayName
        // 
        _lblDisplayName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblDisplayName.Location = new Point(3, 34);
        _lblDisplayName.Name = "_lblDisplayName";
        _lblDisplayName.Size = new Size(84, 23);
        _lblDisplayName.TabIndex = 4;
        _lblDisplayName.Text = "Name:";
        _lblDisplayName.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtDisplayName
        // 
        _txtDisplayName.Dock = DockStyle.Fill;
        _txtDisplayName.Location = new Point(93, 34);
        _txtDisplayName.Name = "_txtDisplayName";
        _txtDisplayName.Size = new Size(243, 23);
        _txtDisplayName.TabIndex = 5;
        // 
        // _lblBaseUrl
        // 
        _lblBaseUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblBaseUrl.Location = new Point(342, 34);
        _lblBaseUrl.Name = "_lblBaseUrl";
        _lblBaseUrl.Size = new Size(104, 23);
        _lblBaseUrl.TabIndex = 6;
        _lblBaseUrl.Text = "Base URL:";
        _lblBaseUrl.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtBaseUrl
        // 
        _txtBaseUrl.Dock = DockStyle.Fill;
        _txtBaseUrl.Location = new Point(452, 34);
        _txtBaseUrl.Name = "_txtBaseUrl";
        _txtBaseUrl.Size = new Size(244, 23);
        _txtBaseUrl.TabIndex = 7;
        // 
        // _lblDefaultModel
        // 
        _lblDefaultModel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblDefaultModel.Location = new Point(3, 64);
        _lblDefaultModel.Name = "_lblDefaultModel";
        _lblDefaultModel.Size = new Size(84, 23);
        _lblDefaultModel.TabIndex = 8;
        _lblDefaultModel.Text = "Chat Model:";
        _lblDefaultModel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbDefaultModel
        // 
        _cmbDefaultModel.Dock = DockStyle.Fill;
        _cmbDefaultModel.FormattingEnabled = true;
        _cmbDefaultModel.Location = new Point(93, 63);
        _cmbDefaultModel.Name = "_cmbDefaultModel";
        _cmbDefaultModel.Size = new Size(243, 25);
        _cmbDefaultModel.TabIndex = 9;
        // 
        // _lblDefaultEmbeddingModel
        // 
        _lblDefaultEmbeddingModel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblDefaultEmbeddingModel.Location = new Point(342, 64);
        _lblDefaultEmbeddingModel.Name = "_lblDefaultEmbeddingModel";
        _lblDefaultEmbeddingModel.Size = new Size(104, 23);
        _lblDefaultEmbeddingModel.TabIndex = 10;
        _lblDefaultEmbeddingModel.Text = "Embedding:";
        _lblDefaultEmbeddingModel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbDefaultEmbeddingModel
        // 
        _cmbDefaultEmbeddingModel.Dock = DockStyle.Fill;
        _cmbDefaultEmbeddingModel.FormattingEnabled = true;
        _cmbDefaultEmbeddingModel.Location = new Point(452, 63);
        _cmbDefaultEmbeddingModel.Name = "_cmbDefaultEmbeddingModel";
        _cmbDefaultEmbeddingModel.Size = new Size(244, 25);
        _cmbDefaultEmbeddingModel.TabIndex = 11;
        // 
        // _grpAuth
        // 
        _grpAuth.Controls.Add(_authPanel);
        _grpAuth.Dock = DockStyle.Top;
        _grpAuth.Location = new Point(3, 99);
        _grpAuth.Name = "_grpAuth";
        _grpAuth.Size = new Size(699, 58);
        _grpAuth.TabIndex = 1;
        _grpAuth.TabStop = false;
        _grpAuth.Text = "Auth";
        // 
        // _authPanel
        // 
        _authPanel.ColumnCount = 4;
        _authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _authPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _authPanel.Controls.Add(_lblAuthType, 0, 0);
        _authPanel.Controls.Add(_cmbAuthType, 1, 0);
        _authPanel.Controls.Add(_lblAuthHeader, 2, 0);
        _authPanel.Controls.Add(_txtAuthHeader, 3, 0);
        _authPanel.Dock = DockStyle.Fill;
        _authPanel.Location = new Point(3, 19);
        _authPanel.Name = "_authPanel";
        _authPanel.RowCount = 1;
        _authPanel.RowStyles.Add(new RowStyle());
        _authPanel.Size = new Size(693, 36);
        _authPanel.TabIndex = 0;
        // 
        // _lblAuthType
        // 
        _lblAuthType.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblAuthType.Location = new Point(3, 6);
        _lblAuthType.Name = "_lblAuthType";
        _lblAuthType.Size = new Size(84, 23);
        _lblAuthType.TabIndex = 0;
        _lblAuthType.Text = "Auth Type:";
        _lblAuthType.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _cmbAuthType
        // 
        _cmbAuthType.Dock = DockStyle.Fill;
        _cmbAuthType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbAuthType.FormattingEnabled = true;
        _cmbAuthType.Items.AddRange(new object[] { "Bearer", "Header", "Query" });
        _cmbAuthType.Location = new Point(93, 3);
        _cmbAuthType.Name = "_cmbAuthType";
        _cmbAuthType.Size = new Size(240, 25);
        _cmbAuthType.TabIndex = 1;
        // 
        // _lblAuthHeader
        // 
        _lblAuthHeader.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblAuthHeader.Location = new Point(339, 6);
        _lblAuthHeader.Name = "_lblAuthHeader";
        _lblAuthHeader.Size = new Size(104, 23);
        _lblAuthHeader.TabIndex = 2;
        _lblAuthHeader.Text = "Auth Name:";
        _lblAuthHeader.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtAuthHeader
        // 
        _txtAuthHeader.Dock = DockStyle.Fill;
        _txtAuthHeader.Location = new Point(449, 3);
        _txtAuthHeader.Name = "_txtAuthHeader";
        _txtAuthHeader.Size = new Size(241, 23);
        _txtAuthHeader.TabIndex = 3;
        // 
        // _grpEndpoints
        // 
        _grpEndpoints.Controls.Add(_endpointsPanel);
        _grpEndpoints.Dock = DockStyle.Top;
        _grpEndpoints.Location = new Point(3, 163);
        _grpEndpoints.Name = "_grpEndpoints";
        _grpEndpoints.Size = new Size(699, 91);
        _grpEndpoints.TabIndex = 2;
        _grpEndpoints.TabStop = false;
        _grpEndpoints.Text = "Endpoints";
        // 
        // _endpointsPanel
        // 
        _endpointsPanel.ColumnCount = 4;
        _endpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        _endpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _endpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        _endpointsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _endpointsPanel.Controls.Add(_lblChatEndpoint, 0, 0);
        _endpointsPanel.Controls.Add(_txtChatEndpoint, 1, 0);
        _endpointsPanel.Controls.Add(_lblEmbeddingsEndpoint, 2, 0);
        _endpointsPanel.Controls.Add(_txtEmbeddingsEndpoint, 3, 0);
        _endpointsPanel.Controls.Add(_lblResponsesEndpoint, 0, 1);
        _endpointsPanel.Controls.Add(_txtResponsesEndpoint, 1, 1);
        _endpointsPanel.Controls.Add(_lblAdditionalHeaders, 2, 1);
        _endpointsPanel.Controls.Add(_txtAdditionalHeaders, 3, 1);
        _endpointsPanel.Dock = DockStyle.Fill;
        _endpointsPanel.Location = new Point(3, 19);
        _endpointsPanel.Name = "_endpointsPanel";
        _endpointsPanel.RowCount = 2;
        _endpointsPanel.RowStyles.Add(new RowStyle());
        _endpointsPanel.RowStyles.Add(new RowStyle());
        _endpointsPanel.Size = new Size(693, 69);
        _endpointsPanel.TabIndex = 0;
        // 
        // _lblChatEndpoint
        // 
        _lblChatEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblChatEndpoint.Location = new Point(3, 3);
        _lblChatEndpoint.Name = "_lblChatEndpoint";
        _lblChatEndpoint.Size = new Size(84, 23);
        _lblChatEndpoint.TabIndex = 0;
        _lblChatEndpoint.Text = "Chat API:";
        _lblChatEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtChatEndpoint
        // 
        _txtChatEndpoint.Dock = DockStyle.Fill;
        _txtChatEndpoint.Location = new Point(93, 3);
        _txtChatEndpoint.Name = "_txtChatEndpoint";
        _txtChatEndpoint.Size = new Size(240, 23);
        _txtChatEndpoint.TabIndex = 1;
        // 
        // _lblEmbeddingsEndpoint
        // 
        _lblEmbeddingsEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblEmbeddingsEndpoint.Location = new Point(339, 3);
        _lblEmbeddingsEndpoint.Name = "_lblEmbeddingsEndpoint";
        _lblEmbeddingsEndpoint.Size = new Size(104, 23);
        _lblEmbeddingsEndpoint.TabIndex = 2;
        _lblEmbeddingsEndpoint.Text = "Embed API:";
        _lblEmbeddingsEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtEmbeddingsEndpoint
        // 
        _txtEmbeddingsEndpoint.Dock = DockStyle.Fill;
        _txtEmbeddingsEndpoint.Location = new Point(449, 3);
        _txtEmbeddingsEndpoint.Name = "_txtEmbeddingsEndpoint";
        _txtEmbeddingsEndpoint.Size = new Size(241, 23);
        _txtEmbeddingsEndpoint.TabIndex = 3;
        // 
        // _lblResponsesEndpoint
        // 
        _lblResponsesEndpoint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblResponsesEndpoint.Location = new Point(3, 37);
        _lblResponsesEndpoint.Name = "_lblResponsesEndpoint";
        _lblResponsesEndpoint.Size = new Size(84, 23);
        _lblResponsesEndpoint.TabIndex = 4;
        _lblResponsesEndpoint.Text = "Resp API:";
        _lblResponsesEndpoint.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtResponsesEndpoint
        // 
        _txtResponsesEndpoint.Dock = DockStyle.Fill;
        _txtResponsesEndpoint.Location = new Point(93, 32);
        _txtResponsesEndpoint.Name = "_txtResponsesEndpoint";
        _txtResponsesEndpoint.Size = new Size(240, 23);
        _txtResponsesEndpoint.TabIndex = 5;
        // 
        // _lblAdditionalHeaders
        // 
        _lblAdditionalHeaders.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblAdditionalHeaders.Location = new Point(339, 37);
        _lblAdditionalHeaders.Name = "_lblAdditionalHeaders";
        _lblAdditionalHeaders.Size = new Size(104, 23);
        _lblAdditionalHeaders.TabIndex = 6;
        _lblAdditionalHeaders.Text = "Extra Headers:";
        _lblAdditionalHeaders.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtAdditionalHeaders
        // 
        _txtAdditionalHeaders.Dock = DockStyle.Fill;
        _txtAdditionalHeaders.Location = new Point(449, 32);
        _txtAdditionalHeaders.Multiline = true;
        _txtAdditionalHeaders.Name = "_txtAdditionalHeaders";
        _txtAdditionalHeaders.ScrollBars = ScrollBars.Vertical;
        _txtAdditionalHeaders.Size = new Size(241, 34);
        _txtAdditionalHeaders.TabIndex = 7;
        _txtAdditionalHeaders.WordWrap = false;
        // 
        // _grpCapabilities
        // 
        _grpCapabilities.Controls.Add(_capabilitiesPanel);
        _grpCapabilities.Dock = DockStyle.Top;
        _grpCapabilities.Location = new Point(3, 260);
        _grpCapabilities.Name = "_grpCapabilities";
        _grpCapabilities.Size = new Size(699, 55);
        _grpCapabilities.TabIndex = 3;
        _grpCapabilities.TabStop = false;
        _grpCapabilities.Text = "Capabilities";
        // 
        // _capabilitiesPanel
        // 
        _capabilitiesPanel.AutoSize = true;
        _capabilitiesPanel.Controls.Add(_chkSupportsChat);
        _capabilitiesPanel.Controls.Add(_chkSupportsEmbeddings);
        _capabilitiesPanel.Controls.Add(_chkSupportsResponses);
        _capabilitiesPanel.Controls.Add(_chkSupportsStreaming);
        _capabilitiesPanel.Dock = DockStyle.Fill;
        _capabilitiesPanel.Location = new Point(3, 19);
        _capabilitiesPanel.Name = "_capabilitiesPanel";
        _capabilitiesPanel.Size = new Size(693, 33);
        _capabilitiesPanel.TabIndex = 0;
        _capabilitiesPanel.WrapContents = false;
        // 
        // _chkSupportsChat
        // 
        _chkSupportsChat.AutoSize = true;
        _chkSupportsChat.Location = new Point(3, 3);
        _chkSupportsChat.Name = "_chkSupportsChat";
        _chkSupportsChat.Size = new Size(110, 21);
        _chkSupportsChat.TabIndex = 0;
        _chkSupportsChat.Text = "Supports Chat";
        // 
        // _chkSupportsEmbeddings
        // 
        _chkSupportsEmbeddings.AutoSize = true;
        _chkSupportsEmbeddings.Location = new Point(119, 3);
        _chkSupportsEmbeddings.Name = "_chkSupportsEmbeddings";
        _chkSupportsEmbeddings.Size = new Size(151, 21);
        _chkSupportsEmbeddings.TabIndex = 1;
        _chkSupportsEmbeddings.Text = "Supports Embedding";
        // 
        // _chkSupportsResponses
        // 
        _chkSupportsResponses.AutoSize = true;
        _chkSupportsResponses.Location = new Point(276, 3);
        _chkSupportsResponses.Name = "_chkSupportsResponses";
        _chkSupportsResponses.Size = new Size(147, 21);
        _chkSupportsResponses.TabIndex = 2;
        _chkSupportsResponses.Text = "Supports Responses";
        // 
        // _chkSupportsStreaming
        // 
        _chkSupportsStreaming.AutoSize = true;
        _chkSupportsStreaming.Location = new Point(429, 3);
        _chkSupportsStreaming.Name = "_chkSupportsStreaming";
        _chkSupportsStreaming.Size = new Size(143, 21);
        _chkSupportsStreaming.TabIndex = 3;
        _chkSupportsStreaming.Text = "Supports Streaming";
        // 
        // _chkShowAdvanced
        // 
        _chkShowAdvanced.AutoSize = true;
        _chkShowAdvanced.Location = new Point(3, 434);
        _chkShowAdvanced.Name = "_chkShowAdvanced";
        _chkShowAdvanced.Size = new Size(119, 21);
        _chkShowAdvanced.TabIndex = 1;
        _chkShowAdvanced.Text = "Show Advanced";
        _chkShowAdvanced.UseVisualStyleBackColor = true;
        // 
        // _grpBasic
        // 
        _grpBasic.Controls.Add(_basicPanel);
        _grpBasic.Dock = DockStyle.Top;
        _grpBasic.Location = new Point(0, 0);
        _grpBasic.Name = "_grpBasic";
        _grpBasic.Size = new Size(711, 319);
        _grpBasic.TabIndex = 0;
        _grpBasic.TabStop = false;
        _grpBasic.Text = "Basic Settings";
        // 
        // _basicPanel
        // 
        _basicPanel.ColumnCount = 2;
        _basicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        _basicPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _basicPanel.Controls.Add(_lblProviderUrl, 0, 0);
        _basicPanel.Controls.Add(_providerUrlPanel, 1, 0);
        _basicPanel.Controls.Add(_lblApiKey, 0, 1);
        _basicPanel.Controls.Add(_txtApiKey, 1, 1);
        _basicPanel.Controls.Add(_lblDisplayNameEditor, 0, 2);
        _basicPanel.Controls.Add(_txtDisplayNameEditor, 1, 2);
        _basicPanel.Controls.Add(_modelActionsPanel, 1, 3);
        _basicPanel.Controls.Add(_lblModelPreview, 0, 4);
        _basicPanel.Controls.Add(_lstModelPreview, 1, 4);
        _basicPanel.Controls.Add(_txtModelMetadata, 1, 5);
        _basicPanel.Controls.Add(_lblModelMetadata, 0, 5);
        _basicPanel.Dock = DockStyle.Fill;
        _basicPanel.Location = new Point(3, 19);
        _basicPanel.Name = "_basicPanel";
        _basicPanel.RowCount = 6;
        _basicPanel.RowStyles.Add(new RowStyle());
        _basicPanel.RowStyles.Add(new RowStyle());
        _basicPanel.RowStyles.Add(new RowStyle());
        _basicPanel.RowStyles.Add(new RowStyle());
        _basicPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        _basicPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _basicPanel.Size = new Size(705, 297);
        _basicPanel.TabIndex = 0;
        // 
        // _lblProviderUrl
        // 
        _lblProviderUrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblProviderUrl.Location = new Point(3, 6);
        _lblProviderUrl.Name = "_lblProviderUrl";
        _lblProviderUrl.Size = new Size(144, 23);
        _lblProviderUrl.TabIndex = 0;
        _lblProviderUrl.Text = "Provider Base URL:";
        _lblProviderUrl.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _providerUrlPanel
        // 
        _providerUrlPanel.ColumnCount = 2;
        _providerUrlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _providerUrlPanel.ColumnStyles.Add(new ColumnStyle());
        _providerUrlPanel.Controls.Add(_txtBaseUrlEditor, 0, 0);
        _providerUrlPanel.Controls.Add(_btnRefreshModels, 1, 0);
        _providerUrlPanel.Dock = DockStyle.Fill;
        _providerUrlPanel.Location = new Point(153, 0);
        _providerUrlPanel.Margin = new Padding(3, 0, 3, 3);
        _providerUrlPanel.Name = "_providerUrlPanel";
        _providerUrlPanel.RowCount = 1;
        _providerUrlPanel.RowStyles.Add(new RowStyle());
        _providerUrlPanel.Size = new Size(549, 33);
        _providerUrlPanel.TabIndex = 1;
        // 
        // _txtBaseUrlEditor
        // 
        _txtBaseUrlEditor.Dock = DockStyle.Fill;
        _txtBaseUrlEditor.Location = new Point(3, 5);
        _txtBaseUrlEditor.Margin = new Padding(3, 5, 3, 5);
        _txtBaseUrlEditor.Name = "_txtBaseUrlEditor";
        _txtBaseUrlEditor.PlaceholderText = "https://api.openai.com/v1/";
        _txtBaseUrlEditor.Size = new Size(427, 23);
        _txtBaseUrlEditor.TabIndex = 0;
        // 
        // _btnRefreshModels
        // 
        _btnRefreshModels.AutoSize = true;
        _btnRefreshModels.Location = new Point(436, 3);
        _btnRefreshModels.Name = "_btnRefreshModels";
        _btnRefreshModels.Size = new Size(110, 27);
        _btnRefreshModels.TabIndex = 1;
        _btnRefreshModels.Text = "Refresh Models";
        _btnRefreshModels.UseVisualStyleBackColor = true;
        // 
        // _lblApiKey
        // 
        _lblApiKey.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblApiKey.Location = new Point(3, 39);
        _lblApiKey.Name = "_lblApiKey";
        _lblApiKey.Size = new Size(144, 23);
        _lblApiKey.TabIndex = 2;
        _lblApiKey.Text = "Provider API Key:";
        _lblApiKey.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtApiKey
        // 
        _txtApiKey.Dock = DockStyle.Fill;
        _txtApiKey.Location = new Point(153, 39);
        _txtApiKey.Name = "_txtApiKey";
        _txtApiKey.PasswordChar = '●';
        _txtApiKey.PlaceholderText = "Stored in Windows Credential Manager, not in config JSON";
        _txtApiKey.Size = new Size(549, 23);
        _txtApiKey.TabIndex = 3;
        // 
        // _lblDisplayNameEditor
        // 
        _lblDisplayNameEditor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _lblDisplayNameEditor.Location = new Point(3, 68);
        _lblDisplayNameEditor.Name = "_lblDisplayNameEditor";
        _lblDisplayNameEditor.Size = new Size(144, 23);
        _lblDisplayNameEditor.TabIndex = 4;
        _lblDisplayNameEditor.Text = "Display Name:";
        _lblDisplayNameEditor.TextAlign = ContentAlignment.MiddleRight;
        // 
        // _txtDisplayNameEditor
        // 
        _txtDisplayNameEditor.Dock = DockStyle.Fill;
        _txtDisplayNameEditor.Location = new Point(153, 68);
        _txtDisplayNameEditor.Name = "_txtDisplayNameEditor";
        _txtDisplayNameEditor.PlaceholderText = "Third-party provider display name";
        _txtDisplayNameEditor.Size = new Size(549, 23);
        _txtDisplayNameEditor.TabIndex = 5;
        // 
        // _modelActionsPanel
        // 
        _modelActionsPanel.AutoSize = true;
        _modelActionsPanel.Controls.Add(_btnRefreshModelsApply);
        _modelActionsPanel.Controls.Add(_btnCopyModelMetadata);
        _modelActionsPanel.Controls.Add(_btnDetectProvider);
        _modelActionsPanel.Dock = DockStyle.Fill;
        _modelActionsPanel.Location = new Point(153, 97);
        _modelActionsPanel.Name = "_modelActionsPanel";
        _modelActionsPanel.Size = new Size(549, 33);
        _modelActionsPanel.TabIndex = 6;
        _modelActionsPanel.WrapContents = false;
        // 
        // _btnRefreshModelsApply
        // 
        _btnRefreshModelsApply.AutoSize = true;
        _btnRefreshModelsApply.Location = new Point(0, 0);
        _btnRefreshModelsApply.Margin = new Padding(0);
        _btnRefreshModelsApply.Name = "_btnRefreshModelsApply";
        _btnRefreshModelsApply.Size = new Size(149, 27);
        _btnRefreshModelsApply.TabIndex = 0;
        _btnRefreshModelsApply.Text = "Refresh + Apply";
        _btnRefreshModelsApply.UseVisualStyleBackColor = true;
        // 
        // _btnCopyModelMetadata
        // 
        _btnCopyModelMetadata.AutoSize = true;
        _btnCopyModelMetadata.Location = new Point(152, 3);
        _btnCopyModelMetadata.Name = "_btnCopyModelMetadata";
        _btnCopyModelMetadata.Size = new Size(134, 27);
        _btnCopyModelMetadata.TabIndex = 14;
        _btnCopyModelMetadata.Text = "Copy Raw Summary";
        _btnCopyModelMetadata.UseVisualStyleBackColor = true;
        // 
        // _btnDetectProvider
        // 
        _btnDetectProvider.AutoSize = true;
        _btnDetectProvider.Location = new Point(292, 3);
        _btnDetectProvider.Name = "_btnDetectProvider";
        _btnDetectProvider.Size = new Size(143, 27);
        _btnDetectProvider.TabIndex = 8;
        _btnDetectProvider.Text = "Test / Detect Defaults";
        _btnDetectProvider.UseVisualStyleBackColor = true;
        // 
        // _lblModelPreview
        // 
        _lblModelPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblModelPreview.Location = new Point(3, 133);
        _lblModelPreview.Name = "_lblModelPreview";
        _lblModelPreview.Size = new Size(144, 23);
        _lblModelPreview.TabIndex = 7;
        _lblModelPreview.Text = "Detected Models:";
        _lblModelPreview.TextAlign = ContentAlignment.TopRight;
        // 
        // _lstModelPreview
        // 
        _lstModelPreview.Dock = DockStyle.Fill;
        _lstModelPreview.FormattingEnabled = true;
        _lstModelPreview.IntegralHeight = false;
        _lstModelPreview.Location = new Point(153, 136);
        _lstModelPreview.Name = "_lstModelPreview";
        _lstModelPreview.Size = new Size(549, 74);
        _lstModelPreview.TabIndex = 8;
        // 
        // _txtModelMetadata
        // 
        _txtModelMetadata.Dock = DockStyle.Fill;
        _txtModelMetadata.Location = new Point(153, 216);
        _txtModelMetadata.Multiline = true;
        _txtModelMetadata.Name = "_txtModelMetadata";
        _txtModelMetadata.ReadOnly = true;
        _txtModelMetadata.ScrollBars = ScrollBars.Vertical;
        _txtModelMetadata.Size = new Size(549, 78);
        _txtModelMetadata.TabIndex = 10;
        // 
        // _lblModelMetadata
        // 
        _lblModelMetadata.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _lblModelMetadata.Location = new Point(3, 213);
        _lblModelMetadata.Name = "_lblModelMetadata";
        _lblModelMetadata.Size = new Size(144, 43);
        _lblModelMetadata.TabIndex = 9;
        _lblModelMetadata.Text = "Model Info:";
        _lblModelMetadata.TextAlign = ContentAlignment.TopRight;
        // 
        // GatewayProviderSettingsControl
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(_rootPanel);
        Name = "GatewayProviderSettingsControl";
        Size = new Size(719, 683);
        _rootPanel.ResumeLayout(false);
        _scrollPanel.ResumeLayout(false);
        _scrollPanel.PerformLayout();
        _grpAdvanced.ResumeLayout(false);
        _advancedLayout.ResumeLayout(false);
        _providerDetailsPanel.ResumeLayout(false);
        _providerDetailsPanel.PerformLayout();
        _grpAuth.ResumeLayout(false);
        _authPanel.ResumeLayout(false);
        _authPanel.PerformLayout();
        _grpEndpoints.ResumeLayout(false);
        _endpointsPanel.ResumeLayout(false);
        _endpointsPanel.PerformLayout();
        _grpCapabilities.ResumeLayout(false);
        _grpCapabilities.PerformLayout();
        _capabilitiesPanel.ResumeLayout(false);
        _capabilitiesPanel.PerformLayout();
        _grpBasic.ResumeLayout(false);
        _basicPanel.ResumeLayout(false);
        _basicPanel.PerformLayout();
        _providerUrlPanel.ResumeLayout(false);
        _providerUrlPanel.PerformLayout();
        _modelActionsPanel.ResumeLayout(false);
        _modelActionsPanel.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _rootPanel;
	private Panel _scrollPanel;
	private GroupBox _grpAdvanced;
	private TableLayoutPanel _advancedLayout;
	private TableLayoutPanel _providerDetailsPanel;
	private Label _lblProviderId;
	private TextBox _txtProviderId;
	private Label _lblProtocol;
	private ComboBox _cmbProtocol;
	private Label _lblDisplayName;
	private TextBox _txtDisplayName;
	private Label _lblBaseUrl;
	private TextBox _txtBaseUrl;
	private Label _lblDefaultModel;
	private ComboBox _cmbDefaultModel;
	private Label _lblDefaultEmbeddingModel;
	private ComboBox _cmbDefaultEmbeddingModel;
	private GroupBox _grpAuth;
	private TableLayoutPanel _authPanel;
	private Label _lblAuthType;
	private ComboBox _cmbAuthType;
	private Label _lblAuthHeader;
	private TextBox _txtAuthHeader;
	private GroupBox _grpEndpoints;
	private TableLayoutPanel _endpointsPanel;
	private Label _lblChatEndpoint;
	private TextBox _txtChatEndpoint;
	private Label _lblEmbeddingsEndpoint;
	private TextBox _txtEmbeddingsEndpoint;
	private Label _lblResponsesEndpoint;
	private TextBox _txtResponsesEndpoint;
	private Label _lblAdditionalHeaders;
	private TextBox _txtAdditionalHeaders;
	private GroupBox _grpCapabilities;
	private FlowLayoutPanel _capabilitiesPanel;
	private CheckBox _chkSupportsChat;
	private CheckBox _chkSupportsEmbeddings;
	private CheckBox _chkSupportsResponses;
	private CheckBox _chkSupportsStreaming;
	private CheckBox _chkShowAdvanced;
	private GroupBox _grpBasic;
	private TableLayoutPanel _basicPanel;
	private Label _lblProviderUrl;
	private TableLayoutPanel _providerUrlPanel;
	private TextBox _txtBaseUrlEditor;
	private Button _btnRefreshModels;
	private Label _lblApiKey;
	private TextBox _txtApiKey;
	private Label _lblDisplayNameEditor;
	private TextBox _txtDisplayNameEditor;
	private Button _btnDetectProvider;
	private FlowLayoutPanel _modelActionsPanel;
	private Button _btnRefreshModelsApply;
	private Label _lblModelPreview;
	private ListBox _lstModelPreview;
	private Button _btnCopyModelMetadata;
    private TextBox _txtModelMetadata;
    private Label _lblModelMetadata;
}
