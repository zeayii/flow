namespace Zeayii.Flow.Core.Abstractions;

/// <summary>
/// 描述一次需要执行的传输任务。
/// </summary>
/// <param name="taskId">任务唯一标识。</param>
/// <param name="sourcePath">源路径。</param>
/// <param name="destinationPath">目标路径。</param>
/// <param name="conflictPolicy">冲突处理策略。</param>
public sealed class TaskRequest(string taskId, string sourcePath, string destinationPath, ConflictPolicy conflictPolicy)
{
    /// <summary>
    /// 任务唯一标识。
    /// </summary>
    public string TaskId { get; } = taskId;

    /// <summary>
    /// 源路径。
    /// </summary>
    public string SourcePath { get; } = sourcePath;

    /// <summary>
    /// 目标路径。
    /// </summary>
    public string DestinationPath { get; } = destinationPath;

    /// <summary>
    /// 冲突处理策略。
    /// </summary>
    public ConflictPolicy ConflictPolicy { get; } = conflictPolicy;
}

