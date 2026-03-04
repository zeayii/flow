# Zeayii.Flow Architecture

[简体中文](./ARCHITECTURE.md) | English

## 1. Module layers

- `CommandLine`: host and entry layer
- `Core`: synchronization execution and policy layer
- `Presentation`: terminal rendering layer
- `Zeayii.Flow.Tests`: validation layer

## 2. Core terms

- `Policy`: selection rule such as conflict policy or failure policy
- `Task`: top-level synchronization unit, either file or directory
- `WorkItem`: per-file unit inside a directory task
- `Outcome`: single-file execution result
- `Resume`: recover-or-skip behavior driven by temporary and final artifacts

## 3. Boundary rules

### 3.1 CommandLine

- owns argument binding, plan loading, DI host, dry-run and exit codes
- does not implement synchronization algorithms
- does not own task lifecycle orchestration

### 3.2 Core

- owns task execution, failure propagation, conflict policies and recovery
- does not own terminal layout or input handling
- reports state through `IPresentationManager`

### 3.3 Presentation

- renders state only, does not make copy decisions
- keeps a single-writer UI state model
- separates rendering from input handling

## 4. Lifecycle model

### 4.1 Top-level task states

- `Pending`
- `Scanning`
- `Running`
- `Completed`
- `CompletedWithErrors`
- `Failed`
- `Skipped`
- `Canceled`

### 4.2 File states

- `Pending`
- `Running`
- `Completed`
- `Failed`
- `Skipped`
- `Canceled`

Rule:

- `Skipped` is an execution result, not a conflict policy.
- Under `Resume`, a file that already exists in complete form should surface as `Skipped`.

## 5. Cancellation model

Zeayii.Flow uses a downward-only cancellation model:

- `GlobalContext`: owns the global cancellation source and stops the whole run
- `TaskExecutionContext`: derives a task-scoped token from the global token and controls one top-level task
- `FileExecutionContext`: derives a file-scoped token from the task token and controls one file copy only

Key rules:

- cancellation flows from parent to child only
- a file-scoped cancellation source must not cancel its parent task
- a task-scoped cancellation source must not implicitly mutate global state through child scopes
- whether a failure escalates into task cancellation or global cancellation is decided by `TaskFailurePolicy`

### 5.1 Failure versus cancellation

- `CancellationTokenSource` is an execution control mechanism
- `Failed / Canceled / Skipped / Completed` are outcome semantics
- a file failure is a file result first
- only `StopCurrentTask` or `StopAll` may explicitly escalate that result into a broader cancellation

### 5.2 Directory-task convergence

- completed, failed and skipped files keep their terminal states on cancellation
- discovered files that are still pending or running converge to `Canceled`
- aggregation converges file terminal states first, then resolves the top-level task state

## 6. Concurrency model

- top-level task concurrency: `TaskConcurrency`
- per-directory file concurrency: `InnerConcurrency`
- single-file copy stays async stream-based; no extra internal block concurrency abstraction

## 7. Recovery model

- final output may already exist
- `.tmp` artifact may already exist
- `Resume` checks final artifact first, then temporary artifact
- complete final output means skip
- incomplete temporary output means resume from offset
- invalid temporary output must be moved aside before retry

## 8. Presentation model

- header: parameters + summary
- left column: detailed task list
- middle column: status distribution grid
- right column: logs
- detail page: file view for a single task

Key rules:

- left and middle columns use fixed widths
- right column is adaptive
- counts, rates and elapsed time use stable fixed-width formatting

## 9. Logging and output rules

- TUI log flow is separated from dashboard rendering
- file logging is separated from terminal logging
- non-TUI text output uses `AnsiConsole.Console`
- CLI text is resolved from localization resources

## 10. Localization rules

- language tags use BCP 47
- base resource file is `Strings.resx`
- satellite resources use `Strings.<culture>.resx`
- neutral language is `en-US`

## 11. Change gates

- any new state must update docs, tests and TUI rendering
- conflict policy changes must verify `Resume / Overwrite / Rename`
- UI layout changes must verify scrolling, final states and exit behavior
- localization changes must verify help output and satellite resource loading

