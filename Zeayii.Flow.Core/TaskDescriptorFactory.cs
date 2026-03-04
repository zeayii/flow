using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Core;

/// <summary>
/// 提供任务展示描述信息的统一构造逻辑。
/// </summary>
public static class TaskDescriptorFactory
{
    /// <summary>
    /// 根据任务请求创建展示描述信息。
    /// </summary>
    /// <param name="request">任务请求。</param>
    /// <param name="createdAt">任务创建时间。</param>
    /// <returns>展示描述信息。</returns>
    public static TaskDescriptor Create(TaskRequest request, DateTimeOffset createdAt)
    {
        var displayName = Path.GetFileName(request.SourcePath);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = request.SourcePath;
        }

        var kind = Directory.Exists(request.SourcePath) ? TaskKind.Directory : TaskKind.File;
        return new TaskDescriptor(request.TaskId, kind, request.SourcePath, request.DestinationPath, displayName, createdAt);
    }
}


