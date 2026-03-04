namespace Zeayii.Flow.Core.Abstractions;

/// <summary>
/// 定义核心引擎的外部配置选项。
/// </summary>
public sealed class CoreOptions
{
    /// <summary>
    /// 初始化核心配置。
    /// </summary>
    /// <param name="taskConcurrency">任务并发数。</param>
    /// <param name="innerConcurrency">单任务内部并发数。</param>
    /// <param name="maxRetries">最大重试次数。</param>
    /// <param name="blockSize">分块大小（KiB）。</param>
    /// <param name="taskFailurePolicy">任务失败策略。</param>
    public CoreOptions(int taskConcurrency, int innerConcurrency, int maxRetries, int blockSize, TaskFailurePolicy taskFailurePolicy)
    {
        TaskConcurrency = taskConcurrency;
        InnerConcurrency = innerConcurrency;
        MaxRetries = maxRetries;
        BlockSize = blockSize;
        TaskFailurePolicy = taskFailurePolicy;
    }

    /// <summary>
    /// 任务并发数。
    /// </summary>
    public int TaskConcurrency { get; }

    /// <summary>
    /// 单任务内部并发数。
    /// </summary>
    public int InnerConcurrency { get; }

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// 分块大小（KiB）。
    /// </summary>
    public int BlockSize { get; }

    /// <summary>
    /// 单个文件失败后的任务处理策略。
    /// </summary>
    public TaskFailurePolicy TaskFailurePolicy { get; }
}

