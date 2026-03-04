using Microsoft.Extensions.Logging;
using Zeayii.Flow.CommandLine.Options;
using Zeayii.Flow.Core.Abstractions;

namespace Zeayii.Flow.Tests;

/// <summary>
/// 校验 OptionsBuilder 模块映射逻辑的测试集合。
/// </summary>
public sealed class OptionsBuilderTests
{
    /// <summary>
    /// 验证 PresentationOptions 会携带标题栏需要的关键参数。
    /// </summary>
    [Fact]
    public void BuildPresentationOptions_ShouldPopulateHeaderFields()
    {
        var applicationOptions = new ApplicationOptions(
            planPath: new FileInfo("plan.json"),
            concurrency: 3,
            innerConcurrency: 5,
            retryAttempts: 7,
            blockSize: 256,
            taskFailurePolicy: TaskFailurePolicy.StopCurrentTask,
            conflictPolicy: ConflictPolicy.Resume,
            dryRun: false,
            enableTui: true,
            uiRefreshInterval: TimeSpan.FromMilliseconds(120),
            uiMaxLogEntries: 200,
            uiLogLevel: LogLevel.Information,
            logDirectory: new DirectoryInfo("logs"),
            fileLogLevel: LogLevel.Warning,
            uiMaxFailuresPerTask: 50,
            uiMaxRecentDonePerFolderTask: 20,
            uiMaxTasksKept: 100,
            uiDefaultPageSize: 20,
            uiVisibleFailuresInDetail: 30,
            uiVisibleRecentDoneInDetail: 40);

        var result = OptionsBuilder.BuildPresentationOptions(applicationOptions);

        Assert.Equal("StopCurrentTask", result.HeaderFailurePolicy);
        Assert.Equal(3, result.HeaderTaskConcurrency);
        Assert.Equal(5, result.HeaderInnerConcurrency);
        Assert.Equal(7, result.HeaderRetryAttempts);
        Assert.Equal("256 KiB", result.HeaderBlockSizeText);
        Assert.Equal("Resume", result.HeaderConflictPolicy);
    }
}


