namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 默认重试策略实现：指数退避 + 抖动。
/// </summary>
internal sealed class RetryPolicy : IRetryPolicy
{
    /// <summary>
    /// 判断异常是否可重试。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <returns>是否可重试。</returns>
    public bool IsRetryable(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => false,
            UnauthorizedAccessException => false,
            DirectoryNotFoundException or FileNotFoundException => false,
            IOException ioException when ContainsNoSpace(ioException) => false,
            _ => true
        };
    }

    /// <summary>
    /// 获取重试等待时间。
    /// </summary>
    /// <param name="attempt">当前重试次数。</param>
    /// <returns>等待时间。</returns>
    public TimeSpan GetDelay(int attempt)
    {
        var baseDelayMs = Math.Min(5000, 200 * Math.Pow(2, attempt - 1));
        var jitter = Random.Shared.NextDouble() * 200;
        return TimeSpan.FromMilliseconds(baseDelayMs + jitter);
    }

    /// <summary>
    /// 获取异常分类名称。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <returns>异常分类。</returns>
    public string GetCategory(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => "Permission",
            DirectoryNotFoundException or FileNotFoundException => "NotFound",
            OperationCanceledException => "Canceled",
            IOException ioException when ContainsNoSpace(ioException) => "NoSpace",
            IOException => "Transient",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 判断是否为磁盘空间不足异常。
    /// </summary>
    /// <param name="exception">IO 异常。</param>
    /// <returns>是否为磁盘空间不足。</returns>
    private static bool ContainsNoSpace(IOException exception)
    {
        var message = exception.Message;
        return message.Contains("No space", StringComparison.OrdinalIgnoreCase) || message.Contains("insufficient disk", StringComparison.OrdinalIgnoreCase);
    }
}

