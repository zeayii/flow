using Zeayii.Flow.Presentation.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 提供 Spectre Presentation 的渲染逻辑。
/// </summary>
public sealed partial class SpectrePresentationManager
{
    /// <summary>
    /// 左列固定宽度。
    /// </summary>
    private const int LeftColumnWidth = 96;

    /// <summary>
    /// 中列固定宽度。
    /// </summary>
    private const int MiddleColumnWidth = 40;

    /// <summary>
    /// 左列状态字段宽度。
    /// </summary>
    private const int LeftStatusWidth = 6;

    /// <summary>
    /// 左列任务名称字段宽度。
    /// </summary>
    private const int LeftNameWidth = 20;

    /// <summary>
    /// 左列进度条字段宽度。
    /// </summary>
    private const int LeftProgressWidth = 12;

    /// <summary>
    /// 左列大小字段宽度。
    /// </summary>
    private const int LeftSizeWidth = 15;

    /// <summary>
    /// 左列速率字段宽度。
    /// </summary>
    private const int LeftSpeedWidth = 11;

    /// <summary>
    /// 左列耗时字段宽度。
    /// </summary>
    private const int LeftElapsedWidth = 8;

    /// <summary>
    /// 左列数量字段宽度。
    /// </summary>
    private const int LeftCountWidth = 4;

    /// <summary>
    /// 中列任务名称单元宽度。
    /// </summary>
    private const int MiddleNameWidth = 10;

    /// <summary>
    /// 中列每行任务名称列数。
    /// </summary>
    private const int MiddleGridColumns = 3;

    /// <summary>
    /// 标题栏摘要数量字段宽度。
    /// </summary>
    private const int HeaderCountWidth = 6;

    /// <summary>
    /// 标题栏摘要速率字段宽度。
    /// </summary>
    private const int HeaderSpeedWidth = 10;

    /// <summary>
    /// 渲染当前视图。
    /// </summary>
    /// <returns>可渲染对象。</returns>
    private IRenderable RenderCurrentView()
    {
        var layout = GetStableLayout();
        return _state.ViewMode == DashboardViewMode.Main
            ? RenderMainView(layout.Width, layout.Height)
            : RenderDetailsView(layout.Width, layout.Height);
    }

    /// <summary>
    /// 渲染主界面。
    /// </summary>
    /// <param name="width">终端宽度。</param>
    /// <param name="height">终端高度。</param>
    /// <returns>可渲染对象。</returns>
    private IRenderable RenderMainView(int width, int height)
    {
        var header = RenderHeader(width);
        var footer = Align.Center(MainFooterMarkup, VerticalAlignment.Middle);
        var contentHeight = Math.Max(8, height - 6);
        var columns = ComputeMainColumns(width);

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(columns.LeftWidth));
        grid.AddColumn(new GridColumn().Width(columns.MiddleWidth));
        grid.AddColumn(new GridColumn().Width(columns.RightWidth));
        grid.AddRow(
            RenderTaskListPanel(columns.LeftWidth, contentHeight),
            RenderSummaryPanel(columns.MiddleWidth, contentHeight),
            RenderLogPanel(columns.RightWidth, contentHeight));
        return new Rows(header, grid, footer);
    }

    /// <summary>
    /// 渲染详情页。
    /// </summary>
    /// <param name="width">终端宽度。</param>
    /// <param name="height">终端高度。</param>
    /// <returns>可渲染对象。</returns>
    private IRenderable RenderDetailsView(int width, int height)
    {
        var header = RenderHeader(width);
        var footer = Align.Center(DetailsFooterMarkup, VerticalAlignment.Middle);
        var summaryHeight = 9;
        var fileHeight = Math.Max(8, height - summaryHeight - 7);
        return new Rows(header, RenderDetailSummaryPanel(width, summaryHeight), RenderDetailFilesPanel(width, fileHeight), footer);
    }

    /// <summary>
    /// 渲染标题栏。
    /// </summary>
    /// <param name="width">终端宽度。</param>
    /// <returns>标题栏面板。</returns>
    private IRenderable RenderHeader(int width)
    {
        var summary = BuildHeaderSummary();
        var table = new Table().NoBorder().HideHeaders().Expand();
        table.AddColumn(new TableColumn(string.Empty).LeftAligned());
        table.AddColumn(new TableColumn(string.Empty).RightAligned());
        table.AddRow(
            new Markup(BuildHeaderConfigMarkup()),
            new Markup(summary));

        return new Panel(table)
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader(string.IsNullOrWhiteSpace(_options.HeaderRunModeText) ? " Flow Dashboard " : $" Flow Dashboard - {Markup.Escape(_options.HeaderRunModeText)} "),
            Width = width,
            Padding = new Padding(1, 0, 1, 0)
        };
    }

    /// <summary>
    /// 渲染左列任务列表面板。
    /// </summary>
    /// <param name="width">列宽。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>任务面板。</returns>
    private IRenderable RenderTaskListPanel(int width, int height)
    {
        var taskIds = _state.GetSortedTaskIds();
        var contentWidth = Math.Max(36, width - 4);
        var now = DateTimeOffset.UtcNow;
        var rows = new List<IRenderable>(Math.Max(8, height))
        {
            new Markup($"[grey]{RenderText.PadRightPlain("State", LeftStatusWidth)} {RenderText.PadRightPlain("Name", LeftNameWidth)} {RenderText.PadRightPlain("Progress", LeftProgressWidth)} {RenderText.PadRightPlain("Done/Total", LeftSizeWidth)} {RenderText.PadRightPlain("Speed", LeftSpeedWidth)} {RenderText.PadRightPlain("Time", LeftElapsedWidth)} {RenderText.PadLeftPlain("Done", LeftCountWidth)} {RenderText.PadLeftPlain("Total", LeftCountWidth)} {RenderText.PadLeftPlain("Fail", LeftCountWidth)}[/]"),
            new Markup($"[grey]{new string('─', contentWidth)}[/]")
        };

        var visibleRows = Math.Max(1, height - 5);
        _state.TaskListPageSize = visibleRows;
        var offset = Math.Clamp(_state.TaskListOffset, 0, Math.Max(0, taskIds.Count - visibleRows));
        _state.TaskListOffset = offset;

        if (offset > 0)
        {
            rows.Add(new Markup("[grey]↑ more[/]"));
        }

        for (var index = 0; index < visibleRows && offset + index < taskIds.Count; index++)
        {
            var task = _state.Tasks[taskIds[offset + index]];
            var elapsedText = RenderText.FormatElapsed(task.StartedAt, task.CompletedAt, now);
            var plainLine = RenderText.PadRightPlain(string.Join(' ',
                RenderText.PadRightPlain(RenderText.FormatTaskStatusLabel(task.Status), LeftStatusWidth),
                RenderText.TruncateAndPad(task.Descriptor.DisplayName, LeftNameWidth),
                new string(' ', LeftProgressWidth),
                RenderText.PadRightPlain($"{RenderText.FormatBytes(task.TransferredBytes)}/{RenderText.FormatBytes(task.TotalBytes ?? 0)}", LeftSizeWidth),
                RenderText.PadLeftPlain(RenderText.FormatBytesPerSecond(task.BytesPerSecond), LeftSpeedWidth),
                RenderText.PadRightPlain(elapsedText, LeftElapsedWidth),
                RenderText.FormatBoundedCount(task.FilesDone, LeftCountWidth),
                RenderText.FormatBoundedCount(task.FilesTotal, LeftCountWidth),
                RenderText.FormatBoundedCount(task.FailedFiles, LeftCountWidth)), contentWidth);

            var progressColor = task.Status switch
            {
                TaskStatus.Completed => RenderText.GetTaskStatusColorName(TaskStatus.Completed),
                TaskStatus.Skipped => RenderText.GetTaskStatusColorName(TaskStatus.Skipped),
                TaskStatus.Failed => RenderText.GetTaskStatusColorName(TaskStatus.Failed),
                TaskStatus.CompletedWithErrors => RenderText.GetTaskStatusColorName(TaskStatus.CompletedWithErrors),
                _ => RenderText.GetTaskStatusColorName(TaskStatus.Running)
            };
            var progressMarkup = RenderText.BuildProgressBar(task.TransferredBytes, task.TotalBytes, LeftProgressWidth, progressColor);
            var markupLine = RenderText.GetFixedWidthMarkup(string.Join(" ",
                RenderText.ColorizePlain(RenderText.PadRightPlain(RenderText.FormatTaskStatusLabel(task.Status), LeftStatusWidth), RenderText.GetTaskStatusColorName(task.Status)),
                Markup.Escape(RenderText.TruncateAndPad(task.Descriptor.DisplayName, LeftNameWidth)),
                progressMarkup,
                Markup.Escape(RenderText.PadRightPlain($"{RenderText.FormatBytes(task.TransferredBytes)}/{RenderText.FormatBytes(task.TotalBytes ?? 0)}", LeftSizeWidth)),
                Markup.Escape(RenderText.PadLeftPlain(RenderText.FormatBytesPerSecond(task.BytesPerSecond), LeftSpeedWidth)),
                Markup.Escape(RenderText.PadRightPlain(elapsedText, LeftElapsedWidth)),
                Markup.Escape(RenderText.FormatBoundedCount(task.FilesDone, LeftCountWidth)),
                Markup.Escape(RenderText.FormatBoundedCount(task.FilesTotal, LeftCountWidth)),
                $"[red]{Markup.Escape(RenderText.FormatBoundedCount(task.FailedFiles, LeftCountWidth))}[/]"), contentWidth);

            if (offset + index == _state.SelectedTaskIndex)
            {
                rows.Add(new Markup($"[black on grey84]{Markup.Escape(plainLine)}[/]"));
            }
            else
            {
                rows.Add(new Markup(markupLine));
            }
        }

        if (offset + visibleRows < taskIds.Count)
        {
            rows.Add(new Markup("[grey]↓ more[/]"));
        }

        if (taskIds.Count == 0)
        {
            rows.Add(new Markup("[grey]No tasks[/]"));
        }

        return new Panel(new Rows(rows.ToArray()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader(" Tasks "),
            Width = width,
            Height = height,
            Padding = new Padding(0, 0, 0, 0)
        };
    }

    /// <summary>
    /// 渲染中列任务分布面板。
    /// </summary>
    /// <param name="width">列宽。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>摘要面板。</returns>
    private IRenderable RenderSummaryPanel(int width, int height)
    {
        var rows = new List<IRenderable>(Math.Max(8, height));
        var sections = BuildSummarySections();
        var visibleRows = Math.Max(1, height - 2);
        _state.SummaryPageSize = visibleRows;
        var totalRows = CalculateSummaryBodyRows(sections);
        var offset = Math.Clamp(_state.SummaryOffset, 0, Math.Max(0, totalRows - visibleRows));
        _state.SummaryOffset = offset;
        var rowCursor = 0;
        var consumedRows = 0;

        if (offset > 0)
        {
            rows.Add(new Markup("[grey]↑ more[/]"));
            consumedRows++;
        }

        foreach (var section in sections)
        {
            if (consumedRows >= visibleRows)
            {
                break;
            }

            var headerRowIndex = rowCursor;
            rowCursor++;
            if (headerRowIndex >= offset && consumedRows < visibleRows)
            {
                rows.Add(new Markup($"[{section.ColorName}]{Markup.Escape(section.Title)}[/]"));
                consumedRows++;
            }

            foreach (var rowMarkup in BuildSummaryGridRows(section, width))
            {
                if (consumedRows >= visibleRows)
                {
                    break;
                }

                var bodyRowIndex = rowCursor;
                rowCursor++;
                if (bodyRowIndex < offset)
                {
                    continue;
                }

                rows.Add(new Markup(rowMarkup));
                consumedRows++;
            }
        }

        if (offset + visibleRows < totalRows)
        {
            rows.Add(new Markup("[grey]↓ more[/]"));
        }

        if (totalRows == 0)
        {
            rows.Add(new Markup("[grey]No tasks[/]"));
        }

        return new Panel(new Rows(rows.ToArray()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader(" Status "),
            Width = width,
            Height = height,
            Padding = new Padding(0, 0, 0, 0)
        };
    }

    /// <summary>
    /// 渲染右列日志面板。
    /// </summary>
    /// <param name="width">列宽。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>日志面板。</returns>
    private IRenderable RenderLogPanel(int width, int height)
    {
        var contentWidth = Math.Max(16, width - 4);
        var rows = new List<IRenderable>(Math.Max(8, height));
        var visibleRows = Math.Max(1, height - 4);
        _state.LogPageSize = visibleRows;

        if (_state.AutoFollowLogs)
        {
            _state.LogOffset = Math.Max(0, _state.LogEntries.Count - visibleRows);
        }

        var offset = Math.Clamp(_state.LogOffset, 0, Math.Max(0, _state.LogEntries.Count - visibleRows));
        _state.LogOffset = offset;

        if (offset > 0)
        {
            rows.Add(new Markup("[grey]↑ more[/]"));
        }

        var entries = _state.LogEntries.CopyWindow(offset, visibleRows);
        foreach (var entry in entries)
        {
            var prefix = $"{entry.Timestamp:HH:mm:ss} [{RenderText.GetLogLevelTag(entry.Level)}]";
            var prefixWidth = RenderText.GetDisplayWidth(prefix);
            var message = string.IsNullOrWhiteSpace(entry.Scope) ? entry.Message : $"({entry.Scope}) {entry.Message}";
            rows.Add(new Markup($"[grey]{Markup.Escape(prefix)}[/] [{RenderText.GetLogLevelColorName(entry.Level)}]{Markup.Escape(RenderText.TruncateAndPad(message, Math.Max(4, contentWidth - prefixWidth - 1)))}[/]"));
        }

        if (offset + visibleRows < _state.LogEntries.Count)
        {
            rows.Add(new Markup("[grey]↓ more[/]"));
        }

        if (_state.LogEntries.Count == 0)
        {
            rows.Add(new Markup("[grey]No logs[/]"));
        }

        return new Panel(new Rows(rows.ToArray()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader(" Logs "),
            Width = width,
            Height = height,
            Padding = new Padding(0, 0, 0, 0)
        };
    }

    /// <summary>
    /// 渲染详情页摘要面板。
    /// </summary>
    /// <param name="width">面板宽度。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>摘要面板。</returns>
    private IRenderable RenderDetailSummaryPanel(int width, int height)
    {
        if (_state.DetailsTaskId is null || !_state.Tasks.TryGetValue(_state.DetailsTaskId, out var task))
        {
            return new Panel(new Markup("[grey]No selected task[/]")) { Border = BoxBorder.Rounded, Header = new PanelHeader(" Task Details "), Width = width, Height = height };
        }

        var files = _state.GetSelectedTaskFiles();
        var failedFiles = files.Where(file => file.Status == FileItemStatus.Failed).Take(Math.Min(_options.VisibleFailuresInDetail, _options.MaxFailuresPerTask)).Select(file => file.RelativePath).ToArray();
        var completedFiles = files.Where(file => file.Status == FileItemStatus.Completed).Take(Math.Min(_options.VisibleRecentCompletedFilesInDetail, _options.MaxRecentCompletedFilesPerFolderTask)).Select(file => file.RelativePath).ToArray();
        var rows = new Rows(
            new Markup($"[grey]Name[/]: {Markup.Escape(task.Descriptor.DisplayName)}"),
            new Markup($"[grey]Status[/]: [{RenderText.GetTaskStatusColorName(task.Status)}]{Markup.Escape(RenderText.FormatTaskStatusLabel(task.Status))}[/]  [grey]Speed[/]: [green]{Markup.Escape(RenderText.FormatBytesPerSecond(task.BytesPerSecond))}[/]"),
            new Markup($"[grey]Size[/]: [green]{Markup.Escape(RenderText.FormatBytes(task.TransferredBytes))}[/] / [green]{Markup.Escape(RenderText.FormatBytes(task.TotalBytes ?? 0))}[/]"),
            new Markup($"[grey]Files[/]: [green]{task.FilesDone}[/] / [grey]{task.FilesTotal}[/] / [red]{task.FailedFiles}[/]"),
            new Markup($"[grey]Source[/]: {Markup.Escape(task.Descriptor.SourcePath)}"),
            new Markup($"[grey]Destination[/]: {Markup.Escape(task.Descriptor.DestinationPath)}"),
            new Markup($"[grey]Failed Sample[/]: {Markup.Escape(failedFiles.Length == 0 ? "-" : string.Join(", ", failedFiles))}"),
            new Markup($"[grey]Done Sample[/]: {Markup.Escape(completedFiles.Length == 0 ? "-" : string.Join(", ", completedFiles))}"),
            new Markup($"[grey]Message[/]: {Markup.Escape(task.StatusMessage ?? task.ErrorSummary ?? "-")}")
        );

        return new Panel(rows)
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader($" Task Details - {Markup.Escape(task.Descriptor.DisplayName)} "),
            Width = width,
            Height = height,
            Padding = new Padding(1, 0, 1, 0)
        };
    }

    /// <summary>
    /// 渲染详情页文件面板。
    /// </summary>
    /// <param name="width">面板宽度。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>文件面板。</returns>
    private IRenderable RenderDetailFilesPanel(int width, int height)
    {
        var files = _state.GetSelectedTaskFiles();
        var contentWidth = Math.Max(40, width - 4);
        const int statusWidth = 8;
        const int sizeWidth = 10;
        const int doneWidth = 10;
        const int speedWidth = 11;
        const int errorWidth = 18;
        var pathWidth = Math.Max(8, contentWidth - statusWidth - sizeWidth - doneWidth - speedWidth - errorWidth - 5);
        var rows = new List<IRenderable>(Math.Max(8, height))
        {
            new Markup($"[grey]{RenderText.PadRightPlain("Status", statusWidth)} {RenderText.PadRightPlain("Path", pathWidth)} {RenderText.PadLeftPlain("Size", sizeWidth)} {RenderText.PadLeftPlain("Done", doneWidth)} {RenderText.PadLeftPlain("Speed", speedWidth)} {RenderText.PadRightPlain("Error", errorWidth)}[/]"),
            new Markup(new string('─', contentWidth))
        };

        var visibleRows = Math.Max(1, height - 5);
        _state.DetailPageSize = visibleRows;
        var offset = Math.Clamp(_state.DetailOffset, 0, Math.Max(0, files.Count - visibleRows));
        _state.DetailOffset = offset;

        if (offset > 0)
        {
            rows.Add(new Markup("[grey]↑ more[/]"));
        }

        foreach (var file in files.Skip(offset).Take(visibleRows))
        {
            var errorText = file.Status == FileItemStatus.Failed ? (file.Message ?? file.ErrorCategory ?? "-") : (file.Message ?? "-");
            rows.Add(new Markup(string.Join(' ',
                RenderText.ColorizePlain(RenderText.PadRightPlain(RenderText.FormatFileStatusLabel(file.Status), statusWidth), RenderText.GetFileStatusColorName(file.Status)),
                Markup.Escape(RenderText.TruncateAndPad(file.RelativePath, pathWidth)),
                Markup.Escape(RenderText.PadLeftPlain(RenderText.FormatBytes(file.TotalBytes), sizeWidth)),
                Markup.Escape(RenderText.PadLeftPlain(RenderText.FormatBytes(file.TransferredBytes), doneWidth)),
                Markup.Escape(RenderText.PadLeftPlain(RenderText.FormatBytesPerSecond(file.BytesPerSecond), speedWidth)),
                Markup.Escape(RenderText.TruncateAndPad(errorText, errorWidth)))));
        }

        if (offset + visibleRows < files.Count)
        {
            rows.Add(new Markup("[grey]↓ more[/]"));
        }

        if (files.Count == 0)
        {
            rows.Add(new Markup("[grey]No files[/]"));
        }

        return new Panel(new Rows(rows.ToArray()))
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader(" Files "),
            Width = width,
            Height = height,
            Padding = new Padding(0, 0, 0, 0)
        };
    }

    /// <summary>
    /// 构建标题栏左侧配置文本。
    /// </summary>
    /// <returns>配置区域标记文本。</returns>
    private string BuildHeaderConfigMarkup()
    {
        return string.Join("  ",
            $"[grey]F[/] [bold yellow]{Markup.Escape(_options.HeaderFailurePolicy)}[/]",
            $"[grey]T[/] [bold white]{_options.HeaderTaskConcurrency}[/]",
            $"[grey]I[/] [bold white]{_options.HeaderInnerConcurrency}[/]",
            $"[grey]R[/] [bold white]{_options.HeaderRetryAttempts}[/]",
            $"[grey]B[/] [bold green]{Markup.Escape(_options.HeaderBlockSizeText)}[/]",
            $"[grey]C[/] [bold green]{Markup.Escape(_options.HeaderConflictPolicy)}[/]",
            $"[grey]L[/] [bold green]{Markup.Escape(_options.FileLogLevel.ToString())}[/]");
    }

    /// <summary>
    /// 构建标题栏右侧实时摘要文本。
    /// </summary>
    /// <returns>摘要区域标记文本。</returns>
    private string BuildHeaderSummary()
    {
        var counts = GetTaskCounts();
        var totalSpeed = _state.Tasks.Values.Sum(task => task.BytesPerSecond);
        return string.Join("  ",
            BuildSummaryBadge("R", counts.Running, "deepskyblue1"),
            BuildSummaryBadge("F", counts.Failed, "red"),
            BuildSummaryBadge("P", counts.Pending, "yellow"),
            BuildSummaryBadge("D", counts.Completed, "green"),
            BuildSummaryBadge("S", counts.Skipped, RenderText.GetTaskStatusColorName(TaskStatus.Skipped)),
            BuildSummaryBadge("T", counts.Total, "white"),
            $"[green]V[/] {Markup.Escape(RenderText.FormatFixedSpeed(totalSpeed, HeaderSpeedWidth))}");
    }

    /// <summary>
    /// 构建标题栏中的单个摘要徽标。
    /// </summary>
    /// <param name="label">摘要标签。</param>
    /// <param name="value">摘要值。</param>
    /// <param name="colorName">颜色名称。</param>
    /// <returns>摘要标记文本。</returns>
    private static string BuildSummaryBadge(string label, long value, string colorName)
    {
        return $"[{colorName}]{label}[/] {Markup.Escape(RenderText.FormatBoundedCount(value, HeaderCountWidth))}";
    }

    /// <summary>
    /// 统计当前任务分布数量。
    /// </summary>
    /// <returns>任务数量快照。</returns>
    private TaskCountSummary GetTaskCounts()
    {
        var summary = new TaskCountSummary();
        foreach (var task in _state.Tasks.Values)
        {
            summary.Total++;
            switch (task.Status)
            {
                case TaskStatus.Running:
                    summary.Running++;
                    break;
                case TaskStatus.Failed:
                case TaskStatus.CompletedWithErrors:
                    summary.Failed++;
                    break;
                case TaskStatus.Pending:
                case TaskStatus.Scanning:
                    summary.Pending++;
                    break;
                case TaskStatus.Completed:
                    summary.Completed++;
                    break;
                case TaskStatus.Skipped:
                case TaskStatus.Canceled:
                    summary.Skipped++;
                    break;
            }
        }

        return summary;
    }

    /// <summary>
    /// 构建中列任务分组。
    /// </summary>
    /// <returns>状态分组集合。</returns>
    private List<SummarySection> BuildSummarySections()
    {
        var taskIds = _state.GetSortedTaskIds();
        var sections = new List<SummarySection>
        {
            new("RUNNING", "deepskyblue1"),
            new("FAILED", "red"),
            new("PENDING", "yellow"),
            new("DONE", "green"),
            new("SKIPPED", RenderText.GetTaskStatusColorName(TaskStatus.Skipped))
        };

        foreach (var taskId in taskIds)
        {
            var task = _state.Tasks[taskId];
            var index = task.Status switch
            {
                TaskStatus.Running => 0,
                TaskStatus.Failed or TaskStatus.CompletedWithErrors => 1,
                TaskStatus.Pending or TaskStatus.Scanning => 2,
                TaskStatus.Completed => 3,
                TaskStatus.Skipped or TaskStatus.Canceled => 4,
                _ => 4
            };
            sections[index].TaskNames.Add(RenderText.TruncateAndPad(task.Descriptor.DisplayName, MiddleNameWidth));
        }

        return sections.Where(section => section.TaskNames.Count > 0).ToList();
    }

    /// <summary>
    /// 计算中列正文总行数。
    /// </summary>
    /// <param name="sections">状态分组集合。</param>
    /// <returns>总行数。</returns>
    private static int CalculateSummaryBodyRows(IEnumerable<SummarySection> sections)
    {
        var totalRows = 0;
        foreach (var section in sections)
        {
            totalRows++;
            totalRows += (int)Math.Ceiling(section.TaskNames.Count / (double)MiddleGridColumns);
        }

        return totalRows;
    }

    /// <summary>
    /// 构建中列某个分组的网格行。
    /// </summary>
    /// <param name="section">状态分组。</param>
    /// <param name="width">列宽。</param>
    /// <returns>网格行集合。</returns>
    private static IEnumerable<string> BuildSummaryGridRows(SummarySection section, int width)
    {
        var contentWidth = Math.Max(8, width - 4);
        var separatorWidth = 1;
        var effectiveColumns = MiddleGridColumns;
        var totalRequiredWidth = effectiveColumns * MiddleNameWidth + (effectiveColumns - 1) * separatorWidth;
        if (contentWidth < totalRequiredWidth)
        {
            effectiveColumns = Math.Max(1, (contentWidth + separatorWidth) / (MiddleNameWidth + separatorWidth));
        }

        var rows = new List<string>();
        for (var index = 0; index < section.TaskNames.Count; index += effectiveColumns)
        {
            var cells = new List<string>(effectiveColumns);
            for (var column = 0; column < effectiveColumns; column++)
            {
                var itemIndex = index + column;
                var name = itemIndex < section.TaskNames.Count
                    ? section.TaskNames[itemIndex]
                    : new string(' ', MiddleNameWidth);
                cells.Add($"[{section.ColorName}]{Markup.Escape(name)}[/]");
            }

            rows.Add(string.Join(" ", cells));
        }

        return rows;
    }

    /// <summary>
    /// 计算主界面三列宽度，并保证右列自适应。
    /// </summary>
    /// <param name="width">终端宽度。</param>
    /// <returns>三列宽度结果。</returns>
    private static MainColumnLayout ComputeMainColumns(int width)
    {
        const int gap = 2;
        var leftWidth = LeftColumnWidth;
        var middleWidth = MiddleColumnWidth;
        var minimumRightWidth = 24;
        var available = Math.Max(leftWidth + middleWidth + minimumRightWidth, width - gap);
        var rightWidth = available - leftWidth - middleWidth;
        return new MainColumnLayout(leftWidth, middleWidth, rightWidth);
    }

    /// <summary>
    /// 表示主界面三列宽度。
    /// </summary>
    /// <param name="LeftWidth">左列宽度。</param>
    /// <param name="MiddleWidth">中列宽度。</param>
    /// <param name="RightWidth">右列宽度。</param>
    private readonly record struct MainColumnLayout(int LeftWidth, int MiddleWidth, int RightWidth);

    /// <summary>
    /// 表示任务数量摘要。
    /// </summary>
    private sealed class TaskCountSummary
    {
        /// <summary>
        /// 运行中任务数量。
        /// </summary>
        public long Running { get; set; }

        /// <summary>
        /// 失败任务数量。
        /// </summary>
        public long Failed { get; set; }

        /// <summary>
        /// 待处理任务数量。
        /// </summary>
        public long Pending { get; set; }

        /// <summary>
        /// 已完成任务数量。
        /// </summary>
        public long Completed { get; set; }

        /// <summary>
        /// 已跳过任务数量。
        /// </summary>
        public long Skipped { get; set; }

        /// <summary>
        /// 总任务数量。
        /// </summary>
        public long Total { get; set; }
    }

    /// <summary>
    /// 表示中列状态分组。
    /// </summary>
    /// <param name="Title">分组标题。</param>
    /// <param name="ColorName">分组颜色。</param>
    private sealed record SummarySection(string Title, string ColorName)
    {
        /// <summary>
        /// 分组内的任务名称集合。
        /// </summary>
        public List<string> TaskNames { get; } = [];
    }
}



