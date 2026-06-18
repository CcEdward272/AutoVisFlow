using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// Runtime host interface — the primary entry point for executing tasks.
/// </summary>
public interface IRuntimeHost
{
    /// <summary>当前运行时状态。</summary>
    RuntimeState State { get; }

    /// <summary>运行任务定义。</summary>
    /// <param name="task">要运行的任务定义。</param>
    /// <param name="options">运行时运行选项，包含超时、调试等配置。</param>
    /// <param name="ct">取消令牌，用于取消运行操作。</param>
    /// <returns>任务的测试结果。</returns>
    Task<TestResult> RunAsync(TaskDefinition task, RuntimeRunOptions options, CancellationToken ct);

    /// <summary>暂停当前正在运行的任务。</summary>
    void Pause();

    /// <summary>恢复已暂停的任务。</summary>
    void Resume();

    /// <summary>单步执行：执行当前节点后在下一个节点前暂停。</summary>
    void Step();

    /// <summary>停止当前正在运行的任务。</summary>
    void Stop();
}

/// <summary>运行时执行状态机。</summary>
public enum RuntimeState { Idle, Ready, Running, Paused, Completed, Failed, Cancelled }

/// <summary>启动任务时传递给运行时的选项。</summary>
public sealed class RuntimeRunOptions
{
    /// <summary>测试报告输出路径。</summary>
    public string? ReportOutputPath { get; set; }
    /// <summary>跟踪数据库文件路径。</summary>
    public string? TraceDatabasePath { get; set; }
    /// <summary>全局超时时间（毫秒），默认 300 秒。</summary>
    public int GlobalTimeoutMs { get; set; } = 300_000;
    /// <summary>是否启用调试模式。</summary>
    public bool EnableDebug { get; set; }
    /// <summary>断点节点 ID 列表，用于调试时定位暂停位置。</summary>
    public List<string>? BreakpointNodeIds { get; set; }
}

/// <summary>运行测试任务后产生的最终结果。</summary>
public sealed class TestResult
{
    /// <summary>任务 ID。</summary>
    public string TaskId { get; set; } = string.Empty;
    /// <summary>任务名称。</summary>
    public string TaskName { get; set; } = string.Empty;
    /// <summary>执行状态（Passed | Failed | Error | Aborted | NotRun）。</summary>
    public string Status { get; set; } = "NotRun";
    /// <summary>失败原因描述。</summary>
    public string? FailureReason { get; set; }
    /// <summary>开始执行的时间。</summary>
    public DateTime StartTime { get; set; }
    /// <summary>执行结束的时间。</summary>
    public DateTime EndTime { get; set; }
    /// <summary>执行耗时（毫秒）。</summary>
    public long DurationMs { get; set; }
    /// <summary>各步骤的执行结果列表。</summary>
    public List<StepResult> StepResults { get; set; } = new();
    /// <summary>任务输出的键值对字典。</summary>
    public Dictionary<string, object?> Outputs { get; set; } = new();
}

/// <summary>单个已执行步骤/节点的结果。</summary>
public sealed class StepResult
{
    /// <summary>步骤节点 ID。</summary>
    public string NodeId { get; set; } = string.Empty;
    /// <summary>步骤节点标题。</summary>
    public string NodeTitle { get; set; } = string.Empty;
    /// <summary>执行状态（Passed | Failed | Error | Aborted | NotRun）。</summary>
    public string Status { get; set; } = "NotRun";
    /// <summary>执行消息或错误描述。</summary>
    public string? Message { get; set; }
    /// <summary>步骤执行耗时（毫秒）。</summary>
    public long ElapsedMs { get; set; }
}
