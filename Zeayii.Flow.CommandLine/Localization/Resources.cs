using System.Globalization;
using System.Resources;

namespace Zeayii.Flow.CommandLine.Localization;

/// <summary>
/// CLI 文案本地化访问器。
/// </summary>
internal static class Resources
{
    /// <summary>
    /// 资源管理器。
    /// </summary>
    private static readonly ResourceManager ResourceManager = new("Zeayii.Flow.CommandLine.Resources.Strings", typeof(Resources).Assembly);

    /// <summary>
    /// 默认回退文化。
    /// </summary>
    private static readonly CultureInfo FallbackCulture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// 读取本地化文本。
    /// </summary>
    /// <param name="key">资源键。</param>
    /// <returns>本地化文本。</returns>
    public static string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var culture = CultureInfo.CurrentUICulture;
        return TryGetString(key, culture) ?? TryGetString(key, culture.Parent) ?? TryGetString(key, FallbackCulture) ?? key;
    }

    /// <summary>
    /// 读取并格式化本地化文本。
    /// </summary>
    /// <param name="key">资源键。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>格式化后的本地化文本。</returns>
    public static string Format(string key, params object?[] args) => string.Format(CultureInfo.CurrentUICulture, GetString(key), args);

    /// <summary>
    /// 尝试读取本地化文本。
    /// </summary>
    /// <param name="key">资源键。</param>
    /// <param name="culture">文化信息。</param>
    /// <returns>命中返回文本，否则返回空。</returns>
    private static string? TryGetString(string key, CultureInfo? culture) => culture is null ? null : ResourceManager.GetString(key, culture);
}

