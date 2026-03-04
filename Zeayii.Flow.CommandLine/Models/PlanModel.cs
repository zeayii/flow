using System.Text.Json.Serialization;

namespace Zeayii.Flow.CommandLine.Models;

/// <summary>
/// 描述命令行解析得到的计划条目。
/// </summary>
internal sealed class PlanModel
{
    /// <summary>
    /// 初始化计划条目。
    /// </summary>
    /// <param name="src">源路径。</param>
    /// <param name="dst">目标路径。</param>
    public PlanModel(string src, string dst)
    {
        Src = src;
        Dst = dst;
    }

    /// <summary>
    /// 源路径。
    /// </summary>
    [JsonPropertyName("src")]
    public string Src { get; }

    /// <summary>
    /// 目标路径。
    /// </summary>
    [JsonPropertyName("dst")]
    public string Dst { get; }
}

/// <summary>
/// PlanModel 的 Json 序列化上下文。
/// </summary>
[JsonSerializable(typeof(PlanModel))]
[JsonSerializable(typeof(List<PlanModel>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
internal partial class PlanModelContext : JsonSerializerContext;

