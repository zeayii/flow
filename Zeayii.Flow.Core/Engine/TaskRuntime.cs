using System.Threading.Channels;
using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Capabilities;
using Zeayii.Flow.Core.Engine.Contexts;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Core.Engine;

/// <summary>
/// 表示单个顶层传输任务的运行时执行单元。
/// </summary>
internal sealed class TaskRuntime
{
    /// <summary>
    /// 任务级进度上报间隔。
    /// </summary>
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// 任务级速度上报间隔。
    /// </summary>
    private static readonly TimeSpan SpeedReportInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 全局上下文。
    /// </summary>
    private readonly GlobalContext _global;

    /// <summary>
    /// 任务展示描述信息。
    /// </summary>
    private readonly TaskDescriptor _descriptor;

    /// <summary>
    /// 原始任务请求。
    /// </summary>
    private readonly TaskRequest _request;

    /// <summary>
    /// 初始化任务运行时实例。
    /// </summary>
    /// <param name="global">全局上下文。</param>
    /// <param name="descriptor">任务描述信息。</param>
    /// <param name="request">任务请求。</param>
    public TaskRuntime(GlobalContext global, TaskDescriptor descriptor, TaskRequest request)
    {
        _global = global;
        _descriptor = descriptor;
        _request = request;
    }

    /// <summary>
    /// 执行当前任务。
    /// </summary>
    /// <returns>是否成功。</returns>
    public async Task<bool> RunAsync()
    {
        using var context = new TaskExecutionContext(
            _global,
            _descriptor,
            _request.ConflictPolicy,
            _global.Options.MaxRetries,
            _global.Options.InnerConcurrency,
            new TaskRuntimeState(new SpeedMeter()));

        try
        {
            if (File.Exists(_request.SourcePath))
            {
                return await ExecuteFileTaskAsync(context).ConfigureAwait(false);
            }

            if (Directory.Exists(_request.SourcePath))
            {
                return await ExecuteDirectoryTaskAsync(context).ConfigureAwait(false);
            }

            _global.Ui.UpdateTaskStatus(_descriptor.TaskId, TaskStatus.Failed, "Source not found.");
            _global.Ui.ReportTaskFailed(_descriptor.TaskId, "Source not found.");
            return false;
        }
        catch (OperationCanceledException) when (context.IsCanceledByFailure)
        {
            var message = "Task canceled by failure policy.";
            _global.Ui.ReportTaskFailed(_descriptor.TaskId, message);
            _global.Ui.UpdateTaskStatus(_descriptor.TaskId, TaskStatus.Failed, message);
            return false;
        }
        catch (OperationCanceledException)
        {
            var message = "Canceled.";
            _global.Ui.UpdateTaskStatus(_descriptor.TaskId, TaskStatus.Canceled, message);
            return false;
        }
    }

    /// <summary>
    /// 执行单文件传输任务。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <returns>是否成功。</returns>
    private async Task<bool> ExecuteFileTaskAsync(TaskExecutionContext executionContext)
    {
        var taskId = executionContext.Descriptor.TaskId;
        var progress = new ProgressSink(executionContext.Global.Ui, taskId, executionContext.State, ProgressInterval, SpeedReportInterval);
        var bufferSize = Math.Max(4 * 1024, executionContext.Global.Options.BlockSize * 1024);
        var relativePath = ResolveSingleFileRelativePath();
        var fileLength = new FileInfo(_request.SourcePath).Length;
        var workItem = new FileTransferWorkItem(_request.SourcePath, _request.DestinationPath, relativePath);

        executionContext.State.IncrementFilesTotal();
        executionContext.Global.Ui.RegisterFile(taskId, relativePath, fileLength);
        executionContext.Global.Ui.UpdateFileStatus(taskId, relativePath, FileItemStatus.Pending);
        executionContext.Global.Ui.ReportFolderCounters(taskId, executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Running);

        using var fileContext = new FileExecutionContext(executionContext, workItem);
        var fileProgress = new FileProgressSink(fileContext, relativePath, fileLength, ProgressInterval, SpeedReportInterval);

        FileTransferOutcome result;
        try
        {
            result = await executionContext.Global.FileTransferCapability
                .CopyOneFileAsync(fileContext, progress, fileProgress, bufferSize, setTotalBytes: true)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (fileContext.FileCancellationToken.IsCancellationRequested)
        {
            progress.ForceReport();
            fileProgress.ForceReport(fileContext);
            MarkFileCanceled(fileContext, ResolveCancellationMessage(executionContext));
            throw;
        }

        progress.ForceReport();
        fileProgress.ForceReport(fileContext);

        if (result.Success)
        {
            executionContext.State.IncrementFilesDone();
            executionContext.Global.Ui.ReportFolderCounters(taskId, executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
            if (result.AlreadyCompleted)
            {
                executionContext.Global.Ui.ReportFileSkipped(taskId, workItem.RelativePath, "Already up to date.");
                executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Skipped, "Already up to date.");
            }
            else
            {
                executionContext.Global.Ui.ReportFileCompleted(taskId, workItem.RelativePath, result.Bytes);
                executionContext.Global.Ui.ReportTaskCompleted(taskId);
                executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Completed);
            }

            return true;
        }

        executionContext.State.IncrementFailedFiles();
        executionContext.Global.Ui.ReportFolderCounters(taskId, executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
        executionContext.Global.Ui.ReportFileFailed(taskId, workItem.RelativePath, result.ErrorCategory ?? "Unknown", result.ErrorMessage ?? "Failed", result.Attempts);
        executionContext.Global.Ui.ReportTaskFailed(taskId, result.ErrorMessage ?? "Failed");
        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Failed, result.ErrorMessage);
        ApplyFailurePolicy(executionContext);
        return false;
    }

    /// <summary>
    /// 执行目录传输任务。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <returns>是否成功。</returns>
    private async Task<bool> ExecuteDirectoryTaskAsync(TaskExecutionContext executionContext)
    {
        var taskId = executionContext.Descriptor.TaskId;
        var progress = new ProgressSink(executionContext.Global.Ui, taskId, executionContext.State, ProgressInterval, SpeedReportInterval);
        var bufferSize = Math.Max(4 * 1024, executionContext.Global.Options.BlockSize * 1024);

        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Scanning);

        var fileQueue = Channel.CreateBounded<FileTransferWorkItem>(new BoundedChannelOptions(GetFileQueueCapacity(executionContext.InnerConcurrency))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });

        var resultQueue = Channel.CreateBounded<FileResultEvent>(new BoundedChannelOptions(GetResultQueueCapacity(executionContext.InnerConcurrency))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

        var workers = new List<Task>(executionContext.InnerConcurrency);
        for (var index = 0; index < executionContext.InnerConcurrency; index++)
        {
            workers.Add(FileWorkerAsync(executionContext, fileQueue.Reader, resultQueue.Writer, progress, bufferSize));
        }

        var aggregator = AggregateDirectoryResultsAsync(executionContext, resultQueue.Reader, progress);

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(_request.SourcePath, "*", SearchOption.AllDirectories))
            {
                executionContext.TaskCancellationToken.ThrowIfCancellationRequested();

                var relativePath = executionContext.Global.PathPolicy.GetRelativePath(_request.SourcePath, filePath);
                var destinationPath = executionContext.Global.PathPolicy.CombineDestination(_request.DestinationPath, relativePath);
                var fileLength = new FileInfo(filePath).Length;

                executionContext.Global.Ui.RegisterFile(taskId, relativePath, fileLength);
                executionContext.Global.Ui.UpdateFileStatus(taskId, relativePath, FileItemStatus.Pending);
                await resultQueue.Writer.WriteAsync(FileResultEvent.Discovered(relativePath, fileLength), executionContext.TaskCancellationToken).ConfigureAwait(false);
                await fileQueue.Writer.WriteAsync(new FileTransferWorkItem(filePath, destinationPath, relativePath), executionContext.TaskCancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            fileQueue.Writer.TryComplete();
        }

        try
        {
            await Task.WhenAll(workers).ConfigureAwait(false);
        }
        finally
        {
            resultQueue.Writer.TryComplete();
        }

        var aggregateResult = await aggregator.ConfigureAwait(false);
        progress.ForceReport();

        if (aggregateResult.WasCanceled)
        {
            throw new OperationCanceledException(executionContext.TaskCancellationToken);
        }

        if (aggregateResult.HasFailures && executionContext.Global.Options.TaskFailurePolicy != TaskFailurePolicy.Continue)
        {
            executionContext.Global.Ui.ReportTaskFailed(taskId, aggregateResult.Message);
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Failed, aggregateResult.Message);
            return false;
        }

        if (aggregateResult.HasFailures)
        {
            executionContext.Global.Ui.ReportTaskFailed(taskId, aggregateResult.Message);
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.CompletedWithErrors, aggregateResult.Message);
            return true;
        }

        if (aggregateResult.AllSkipped)
        {
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Skipped, "Already up to date.");
            return true;
        }

        executionContext.Global.Ui.ReportTaskCompleted(taskId);
        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Completed);
        return true;
    }

    /// <summary>
    /// 解析单文件任务的展示相对路径。
    /// </summary>
    /// <returns>用于展示的相对路径。</returns>
    private string ResolveSingleFileRelativePath()
    {
        var relativePath = Path.GetFileName(_request.SourcePath);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return _request.SourcePath;
        }

        return relativePath;
    }

    /// <summary>
    /// 目录任务中的文件工作线程。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <param name="reader">文件工作项读取器。</param>
    /// <param name="resultWriter">结果写入器。</param>
    /// <param name="progress">任务级进度汇报器。</param>
    /// <param name="bufferSize">文件缓冲区大小。</param>
    private async Task FileWorkerAsync(
        TaskExecutionContext executionContext,
        ChannelReader<FileTransferWorkItem> reader,
        ChannelWriter<FileResultEvent> resultWriter,
        IProgressSink progress,
        int bufferSize)
    {
        try
        {
            await foreach (var workItem in reader.ReadAllAsync(executionContext.TaskCancellationToken).ConfigureAwait(false))
            {
                await WriteFileResultAsync(resultWriter, FileResultEvent.Started(workItem.RelativePath)).ConfigureAwait(false);

                using var fileContext = new FileExecutionContext(executionContext, workItem);
                var sourceLength = new FileInfo(workItem.SourcePath).Length;
                var fileProgress = new FileProgressSink(fileContext, workItem.RelativePath, sourceLength, ProgressInterval, SpeedReportInterval);

                try
                {
                    var result = await executionContext.Global.FileTransferCapability
                        .CopyOneFileAsync(fileContext, progress, fileProgress, bufferSize, setTotalBytes: false)
                        .ConfigureAwait(false);

                    fileProgress.ForceReport(fileContext);

                    var resultEvent = result.Success
                        ? result.AlreadyCompleted
                            ? FileResultEvent.Skipped(workItem.RelativePath, result.Bytes, "Already up to date.")
                            : FileResultEvent.Completed(workItem.RelativePath, result.Bytes)
                        : FileResultEvent.Failed(workItem.RelativePath, result.ErrorCategory ?? "Unknown", result.ErrorMessage ?? "Failed", result.Attempts);

                    await WriteFileResultAsync(resultWriter, resultEvent).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (fileContext.FileCancellationToken.IsCancellationRequested)
                {
                    fileProgress.ForceReport(fileContext);
                    await WriteFileResultAsync(resultWriter, FileResultEvent.Canceled(workItem.RelativePath, ResolveCancellationMessage(executionContext))).ConfigureAwait(false);

                    if (executionContext.TaskCancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (executionContext.TaskCancellationToken.IsCancellationRequested)
        {
            // 说明：任务级取消时，工作线程应自然结束，不再向上包装为失败。
        }
    }

    /// <summary>
    /// 聚合目录任务中的文件结果。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <param name="reader">结果读取器。</param>
    /// <param name="progress">任务级进度汇报器。</param>
    /// <returns>目录聚合结果。</returns>
    private async Task<DirectoryAggregateResult> AggregateDirectoryResultsAsync(
        TaskExecutionContext executionContext,
        ChannelReader<FileResultEvent> reader,
        ProgressSink progress)
    {
        var hasRunningFile = false;
        var discoveredFiles = 0;
        var skippedFiles = 0;
        var discoveredRelativePaths = new HashSet<string>(StringComparer.Ordinal);
        var finalizedRelativePaths = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var resultEvent in reader.ReadAllAsync().ConfigureAwait(false))
        {
            switch (resultEvent.Kind)
            {
                case FileResultKind.Discovered:
                    discoveredFiles++;
                    discoveredRelativePaths.Add(resultEvent.RelativePath);
                    executionContext.State.IncrementFilesTotal();
                    progress.SetTotalBytes(executionContext.State.TotalBytes + resultEvent.Bytes);
                    progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
                    break;

                case FileResultKind.Started:
                    if (!hasRunningFile)
                    {
                        hasRunningFile = true;
                        executionContext.Global.Ui.UpdateTaskStatus(executionContext.Descriptor.TaskId, TaskStatus.Running);
                    }

                    break;

                case FileResultKind.Completed:
                    finalizedRelativePaths.Add(resultEvent.RelativePath);
                    executionContext.State.IncrementFilesDone();
                    progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
                    executionContext.Global.Ui.ReportFileCompleted(executionContext.Descriptor.TaskId, resultEvent.RelativePath, resultEvent.Bytes);
                    break;

                case FileResultKind.Skipped:
                    finalizedRelativePaths.Add(resultEvent.RelativePath);
                    skippedFiles++;
                    executionContext.State.IncrementFilesDone();
                    progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
                    executionContext.Global.Ui.ReportFileSkipped(executionContext.Descriptor.TaskId, resultEvent.RelativePath, resultEvent.ErrorMessage ?? "Already up to date.");
                    break;

                case FileResultKind.Failed:
                    finalizedRelativePaths.Add(resultEvent.RelativePath);
                    executionContext.State.IncrementFailedFiles();
                    progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
                    executionContext.Global.Ui.ReportFileFailed(
                        executionContext.Descriptor.TaskId,
                        resultEvent.RelativePath,
                        resultEvent.ErrorCategory ?? "Unknown",
                        resultEvent.ErrorMessage ?? "Failed",
                        resultEvent.Attempts);
                    ApplyFailurePolicy(executionContext);
                    break;

                case FileResultKind.Canceled:
                    finalizedRelativePaths.Add(resultEvent.RelativePath);
                    executionContext.Global.Ui.UpdateFileStatus(executionContext.Descriptor.TaskId, resultEvent.RelativePath, FileItemStatus.Canceled, resultEvent.ErrorMessage);
                    break;
            }
        }

        if (executionContext.TaskCancellationToken.IsCancellationRequested)
        {
            foreach (var relativePath in discoveredRelativePaths)
            {
                if (finalizedRelativePaths.Contains(relativePath))
                {
                    continue;
                }

                executionContext.Global.Ui.UpdateFileStatus(executionContext.Descriptor.TaskId, relativePath, FileItemStatus.Canceled, ResolveCancellationMessage(executionContext));
            }

            return new DirectoryAggregateResult(hasFailures: false, message: ResolveCancellationMessage(executionContext), allSkipped: false, wasCanceled: true);
        }

        if (executionContext.State.FailedFiles > 0)
        {
            return new DirectoryAggregateResult(true, executionContext.Global.Options.TaskFailurePolicy == TaskFailurePolicy.Continue
                ? "Completed with file failures."
                : "Task stopped by failure policy.");
        }

        return new DirectoryAggregateResult(false, string.Empty, discoveredFiles > 0 && skippedFiles == discoveredFiles, wasCanceled: false);
    }

    /// <summary>
    /// 写入文件结果事件。
    /// 说明：结果通道收尾阶段不能再绑定任务级取消令牌，否则会丢失取消终态。
    /// </summary>
    /// <param name="writer">结果写入器。</param>
    /// <param name="resultEvent">文件结果事件。</param>
    private static async Task WriteFileResultAsync(ChannelWriter<FileResultEvent> writer, FileResultEvent resultEvent)
    {
        await writer.WriteAsync(resultEvent).ConfigureAwait(false);
    }

    /// <summary>
    /// 标记单文件已取消。
    /// </summary>
    /// <param name="fileContext">文件执行上下文。</param>
    /// <param name="message">取消说明。</param>
    private static void MarkFileCanceled(FileExecutionContext fileContext, string message)
    {
        fileContext.Global.Ui.UpdateFileStatus(fileContext.TaskContext.Descriptor.TaskId, fileContext.WorkItem.RelativePath, FileItemStatus.Canceled, message);
    }

    /// <summary>
    /// 解析当前取消消息。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <returns>取消消息。</returns>
    private static string ResolveCancellationMessage(TaskExecutionContext executionContext)
    {
        return executionContext.IsCanceledByFailure ? "Canceled by task failure policy." : "Canceled.";
    }

    /// <summary>
    /// 根据失败策略应用取消行为。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    private static void ApplyFailurePolicy(TaskExecutionContext executionContext)
    {
        switch (executionContext.Global.Options.TaskFailurePolicy)
        {
            case TaskFailurePolicy.StopCurrentTask:
                executionContext.CancelTaskByFailure();
                break;
            case TaskFailurePolicy.StopAll:
                executionContext.CancelTaskByFailure();
                executionContext.Global.CancelAll();
                break;
        }
    }

    /// <summary>
    /// 计算文件队列容量。
    /// </summary>
    /// <param name="innerConcurrency">任务内部并发数。</param>
    /// <returns>队列容量。</returns>
    private static int GetFileQueueCapacity(int innerConcurrency)
    {
        return Math.Max(256, innerConcurrency * 16);
    }

    /// <summary>
    /// 计算结果队列容量。
    /// </summary>
    /// <param name="innerConcurrency">任务内部并发数。</param>
    /// <returns>队列容量。</returns>
    private static int GetResultQueueCapacity(int innerConcurrency)
    {
        return Math.Max(256, innerConcurrency * 32);
    }

    /// <summary>
    /// 表示目录任务中的文件结果事件。
    /// </summary>
    private sealed class FileResultEvent
    {
        /// <summary>
        /// 初始化文件结果事件。
        /// </summary>
        /// <param name="kind">事件类型。</param>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="bytes">文件字节数。</param>
        /// <param name="errorCategory">错误分类。</param>
        /// <param name="errorMessage">错误消息。</param>
        /// <param name="attempts">尝试次数。</param>
        private FileResultEvent(FileResultKind kind, string relativePath, long bytes, string? errorCategory, string? errorMessage, int attempts)
        {
            Kind = kind;
            RelativePath = relativePath;
            Bytes = bytes;
            ErrorCategory = errorCategory;
            ErrorMessage = errorMessage;
            Attempts = attempts;
        }

        /// <summary>
        /// 事件类型。
        /// </summary>
        public FileResultKind Kind { get; }

        /// <summary>
        /// 文件相对路径。
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// 文件字节数。
        /// </summary>
        public long Bytes { get; }

        /// <summary>
        /// 错误分类。
        /// </summary>
        public string? ErrorCategory { get; }

        /// <summary>
        /// 错误消息。
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// 尝试次数。
        /// </summary>
        public int Attempts { get; }

        /// <summary>
        /// 创建发现事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="bytes">文件字节数。</param>
        /// <returns>发现事件。</returns>
        public static FileResultEvent Discovered(string relativePath, long bytes)
        {
            return new FileResultEvent(FileResultKind.Discovered, relativePath, bytes, null, null, 0);
        }

        /// <summary>
        /// 创建开始事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <returns>开始事件。</returns>
        public static FileResultEvent Started(string relativePath)
        {
            return new FileResultEvent(FileResultKind.Started, relativePath, 0, null, null, 0);
        }

        /// <summary>
        /// 创建完成事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="bytes">文件字节数。</param>
        /// <returns>完成事件。</returns>
        public static FileResultEvent Completed(string relativePath, long bytes)
        {
            return new FileResultEvent(FileResultKind.Completed, relativePath, bytes, null, null, 0);
        }

        /// <summary>
        /// 创建跳过事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="bytes">文件字节数。</param>
        /// <param name="message">跳过原因。</param>
        /// <returns>跳过事件。</returns>
        public static FileResultEvent Skipped(string relativePath, long bytes, string message)
        {
            return new FileResultEvent(FileResultKind.Skipped, relativePath, bytes, null, message, 0);
        }

        /// <summary>
        /// 创建失败事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="category">错误分类。</param>
        /// <param name="message">错误消息。</param>
        /// <param name="attempts">尝试次数。</param>
        /// <returns>失败事件。</returns>
        public static FileResultEvent Failed(string relativePath, string category, string message, int attempts)
        {
            return new FileResultEvent(FileResultKind.Failed, relativePath, 0, category, message, attempts);
        }

        /// <summary>
        /// 创建取消事件。
        /// </summary>
        /// <param name="relativePath">文件相对路径。</param>
        /// <param name="message">取消消息。</param>
        /// <returns>取消事件。</returns>
        public static FileResultEvent Canceled(string relativePath, string message)
        {
            return new FileResultEvent(FileResultKind.Canceled, relativePath, 0, null, message, 0);
        }
    }

    /// <summary>
    /// 表示文件结果事件类型。
    /// </summary>
    private enum FileResultKind
    {
        /// <summary>
        /// 已发现文件。
        /// </summary>
        Discovered,

        /// <summary>
        /// 文件开始处理。
        /// </summary>
        Started,

        /// <summary>
        /// 文件完成。
        /// </summary>
        Completed,

        /// <summary>
        /// 文件跳过。
        /// </summary>
        Skipped,

        /// <summary>
        /// 文件失败。
        /// </summary>
        Failed,

        /// <summary>
        /// 文件取消。
        /// </summary>
        Canceled
    }

    /// <summary>
    /// 表示目录任务聚合结果。
    /// </summary>
    private sealed class DirectoryAggregateResult
    {
        /// <summary>
        /// 初始化目录聚合结果。
        /// </summary>
        /// <param name="hasFailures">是否存在失败。</param>
        /// <param name="message">结果说明。</param>
        /// <param name="allSkipped">是否全部跳过。</param>
        /// <param name="wasCanceled">是否已取消。</param>
        public DirectoryAggregateResult(bool hasFailures, string message, bool allSkipped = false, bool wasCanceled = false)
        {
            HasFailures = hasFailures;
            Message = message;
            AllSkipped = allSkipped;
            WasCanceled = wasCanceled;
        }

        /// <summary>
        /// 是否存在失败。
        /// </summary>
        public bool HasFailures { get; }

        /// <summary>
        /// 结果说明。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 是否所有文件都被跳过。
        /// </summary>
        public bool AllSkipped { get; }

        /// <summary>
        /// 是否已取消。
        /// </summary>
        public bool WasCanceled { get; }
    }
}


