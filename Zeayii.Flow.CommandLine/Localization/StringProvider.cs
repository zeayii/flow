using System.Globalization;

namespace Zeayii.Flow.CommandLine.Localization;

/// <summary>
/// 提供命令行文案的本地化解析能力。
/// </summary>
internal static class StringProvider
{
    /// <summary>
    /// 根据参数与系统文化配置文案文化。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    public static void Configure(string[] args)
    {
        var culture = ResolveCulture(args);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>
    /// 解析最终使用的文化。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>归一化后的文化对象。</returns>
    private static CultureInfo ResolveCulture(string[] args)
    {
        var fromArgument = TryReadLanguageFromArguments(args);
        var normalizedTag = !string.IsNullOrWhiteSpace(fromArgument) ? NormalizeLanguageTag(fromArgument) : NormalizeLanguageTag(CultureInfo.CurrentUICulture.Name);
        return CultureInfo.GetCultureInfo(normalizedTag);
    }

    /// <summary>
    /// 尝试从命令行参数中读取语言值。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>读取到的语言标签；未提供时返回空。</returns>
    private static string? TryReadLanguageFromArguments(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
            {
                return arg["--lang=".Length..];
            }

            if (!arg.Equals("--lang", StringComparison.OrdinalIgnoreCase) &&
                !arg.Equals("-l", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// 将输入语言标签归一化到受支持集合。
    /// </summary>
    /// <param name="language">原始语言标签。</param>
    /// <returns>归一化后的语言标签。</returns>
    private static string NormalizeLanguageTag(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en-US";
        }

        var normalized = language.Trim().Replace('_', '-').ToLowerInvariant();
        return normalized switch
        {
            "en" => "en-US",
            "en-us" => "en-US",
            "zh" => "zh-CN",
            "zh-cn" => "zh-CN",
            "zh-sg" => "zh-CN",
            "zh-hans" => "zh-CN",
            "zh-tw" => "zh-TW",
            "zh-hk" => "zh-TW",
            "zh-mo" => "zh-TW",
            "zh-hant" => "zh-TW",
            "ja" => "ja-JP",
            "ja-jp" => "ja-JP",
            "ko" => "ko-KR",
            "ko-kr" => "ko-KR",
            _ when normalized.StartsWith("en-") => "en-US",
            _ when normalized.StartsWith("zh-hans") => "zh-CN",
            _ when normalized.StartsWith("zh-hant") => "zh-TW",
            _ when normalized.StartsWith("zh-") => "zh-CN",
            _ when normalized.StartsWith("ja-") => "ja-JP",
            _ when normalized.StartsWith("ko-") => "ko-KR",
            _ => "en-US"
        };
    }
}

