# Zeayii.Flow

简体中文 | [English](./README.en.md)

Flow 是一个面向 `网盘映射目录 <-> 本地媒体目录` 场景的模块化同步工具，由 Zeayii 工程体系维护。  
它的设计目标不是替代本地磁盘内的 `copy/move`，而是为“文件路径表面上是本地目录，底层语义实际上是上传/下载”的场景提供稳定、可恢复、可观测的同步能力。

## 1. 设计初衷

很多家庭媒体或服务器场景会采用下面的链路：

1. 云盘或网盘通过挂载工具映射到本地路径。
2. Jellyfin / Emby / Plex 等媒体服务把这个映射路径当作媒体库目录。
3. 用户希望在“本地整理目录”和“映射目录”之间做批量同步。

表面上看这是文件复制，实际上却不是传统的本地磁盘复制：

- 从本地写入映射目录，本质上往往是“上传”。
- 从映射目录读回本地，本质上往往是“下载”。
- 文件操作耗时长、易中断、可能出现部分产物。
- 同步过程需要进度、速度、日志和失败恢复。

因此 Flow 选择了：

- 基于异步文件流的复制模型
- 基于临时产物的断点续传模型
- 基于 TUI Dashboard 的运行时观测模型
- 基于策略的冲突处理与失败处理模型
- 基于 `Global -> Task -> File` 的单向取消模型

## 2. 工程定位

- 为网盘映射目录场景提供更接近“传输系统”而不是“文件管理器”的执行语义。
- 对单文件和目录任务统一抽象，支持可恢复、可中断、可重试的同步。
- 保持架构语义清晰：`CommandLine` 负责宿主，`Core` 负责执行，`Presentation` 负责呈现。
- 保持发布可用：支持 NativeAOT 单文件发布。

## 3. 项目结构

- `Zeayii.Flow.CommandLine`：命令行宿主、参数绑定、本地化与 dry-run。
- `Zeayii.Flow.Core`：同步引擎、任务运行时、异步流复制、策略与上下文。
- `Zeayii.Flow.Presentation`：Spectre.Console 终端 Dashboard。
- `Zeayii.Flow.Tests`：排序、文本渲染、日志缓冲、配置映射等测试。

## 4. 端到端调用链（Mermaid）

```mermaid
sequenceDiagram
    participant User as User
    participant CLI as CommandLine
    participant OPT as OptionsBuilder
    participant ENG as TaskTransferEngine
    participant RUN as TaskRuntime
    participant CAP as FileTransferCapability
    participant UI as Presentation

    User->>CLI: flow plan.json [options]
    CLI->>OPT: parse CLI + build ApplicationOptions
    OPT->>ENG: build CoreOptions / TaskRequest list
    CLI->>UI: start dashboard
    ENG->>RUN: schedule top-level tasks
    RUN->>CAP: stream copy / resume / finalize
    CAP-->>RUN: file outcome
    RUN->>UI: task progress + file progress + logs
    RUN-->>ENG: task result
    ENG-->>CLI: final exit code
    CLI-->>User: final dashboard / console output
```

## 5. 执行模型

### 5.1 为什么不用 `File.Copy`

在传统磁盘之间，`File.Copy` 很直接。  
但对“映射目录 = 上传/下载代理层”的场景，它有几个问题：

- 难以表达异步传输过程。
- 难以暴露稳定的实时进度与速度。
- 难以做精细的断点续传。
- 难以把 `.tmp` 产物、失败重试和状态机串起来。

因此 Flow 的核心传输路径使用异步流式 IO，而不是把同步语义外包给一次性系统调用。

### 5.2 断点续传

Flow 采用临时产物文件驱动恢复：

- 正在写入时先写 `.tmp`
- 若任务中断，后续可根据 `.tmp` 长度决定是否续传
- 若最终目标已完整存在，在 `Resume` 策略下直接视为 `Skipped`
- 复制完成后再做最终落盘

### 5.3 策略语义

- `ConflictPolicy`
  - `Resume`
  - `Overwrite`
  - `Rename`
- `TaskFailurePolicy`
  - `Continue`
  - `StopCurrentTask`
  - `StopAll`

注意：

- `Skipped` 是执行结果，不是冲突策略。
- `Resume` 包含“无需恢复则直接跳过”这一边界语义。

### 5.4 取消与失败语义

Flow 将“取消控制”和“执行结果”分开：

- `CancellationTokenSource` 负责停止执行
- `Completed / Skipped / Failed / Canceled` 负责表达结果
- 失败策略只决定是否把文件失败升级成更大范围的取消

当前取消层级为：

1. 全局运行取消
2. 顶层任务取消
3. 单文件取消

传播原则：

- 取消只从上层向下层传播
- 单文件不能反向取消任务
- 文件失败默认只是文件失败
- `StopCurrentTask` 才会取消当前任务
- `StopAll` 才会取消整个程序内的全部任务

## 6. 终端界面（TUI）

Flow 的主界面采用三列布局：

1. 左列：任务详细列表
2. 中列：状态分布与任务名称网格
3. 右列：日志输出

顶部标题栏分为两部分：

- 左侧：关键运行参数
- 右侧：实时摘要（数量 / 速率）

底部保留快捷键说明，详情页支持单独滚动查看目录下所有文件。

## 7. 运行前置

- .NET SDK 10.0+
- Windows / Linux / macOS 受支持运行时
- 若涉及网盘挂载，请先确保挂载层自身可稳定读写
- 若要使用 NativeAOT 单文件，请使用固定 RID 发布

## 8. 常用命令

```bash
dotnet build Zeayii.Flow.slnx -v minimal
```

```bash
dotnet test Zeayii.Flow.slnx -v minimal
```

```bash
dotnet run --project Zeayii.Flow.CommandLine -- plan.json
```

```bash
dotnet run --project Zeayii.Flow.CommandLine -- plan.json --dry-run
```

```bash
dotnet publish Zeayii.Flow.CommandLine/Zeayii.Flow.CommandLine.csproj -c Release -r win-x64 -p:PublishAot=true -p:PublishSingleFile=true -o .artifacts/publish/win-x64-aot
```

## 9. 文档导航

- 架构规范：[ARCHITECTURE.md](./ARCHITECTURE.md)
- 工程排障：[TROUBLESHOOTING.md](./TROUBLESHOOTING.md)
- CommandLine：[README.md](Zeayii.Flow.CommandLine/README.md)
- Core：[README.md](Zeayii.Flow.Core/README.md)
- Presentation：[README.md](Zeayii.Flow.Presentation/README.md)
- Tests：[README.md](Zeayii.Flow.Tests/README.md)

## 10. 发布检查清单

- `dotnet build Zeayii.Flow.slnx -v minimal` 通过
- `dotnet test Zeayii.Flow.slnx -v minimal` 通过
- `--dry-run` 与真实执行路径烟测通过
- `Resume / Overwrite / Rename` 语义符合预期
- Dashboard 布局、滚动、详情页可用
- 本地化资源和帮助文本已同步更新
- AOT 单文件产物已验证


