# VSifier (C# + WinDivert)

将指定进程的 **出站 IPv4 TCP 流量** 强制重定向到指定代理地址。

默认代理：`host.docker.internal:7899`，协议标识：`https`。

## 功能说明

- 使用 WinDivert 拦截 `ip and tcp`（出站+入站）。
- 通过系统 TCP 连接表（`GetExtendedTcpTable`）将连接五元组映射到进程 PID。
- 仅对目标进程的数据包改写目标 IP/端口到代理。
- 维护会话映射并对入站回包做反向改写（将代理源地址还原为原始目标），保证 TCP 会话可用。
- 改写后自动重算 IP/TCP 校验和。

## 前置要求

1. Windows 系统。
2. 以管理员权限运行。
3. 准备 WinDivert 二进制文件（与程序位数一致）：
	- `WinDivert.dll`
	- `WinDivert64.sys`（或对应架构驱动）

> 建议将上述文件放在可执行程序同目录，或加入系统可搜索路径。

## 构建

```powershell
dotnet build
```

## 运行

使用默认配置直接运行（已内置常见进程白名单）：

```powershell
dotnet run --
```

按 PID 指定：

```powershell
dotnet run -- --pid 1234 --proxy host.docker.internal:7899 --proxy-scheme https
```

按进程名指定：

```powershell
dotnet run -- --process devenv.exe --proxy host.docker.internal:7899 --proxy-scheme https
```

按进程名列表覆盖默认白名单：

```powershell
dotnet run -- --process-list "devenv.exe;msbuild.exe;onedrive.exe" --proxy host.docker.internal:7899
```

## 参数

- `--pid <数字>`：目标进程 PID。
- `--process <进程名>`：目标进程名（不区分是否带 `.exe` 后缀）。
- `--process-list "a.exe;b.exe"`：用分号分隔进程名，覆盖默认白名单。
- `--proxy <HOST:PORT>`：代理地址，支持主机名，例如 `host.docker.internal:7899`。
- `--proxy-scheme <http|https>`：代理协议标识（默认 `https`）。

## 默认进程白名单

- `devenv.exe`
- `blend.exe`
- `servicehub.host.netfx.x64.exe`
- `servicehub.intellicodemodelservice.exe`
- `servicehub.datawarehousehost.exe`
- `copilot-language-server.exe`
- `onedrive.exe`
- `perfwatson2.exe`
- `servicehub.roslyncodeanalysisservice.exe`
- `devhub.exe`
- `servicehub.host.extensibility.x64.exe`
- `servicehub.roslyncodeanalysisservices.exe`
- `servicehub.identityhost.exe`
- `servicehub.host.netfx.x86.exe`
- `msbuild.exe`
- `msbuildtaskhost.exe`
- `m365copilot.exe`
- `m365copilot_autostarter.exe`
- `m365copilot_widget.exe`
- `webviewhost.exe`

## 注意事项

1. 当前实现只处理 **IPv4 + TCP**，不含 UDP/IPv6。
2. 这是“强制改写目标地址”方案，代理端需能接收这类转发流量（通常用于透明代理/TUN 网关场景）。`https` 在此处是代理协议配置标识，不会把普通 TCP 数据自动转换为 HTTP CONNECT；请使用代理软件的透明转发端口（redir/tproxy），不要直接用显式 HTTP/HTTPS 代理端口。
3. 如需保留原目标信息给代理端，通常需要额外协议或配套透明代理方案。
4. 本地网络流量会被自动忽略（不重定向），规则：`localhost; 127.0.0.1; %ComputerName%; ::1; 10.*.*.*; 172.16-31.*.*`。

## 观测输出

程序在命中白名单进程时会输出日志，包含：源进程、PID、原始目标地址、重定向目标地址。例如：

```text
[15:21:06.318] process=devenv.exe(10432) target=13.107.42.14:443 redirect=192.168.65.254:7899
```

统计日志会包含：

- `out`：出站 TCP 包数量
- `in`：入站 TCP 包数量
- `redirected`：已改写到代理的出站包数量
- `inRewrite`：已反向改写的入站回包数量（这个值持续增长通常表示链路已打通）
