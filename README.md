# TrafficPilot - WinForms 代理管理工具

## 项目说明

TrafficPilot 已从控制台应用转换为 **WinForms 桌面应用**，提供了友好的图形化界面来管理网络代理规则。

## 主要功能

### 1. **图形化配置界面**
- 代理设置：支持设置代理主机、端口和协议（SOCKS4/SOCKS5/HTTP）
- 进程管理：
  - 添加/移除要拦截的进程名称
  - 支持通配符 (如 `msbuild*.exe`)
  - 添加/移除特定的进程 PID
- JSON 配置文件支持：自动加载和保存配置

### 2. **系统托盘集成**
- 最小化到系统托盘
- 托盘图标快速访问
- 右键菜单：显示/隐藏、退出

### 3. **实时日志视图**
- 彩色日志显示
- 实时统计信息
- 可清空日志

### 4. **JSON 配置文件格式**

配置文件位置：`%AppData%\TrafficPilot\config.json`

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

## 项目结构

### 核心文件

| 文件 | 描述 |
|------|------|
| `ProgramEntry.cs` | 应用入口点，包含 Main 方法和管理员检查 |
| `MainForm.cs` | 主 WinForms 窗体，包含所有 UI 控件 |
| `ProxyEngine.cs` | 代理引擎核心，处理数据包转发 |
| `ProxyConfig.cs` | JSON 配置管理 |
| `TcpRelayServer.cs` | TCP 中继服务器 |
| `NetworkClasses.cs` | 网络处理核心类（WinDivert、数据包检查等） |
| `UtilityClasses.cs` | 工具类（进程匹配、本地流量绕过等） |

### WinDivert 依赖

- `WinDivert.dll`：Windows 网络数据包拦截库
- `WinDivert64.sys`：系统驱动程序

## 使用方法

### 基本流程

1. **以管理员身份运行** - 应用需要管理员权限
2. **配置代理设置**：
   - 填入代理服务器地址和端口
   - 选择代理协议
3. **配置目标进程**：
   - 添加要拦截流量的进程名称
   - 或添加具体的进程 PID
4. **保存配置**：点击"Save Config"保存到 JSON 文件
5. **启动代理**：点击"Start Proxy"开始拦截

### UI 界面

#### Configuration 标签页
- 代理设置表单
- 进程名称列表（支持通配符）
- 额外 PID 列表
- 保存/加载配置按钮

#### Logs 标签页
- 实时日志显示
- 清除日志按钮

#### 状态栏
- 当前运行状态
- 实时统计信息（重定向数、代理成功/失败数）

## 配置说明

### 代理协议支持

- **SOCKS4**：基础 SOCKS 协议
- **SOCKS5**：支持更多功能的 SOCKS 协议
- **HTTP**：HTTP CONNECT 隧道

### 进程名称格式

- 完整名称：`devenv.exe`
- 通配符：`msbuild*.exe` 匹配所有 msbuild 相关程序
- 自动补全：输入 `devenv` 会自动转换为 `devenv.exe`

### 本地流量自动绕过

以下本地流量会自动绕过代理：
- 127.0.0.0/8 (本地回环)
- 10.0.0.0/8 (私有网络)
- 172.16.0.0/12 (私有网络)
- 192.168.0.0/16 (私有网络)
- 169.254.0.0/16 (链接本地)
- 本机名称
- localhost

## 技术架构

### 分层设计

```
┌─────────────────────────────────┐
│     WinForms UI (MainForm)      │  用户界面
├─────────────────────────────────┤
│     ProxyEngine (ProxyEngine)   │  业务逻辑
├─────────────────────────────────┤
│  Network Stack & WinDivert      │  低层网络
└─────────────────────────────────┘
```

### 关键组件

1. **ProxyEngine**：
   - 管理 WinDivert 会话
   - 处理数据包拦截和重定向
   - 发送事件给 UI 更新

2. **TcpRelayServer**：
   - 接收被重定向的连接
   - 与上游代理进行握手
   - 双向数据转发

3. **ProxyConfigManager**：
   - JSON 配置的序列化/反序列化
   - 自动创建配置目录

4. **ProcessAllowListMatcher**：
   - 进程白名单管理
   - 支持通配符模式匹配
   - 缓存活跃进程列表

## 异步设计

- 所有网络操作都是异步的
- UI 操作在主线程执行
- 支持 CancellationToken 优雅关闭

## 安全性注意

1. **管理员权限**：必须以管理员身份运行
2. **配置文件保护**：存储在用户 AppData 目录
3. **错误处理**：捕获并记录所有异常
4. **资源清理**：使用 IDisposable 模式确保资源释放

## 故障排除

### 启动失败

- 检查是否以管理员身份运行
- 检查 WinDivert.dll 和 WinDivert64.sys 是否存在
- 查看日志信息获取详细错误

### 代理不生效

- 确认目标进程名称正确
- 检查代理服务器是否可访问
- 查看日志中是否有"skip"消息

### 配置文件丢失

- 文件位置：`%AppData%\TrafficPilot\config.json`
- 删除配置文件后重启会使用默认值

## 构建和编译

### 系统要求
- Windows 7 或更新版本
- .NET 8.0 或更新版本
- Visual Studio 2022 或更新版本（开发用）

### 编译命令
```bash
dotnet build
dotnet publish -c Release
```

## 许可证

按照原项目许可证

## 贡献指南

欢迎提交 Issue 和 Pull Request！
