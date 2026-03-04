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
        _global.LogInformation(_descriptor.DisplayName, $"Task started. id={_descriptor.TaskId}, kind={_descriptor.Kind}.");

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

            _global.LogError(_descriptor.DisplayName, $"Source not found. src={_request.SourcePath}");
            _global.Ui.UpdateTaskStatus(_descriptor.TaskId, TaskStatus.Failed, "Source not found.");
            _global.Ui.ReportTaskFailed(_descriptor.TaskId, "Source not found.");
            return false;
        }
        catch (OperationCanceledException) when (context.IsCanceledByFailure)
        {
            var message = "Task canceled by failure policy.";
            _global.LogWarning(_descriptor.DisplayName, message);
            _global.Ui.ReportTaskFailed(_descriptor.TaskId, message);
            _global.Ui.UpdateTaskStatus(_descriptor.TaskId, TaskStatus.Failed, message);
            return false;
        }
        catch (OperationCanceledException)
        {
            var message = "Canceled.";
            _global.LogWarning(_descriptor.DisplayName, message);
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
        executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, $"File transfer started. path={relativePath}, bytes={fileLength}.");

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
                executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, $"File skipped. path={workItem.RelativePath}, reason=already-up-to-date.");
                executionContext.Global.Ui.ReportFileSkipped(taskId, workItem.RelativePath, "Already up to date.");
                executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Skipped, "Already up to date.");
            }
            else
            {
                executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, $"File completed. path={workItem.RelativePath}, bytes={result.Bytes}.");
                executionContext.Global.Ui.ReportFileCompleted(taskId, workItem.RelativePath, result.Bytes);
                executionContext.Global.Ui.ReportTaskCompleted(taskId);
                executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Completed);
            }

            return true;
        }

        executionContext.State.IncrementFailedFiles();
        executionContext.Global.Ui.ReportFolderCounters(taskId, executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
        executionContext.Global.LogError(executionContext.Descriptor.DisplayName, $"File failed. path={workItem.RelativePath}, category={result.ErrorCategory ?? "Unknown"}, attempts={result.Attempts}, message={result.ErrorMessage ?? "Failed"}.");
        executionContext.Global.Ui.ReportFileFailed(taskId, workItem.RelativePath, result.ErrorCategory ?? "Unknown", result.ErrorMessage ?? "Failed", result.Attempts);
        executionContext.Global.Ui.ReportTaskFailed(taskId, result.ErrorMessage ?? "Failed");
        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Failed, result.ErrorMessage);
        ApplyFailurePolicy(executionContext);
        return false;
    }

    /// <summary>
    /// 执行目录传输任务（顺序执行，不使用协程并发编排）。
    /// </summary>
    /// <param name="executionContext">任务执行上下文。</param>
    /// <returns>是否成功。</returns>
    private async Task<bool> ExecuteDirectoryTaskAsync(TaskExecutionContext executionContext)
    {
        var taskId = executionContext.Descriptor.TaskId;
        var progress = new ProgressSink(executionContext.Global.Ui, taskId, executionContext.State, ProgressInterval, SpeedReportInterval);
        var bufferSize = Math.Max(4 * 1024, executionContext.Global.Options.BlockSize * 1024);
        var discoveredFiles = 0;
        var skippedFiles = 0;
        var runningStateApplied = false;

        executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Scanning);
        executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, $"Directory scan started. src={_request.SourcePath}.");

        foreach (var filePath in Directory.EnumerateFiles(_request.SourcePath, "*", SearchOption.AllDirectories))
        {
            executionContext.TaskCancellationToken.ThrowIfCancellationRequested();
            discoveredFiles++;

            var relativePath = executionContext.Global.PathPolicy.GetRelativePath(_request.SourcePath, filePath);
            var destinationPath = executionContext.Global.PathPolicy.CombineDestination(_request.DestinationPath, relativePath);
            var fileLength = new FileInfo(filePath).Length;
            var workItem = new FileTransferWorkItem(filePath, destinationPath, relativePath);

            executionContext.Global.Ui.RegisterFile(taskId, relativePath, fileLength);
            executionContext.Global.Ui.UpdateFileStatus(taskId, relativePath, FileItemStatus.Pending);

            executionContext.State.IncrementFilesTotal();
            progress.SetTotalBytes(executionContext.State.TotalBytes + fileLength);
            progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);

            if (!runningStateApplied)
            {
                runningStateApplied = true;
                executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Running);
            }

            using var fileContext = new FileExecutionContext(executionContext, workItem);
            var fileProgress = new FileProgressSink(fileContext, relativePath, fileLength, ProgressInterval, SpeedReportInterval);

            FileTransferOutcome result;
            try
            {
                result = await executionContext.Global.FileTransferCapability
                    .CopyOneFileAsync(fileContext, progress, fileProgress, bufferSize, setTotalBytes: false)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (fileContext.FileCancellationToken.IsCancellationRequested)
            {
                fileProgress.ForceReport(fileContext);
                MarkFileCanceled(fileContext, ResolveCancellationMessage(executionContext));
                throw;
            }

            fileProgress.ForceReport(fileContext);

            if (result.Success)
            {
                executionContext.State.IncrementFilesDone();
                progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
                if (result.AlreadyCompleted)
                {
                    skippedFiles++;
                    executionContext.Global.Ui.ReportFileSkipped(taskId, relativePath, "Already up to date.");
                }
                else
                {
                    executionContext.Global.Ui.ReportFileCompleted(taskId, relativePath, result.Bytes);
                }

                continue;
            }

            executionContext.State.IncrementFailedFiles();
            progress.ReportFolderCounters(executionContext.State.FilesDone, executionContext.State.FilesTotal, executionContext.State.FailedFiles);
            executionContext.Global.LogError(executionContext.Descriptor.DisplayName, $"File failed. path={relativePath}, category={result.ErrorCategory ?? "Unknown"}, attempts={result.Attempts}, message={result.ErrorMessage ?? "Failed"}.");
            executionContext.Global.Ui.ReportFileFailed(taskId, relativePath, result.ErrorCategory ?? "Unknown", result.ErrorMessage ?? "Failed", result.Attempts);
            ApplyFailurePolicy(executionContext);

            if (executionContext.Global.Options.TaskFailurePolicy != TaskFailurePolicy.Continue)
            {
                break;
            }
        }

        executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, $"Directory scan finished. discovered={executionContext.State.FilesTotal}.");
        progress.ForceReport();

        if (executionContext.TaskCancellationToken.IsCancellationRequested)
        {
            executionContext.Global.LogWarning(executionContext.Descriptor.DisplayName, $"Task canceled. reason={ResolveCancellationMessage(executionContext)}");
            throw new OperationCanceledException(executionContext.TaskCancellationToken);
        }

        if (executionContext.State.FailedFiles > 0 && executionContext.Global.Options.TaskFailurePolicy != TaskFailurePolicy.Continue)
        {
            const string message = "Task stopped by failure policy.";
            executionContext.Global.LogError(executionContext.Descriptor.DisplayName, $"Task failed by policy. message={message}");
            executionContext.Global.Ui.ReportTaskFailed(taskId, message);
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Failed, message);
            return false;
        }

        if (executionContext.State.FailedFiles > 0)
        {
            const string message = "Completed with file failures.";
            executionContext.Global.LogWarning(executionContext.Descriptor.DisplayName, $"Task completed with errors. message={message}");
            executionContext.Global.Ui.ReportTaskFailed(taskId, message);
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.CompletedWithErrors, message);
            return true;
        }

        if (discoveredFiles > 0 && skippedFiles == discoveredFiles)
        {
            executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, "Task skipped. all files already up to date.");
            executionContext.Global.Ui.UpdateTaskStatus(taskId, TaskStatus.Skipped, "Already up to date.");
            return true;
        }

        executionContext.Global.LogInformation(executionContext.Descriptor.DisplayName, "Task completed.");
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
                executionContext.Global.LogWarning(executionContext.Descriptor.DisplayName, "Failure policy triggered: StopCurrentTask.");
                executionContext.CancelTaskByFailure();
                break;
            case TaskFailurePolicy.StopAll:
                executionContext.Global.LogWarning(executionContext.Descriptor.DisplayName, "Failure policy triggered: StopAll.");
                executionContext.CancelTaskByFailure();
                executionContext.Global.CancelAll();
                break;
        }
    }
}
