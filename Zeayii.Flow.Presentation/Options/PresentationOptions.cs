using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Presentation.Options;

/// <summary>
/// 呈现层渲染行为与容量限制的配置项。
/// </summary>
public sealed class PresentationOptions
{
    /// <summary>
    /// 标题栏中展示的失败策略文本。
    /// </summary>
    public required string HeaderFailurePolicy { get; init; }

    /// <summary>
    /// 标题栏中展示的任务并发数。
    /// </summary>
    public required int HeaderTaskConcurrency { get; init; }

    /// <summary>
    /// 标题栏中展示的内部并发数。
    /// </summary>
    public required int HeaderInnerConcurrency { get; init; }

    /// <summary>
    /// 标题栏中展示的重试次数。
    /// </summary>
    public required int HeaderRetryAttempts { get; init; }

    /// <summary>
    /// 标题栏中展示的块大小文本。
    /// </summary>
    public required string HeaderBlockSizeText { get; init; }

    /// <summary>
    /// 标题栏中展示的冲突策略文本。
    /// </summary>
    public required string HeaderConflictPolicy { get; init; }

    /// <summary>
    /// 标题栏中展示的运行模式文本。
    /// </summary>
    public string? HeaderRunModeText { get; init; }

    /// <summary>
    /// 界面刷新时间间隔。
    /// </summary>
    public required TimeSpan RefreshInterval { get; init; }

    /// <summary>
    /// 内存中保留的日志条目上限。
    /// </summary>
    public required int MaxLogEntries { get; init; }

    /// <summary>
    /// 呈现层日志等级。
    /// </summary>
    public required PresentationLogLevel TuiLogLevel { get; init; }


    /// <summary>
    /// 文件日志输出目录（为空表示禁用文件日志）。
    /// </summary>
    public string? LogDirectory { get; init; }

    /// <summary>
    /// 文件日志等级。
    /// </summary>
    public required PresentationLogLevel FileLogLevel { get; init; }

    /// <summary>
    /// 单任务保留的失败条目上限。
    /// </summary>
    public required int MaxFailuresPerTask { get; init; }

    /// <summary>
    /// 目录任务中记录的最近完成文件数量上限。
    /// </summary>
    public required int MaxRecentCompletedFilesPerFolderTask { get; init; }

    /// <summary>
    /// 内存中保留的任务数量上限。
    /// </summary>
    public required int MaxTasksKept { get; init; }

    /// <summary>
    /// 当终端高度不可用时的默认页大小。
    /// </summary>
    public required int DefaultPageSize { get; init; }

    /// <summary>
    /// 详情视图中展示的失败条目数量。
    /// </summary>
    public required int VisibleFailuresInDetail { get; init; }

    /// <summary>
    /// 详情视图中展示的最近完成文件数量。
    /// </summary>
    public required int VisibleRecentCompletedFilesInDetail { get; init; }
}

