using Zeayii.Flow.Presentation.Implementations;
using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Tests;

/// <summary>
/// 校验日志环形缓冲区行为的测试集合。
/// </summary>
public sealed class LogBufferTests
{
    /// <summary>
    /// 验证缓冲区满后会保留最新的日志窗口。
    /// </summary>
    [Fact]
    public void Add_ShouldKeepNewestEntriesWhenCapacityIsReached()
    {
        var buffer = new LogBuffer();
        buffer.SetCapacity(3);

        buffer.Add(new LogEntry(DateTimeOffset.Parse("2026-03-04T00:00:00+08:00"), PresentationLogLevel.Information, "one", null));
        buffer.Add(new LogEntry(DateTimeOffset.Parse("2026-03-04T00:00:01+08:00"), PresentationLogLevel.Information, "two", null));
        buffer.Add(new LogEntry(DateTimeOffset.Parse("2026-03-04T00:00:02+08:00"), PresentationLogLevel.Information, "three", null));
        buffer.Add(new LogEntry(DateTimeOffset.Parse("2026-03-04T00:00:03+08:00"), PresentationLogLevel.Information, "four", null));

        var window = buffer.CopyWindow(0, 3);

        Assert.Equal(3, buffer.Count);
        Assert.Equal(["two", "three", "four"], window.Select(entry => entry.Message).ToArray());
    }
}


