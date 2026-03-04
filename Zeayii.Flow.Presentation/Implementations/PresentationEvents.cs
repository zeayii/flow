using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 表示呈现层事件基类。
/// </summary>
internal abstract record PresentationEvent;

/// <summary>
/// 表示任务注册事件。
/// </summary>
/// <param name="Descriptor">任务描述信息。</param>
internal sealed record RegisterTaskEvent(TaskDescriptor Descriptor) : PresentationEvent;

/// <summary>
/// 表示任务状态更新事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="Status">任务状态。</param>
/// <param name="Message">状态消息。</param>
internal sealed record UpdateTaskStatusEvent(string TaskId, TaskStatus Status, string? Message) : PresentationEvent;

/// <summary>
/// 表示任务进度事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="TransferredBytes">已传输字节数。</param>
/// <param name="TotalBytes">总字节数。</param>
internal sealed record TaskProgressEvent(string TaskId, long TransferredBytes, long? TotalBytes) : PresentationEvent;

/// <summary>
/// 表示任务速度事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="BytesPerSecond">每秒字节数。</param>
internal sealed record TaskSpeedEvent(string TaskId, double BytesPerSecond) : PresentationEvent;

/// <summary>
/// 表示任务完成事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
internal sealed record TaskCompletedEvent(string TaskId) : PresentationEvent;

/// <summary>
/// 表示任务失败事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="ErrorSummary">错误摘要。</param>
internal sealed record TaskFailedEvent(string TaskId, string ErrorSummary) : PresentationEvent;

/// <summary>
/// 表示目录计数事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="FilesDone">已完成文件数。</param>
/// <param name="FilesTotal">总文件数。</param>
/// <param name="FailedFiles">失败文件数。</param>
internal sealed record FolderCountersEvent(string TaskId, int FilesDone, int FilesTotal, int FailedFiles) : PresentationEvent;

/// <summary>
/// 表示文件注册事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="FileBytes">文件总字节数。</param>
internal sealed record RegisterFileEvent(string TaskId, string RelativePath, long FileBytes) : PresentationEvent;

/// <summary>
/// 表示文件状态更新事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="Status">文件状态。</param>
/// <param name="Message">附加消息。</param>
internal sealed record UpdateFileStatusEvent(string TaskId, string RelativePath, FileItemStatus Status, string? Message) : PresentationEvent;

/// <summary>
/// 表示文件进度事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="TransferredBytes">已传输字节数。</param>
/// <param name="TotalBytes">总字节数。</param>
/// <param name="BytesPerSecond">实时速度。</param>
internal sealed record FileProgressEvent(string TaskId, string RelativePath, long TransferredBytes, long TotalBytes, double BytesPerSecond) : PresentationEvent;

/// <summary>
/// 表示文件完成事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="FileBytes">文件总字节数。</param>
internal sealed record FileCompletedEvent(string TaskId, string RelativePath, long FileBytes) : PresentationEvent;

/// <summary>
/// 表示文件失败事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="ErrorCategory">错误分类。</param>
/// <param name="Message">错误消息。</param>
/// <param name="Attempt">尝试次数。</param>
internal sealed record FileFailedEvent(string TaskId, string RelativePath, string ErrorCategory, string Message, int Attempt) : PresentationEvent;

/// <summary>
/// 表示文件跳过事件。
/// </summary>
/// <param name="TaskId">任务标识。</param>
/// <param name="RelativePath">文件相对路径。</param>
/// <param name="Reason">跳过原因。</param>
internal sealed record FileSkippedEvent(string TaskId, string RelativePath, string Reason) : PresentationEvent;


