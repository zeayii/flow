namespace Zeayii.Flow.Core.Abstractions;

/// <summary>
/// 定义单个文件失败后顶层任务的处理策略。
/// </summary>
public enum TaskFailurePolicy
{
    /// <summary>
    /// 继续处理当前任务中剩余的文件。
    /// </summary>
    Continue,

    /// <summary>
    /// 立即停止当前任务，但不影响其他顶层任务。
    /// </summary>
    StopCurrentTask,

    /// <summary>
    /// 立即停止所有任务。
    /// </summary>
    StopAll
}

