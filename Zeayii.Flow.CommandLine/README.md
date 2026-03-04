# Zeayii.Flow.CommandLine

简体中文 | [English](./README.en.md)

`Zeayii.Flow.CommandLine` 模块是 Zeayii.Flow 的宿主层，负责参数绑定、计划加载、本地化、dry-run 与服务组装。

## 1. 模块职责

- 构建 CLI 参数和验证逻辑
- 解析计划文件
- 组装 `ApplicationOptions / CoreOptions / PresentationOptions`
- 启动 `TaskTransferEngine`
- 处理 dry-run 与退出码
- 提供本地化资源访问

## 2. 目录结构

- `Default/`：默认宿主实现，例如 `DryRunExecutor`、`JsonFileLoader`
- `Localization/`：语言解析与资源访问器
- `Models/`：计划文件模型
- `Options/`：宿主配置模型与构建器
- `Resources/`：`Strings.*.resx` 多语言资源

## 3. 调用链（Mermaid）

```mermaid
sequenceDiagram
    participant User
    participant CLI as Program
    participant LOC as CliTextProvider
    participant OPT as OptionsBuilder
    participant LDR as JsonFileLoader
    participant ENG as TaskTransferEngine

    User->>CLI: run flow with plan
    CLI->>LOC: configure culture
    CLI->>LDR: load plan.json
    CLI->>OPT: build options
    CLI->>ENG: run tasks
    ENG-->>CLI: exit code
    CLI-->>User: console or tui
```

## 4. 本地化规范

- 语言标签使用 BCP 47
- 当前支持：
  - `en-US`
  - `zh-CN`
  - `zh-TW`
  - `ja-JP`
  - `ko-KR`
- 基础资源文件：`Strings.resx`
- 卫星资源文件：`Strings.<culture>.resx`

## 5. dry-run 语义

- `--dry-run`
  - 在启用 UI 时进入 TUI 预演
- `--dry-run --no-ui`
  - 只输出文本预览，不执行复制

## 6. 排障重点

- 参数验证报错：先检查数值范围
- `--lang` 无效：检查是否符合 BCP 47
- 计划文件报错：检查 `src/dst` 和 JSON 结构

## 7. 发布检查清单

- `--help` 文案正确
- `--lang` 切换有效
- dry-run 文本与 TUI 两条路径都可用
- 退出码和错误路径符合预期

