# Zeayii.Flow.CommandLine

[简体中文](./README.md) | English

`Zeayii.Flow.CommandLine` is the host layer of Zeayii.Flow. It owns argument binding, plan loading, localization, dry-run behavior and host composition.

## 1. Responsibilities

- build CLI options and validation
- load plan files
- compose `ApplicationOptions / CoreOptions / PresentationOptions`
- start `TaskTransferEngine`
- handle dry-run and exit codes
- provide localization access

## 2. Directory layout

- `Default/`: default host implementations such as `DryRunExecutor` and `JsonFileLoader`
- `Localization/`: language resolution and resource accessor
- `Models/`: plan file model
- `Options/`: host option models and builders
- `Resources/`: `Strings.*.resx` localization resources

## 3. Call flow (Mermaid)

```mermaid
sequenceDiagram
    participant User
    participant CLI as Program
    participant LOC as CliTextProvider
    participant OPT as OptionsBuilder
    participant LDR as JsonFileLoader
    participant ENG as TaskTransferEngine

    User->>CLI: sync plan.json [options]
    CLI->>LOC: configure culture
    CLI->>LDR: load plan.json
    CLI->>OPT: build options
    CLI->>ENG: run tasks
    ENG-->>CLI: exit code
    CLI-->>User: console / TUI
```

## 4. Localization rules

- language tags use BCP 47
- currently supported:
  - `en-US`
  - `zh-CN`
  - `zh-TW`
  - `ja-JP`
  - `ko-KR`
- base resource file: `Strings.resx`
- satellite resources: `Strings.<culture>.resx`

## 5. dry-run semantics

- `--dry-run`
  - enters TUI preview when UI is enabled
- `--dry-run --no-ui`
  - prints text preview only and does not execute copy

## 6. Troubleshooting focus

- validation errors: check numeric ranges first
- invalid `--lang`: check BCP 47 compliance
- plan loading failure: check `src/dst` and JSON structure

## 7. Release checklist

- `--help` text is correct
- `--lang` switching works
- both dry-run paths work
- exit codes and error paths are predictable

