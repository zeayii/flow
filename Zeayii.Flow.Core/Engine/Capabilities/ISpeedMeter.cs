namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 提供速度计算能力。
/// </summary>
internal interface ISpeedMeter
{
    /// <summary>
    /// 累加字节数。
    /// </summary>
    /// <param name="bytes">新增字节数。</param>
    void AddBytes(long bytes);

    /// <summary>
    /// 获取每秒字节数。
    /// </summary>
    /// <returns>速度值。</returns>
    double GetBytesPerSecond();
}

