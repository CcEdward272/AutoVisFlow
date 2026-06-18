using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// 单个流程节点的执行结果。
/// </summary>
public enum NodeExecutionResult
{
    /// <summary>节点执行成功通过。</summary>
    Passed,
    /// <summary>节点执行失败。</summary>
    Failed,
    /// <summary>节点被跳过（例如因 Enabled=false）。</summary>
    Skipped,
    /// <summary>节点尚未运行。</summary>
    NotRun
}

/// <summary>
/// 表示运行时调用栈中的一个栈帧，用于 CallTask（子任务调用）支持。
/// </summary>
public sealed class CallStackFrame
{
    /// <summary>当前正在执行的任务的唯一标识符。</summary>
    public string TaskId { get; set; }

    /// <summary>当前任务的可读名称。</summary>
    public string TaskName { get; set; }

    /// <summary>当前正在执行的节点 ID。</summary>
    public string CurrentNodeId { get; set; }

    /// <summary>当前节点的标题文本。</summary>
    public string CurrentNodeTitle { get; set; }

    /// <summary>
    /// 创建一个新的调用栈帧。
    /// </summary>
    /// <param name="taskId">任务 ID。</param>
    /// <param name="taskName">任务名称。</param>
    /// <param name="currentNodeId">当前节点 ID。</param>
    /// <param name="currentNodeTitle">当前节点标题。</param>
    public CallStackFrame(string taskId, string taskName, string currentNodeId, string currentNodeTitle)
    {
        TaskId = taskId;
        TaskName = taskName;
        CurrentNodeId = currentNodeId;
        CurrentNodeTitle = currentNodeTitle;
    }
}

/// <summary>
/// 任务运行时的执行状态包。保存任务局部变量、输入/输出参数、调用栈、步骤结果和停止标志。
/// </summary>
public sealed class RuntimeContext
{
    /// <summary>任务局部变量字典，键为变量名。</summary>
    public Dictionary<string, object?> Variables { get; } = new();

    /// <summary>输入参数字典，由调用方在任务启动前填充。</summary>
    public Dictionary<string, object?> Inputs { get; } = new();

    /// <summary>输出参数字典，由任务执行过程中写入，任务结束后返回给调用方。</summary>
    public Dictionary<string, object?> Outputs { get; } = new();

    /// <summary>
    /// 调用栈，用于 CallTask（子任务调用）支持（Phase 7）。
    /// 每进入一个子任务时压入一个 <see cref="CallStackFrame"/>，返回时弹出。
    /// </summary>
    public Stack<CallStackFrame> CallStack { get; } = new();

    /// <summary>已收集的各节点步骤执行结果列表。</summary>
    public List<StepResult> StepResults { get; } = new();

    /// <summary>
    /// 当设置为 <c>true</c> 时，执行引擎在下一个节点执行前终止执行。
    /// 各节点在执行之间检查此标志。
    /// </summary>
    public bool StopRequested { get; set; }

    /// <summary>任务开始执行的时间（UTC）。</summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 记录一个步骤节点的执行结果。
    /// </summary>
    /// <param name="node">已执行的流程节点。</param>
    /// <param name="execResult">节点的执行结果枚举。</param>
    public void RecordStepResult(FlowNodeDefinition node, NodeExecutionResult execResult)
    {
        var status = execResult switch
        {
            NodeExecutionResult.Passed => "Passed",
            NodeExecutionResult.Failed => "Failed",
            NodeExecutionResult.Skipped => "Skipped",
            NodeExecutionResult.NotRun => "NotRun",
            _ => "NotRun"
        };

        StepResults.Add(new StepResult
        {
            NodeId = node.Id,
            NodeTitle = node.Title,
            Status = status,
            ElapsedMs = 0
        });
    }

    /// <summary>
    /// 获取任务局部变量的值。若变量不存在则返回 <c>null</c>。
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <returns>变量的值，或 <c>null</c>。</returns>
    public object? GetVariable(string name)
    {
        return Variables.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// 设置任务局部变量的值。若变量已存在则覆盖。
    /// </summary>
    /// <param name="name">变量名。</param>
    /// <param name="value">要设置的值。</param>
    public void SetVariable(string name, object? value)
    {
        Variables[name] = value;
    }

    /// <summary>
    /// 将当前状态压入调用栈并初始化子任务上下文。
    /// </summary>
    /// <param name="childTask">要调用的子任务定义。</param>
    /// <param name="currentNodeId">当前正在执行的节点 ID。</param>
    /// <param name="currentNodeTitle">当前节点的标题。</param>
    public void PushCallStack(TaskDefinition childTask, string currentNodeId, string currentNodeTitle)
    {
        CallStack.Push(new CallStackFrame(
            childTask.Id, childTask.Name, currentNodeId, currentNodeTitle));
    }

    /// <summary>
    /// 弹出调用栈顶帧，返回调用方上下文信息。
    /// </summary>
    /// <returns>弹出的栈帧；若栈已空则返回 <c>null</c>。</returns>
    public CallStackFrame? PopCallStack()
    {
        return CallStack.Count > 0 ? CallStack.Pop() : null;
    }

    /// <summary>
    /// 获取当前调用栈深度。
    /// </summary>
    public int CallStackDepth => CallStack.Count;

    /// <summary>
    /// 最大允许的调用栈深度（防止无限递归）。
    /// </summary>
    public const int MaxCallStackDepth = 10;
}
