
# TrafficPilot - 网络流量重定向工具

[更新日志](CHANGELOG.md)

## 项目简介

由人类指导 AI 编写的 TrafficPilot 是一款 Windows 平台的网络流量代理转发工具，能够将指定进程的网络流量重定向到代理服务器以及进行 DNS 覆写。该工具提供图形化配置界面、系统托盘驻留和实时日志统计功能，特别适合为无法直接设置代理的软件转发网络请求。在多网络环境下，该工具可以让不同的软件使用不同的网络出口，非常实用。

> 说明：本轮已开始将原有单 provider 的 `Local API` 转发能力重构为统一的 `Ollama Gateway`。当前 UI 仍沿用现有编辑方式，但配置文件已支持新的 `ollamaGateway` 结构，并兼容旧的 `localApiForwarder` 字段。

> 当前进度：运行时已建立多 provider 的基础路由上下文，模型目录与模型到上游的映射开始基于 `providers + routes` 工作；后续将继续补齐真正的单端口多上游分发与 Anthropic 流式支持。

> 本轮补充：OpenAI / Ollama 的关键请求链路已开始根据本地模型名选择目标 provider，并使用对应 provider 的认证、端点与上游地址发起请求；这为后续单端口统一路由打下基础。

> 进一步进展：模型详情、模型列表与上游模型目录拉取也已开始按 provider 上下文运行，运行时对“默认 provider”假设已进一步收敛。

> Anthropic 流式进展：已建立 Anthropic Messages SSE 到本地 OpenAI / Responses / Ollama 流式输出的基础骨架。目前优先支持文本增量流式与结束事件，工具调用的完整流式对齐将在后续继续完善。

> 工具调用流式补充：Anthropic `tool_use` / `input_json_delta` 事件已开始映射到本地 OpenAI / Responses / Ollama 的函数调用流式结构，但当前仍属于基础兼容阶段。

> UI 进展：已开始第一阶段的 Gateway 配置页改造，并优先保证 WinForms 设计器可见。当前页面已升级为 `Ollama Gateway` 语义，同时新增 Gateway 路由只读预览，后续将继续演进到完整的 provider / route 编辑体验。

> UI 第二阶段：已补充设计器友好的基础 route 编辑区，可按 provider 视图查看对应 route 文本；当前仍属于过渡形态，后续会继续完善为完整的 Gateway provider / route 编辑界面。

> UI 第二阶段补充：当前 route 编辑区在切换 provider 与保存配置时，已开始将编辑内容回写到 `ollamaGateway.routes`，从“仅查看”进入基础可编辑状态。

> UI 第三阶段起步：已新增设计器可见的基础 provider 编辑区，可按 provider 视图查看并修改 `id / protocol / name / baseUrl / defaultModel / defaultEmbeddingModel`，保存配置时会同步回写到 `ollamaGateway.providers`。

> UI 第三阶段补充：provider 编辑区现已支持基础新增/删除，并已暴露高级字段：`authType / authHeaderName / chatEndpoint / embeddingsEndpoint / responsesEndpoint / additionalHeaders / capabilities`，可直接回写到 `ollamaGateway.providers`。

> 交互增强：切换 provider `protocol` 时，UI 会自动套用对应的默认 `baseUrl / auth / endpoint / capability` 模板；同时 embeddings 能力关闭时，相关模型与端点输入框会自动禁用并清空。

> Route 编辑增强：Gateway route 文本区已增加实时格式校验、重复 `local model` 提示，以及错误高亮反馈；格式错误时不会将当前 route 文本写回配置。

> Gateway UI 调整：`Ollama Gateway` 页面现已拆分为内部多选项卡：`Overview / Providers / Routes / Diagnostics`，避免所有配置堆叠在单页中，便于理解与操作。

> Providers 进一步简化：基础区现在优先围绕 `Provider Base URL` 和 `API Key` 配置，并新增 `Test / Detect Defaults` 按钮。点击后程序会按地址尝试推测协议类型，并自动回填默认鉴权和端点模板。

> 基础区继续收敛：`Detected Protocol` 现在改为只读展示，`Display Name` 为可选项；`Test / Detect Defaults` 会优先结合已填写的 API Key 做探测，以提高默认值判断准确度。

> Provider 基础体验进一步优化：当前会按 provider 维度单独保存/读取 API Key；点击 `Test / Detect Defaults` 前会先持久化当前 Key，切换 provider 后也会自动载入对应 Key，减少重复输入。

> 本轮可用性补充：`Test / Detect Defaults` 在探测成功后，会进一步尝试读取上游模型列表，并提示是否自动填入默认 `Chat Model` / `Embedding Model`；同时高级区已拆分为 `Auth / Endpoints / Capabilities` 三组，降低理解成本。

> 本轮继续优化：`Chat Model / Embedding Model` 已改为可编辑下拉框；检测得到的模型列表会缓存到当前 provider，下次切换回来可直接选用；Anthropic 模型探测也增加了专门兼容处理，优先兼容其返回格式与请求头要求。

> 本轮补充交互：Providers 基础区新增独立 `Refresh Models` 按钮，并在 provider 下方显示只读模型预览；对于 Anthropic，预览会进一步区分 `messages` 可用模型与其他模型，便于挑选默认聊天模型。

> 本轮再优化：模型预览现在会高亮当前默认 `Chat Model / Embedding Model`；Anthropic 在刷新后会优先从 `messages` 模型中自动选择默认 `Chat Model`；同时其 `other` 模型也继续细分为 `embeddings / moderation / unknown`，更方便人工确认用途。

> 本轮交互增强：模型预览区已改为可双击选择，按分类自动回填默认模型；同时 `Refresh Models` 已拆分为 `Refresh Only` 和 `Refresh + Apply` 两种模式，分别用于仅更新缓存或刷新后自动应用推荐默认值。

> 本轮补充：模型预览区已增加右键菜单，可显式设为 `Chat Model` 或 `Embedding Model`；`Refresh + Apply` 完成后也会显示结果提示，例如已刷新模型数量和最终应用的默认模型。

> 本轮继续增强：单击模型预览项时，底部会显示该模型的只读元信息（provider、protocol、分类、当前是否为默认模型）；同时 `Refresh + Apply` 的结果也会同步写入日志区，便于后续回溯。

> 本轮补充：元信息区现在会追加展示原始服务端返回字段摘要；同时选中模型时，状态栏也会显示简短提示，方便快速确认当前聚焦的模型与分类。

> 本轮继续补充：状态栏中的模型提示会在几秒后自动恢复为 `Status: Running / Stopped`；元信息区也新增了 `Copy Raw Summary` 按钮，方便复制当前模型的原始字段摘要。

> Provider 交互已进一步调整：`Providers` 页面现在按“一个 provider 一个页签”组织；编辑 provider 名称时，页签标题会实时刷新。`Overview` 中的 provider 列表也会显示每个 provider 的协议、启用状态以及默认 chat / embedding 模型。

> 本轮补充：`Overview` 中的 provider 列表现在支持双击后直接跳转到对应 provider 页签；同时 `Providers` 页签区右侧的删除操作也改成更直白的 `Delete` 按钮，便于理解当前是在删除正在查看的 provider。

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
3. 配置本地监听端口（默认 Ollama `11434`、Foundry / Agent `5273`）
4. 选择上游 `Protocol`：
   - `OpenAICompatible`：适配大多数 OpenAI 风格供应商
   - `Anthropic`：将本地请求转换为 Anthropic Messages API
5. 填写 `Provider Name`、`Provider Base URL`、`Default Model`，如需 embeddings 可额外填写 `Embedding Model`
6. 按供应商要求设置 `Auth Type` 与 `Header / Query Name`
   - `Bearer`：标准 `Authorization: Bearer <token>`
   - `Header`：如 `x-api-key: <token>`
   - `Query`：如 `?key=<token>`
7. 如需给上游透传智能体上下文，可在 `Extra Headers` 中按 `Header=Value` 逐行填写
8. 在 `Model Mappings` 中按 `本地模型名=远程模型名` 逐行填写映射关系
9. 可按需开启 `Enable request/response logging`、`Include bodies` 和 `Include error diagnostics in responses`
10. 输入供应商 API Key（会保存到 Windows Credential Manager，不会写入 JSON 配置）
11. 保存配置后启动 TrafficPilot，本机其他应用即可继续访问 `http://127.0.0.1:<端口>/...`

**当前已兼容的本地接口：**

- **Ollama 兼容**
  - `POST /api/generate`
  - `POST /api/chat`
  - `POST /api/embeddings`
  - `POST /api/embed`
  - `GET /api/tags`
  - `GET /api/version`
- **Foundry / OpenAI / 智能体 兼容**
  - `POST /v1/chat/completions`
  - `POST /chat/completions`
  - `POST /v1/embeddings`
  - `POST /embeddings`
  - `POST /v1/responses`
  - `POST /responses`
  - `GET /v1/models`
  - `GET /models`

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
    "includeErrorDiagnostics": true,
    "provider": {
      "protocol": "OpenAICompatible",
      "name": "OpenAI Compatible",
      "baseUrl": "https://api.openai.com/v1/",
      "defaultModel": "gpt-4.1-mini",
      "defaultEmbeddingModel": "text-embedding-3-small",
      "authType": "Header",
      "authHeaderName": "x-api-key",
      "chatEndpoint": "chat/completions",
      "embeddingsEndpoint": "embeddings",
      "additionalHeaders": [
        {
          "name": "X-Agent-ID",
          "value": "trafficpilot-agent"
        }
      ]
    },
    "modelMappings": [
      {
        "localModel": "qwen2.5:7b",
        "upstreamModel": "gpt-4.1-mini"
      }
    ],
    "requestResponseLogging": {
      "enabled": true,
      "includeBodies": false,
      "maxBodyCharacters": 4000
    }
  }
}
```

**Anthropic 配置示例：**

```json
{
  "localApiForwarder": {
    "enabled": true,
    "ollamaPort": 11434,
    "foundryPort": 5273,
    "provider": {
      "protocol": "Anthropic",
      "name": "Anthropic",
      "baseUrl": "https://api.anthropic.com/v1/",
      "defaultModel": "claude-sonnet-4-20250514",
      "authType": "Bearer",
      "authHeaderName": "Authorization",
      "chatEndpoint": "messages",
      "additionalHeaders": [
        {
          "name": "anthropic-version",
          "value": "2023-06-01"
        }
      ]
    },
    "modelMappings": [
      {
        "localModel": "claude-local",
        "upstreamModel": "claude-sonnet-4-20250514"
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
| `localApiForwarder.includeErrorDiagnostics` | bool | 是否在错误返回中附带本地路径、上游状态、供应商信息等诊断信息 |
| `localApiForwarder.requestResponseLogging.enabled` | bool | 是否启用本地转发请求/响应日志 |
| `localApiForwarder.requestResponseLogging.includeBodies` | bool | 是否在日志中附带请求/响应正文（按长度截断） |
| `localApiForwarder.requestResponseLogging.maxBodyCharacters` | int | 请求/响应正文日志最大字符数 |
| `localApiForwarder.provider.protocol` | string | 上游协议适配器（`OpenAICompatible` / `Anthropic`） |
| `localApiForwarder.provider.name` | string | 第三方 API 供应商显示名称 |
| `localApiForwarder.provider.baseUrl` | string | 第三方 OpenAI-compatible API 基础地址（建议包含 `/v1/`） |
| `localApiForwarder.provider.defaultModel` | string | 未命中映射时使用的默认远程模型 |
| `localApiForwarder.provider.defaultEmbeddingModel` | string | embeddings 请求默认使用的远程模型 |
| `localApiForwarder.provider.authType` | string | 上游鉴权方式（`Bearer` / `Header` / `Query`） |
| `localApiForwarder.provider.authHeaderName` | string | Header 或 Query 模式下的键名（例如 `x-api-key`、`key`） |
| `localApiForwarder.provider.chatEndpoint` | string | 上游聊天接口相对路径；Anthropic 典型值为 `messages` |
| `localApiForwarder.provider.embeddingsEndpoint` | string | 上游 embeddings 接口相对路径 |
| `localApiForwarder.provider.additionalHeaders` | object[] | 要附加到上游请求的固定 Header，可用于智能体上下文或供应商版本头 |
| `localApiForwarder.modelMappings` | object[] | 本地模型别名与远程模型名的映射列表 |

### 配置管理说明

- 默认配置文件路径：`%AppData%\TrafficPilot\config.json`
- `Load Config`：从任意 JSON 文件加载配置
- `Save As`：将当前配置保存为新文件
- `Save Config`：覆盖保存当前活动配置文件
- 配置区左侧会显示最近使用的配置快捷按钮，便于快速切换
- 系统托盘菜单中也提供相同的配置切换与保存入口
- 本地 API 转发的供应商 API Key 存储在 **Windows Credential Manager**，不会写入配置文件

### 本地转发说明

- 本地监听地址固定为 `127.0.0.1` / `localhost`，不会绑定外网地址
- `/v1/responses` 主要用于兼容智能体 / agent 客户端；TrafficPilot 会尽量将其转换为上游聊天接口
- 对于 `Anthropic` 适配器，当前主要覆盖聊天与智能体相关请求；embeddings 会返回明确错误提示
- 开启 `Include request/response bodies` 后，正文会按 `maxBodyCharacters` 截断后写入日志，请注意敏感内容

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
