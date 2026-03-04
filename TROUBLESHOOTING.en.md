# Zeayii.Flow Troubleshooting

[简体中文](./TROUBLESHOOTING.md) | English

## 1. Files are not copied again on the second run

Symptoms:

- the first run created outputs
- the second run finishes very quickly

Cause:

- default conflict policy is `Resume`
- if the final output already exists in complete form, Zeayii.Flow surfaces it as `Skipped`

What to do:

- use `--conflict-policy overwrite` to force rewriting
- use `--conflict-policy rename` to keep existing outputs and write new names

## 2. Throughput on mapped directories fluctuates heavily

Cause:

- mapped paths are not plain local disk behavior
- upload/download depends on network, provider behavior, cache hits and mount implementation

What to do:

- do not judge the engine from short-term speed swings alone
- correlate with file logs and final outcomes
- reduce concurrency and observe whether stability improves

## 3. TUI exit behavior looks odd on some terminals

Symptoms:

- prompt position looks unstable after completion
- some terminals may appear to push the final frame by one line

What to do:

- validate in a real PowerShell / Windows Terminal / SSH terminal first
- avoid hosts that alter console buffer behavior
- if the problem is terminal-specific, treat it as terminal integration first

## 4. `--help` uses the wrong language

Check:

- whether `--lang` uses a BCP 47 tag
- examples: `en-US`, `zh-CN`, `zh-TW`, `ja-JP`, `ko-KR`

Fallback order:

- `--lang`
- `CurrentUICulture`
- `en-US`

## 5. Plan loading fails

Symptoms:

- `Plan file not found`
- `Invalid plan file`

Check:

- the path exists
- JSON is an array
- each item contains `src` and `dst`
- each `src` exists

## 6. Build fails with file locking errors

Symptoms:

- `CS2012`
- `MSB3026`

Common causes:

- `dotnet test` / `dotnet run` processes are still alive
- Windows Defender is scanning output DLLs

What to do:

- stop `testhost` / `flow.exe`
- run `build -> test -> publish` sequentially
- retry after Defender releases the file lock

## 7. NativeAOT output does not run as expected

Check:

- RID matches the target machine
- mapped directories are available on the target machine
- plan paths are valid on the target machine

## 8. Too many logs or higher memory usage than expected

What to do:

- lower `--ui-log-level`
- tune `--ui-log-max`
- move detailed diagnosis to file logs


