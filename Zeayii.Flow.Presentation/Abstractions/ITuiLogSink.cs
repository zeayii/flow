using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Presentation.Abstractions;

/// <summary>
/// 定义呈现层日志输入接口。
/// </summary>
public interface ITuiLogSink
{
    /// <summary>
    /// 记录 Trace 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Trace(string message, string? scope = null);

    /// <summary>
    /// 记录 Debug 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Debug(string message, string? scope = null);

    /// <summary>
    /// 记录 Information 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Information(string message, string? scope = null);

    /// <summary>
    /// 记录 Warning 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Warning(string message, string? scope = null);

    /// <summary>
    /// 记录 Error 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Error(string message, string? scope = null);

    /// <summary>
    /// 记录 Critical 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">可选作用域。</param>
    void Critical(string message, string? scope = null);

    /// <summary>
    /// 追加一条日志记录。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    void AppendLog(LogEntry entry);
}

