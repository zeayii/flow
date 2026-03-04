using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Implementations;
using Zeayii.Flow.Presentation.Options;

[assembly: InternalsVisibleTo("Zeayii.Flow.Tests")]

namespace Zeayii.Flow.Presentation.Extensions;

/// <summary>
/// 依赖注入扩展方法集合。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册呈现层所需的服务与配置。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="options">呈现层设置选项。</param>
    /// <returns>注册后的服务集合。</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, PresentationOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<SpectrePresentationManager>();
        services.AddSingleton<IPresentationManager>(provider => provider.GetRequiredService<SpectrePresentationManager>());
        services.AddSingleton<ITuiLogSink>(provider => provider.GetRequiredService<SpectrePresentationManager>());
        return services;
    }
}


