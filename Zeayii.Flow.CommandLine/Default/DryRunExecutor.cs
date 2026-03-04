using Zeayii.Flow.CommandLine.Options;
using Zeayii.Flow.Core;
using Zeayii.Flow.Core.Abstractions;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.CommandLine.Default;

/// <summary>
/// 提供空跑模式的执行与预览逻辑。
/// </summary>
internal static class DryRunExecutor
{
    /// <summary>
    /// 在 TUI 中填充空跑预览数据。
    /// </summary>
    /// <param name="tasks">任务请求列表。</param>
    /// <param name="options">应用配置。</param>
    /// <param name="ui">展示层管理器。</param>
    /// <param name="logSink">可选日志输入接口。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>完成任务。</returns>
    public static async Task PopulatePreviewAsync(
        IReadOnlyList<TaskRequest> tasks,
        ApplicationOptions options,
        IPresentationManager ui,
        ITuiLogSink? logSink,
        CancellationToken ct)
    {
        logSink?.Information("Dry-run preview mode enabled. No file operation will be executed.");
        logSink?.Information($"Loaded {tasks.Count} tasks for preview.");

        foreach (var task in tasks)
        {
            ct.ThrowIfCancellationRequested();
            var descriptor = TaskDescriptorFactory.Create(task, DateTimeOffset.UtcNow);
            ui.RegisterTask(descriptor);
            ui.UpdateTaskStatus(descriptor.TaskId, TaskStatus.Pending, "Dry-run preview.");

            if (descriptor.Kind == TaskKind.File)
            {
                RegisterSingleFilePreview(descriptor, ui, logSink);
                continue;
            }

            await RegisterDirectoryPreviewAsync(descriptor, ui, options, logSink, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 输出空跑模式的文本结果。
    /// </summary>
    /// <param name="tasks">任务请求列表。</param>
    /// <param name="options">应用配置。</param>
    public static void PrintConsolePreview(IReadOnlyList<TaskRequest> tasks, ApplicationOptions options)
    {
        ConsoleOutput.WriteLine("Dry run:");
        ConsoleOutput.WriteLine($"  tasks: {tasks.Count}");
        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            ConsoleOutput.WriteLine($"  [{index}] src: {task.SourcePath}");
            ConsoleOutput.WriteLine($"  [{index}] dst: {task.DestinationPath}");
        }

        ConsoleOutput.WriteLine($"  concurrency: {options.Concurrency}");
        ConsoleOutput.WriteLine($"  inner concurrency: {options.InnerConcurrency}");
        ConsoleOutput.WriteLine($"  retries: {options.RetryAttempts}");
        ConsoleOutput.WriteLine($"  block size (KiB): {options.BlockSize}");
        ConsoleOutput.WriteLine($"  conflict: {options.ConflictPolicy}");
        ConsoleOutput.WriteLine($"  tui: {(options.EnableTui ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 注册单文件任务的空跑预览信息。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    /// <param name="ui">展示层管理器。</param>
    /// <param name="logSink">可选日志输入接口。</param>
    private static void RegisterSingleFilePreview(TaskDescriptor descriptor, IPresentationManager ui, ITuiLogSink? logSink)
    {
        var fileLength = new FileInfo(descriptor.SourcePath).Length;
        var relativePath = Path.GetFileName(descriptor.SourcePath);
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            relativePath = descriptor.SourcePath;
        }

        ui.RegisterFile(descriptor.TaskId, relativePath, fileLength);
        ui.UpdateFileStatus(descriptor.TaskId, relativePath, FileItemStatus.Pending, "Dry-run preview.");
        ui.ReportTaskProgress(descriptor.TaskId, 0, fileLength);
        ui.ReportTaskSpeed(descriptor.TaskId, 0);
        ui.ReportFolderCounters(descriptor.TaskId, 0, 1, 0);
        ui.ReportFileProgress(descriptor.TaskId, relativePath, 0, fileLength, 0);
        logSink?.Debug($"Prepared file preview: {descriptor.SourcePath}", descriptor.DisplayName);
    }

    /// <summary>
    /// 注册目录任务的空跑预览信息。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    /// <param name="ui">展示层管理器。</param>
    /// <param name="options">应用配置。</param>
    /// <param name="logSink">可选日志输入接口。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>完成任务。</returns>
    private static async Task RegisterDirectoryPreviewAsync(
        TaskDescriptor descriptor,
        IPresentationManager ui,
        ApplicationOptions options,
        ITuiLogSink? logSink,
        CancellationToken ct)
    {
        ui.UpdateTaskStatus(descriptor.TaskId, TaskStatus.Scanning, "Dry-run scanning.");

        var filesTotal = 0;
        long totalBytes = 0;
        var fileIndex = 0;
        foreach (var filePath in Directory.EnumerateFiles(descriptor.SourcePath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(descriptor.SourcePath, filePath);
            ui.RegisterFile(descriptor.TaskId, relativePath, fileInfo.Length);
            ui.UpdateFileStatus(descriptor.TaskId, relativePath, FileItemStatus.Pending, "Dry-run preview.");
            ui.ReportFileProgress(descriptor.TaskId, relativePath, 0, fileInfo.Length, 0);

            filesTotal++;
            totalBytes += fileInfo.Length;
            fileIndex++;
            if (fileIndex % Math.Max(16, options.InnerConcurrency * 8) == 0)
            {
                ui.ReportFolderCounters(descriptor.TaskId, 0, filesTotal, 0);
                ui.ReportTaskProgress(descriptor.TaskId, 0, totalBytes);
                await Task.Yield();
            }
        }

        ui.UpdateTaskStatus(descriptor.TaskId, TaskStatus.Pending, "Dry-run preview.");
        ui.ReportFolderCounters(descriptor.TaskId, 0, filesTotal, 0);
        ui.ReportTaskProgress(descriptor.TaskId, 0, totalBytes);
        ui.ReportTaskSpeed(descriptor.TaskId, 0);
        logSink?.Debug($"Prepared directory preview with {filesTotal} files.", descriptor.DisplayName);
    }
}



