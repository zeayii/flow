using System.Diagnostics;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 负责限频上报单文件进度与速度的汇报器。
/// </summary>
internal sealed class FileProgressSink
{
    /// <summary>
    /// 文件所属任务标识。
    /// </summary>
    private readonly string _taskId;

    /// <summary>
    /// 文件相对路径。
    /// </summary>
    private readonly string _relativePath;

    /// <summary>
    /// 文件总字节数。
    /// </summary>
    private readonly long _totalBytes;

    /// <summary>
    /// 进度上报间隔。
    /// </summary>
    private readonly long _progressReportIntervalTicks;

    /// <summary>
    /// 速度上报间隔。
    /// </summary>
    private readonly long _speedReportIntervalTicks;

    /// <summary>
    /// 文件已传输字节数。
    /// </summary>
    private long _transferredBytes;

    /// <summary>
    /// 文件首字节传输时间戳。
    /// </summary>
    private long _startedAtTicks;

    /// <summary>
    /// 上次进度上报时间戳。
    /// </summary>
    private long _lastProgressReportTicks;

    /// <summary>
    /// 上次速度上报时间戳。
    /// </summary>
    private long _lastSpeedReportTicks;

    /// <summary>
    /// 最近一次对外上报的速度值。
    /// </summary>
    private double _lastReportedBytesPerSecond;

    /// <summary>
    /// 初始化文件进度汇报器。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="totalBytes">文件总字节数。</param>
    /// <param name="progressReportInterval">进度上报间隔。</param>
    /// <param name="speedReportInterval">速度上报间隔。</param>
    public FileProgressSink(FileExecutionContext executionContext, string relativePath, long totalBytes, TimeSpan progressReportInterval, TimeSpan speedReportInterval)
    {
        _taskId = executionContext.TaskContext.Descriptor.TaskId;
        _relativePath = relativePath;
        _totalBytes = totalBytes;
        _progressReportIntervalTicks = Math.Max(1, (long)(progressReportInterval.TotalSeconds * Stopwatch.Frequency));
        _speedReportIntervalTicks = Math.Max(1, (long)(speedReportInterval.TotalSeconds * Stopwatch.Frequency));
        executionContext.Global.Ui.UpdateFileStatus(_taskId, _relativePath, FileItemStatus.Running);
        executionContext.Global.Ui.ReportFileProgress(_taskId, _relativePath, 0, _totalBytes, 0);
    }

    /// <summary>
    /// 累加文件已传输字节并尝试上报。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    /// <param name="bytes">本次新增字节数。</param>
    public void AddBytes(FileExecutionContext executionContext, long bytes)
    {
        if (bytes <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _startedAtTicks) == 0)
        {
            Interlocked.CompareExchange(ref _startedAtTicks, Stopwatch.GetTimestamp(), 0);
        }

        Interlocked.Add(ref _transferredBytes, bytes);
        TryReport(executionContext);
    }

    /// <summary>
    /// 将文件进度直接推进到指定总量。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    /// <param name="targetBytes">目标累计字节数。</param>
    public void SetTransferred(FileExecutionContext executionContext, long targetBytes)
    {
        var current = Interlocked.Read(ref _transferredBytes);
        if (targetBytes <= current)
        {
            return;
        }

        AddBytes(executionContext, targetBytes - current);
    }

    /// <summary>
    /// 强制上报一次最终进度。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    public void ForceReport(FileExecutionContext executionContext)
    {
        executionContext.Global.Ui.ReportFileProgress(_taskId, _relativePath, Interlocked.Read(ref _transferredBytes), _totalBytes, GetBytesPerSecond());
    }

    /// <summary>
    /// 尝试在限频条件下上报文件进度。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    private void TryReport(FileExecutionContext executionContext)
    {
        while (true)
        {
            var nowTicks = Stopwatch.GetTimestamp();
            var snapshot = Volatile.Read(ref _lastProgressReportTicks);
            if (snapshot != 0 && nowTicks - snapshot < _progressReportIntervalTicks)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _lastProgressReportTicks, nowTicks, snapshot) != snapshot)
            {
                continue;
            }

            executionContext.Global.Ui.ReportFileProgress(_taskId, _relativePath, Interlocked.Read(ref _transferredBytes), _totalBytes, GetReportedBytesPerSecond(nowTicks));
            return;
        }
    }

    /// <summary>
    /// 获取当前对外可见的速度值，并按固定频率更新。
    /// </summary>
    /// <param name="nowTicks">当前计时器刻度。</param>
    /// <returns>对外展示的速度值。</returns>
    private double GetReportedBytesPerSecond(long nowTicks)
    {
        while (true)
        {
            var snapshot = Volatile.Read(ref _lastSpeedReportTicks);
            if (snapshot != 0 && nowTicks - snapshot < _speedReportIntervalTicks)
            {
                return Volatile.Read(ref _lastReportedBytesPerSecond);
            }

            if (Interlocked.CompareExchange(ref _lastSpeedReportTicks, nowTicks, snapshot) != snapshot)
            {
                continue;
            }

            var bytesPerSecond = GetBytesPerSecond();
            Volatile.Write(ref _lastReportedBytesPerSecond, bytesPerSecond);
            return bytesPerSecond;
        }
    }

    /// <summary>
    /// 计算当前文件平均速度。
    /// </summary>
    /// <returns>当前字节每秒速率。</returns>
    private double GetBytesPerSecond()
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

        return Interlocked.Read(ref _transferredBytes) / elapsedSeconds;
    }
}

