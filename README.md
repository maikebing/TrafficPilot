# TrafficPilot - 加速器

## 项目简介

TrafficPilot 是用于将指定进程的网络流量转发到代理服务器。提供图形化配置、系统托盘和实时日志统计，适合为无法设置配置代理的软件转发网络请求到指定的代理服务器，尤其在多网络环境中多个软件需要使用不同的网络时非常有用。

## 主要功能

- 图形化配置代理（SOCKS4/SOCKS5/HTTP）
- 进程名与 PID 过滤（支持通配符）
- JSON 配置保存/加载
- 系统托盘驻留与快捷控制
- 实时日志与统计信息

## 运行环境

- Windows 10 或更高版本
- .NET 10（`net10.0-windows`）
- 需要管理员权限运行（依赖 WinDivert 驱动）

## 快速开始

1. 以管理员身份运行程序
2. 在 `Configuration` 选项卡中配置代理主机、端口和协议
3. 添加需要代理的进程名或 PID
4. 点击 `Save Config` 保存配置
5. 点击 `Start Proxy` 启动代理

## 配置文件

默认路径：`%AppData%\TrafficPilot\config.json`

默认配置为Visual Studio 2026的相关进程，代理服务器地址为 `host.docker.internal:7890`，协议为 SOCKS5。启动后包括GitHub Copilot等皆可正常使用。

示例：

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

## 目录结构

| 文件 | 说明 |
| --- | --- |
| `Program.cs` | 应用入口与启动逻辑 |
| `MainForm.cs` | 主窗口与 UI 逻辑 |
| `ProxyEngine.cs` | 代理核心逻辑 |
| `ProxyConfig.cs` | 配置模型与读写 |
| `TcpRelayServer.cs` | TCP 中继与握手 |
| `NetworkClasses.cs` | WinDivert 相关网络处理 |
| `UtilityClasses.cs` | 工具类与辅助功能 |
| `LogBuffer.cs` | 日志缓冲处理 |

## WinDivert 依赖

运行时需包含以下文件（项目已配置为输出到构建目录）：

- `WinDivert.dll`
- `WinDivert64.sys`

## 构建与发布

```bash
dotnet build

dotnet publish -c Release
```

## 故障排除

- 启动失败：确认管理员权限与 WinDivert 文件存在
- 代理无效：检查进程名/通配符与代理地址是否正确
- 配置异常：删除 `config.json` 后重启即可恢复默认配置

## 许可证

遵循原项目许可证。
