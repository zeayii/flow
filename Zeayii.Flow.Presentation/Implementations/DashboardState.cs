using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 表示 Spectre Dashboard 的单线程状态容器。
/// </summary>
internal sealed class DashboardState
{
    /// <summary>
    /// 主界面可聚焦区域。
    /// </summary>
    internal enum DashboardFocusRegion
    {
        Tasks = 0,
        Summary = 1,
        Logs = 2
    }

    /// <summary>
    /// 任务排序缓存版本号。
    /// </summary>
    private int _taskVersion;

    /// <summary>
    /// 任务排序缓存命中的版本号。
    /// </summary>
    private int _taskCacheVersion = -1;

    /// <summary>
    /// 文件排序缓存版本号表。
    /// </summary>
    private readonly Dictionary<string, int> _fileVersions = new(StringComparer.Ordinal);

    /// <summary>
    /// 文件排序缓存表。
    /// </summary>
    private readonly Dictionary<string, CachedFileList> _fileCache = new(StringComparer.Ordinal);

    /// <summary>
    /// 任务排序缓存。
    /// </summary>
    private List<string> _sortedTaskIdsCache = [];

    /// <summary>
    /// 初始化状态容器。
    /// </summary>
    /// <param name="defaultPageSize">默认分页大小。</param>
    public DashboardState(int defaultPageSize)
    {
        var safePageSize = Math.Max(1, defaultPageSize);
        TaskListPageSize = safePageSize;
        SummaryPageSize = safePageSize;
        LogPageSize = safePageSize;
        DetailPageSize = safePageSize;
    }

    /// <summary>
    /// 任务集合。
    /// </summary>
    public Dictionary<string, TaskViewModel> Tasks { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 日志环形缓冲区。
    /// </summary>
    public LogBuffer LogEntries { get; } = new();

    /// <summary>
    /// 当前页面模式。
    /// </summary>
    public DashboardViewMode ViewMode { get; set; } = DashboardViewMode.Main;

    /// <summary>
    /// 当前焦点区域。
    /// </summary>
    public DashboardFocusRegion ActiveRegion { get; set; } = DashboardFocusRegion.Tasks;

    /// <summary>
    /// 是否等待 Enter 确认退出。
    /// </summary>
    public bool ExitPending { get; set; }

    /// <summary>
    /// 左列选中任务索引。
    /// </summary>
    public int SelectedTaskIndex { get; set; }

    /// <summary>
    /// 左列滚动偏移。
    /// </summary>
    public int TaskListOffset { get; set; }

    /// <summary>
    /// 左列页大小。
    /// </summary>
    public int TaskListPageSize { get; set; }

    /// <summary>
    /// 中列滚动偏移。
    /// </summary>
    public int SummaryOffset { get; set; }

    /// <summary>
    /// 中列页大小。
    /// </summary>
    public int SummaryPageSize { get; set; }

    /// <summary>
    /// 右列日志滚动偏移。
    /// </summary>
    public int LogOffset { get; set; }

    /// <summary>
    /// 右列日志页大小。
    /// </summary>
    public int LogPageSize { get; set; }

    /// <summary>
    /// 是否自动跟随日志底部。
    /// </summary>
    public bool AutoFollowLogs { get; set; } = true;

    /// <summary>
    /// 当前详情页任务标识。
    /// </summary>
    public string? DetailsTaskId { get; set; }

    /// <summary>
    /// 详情页滚动偏移。
    /// </summary>
    public int DetailOffset { get; set; }

    /// <summary>
    /// 详情页页大小。
    /// </summary>
    public int DetailPageSize { get; set; }

    /// <summary>
    /// 获取按统一规则排序后的任务标识集合。
    /// </summary>
    /// <returns>排序后的任务标识列表。</returns>
    public IReadOnlyList<string> GetSortedTaskIds()
    {
        if (_taskCacheVersion == _taskVersion)
        {
            return _sortedTaskIdsCache;
        }

        _sortedTaskIdsCache = Tasks.Values
            .OrderBy(task => GetTaskSortGroup(task.Status))
            .ThenBy(task => task.Descriptor.CreatedAt)
            .ThenBy(task => task.Descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(task => task.Descriptor.TaskId)
            .ToList();
        _taskCacheVersion = _taskVersion;
        return _sortedTaskIdsCache;
    }

    /// <summary>
    /// 获取当前详情页任务的文件集合。
    /// </summary>
    /// <returns>排序后的文件视图模型列表。</returns>
    public IReadOnlyList<FileViewModel> GetSelectedTaskFiles()
    {
        if (DetailsTaskId is null || !Tasks.TryGetValue(DetailsTaskId, out var task))
        {
            return [];
        }

        var version = GetFileVersion(DetailsTaskId);
        if (_fileCache.TryGetValue(DetailsTaskId, out var cache) && cache.Version == version)
        {
            return cache.Files;
        }

        var files = task.Files.Values
            .OrderBy(file => GetFileSortGroup(file.Status))
            .ThenBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _fileCache[DetailsTaskId] = new CachedFileList(version, files);
        return files;
    }

    /// <summary>
    /// 标记任务列表发生变化。
    /// </summary>
    public void InvalidateTaskOrder()
    {
        _taskVersion++;
    }

    /// <summary>
    /// 标记指定任务的文件列表发生变化。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    public void InvalidateFileOrder(string taskId)
    {
        _fileVersions[taskId] = GetFileVersion(taskId) + 1;
    }

    /// <summary>
    /// 从状态容器中移除指定任务。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    public void RemoveTask(string taskId)
    {
        Tasks.Remove(taskId);
        _fileVersions.Remove(taskId);
        _fileCache.Remove(taskId);
        if (DetailsTaskId == taskId)
        {
            DetailsTaskId = null;
            ViewMode = DashboardViewMode.Main;
            DetailOffset = 0;
        }

        InvalidateTaskOrder();
    }

    /// <summary>
    /// 获取指定任务的文件缓存版本号。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <returns>版本号。</returns>
    private int GetFileVersion(string taskId)
    {
        return _fileVersions.GetValueOrDefault(taskId, 0);
    }

    /// <summary>
    /// 获取任务排序分组。
    /// </summary>
    /// <param name="status">任务状态。</param>
    /// <returns>排序分组值。</returns>
    private static int GetTaskSortGroup(TaskStatus status)
    {
        return status switch
        {
            TaskStatus.Running => 0,
            TaskStatus.Failed or TaskStatus.CompletedWithErrors => 1,
            TaskStatus.Pending or TaskStatus.Scanning => 2,
            TaskStatus.Completed => 3,
            TaskStatus.Skipped or TaskStatus.Canceled => 4,
            _ => 5
        };
    }

    /// <summary>
    /// 获取文件排序分组。
    /// </summary>
    /// <param name="status">文件状态。</param>
    /// <returns>排序分组值。</returns>
    private static int GetFileSortGroup(FileItemStatus status)
    {
        return status switch
        {
            FileItemStatus.Running => 0,
            FileItemStatus.Failed => 1,
            FileItemStatus.Pending => 2,
            FileItemStatus.Completed => 3,
            FileItemStatus.Skipped or FileItemStatus.Canceled => 4,
            _ => 5
        };
    }

    /// <summary>
    /// 表示文件排序缓存条目。
    /// </summary>
    /// <param name="Version">缓存版本号。</param>
    /// <param name="Files">缓存文件列表。</param>
    private readonly record struct CachedFileList(int Version, List<FileViewModel> Files);
}


