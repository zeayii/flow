namespace Zeayii.Flow.Core.Engine.Capabilities;

/// <summary>
/// 路径与命名策略实现。
/// </summary>
internal sealed class PathPolicy : IPathPolicy
{
    /// <summary>
    /// 计算相对路径。
    /// </summary>
    /// <param name="rootPath">根路径。</param>
    /// <param name="fullPath">完整路径。</param>
    /// <returns>相对路径。</returns>
    public string GetRelativePath(string rootPath, string fullPath)
    {
        return Path.GetRelativePath(rootPath, fullPath);
    }

    /// <summary>
    /// 组合目标路径。
    /// </summary>
    /// <param name="destinationRoot">目标根路径。</param>
    /// <param name="relativePath">相对路径。</param>
    /// <returns>组合后的目标路径。</returns>
    public string CombineDestination(string destinationRoot, string relativePath)
    {
        return Path.Combine(destinationRoot, relativePath);
    }

    /// <summary>
    /// 获取临时文件路径。
    /// </summary>
    /// <param name="destinationFinal">最终目标路径。</param>
    /// <returns>临时文件路径。</returns>
    public string GetTemporaryPath(string destinationFinal)
    {
        return destinationFinal + ".tmp";
    }

    /// <summary>
    /// 获取下一个可用路径。
    /// </summary>
    /// <param name="destinationFinal">最终目标路径。</param>
    /// <returns>可用路径。</returns>
    public string GetNextAvailablePath(string destinationFinal)
    {
        if (!File.Exists(destinationFinal))
        {
            return destinationFinal;
        }

        var directory = Path.GetDirectoryName(destinationFinal) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(destinationFinal);
        var extension = Path.GetExtension(destinationFinal);

        for (var index = 1; index < 1000; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName}_{index:000}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{fileName}_{Guid.NewGuid():N}{extension}");
    }

    /// <summary>
    /// 获取损坏临时文件路径。
    /// </summary>
    /// <param name="temporaryPath">临时文件路径。</param>
    /// <returns>损坏文件路径。</returns>
    public string GetBadTemporaryPath(string temporaryPath)
    {
        var directory = Path.GetDirectoryName(temporaryPath) ?? string.Empty;
        var fileName = Path.GetFileName(temporaryPath);

        for (var index = 1; index < 1000; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName}.bad_{index:000}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{fileName}.bad_{Guid.NewGuid():N}");
    }
}

