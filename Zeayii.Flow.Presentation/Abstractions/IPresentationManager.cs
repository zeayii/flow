using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Abstractions;

/// <summary>
/// 定义呈现层与业务层交互的任务展示管理接口。
/// </summary>
public interface IPresentationManager
{
    /// <summary>
    /// 启动呈现层渲染与输入监听。
    /// </summary>
    /// <param name="ct">用于取消启动流程的取消令牌。</param>
    ValueTask StartAsync(CancellationToken ct);

    /// <summary>
    /// 请求停止呈现层并等待后台任务结束。
    /// </summary>
    ValueTask StopAsync();

    /// <summary>
    /// 注册新的任务描述信息。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    void RegisterTask(TaskDescriptor descriptor);

    /// <summary>
    /// 更新指定任务的状态与状态提示信息。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="status">新的任务状态。</param>
    /// <param name="message">可选的状态描述。</param>
    void UpdateTaskStatus(string taskId, TaskStatus status, string? message = null);

    /// <summary>
    /// 汇报任务的传输进度。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="transferredBytes">已传输的字节数。</param>
    /// <param name="totalBytes">总字节数，可为空。</param>
    void ReportTaskProgress(string taskId, long transferredBytes, long? totalBytes);

    /// <summary>
    /// 汇报任务的实时速度。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="bytesPerSecond">每秒字节数。</param>
    void ReportTaskSpeed(string taskId, double bytesPerSecond);

    /// <summary>
    /// 汇报任务完成事件。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    void ReportTaskCompleted(string taskId);

    /// <summary>
    /// 汇报任务失败事件。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="errorSummary">失败摘要信息。</param>
    void ReportTaskFailed(string taskId, string errorSummary);

    /// <summary>
    /// 汇报目录任务中文件计数信息。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="filesDone">已完成文件数。</param>
    /// <param name="filesTotal">总文件数。</param>
    /// <param name="failedFiles">失败文件数。</param>
    void ReportFolderCounters(string taskId, int filesDone, int filesTotal, int failedFiles);

    /// <summary>
    /// 注册单个文件条目。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="fileBytes">文件总字节数。</param>
    void RegisterFile(string taskId, string relativePath, long fileBytes);

    /// <summary>
    /// 更新单个文件条目的状态。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="status">文件状态。</param>
    /// <param name="message">可选状态消息。</param>
    void UpdateFileStatus(string taskId, string relativePath, Presentation.Models.FileItemStatus status, string? message = null);

    /// <summary>
    /// 汇报单个文件的实时进度。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    /// <param name="bytesPerSecond">实时速度。</param>
    void ReportFileProgress(string taskId, string relativePath, long transferredBytes, long totalBytes, double bytesPerSecond);

    /// <summary>
    /// 汇报单个文件完成事件。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="fileBytes">文件字节大小。</param>
    void ReportFileCompleted(string taskId, string relativePath, long fileBytes);

    /// <summary>
    /// 汇报单个文件失败事件。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="errorCategory">错误分类。</param>
    /// <param name="message">错误消息。</param>
    /// <param name="attempt">重试次数。</param>
    void ReportFileFailed(string taskId, string relativePath, string errorCategory, string message, int attempt);

    /// <summary>
    /// 汇报单个文件被跳过。
    /// </summary>
    /// <param name="taskId">任务唯一标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="reason">跳过原因。</param>
    void ReportFileSkipped(string taskId, string relativePath, string reason);
}


