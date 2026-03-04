using Zeayii.Flow.Presentation.Models;
using Spectre.Console;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 提供 Spectre Presentation 的运行时循环、事件应用与输入处理逻辑。
/// </summary>
public sealed partial class SpectrePresentationManager
{
    /// <summary>
    /// 运行控制台 Dashboard 主循环。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>后台任务。</returns>
    private async Task RunDashboardAsync(CancellationToken ct)
    {
        await InitializeFileLoggerAsync(ct).ConfigureAwait(false);

        try
        {
            await AnsiConsole.Live(RenderCurrentView())
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .StartAsync(async context =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        ApplyIncomingEvents();
                        DrainLogs();
                        PollKeyboard();
                        context.UpdateTarget(RenderCurrentView());

                        try
                        {
                            await Task.Delay(_options.RefreshInterval, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                    ApplyIncomingEvents();
                    DrainLogs();
                    context.UpdateTarget(RenderCurrentView());
                }).ConfigureAwait(false);
        }
        finally
        {
            await FlushAndDisposeLoggerAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 处理事件通道中的所有待处理事件。
    /// </summary>
    private void ApplyIncomingEvents()
    {
        while (_events.Reader.TryRead(out var presentationEvent))
        {
            switch (presentationEvent)
            {
                case RegisterTaskEvent registerTaskEvent:
                    if (!_state.Tasks.ContainsKey(registerTaskEvent.Descriptor.TaskId))
                    {
                        _state.Tasks[registerTaskEvent.Descriptor.TaskId] = new TaskViewModel(registerTaskEvent.Descriptor);
                        _state.InvalidateTaskOrder();
                        TrimCompletedTasksIfNeeded();
                    }
                    break;
                case UpdateTaskStatusEvent updateTaskStatusEvent:
                    ApplyTaskStatus(updateTaskStatusEvent);
                    break;
                case TaskProgressEvent taskProgressEvent:
                    if (_state.Tasks.TryGetValue(taskProgressEvent.TaskId, out var progressTask))
                    {
                        progressTask.TransferredBytes = taskProgressEvent.TransferredBytes;
                        progressTask.TotalBytes = taskProgressEvent.TotalBytes;
                    }
                    break;
                case TaskSpeedEvent taskSpeedEvent:
                    if (_state.Tasks.TryGetValue(taskSpeedEvent.TaskId, out var speedTask))
                    {
                        speedTask.BytesPerSecond = taskSpeedEvent.BytesPerSecond;
                    }
                    break;
                case TaskCompletedEvent taskCompletedEvent:
                    if (_state.Tasks.TryGetValue(taskCompletedEvent.TaskId, out var completedTask))
                    {
                        completedTask.CompletedAt = DateTimeOffset.UtcNow;
                        completedTask.UpdatedAt = completedTask.CompletedAt.Value;
                        _state.InvalidateTaskOrder();
                    }
                    break;
                case TaskFailedEvent taskFailedEvent:
                    if (_state.Tasks.TryGetValue(taskFailedEvent.TaskId, out var failedTask))
                    {
                        failedTask.ErrorSummary = taskFailedEvent.ErrorSummary;
                        failedTask.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateTaskOrder();
                    }
                    break;
                case FolderCountersEvent folderCountersEvent:
                    if (_state.Tasks.TryGetValue(folderCountersEvent.TaskId, out var counterTask))
                    {
                        counterTask.FilesDone = folderCountersEvent.FilesDone;
                        counterTask.FilesTotal = folderCountersEvent.FilesTotal;
                        counterTask.FailedFiles = folderCountersEvent.FailedFiles;
                        counterTask.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateTaskOrder();
                    }
                    break;
                case RegisterFileEvent registerFileEvent:
                    ApplyRegisterFile(registerFileEvent);
                    break;
                case UpdateFileStatusEvent updateFileStatusEvent:
                    if (TryGetFile(updateFileStatusEvent.TaskId, updateFileStatusEvent.RelativePath, out var stateFile))
                    {
                        stateFile.Status = updateFileStatusEvent.Status;
                        stateFile.Message = updateFileStatusEvent.Message;
                        stateFile.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateFileOrder(updateFileStatusEvent.TaskId);
                    }
                    break;
                case FileProgressEvent fileProgressEvent:
                    if (TryGetFile(fileProgressEvent.TaskId, fileProgressEvent.RelativePath, out var progressFile))
                    {
                        progressFile.TransferredBytes = fileProgressEvent.TransferredBytes;
                        progressFile.TotalBytes = fileProgressEvent.TotalBytes;
                        progressFile.BytesPerSecond = fileProgressEvent.BytesPerSecond;
                        progressFile.Status = progressFile.Status == FileItemStatus.Pending ? FileItemStatus.Running : progressFile.Status;
                        if (progressFile.UpdatedAt == default && progressFile.Status == FileItemStatus.Running)
                        {
                            progressFile.UpdatedAt = DateTimeOffset.UtcNow;
                            _state.InvalidateFileOrder(fileProgressEvent.TaskId);
                        }
                    }
                    break;
                case FileCompletedEvent fileCompletedEvent:
                    if (TryGetFile(fileCompletedEvent.TaskId, fileCompletedEvent.RelativePath, out var completedFile))
                    {
                        completedFile.Status = FileItemStatus.Completed;
                        completedFile.TotalBytes = fileCompletedEvent.FileBytes;
                        completedFile.TransferredBytes = fileCompletedEvent.FileBytes;
                        completedFile.BytesPerSecond = 0;
                        completedFile.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateFileOrder(fileCompletedEvent.TaskId);
                    }
                    break;
                case FileFailedEvent fileFailedEvent:
                    if (TryGetFile(fileFailedEvent.TaskId, fileFailedEvent.RelativePath, out var failedFile))
                    {
                        failedFile.Status = FileItemStatus.Failed;
                        failedFile.ErrorCategory = fileFailedEvent.ErrorCategory;
                        failedFile.Message = fileFailedEvent.Message;
                        failedFile.Attempt = fileFailedEvent.Attempt;
                        failedFile.BytesPerSecond = 0;
                        failedFile.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateFileOrder(fileFailedEvent.TaskId);
                    }
                    break;
                case FileSkippedEvent fileSkippedEvent:
                    if (TryGetFile(fileSkippedEvent.TaskId, fileSkippedEvent.RelativePath, out var skippedFile))
                    {
                        skippedFile.Status = FileItemStatus.Skipped;
                        skippedFile.Message = fileSkippedEvent.Reason;
                        skippedFile.BytesPerSecond = 0;
                        skippedFile.UpdatedAt = DateTimeOffset.UtcNow;
                        _state.InvalidateFileOrder(fileSkippedEvent.TaskId);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 应用任务状态更新事件。
    /// </summary>
    /// <param name="presentationEvent">状态更新事件。</param>
    private void ApplyTaskStatus(UpdateTaskStatusEvent presentationEvent)
    {
        if (!_state.Tasks.TryGetValue(presentationEvent.TaskId, out var task))
        {
            return;
        }

        task.Status = presentationEvent.Status;
        task.StatusMessage = presentationEvent.Message;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        if (presentationEvent.Status is TaskStatus.Scanning or TaskStatus.Running)
        {
            task.StartedAt ??= task.UpdatedAt;
        }

        if (presentationEvent.Status is TaskStatus.Completed or TaskStatus.CompletedWithErrors or TaskStatus.Failed or TaskStatus.Canceled or TaskStatus.Skipped)
        {
            task.CompletedAt = DateTimeOffset.UtcNow;
        }

        _state.InvalidateTaskOrder();
    }

    /// <summary>
    /// 应用文件注册事件。
    /// </summary>
    /// <param name="presentationEvent">文件注册事件。</param>
    private void ApplyRegisterFile(RegisterFileEvent presentationEvent)
    {
        if (!_state.Tasks.TryGetValue(presentationEvent.TaskId, out var task))
        {
            return;
        }

        if (!task.Files.ContainsKey(presentationEvent.RelativePath))
        {
            task.Files[presentationEvent.RelativePath] = new FileViewModel(presentationEvent.RelativePath, presentationEvent.FileBytes);
            task.UpdatedAt = DateTimeOffset.UtcNow;
            _state.InvalidateTaskOrder();
            _state.InvalidateFileOrder(presentationEvent.TaskId);
        }
    }

    /// <summary>
    /// 尝试获取文件视图模型。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="file">输出文件视图模型。</param>
    /// <returns>是否获取成功。</returns>
    private bool TryGetFile(string taskId, string relativePath, out FileViewModel file)
    {
        file = null!;
        if (!_state.Tasks.TryGetValue(taskId, out var task))
        {
            return false;
        }

        if (!task.Files.TryGetValue(relativePath, out var foundFile))
        {
            return false;
        }

        file = foundFile;
        return true;
    }

    /// <summary>
    /// 从日志通道拉取日志并写入内存与文件。
    /// </summary>
    private void DrainLogs()
    {
        while (_logs.Reader.TryRead(out var entry))
        {
            if (entry.Level >= _options.TuiLogLevel)
            {
                _state.LogEntries.Add(entry);
            }

            if (_fileLogWriter is not null && entry.Level >= _options.FileLogLevel)
            {
                var scope = string.IsNullOrWhiteSpace(entry.Scope) ? "global" : entry.Scope;
                _fileLogWriter.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss zzz}] [{RenderText.GetLogLevelTag(entry.Level)}] [{scope}] {entry.Message}");
            }
        }
    }

    /// <summary>
    /// 轮询键盘输入并更新界面状态。
    /// </summary>
    private void PollKeyboard()
    {
        while (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(true);
            HandleKey(keyInfo);
        }
    }

    /// <summary>
    /// 处理按键逻辑。
    /// </summary>
    /// <param name="keyInfo">按键信息。</param>
    private void HandleKey(ConsoleKeyInfo keyInfo)
    {
        if (_state.ViewMode == DashboardViewMode.TaskDetails)
        {
            HandleDetailsKey(keyInfo);
            return;
        }

        if (keyInfo.Key == ConsoleKey.Enter)
        {
            OpenSelectedTaskDetails();
            return;
        }

        if ((keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
        {
            HandleSummaryKey(keyInfo.Key);
            return;
        }

        if ((keyInfo.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt)
        {
            HandleLogKey(keyInfo.Key);
            return;
        }

        HandleTaskListKey(keyInfo.Key);
    }

    /// <summary>
    /// 处理左列按键。
    /// </summary>
    /// <param name="key">按键代码。</param>
    private void HandleTaskListKey(ConsoleKey key)
    {
        var sortedTaskIds = _state.GetSortedTaskIds();
        if (sortedTaskIds.Count == 0)
        {
            _state.SelectedTaskIndex = 0;
            _state.TaskListOffset = 0;
            return;
        }

        switch (key)
        {
            case ConsoleKey.UpArrow:
                _state.SelectedTaskIndex = Math.Max(0, _state.SelectedTaskIndex - 1);
                break;
            case ConsoleKey.DownArrow:
                _state.SelectedTaskIndex = Math.Min(sortedTaskIds.Count - 1, _state.SelectedTaskIndex + 1);
                break;
            case ConsoleKey.PageUp:
                _state.SelectedTaskIndex = Math.Max(0, _state.SelectedTaskIndex - Math.Max(1, _state.TaskListPageSize));
                break;
            case ConsoleKey.PageDown:
                _state.SelectedTaskIndex = Math.Min(sortedTaskIds.Count - 1, _state.SelectedTaskIndex + Math.Max(1, _state.TaskListPageSize));
                break;
            case ConsoleKey.Home:
                _state.SelectedTaskIndex = 0;
                break;
            case ConsoleKey.End:
                _state.SelectedTaskIndex = sortedTaskIds.Count - 1;
                break;
        }

        EnsureTaskSelectionVisible(sortedTaskIds.Count);
    }

    /// <summary>
    /// 处理中列按键。
    /// </summary>
    /// <param name="key">按键代码。</param>
    private void HandleSummaryKey(ConsoleKey key)
    {
        var maxOffset = Math.Max(0, CalculateSummaryBodyRows(BuildSummarySections()) - _state.SummaryPageSize);
        switch (key)
        {
            case ConsoleKey.UpArrow:
                _state.SummaryOffset = Math.Max(0, _state.SummaryOffset - 1);
                break;
            case ConsoleKey.DownArrow:
                _state.SummaryOffset = Math.Min(maxOffset, _state.SummaryOffset + 1);
                break;
            case ConsoleKey.PageUp:
                _state.SummaryOffset = Math.Max(0, _state.SummaryOffset - Math.Max(1, _state.SummaryPageSize));
                break;
            case ConsoleKey.PageDown:
                _state.SummaryOffset = Math.Min(maxOffset, _state.SummaryOffset + Math.Max(1, _state.SummaryPageSize));
                break;
            case ConsoleKey.Home:
                _state.SummaryOffset = 0;
                break;
            case ConsoleKey.End:
                _state.SummaryOffset = maxOffset;
                break;
        }
    }

    /// <summary>
    /// 处理右列日志按键。
    /// </summary>
    /// <param name="key">按键代码。</param>
    private void HandleLogKey(ConsoleKey key)
    {
        var maxOffset = Math.Max(0, _state.LogEntries.Count - _state.LogPageSize);
        switch (key)
        {
            case ConsoleKey.UpArrow:
                _state.LogOffset = Math.Max(0, _state.LogOffset - 1);
                _state.AutoFollowLogs = false;
                break;
            case ConsoleKey.DownArrow:
                _state.LogOffset = Math.Min(maxOffset, _state.LogOffset + 1);
                _state.AutoFollowLogs = _state.LogOffset >= maxOffset;
                break;
            case ConsoleKey.PageUp:
                _state.LogOffset = Math.Max(0, _state.LogOffset - Math.Max(1, _state.LogPageSize));
                _state.AutoFollowLogs = false;
                break;
            case ConsoleKey.PageDown:
                _state.LogOffset = Math.Min(maxOffset, _state.LogOffset + Math.Max(1, _state.LogPageSize));
                _state.AutoFollowLogs = _state.LogOffset >= maxOffset;
                break;
            case ConsoleKey.Home:
                _state.LogOffset = 0;
                _state.AutoFollowLogs = false;
                break;
            case ConsoleKey.End:
                _state.LogOffset = maxOffset;
                _state.AutoFollowLogs = true;
                break;
        }
    }

    /// <summary>
    /// 处理详情页按键。
    /// </summary>
    /// <param name="keyInfo">按键信息。</param>
    private void HandleDetailsKey(ConsoleKeyInfo keyInfo)
    {
        if (keyInfo.Key == ConsoleKey.Escape)
        {
            _state.ViewMode = DashboardViewMode.Main;
            _state.DetailOffset = 0;
            return;
        }

        var files = _state.GetSelectedTaskFiles();
        var maxOffset = Math.Max(0, files.Count - _state.DetailPageSize);
        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                _state.DetailOffset = Math.Max(0, _state.DetailOffset - 1);
                break;
            case ConsoleKey.DownArrow:
                _state.DetailOffset = Math.Min(maxOffset, _state.DetailOffset + 1);
                break;
            case ConsoleKey.PageUp:
                _state.DetailOffset = Math.Max(0, _state.DetailOffset - Math.Max(1, _state.DetailPageSize));
                break;
            case ConsoleKey.PageDown:
                _state.DetailOffset = Math.Min(maxOffset, _state.DetailOffset + Math.Max(1, _state.DetailPageSize));
                break;
            case ConsoleKey.Home:
                _state.DetailOffset = 0;
                break;
            case ConsoleKey.End:
                _state.DetailOffset = maxOffset;
                break;
        }
    }

    /// <summary>
    /// 打开当前选中任务详情页。
    /// </summary>
    private void OpenSelectedTaskDetails()
    {
        var sortedTaskIds = _state.GetSortedTaskIds();
        if (sortedTaskIds.Count == 0)
        {
            return;
        }

        var safeIndex = Math.Clamp(_state.SelectedTaskIndex, 0, sortedTaskIds.Count - 1);
        _state.DetailsTaskId = sortedTaskIds[safeIndex];
        _state.DetailOffset = 0;
        _state.ViewMode = DashboardViewMode.TaskDetails;
    }

    /// <summary>
    /// 在任务数量超限时裁剪已终结任务。
    /// </summary>
    private void TrimCompletedTasksIfNeeded()
    {
        if (_state.Tasks.Count <= _options.MaxTasksKept)
        {
            return;
        }

        var removableTasks = _state.Tasks.Values
            .Where(task => task.Status is TaskStatus.Completed or TaskStatus.CompletedWithErrors or TaskStatus.Failed or TaskStatus.Canceled or TaskStatus.Skipped)
            .OrderBy(task => task.CompletedAt ?? task.UpdatedAt)
            .Select(task => task.Descriptor.TaskId)
            .ToList();
        var removeCount = Math.Min(removableTasks.Count, _state.Tasks.Count - _options.MaxTasksKept);
        for (var index = 0; index < removeCount; index++)
        {
            _state.RemoveTask(removableTasks[index]);
        }

        var sortedCount = _state.GetSortedTaskIds().Count;
        if (sortedCount == 0)
        {
            _state.SelectedTaskIndex = 0;
            _state.TaskListOffset = 0;
            return;
        }

        _state.SelectedTaskIndex = Math.Clamp(_state.SelectedTaskIndex, 0, sortedCount - 1);
        _state.TaskListOffset = Math.Clamp(_state.TaskListOffset, 0, Math.Max(0, sortedCount - _state.TaskListPageSize));
    }

    /// <summary>
    /// 确保左列选中项可见。
    /// </summary>
    /// <param name="taskCount">任务总数。</param>
    private void EnsureTaskSelectionVisible(int taskCount)
    {
        var pageSize = Math.Max(1, _state.TaskListPageSize);
        if (_state.SelectedTaskIndex < _state.TaskListOffset)
        {
            _state.TaskListOffset = _state.SelectedTaskIndex;
            return;
        }

        if (_state.SelectedTaskIndex >= _state.TaskListOffset + pageSize)
        {
            _state.TaskListOffset = Math.Min(_state.SelectedTaskIndex - pageSize + 1, Math.Max(0, taskCount - pageSize));
        }
    }
}


