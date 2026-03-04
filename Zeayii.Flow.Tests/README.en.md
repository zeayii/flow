# Zeayii.Flow.Tests

[简体中文](./README.md) | English

`Zeayii.Flow.Tests` provides automated validation for key Zeayii.Flow behavior.

## 1. Current coverage

- task ordering
- file ordering
- text rendering and display width
- ring-buffer log storage
- option mapping

## 2. Current value

The test volume is still small, but it already protects several regression-prone areas:

- UI ordering semantics
- fixed-width text handling
- in-memory log model
- `OptionsBuilder` to runtime option mapping

## 3. Recommended future additions

- conflict policy regression tests
- `Resume` skip semantics tests
- failure policy propagation tests
- dry-run preview tests
- directory aggregate result tests

## 4. Common command

```bash
dotnet test Zeayii.Flow.sln -v minimal
```



