# Changelog

## [Unreleased]

### 新增
- 引入新的 `ollamaGateway` 配置模型，作为统一 Ollama Gateway 的基础结构。
- 新增多 provider / 多 route 配置能力，为后续接入原生 Ollama、OpenAI Compatible、Anthropic 及更多服务商协议打基础。
- 新增 provider capability 配置模型，用于描述 chat / embeddings / responses / streaming 支持能力。

### 改进
- 运行时启动逻辑已优先识别 `ollamaGateway`，并在当前阶段兼容回落到旧的 `localApiForwarder` 结构。
- 当前 UI 仍使用现有 `Local API` 单 provider 编辑方式，但保存配置时会同步生成新的 `ollamaGateway` 结构。
- 配置加载时会自动将旧版 `localApiForwarder` 迁移映射为新的 Gateway 结构。
- `LocalApiForwarder` 已建立多 provider 运行时上下文与基础模型路由能力，可根据本地模型名解析目标 provider。
- 模型目录构建逻辑已开始基于 provider / route 生成，为后续单端口多上游路由做准备。
- OpenAI / Ollama 关键请求链路已开始按模型解析目标 provider，并使用对应 provider 的 endpoint、认证与 base URL 发起上游请求。
- 诊断信息中的 provider 元数据已开始随模型路由动态切换。
- 模型详情、OpenAI 模型列表、Foundry 本地模型目录及上游 catalog 拉取流程，已进一步减少默认 provider 假设并开始按 provider 上下文工作。
- 已为 Anthropic 流式适配建立基础骨架：Anthropic Messages 流式响应现在可以进入本地 OpenAI / Responses / Ollama 流式转换通路。
- 当前 Anthropic 流式基础优先支持文本增量与结束事件，复杂工具调用流式仍需后续继续补齐。
- Anthropic 流式工具调用已开始支持基础累积：可识别 `tool_use` 与 `input_json_delta`，并向 OpenAI / Responses / Ollama 流式输出映射基础的函数调用结构。
- 当前 Anthropic 工具流仍属于基础对齐阶段，后续还需继续完善更细粒度事件、usage 与结束状态一致性。
- 开始进行 Gateway UI 第一阶段改造，并明确保持 WinForms 设计器可见与可加载。
- 当前 `Local API` 页面已开始过渡为 `Ollama Gateway` 语义，并新增只读的 Gateway 路由预览区域，作为后续 provider / route 编辑界面的过渡步骤。
- UI 第二阶段已增加设计器友好的基础路由编辑区：新增 provider 视图下拉与对应 route 文本编辑框，用于向真正的 Gateway route 编辑体验过渡。
- 当前 Gateway route 编辑区已支持在切换 provider 与保存配置时回写 `ollamaGateway.routes`，基础编辑闭环已打通。
- 新增设计器可见的基础 Gateway provider 编辑区，并已支持在保存配置时将 provider 详情回写到 `ollamaGateway.providers`。
- Gateway provider 编辑区现已支持基础新增/删除，并补充高级字段编辑：鉴权、端点、附加头与能力开关均可保存到 `ollamaGateway.providers`。
- 新增 provider protocol 模板联动：切换 `OpenAICompatible / Anthropic` 时，默认鉴权、端点、baseUrl 与能力开关会自动同步更新；embeddings 关闭时相关输入框自动禁用。
- Gateway route 编辑区新增实时校验与交互反馈：无效行会高亮提示，重复 `localModel` 会提示“last one wins”，并阻止错误 route 写回配置。
- `Ollama Gateway` 页面已重构为更直观的内部多选项卡布局：`Overview / Providers / Routes / Diagnostics`，改善可发现性与可操作性。
- Providers 基础区已进一步简化，并新增 `Test / Detect Defaults`：可基于提供者地址自动推测协议并回填默认值，减少手工填写高级参数的需求。
- Providers 基础区继续精简：协议改为只读探测结果显示，显示名为可选；`Test / Detect Defaults` 现在会结合 API Key 进行探测，提高协议和默认模板识别的准确度。
- Provider 基础区已支持按 provider 单独保存与载入 API Key；`Test / Detect Defaults` 现在会在探测前先保存当前 Key，并在切换 provider 时自动恢复对应凭据。
- `Test / Detect Defaults` 探测成功后，会继续尝试读取上游模型列表，并提示用户一键填入默认 `Chat Model` / `Embedding Model`。
- Providers 高级区已进一步按 `Auth / Endpoints / Capabilities` 分组，减少单块高级配置的认知负担。
- Providers 中的 `Chat Model / Embedding Model` 已改为可编辑下拉框，优先从当前 provider 的已缓存模型列表中选择。
- 模型探测结果现在会缓存到对应 provider 的配置中，切换 provider 后仍可复用该模型列表。
- Anthropic 模型探测增加了专门兼容逻辑：补充所需请求头，并兼容非 OpenAI 风格的模型列表返回结构。
- Providers 基础区新增只读模型预览，并增加独立 `Refresh Models` 按钮，支持在不重新执行默认值探测时单独刷新模型目录。
- Anthropic provider 的模型缓存与预览现已区分 `messages` 可用模型和其他模型，便于默认 Chat Model 选择。
- 模型预览现在会高亮当前默认 Chat / Embedding 模型，便于快速核对当前 provider 配置。
- Anthropic 刷新模型后会优先从 `messages` 模型集合中自动选择默认 `Chat Model`。
- Anthropic 的非 `messages` 模型现已进一步细分为 `embeddings / moderation / unknown` 三类并显示在预览中。
- 模型预览区现支持双击模型名直接回填默认值：messages/普通模型回填 Chat，embeddings 类模型回填 Embedding。
- `Refresh Models` 已拆分为两个显式入口：`Refresh Only` 仅刷新缓存，`Refresh + Apply` 在刷新后自动应用推荐默认值。
- 模型预览区新增右键菜单：可将当前选中模型显式设为 `Chat Model` 或 `Embedding Model`。
- `Refresh + Apply` 完成后会显示结果提示，反馈刷新到的模型数量以及最终应用的默认 Chat / Embedding 模型。
- 模型预览区现支持单击显示底部元信息，包括 provider、protocol、模型分类及默认角色。
- `Refresh + Apply` 的结果现在除弹窗外，还会同步写入运行日志区，便于排查和回看。
- 元信息区新增原始服务端字段摘要展示，便于快速查看模型条目的关键返回内容。
- 选中模型时，状态栏会显示简短提示，例如当前模型名及其分类。
- 状态栏中的临时模型提示现会在几秒后自动恢复为正常运行状态文本。
- 元信息区新增 `Copy Raw Summary` 按钮，可一键复制当前模型的原始字段摘要。
- `Providers` 页面现改为每个 provider 一个页签，编辑 provider 名称时页签标题会实时刷新。
- `Overview` 中的 provider 列表现会显示 provider 协议、启用状态，以及默认 chat / embedding 模型摘要。
- `Overview` 中的 provider 列表现支持双击后直接跳转到对应的 provider 页签。
- `Providers` 页签区右侧的删除按钮已改为更明确的 `Delete` 文案，表示删除当前正在查看的 provider。
- `Overview` 中的 provider 列表现支持单击同步选中 provider，双击则直接切换到 `Providers` 页签并定位到对应 provider。
- `Providers` 页签工具区新增 `Duplicate` 按钮，可快速复制当前 provider 的配置、缓存模型和关联 routes。
- `Routes` 页面已调整为“上方编辑、下方预览”的固定布局，避免路由编辑区被只读预览覆盖。
- `Providers` 页签工具区的 `+ / Duplicate / Delete` 按钮区域已扩宽，避免按钮在常规窗口宽度下被裁切。
- Providers 基础区与高级区高度已重新整理，模型元信息与 `Copy Raw Summary` 按钮恢复可见可用。
- `Overview` 的 provider 列表补充手型光标与只读窗口样式，降低“看得见但点不动”的误解。
- README 的快速开始已更新为当前 `Ollama Gateway -> Overview / Providers / Routes / Diagnostics` 实际操作流程。
- Providers 基础区的 `Base URL` 现在会实时同步到当前 provider 配置，避免高级区或切换后回退为旧地址。
- Providers 管理已调整为固定内置三家：`OpenAI`、`Anthropic`、`Gemini`，减少新增/复制/删除导致的切换复杂度。
- Providers 页签现保留禁用项并显示 `(disabled)`，避免因过滤隐藏而造成“切不过去/像是丢了”的错觉。
- Providers 工具按钮现改为 `Preset / Enable / Disable` 语义，匹配当前固定 provider 模式。
- Gateway 配置模型已进一步收敛为显式三家 provider 对象；每个 provider 自带自己的模型路由 `routes`，不再依赖顶层共享 `providers / routes` 运行时结构。

### 兼容性
- 保留 `localApiForwarder` 字段，避免旧配置立即失效。
- 当前运行时仍通过兼容层桥接旧的单 provider forwarder 实现，后续将继续演进为真正的多 provider Gateway。

## [1.1.0.0] - 2026-03-18

### 概述
1.1.0.0 是一个功能汇总版本，正式整合并发布自 1.0.5 以来新增的全部特性，并对文档与关于页面进行了全面更新。

### 功能亮点

- **域名路由规则**：支持按域名（精确匹配或通配符）决定流量是否走代理，内置 GitHub 相关域名默认规则；不匹配的连接可直接透传。
- **DNS Hosts 重定向双模式**：
  - `DNS Interception`（默认）：数据包级 DNS 拦截，无需管理员权限，无文件修改。
  - `System Hosts File`：将 hosts 条目写入 `C:\Windows\System32\drivers\etc\hosts`，并通过 `wsl.exe` 同步写入所有已安装 WSL 发行版的 `/etc/hosts`；停止代理时自动清理；写入前自动创建备份（`%AppData%\TrafficPilot\HostsBackups\`，每目标最多保留 10 份）。
- **DoH 自动筛选**：通过多个 DNS-over-HTTPS 服务自动解析域名 IP，测量直连与代理延迟，自动选出最优 IP 写入 hosts。延迟列表支持颜色标注（绿/橙/红）。
- **自动定时刷新**（Auto Fetch）：可按分钟间隔自动循环执行 IP 解析与 hosts 更新。
- **本机代理模式**（Local Proxy）：勾选后自动将代理地址替换为本机当前 IP；监听网络变化事件，WiFi 重连或切换网卡后实时刷新，代理地址下拉框同步更新候选列表。`proxy.isLocalProxy` 持久化至配置文件。
- **配置云同步**：通过 GitHub Gist 在多台设备间同步配置文件（`configSync` 配置字段）。
- **代理地址下拉框**：`Proxy Host` 由文本框改为下拉框，自动填充本机所有有效网络接口 IP（排除回环及隧道接口）。
- **快捷配置切换**：最近使用的配置文件以快捷按钮形式呈现，同步显示在托盘菜单。
- **开机启动 & 自动启动代理**：支持注册用户级启动项，及程序加载配置后自动启动代理。

### 文档更新
- `README.md` 全面重写：补充域名路由、DoH 筛选、自动刷新、本机代理、配置云同步等所有新功能的说明，更新配置示例与配置项表格，修正默认代理协议（`socks5`），更新技术架构组件表；新增 **WSL Hosts 文件同步** 专节，说明 System Hosts File 模式的同步机制、权限要求、备份策略及自动清理。
- `About` 页描述重写：使用中文全面列出当前版本所有功能特性，包含 WSL hosts 同步说明。

### 破坏性变更（继承自 1.0.6）
- 配置字段 `targeting.extraPids`（额外进程 PID 列表）已移除，由 `targeting.domainRules`（域名路由规则列表）取代。旧配置文件中的 `extraPids` 字段加载后将被忽略。
- 命令行参数 `--pid` 已移除。

---

## [1.0.11.0] - 2026-03-18

### 新增
- 代理地址前新增 **Local Proxy** 复选框：
  - 勾选时为**本机代理**模式，自动将代理服务器地址更新为本机当前 IP 地址。
  - 不勾选时为**远程代理**模式，手动填写远程代理地址。
- 本机代理模式下实时监测网络变化（`NetworkChange.NetworkAddressChanged`）：WiFi 重连或切换网卡后，自动刷新代理 Host 为最新本机 IP，并更新下拉框候选列表。
- `IsLocalProxy` 配置项已持久化至 `config.json`（`proxy.isLocalProxy`），重启后保持上次选择状态。

---

## [1.0.10.0] - 2026-03-10

### 修复
- 修复版本号比对逻辑：版本比对从 3 位（Major.Minor.Build）改为 4 位（Major.Minor.Build.Revision），以支持完整的语义化版本号。

### 改进
- `AutoUpdater.NormalizeVersion` 和 `MainForm.GetCurrentVersion` 现在使用 4 位版本号进行比对，避免版本号截断导致的更新检测错误。

---

## [1.0.9.1] - 2026-03-10

### 新增
- 新增 `LocalNetworkHelper.GetLocalIpsWithGateway()` 工具方法：自动获取本机所有有效网络接口的 IPv4 地址（需具备默认网关）。
- 代理服务器地址输入框（`Proxy Host`）改为**下拉框**（`ComboBox`），自动填充本机 IP 地址，方便快速选择。

### 改进
- UI 初始化时自动加载本机 IP 地址列表至代理地址下拉框，提升配置便捷性。
- 若下拉框中有可用 IP 且当前值为空，自动选择第一个 IP 作为默认值。
- 排除回环接口（Loopback）和隧道接口（Tunnel），只列出实际网络连接的 IP。

---

## [1.0.9.0] - 2026-03-10

### 修复
- 修复 Gitee 流水线（`.workflow/tag-release-pipeline.yml`）版本号提取逻辑：从 `git describe` 方式改为直接使用 `${GITEE_BRANCH#v}`，避免非 tag 提交构建失败。

---

## [1.0.8] - 2026-03-10

### 新增
- 新增 GitHub Actions 自动化发布流程：Release Notes 自动从 `CHANGELOG.md` 读取对应版本内容，无需手动编写。

### 改进
- GitHub Actions `build-release.yml` 工作流优化：
  - 新增 `Create Release Notes` 步骤，自动解析 `CHANGELOG.md` 按版本号提取变更内容。
  - 若对应版本不存在于 CHANGELOG 中，自动回退至默认文案，并输出警告而不中断构建。
  - Release Notes 格式统一：变更内容 + 安装说明 + 系统需求。

---

## [1.0.7] - 2026-03-10

### 修复
- 修复单文件发布（Single-file publish）时 `StartupManager.Enable()` 方法无法正确获取程序路径的问题：
  - 将 `Assembly.GetEntryAssembly()!.Location` 改为 `Path.Combine(AppContext.BaseDirectory, $"{AppName}.exe")`，避免单文件发布时 `Location` 为空字符串导致开机启动注册失败。

### 新增
- 新增 Gitee 流水线模板（`.workflow/tag-release-pipeline.yml`），支持 Gitee 自动化构建与发布。

---

## [1.0.6] - 2026-03-10

### 新增
- 新增**域名路由规则**（`domainRules`）：支持按域名（精确匹配或通配符）决定流量是否走代理，内置 GitHub 相关域名的默认规则。
- 新增 `DomainRuleMatcher` 类，支持精确域名与通配符模式（`*`/`?`）匹配。
- 新增 `--domain` / `--domain-list` 命令行参数，支持运行时指定域名规则。
- 新增配置字段 `domainRules`（替换旧的 `extraPids`），可在配置文件中持久化域名路由规则。
- 新增 `HostsRedirectSettings.refreshDomains` 配置字段，用于指定需要自动刷新解析的域名列表。
- 新增 **DoH（DNS-over-HTTPS）自动筛选**功能：自动通过多个 DoH 服务解析域名 IP，并测量直连与代理延迟，自动选出最优 IP 写入 hosts。
- 新增 IP 延迟测试支持代理协议（SOCKS4 / SOCKS5 / HTTP CONNECT），可同时展示直连延迟与代理延迟。
- 新增 **自动定时刷新**功能（`Auto Fetch`），可按分钟间隔自动循环执行 IP 解析与 hosts 更新。
- `TcpRelayServer` 新增 `OnLog` 事件，支持将中继日志回传主界面。
- 新增 `TargetRuleNormalizer` 工具类，统一进程名与域名规则的规范化逻辑。
- Gitee CI/CD 新增流水线模板（`.workflow/branch-pipeline.yml`、`master-pipeline.yml`、`pr-pipeline.yml`、`tag-release-pipeline.yml`）。

### 改进
- `TcpRelayServer` 支持**按域名规则决定直连或代理**：不匹配域名规则的连接直接透传，不再强制走代理。
- IP 结果列表（`ListView`）显示优化：新增"代理延迟"与"DoH 来源"列，延迟按阈值显示绿/橙/红色。
- `ProcessAllowListMatcher` 移除对 PID 集合的直接维护，简化匹配逻辑，规范化处理委托给 `TargetRuleNormalizer`。
- 默认配置（`DefaultConfig`）改为引用 `ProxyOptions.DefaultProcessNames` 与 `ProxyOptions.DefaultDomainRules`，避免重复维护。
- `ProxyOptions` 默认域名规则内置了完整的 GitHub 相关域名列表（含 CDN、API、用户内容等子域名）。
- 解析与延迟测试流程拆分更清晰，错误处理更细致（超时与取消分别处理）。

### 修复
- 修复单文件发布（Single-file publish）时 `StartupManager` 使用 `Assembly.Location` 导致路径为空的问题，改用 `AppContext.BaseDirectory`。
- 修复 hosts 刷新完成后未更新最终状态文本（`Done — Resolved / Failed / Total`）的问题。
- 修复多处 `catch { }` 吞掉异常而不做任何处理的问题，改为规范的空 `catch` 块格式。

### 破坏性变更
- 配置字段 `extraPids`（额外进程 PID 列表）已移除，由 `domainRules`（域名路由规则列表）取代。旧配置文件中的 `extraPids` 字段加载后将被忽略。
- 命令行参数 `--pid` 已移除。

## [1.0.5] - 2026-03-09

### 新增
- 新增在线更新能力，可从 GitHub / Gitee 查询最新版本并下载更新包。
- 新增 `CHANGELOG.md`，用于记录版本变更。
- 新增配置名称 `configName` 字段，用于在界面和托盘中显示更友好的配置名称。
- 新增最近配置快捷按钮，并在托盘菜单中提供快速切换入口。
- 新增开机启动选项，支持注册当前用户的 Windows 启动项。
- 新增程序启动后自动启动代理选项。

### 改进
- 优化配置区操作流程，支持 `Load Config`、`Save As`、`Save Config` 的固定布局与更清晰的配置切换体验。
- 更新说明文档，使配置示例、默认路径、功能说明与当前实现保持一致。
- 补充 `About` 页版本信息展示与更新状态提示。

### 修复
- 修正文档中与默认配置、WinDivert 释放位置、配置结构不一致的说明。
