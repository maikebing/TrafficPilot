# Changelog

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
