using System.Diagnostics;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Abstractions;

namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 负责限频调用 IPresentationManager 的进度汇报实现。
/// </summary>
internal sealed class ProgressSink : IProgressSink
{
    /// <summary>
    /// 展示层管理器。
    /// </summary>
    private readonly IPresentationManager _ui;

    /// <summary>
    /// 任务标识。
    /// </summary>
    private readonly string _taskId;

    /// <summary>
    /// 任务运行状态。
    /// </summary>
    private readonly TaskRuntimeState _state;

    /// <summary>
    /// 进度上报间隔对应的计时器刻度。
    /// </summary>
    private readonly long _progressReportIntervalTicks;

    /// <summary>
    /// 速度上报间隔对应的计时器刻度。
    /// </summary>
    private readonly long _speedReportIntervalTicks;

    /// <summary>
    /// 上次进度上报时间戳。
    /// </summary>
    private long _lastProgressReportTicks;

    /// <summary>
    /// 上次速度上报时间戳。
    /// </summary>
    private long _lastSpeedReportTicks;

    /// <summary>
    /// 上次目录计数上报时间戳。
    /// </summary>
    private long _lastFolderReportTicks;

    /// <summary>
    /// 初始化进度汇报器。
    /// </summary>
    /// <param name="ui">展示层管理器。</param>
    /// <param name="taskId">任务标识。</param>
    /// <param name="state">任务运行状态。</param>
    /// <param name="progressReportInterval">进度上报间隔。</param>
    /// <param name="speedReportInterval">速度上报间隔。</param>
    public ProgressSink(IPresentationManager ui, string taskId, TaskRuntimeState state, TimeSpan progressReportInterval, TimeSpan speedReportInterval)
    {
        _ui = ui;
        _taskId = taskId;
        _state = state;
        _progressReportIntervalTicks = (long)(progressReportInterval.TotalSeconds * Stopwatch.Frequency);
        _speedReportIntervalTicks = (long)(speedReportInterval.TotalSeconds * Stopwatch.Frequency);
    }

    /// <summary>
    /// 设置总字节数并尝试上报进度。
    /// </summary>
    /// <param name="totalBytes">总字节数。</param>
    public void SetTotalBytes(long totalBytes)
    {
        _state.SetTotalBytes(totalBytes);
        TryReportProgress();
    }

    /// <summary>
    /// 增加字节数并尝试上报进度与速度。
    /// </summary>
    /// <param name="bytes">新增字节数。</param>
    public void AddBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return;
        }

        _state.AddBytes(bytes);
        _state.SpeedMeter.AddBytes(bytes);
        TryReportProgress();
        TryReportSpeed();
    }

    /// <summary>
    /// 上报目录计数器。
    /// </summary>
    /// <param name="filesDone">已完成文件数。</param>
    /// <param name="filesTotal">总文件数。</param>
    /// <param name="failedFiles">失败文件数。</param>
    public void ReportFolderCounters(int filesDone, int filesTotal, int failedFiles)
    {
        if (!ShouldReport(ref _lastFolderReportTicks, _progressReportIntervalTicks))
        {
            return;
        }

        _ui.ReportFolderCounters(_taskId, filesDone, filesTotal, failedFiles);
    }

    /// <summary>
    /// 强制上报所有进度信息。
    /// </summary>
    public void ForceReport()
    {
        var totalBytes = _state.TotalBytes;
        _ui.ReportTaskProgress(_taskId, _state.TransferredBytes, totalBytes > 0 ? totalBytes : null);
        _ui.ReportTaskSpeed(_taskId, _state.SpeedMeter.GetBytesPerSecond());
        _ui.ReportFolderCounters(_taskId, _state.FilesDone, _state.FilesTotal, _state.FailedFiles);
    }

    /// <summary>
    /// 尝试上报进度。
    /// </summary>
    private void TryReportProgress()
    {
        if (!ShouldReport(ref _lastProgressReportTicks, _progressReportIntervalTicks))
        {
            return;
        }

        var totalBytes = _state.TotalBytes;
        _ui.ReportTaskProgress(_taskId, _state.TransferredBytes, totalBytes > 0 ? totalBytes : null);
    }

    /// <summary>
    /// 尝试上报速度。
    /// </summary>
    private void TryReportSpeed()
    {
        if (!ShouldReport(ref _lastSpeedReportTicks, _speedReportIntervalTicks))
        {
            return;
        }

        _ui.ReportTaskSpeed(_taskId, _state.SpeedMeter.GetBytesPerSecond());
    }

    /// <summary>
    /// 判断是否满足上报间隔要求。
    /// </summary>
    /// <param name="lastReportTicks">上次上报时间戳。</param>
    /// <param name="intervalTicks">目标上报间隔。</param>
    /// <returns>是否允许上报。</returns>
    private static bool ShouldReport(ref long lastReportTicks, long intervalTicks)
    {
        while (true)
        {
            var nowTicks = Stopwatch.GetTimestamp();
            var snapshot = Volatile.Read(ref lastReportTicks);
            if (snapshot != 0 && nowTicks - snapshot < intervalTicks)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref lastReportTicks, nowTicks, snapshot) == snapshot)
            {
                return true;
            }
        }
    }
}

