namespace Zeayii.Flow.Presentation.Models;

/// <summary>
/// 描述一个需要展示的同步任务。
/// </summary>
public sealed class TaskDescriptor
{
    /// <summary>
    /// 初始化任务描述信息。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="kind">任务类型。</param>
    /// <param name="sourcePath">源路径。</param>
    /// <param name="destinationPath">目标路径。</param>
    /// <param name="displayName">显示名称。</param>
    /// <param name="createdAt">任务创建时间。</param>
    public TaskDescriptor(
        string taskId,
        TaskKind kind,
        string sourcePath,
        string destinationPath,
        string displayName,
        DateTimeOffset createdAt)
    {
        TaskId = taskId;
        Kind = kind;
        SourcePath = sourcePath;
        DestinationPath = destinationPath;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// 任务唯一标识。
    /// </summary>
    public string TaskId { get; }

    /// <summary>
    /// 任务类型。
    /// </summary>
    public TaskKind Kind { get; }

    /// <summary>
    /// 源路径。
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// 目标路径。
    /// </summary>
    public string DestinationPath { get; }

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// 任务创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; }
}

