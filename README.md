

# TrafficPilot - 网络流量重定向工具

## 项目简介

由人类指导AI编写的TrafficPilot 是一款 Windows 平台的网络流量代理转发工具，能够将指定进程的网络流量重定向到代理服务器以及DNS覆写。该工具提供图形化配置界面、系统托盘驻留和实时日志统计功能，特别适合为无法直接设置代理的软件转发网络请求。在多网络环境下，该工具可以让不同的软件使用不同的网络出口，非常实用。

## 主要功能

- **进程流量重定向**：支持按进程名（包含通配符匹配）和 PID 进行筛选，并将相关的网络请求通过您指定的代理服务器来实现访问目标网络。 
- **DNS地址重定向**：自动下载您指定的 hosts 列表，例如: [GitHub520](https://github.com/521xueweihan/GitHub520) ,通过 DNS 拦截将域名解析结果替换为对应 IP，使 相关网站可直接访问（无需代理）
- **图形化配置**：直观的配置界面，支持 JSON 格式配置文件的保存与加载
- **流量处理实时统计**：显示接收数据包、转发成功/失败等统计信息



## 运行环境

- Windows 10 或更高版本
- .NET 10（`net10.0-windows`）
- **需要管理员权限运行**（依赖 WinDivert 驱动程序进行网络流量拦截）

## 快速开始

1. 以管理员身份运行程序
2. 在 `Configuration`（配置）选项卡中配置代理主机、端口和协议类型
3. 添加需要代理的进程名（如 `devenv.exe`）或 PID
4. 点击 `Save Config` 保存配置
5. 点击 `Start Proxy` 启动代理服务

**使用 GitHub520 Hosts 重定向（可选）：**

1. 切换到 `Hosts Redirect`（Hosts 重定向）选项卡
2. 勾选 `Enable DNS  Hosts Redirect`
3. 点击 `Refresh Hosts Now` 下载最新的 hosts 列表（约数千条 GitHub 相关域名）
4. 保存配置后启动代理，程序将自动拦截 DNS 响应并将 GitHub 域名解析到对应 IP

## 配置文件

配置文件默认路径：`%AppData%\TrafficPilot\config.json`

程序启动时会自动加载默认配置，默认配置为 Visual Studio 2026 相关进程，代理服务器地址为 `host.docker.internal:7890`，协议为 SOCKS5。

**配置示例：**

```json
{
  "proxy": {
    "host": "host.docker.internal",
    "port": 7890,
    "scheme": "socks5"
  },
  "targeting": {
    "processNames": [
      "devenv.exe",
      "msbuild*.exe",
      "vstest*.exe"
    ],
    "extraPids": [1234, 5678]
  },
  "hostsRedirect": {
    "enabled": true,
    "hostsUrl": "https://raw.githubusercontent.com/521xueweihan/GitHub520/refs/heads/main/hosts"
  }
}
```

**配置项说明：**

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `proxy.host` | string | 代理服务器地址 |
| `proxy.port` | uint | 代理服务器端口 |
| `proxy.scheme` | string | 代理协议（socks4/socks5/http） |
| `targeting.processNames` | string[] | 要代理的进程名，支持通配符（*） |
| `targeting.extraPids` | int[] | 额外要代理的进程 PID |
| `hostsRedirect.enabled` | bool | 是否启用 DNS Hosts 重定向 |
| `hostsRedirect.hostsUrl` | string | Hosts 列表下载地址（默认为 GitHub520 官方地址） |

## 技术架构

### 核心组件

| 文件 | 说明 |
|------|------|
| `Program.cs` | 应用程序入口点，负责初始化和 WinDivert 驱动加载 |
| `MainForm.cs` | 主窗口界面，包含所有 UI 逻辑和事件处理 |
| `ProxyEngine.cs` | 代理引擎核心，负责数据包拦截、处理和转发 |
| `ProxyConfig.cs` | 配置模型定义和配置文件读写管理 |
| `TcpRelayServer.cs` | TCP 中继服务器，处理代理协议握手（SOCKS4/SOCKS5/HTTP CONNECT） |
| `NetworkClasses.cs` | WinDivert 相关网络处理，包括数据包解析、NAT 表管理、TCP 连接 PID 解析 |
| `UtilityClasses.cs` | 工具类，包含进程白名单匹配、本地流量绕过、统计信息等 |
| `HostsRedirectManager.cs` | GitHub520 Hosts 提供者及 DNS 拦截器，通过修改 DNS 响应实现域名级流量重定向 |
| `LogBuffer.cs` | 日志缓冲处理，支持批量写入提高性能 |

### 工作原理

1. **流量拦截**：通过 WinDivert 驱动程序拦截指定进程的外出 TCP 连接
2. **目标重定向**：将目标地址重定向到本地中继端口
3. **协议握手**：中继服务器与代理服务器完成 SOCKS4/SOCKS5 或 HTTP CONNECT 握手
4. **数据转发**：在客户端和代理服务器之间透明转发数据
5. **DNS 拦截**（Hosts Redirect 功能）：通过独立的 WinDivert 句柄拦截入站 UDP 53 端口的 DNS 响应报文，解析其中的 A 记录，若域名匹配到你指定的hosts列表则将 IP 替换为对应值后重新注入，使应用程序直接连接到你指定的hosts列表提供的 IP 地址

## WinDivert 依赖

运行时需要以下文件（项目已配置为自动输出到构建目录）：

- `WinDivert.dll` - 32位动态库
- `WinDivert64.sys` - 64位驱动程序

这些文件已嵌入到项目的资源文件中，程序启动时会自动解压到运行目录。

## 构建与发布

```bash
# 构建项目
dotnet build

# 发布 Release 版本
dotnet publish -c Release
```

## 故障排除

- **启动失败**：请确认以管理员权限运行程序，且 WinDivert 相关文件存在
- **代理无效**：请检查进程名/通配符配置是否正确，以及代理服务器地址是否可达
- **配置异常**：删除 `%AppData%\TrafficPilot\config.json` 后重启程序即可恢复默认配置

## 许可证

遵循原项目许可证。

---

**注意**：本工具会拦截和重定向本机网络流量，请确保仅用于合法用途。使用前请仔细阅读相关法律法规。
