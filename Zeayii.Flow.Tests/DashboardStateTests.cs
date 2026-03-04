using Zeayii.Flow.Presentation.Implementations;
using Zeayii.Flow.Presentation.Models;
using TaskStatus = Zeayii.Flow.Presentation.Models.TaskStatus;

namespace Zeayii.Flow.Tests;

/// <summary>
/// 校验 Dashboard 状态排序规则的测试集合。
/// </summary>
public sealed class DashboardStateTests
{
    /// <summary>
    /// 验证任务排序遵循 Running、Failed、Pending、Completed、Skipped 的统一顺序。
    /// </summary>
    [Fact]
    public void GetSortedTaskIds_ShouldUseUnifiedTaskOrder()
    {
        var state = new DashboardState(20)
        {
            Tasks =
            {
                ["pending"] = CreateTask("pending", TaskStatus.Pending, 1),
                ["running"] = CreateTask("running", TaskStatus.Running, 5),
                ["failed"] = CreateTask("failed", TaskStatus.Failed, 4),
                ["completed"] = CreateTask("completed", TaskStatus.Completed, 3),
                ["skipped"] = CreateTask("skipped", TaskStatus.Skipped, 2)
            }
        };

        var result = state.GetSortedTaskIds();

        Assert.Equal(["running", "failed", "pending", "completed", "skipped"], result);
    }

    /// <summary>
    /// 验证详情页文件排序遵循 Running、Failed、Pending、Completed、Skipped、Canceled 的统一顺序。
    /// </summary>
    [Fact]
    public void GetSelectedTaskFiles_ShouldUseUnifiedFileOrder()
    {
        var state = new DashboardState(20);
        var task = CreateTask("task", TaskStatus.Running, 1);
        task.Files["pending"] = CreateFile("pending", FileItemStatus.Pending, 1);
        task.Files["running"] = CreateFile("running", FileItemStatus.Running, 5);
        task.Files["failed"] = CreateFile("failed", FileItemStatus.Failed, 4);
        task.Files["completed"] = CreateFile("completed", FileItemStatus.Completed, 3);
        task.Files["skipped"] = CreateFile("skipped", FileItemStatus.Skipped, 2);
        task.Files["canceled"] = CreateFile("canceled", FileItemStatus.Canceled, 6);
        state.Tasks["task"] = task;
        state.DetailsTaskId = "task";

        var result = state.GetSelectedTaskFiles();

        Assert.Equal(["running", "failed", "pending", "completed", "canceled", "skipped"], result.Select(file => file.RelativePath).ToArray());
    }

    /// <summary>
    /// 创建任务视图模型。
    /// </summary>
    /// <param name="taskId">任务标识。</param>
    /// <param name="status">任务状态。</param>
    /// <param name="sortWeight">排序时间偏移。</param>
    /// <returns>任务视图模型。</returns>
    private static TaskViewModel CreateTask(string taskId, TaskStatus status, int sortWeight)
    {
        var descriptor = new TaskDescriptor(taskId, TaskKind.Directory, $"src-{taskId}", $"dst-{taskId}", taskId, DateTimeOffset.UtcNow);
        return new TaskViewModel(descriptor)
        {
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(sortWeight)
        };
    }

    /// <summary>
    /// 创建文件视图模型。
    /// </summary>
    /// <param name="relativePath">文件相对路径。</param>
    /// <param name="status">文件状态。</param>
    /// <param name="sortWeight">排序时间偏移。</param>
    /// <returns>文件视图模型。</returns>
    private static FileViewModel CreateFile(string relativePath, FileItemStatus status, int sortWeight)
    {
        return new FileViewModel(relativePath, 1024)
        {
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(sortWeight)
        };
    }
}


