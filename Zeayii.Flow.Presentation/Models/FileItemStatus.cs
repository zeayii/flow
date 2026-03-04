namespace Zeayii.Flow.Presentation.Models;

/// <summary>
/// 表示目录任务中文件级条目的运行状态。
/// </summary>
public enum FileItemStatus
{
    /// <summary>
    /// 已发现但尚未开始处理。
    /// </summary>
    Pending,

    /// <summary>
    /// 正在传输。
    /// </summary>
    Running,

    /// <summary>
    /// 已完成。
    /// </summary>
    Completed,

    /// <summary>
    /// 已失败。
    /// </summary>
    Failed,

    /// <summary>
    /// 已跳过。
    /// </summary>
    Skipped,

    /// <summary>
    /// 已取消。
    /// </summary>
    Canceled
}

