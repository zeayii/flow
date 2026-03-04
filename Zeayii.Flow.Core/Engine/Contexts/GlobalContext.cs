using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Capabilities;
using Zeayii.Flow.Presentation.Abstractions;

namespace Zeayii.Flow.Core.Engine.Contexts;

/// <summary>
/// 表示一次运行的全局上下文，负责共享配置、能力组件与全局取消语义。
/// </summary>
/// <param name="ui">展示层管理器。</param>
/// <param name="options">核心配置。</param>
/// <param name="cancellationToken">宿主取消令牌。</param>
internal sealed class GlobalContext(IPresentationManager ui, CoreOptions options, CancellationToken cancellationToken) : IDisposable
{
    /// <summary>
    /// 全局取消令牌源。
    /// 说明：该令牌源会与宿主传入的取消令牌关联，支持统一终止所有任务。
    /// </summary>
    private readonly CancellationTokenSource _globalCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);


    /// <summary>
    /// 展示层管理器。
    /// </summary>
    public IPresentationManager Ui { get; } = ui;

    /// <summary>
    /// 核心配置。
    /// </summary>
    public CoreOptions Options { get; } = options;

    /// <summary>
    /// 全局取消令牌。
    /// </summary>
    public CancellationToken CancellationToken => _globalCancellationSource.Token;

    /// <summary>
    /// 路径策略实现。
    /// </summary>
    public IPathPolicy PathPolicy { get; } = new PathPolicy();

    /// <summary>
    /// 重试策略实现。
    /// </summary>
    public IRetryPolicy RetryPolicy { get; } = new RetryPolicy();

    /// <summary>
    /// 文件传输能力实现。
    /// </summary>
    public IFileTransferCapability FileTransferCapability { get; } = new FileTransferCapability();

    /// <summary>
    /// 创建任务级取消令牌源。
    /// 说明：任务令牌与全局令牌关联；全局取消会级联到所有任务。
    /// </summary>
    /// <returns>任务级取消令牌源。</returns>
    public CancellationTokenSource CreateTaskCancellationSource()
    {
        return CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
    }

    /// <summary>
    /// 触发全局取消，终止所有任务。
    /// </summary>
    public void CancelAll()
    {
        if (!_globalCancellationSource.IsCancellationRequested)
        {
            _globalCancellationSource.Cancel();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _globalCancellationSource.Dispose();
    }
}

