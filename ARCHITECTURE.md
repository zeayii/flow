# Zeayii.Flow 架构规范

简体中文 | [English](./ARCHITECTURE.en.md)

## 1. 模块分层

- `CommandLine`：宿主与入口层。
- `Core`：同步执行与策略层。
- `Presentation`：终端呈现层。
- `Zeayii.Flow.Tests`：验证层。

## 2. 核心术语

- `Policy`：策略选择规则，例如冲突策略、失败策略。
- `Task`：顶层同步任务，可能是单文件，也可能是目录。
- `WorkItem`：目录任务中的单文件工作单元。
- `Outcome`：单文件执行结果，描述成功/失败/已跳过等语义。
- `Resume`：基于临时产物和最终产物状态进行恢复或跳过。

## 3. 边界约束

### 3.1 CommandLine 约束

- 只负责参数绑定、计划加载、宿主 DI、dry-run 和退出码。
- 不实现同步算法。
- 不持有任务生命周期编排逻辑。

### 3.2 Core 约束

- 负责真正的任务执行、失败传播、冲突策略和恢复逻辑。
- 不承担终端布局和输入交互。
- 通过 `IPresentationManager` 输出任务、文件、日志与进度事件。

### 3.3 Presentation 约束

- 只负责展示状态，不参与文件复制决策。
- 保持单写者 UI 状态模型，避免多线程共享集合读写。
- 渲染逻辑与输入逻辑分离。

## 4. 生命周期模型

### 4.1 顶层任务生命周期

- `Pending`
- `Scanning`
- `Running`
- `Completed`
- `CompletedWithErrors`
- `Failed`
- `Skipped`
- `Canceled`

### 4.2 文件生命周期

- `Pending`
- `Running`
- `Completed`
- `Failed`
- `Skipped`
- `Canceled`

规则：

- `Skipped` 是执行结果，不是冲突策略。
- `Resume` 下命中“最终文件已完整存在”时，应表现为 `Skipped`。

## 5. 取消模型

Zeayii.Flow 采用单向下传的取消模型：

- `GlobalContext`：持有全局取消令牌源，负责停止整个程序
- `TaskExecutionContext`：从全局令牌派生任务级取消令牌，只负责当前顶层任务
- `FileExecutionContext`：从任务级令牌派生文件级取消令牌，只负责当前文件复制

关键约束：

- 取消只能从上层传播到下层
- 文件级取消不能反向取消任务
- 任务级取消不能通过子级令牌隐式修改全局状态
- 失败是否升级为任务取消或全局取消，由 `TaskFailurePolicy` 决定

### 5.1 失败与取消的关系

- `CancellationTokenSource` 是执行控制机制
- `Failed / Canceled / Skipped / Completed` 是结果语义
- 文件失败默认只产生文件失败结果
- 只有在 `StopCurrentTask` 或 `StopAll` 下，任务层才会显式触发更大范围的取消

### 5.2 目录任务的收口原则

- 目录任务取消时，已完成/已失败/已跳过的文件保持终态
- 正在运行或尚未开始的已发现文件会收敛为 `Canceled`
- 聚合阶段先收口文件终态，再决定顶层任务终态

## 6. 并发模型

- 顶层任务并发：`TaskConcurrency`
- 目录内部文件并发：`InnerConcurrency`
- 单文件复制内部是异步流式 IO，不再拆更细的分块并发语义

## 7. 恢复模型

- 最终目标路径可存在
- `.tmp` 临时产物可存在
- `Resume` 先检查最终产物，再检查临时产物
- 若最终产物已完整，则直接跳过
- 若临时产物不完整且可续，则从断点继续
- 若临时产物异常（例如长度超出），需转移为坏产物后重新执行

## 8. 呈现模型

- 标题栏：参数 + 摘要
- 左列：详细任务列表
- 中列：状态分布网格
- 右列：日志输出
- 详情页：单任务文件视图

关键规则：

- 左列和中列使用固定宽度布局
- 右列自适应
- 数量、速率、时长都要使用稳定宽度，避免抖动

## 9. 日志与输出规范

- TUI 内日志和窗口渲染分离
- 文件日志和终端日志分离
- 非 TUI 文本输出统一走 `AnsiConsole.Console`
- 命令行文案通过本地化资源访问器读取

## 10. 本地化规范

- 语言标签使用 BCP 47
- 基础资源使用 `Strings.resx`
- 卫星资源文件使用 `Strings.<culture>.resx`
- 默认中性文化为 `en-US`

## 11. 变更门禁

- 新增状态必须同步文档、测试和 TUI 渲染
- 冲突策略语义调整必须验证 `Resume / Overwrite / Rename`
- UI 布局变更必须验证滚动、终态与收尾
- 本地化结构变更必须验证帮助输出与卫星资源加载

