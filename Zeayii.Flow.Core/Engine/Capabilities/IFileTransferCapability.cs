using Zeayii.Flow.Core.Engine.Contexts;

namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 提供单文件复制能力（含冲突策略、续传、重试与最终落盘）。
/// </summary>
internal interface IFileTransferCapability
{
    /// <summary>
    /// 复制单个文件并返回执行结果。
    /// </summary>
    /// <param name="executionContext">文件执行上下文。</param>
    /// <param name="progress">进度汇报器。</param>
    /// <param name="fileProgress">文件级进度汇报器。</param>
    /// <param name="bufferSize">缓冲区大小。</param>
    /// <param name="setTotalBytes">是否设置总字节数。</param>
    /// <returns>复制结果。</returns>
    Task<FileTransferOutcome> CopyOneFileAsync(
        FileExecutionContext executionContext,
        IProgressSink progress,
        FileProgressSink? fileProgress,
        int bufferSize,
        bool setTotalBytes);
}

/// <summary>
/// 表示单文件复制工作项。
/// </summary>
/// <param name="SourcePath">源路径。</param>
/// <param name="DestinationPath">目标路径。</param>
/// <param name="RelativePath">相对路径（用于展示）。</param>
internal sealed record FileTransferWorkItem(string SourcePath, string DestinationPath, string RelativePath);

/// <summary>
/// 表示单文件复制结果。
/// </summary>
internal sealed class FileTransferOutcome
{
    private FileTransferOutcome(
        bool success,
        string relativePath,
        string destinationPath,
        long bytes,
        bool alreadyCompleted,
        int attempts,
        string? errorCategory,
        string? errorMessage)
    {
        Success = success;
        RelativePath = relativePath;
        DestinationPath = destinationPath;
        Bytes = bytes;
        AlreadyCompleted = alreadyCompleted;
        Attempts = attempts;
        ErrorCategory = errorCategory;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 已复制字节数。
    /// </summary>
    public long Bytes { get; }

    /// <summary>
    /// 是否命中“已完成”分支。
    /// </summary>
    public bool AlreadyCompleted { get; }

    /// <summary>
    /// 尝试次数。
    /// </summary>
    public int Attempts { get; }

    /// <summary>
    /// 错误分类。
    /// </summary>
    public string? ErrorCategory { get; }

    /// <summary>
    /// 错误消息。
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 相对路径。
    /// </summary>
    private string RelativePath { get; }

    /// <summary>
    /// 最终目标路径。
    /// </summary>
    private string DestinationPath { get; }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static FileTransferOutcome Succeeded(string relativePath, string destinationPath, long bytes, bool alreadyCompleted)
        => new(true, relativePath, destinationPath, bytes, alreadyCompleted, 1, null, null);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static FileTransferOutcome Failed(string relativePath, long bytes, int attempts, string category, string message)
        => new(false, relativePath, string.Empty, bytes, false, attempts, category, message);

    /// <summary>
    /// 返回带有指定尝试次数的新结果。
    /// </summary>
    /// <param name="attempts">尝试次数。</param>
    /// <returns>更新后的结果。</returns>
    public FileTransferOutcome WithAttempts(int attempts)
        => new(Success, RelativePath, DestinationPath, Bytes, AlreadyCompleted, attempts, ErrorCategory, ErrorMessage);
}

