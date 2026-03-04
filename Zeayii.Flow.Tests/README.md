# Zeayii.Flow.Tests

简体中文 | [English](./README.en.md)

`Zeayii.Flow.Tests` 负责对 Zeayii.Flow 的关键行为做自动化验证。

## 1. 当前覆盖范围

- 任务排序规则
- 文件排序规则
- 文本渲染与显示宽度
- 日志环形缓冲
- 配置映射

## 2. 当前价值

虽然测试量不大，但它覆盖了几个容易回退的点：

- UI 排序语义
- 固定宽度文本处理
- 日志内存模型
- `OptionsBuilder` 到运行时配置的映射

## 3. 建议长期补充

- 冲突策略回归测试
- `Resume` 跳过语义测试
- 失败策略传播测试
- dry-run 预演测试
- 目录任务聚合结果测试

## 4. 常用命令

```bash
dotnet test Zeayii.Flow.sln -v minimal
```



