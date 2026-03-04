namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 负责限频进度与速度上报的能力接口。
/// </summary>
internal interface IProgressSink
{
    /// <summary>
    /// 设置总字节数。
    /// </summary>
    /// <param name="totalBytes">总字节数。</param>
    void SetTotalBytes(long totalBytes);

    /// <summary>
    /// 增加已传输字节数。
    /// </summary>
    /// <param name="bytes">新增字节数。</param>
    void AddBytes(long bytes);

    /// <summary>
    /// 上报目录计数器。
    /// </summary>
    /// <param name="filesDone">已完成文件数。</param>
    /// <param name="filesTotal">总文件数。</param>
    /// <param name="failedFiles">失败文件数。</param>
    void ReportFolderCounters(int filesDone, int filesTotal, int failedFiles);

    /// <summary>
    /// 强制上报一次进度与速度。
    /// </summary>
    void ForceReport();
}

