using System.Text;
using Zeayii.Flow.Presentation.Models;
using Spectre.Console;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 提供 Spectre Dashboard 的文本渲染辅助方法。
/// </summary>
internal static class RenderText
{
    /// <summary>
    /// 格式化任务状态标签。
    /// </summary>
    /// <param name="status">任务状态。</param>
    /// <returns>状态标签。</returns>
    public static string FormatTaskStatusLabel(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Pending => "PEND",
            TaskStatus.Scanning => "SCAN",
            TaskStatus.Running => "RUN",
            TaskStatus.Completed => "DONE",
            TaskStatus.CompletedWithErrors => "CERR",
            TaskStatus.Failed => "FAIL",
            TaskStatus.Canceled => "CANC",
            TaskStatus.Skipped => "SKIP",
            _ => "UNKN"
        };
    }

    /// <summary>
    /// 格式化文件状态标签。
    /// </summary>
    /// <param name="status">文件状态。</param>
    /// <returns>状态标签。</returns>
    public static string FormatFileStatusLabel(FileItemStatus status)
    {
        return status switch
        {
            FileItemStatus.Pending => "Pending",
            FileItemStatus.Running => "Running",
            FileItemStatus.Completed => "Done",
            FileItemStatus.Failed => "Failed",
            FileItemStatus.Skipped => "Skipped",
            FileItemStatus.Canceled => "Canceled",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 获取任务状态颜色名称。
    /// </summary>
    /// <param name="status">任务状态。</param>
    /// <returns>颜色名称。</returns>
    public static Color GetTaskStatusColor(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Pending => PresentationPalette.Warning,
            TaskStatus.Scanning => Color.Khaki1,
            TaskStatus.Running => PresentationPalette.Accent,
            TaskStatus.Completed => PresentationPalette.Success,
            TaskStatus.CompletedWithErrors => Color.Orange1,
            TaskStatus.Failed => PresentationPalette.Failure,
            TaskStatus.Canceled => PresentationPalette.Muted,
            TaskStatus.Skipped => PresentationPalette.Skipped,
            _ => PresentationPalette.Info
        };
    }

    /// <summary>
    /// 获取文件状态颜色名称。
    /// </summary>
    /// <param name="status">文件状态。</param>
    /// <returns>颜色名称。</returns>
    public static Color GetFileStatusColor(FileItemStatus status)
    {
        return status switch
        {
            FileItemStatus.Pending => PresentationPalette.Warning,
            FileItemStatus.Running => PresentationPalette.Accent,
            FileItemStatus.Completed => PresentationPalette.Success,
            FileItemStatus.Failed => PresentationPalette.Failure,
            FileItemStatus.Skipped => PresentationPalette.Skipped,
            FileItemStatus.Canceled => PresentationPalette.Muted,
            _ => PresentationPalette.Info
        };
    }

    /// <summary>
    /// 获取日志等级标签。
    /// </summary>
    /// <param name="level">日志等级。</param>
    /// <returns>日志标签。</returns>
    public static string GetLogLevelTag(PresentationLogLevel level)
    {
        return level switch
        {
            PresentationLogLevel.Trace => "TRC",
            PresentationLogLevel.Debug => "DBG",
            PresentationLogLevel.Information => "INF",
            PresentationLogLevel.Warning => "WRN",
            PresentationLogLevel.Error => "ERR",
            PresentationLogLevel.Critical => "CRT",
            _ => "NON"
        };
    }

    /// <summary>
    /// 获取日志等级颜色名称。
    /// </summary>
    /// <param name="level">日志等级。</param>
    /// <returns>颜色名称。</returns>
    public static Color GetLogLevelColor(PresentationLogLevel level)
    {
        return level switch
        {
            PresentationLogLevel.Trace => PresentationPalette.Muted,
            PresentationLogLevel.Debug => PresentationPalette.Accent,
            PresentationLogLevel.Information => PresentationPalette.Success,
            PresentationLogLevel.Warning => PresentationPalette.Warning,
            PresentationLogLevel.Error => PresentationPalette.Failure,
            PresentationLogLevel.Critical => PresentationPalette.Failure,
            _ => PresentationPalette.Info
        };
    }

    /// <summary>
    /// 对文本应用颜色。
    /// </summary>
    /// <param name="plainText">纯文本。</param>
    /// <param name="color">颜色。</param>
    /// <returns>带样式文本。</returns>
    public static string ColorizePlain(string plainText, Color color)
    {
        return $"[{color}]{Markup.Escape(plainText)}[/]";
    }

    /// <summary>
    /// 构建文本进度条。
    /// </summary>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    /// <param name="width">进度条宽度。</param>
    /// <returns>文本进度条。</returns>
    public static string BuildProgressBar(long transferredBytes, long? totalBytes, int width, Color fillColor)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var barChar = AnsiConsole.Profile.Capabilities.Unicode ? '━' : '-';
        if (!totalBytes.HasValue || totalBytes.Value <= 0)
        {
            return GetFixedWidthMarkup($"[{PresentationPalette.Muted}]{new string(barChar, width)}[/]", width);
        }

        var ratio = Math.Clamp(transferredBytes / (double)totalBytes.Value, 0d, 1d);
        var filled = Math.Clamp((int)Math.Round(width * ratio, MidpointRounding.AwayFromZero), 0, width);
        var remaining = width - filled;
        var filledText = filled > 0 ? new string(barChar, filled) : string.Empty;
        var remainingText = remaining > 0 ? new string(barChar, remaining) : string.Empty;
        return $"[{fillColor}]{Markup.Escape(filledText)}[/][{PresentationPalette.Muted}]{Markup.Escape(remainingText)}[/]";
    }

    /// <summary>
    /// 格式化字节数。
    /// </summary>
    /// <param name="bytes">字节数。</param>
    /// <returns>格式化文本。</returns>
    public static string FormatBytes(long bytes)
    {
        const double kilo = 1024d;
        const double mega = kilo * 1024d;
        const double giga = mega * 1024d;
        const double tera = giga * 1024d;

        return bytes switch
        {
            >= (long)tera => $"{bytes / tera:0.0}TB",
            >= (long)giga => $"{bytes / giga:0.0}GB",
            >= (long)mega => $"{bytes / mega:0.0}MB",
            >= (long)kilo => $"{bytes / kilo:0.0}KB",
            _ => $"{bytes}B"
        };
    }

    /// <summary>
    /// 格式化速度文本。
    /// </summary>
    /// <param name="bytesPerSecond">每秒字节数。</param>
    /// <returns>格式化文本。</returns>
    public static string FormatBytesPerSecond(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
        {
            return "-";
        }

        const double kilo = 1024d;
        const double mega = kilo * 1024d;
        const double giga = mega * 1024d;
        const double tera = giga * 1024d;

        return bytesPerSecond switch
        {
            >= tera => $"{bytesPerSecond / tera:0.0}TB/s",
            >= giga => $"{bytesPerSecond / giga:0.0}GB/s",
            >= mega => $"{bytesPerSecond / mega:0.0}MB/s",
            >= kilo => $"{bytesPerSecond / kilo:0.0}KB/s",
            _ => $"{bytesPerSecond:0.0}B/s"
        };
    }

    /// <summary>
    /// 生成固定宽度的左对齐文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="width">目标宽度。</param>
    /// <returns>固定宽度文本。</returns>
    public static string PadRightPlain(string text, int width)
    {
        return PadPlain(text, width, alignLeft: true);
    }

    /// <summary>
    /// 生成固定宽度的右对齐文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="width">目标宽度。</param>
    /// <returns>固定宽度文本。</returns>
    public static string PadLeftPlain(string text, int width)
    {
        return PadPlain(text, width, alignLeft: false);
    }

    /// <summary>
    /// 截断并填充字符串。
    /// </summary>
    /// <param name="text">原始字符串。</param>
    /// <param name="width">目标宽度。</param>
    /// <returns>固定宽度字符串。</returns>
    public static string TruncateAndPad(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var cellLength = GetDisplayWidth(text);
        if (cellLength < width)
        {
            return text + new string(' ', width - cellLength);
        }

        if (cellLength == width)
        {
            return text;
        }

        if (width == 1)
        {
            return "…";
        }

        return TruncateToDisplayWidth(text, width - 1) + "…";
    }

    /// <summary>
    /// 将数字格式化为固定宽度的摘要字段。
    /// </summary>
    /// <param name="value">数字值。</param>
    /// <param name="width">字段宽度。</param>
    /// <returns>固定宽度摘要字段。</returns>
    public static string FormatBoundedCount(long value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var rawText = value.ToString();
        if (GetDisplayWidth(rawText) <= width)
        {
            return PadLeftPlain(rawText, width);
        }

        if (width == 1)
        {
            return "+";
        }

        return new string('9', width - 1) + "+";
    }

    /// <summary>
    /// 将速率格式化为固定宽度字段。
    /// </summary>
    /// <param name="bytesPerSecond">字节每秒速率。</param>
    /// <param name="width">字段宽度。</param>
    /// <returns>固定宽度速率字段。</returns>
    public static string FormatFixedSpeed(double bytesPerSecond, int width)
    {
        return PadLeftPlain(TruncateAndPad(FormatBytesPerSecond(bytesPerSecond), width), width);
    }

    /// <summary>
    /// 将任务耗时格式化为固定宽度文本。
    /// </summary>
    /// <param name="startedAt">开始时间。</param>
    /// <param name="completedAt">完成时间。</param>
    /// <param name="now">当前时间。</param>
    /// <returns>耗时文本。</returns>
    public static string FormatElapsed(DateTimeOffset? startedAt, DateTimeOffset? completedAt, DateTimeOffset now)
    {
        if (!startedAt.HasValue)
        {
            return "--:--:--";
        }

        var end = completedAt ?? now;
        var elapsed = end - startedAt.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var totalHours = (int)Math.Min(99, Math.Floor(elapsed.TotalHours));
        return $"{totalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    /// <summary>
    /// 将文本包装为固定显示宽度的标记字符串。
    /// </summary>
    /// <param name="markup">标记文本。</param>
    /// <param name="width">目标宽度。</param>
    /// <returns>固定宽度的标记文本。</returns>
    public static string GetFixedWidthMarkup(string markup, int width)
    {
        var plain = Markup.Remove(markup);
        if (GetDisplayWidth(plain) > width)
        {
            return Markup.Escape(TruncateAndPad(plain, width));
        }

        var displayWidth = GetDisplayWidth(plain);
        if (displayWidth >= width)
        {
            return markup;
        }

        return markup + new string(' ', width - displayWidth);
    }

    /// <summary>
    /// 计算字符串在终端中的显示宽度。
    /// </summary>
    /// <param name="text">目标字符串。</param>
    /// <returns>显示宽度。</returns>
    public static int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var width = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            width += GetRuneDisplayWidth(rune);
        }

        return width;
    }

    /// <summary>
    /// 截断字符串到指定显示宽度。
    /// </summary>
    /// <param name="text">原始字符串。</param>
    /// <param name="targetWidth">目标宽度。</param>
    /// <returns>截断后的字符串。</returns>
    private static string TruncateToDisplayWidth(string text, int targetWidth)
    {
        if (targetWidth <= 0 || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var width = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var runeWidth = GetRuneDisplayWidth(rune);
            if (width + runeWidth > targetWidth)
            {
                break;
            }

            builder.Append(rune.ToString());
            width += runeWidth;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 生成固定显示宽度的文本。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="width">目标宽度。</param>
    /// <param name="alignLeft">是否左对齐。</param>
    /// <returns>处理后的文本。</returns>
    private static string PadPlain(string text, int width, bool alignLeft)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var normalized = TruncateToDisplayWidth(text, width);
        var displayWidth = GetDisplayWidth(normalized);
        if (displayWidth >= width)
        {
            return normalized;
        }

        var padding = new string(' ', width - displayWidth);
        return alignLeft ? normalized + padding : padding + normalized;
    }

    /// <summary>
    /// 获取单个 Unicode 字符的显示宽度。
    /// </summary>
    /// <param name="rune">目标字符。</param>
    /// <returns>显示宽度。</returns>
    private static int GetRuneDisplayWidth(Rune rune)
    {
        if (Rune.IsControl(rune))
        {
            return 0;
        }

        var value = rune.Value;
        return value switch
        {
            >= 0x1100 and <= 0x115F => 2,
            >= 0x2329 and <= 0x232A => 2,
            >= 0x2E80 and <= 0xA4CF => 2,
            >= 0xAC00 and <= 0xD7A3 => 2,
            >= 0xF900 and <= 0xFAFF => 2,
            >= 0xFE10 and <= 0xFE19 => 2,
            >= 0xFE30 and <= 0xFE6F => 2,
            >= 0xFF00 and <= 0xFF60 => 2,
            >= 0xFFE0 and <= 0xFFE6 => 2,
            >= 0x1F300 and <= 0x1FAFF => 2,
            _ => 1
        };
    }
}


