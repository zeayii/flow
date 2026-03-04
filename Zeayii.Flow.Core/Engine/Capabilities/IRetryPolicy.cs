namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 提供重试与退避策略能力。
/// </summary>
internal interface IRetryPolicy
{
    /// <summary>
    /// 判断异常是否可重试。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <returns>是否可重试。</returns>
    bool IsRetryable(Exception exception);

    /// <summary>
    /// 获取重试等待时间。
    /// </summary>
    /// <param name="attempt">当前重试次数。</param>
    /// <returns>等待时间。</returns>
    TimeSpan GetDelay(int attempt);

    /// <summary>
    /// 获取异常分类名称。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <returns>异常分类。</returns>
    string GetCategory(Exception exception);
}

