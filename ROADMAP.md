# ROADMAP

## 当前规划（2026-03-22）

### 目标
将当前 `Local API` 单上游转发能力，演进为统一的 **Ollama Gateway**：

- 对外优先保持 **单一 Ollama 协议入口**。
- 同一入口下兼容：
  - 原生 Ollama 上游
  - OpenAI Compatible 上游
  - Anthropic 上游
  - 后续可扩展更多服务商协议
- 无论背后服务商是谁，客户端始终以 Ollama 协议接入。

### 核心原则
1. **单端口优先**：默认只保留一个 Ollama 监听端口，不为不同上游协议单独新增 Ollama 端口。
2. **统一前门**：本地调用方不感知上游服务商差异。
3. **可扩展适配器**：未来新增服务商时，不改客户端接入方式，只增加上游协议适配器。
4. **模型路由驱动**：根据本地模型名 / 路由配置，将请求分发到不同 provider。
5. **文档同步**：每轮实施完成后同步更新 `README.md` 与 `CHANGELOG.md`。

### 分阶段计划

#### 第一阶段：架构重构
- 将当前单一 `LocalApiForwarderSettings.Provider` 设计，重构为多 provider 结构。
- 引入统一的 provider / route 配置模型：
  - Providers
  - Model Routes / Model Mappings
  - Provider Capabilities
- 保留现有配置兼容迁移逻辑，尽量不破坏旧配置。
- 将产品概念从 `Local API` 逐步演进到 `Ollama Gateway`。

#### 第二阶段：统一 Ollama 单入口
- 保持一个 Ollama 监听端口。
- 统一支持：
  - `/api/chat`
  - `/api/generate`
  - `/api/embed`
  - `/api/embeddings`
  - `/api/tags`
  - `/api/show`
- 请求进入后，根据模型映射选择上游 provider。
- 支持原生 Ollama 作为上游之一。

#### 第三阶段：Anthropic 完整适配
- 为 Anthropic provider 补齐流式支持：
  - Anthropic Stream -> Ollama NDJSON
  - Anthropic Stream -> OpenAI / Responses SSE（如保留兼容入口）
- 将 Anthropic 能力纳入统一 capability 模型。
- 对 embeddings 不再写死限制，改为按 provider capability 动态控制。

#### 第四阶段：配置界面重构
- 将当前 `Local API` 页面升级为统一 Gateway 配置页。
- 优先展示：
  - 本地监听配置
  - Provider 列表
  - 模型路由表
  - 认证配置
  - 能力与诊断选项
- 如有必要，再增加更清晰的 provider 分组视图，而不是为每种协议做独立、割裂的固定页面。

#### 第五阶段：扩展性能力
- 为未来服务商保留协议适配点。
- 支持新增更多 provider 类型，例如其他 OpenAI-compatible / Anthropic-compatible / 原生协议服务商。
- 保持统一 Ollama 客户端接入体验不变。

### 当前已确认约束
- Anthropic 路径最终需要支持 **流式**。
- Anthropic 路径需要能配置 **embedding**，但应以 provider capability 控制真实可用性。
- **不建议**为不同服务商再单独新增 Ollama 端口。
- 长期目标不是多个割裂 tab，而是统一网关能力。

### 实施注意事项
- 每次处理完后，必须同步更新：
  - `README.md`
  - `CHANGELOG.md`
- 若本轮涉及产品方向变化，也应同步更新本文件。
