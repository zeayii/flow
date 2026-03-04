using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.CommandLine.Default;

/// <summary>
/// 空实现的呈现层管理器，用于禁用 UI。
/// </summary>
internal sealed class NoOpPresentationManager : IPresentationManager
{
    /// <summary>
    /// 启动呈现层（空实现）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>完成任务。</returns>
    public ValueTask StartAsync(CancellationToken ct) => ValueTask.CompletedTask;

    /// <summary>
    /// 停止呈现层（空实现）。
    /// </summary>
    /// <returns>完成任务。</returns>
    public ValueTask StopAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// 注册任务（空实现）。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    public void RegisterTask(TaskDescriptor descriptor) { }

    /// <summary>
    /// 更新任务状态（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="status">任务状态。</param>
    /// <param name="message">状态描述。</param>
    public void UpdateTaskStatus(string taskId, TaskStatus status, string? message = null) { }

    /// <summary>
    /// 汇报任务进度（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    public void ReportTaskProgress(string taskId, long transferredBytes, long? totalBytes) { }

    /// <summary>
    /// 汇报任务速度（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="bytesPerSecond">每秒字节数。</param>
    public void ReportTaskSpeed(string taskId, double bytesPerSecond) { }

    /// <summary>
    /// 汇报任务完成（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    public void ReportTaskCompleted(string taskId) { }

    /// <summary>
    /// 汇报任务失败（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="errorSummary">错误摘要。</param>
    public void ReportTaskFailed(string taskId, string errorSummary) { }

    /// <summary>
    /// 汇报目录统计（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="filesDone">已完成文件数。</param>
    /// <param name="filesTotal">总文件数。</param>
    /// <param name="failedFiles">失败文件数。</param>
    public void ReportFolderCounters(string taskId, int filesDone, int filesTotal, int failedFiles) { }

    /// <summary>
    /// 注册文件（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="fileBytes">文件字节数。</param>
    public void RegisterFile(string taskId, string relativePath, long fileBytes) { }

    /// <summary>
    /// 更新文件状态（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="status">文件状态。</param>
    /// <param name="message">状态消息。</param>
    public void UpdateFileStatus(string taskId, string relativePath, FileItemStatus status, string? message = null) { }

    /// <summary>
    /// 汇报文件进度（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    /// <param name="bytesPerSecond">实时速度。</param>
    public void ReportFileProgress(string taskId, string relativePath, long transferredBytes, long totalBytes, double bytesPerSecond) { }

    /// <summary>
    /// 汇报文件完成（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">相对路径。</param>
    /// <param name="fileBytes">文件字节数。</param>
    public void ReportFileCompleted(string taskId, string relativePath, long fileBytes) { }

    /// <summary>
    /// 汇报文件失败（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">相对路径。</param>
    /// <param name="errorCategory">错误分类。</param>
    /// <param name="message">错误信息。</param>
    /// <param name="attempt">尝试次数。</param>
    public void ReportFileFailed(string taskId, string relativePath, string errorCategory, string message, int attempt) { }

    /// <summary>
    /// 汇报文件跳过（空实现）。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="reason">跳过原因。</param>
    public void ReportFileSkipped(string taskId, string relativePath, string reason) { }
}


