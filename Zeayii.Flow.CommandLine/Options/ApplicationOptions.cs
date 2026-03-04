using Zeayii.Flow.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Zeayii.Flow.CommandLine.Options;

/// <summary>
/// 描述命令行解析得到的应用配置。
/// </summary>
internal sealed class ApplicationOptions
{
    /// <summary>
    /// 初始化应用配置。
    /// </summary>
    /// <param name="planPath">计划文件路径。</param>
    /// <param name="concurrency">任务并发数。</param>
    /// <param name="innerConcurrency">内部并发数。</param>
    /// <param name="retryAttempts">最大重试次数。</param>
    /// <param name="blockSize">分块大小（KiB）。</param>
    /// <param name="taskFailurePolicy">任务失败策略。</param>
    /// <param name="conflictPolicy">冲突处理策略。</param>
    /// <param name="dryRun">是否启用空跑模式。</param>
    /// <param name="enableTui">是否启用 UI。</param>
    /// <param name="uiRefreshInterval">UI刷新间隔。</param>
    /// <param name="uiMaxLogEntries">日志条目上限。</param>
    /// <param name="uiLogLevel">UI日志等级。</param>
    /// <param name="logDirectory">日志输出目录。</param>
    /// <param name="fileLogLevel">文件日志等级。</param>
    /// <param name="uiMaxFailuresPerTask">单任务保留的失败条目上限。</param>
    /// <param name="uiMaxRecentDonePerFolderTask">目录任务中记录的最近完成文件数量上限。</param>
    /// <param name="uiMaxTasksKept">内存中保留的任务数量上限。</param>
    /// <param name="uiDefaultPageSize">终端高度不可用时的默认页大小。</param>
    /// <param name="uiVisibleFailuresInDetail">详情视图中展示的失败条目数量。</param>
    /// <param name="uiVisibleRecentDoneInDetail">详情视图中展示的最近完成文件数量。</param>
    public ApplicationOptions(
        FileInfo planPath,
        int concurrency,
        int innerConcurrency,
        int retryAttempts,
        int blockSize,
        TaskFailurePolicy taskFailurePolicy,
        ConflictPolicy conflictPolicy,
        bool dryRun,
        bool enableTui,
        TimeSpan uiRefreshInterval,
        int uiMaxLogEntries,
        LogLevel uiLogLevel,
        DirectoryInfo logDirectory,
        LogLevel fileLogLevel,
        int uiMaxFailuresPerTask,
        int uiMaxRecentDonePerFolderTask,
        int uiMaxTasksKept,
        int uiDefaultPageSize,
        int uiVisibleFailuresInDetail,
        int uiVisibleRecentDoneInDetail)
    {
        PlanPath = planPath;
        Concurrency = concurrency;
        InnerConcurrency = innerConcurrency;
        RetryAttempts = retryAttempts;
        BlockSize = blockSize;
        TaskFailurePolicy = taskFailurePolicy;
        ConflictPolicy = conflictPolicy;
        DryRun = dryRun;
        EnableTui = enableTui;
        UiRefreshInterval = uiRefreshInterval;
        UiMaxLogEntries = uiMaxLogEntries;
        UiLogLevel = uiLogLevel;
        LogDirectory = logDirectory;
        FileLogLevel = fileLogLevel;
        UiMaxFailuresPerTask = uiMaxFailuresPerTask;
        UiMaxRecentDonePerFolderTask = uiMaxRecentDonePerFolderTask;
        UiMaxTasksKept = uiMaxTasksKept;
        UiDefaultPageSize = uiDefaultPageSize;
        UiVisibleFailuresInDetail = uiVisibleFailuresInDetail;
        UiVisibleRecentDoneInDetail = uiVisibleRecentDoneInDetail;
    }

    /// <summary>
    /// 计划文件路径。
    /// </summary>
    public FileInfo PlanPath { get; }

    /// <summary>
    /// 任务并发数。
    /// </summary>
    public int Concurrency { get; }

    /// <summary>
    /// 内部并发数。
    /// </summary>
    public int InnerConcurrency { get; }

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int RetryAttempts { get; }

    /// <summary>
    /// 分块大小（KiB）。
    /// </summary>
    public int BlockSize { get; }

    /// <summary>
    /// 任务失败策略。
    /// </summary>
    public TaskFailurePolicy TaskFailurePolicy { get; }

    /// <summary>
    /// 冲突处理策略。
    /// </summary>
    public ConflictPolicy ConflictPolicy { get; }

    /// <summary>
    /// 是否启用空跑模式。
    /// </summary>
    public bool DryRun { get; }

    /// <summary>
    /// 是否启用 UI。
    /// </summary>
    public bool EnableTui { get; }

    /// <summary>
    /// UI刷新间隔。
    /// </summary>
    public TimeSpan UiRefreshInterval { get; }

    /// <summary>
    /// 日志条目上限。
    /// </summary>
    public int UiMaxLogEntries { get; }

    /// <summary>
    /// UI日志等级。
    /// </summary>
    public LogLevel UiLogLevel { get; }

    /// <summary>
    /// 日志输出目录。
    /// </summary>
    public DirectoryInfo LogDirectory { get; }

    /// <summary>
    /// 文件日志等级。
    /// </summary>
    public LogLevel FileLogLevel { get; }

    /// <summary>
    /// 单任务保留的失败条目上限。
    /// </summary>
    public int UiMaxFailuresPerTask { get; }

    /// <summary>
    /// 目录任务中记录的最近完成文件数量上限。
    /// </summary>
    public int UiMaxRecentDonePerFolderTask { get; }

    /// <summary>
    /// 内存中保留的任务数量上限。
    /// </summary>
    public int UiMaxTasksKept { get; }

    /// <summary>
    /// 当终端高度不可用时的默认页大小。
    /// </summary>
    public int UiDefaultPageSize { get; }

    /// <summary>
    /// 详情视图中展示的失败条目数量。
    /// </summary>
    public int UiVisibleFailuresInDetail { get; }

    /// <summary>
    /// 详情视图中展示的最近完成文件数量。
    /// </summary>
    public int UiVisibleRecentDoneInDetail { get; }
}

