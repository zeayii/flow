using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Core.Engine.Contexts;

namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 单文件复制能力实现。
/// 该实现封装了冲突策略、断点续传、重试和最终落盘逻辑。
/// </summary>
internal sealed class FileTransferCapability : IFileTransferCapability
{
    /// <inheritdoc />
    public async Task<FileTransferOutcome> CopyOneFileAsync(
        FileExecutionContext executionContext,
        IProgressSink progress,
        FileProgressSink? fileProgress,
        int bufferSize,
        bool setTotalBytes)
    {
        var workItem = executionContext.WorkItem;
        var sourceInfo = new FileInfo(workItem.SourcePath);
        if (!sourceInfo.Exists)
        {
            return FileTransferOutcome.Failed(workItem.RelativePath, 0, 1, "NotFound", "Source file not found.");
        }

        var sourceLength = sourceInfo.Length;
        if (setTotalBytes)
        {
            progress.SetTotalBytes(sourceLength);
        }

        var attempt = 0;
        var progressState = new FileTransferProgressState();
        while (true)
        {
            executionContext.FileCancellationToken.ThrowIfCancellationRequested();
            try
            {
                var outcome = await CopyWithPolicyAsync(executionContext, workItem, sourceLength, progress, fileProgress, progressState, bufferSize);
                return outcome.WithAttempts(attempt + 1);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (executionContext.Global.RetryPolicy.IsRetryable(ex) && attempt < executionContext.TaskContext.MaxRetries)
            {
                attempt++;
                await Task.Delay(executionContext.Global.RetryPolicy.GetDelay(attempt), executionContext.FileCancellationToken);
            }
            catch (Exception ex)
            {
                var category = executionContext.Global.RetryPolicy.GetCategory(ex);
                return FileTransferOutcome.Failed(workItem.RelativePath, sourceLength, attempt + 1, category, ex.Message);
            }
        }
    }

    /// <summary>
    /// 根据冲突策略执行文件复制。
    /// </summary>
    private static async Task<FileTransferOutcome> CopyWithPolicyAsync(
        FileExecutionContext executionContext,
        FileTransferWorkItem workItem,
        long sourceLength,
        IProgressSink progress,
        FileProgressSink? fileProgress,
        FileTransferProgressState progressState,
        int bufferSize)
    {
        var pathPolicy = executionContext.Global.PathPolicy;
        var destinationFinal = workItem.DestinationPath;

        if (executionContext.TaskContext.ConflictPolicy == ConflictPolicy.Rename)
        {
            destinationFinal = pathPolicy.GetNextAvailablePath(destinationFinal);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationFinal) ?? string.Empty);

        var destinationTemp = pathPolicy.GetTemporaryPath(destinationFinal);
        var resumeOffset = 0L;

        if (executionContext.TaskContext.ConflictPolicy == ConflictPolicy.Resume)
        {
            if (File.Exists(destinationFinal))
            {
                var finalLength = new FileInfo(destinationFinal).Length;
                if (finalLength == sourceLength)
                {
                    progressState.ReportToProgress(executionContext, progress, fileProgress, sourceLength);
                    return FileTransferOutcome.Succeeded(workItem.RelativePath, destinationFinal, sourceLength, alreadyCompleted: true);
                }

                destinationFinal = pathPolicy.GetNextAvailablePath(destinationFinal);
                destinationTemp = pathPolicy.GetTemporaryPath(destinationFinal);
            }

            if (File.Exists(destinationTemp))
            {
                var tempLength = new FileInfo(destinationTemp).Length;
                if (tempLength == sourceLength)
                {
                    if (!File.Exists(destinationFinal))
                    {
                        File.Move(destinationTemp, destinationFinal);
                    }
                    else if (new FileInfo(destinationFinal).Length == sourceLength)
                    {
                        File.Delete(destinationTemp);
                    }

                    progressState.ReportToProgress(executionContext, progress, fileProgress, sourceLength);
                    return FileTransferOutcome.Succeeded(workItem.RelativePath, destinationFinal, sourceLength, alreadyCompleted: true);
                }

                if (tempLength > sourceLength)
                {
                    var badPath = pathPolicy.GetBadTemporaryPath(destinationTemp);
                    File.Move(destinationTemp, badPath);
                }
                else
                {
                    resumeOffset = tempLength;
                }
            }
        }
        else
        {
            if (File.Exists(destinationFinal) && executionContext.TaskContext.ConflictPolicy == ConflictPolicy.Overwrite)
            {
                File.Delete(destinationFinal);
            }

            if (File.Exists(destinationTemp))
            {
                File.Delete(destinationTemp);
            }
        }

        if (resumeOffset > 0)
        {
            progressState.ReportToProgress(executionContext, progress, fileProgress, resumeOffset);
        }

        await CopyStreamAsync(workItem.SourcePath, destinationTemp, resumeOffset, bufferSize, progress, fileProgress, progressState, executionContext, executionContext.FileCancellationToken);
        FinalizeCopy(destinationTemp, destinationFinal, sourceLength);
        progressState.ReportToProgress(executionContext, progress, fileProgress, sourceLength);

        return FileTransferOutcome.Succeeded(workItem.RelativePath, destinationFinal, sourceLength, alreadyCompleted: false);
    }

    /// <summary>
    /// 将源文件内容复制到临时目标文件。
    /// </summary>
    private static async Task CopyStreamAsync(
        string sourcePath,
        string destinationTemp,
        long resumeOffset,
        int bufferSize,
        IProgressSink progress,
        FileProgressSink? fileProgress,
        FileTransferProgressState progressState,
        FileExecutionContext executionContext,
        CancellationToken fileCancellationToken)
    {
        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
        await using var destStream = new FileStream(destinationTemp, resumeOffset > 0 ? FileMode.OpenOrCreate : FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

        if (resumeOffset > 0)
        {
            sourceStream.Position = resumeOffset;
            destStream.Position = resumeOffset;
        }

        var buffer = new byte[bufferSize];
        while (true)
        {
            var read = await sourceStream.ReadAsync(buffer, fileCancellationToken);
            if (read == 0)
            {
                break;
            }

            await destStream.WriteAsync(buffer.AsMemory(0, read), fileCancellationToken);
            progressState.ReportIncrement(executionContext, progress, fileProgress, read);
        }

        await destStream.FlushAsync(fileCancellationToken);
    }

    /// <summary>
    /// 完成临时文件到最终文件的原子落盘。
    /// </summary>
    private static void FinalizeCopy(string destinationTemp, string destinationFinal, long sourceLength)
    {
        if (!File.Exists(destinationTemp))
        {
            return;
        }

        if (File.Exists(destinationFinal))
        {
            var finalLength = new FileInfo(destinationFinal).Length;
            if (finalLength == sourceLength)
            {
                File.Delete(destinationTemp);
                return;
            }
        }

        File.Move(destinationTemp, destinationFinal, overwrite: false);
    }

    /// <summary>
    /// 表示单个文件在多次重试过程中的已记账进度状态。
    /// </summary>
    private sealed class FileTransferProgressState
    {
        /// <summary>
        /// 当前文件已经上报到任务进度中的字节数。
        /// </summary>
        private long _accountedBytes;

        /// <summary>
        /// 将目标总进度推进到指定字节数。
        /// </summary>
        /// <param name="executionContext">执行上下文。</param>
        /// <param name="progress">进度汇报器。</param>
        /// <param name="fileProgress">文件进度。</param>
        /// <param name="targetBytes">目标累计字节数。</param>
        public void ReportToProgress(FileExecutionContext executionContext, IProgressSink progress, FileProgressSink? fileProgress, long targetBytes)
        {
            var delta = targetBytes - Interlocked.Read(ref _accountedBytes);
            if (delta <= 0)
            {
                return;
            }

            Interlocked.Add(ref _accountedBytes, delta);
            progress.AddBytes(delta);
            fileProgress?.SetTransferred(executionContext, targetBytes);
        }

        /// <summary>
        /// 上报本次新增写入的字节数。
        /// </summary>
        /// <param name="executionContext">执行上下文。</param>
        /// <param name="progress">进度汇报器。</param>
        /// <param name="fileProgress">文件进度。</param>
        /// <param name="deltaBytes">新增字节数。</param>
        public void ReportIncrement(FileExecutionContext executionContext, IProgressSink progress, FileProgressSink? fileProgress, long deltaBytes)
        {
            if (deltaBytes <= 0)
            {
                return;
            }

            Interlocked.Add(ref _accountedBytes, deltaBytes);
            progress.AddBytes(deltaBytes);
            fileProgress?.AddBytes(executionContext, deltaBytes);
        }
    }
}

