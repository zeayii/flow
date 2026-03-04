using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Core.Engine;

/// <summary>
/// 提供核心传输引擎实现，按顺序编排顶层任务并归集退出码。
/// </summary>
public sealed class TaskTransferEngine
{
    /// <summary>
    /// 执行传输任务列表。
    /// </summary>
    /// <param name="tasks">任务请求列表。</param>
    /// <param name="ui">展示层管理器。</param>
    /// <param name="options">核心配置。</param>
    /// <param name="ct">全局取消令牌。</param>
    /// <returns>退出码。</returns>
    public async ValueTask<int> RunAsync(IReadOnlyList<TaskRequest> tasks, IPresentationManager ui, CoreOptions options, CancellationToken ct)
    {
        if (tasks.Count == 0)
        {
            return 0;
        }

        using var global = new GlobalContext(ui, options, ct);
        var hasFailures = false;
        global.LogInformation("Engine", $"Run started. tasks={tasks.Count}, task-concurrency={options.TaskConcurrency}, inner-concurrency={options.InnerConcurrency}, retries={options.MaxRetries}, policy={options.TaskFailurePolicy}.");

        foreach (var request in tasks)
        {
            global.CancellationToken.ThrowIfCancellationRequested();
            var descriptor = TaskDescriptorFactory.Create(request, DateTimeOffset.UtcNow);
            ui.RegisterTask(descriptor);
            ui.UpdateTaskStatus(descriptor.TaskId, TaskStatus.Pending);
            global.LogDebug("Engine", $"Task registered. id={descriptor.TaskId}, kind={descriptor.Kind}, src={request.SourcePath}, dst={request.DestinationPath}.");

            var runtime = new TaskRuntime(global, descriptor, request);
            var success = await runtime.RunAsync().ConfigureAwait(false);
            if (!success)
            {
                hasFailures = true;
                if (options.TaskFailurePolicy == TaskFailurePolicy.StopAll)
                {
                    break;
                }
            }
        }

        global.LogInformation("Engine", hasFailures ? "Run finished with failures." : "Run finished successfully.");
        return hasFailures ? 1 : 0;
    }
}
