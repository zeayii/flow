using Spectre.Console;

namespace Zeayii.Flow.CommandLine.Default;

/// <summary>
/// 封装基于 AnsiConsole.Console 的命令行文本输出。
/// </summary>
internal static class ConsoleOutput
{
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
}

