using System.Collections.Concurrent;
using System.Text;
using Zeayii.Flow.Presentation.Abstractions;
using Zeayii.Flow.Presentation.Models;
using Zeayii.Flow.Presentation.Options;
using Spectre.Console;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 使用 Spectre.Console 渲染三列同步控制台界面的呈现层管理器。
/// </summary>
public sealed partial class SpectrePresentationManager : IPresentationManager, ITuiLogSink
{
    /// <summary>
    /// 任务事件队列容量。
    /// </summary>
    private const int EventChannelCapacity = 32_768;

    /// <summary>
    /// 日志队列容量。
    /// </summary>
    private const int LogChannelCapacity = 16_384;

    /// <summary>
    /// UI 待展示日志容量。
    /// </summary>
    private const int PendingUiLogCapacity = 16_384;

    /// <summary>
    /// 默认文件日志名前缀。
    /// </summary>
    private const string DefaultLogFilePrefix = "flow";

    /// <summary>
    /// 窗口尺寸重布局防抖时长。
    /// </summary>
    private static readonly TimeSpan LayoutDebounceWindow = TimeSpan.FromMilliseconds(180);

    /// <summary>
    /// 主界面底部帮助文本。
    /// </summary>
    private static readonly Markup MainFooterMarkup = new($"[{PresentationPalette.Muted}]Tasks[/] Up/Down/PgUp/PgDn/Home/End   [{PresentationPalette.Muted}]Summary[/] Ctrl+Up/Down/PgUp/PgDn/Home/End   [{PresentationPalette.Muted}]Logs[/] Alt+Up/Down/PgUp/PgDn/Home/End   [{PresentationPalette.Muted}]Details[/] Enter   [{PresentationPalette.Failure}]Exit[/] Ctrl+C");

    /// <summary>
    /// 详情页底部帮助文本。
    /// </summary>
    private static readonly Markup DetailsFooterMarkup = new($"[{PresentationPalette.Muted}]Files[/] Up/Down/PgUp/PgDn/Home/End   [{PresentationPalette.Muted}]Back[/] Esc   [{PresentationPalette.Failure}]Exit[/] Ctrl+C");

    /// <summary>
    /// Presentation 配置项。
    /// </summary>
    private readonly PresentationOptions _options;

    /// <summary>
    /// UI 事件队列。
    /// </summary>
    private readonly BlockingCollection<PresentationEvent> _events;

    /// <summary>
    /// 日志队列。
    /// </summary>
    private readonly BlockingCollection<LogEntry> _logs;

    /// <summary>
    /// 待展示到 UI 的日志队列。
    /// </summary>
    private readonly ConcurrentQueue<LogEntry> _pendingUiLogs = new();

    /// <summary>
    /// 内部状态容器。
    /// </summary>
    private readonly DashboardState _state;

    /// <summary>
    /// 内部停止令牌源。
    /// </summary>
    private readonly CancellationTokenSource _shutdownCts = new();

    /// <summary>
    /// 链接后的运行令牌源。
    /// </summary>
    private CancellationTokenSource? _linkedCts;

    /// <summary>
    /// UI 线程。
    /// </summary>
    private Thread? _uiThread;

    /// <summary>
    /// 日志线程。
    /// </summary>
    private Thread? _logThread;

    /// <summary>
    /// 当前是否已启动。
    /// </summary>
    private int _isStarted;

    /// <summary>
    /// UI 待展示日志当前数量。
    /// </summary>
    private int _pendingUiLogCount;

    /// <summary>
    /// 文件日志写入器。
    /// </summary>
    private StreamWriter? _fileLogWriter;

    /// <summary>
    /// 已应用的布局尺寸。
    /// </summary>
    private LayoutMetrics _activeLayout;

    /// <summary>
    /// 等待稳定的布局尺寸。
    /// </summary>
    private LayoutMetrics _pendingLayout;

    /// <summary>
    /// 待应用布局的首次观察时间。
    /// </summary>
    private DateTimeOffset _pendingLayoutObservedAt;

    /// <summary>
    /// 初始化呈现层管理器。
    /// </summary>
    /// <param name="options">呈现层配置项。</param>
    public SpectrePresentationManager(PresentationOptions options)
    {
        _options = options;
        _state = new DashboardState(options.DefaultPageSize);
        _events = new BlockingCollection<PresentationEvent>(EventChannelCapacity);
        _logs = new BlockingCollection<LogEntry>(LogChannelCapacity);
        _state.LogEntries.SetCapacity(_options.MaxLogEntries);
    }

    /// <summary>
    /// 启动呈现层主循环。
    /// </summary>
    /// <param name="ct">外部取消令牌。</param>
    /// <returns>完成值任务。</returns>
    public ValueTask StartAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _isStarted, 1) == 1)
        {
            return ValueTask.CompletedTask;
        }

        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _shutdownCts.Token);
        _logThread = new Thread(() => RunLogLoop(_linkedCts.Token))
        {
            IsBackground = true,
            Name = "flow-log-thread"
        };
        _uiThread = new Thread(() => RunDashboardLoop(_linkedCts.Token))
        {
            IsBackground = true,
            Name = "flow-ui-thread"
        };
        _logThread.Start();
        _uiThread.Start();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 停止呈现层主循环。
    /// </summary>
    /// <returns>完成值任务。</returns>
    public ValueTask StopAsync()
    {
        if (Interlocked.Exchange(ref _isStarted, 0) == 0)
        {
            return ValueTask.CompletedTask;
        }

        _shutdownCts.Cancel();
        _events.CompleteAdding();
        _logs.CompleteAdding();
        _uiThread?.Join();
        _logThread?.Join();
        _uiThread = null;
        _logThread = null;
        _linkedCts?.Dispose();
        _linkedCts = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 注册新的任务。
    /// </summary>
    /// <param name="descriptor">任务描述信息。</param>
    public void RegisterTask(TaskDescriptor descriptor) => EnqueueEvent(new RegisterTaskEvent(descriptor));

    /// <summary>
    /// 更新任务状态。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="status">任务状态。</param>
    /// <param name="message">状态消息。</param>
    public void UpdateTaskStatus(string taskId, TaskStatus status, string? message = null) => EnqueueEvent(new UpdateTaskStatusEvent(taskId, status, message));

    /// <summary>
    /// 汇报任务进度。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    public void ReportTaskProgress(string taskId, long transferredBytes, long? totalBytes) => EnqueueEvent(new TaskProgressEvent(taskId, transferredBytes, totalBytes));

    /// <summary>
    /// 汇报任务速度。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="bytesPerSecond">每秒字节数。</param>
    public void ReportTaskSpeed(string taskId, double bytesPerSecond) => EnqueueEvent(new TaskSpeedEvent(taskId, bytesPerSecond));

    /// <summary>
    /// 汇报任务完成。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    public void ReportTaskCompleted(string taskId) => EnqueueEvent(new TaskCompletedEvent(taskId));

    /// <summary>
    /// 汇报任务失败。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="errorSummary">错误摘要。</param>
    public void ReportTaskFailed(string taskId, string errorSummary) => EnqueueEvent(new TaskFailedEvent(taskId, errorSummary));

    /// <summary>
    /// 汇报目录任务计数。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="filesDone">完成文件数。</param>
    /// <param name="filesTotal">总文件数。</param>
    /// <param name="failedFiles">失败文件数。</param>
    public void ReportFolderCounters(string taskId, int filesDone, int filesTotal, int failedFiles) => EnqueueEvent(new FolderCountersEvent(taskId, filesDone, filesTotal, failedFiles));

    /// <summary>
    /// 注册文件条目。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="fileBytes">文件总字节数。</param>
    public void RegisterFile(string taskId, string relativePath, long fileBytes) => EnqueueEvent(new RegisterFileEvent(taskId, relativePath, fileBytes));

    /// <summary>
    /// 更新文件状态。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="status">文件状态。</param>
    /// <param name="message">状态消息。</param>
    public void UpdateFileStatus(string taskId, string relativePath, FileItemStatus status, string? message = null) => EnqueueEvent(new UpdateFileStatusEvent(taskId, relativePath, status, message));

    /// <summary>
    /// 汇报文件实时进度。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="transferredBytes">已传输字节数。</param>
    /// <param name="totalBytes">总字节数。</param>
    /// <param name="bytesPerSecond">实时速度。</param>
    public void ReportFileProgress(string taskId, string relativePath, long transferredBytes, long totalBytes, double bytesPerSecond) => EnqueueEvent(new FileProgressEvent(taskId, relativePath, transferredBytes, totalBytes, bytesPerSecond));

    /// <summary>
    /// 汇报文件完成。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="fileBytes">文件总字节数。</param>
    public void ReportFileCompleted(string taskId, string relativePath, long fileBytes) => EnqueueEvent(new FileCompletedEvent(taskId, relativePath, fileBytes));

    /// <summary>
    /// 汇报文件失败。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="errorCategory">错误分类。</param>
    /// <param name="message">错误消息。</param>
    /// <param name="attempt">尝试次数。</param>
    public void ReportFileFailed(string taskId, string relativePath, string errorCategory, string message, int attempt) => EnqueueEvent(new FileFailedEvent(taskId, relativePath, errorCategory, message, attempt));

    /// <summary>
    /// 汇报文件跳过。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="reason">跳过原因。</param>
    public void ReportFileSkipped(string taskId, string relativePath, string reason) => EnqueueEvent(new FileSkippedEvent(taskId, relativePath, reason));

    /// <summary>
    /// 记录 Trace 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Trace(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Trace, message, scope));

    /// <summary>
    /// 记录 Debug 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Debug(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Debug, message, scope));

    /// <summary>
    /// 记录 Information 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Information(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Information, message, scope));

    /// <summary>
    /// 记录 Warning 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Warning(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Warning, message, scope));

    /// <summary>
    /// 记录 Error 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Error(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Error, message, scope));

    /// <summary>
    /// 记录 Critical 级别日志。
    /// </summary>
    /// <param name="message">日志消息。</param>
    /// <param name="scope">日志作用域。</param>
    public void Critical(string message, string? scope = null) => AppendLog(new LogEntry(DateTimeOffset.Now, PresentationLogLevel.Critical, message, scope));

    /// <summary>
    /// 追加日志条目。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    public void AppendLog(LogEntry entry)
    {
        TryAddWithDropOldest(_logs, entry);
    }

    /// <summary>
    /// 将状态事件写入事件通道。
    /// </summary>
    /// <param name="presentationEvent">待写入的状态事件。</param>
    private void EnqueueEvent(PresentationEvent presentationEvent)
    {
        TryAddWithDropOldest(_events, presentationEvent);
    }

    /// <summary>
    /// 向有界队列追加元素，满时优先丢弃最旧元素，保证业务线程不阻塞。
    /// </summary>
    /// <param name="queue">目标队列。</param>
    /// <param name="item">待写入元素。</param>
    /// <typeparam name="T">元素类型。</typeparam>
    private static void TryAddWithDropOldest<T>(BlockingCollection<T> queue, T item)
    {
        if (queue.TryAdd(item))
        {
            return;
        }

        queue.TryTake(out _);
        queue.TryAdd(item);
    }

    /// <summary>
    /// 初始化文件日志写入器。
    /// </summary>
    private void InitializeFileLogger()
    {
        if (string.IsNullOrWhiteSpace(_options.LogDirectory) || _options.FileLogLevel == PresentationLogLevel.None)
        {
            return;
        }

        Directory.CreateDirectory(_options.LogDirectory);
        var files = new DirectoryInfo(_options.LogDirectory).GetFiles($"{DefaultLogFilePrefix}-*.log", SearchOption.TopDirectoryOnly).OrderByDescending(file => file.CreationTimeUtc).ToList();
        for (var index = 10; index < files.Count; index++)
        {
            try
            {
                files[index].Delete();
            }
            catch
            {
                // 忽略旧日志清理失败。
            }
        }

        var logFilePath = Path.Combine(_options.LogDirectory, $"{DefaultLogFilePrefix}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
        var stream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        _fileLogWriter = new StreamWriter(stream, new UTF8Encoding(false), 64 * 1024)
        {
            AutoFlush = false,
            NewLine = Environment.NewLine
        };
        _fileLogWriter.WriteLine($"# flow log started at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        _fileLogWriter.Flush();
    }

    /// <summary>
    /// 获取当前稳定布局尺寸。
    /// </summary>
    /// <returns>稳定后的布局尺寸。</returns>
    private LayoutMetrics GetStableLayout()
    {
        var observed = new LayoutMetrics(Math.Max(80, AnsiConsole.Profile.Width), Math.Max(20, AnsiConsole.Profile.Height));
        if (_activeLayout.Width == 0 || _activeLayout.Height == 0)
        {
            _activeLayout = observed;
            _pendingLayout = observed;
            _pendingLayoutObservedAt = DateTimeOffset.UtcNow;
            return observed;
        }

        if (observed.Equals(_activeLayout))
        {
            _pendingLayout = observed;
            _pendingLayoutObservedAt = DateTimeOffset.UtcNow;
            return _activeLayout;
        }

        var now = DateTimeOffset.UtcNow;
        if (!observed.Equals(_pendingLayout))
        {
            _pendingLayout = observed;
            _pendingLayoutObservedAt = now;
            return _activeLayout;
        }

        if (now - _pendingLayoutObservedAt < LayoutDebounceWindow)
        {
            return _activeLayout;
        }

        _activeLayout = observed;
        return _activeLayout;
    }

    /// <summary>
    /// 刷新并释放文件日志写入器。
    /// </summary>
    private void FlushAndDisposeLogger()
    {
        if (_fileLogWriter is null)
        {
            return;
        }

        _fileLogWriter.Flush();
        _fileLogWriter.Dispose();
        _fileLogWriter = null;
    }

    /// <summary>
    /// 表示布局宽高快照。
    /// </summary>
    /// <param name="Width">终端宽度。</param>
    /// <param name="Height">终端高度。</param>
    private readonly record struct LayoutMetrics(int Width, int Height);
}


