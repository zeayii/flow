using System.Threading.Channels;
using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Core.Engine;

/// <summary>
/// 提供核心传输引擎实现，负责任务级并发编排与失败归集。
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
        var tracker = new TaskOutcomeTracker();

        var taskQueue = Channel.CreateBounded<TaskWorkItem>(new BoundedChannelOptions(GetTaskQueueCapacity(options.TaskConcurrency))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });

        foreach (var request in tasks)
        {
            var descriptor = TaskDescriptorFactory.Create(request, DateTimeOffset.UtcNow);
            ui.RegisterTask(descriptor);
            ui.UpdateTaskStatus(descriptor.TaskId, TaskStatus.Pending);
            await taskQueue.Writer.WriteAsync(new TaskWorkItem(request, descriptor), ct);
        }

        taskQueue.Writer.Complete();

        var workers = new List<Task>();
        var workerCount = Math.Max(1, options.TaskConcurrency);
        for (var index = 0; index < workerCount; index++)
        {
            workers.Add(TaskWorkerAsync(taskQueue.Reader, global, tracker));
        }

        await Task.WhenAll(workers);

        return tracker.HasFailures ? 1 : 0;
    }

    /// <summary>
    /// 计算任务队列容量。
    /// </summary>
    private static int GetTaskQueueCapacity(int taskConcurrency)
    {
        return Math.Max(64, taskConcurrency * 4);
    }

    /// <summary>
    /// 处理任务队列的工作线程。
    /// </summary>
    private async Task TaskWorkerAsync(ChannelReader<TaskWorkItem> reader, GlobalContext global, TaskOutcomeTracker tracker)
    {
        await foreach (var item in reader.ReadAllAsync(global.CancellationToken))
        {
            var runtime = new TaskRuntime(global, item.Descriptor, item.Request);
            var success = await runtime.RunAsync();
            if (!success)
            {
                tracker.MarkFailed();
            }
        }
    }

    /// <summary>
    /// 任务队列工作项。
    /// </summary>
    private sealed class TaskWorkItem
    {
        /// <summary>
        /// 初始化任务工作项。
        /// </summary>
        public TaskWorkItem(TaskRequest request, TaskDescriptor descriptor)
        {
            Request = request;
            Descriptor = descriptor;
        }

        /// <summary>
        /// 任务请求。
        /// </summary>
        public TaskRequest Request { get; }

        /// <summary>
        /// 展示描述信息。
        /// </summary>
        public TaskDescriptor Descriptor { get; }
    }

    /// <summary>
    /// 任务失败统计器。
    /// </summary>
    private sealed class TaskOutcomeTracker
    {
        /// <summary>
        /// 失败任务计数。
        /// </summary>
        private int _failed;

        /// <summary>
        /// 是否存在失败任务。
        /// </summary>
        public bool HasFailures => Volatile.Read(ref _failed) > 0;

        /// <summary>
        /// 标记一次失败。
        /// </summary>
        public void MarkFailed()
        {
            Interlocked.Increment(ref _failed);
        }
    }
}


