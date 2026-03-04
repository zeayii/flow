namespace Zeayii.Flow.Core.Abstractions;

/// <summary>
/// 定义目标路径冲突时的处理策略。
/// </summary>
public enum ConflictPolicy
{
    /// <summary>
    /// 尝试基于临时文件续传。
    /// </summary>
    Resume,

    /// <summary>
    /// 覆盖现有目标文件。
    /// </summary>
    Overwrite,

    /// <summary>
    /// 为目标文件自动生成新名称。
    /// </summary>
    Rename
}
