using Spectre.Console;

namespace Zeayii.Flow.CommandLine.Default;

/// <summary>
/// 封装基于 AnsiConsole.Console 的命令行文本输出。
/// </summary>
internal static class ConsoleOutput
{
    /// <summary>
    /// 输出统一风格日志行。
    /// </summary>
    /// <param name="levelTag">日志级别标签。</param>
    /// <param name="scope">日志作用域。</param>
    /// <param name="message">日志内容。</param>
    public static void WriteLogLine(string levelTag, string scope, string message)
    {
        var safeScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope;
        var safeMessage = string.IsNullOrWhiteSpace(message) ? "-" : message;
        var line = $"[{DateTimeOffset.Now:HH:mm:ss}] [{levelTag}] [{safeScope}] {safeMessage}";
        AnsiConsole.Console.Write(new Text(line));
        AnsiConsole.Console.Write(new Text(Environment.NewLine));
    }

    /// <summary>
    /// 输出 Information 级别日志行。
    /// </summary>
    /// <param name="scope">日志作用域。</param>
    /// <param name="message">日志内容。</param>
    public static void WriteInfoLine(string scope, string message) => WriteLogLine("INF", scope, message);

    /// <summary>
    /// 输出 Warning 级别日志行。
    /// </summary>
    /// <param name="scope">日志作用域。</param>
    /// <param name="message">日志内容。</param>
    public static void WriteWarningLine(string scope, string message) => WriteLogLine("WRN", scope, message);

    /// <summary>
    /// 输出普通文本行。
    /// </summary>
    /// <param name="message">文本内容。</param>
    public static void WriteLine(string message)
    {
        AnsiConsole.Console.Write(new Text(message));
        AnsiConsole.Console.Write(new Text(Environment.NewLine));
    }

    /// <summary>
    /// 输出错误文本行。
    /// </summary>
    /// <param name="message">错误内容。</param>
    public static void WriteErrorLine(string message)
    {
        AnsiConsole.Console.Write(new Markup($"[red]{Markup.Escape(message)}[/]"));
        AnsiConsole.Console.Write(new Text(Environment.NewLine));
    }

    /// <summary>
    /// 输出 Error 级别日志行。
    /// </summary>
    /// <param name="scope">日志作用域。</param>
    /// <param name="message">日志内容。</param>
    public static void WriteErrorLine(string scope, string message)
    {
        var safeScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope;
        var safeMessage = string.IsNullOrWhiteSpace(message) ? "-" : message;
        var line = $"[{DateTimeOffset.Now:HH:mm:ss}] [ERR] [{safeScope}] {safeMessage}";
        AnsiConsole.Console.Write(new Markup($"[red]{Markup.Escape(line)}[/]"));
        AnsiConsole.Console.Write(new Text(Environment.NewLine));
    }
}

