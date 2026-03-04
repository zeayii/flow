using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 表示任务视图模型。
/// </summary>
internal sealed class TaskViewModel
{
    /// <summary>
    /// 初始化任务视图模型。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    public TaskViewModel(TaskDescriptor descriptor)
    {
        Descriptor = descriptor;
        UpdatedAt = descriptor.CreatedAt;
    }

    /// <summary>
    /// 任务描述信息。
    /// </summary>
    public TaskDescriptor Descriptor { get; }

    /// <summary>
    /// 任务状态。
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 任务状态消息。
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// 任务错误摘要。
    /// </summary>
    public string? ErrorSummary { get; set; }

    /// <summary>
    /// 任务已传输字节数。
    /// </summary>
    public long TransferredBytes { get; set; }

    /// <summary>
    /// 任务总字节数。
    /// </summary>
    public long? TotalBytes { get; set; }

    /// <summary>
    /// 任务速度。
    /// </summary>
    public double BytesPerSecond { get; set; }

    /// <summary>
    /// 任务开始执行时间。
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// 已完成文件数。
    /// </summary>
    public int FilesDone { get; set; }

    /// <summary>
    /// 总文件数。
    /// </summary>
    public int FilesTotal { get; set; }

    /// <summary>
    /// 失败文件数。
    /// </summary>
    public int FailedFiles { get; set; }

    /// <summary>
    /// 任务完成时间。
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// 任务更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// 文件集合。
    /// </summary>
    public Dictionary<string, FileViewModel> Files { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// 表示文件视图模型。
/// </summary>
internal sealed class FileViewModel
{
    /// <summary>
    /// 初始化文件视图模型。
    /// </summary>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="totalBytes">文件总字节数。</param>
    public FileViewModel(string relativePath, long totalBytes)
    {
        RelativePath = relativePath;
        TotalBytes = totalBytes;
    }

    /// <summary>
    /// 文件相对路径。
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    /// 文件状态。
    /// </summary>
    public FileItemStatus Status { get; set; } = FileItemStatus.Pending;

    /// <summary>
    /// 文件总字节数。
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 已传输字节数。
    /// </summary>
    public long TransferredBytes { get; set; }

    /// <summary>
    /// 文件实时速度。
    /// </summary>
    public double BytesPerSecond { get; set; }

    /// <summary>
    /// 错误分类。
    /// </summary>
    public string? ErrorCategory { get; set; }

    /// <summary>
    /// 附加消息。
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 尝试次数。
    /// </summary>
    public int Attempt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}


