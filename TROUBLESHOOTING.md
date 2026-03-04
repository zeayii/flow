# Zeayii.Flow 排障指南

简体中文 | [English](./TROUBLESHOOTING.en.md)

## 1. 第二次执行时文件没有重新复制

现象：

- 第一次执行后目标目录已有文件
- 第二次执行看起来“很快结束”

原因：

- 默认冲突策略是 `Resume`
- 若目标最终文件已完整存在，Zeayii.Flow 会把该文件视为 `Skipped`

建议：

- 若希望强制重写，请改用 `--conflict-policy overwrite`
- 若希望保留已有文件并生成新名字，请改用 `--conflict-policy rename`

## 2. 映射目录速度波动很大

原因：

- 映射目录底层通常不是纯本地磁盘
- 上传/下载过程受网络、网盘服务端、缓存命中和挂载层影响

建议：

- 不要把瞬时速度波动误判为引擎异常
- 结合文件日志和最终结果判断
- 适当调小并发，观察是否更稳定

## 3. TUI 收尾时界面位置异常

现象：

- 任务结束后提示符位置看起来不稳定
- 某些终端可能把最终画面再顶一行

建议：

- 优先在真实 PowerShell / Windows Terminal / SSH 终端中验证
- 避免在会篡改控制台缓冲区的宿主中观察
- 若问题只在个别终端复现，优先按终端行为排查

## 4. `--help` 语言不正确

检查项：

- `--lang` 是否使用了 BCP 47 标签
- 示例：`en-US`、`zh-CN`、`zh-TW`、`ja-JP`、`ko-KR`

回退规则：

- 先看 `--lang`
- 再看 `CurrentUICulture`
- 最后回退到 `en-US`

## 5. 计划文件加载失败

现象：

- `Plan file not found`
- `Invalid plan file`

检查项：

- 路径是否存在
- JSON 是否是数组
- 每个元素是否包含 `src` 和 `dst`
- `src` 是否存在

## 6. 构建时出现文件占用错误

现象：

- `CS2012`
- `MSB3026`

常见原因：

- `dotnet test` / `dotnet run` 进程尚未退出
- Windows Defender 正在扫描输出 DLL

建议：

- 停止仍在运行的 `testhost` / `flow.exe`
- 串行执行 `build -> test -> publish`
- 若是 Defender 锁定，等待后重试

## 7. AOT 发布后无法正常运行

检查项：

- RID 是否与目标环境一致
- 挂载目录在目标机器上是否可访问
- 计划文件路径是否在目标环境有效

## 8. 日志过多或内存占用偏高

建议：

- 降低 `--ui-log-level`
- 调整 `--ui-log-max`
- 将排障重点转移到文件日志


