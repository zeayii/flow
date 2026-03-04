using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Core.Engine.Contexts;

/// <summary>
/// 表示一个顶层任务执行期间的上下文。
/// </summary>
/// <param name="global">全局上下文。</param>
/// <param name="descriptor">任务描述信息。</param>
/// <param name="conflictPolicy">冲突处理策略。</param>
/// <param name="maxRetries">最大重试次数。</param>
/// <param name="innerConcurrency">内部并发数。</param>
/// <param name="state">运行状态。</param>
internal sealed class TaskExecutionContext(
    GlobalContext global,
    TaskDescriptor descriptor,
    ConflictPolicy conflictPolicy,
    int maxRetries,
    int innerConcurrency,
    TaskRuntimeState state
) : IDisposable
{
    /// <summary>
    /// 任务级取消令牌源。
    /// 说明：由全局上下文派生，保证“全局可中断、任务可独立取消”。
    /// </summary>
    private readonly CancellationTokenSource _taskCancellationSource = global.CreateTaskCancellationSource();

    /// <summary>
    /// 标记是否由任务失败触发了任务级取消。
    /// </summary>
    private int _isCanceledByFailure;


    /// <summary>
    /// 全局上下文。
    /// </summary>
    public GlobalContext Global { get; } = global;

    /// <summary>
    /// 任务描述信息。
    /// </summary>
    public TaskDescriptor Descriptor { get; } = descriptor;

    /// <summary>
    /// 冲突处理策略。
    /// </summary>
    public ConflictPolicy ConflictPolicy { get; } = conflictPolicy;

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int MaxRetries { get; } = maxRetries;

    /// <summary>
    /// 内部并发数。
    /// </summary>
    public int InnerConcurrency { get; } = innerConcurrency;

    /// <summary>
    /// 任务级取消令牌。
    /// </summary>
    public CancellationToken TaskCancellationToken => _taskCancellationSource.Token;

    /// <summary>
    /// 创建文件级取消令牌源。
    /// 说明：文件级令牌由任务级令牌派生，保证取消只能从任务向文件级联。
    /// </summary>
    /// <returns>文件级取消令牌源。</returns>
    public CancellationTokenSource CreateFileCancellationSource()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(TaskCancellationToken);
    }

    /// <summary>
    /// 是否由任务失败触发取消。
    /// </summary>
    public bool IsCanceledByFailure => Volatile.Read(ref _isCanceledByFailure) == 1;

    /// <summary>
    /// 运行状态。
    /// </summary>
    public TaskRuntimeState State { get; } = state;

    /// <summary>
    /// 仅取消当前任务，不影响其他任务。
    /// 通常在判定任务已失败时调用，用于尽快停止当前任务内部并发分支。
    /// </summary>
    public void CancelTaskByFailure()
    {
        Interlocked.Exchange(ref _isCanceledByFailure, 1);
        if (!_taskCancellationSource.IsCancellationRequested)
        {
            _taskCancellationSource.Cancel();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _taskCancellationSource.Dispose();
    }
}


