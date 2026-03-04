using System.CommandLine;
using Zeayii.Flow.CommandLine.Models;
using Zeayii.Flow.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Zeayii.Flow.Presentation.Models;
using Zeayii.Flow.Presentation.Options;

namespace Zeayii.Flow.CommandLine.Options;

/// <summary>
/// 提供命令行选项到业务配置的转换方法。
/// </summary>
internal static class OptionsBuilder
{
    /// <summary>
    /// 构建应用配置。
    /// </summary>
    /// <param name="parseResult">命令行解析结果。</param>
    /// <param name="planArgument">计划文件参数。</param>
    /// <param name="concurrencyOption">任务并发参数。</param>
    /// <param name="innerConcurrencyOption">子任务并发参数。</param>
    /// <param name="retryAttemptsOption">重试次数参数。</param>
    /// <param name="blockSizeOption">分块大小参数。</param>
    /// <param name="taskFailurePolicyOption">任务失败策略参数。</param>
    /// <param name="conflictPolicyOption">冲突策略参数。</param>
    /// <param name="dryRunOption">空跑参数。</param>
    /// <param name="noTuiOption">禁用 TUI 参数。</param>
    /// <param name="uiRefreshOption">UI刷新间隔（毫秒）参数。</param>
    /// <param name="uiLogMaxOption">UI日志条目上限参数。</param>
    /// <param name="uiLogLevelOption">UI日志等级参数。</param>
    /// <param name="logDirectoryOption">日志目录参数。</param>
    /// <param name="fileLogLevelOption">文件日志等级参数。</param>
    /// <param name="uiFailMaxOption">UI单任务失败条目上限参数。</param>
    /// <param name="uiDoneMaxOption">UI目录任务最近完成文件上限参数。</param>
    /// <param name="uiTaskMaxOption">UI任务缓存上限参数。</param>
    /// <param name="uiPageSizeOption">UI默认分页大小参数。</param>
    /// <param name="uiFailShowOption">UI详情失败展示数量参数。</param>
    /// <param name="uiDoneShowOption">UI详情最近完成文件展示数量参数。</param>
    /// <returns>应用配置对象。</returns>
    public static ApplicationOptions BuildApplicationOptions(
        ParseResult parseResult,
        Argument<FileInfo> planArgument,
        Option<int> concurrencyOption,
        Option<int> innerConcurrencyOption,
        Option<int> retryAttemptsOption,
        Option<int> blockSizeOption,
        Option<TaskFailurePolicy> taskFailurePolicyOption,
        Option<ConflictPolicy> conflictPolicyOption,
        Option<bool> dryRunOption,
        Option<bool> noTuiOption,
        Option<int> uiRefreshOption,
        Option<int> uiLogMaxOption,
        Option<LogLevel> uiLogLevelOption,
        Option<DirectoryInfo> logDirectoryOption,
        Option<LogLevel> fileLogLevelOption,
        Option<int> uiFailMaxOption,
        Option<int> uiDoneMaxOption,
        Option<int> uiTaskMaxOption,
        Option<int> uiPageSizeOption,
        Option<int> uiFailShowOption,
        Option<int> uiDoneShowOption)
    {
        return new ApplicationOptions(
            planPath: parseResult.GetRequiredValue(planArgument),
            concurrency: parseResult.GetValue(concurrencyOption),
            innerConcurrency: parseResult.GetValue(innerConcurrencyOption),
            retryAttempts: parseResult.GetValue(retryAttemptsOption),
            blockSize: parseResult.GetValue(blockSizeOption),
            taskFailurePolicy: parseResult.GetValue(taskFailurePolicyOption),
            conflictPolicy: parseResult.GetValue(conflictPolicyOption),
            dryRun: parseResult.GetValue(dryRunOption),
            enableTui: !parseResult.GetValue(noTuiOption),
            uiRefreshInterval: TimeSpan.FromMilliseconds(parseResult.GetValue(uiRefreshOption)),
            uiMaxLogEntries: parseResult.GetValue(uiLogMaxOption),
            uiLogLevel: parseResult.GetValue(uiLogLevelOption),
            logDirectory: parseResult.GetValue(logDirectoryOption) ?? new DirectoryInfo(Path.Combine(Path.GetTempPath(), "sync")),
            fileLogLevel: parseResult.GetValue(fileLogLevelOption),
            uiMaxFailuresPerTask: parseResult.GetValue(uiFailMaxOption),
            uiMaxRecentDonePerFolderTask: parseResult.GetValue(uiDoneMaxOption),
            uiMaxTasksKept: parseResult.GetValue(uiTaskMaxOption),
            uiDefaultPageSize: parseResult.GetValue(uiPageSizeOption),
            uiVisibleFailuresInDetail: parseResult.GetValue(uiFailShowOption),
            uiVisibleRecentDoneInDetail: parseResult.GetValue(uiDoneShowOption)
        );
    }

    /// <summary>
    /// 构建核心引擎配置。
    /// </summary>
    /// <param name="app">应用配置。</param>
    /// <returns>核心引擎配置。</returns>
    public static CoreOptions BuildCoreOptions(ApplicationOptions app)
        => new(taskConcurrency: app.Concurrency, innerConcurrency: app.InnerConcurrency, maxRetries: app.RetryAttempts, blockSize: app.BlockSize, taskFailurePolicy: app.TaskFailurePolicy);

    /// <summary>
    /// 构建UI配置。
    /// </summary>
    /// <param name="applicationOptions">应用配置。</param>
    /// <returns>UI配置。</returns>
    public static PresentationOptions BuildPresentationOptions(ApplicationOptions applicationOptions)
        => new()
        {
            HeaderFailurePolicy = applicationOptions.TaskFailurePolicy.ToString(),
            HeaderTaskConcurrency = applicationOptions.Concurrency,
            HeaderInnerConcurrency = applicationOptions.InnerConcurrency,
            HeaderRetryAttempts = applicationOptions.RetryAttempts,
            HeaderBlockSizeText = $"{applicationOptions.BlockSize} KiB",
            HeaderConflictPolicy = applicationOptions.ConflictPolicy.ToString(),
            HeaderRunModeText = applicationOptions.DryRun ? "DRY RUN" : null,
            RefreshInterval = applicationOptions.UiRefreshInterval,
            MaxLogEntries = applicationOptions.UiMaxLogEntries,
            TuiLogLevel = MapLogLevel(applicationOptions.UiLogLevel, PresentationLogLevel.Information),
            LogDirectory = applicationOptions.LogDirectory.FullName,
            FileLogLevel = MapLogLevel(applicationOptions.FileLogLevel, PresentationLogLevel.Information),
            MaxFailuresPerTask = applicationOptions.UiMaxFailuresPerTask,
            MaxRecentCompletedFilesPerFolderTask = applicationOptions.UiMaxRecentDonePerFolderTask,
            MaxTasksKept = applicationOptions.UiMaxTasksKept,
            DefaultPageSize = applicationOptions.UiDefaultPageSize,
            VisibleFailuresInDetail = applicationOptions.UiVisibleFailuresInDetail,
            VisibleRecentCompletedFilesInDetail = applicationOptions.UiVisibleRecentDoneInDetail
        };

    /// <summary>
    /// 将通用日志等级映射为UI日志等级。
    /// </summary>
    /// <param name="level">通用日志等级。</param>
    /// <param name="fallback">兜底等级。</param>
    /// <returns>映射后的UI日志等级。</returns>
    private static PresentationLogLevel MapLogLevel(LogLevel level, PresentationLogLevel fallback)
    {
        return level switch
        {
            LogLevel.None => PresentationLogLevel.None,
            LogLevel.Trace => PresentationLogLevel.Trace,
            LogLevel.Debug => PresentationLogLevel.Debug,
            LogLevel.Information => PresentationLogLevel.Information,
            LogLevel.Warning => PresentationLogLevel.Warning,
            LogLevel.Error => PresentationLogLevel.Error,
            LogLevel.Critical => PresentationLogLevel.Critical,
            _ => fallback
        };
    }

    /// <summary>
    /// 将计划条目列表转换为任务请求对象列表。
    /// </summary>
    /// <param name="app">应用配置。</param>
    /// <param name="plans">计划条目列表。</param>
    /// <returns>任务请求。</returns>
    public static IReadOnlyList<TaskRequest> ToTaskRequests(ApplicationOptions app, IReadOnlyList<PlanModel> plans)
        => plans.Select(plan => new TaskRequest(taskId: $"task-{Guid.NewGuid():N}", sourcePath: plan.Src, destinationPath: plan.Dst, conflictPolicy: app.ConflictPolicy)).ToList();
}

