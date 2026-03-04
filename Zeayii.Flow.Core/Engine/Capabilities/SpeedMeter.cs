using System.Diagnostics;

namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 基于滑动窗口的速度计算实现。
/// </summary>
internal sealed class SpeedMeter : ISpeedMeter
{
    /// <summary>
    /// 首次接收到字节样本的时间戳。
    /// </summary>
    private long _startedAtTicks;

    /// <summary>
    /// 已接收的累计字节数。
    /// </summary>
    private long _totalBytes;

    /// <summary>
    /// 累加字节数并更新窗口。
    /// </summary>
    /// <param name="bytes">新增字节数。</param>
    public void AddBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _startedAtTicks) == 0)
        {
            Interlocked.CompareExchange(ref _startedAtTicks, Stopwatch.GetTimestamp(), 0);
        }

        Interlocked.Add(ref _totalBytes, bytes);
    }

    /// <summary>
    /// 获取每秒字节数。
    /// </summary>
    /// <returns>速度值。</returns>
    public double GetBytesPerSecond()
    {
        var startedAtTicks = Volatile.Read(ref _startedAtTicks);
        if (startedAtTicks == 0)
        {
            return 0;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - startedAtTicks;
        if (elapsedTicks <= 0)
        {
            return 0;
        }

        var elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;
        if (elapsedSeconds <= 0)
        {
            return 0;
        }

        var totalBytes = Interlocked.Read(ref _totalBytes);
        return totalBytes / elapsedSeconds;
    }
}

