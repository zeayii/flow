namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 提供路径计算与命名策略能力。
/// </summary>
internal interface IPathPolicy
{
    /// <summary>
    /// 计算相对路径。
    /// </summary>
    /// <param name="rootPath">根路径。</param>
    /// <param name="fullPath">完整路径。</param>
    /// <returns>相对路径。</returns>
    string GetRelativePath(string rootPath, string fullPath);

    /// <summary>
    /// 组合目标路径。
    /// </summary>
    /// <param name="destinationRoot">目标根路径。</param>
    /// <param name="relativePath">相对路径。</param>
    /// <returns>组合后的目标路径。</returns>
    string CombineDestination(string destinationRoot, string relativePath);

    /// <summary>
    /// 获取临时文件路径。
    /// </summary>
    /// <param name="destinationFinal">最终目标路径。</param>
    /// <returns>临时文件路径。</returns>
    string GetTemporaryPath(string destinationFinal);

    /// <summary>
    /// 获取下一个可用路径。
    /// </summary>
    /// <param name="destinationFinal">最终目标路径。</param>
    /// <returns>可用路径。</returns>
    string GetNextAvailablePath(string destinationFinal);

    /// <summary>
    /// 获取损坏临时文件路径。
    /// </summary>
    /// <param name="temporaryPath">临时文件路径。</param>
    /// <returns>损坏文件路径。</returns>
    string GetBadTemporaryPath(string temporaryPath);
}

