using Zeayii.Flow.Presentation.Models;

namespace Zeayii.Flow.Presentation.Implementations;

/// <summary>
/// 提供固定容量的日志环形缓冲区。
/// </summary>
internal sealed class LogBuffer
{
    /// <summary>
    /// 日志存储数组。
    /// </summary>
    private LogEntry[] _items = [];

    /// <summary>
    /// 当前起始索引。
    /// </summary>
    private int _start;

    /// <summary>
    /// 当前日志数量。
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 设置缓冲区容量。
    /// </summary>
    /// <param name="capacity">目标容量。</param>
    public void SetCapacity(int capacity)
    {
        var safeCapacity = Math.Max(1, capacity);
        if (safeCapacity == _items.Length)
        {
            return;
        }

        var resized = new LogEntry[safeCapacity];
        var copyCount = Math.Min(safeCapacity, Count);
        for (var index = 0; index < copyCount; index++)
        {
            resized[index] = this[Count - copyCount + index];
        }

        _items = resized;
        _start = 0;
        Count = copyCount;
    }

    /// <summary>
    /// 追加日志条目。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    public void Add(LogEntry entry)
    {
        if (_items.Length == 0)
        {
            return;
        }

        if (Count < _items.Length)
        {
            _items[(_start + Count) % _items.Length] = entry;
            Count++;
            return;
        }

        _items[_start] = entry;
        _start = (_start + 1) % _items.Length;
    }

    /// <summary>
    /// 复制指定窗口中的日志条目。
    /// </summary>
    /// <param name="offset">起始偏移。</param>
    /// <param name="count">复制数量。</param>
    /// <returns>日志窗口快照。</returns>
    public IReadOnlyList<LogEntry> CopyWindow(int offset, int count)
    {
        if (Count == 0 || count <= 0)
        {
            return [];
        }

        var safeOffset = Math.Clamp(offset, 0, Math.Max(0, Count - 1));
        var safeCount = Math.Min(count, Count - safeOffset);
        if (safeCount <= 0)
        {
            return [];
        }

        var snapshot = new LogEntry[safeCount];
        for (var index = 0; index < safeCount; index++)
        {
            snapshot[index] = this[safeOffset + index];
        }

        return snapshot;
    }

    /// <summary>
    /// 获取逻辑位置上的日志条目。
    /// </summary>
    /// <param name="index">逻辑索引。</param>
    /// <returns>日志条目。</returns>
    private LogEntry this[int index] => (uint)index >= (uint)Count ? throw new ArgumentOutOfRangeException(nameof(index)) : _items[(_start + index) % _items.Length];
}

