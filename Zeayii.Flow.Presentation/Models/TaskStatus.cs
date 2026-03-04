namespace Zeayii.Flow.Presentation.Models;

/// <summary>
/// 表示任务在呈现层中的运行状态。
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// 等待开始执行。
    /// </summary>
    Pending,

    /// <summary>
    /// 正在扫描目录内容。
    /// </summary>
    Scanning,

    /// <summary>
    /// 正在运行。
    /// </summary>
    Running,

    /// <summary>
    /// 已完成且无失败项。
    /// </summary>
    Completed,

    /// <summary>
    /// 已完成，但过程中存在失败项。
    /// </summary>
    CompletedWithErrors,

    /// <summary>
    /// 已失败。
    /// </summary>
    Failed,

    /// <summary>
    /// 已取消。
    /// </summary>
    Canceled,

    /// <summary>
    /// 已跳过。
    /// </summary>
    Skipped
}

