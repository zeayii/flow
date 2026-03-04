# Zeayii.Flow

[简体中文](./README.md) | English

Flow is a modular synchronization tool for the `cloud-drive mapped directory <-> local media directory` scenario, maintained under the Zeayii engineering family.  
It is not designed as a replacement for plain local `copy/move`. Its purpose is to handle cases where a path looks local, but the real behavior underneath is upload/download through a mount layer.

## 1. Why Flow exists

A common home-media or server setup looks like this:

1. A cloud drive is mounted to a local path by a drive-mapping tool.
2. Jellyfin / Emby / Plex treats that mapped path as a media library root.
3. The user wants to synchronize files between a local staging directory and that mapped directory.

On the surface this looks like file copy. In practice it behaves like transfer:

- Writing into the mapped directory is effectively an upload.
- Reading back from the mapped directory is effectively a download.
- Transfers can be long-running and interruptible.
- Partial outputs and retries must be handled explicitly.
- Real-time progress, throughput and logs are operational requirements.

For that reason Flow uses:

- asynchronous stream-based copy
- temporary-artifact-based resume
- TUI dashboard observability
- explicit conflict and failure policies
- a downward-only `Global -> Task -> File` cancellation model

## 2. Project role

- Provide transfer-oriented semantics for mapped-cloud-drive paths.
- Unify single-file and directory tasks under the same runtime model.
- Keep architecture boundaries clear: `CommandLine` hosts, `Core` executes, `Presentation` renders.
- Stay publishable and practical, including NativeAOT single-file output.

## 3. Project structure

- `Zeayii.Flow.CommandLine`: host, CLI binding, localization and dry-run.
- `Zeayii.Flow.Core`: engine, task runtime, async stream copy, policies and contexts.
- `Zeayii.Flow.Presentation`: Spectre.Console dashboard.
- `Zeayii.Flow.Tests`: validation for ordering, rendering helpers, log buffer and option mapping.

## 4. End-to-end call flow (Mermaid)

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

## 5. Execution model

### 5.1 Why Flow does not rely on `File.Copy`

`File.Copy` is fine for plain local disk operations.  
For mounted cloud-drive paths, it is not a sufficient execution model:

- it does not express asynchronous transfer behavior
- it does not provide stable task-level progress and throughput semantics
- it does not provide a natural resume model
- it does not fit a `.tmp` artifact and retry-based workflow cleanly

Flow therefore keeps the transfer path inside explicit async stream IO.

### 5.2 Resume model

Flow uses temporary artifacts for recovery:

- write into `.tmp` first
- on interruption, decide resume from current `.tmp` length
- if final output already exists and is complete, `Resume` treats it as `Skipped`
- finalize only after the copy succeeds

### 5.3 Policy model

- `ConflictPolicy`
  - `Resume`
  - `Overwrite`
  - `Rename`
- `TaskFailurePolicy`
  - `Continue`
  - `StopCurrentTask`
  - `StopAll`

Important:

- `Skipped` is an execution result, not a conflict policy.
- `Resume` includes the edge case "nothing to resume, already up to date".

### 5.4 Cancellation and failure semantics

Flow keeps execution control separate from execution result:

- `CancellationTokenSource` stops work
- `Completed / Skipped / Failed / Canceled` describe outcomes
- failure policy only decides whether a file failure escalates into broader cancellation

The active cancellation hierarchy is:

1. global run cancellation
2. top-level task cancellation
3. single-file cancellation

Propagation rules:

- cancellation flows downward only
- a file must not cancel its parent task
- a file failure is a file failure first
- only `StopCurrentTask` cancels the current task
- only `StopAll` cancels every task in the current run

## 6. Terminal UI

The main dashboard uses a three-column layout:

1. left: detailed task list
2. middle: status distribution and task name grid
3. right: logs

The header is split into:

- left side: configuration parameters
- right side: runtime summary

The footer keeps key bindings visible, and the detail page supports full scrolling through all files under a directory task.

## 7. Prerequisites

- .NET SDK 10.0+
- supported runtime on Windows / Linux / macOS
- a stable mounted cloud-drive layer if the workload depends on it
- fixed RID for NativeAOT single-file publishing

## 8. Common commands

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

## 9. Documentation map

- Architecture: [ARCHITECTURE.en.md](./ARCHITECTURE.en.md)
- Troubleshooting: [TROUBLESHOOTING.en.md](./TROUBLESHOOTING.en.md)
- CommandLine: [README.en.md](Zeayii.Flow.CommandLine/README.en.md)
- Core: [README.en.md](Zeayii.Flow.Core/README.en.md)
- Presentation: [README.en.md](Zeayii.Flow.Presentation/README.en.md)
- Tests: [README.en.md](Zeayii.Flow.Tests/README.en.md)

## 10. Release checklist

- `dotnet build Zeayii.Flow.slnx -v minimal` passes
- `dotnet test Zeayii.Flow.slnx -v minimal` passes
- `--dry-run` and real execution smoke tests pass
- `Resume / Overwrite / Rename` semantics are verified
- dashboard layout, scrolling and detail page are usable
- localization resources and help text are synchronized
- NativeAOT single-file output is validated


