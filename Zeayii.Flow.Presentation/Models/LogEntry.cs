namespace Zeayii.Flow.Presentation.Models;

/// <summary>
/// 表示一条日志记录。
/// </summary>
/// <param name="Timestamp">时间戳。</param>
/// <param name="Level">日志等级。</param>
/// <param name="Message">日志消息。</param>
/// <param name="Scope">可选作用域。</param>
public readonly record struct LogEntry(DateTimeOffset Timestamp, PresentationLogLevel Level, string Message, string? Scope = null);
