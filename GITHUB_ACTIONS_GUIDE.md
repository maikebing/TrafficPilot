# GitHub Actions 发布工作流指南

## 📋 工作流说明

这个 GitHub Actions 工作流自动构建和发布 TrafficPilot 软件。

### 工作流触发条件

| 触发事件 | 行为 | 版本号 |
|---------|------|--------|
| 推送到 `master` 分支 | 仅构建，保存构建产物为 artifacts | `beta` |
| 创建 `v*` 标签 | 构建、打包、创建 Release | 从标签名称提取（如 `v1.0.0` → `1.0.0`） |
| 手动触发（workflow_dispatch） | 根据分支决定 | 同上 |

## 🚀 快速开始

### 1. 在 Master 分支进行构建

当你推送代码到 master 分支时，工作流会自动：
- 恢复依赖
- 编译项目
- 上传构建产物到 GitHub Actions artifacts（保留 7 天）

```bash
git push origin master
```

查看构建结果：
1. 打开 GitHub 仓库
2. 点击 **Actions** 标签
3. 选择最新的 workflow run
4. 下载 artifacts 中的构建产物

### 2. 发布新版本

当你需要发布新版本时：

#### 步骤 1：在本地创建标签

```bash
# 创建标签（格式：v主版本.次版本.修订版本）
git tag v1.0.0

# 或带有发布说明
git tag v1.0.0 -m "Release version 1.0.0"
```

#### 步骤 2：推送标签到 GitHub

```bash
# 推送单个标签
git push origin v1.0.0

# 或推送所有标签
git push origin --tags
```

#### 步骤 3：工作流自动执行

工作流将会：
1. ✅ 编译项目（版本号为 `1.0.0`）
2. ✅ 发布为自包含的可执行文件
3. ✅ 压缩为 `TrafficPilot-1.0.0.zip`
4. ✅ 创建 GitHub Release
5. ✅ 上传 zip 文件到 Release

#### 步骤 4：查看发布

1. 打开 GitHub 仓库主页
2. 右侧边栏点击 **Releases**
3. 找到你新创建的版本
4. 下载 `TrafficPilot-x.x.x.zip` 文件

## 📦 发布产物

### Release 包含内容

- **TrafficPilot-x.x.x.zip** - 完整发布包，包含：
  - TrafficPilot.exe - 主应用程序
  - 所有必要的 .NET Runtime 文件（自包含）
  - 配置文件
  - 依赖库

### 用户下载后

用户只需要：
1. 解压 zip 文件
2. 双击 `TrafficPilot.exe` 运行

无需安装 .NET Runtime（已包含在内）

## 🔍 工作流细节

### 环境变量

```yaml
DOTNET_VERSION: '10.0.x'
PROJECT_NAME: 'TrafficPilot'
```

### 主要步骤

| 步骤 | 说明 | 条件 |
|------|------|------|
| Checkout | 检出代码 | 总是 |
| Setup .NET | 安装 .NET 10 | 总是 |
| Restore dependencies | 恢复 NuGet 依赖 | 总是 |
| Determine version | 判断版本号 | 总是 |
| Build | 编译项目 | 总是 |
| Publish | 发布为自包含可执行文件 | 仅 Release |
| Package Release | 压缩为 zip | 仅 Release |
| Create Release Notes | 生成发布说明 | 仅 Release |
| Upload Release | 创建 GitHub Release | 仅 Release |
| Upload build artifacts | 上传构建产物 | Master 分支 |

## 🐛 故障排除

### 构建失败

1. 检查 **Actions** 标签中的错误日志
2. 确保所有依赖已正确配置
3. 验证 C# 版本是否为 14.0
4. 验证 .NET 版本是否为 10

### Release 未创建

1. 确保标签名称格式正确（`v*`）
2. 检查 `GITHUB_TOKEN` 权限
3. 查看 Actions 日志中的错误信息

### zip 文件过大

这是正常的，因为包含了自包含的 .NET Runtime。

## 🔐 权限设置

确保 GitHub 仓库的 Actions 权限设置正确：

1. 打开仓库设置 **Settings**
2. 左侧选择 **Actions** → **General**
3. 确保 **Workflow permissions** 设置为 **Read and write permissions**
4. 启用 **Allow GitHub Actions to create and approve pull requests**

## 📝 版本号规范

建议使用语义化版本号（Semantic Versioning）：

```
v主版本.次版本.修订版本[-预发布版本]

示例：
- v1.0.0       - 正式版
- v1.0.1       - 补丁版本
- v1.1.0       - 次版本更新
- v2.0.0       - 主版本更新
- v1.0.0-beta  - 测试版
- v1.0.0-rc.1  - 候选版
```

## 🎯 最佳实践

1. ✅ 在创建 Release 前进行充分测试
2. ✅ 使用有意义的标签名称和提交说明
3. ✅ 在 master 分支上保持代码的稳定性
4. ✅ 定期检查 Actions 运行日志
5. ✅ 为 Release 添加详细的变更说明

## 📚 相关文档

- [GitHub Actions 文档](https://docs.github.com/actions)
- [Semantic Versioning](https://semver.org/)
- [.NET 发布配置](https://docs.microsoft.com/dotnet/core/deploying/)
