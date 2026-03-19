
# TrafficPilot - 网络流量重定向工具

[更新日志](CHANGELOG.md)

## 项目简介

由人类指导 AI 编写的 TrafficPilot 是一款 Windows 平台的网络流量代理转发工具，能够将指定进程的网络流量重定向到代理服务器以及进行 DNS 覆写。该工具提供图形化配置界面、系统托盘驻留和实时日志统计功能，特别适合为无法直接设置代理的软件转发网络请求。在多网络环境下，该工具可以让不同的软件使用不同的网络出口，非常实用。

## 主要功能

- **进程流量重定向**：支持按进程名（含通配符匹配）进行筛选，将相关网络请求通过指定代理服务器转发到目标网络。
- **域名路由规则**：支持按域名（精确匹配或通配符）决定流量是否走代理，内置 GitHub 相关域名默认规则，不匹配的连接可直接透传。
- **DNS 地址重定向**：自动下载指定的 hosts 列表（例如 [GitHub520](https://github.com/521xueweihan/GitHub520)），通过 DNS 拦截将域名解析结果替换为对应 IP，使相关网站可直接访问（无需代理）。支持两种模式：`DNS Interception`（数据包级 DNS 拦截，无需管理员权限）和 `System Hosts File`（写入 Windows hosts 文件并同步到所有 WSL 发行版的 `/etc/hosts`，需管理员权限）。
- **DoH 自动筛选**：通过多个 DNS-over-HTTPS 服务自动解析域名 IP，并测量直连与代理延迟，自动选出最优 IP 写入 hosts。
- **自动定时刷新**（Auto Fetch）：可按分钟间隔自动循环执行 IP 解析与 hosts 更新。
- **本机代理模式**：勾选 `Local Proxy` 后，自动将代理地址更新为本机当前 IP，并在网络切换（WiFi 重连/换网卡）时实时刷新，无需手动修改代理地址。
- **图形化配置**：直观的配置界面，支持配置名称、自定义 JSON 配置文件保存 / 另存为 / 加载。
- **快捷配置切换**：自动显示最近使用的配置快捷按钮，并在托盘菜单中同步提供快速切换入口。
- **配置云同步**：支持通过 GitHub Gist 在多台设备间同步配置文件。
- **本地 LLM 接口转发**：可在本机暴露 Ollama 与 Foundry Local / OpenAI 兼容接口，并将请求转换后转发到可配置的第三方模型 API。
- **启动项控制**：支持开机启动，以及程序启动后自动启动代理。
- **在线更新**：在 `About` 页面自动查询 GitHub / Gitee 最新版本，并支持一键下载更新。
- **流量处理实时统计**：显示接收数据包、转发成功 / 失败等统计信息。



## 运行环境

- Windows 10 或更高版本
- .NET 10（`net10.0-windows`）
- **需要管理员权限运行**（依赖 WinDivert 驱动程序进行网络流量拦截）

## 快速开始

1. 以管理员身份运行程序
2. 在 `Configuration`（配置）选项卡中设置 `Config Name`、代理主机、端口和协议类型
3. 添加需要代理的进程名（如 `devenv.exe`）；如需限制仅对特定域名走代理，可在域名规则框中填写对应域名
4. 按需勾选 `Start on Windows startup` 和 `Start Proxy after launch`
5. 点击 `Save Config` 保存当前配置，或使用 `Save As` 保存为新的配置文件
6. 点击 `Start Proxy` 启动代理服务

**使用本地 LLM 接口转发（可选）：**

1. 切换到 `Local API` 选项卡
2. 勾选 `Enable local Ollama / Foundry forwarding`
3. 配置本地监听端口（默认 Ollama `11434`、Foundry `5273`）
4. 填写第三方供应商的 `Provider Name`、`Provider Base URL` 与默认远程模型
5. 在 `Model Mappings` 中按 `本地模型名=远程模型名` 逐行填写映射关系
6. 输入供应商 API Key（会保存到 Windows Credential Manager，不会写入 JSON 配置）
7. 保存配置后启动 TrafficPilot，本机其他应用即可继续访问 `http://127.0.0.1:<端口>/...`

**使用 GitHub520 Hosts 重定向（可选）：**

1. 切换到 `Hosts Redirect`（Hosts 重定向）选项卡
2. 勾选 `Enable DNS Hosts Redirect`
3. 选择重定向模式：
   - **DNS Interception**（默认）：在数据包层拦截 DNS 响应，无需修改任何文件，无需管理员权限。
   - **System Hosts File**：将 hosts 条目写入 `C:\Windows\System32\drivers\etc\hosts`，同时自动同步到所有已安装 WSL 发行版的 `/etc/hosts`。需要以管理员身份运行程序。停止代理时会自动清理写入的条目并还原备份。
4. 点击 `Refresh Hosts Now` 下载最新的 hosts 列表（约数千条 GitHub 相关域名）
5. 保存配置后启动代理

**使用本机代理模式（可选）：**

1. 在 `Configuration` 选项卡中勾选代理地址旁的 `Local Proxy` 复选框
2. 程序将自动填充并跟踪本机当前 IP 地址，切换网络后无需手动更新

## 配置文件

配置文件默认路径：`%AppData%\TrafficPilot\config.json`

程序启动时会自动加载默认配置，默认配置包含 Visual Studio 相关进程，代理服务器地址为 `host.docker.internal:7890`，默认协议为 `socks5`。

**配置示例：**

```json
{
  "configName": "Visual Studio",
  "proxy": {
    "enabled": true,
    "host": "host.docker.internal",
    "port": 7890,
    "scheme": "socks5",
    "isLocalProxy": false
  },
  "targeting": {
    "processNames": [
      "devenv.exe",
      "servicehub*.exe",
      "msbuild*.exe",
      "vstest*.exe"
    ],
    "domainRules": [
      "github.com",
      "*.github.com",
      "*.githubusercontent.com"
    ]
  },
  "hostsRedirect": {
    "enabled": true,
    "mode": "DnsInterception",
    "hostsUrl": "https://raw.githubusercontent.com/521xueweihan/GitHub520/refs/heads/main/hosts",
    "refreshDomains": ["github.com", "raw.githubusercontent.com"]
  },
  "startOnBoot": false,
  "autoStartProxy": false,
  "configSync": {
    "provider": "GitHub",
    "gistId": ""
  },
  "localApiForwarder": {
    "enabled": true,
    "ollamaPort": 11434,
    "foundryPort": 5273,
    "provider": {
      "name": "OpenAI Compatible",
      "baseUrl": "https://api.openai.com/v1/",
      "defaultModel": "gpt-4.1-mini"
    },
    "modelMappings": [
      {
        "localModel": "qwen2.5:7b",
        "upstreamModel": "gpt-4.1-mini"
      }
    ]
  }
}
```

**配置项说明：**

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `configName` | string | 配置显示名称，用于界面快捷按钮和托盘菜单显示 |
| `proxy.enabled` | bool | 是否启用 TCP 代理流量重定向 |
| `proxy.host` | string | 代理服务器地址 |
| `proxy.port` | uint | 代理服务器端口 |
| `proxy.scheme` | string | 代理协议（socks4 / socks5 / http / https） |
| `proxy.isLocalProxy` | bool | 是否为本机代理模式（自动使用本机 IP，随网络变化实时更新） |
| `targeting.processNames` | string[] | 要代理的进程名，支持通配符（*） |
| `targeting.domainRules` | string[] | 域名路由规则，支持精确域名和通配符（*/?）；不匹配的连接直接透传 |
| `hostsRedirect.enabled` | bool | 是否启用 DNS Hosts 重定向 |
| `hostsRedirect.mode` | string | 重定向模式：`DnsInterception`（数据包级 DNS 拦截，无需管理员权限）或 `HostsFile`（写入 Windows hosts 文件并同步到所有 WSL 发行版 `/etc/hosts`，需管理员权限） |
| `hostsRedirect.hostsUrl` | string | Hosts 列表下载地址（默认为 GitHub520 官方地址） |
| `hostsRedirect.refreshDomains` | string[] | 需要自动刷新解析的域名列表（用于 DoH 筛选与定时更新） |
| `startOnBoot` | bool | 是否注册为当前用户的 Windows 开机启动 |
| `autoStartProxy` | bool | 程序启动并加载配置后是否自动启动代理 |
| `configSync.provider` | string | 配置同步服务提供商（`GitHub` 或 `Gitee`） |
| `configSync.gistId` | string | 远程 Gist / Snippet ID，用于多设备配置同步 |
| `localApiForwarder.enabled` | bool | 是否启用本地 Ollama / Foundry Local 接口转发 |
| `localApiForwarder.ollamaPort` | uint | Ollama 兼容接口监听端口 |
| `localApiForwarder.foundryPort` | uint | Foundry Local / OpenAI 兼容接口监听端口 |
| `localApiForwarder.provider.name` | string | 第三方 API 供应商显示名称 |
| `localApiForwarder.provider.baseUrl` | string | 第三方 OpenAI-compatible API 基础地址（建议包含 `/v1/`） |
| `localApiForwarder.provider.defaultModel` | string | 未命中映射时使用的默认远程模型 |
| `localApiForwarder.modelMappings` | object[] | 本地模型别名与远程模型名的映射列表 |

### 配置管理说明

- 默认配置文件路径：`%AppData%\TrafficPilot\config.json`
- `Load Config`：从任意 JSON 文件加载配置
- `Save As`：将当前配置保存为新文件
- `Save Config`：覆盖保存当前活动配置文件
- 配置区左侧会显示最近使用的配置快捷按钮，便于快速切换
- 系统托盘菜单中也提供相同的配置切换与保存入口
- 本地 API 转发的供应商 API Key 存储在 **Windows Credential Manager**，不会写入配置文件

## 技术架构

### 核心组件

| 文件 | 说明 |
|------|------|
| `Program.cs` | 应用程序入口点，负责初始化和 WinDivert 驱动加载 |
| `MainForm.cs` | 主窗口界面，包含所有 UI 逻辑和事件处理 |
| `ProxyEngine.cs` | 代理引擎核心，负责数据包拦截、处理和转发 |
| `ProxyConfig.cs` | 配置模型定义和配置文件读写管理 |
| `TcpRelayServer.cs` | TCP 中继服务器，处理代理协议握手（SOCKS4/SOCKS5/HTTP CONNECT），并按域名规则决定直连或代理 |
| `NetworkClasses.cs` | WinDivert 相关网络处理，包括数据包解析、NAT 表管理、TCP 连接 PID 解析 |
| `UtilityClasses.cs` | 工具类，包含进程白名单匹配、域名规则匹配、本地流量绕过、开机启动管理、统计信息等 |
| `HostsRedirectManager.cs` | GitHub520 Hosts 提供者及 DNS 拦截器，通过修改 DNS 响应实现域名级流量重定向；支持 DoH 自动筛选最优 IP |
| `LogBuffer.cs` | 日志缓冲处理，支持批量写入提高性能 |
| `AutoUpdater.cs` | 在线更新模块，负责检查新版本、下载压缩包并完成替换更新 |
| `ConfigSyncService.cs` | 配置云同步服务，通过 GitHub Gist 在多设备间同步配置 |
| `SystemHostsFileManager.cs` | Windows hosts 文件及 WSL 发行版 hosts 文件管理器，支持写入、清理和自动备份 |

### 工作原理

1. **流量拦截**：通过 WinDivert 驱动程序拦截指定进程的外出 TCP 连接
2. **域名判断**：对拦截到的连接查询域名路由规则，不匹配则直接透传，不走代理
3. **目标重定向**：将匹配的连接目标地址重定向到本地中继端口
4. **协议握手**：中继服务器与代理服务器完成 SOCKS4/SOCKS5 或 HTTP CONNECT 握手
5. **数据转发**：在客户端和代理服务器之间透明转发数据
6. **DNS 拦截**（Hosts Redirect 功能）：通过独立的 WinDivert 句柄拦截入站 UDP 53 端口的 DNS 响应报文，解析其中的 A 记录，若域名匹配 hosts 列表则将 IP 替换为对应值后重新注入，使应用程序直接连接到 hosts 指定的 IP 地址

## WinDivert 依赖

运行时需要以下文件（项目已配置为自动输出到构建目录）：

- `WinDivert.dll` - 32 位动态库
- `WinDivert64.sys` - 64 位驱动程序

这些文件已嵌入到项目资源中，程序启动时会自动释放到 `%AppData%\TrafficPilot` 目录。

## WSL Hosts 文件同步

在 `Hosts Redirect` 选项卡中选择 **System Hosts File** 模式后，TrafficPilot 会在启动代理时自动将 hosts 条目同步写入所有已安装的 WSL 发行版（通过 `wsl.exe --list` 自动枚举，排除 `docker-desktop`）：

- **写入路径**：每个 WSL 发行版的 `/etc/hosts`
- **执行方式**：`wsl.exe --distribution <发行版名称> --user root --exec tee /etc/hosts`
- **权限要求**：程序本身需以 **管理员身份**运行（用于写入 Windows hosts 文件），WSL 内部以 `root` 用户操作
- **自动备份**：写入前会在 `%AppData%\TrafficPilot\HostsBackups\` 目录下保存备份（每个目标最多保留 10 份）
- **自动还原**：停止代理时，程序会自动从 Windows hosts 文件及各 WSL 发行版的 `/etc/hosts` 中清理由 TrafficPilot 写入的条目（以 `# ========== TrafficPilot Managed Section ==========` 标记的区块）
- **实时更新**：通过 Auto Fetch 或手动刷新触发 IP 更新时，Windows hosts 文件与 WSL hosts 文件会同步更新

> **提示**：如果不需要 WSL 同步，建议使用默认的 **DNS Interception** 模式，无需管理员权限，也不会修改任何文件。

## 系统托盘与更新

- 主窗口关闭时默认最小化到系统托盘，不会直接退出程序
- 托盘菜单支持显示/隐藏主窗口、启动/停止代理、切换启动项、加载/保存配置
- `About` 页会自动查询在线最新版本
- 当检测到新版本时，可直接下载更新包，程序会在退出后自动替换文件并重启

## 构建与发布

```bash
# 构建项目
dotnet build

# 发布 Release 版本
dotnet publish -c Release
```

项目附带 GitHub Actions 发布工作流，打标签 `v*` 后会自动生成 Windows x64 单文件发布包并压缩为 zip。

## 故障排除

- **启动失败**：请确认以管理员权限运行程序，且 WinDivert 相关文件存在
- **代理无效**：请检查进程名/通配符配置是否正确，以及代理服务器地址是否可达
- **域名规则不生效**：请确认域名规则列表已填写，且格式正确（支持精确域名与 `*`/`?` 通配符）
- **配置异常**：删除 `%AppData%\TrafficPilot\config.json` 后重启程序即可恢复默认配置
- **WSL hosts 未更新**：请确认已选择 `System Hosts File` 模式并以管理员权限运行；可在命令行执行 `wsl.exe --list --quiet` 确认 WSL 发行版已安装且可正常列出；若 WSL 内仍无效果，可手动查看对应发行版的 `/etc/hosts` 并核查 TrafficPilot 管理区块是否存在
- **开机启动失败**：请确认当前用户具备写入 `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` 的权限
- **在线更新失败**：请检查是否能够访问 GitHub 或 Gitee 发布接口，以及发布包下载链接是否可达

## 许可证

遵循原项目许可证。

---

**注意**：本工具会拦截和重定向本机网络流量，请确保仅用于合法用途。使用前请仔细阅读相关法律法规。
