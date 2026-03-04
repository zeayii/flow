using Zeayii.Flow.Core.Engine.Capabilities;

namespace Zeayii.Flow.Core.Engine.Contexts;

/// <summary>
/// 表示任务运行时状态（进度、速度窗口与目录计数器）。
/// </summary>
/// <param name="speedMeter">速度计量器。</param>
internal sealed class TaskRuntimeState(ISpeedMeter speedMeter)
{
    /// <summary>
    /// 已传输字节数的累计值。
    /// </summary>
    private long _transferredBytes;

    /// <summary>
    /// 需要传输的总字节数。
    /// </summary>
    private long _totalBytes;

    /// <summary>
    /// 需要处理的文件总数。
    /// </summary>
    private int _filesTotal;

    /// <summary>
    /// 已完成的文件数量。
    /// </summary>
    private int _filesDone;

    /// <summary>
    /// 失败的文件数量。
    /// </summary>
    private int _failedFiles;


    /// <summary>
    /// 速度计量器。
    /// </summary>
    public ISpeedMeter SpeedMeter { get; } = speedMeter;

    /// <summary>
    /// 已传输字节数。
    /// </summary>
    public long TransferredBytes => Interlocked.Read(ref _transferredBytes);

    /// <summary>
    /// 需要传输的总字节数。
    /// </summary>
    public long TotalBytes => Interlocked.Read(ref _totalBytes);

    /// <summary>
    /// 需要处理的文件总数。
    /// </summary>
    public int FilesTotal => Volatile.Read(ref _filesTotal);

    /// <summary>
    /// 已完成的文件数量。
    /// </summary>
    public int FilesDone => Volatile.Read(ref _filesDone);

    /// <summary>
    /// 失败的文件数量。
    /// </summary>
    public int FailedFiles => Volatile.Read(ref _failedFiles);

    /// <summary>
    /// 增加已传输字节数。
    /// </summary>
    /// <param name="bytes">新增的字节数。</param>
    public void AddBytes(long bytes)
    {
        Interlocked.Add(ref _transferredBytes, bytes);
    }

    /// <summary>
    /// 设置需要传输的总字节数。
    /// </summary>
    /// <param name="totalBytes">总字节数。</param>
    public void SetTotalBytes(long totalBytes)
    {
        Interlocked.Exchange(ref _totalBytes, totalBytes);
    }

    /// <summary>
    /// 增加文件总数。
    /// </summary>
    public void IncrementFilesTotal()
    {
        Interlocked.Increment(ref _filesTotal);
    }

    /// <summary>
    /// 增加已完成文件数。
    /// </summary>
    public void IncrementFilesDone()
    {
        Interlocked.Increment(ref _filesDone);
    }

    /// <summary>
    /// 增加失败文件数。
    /// </summary>
    public void IncrementFailedFiles()
    {
        Interlocked.Increment(ref _failedFiles);
    }
}

