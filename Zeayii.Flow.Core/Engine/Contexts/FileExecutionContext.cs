using Zeayii.Flow.Core.Engine.Capabilities;

namespace Zeayii.Flow.Core.Engine.Contexts;

/// <summary>
/// 表示单个文件执行期间的上下文。
/// 说明：该上下文从任务级取消令牌派生，只负责当前文件，不允许反向控制任务或全局。
/// </summary>
/// <param name="taskContext">所属任务执行上下文。</param>
/// <param name="workItem">文件工作项。</param>
internal sealed class FileExecutionContext(TaskExecutionContext taskContext, FileTransferWorkItem workItem) : IDisposable
{
    /// <summary>
    /// 文件级取消令牌源。
    /// 说明：与任务级令牌关联，任务取消时会级联取消当前文件。
    /// </summary>
    private readonly CancellationTokenSource _fileCancellationSource = taskContext.CreateFileCancellationSource();

    /// <summary>
    /// 所属任务执行上下文。
    /// </summary>
    public TaskExecutionContext TaskContext { get; } = taskContext;

    /// <summary>
    /// 当前文件工作项。
    /// </summary>
    public FileTransferWorkItem WorkItem { get; } = workItem;

    /// <summary>
    /// 全局上下文。
    /// </summary>
    public GlobalContext Global => TaskContext.Global;

    /// <summary>
    /// 文件级取消令牌。
    /// </summary>
    public CancellationToken FileCancellationToken => _fileCancellationSource.Token;

    /// <summary>
    /// 主动取消当前文件。
    /// 说明：仅影响当前文件，不影响所属任务和其他文件。
    /// </summary>
    public void CancelFile()
    {
        if (!_fileCancellationSource.IsCancellationRequested)
        {
            _fileCancellationSource.Cancel();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _fileCancellationSource.Dispose();
    }
}

