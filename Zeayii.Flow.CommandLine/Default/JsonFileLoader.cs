using System.Text.Json;
using Zeayii.Flow.CommandLine.Models;

namespace Zeayii.Flow.CommandLine.Default;

/// <summary>
/// 提供计划文件的 JSON 解析功能。
/// </summary>
internal static class JsonFileLoader
{
    /// <summary>
    /// 异步加载计划文件并解析为模型。
    /// </summary>
    /// <param name="planPath">计划文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>计划模型。</returns>
    public static async Task<IReadOnlyList<PlanModel>> LoadPlanAsync(FileInfo planPath, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(planPath.FullName, ct);
        var plans = JsonSerializer.Deserialize(json, PlanModelContext.Default.ListPlanModel);

        if (plans is null || plans.Count == 0)
        {
            throw new JsonException("Plan file must be a non-empty JSON array.");
        }

        for (var index = 0; index < plans.Count; index++)
        {
            var plan = plans[index];
            if (plan is null || string.IsNullOrWhiteSpace(plan.Src) || string.IsNullOrWhiteSpace(plan.Dst))
            {
                throw new JsonException($"Plan item at index {index} must contain non-empty src and dst.");
            }

            if (!File.Exists(plan.Src) && !Directory.Exists(plan.Src))
            {
                throw new JsonException($"Plan item at index {index} has non-existent src: {plan.Src}");
            }

            ValidateDestinationPath(plan.Dst, index);
        }

        return plans;
    }

    /// <summary>
    /// 校验目标路径合法性（不产生目录创建等副作用）。
    /// </summary>
    private static void ValidateDestinationPath(string destinationPath, int index)
    {
        try
        {
            _ = Path.GetFullPath(destinationPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new JsonException($"Plan item at index {index} has invalid dst path: {destinationPath}", ex);
        }
    }

}

