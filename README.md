

# TrafficPilot - 网络流量代理转发工具

## 项目简介

TrafficPilot 是一款 Windows 平台的网络流量代理转发工具，能够将指定进程的网络流量重定向到代理服务器。该工具提供图形化配置界面、系统托盘驻留和实时日志统计功能，特别适合为无法直接设置代理的软件转发网络请求。在多网络环境下，该工具可以让不同的软件使用不同的网络出口，非常实用。

## 主要功能

- **多协议支持**：支持 SOCKS4、SOCKS5、HTTP 三种代理协议
- **进程过滤**：支持按进程名（包含通配符匹配）和 PID 进行过滤
- **图形化配置**：直观的配置界面，支持 JSON 格式配置文件的保存与加载
- **系统托盘**：支持最小化到系统托盘，提供快捷控制菜单
- **实时统计**：显示接收数据包、转发成功/失败等统计信息
- **日志管理**：实时日志显示，支持批量日志写入和日志清理

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
| `LogBuffer.cs` | 日志缓冲处理，支持批量写入提高性能 |

### 工作原理

1. **流量拦截**：通过 WinDivert 驱动程序拦截指定进程的外出 TCP 连接
2. **目标重定向**：将目标地址重定向到本地中继端口
3. **协议握手**：中继服务器与代理服务器完成 SOCKS4/SOCKS5 或 HTTP CONNECT 握手
4. **数据转发**：在客户端和代理服务器之间透明转发数据

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

**注意**：本工具会拦截和重定向网络流量，请确保仅用于合法用途。使用前请仔细阅读相关法律法规。