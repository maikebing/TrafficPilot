# TrafficPilot 快速使用指南

## 功能概述

TrafficPilot 是一个 Windows 代理管理器，可以让**指定进程名称的所有网络流量通过代理服务器**。

### 主要特性
- ✅ 指定单个或多个进程名称进行代理
- ✅ 支持通配符进程匹配（例如：`chrome*.exe`）
- ✅ 支持指定特定 PID
- ✅ 实时日志监控和统计信息
- ✅ 代理配置保存/加载

## 工作原理

```
应用程序发送数据包
    ↓
WinDivert 拦截
    ↓
检查发送进程 PID
    ↓
比对进程允许列表
    ↓
匹配 ✅ → 重定向到代理服务器 → 转发到目标
不匹配 ❌ → 直接转发到目标（不经过代理）
```

## 使用步骤

### 1. 启动应用程序
```
打开 TrafficPilot.exe
```

### 2. 配置代理参数

#### 代理设置
- **Proxy Host**：代理服务器地址（例如：`127.0.0.1`、`10.0.0.1`、`proxy.company.com`）
- **Proxy Port**：代理端口（例如：`7890` for Clash、`1080` for Shadowsocks）
- **Proxy Scheme**：代理协议（`socks4`、`socks5`、`http`）

#### 进程配置
在 "Process Names" 框中添加需要代理的进程：

**完整进程名称示例：**
- `chrome.exe` - Google Chrome
- `firefox.exe` - Mozilla Firefox
- `notepad.exe` - 记事本
- `code.exe` - Visual Studio Code
- `git.exe` - Git 命令行工具

**通配符示例：**
- `chrome*.exe` - 所有 chrome 开头的进程
- `vs?ext.exe` - vstext.exe 这样的进程

**额外 PID：**
- 如果知道具体的进程 ID，也可以直接添加 PID 号
- 系统进程 PID 可以通过任务管理器查看

### 3. 保存配置（可选）
点击 "Save Config" 按钮保存当前配置到 `TrafficPilot.config.json` 文件。

下次启动时可以点击 "Load Config" 恢复配置。

### 4. 启动代理
点击绿色的 "Start Proxy" 按钮。

**验证启动成功：**
- 按钮变为红色，显示 "Stop Proxy"
- 状态显示 "Status: Running"（绿色）
- 日志显示相关信息

### 5. 监控日志

在 "Logs" 标签页可以看到：

**代理重定向日志示例：**
```
[14:32:45.123] Relay started at 0.0.0.0:54321
[14:32:45.234] Proxy target: 127.0.0.1:7890 (socks5)
[14:32:45.345] Process rules: names=[chrome.exe, firefox.exe] pids=[none]
[14:32:50.456] chrome.exe(2856) 192.168.1.100:54632 -> 8.8.8.8:443 >> relay >> socks5://127.0.0.1:7890  [DNS over HTTPS]
[14:32:51.567] chrome.exe(2856) 192.168.1.100:54633 -> 142.251.41.14:443 >> relay >> socks5://127.0.0.1:7890  [google.com]
```

**被跳过的连接：**
```
[14:33:00.123] [skip] svchost.exe(456) 192.168.1.100:54634 -> 8.8.8.8:53 (Process not in allow-list)
```

**统计信息：**
- **Redirected** - 重定向到代理的连接数
- **Proxied** - 成功代理的连接数
- **Failed** - 代理失败的连接数

### 6. 停止代理
点击红色的 "Stop Proxy" 按钮。

**验证停止成功：**
- 按钮变为绿色，显示 "Start Proxy"
- 状态显示 "Status: Stopped"（黑色）
- 所有资源已释放

## 常见使用场景

### 场景 1：为 Chrome 配置代理

1. 代理设置
   - Host: `127.0.0.1`
   - Port: `7890`
   - Scheme: `socks5`

2. 进程配置
   - 添加 "chrome.exe"

3. 启动代理后，所有 Chrome 的网络请求都会通过代理

### 场景 2：为多个应用配置代理

1. 进程配置
   - 添加 "chrome.exe"
   - 添加 "firefox.exe"
   - 添加 "code.exe"

2. 启动代理，这三个应用的所有网络流量都会经过代理

### 场景 3：为特定版本的应用配置代理

某些应用有多个版本的可执行文件名：
- 使用通配符：`discord*.exe` 可以匹配 Discord 的各个版本

### 场景 4：为系统服务配置代理

1. 打开任务管理器，找到服务进程的 PID
2. 在 "Extra PIDs" 中添加该 PID
3. 启动代理

## 故障排查

### 问题：点击 "Start Proxy" 后没有反应

**解决方案：**
1. 确认代理服务器地址正确且可访问
2. 确认已添加至少一个进程
3. 查看日志是否有错误信息
4. 以管理员身份运行 TrafficPilot（需要 WinDivert 驱动权限）

### 问题：应用流量没有经过代理

**检查清单：**
1. ✓ 应用进程名称拼写正确（注意大小写）
2. ✓ 进程名称包含 `.exe` 扩展名
3. ✓ 代理状态显示 "Running"（绿色）
4. ✓ 查看日志确认该进程名称在允许列表中
5. ✓ 如果应用有多个进程，需要添加所有相关进程

### 问题：某些连接被跳过

这是正常现象。日志中的 `[skip]` 表示：
- 该连接的发起进程不在允许列表中
- 或系统无法确定该连接的所有者进程

### 问题：代理运行一段时间后变慢

**可能原因：**
1. 网络连接过多，统计信息缓存增大
2. 日志缓冲区已满（超过 10000 行时自动清空）

**解决方案：**
- 点击 Logs 标签页的 "Clear Logs" 按钮清空日志

## 技术细节

### 配置文件位置
```
%APPDATA%/TrafficPilot/TrafficPilot.config.json
```

### 配置文件格式
```json
{
  "proxy": {
    "host": "127.0.0.1",
    "port": 7890,
    "scheme": "socks5"
  },
  "targeting": {
    "processNames": ["chrome.exe", "firefox.exe"],
    "extraPids": [1234, 5678]
  }
}
```

### 系统要求
- Windows 7 或更高版本
- .NET 10 运行时
- 管理员权限（用于 WinDivert 驱动加载）
- 有效的代理服务器

### 支持的代理协议
- **SOCKS4** - 传统 SOCKS 协议
- **SOCKS5** - SOCKS v5 协议（推荐）
- **HTTP** - HTTP 代理

## 最佳实践

1. **保存配置** - 经常使用的配置应该保存以便快速恢复
2. **监控日志** - 定期检查日志确保预期的进程被代理
3. **测试连接** - 使用 `ping` 或浏览网页验证代理生效
4. **性能监控** - 注意 "Failed" 计数，过多失败可能表示代理配置有问题

## 提示和技巧

- 💡 进程名称匹配是**不区分大小写**的
- 💡 添加通配符时支持 `*` 和 `?` 通配符
- 💡 可以同时为多个应用配置代理
- 💡 系统进程会自动被跳过（代理程序自己的进程）
- 💡 本地流量（127.0.0.1）会自动被跳过

---

有任何问题或建议，欢迎提交 Issue！
