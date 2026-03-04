using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Capabilities;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Tests;

/// <summary>
/// 校验取消语义分层关系的测试集合。
/// </summary>
public sealed class CancellationSemanticsTests
{
    /// <summary>
    /// 验证任务级取消会向下传播到文件级上下文。
    /// </summary>
    [Fact]
    public void TaskCancellation_ShouldFlowDownToFileContext()
    {
        using var globalCancellationSource = new CancellationTokenSource();
        using var globalContext = new GlobalContext(new FakePresentationManager(), new CoreOptions(1, 1, 1, 256, TaskFailurePolicy.Continue), globalCancellationSource.Token);
        using var taskContext = new TaskExecutionContext(
            globalContext,
            new TaskDescriptor("task-1", TaskKind.File, "src", "dst", "task-1", DateTimeOffset.UtcNow),
            ConflictPolicy.Resume,
            1,
            1,
            new TaskRuntimeState(new SpeedMeter()));
        using var fileContext = new FileExecutionContext(taskContext, new FileTransferWorkItem("src", "dst", "file-1"));

        taskContext.CancelTaskByFailure();

        Assert.True(taskContext.TaskCancellationToken.IsCancellationRequested);
        Assert.True(fileContext.FileCancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// 验证文件级取消不会反向传播到任务级上下文。
    /// </summary>
    [Fact]
    public void FileCancellation_ShouldNotPropagateUpward()
    {
        using var globalCancellationSource = new CancellationTokenSource();
        using var globalContext = new GlobalContext(new FakePresentationManager(), new CoreOptions(1, 1, 1, 256, TaskFailurePolicy.Continue), globalCancellationSource.Token);
        using var taskContext = new TaskExecutionContext(
            globalContext,
            new TaskDescriptor("task-2", TaskKind.File, "src", "dst", "task-2", DateTimeOffset.UtcNow),
            ConflictPolicy.Resume,
            1,
            1,
            new TaskRuntimeState(new SpeedMeter()));
        using var fileContext = new FileExecutionContext(taskContext, new FileTransferWorkItem("src", "dst", "file-2"));

        fileContext.CancelFile();

        Assert.True(fileContext.FileCancellationToken.IsCancellationRequested);
        Assert.False(taskContext.TaskCancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// 提供测试用的空实现展示层。
    /// </summary>
    private sealed class FakePresentationManager : IPresentationManager
    {
        /// <inheritdoc />
        public ValueTask StartAsync(CancellationToken ct) => ValueTask.CompletedTask;

        /// <inheritdoc />
        public ValueTask StopAsync() => ValueTask.CompletedTask;

        /// <inheritdoc />
        public void RegisterTask(TaskDescriptor descriptor)
        {
        }

        /// <inheritdoc />
        public void UpdateTaskStatus(string taskId, TaskStatus status, string? message = null)
        {
        }

        /// <inheritdoc />
        public void ReportTaskProgress(string taskId, long transferredBytes, long? totalBytes)
        {
        }

        /// <inheritdoc />
        public void ReportTaskSpeed(string taskId, double bytesPerSecond)
        {
        }

        /// <inheritdoc />
        public void ReportTaskCompleted(string taskId)
        {
        }

        /// <inheritdoc />
        public void ReportTaskFailed(string taskId, string errorSummary)
        {
        }

        /// <inheritdoc />
        public void ReportFolderCounters(string taskId, int filesDone, int filesTotal, int failedFiles)
        {
        }

        /// <inheritdoc />
        public void RegisterFile(string taskId, string relativePath, long fileBytes)
        {
        }

        /// <inheritdoc />
        public void UpdateFileStatus(string taskId, string relativePath, FileItemStatus status, string? message = null)
        {
        }

        /// <inheritdoc />
        public void ReportFileProgress(string taskId, string relativePath, long transferredBytes, long totalBytes, double bytesPerSecond)
        {
        }

        /// <inheritdoc />
        public void ReportFileCompleted(string taskId, string relativePath, long fileBytes)
        {
        }

        /// <inheritdoc />
        public void ReportFileFailed(string taskId, string relativePath, string errorCategory, string message, int attempt)
        {
        }

        /// <inheritdoc />
        public void ReportFileSkipped(string taskId, string relativePath, string reason)
        {
        }
    }
}


