using Cc.IDE.ProjectSystem.Models;
using Cc.IDE.DriverSdk;

namespace Cc.IDE.Runtime;

/// <summary>
/// 主任务运行器。负责遍历并执行 <see cref="TaskDefinition"/> 中的流程图节点，
/// 按顺序从开始节点走到结束节点，并在每个节点执行前后检查停止和取消标志。
/// </summary>
public class TaskRunner
{
    private readonly RuntimeContext _context;
    private readonly RuntimeRunOptions _options;
    private readonly IIOExecutionService _ioService;
    private readonly IInstrumentManager? _instrumentManager;
    private readonly Func<string, Task<TaskDefinition?>>? _taskLoader;
    private readonly DebugController? _debug;

    // 暂停/恢复机制
    private TaskCompletionSource<bool>? _pauseGate;
    private FlowNodeDefinition? _pendingNode; // 当前等待执行的节点

    /// <summary>
    /// 创建一个新的任务运行器实例。
    /// </summary>
    public TaskRunner(
        RuntimeContext context,
        RuntimeRunOptions options,
        IIOExecutionService ioService,
        IInstrumentManager? instrumentManager = null,
        Func<string, Task<TaskDefinition?>>? taskLoader = null,
        DebugController? debug = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ioService = ioService ?? throw new ArgumentNullException(nameof(ioService));
        _instrumentManager = instrumentManager;
        _taskLoader = taskLoader;
        _debug = debug;
    }

    /// <summary>获取当前运行时的执行状态。</summary>
    public RuntimeState State { get; private set; } = RuntimeState.Ready;

    /// <summary>获取调试控制器（仅当启用调试时非 null）。</summary>
    public DebugController? Debug => _debug;

    /// <summary>
    /// 从起始节点到结束节点完整执行一个任务定义中的流程图。
    /// </summary>
    /// <param name="task">要执行的任务定义。</param>
    /// <param name="options">运行时运行选项，包含超时和调试配置。</param>
    /// <param name="ct">取消令牌。</param>
    public async Task ExecuteAsync(TaskDefinition task, RuntimeRunOptions options, CancellationToken ct)
    {
        _context.StartTime = DateTime.UtcNow;
        TransitionTo(RuntimeState.Running);

        // 查找起始节点
        var startNode = task.Nodes.FirstOrDefault(n => n.NodeType == "Start");
        if (startNode == null)
        {
            TransitionTo(RuntimeState.Failed);
            return;
        }

        var currentNode = startNode;
        while (currentNode != null && !_context.StopRequested && !ct.IsCancellationRequested)
        {
            // ── 调试门控 ──────────────────────────────────────
            if (_debug != null)
            {
                _pendingNode = currentNode;

                // 检查是否应该在此暂停
                if (!_debug.CheckBeforeExecution(currentNode.Id))
                {
                    // 需要暂停：创建 pause gate 并等待
                    _pauseGate = new TaskCompletionSource<bool>();
                    TransitionTo(RuntimeState.Paused);
                    await _pauseGate.Task; // 阻塞直到用户继续/单步
                    TransitionTo(RuntimeState.Running);
                    _pauseGate = null;
                }

                // 检查是否应跳过此节点
                if (_debug.ShouldSkip(currentNode.Id))
                {
                    _context.RecordStepResult(currentNode, NodeExecutionResult.Skipped);
                    _pendingNode = null;
                    var nextNode = ResolveNextNode(task, currentNode.Id, NodeExecutionResult.Skipped);
                    currentNode = nextNode;
                    continue;
                }
            }

            _pendingNode = null;
            var result = await ExecuteNodeAsync(currentNode, task, ct);
            _context.RecordStepResult(currentNode, result);

            // 检查失败策略
            if (result == NodeExecutionResult.Failed)
            {
                var policy = currentNode.ExecutionPolicy ?? task.ExecutionPolicy;
                if (policy?.FailurePolicy == FailurePolicy.StopOnFailure)
                {
                    TransitionTo(RuntimeState.Failed);
                    return;
                }
            }

            // 解析下一个节点
            var fromNodeId = currentNode.Id;
            currentNode = ResolveNextNode(task, fromNodeId, result);
        }

        if (ct.IsCancellationRequested)
            TransitionTo(RuntimeState.Cancelled);
        else
            TransitionTo(RuntimeState.Completed);
    }

    /// <summary>
    /// 根据节点类型分发执行单个流程节点。
    /// </summary>
    /// <param name="node">要执行的流程节点。</param>
    /// <param name="task">包含此节点的任务定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>节点的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteNodeAsync(FlowNodeDefinition node, TaskDefinition task, CancellationToken ct)
    {
        if (!node.Enabled) return NodeExecutionResult.Skipped;

        return node.NodeType switch
        {
            "Start" => NodeExecutionResult.Passed,
            "End" => HandleEnd(),
            "TestStep" => await ExecuteTestStepAsync(node, ct),
            "Delay" => await ExecuteDelayAsync(node, ct),
            "IOAction" => await ExecuteIOActionNodeAsync(node, ct),
            "Condition" => await ExecuteConditionAsync(node, task, ct),
            "CallTask" => await ExecuteCallTaskAsync(node, task, ct, _options),
            _ => NodeExecutionResult.Passed
        };
    }

    /// <summary>
    /// 处理结束节点：设置停止标志并返回 Passed。
    /// </summary>
    private NodeExecutionResult HandleEnd()
    {
        _context.StopRequested = true;
        return NodeExecutionResult.Passed;
    }

    /// <summary>
    /// 执行 TestStep 节点，按顺序：PreIOAction → InstrumentCall → PostIOAction。
    /// 即使 PreIO 或 InstrumentCall 失败，PostIO（安全恢复动作）始终执行。
    /// </summary>
    /// <param name="node">要执行的 TestStep 节点。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>节点的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteTestStepAsync(FlowNodeDefinition node, CancellationToken ct)
    {
        // 阶段 1：PreIOAction
        if (node.PreIOActions is { Count: > 0 })
        {
            foreach (var ioAction in node.PreIOActions)
            {
                var result = await ExecuteIOActionAsync(ioAction, ct);
                if (result == NodeExecutionResult.Failed)
                {
                    // 安全恢复：即使 PreIO 失败也始终执行 PostIO
                    await ExecutePostIOAsync(node, ct);
                    return NodeExecutionResult.Failed;
                }
            }
        }

        // 阶段 2：InstrumentCall — 通过 IInstrumentManager 调用真实仪器驱动
        if (node.InstrumentCalls is { Count: > 0 })
        {
            if (_instrumentManager != null)
            {
                // 按 ExecutionOrder 和 DependsOn DAG 调度执行
                var instrResult = await ExecuteInstrumentCallsAsync(node.InstrumentCalls, ct);
                if (!instrResult)
                {
                    await ExecutePostIOAsync(node, ct);
                    return NodeExecutionResult.Failed;
                }
            }
            // 无 InstrumentManager 时仪器调用静默跳过（CLI 模式）
        }

        // 阶段 3：PostIOAction（始终执行）
        await ExecutePostIOAsync(node, ct);

        return NodeExecutionResult.Passed;
    }

    /// <summary>
    /// 执行节点的 PostIOAction 列表。无论前置步骤是否失败，此方法始终被调用，
    /// 以确保安全恢复动作得以执行。
    /// </summary>
    /// <param name="node">包含 PostIOAction 的流程节点。</param>
    /// <param name="ct">取消令牌。</param>
    private async Task ExecutePostIOAsync(FlowNodeDefinition node, CancellationToken ct)
    {
        if (node.PostIOActions is not { Count: > 0 }) return;
        foreach (var ioAction in node.PostIOActions)
        {
            await ExecuteIOActionAsync(ioAction, ct);
        }
    }

    /// <summary>
    /// 执行单个 I/O 动作，通过 <see cref="IIOExecutionService"/> 调用真实的 PLC/CAN 设备。
    /// </summary>
    /// <param name="ioAction">要执行的 I/O 动作定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>I/O 动作的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteIOActionAsync(IOActionDefinition ioAction, CancellationToken ct)
    {
        try
        {
            var result = await _ioService.ExecuteAsync(ioAction, ct).ConfigureAwait(false);
            if (result.Success)
            {
                // 将读取值写入上下文变量供后续步骤使用
                if (ioAction.ActionType == IOActionType.Read && result.Value != null)
                    _context.SetVariable($"io_{ioAction.PointCode}", result.Value);
                return NodeExecutionResult.Passed;
            }
            else
            {
                // 记录失败消息
                _context.SetVariable("_lastIOError", result.Message ?? "IO 操作失败");
                return NodeExecutionResult.Failed;
            }
        }
        catch (OperationCanceledException)
        {
            return NodeExecutionResult.Failed;
        }
        catch (Exception ex)
        {
            _context.SetVariable("_lastIOError", ex.Message);
            return NodeExecutionResult.Failed;
        }
    }

    /// <summary>
    /// 执行延时节点。最多等待 5 分钟，超时后自动继续。
    /// </summary>
    /// <param name="node">包含延时时长的流程节点。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>节点的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteDelayAsync(FlowNodeDefinition node, CancellationToken ct)
    {
        await Task.Delay(Math.Min(node.DelayMs, 300000), ct);
        return NodeExecutionResult.Passed;
    }

    /// <summary>
    /// 执行独立的 IOAction 节点（非 TestStep 内的 I/O）。
    /// </summary>
    /// <param name="node">包含独立 I/O 动作的流程节点。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>节点的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteIOActionNodeAsync(FlowNodeDefinition node, CancellationToken ct)
    {
        if (node.IOAction != null)
            return await ExecuteIOActionAsync(node.IOAction, ct);
        return NodeExecutionResult.Passed;
    }

    /// <summary>
    /// 执行条件节点：按顺序评估每个分支的 <see cref="ConditionBranchDefinition.Expression"/>，
    /// 取第一个求值为 <c>true</c> 的分支。若没有分支匹配，使用 <see cref="FlowNodeDefinition.DefaultBranchTargetId"/>。
    /// 表达式由 <see cref="ExpressionEvaluator"/> 进行求值，支持变量引用、比较和逻辑运算。
    /// </summary>
    /// <param name="node">包含条件分支的流程节点。</param>
    /// <param name="task">包含此节点的任务定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>节点的执行结果。</returns>
    private Task<NodeExecutionResult> ExecuteConditionAsync(FlowNodeDefinition node, TaskDefinition task, CancellationToken ct)
    {
        if (node.Branches is not { Count: > 0 })
            return Task.FromResult(NodeExecutionResult.Passed);

        var evaluator = new ExpressionEvaluator(_context);

        foreach (var branch in node.Branches)
        {
            bool conditionMet;
            try
            {
                conditionMet = evaluator.Evaluate(branch.Expression);
            }
            catch (ExpressionEvalException ex)
            {
                // 表达式求值失败 → 记录错误，跳过此分支
                _context.RecordStepResult(new FlowNodeDefinition
                {
                    Id = node.Id + "_cond_" + branch.Label,
                    Title = $"条件分支「{branch.Label}」表达式错误: {ex.Message}"
                }, NodeExecutionResult.Failed);
                continue;
            }

            if (conditionMet)
            {
                // 存储已解析的跳转目标，供 ResolveNextNode 使用
                _context.SetVariable("_lastConditionTarget", branch.TargetNodeId);
                return Task.FromResult(NodeExecutionResult.Passed);
            }
        }

        // 默认分支
        if (node.DefaultBranchTargetId != null)
        {
            _context.SetVariable("_lastConditionTarget", node.DefaultBranchTargetId);
            return Task.FromResult(NodeExecutionResult.Passed);
        }

        return Task.FromResult(NodeExecutionResult.Passed);
    }

    /// <summary>
    /// 执行 CallTask 节点。保存父任务上下文，初始化子任务作用域，
    /// 执行子任务节点，然后恢复父任务上下文。
    /// </summary>
    /// <param name="node">包含子任务调用的流程节点。</param>
    /// <param name="parentTask">父任务定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <param name="options">运行时运行选项。</param>
    /// <returns>节点的执行结果。</returns>
    private async Task<NodeExecutionResult> ExecuteCallTaskAsync(
        FlowNodeDefinition node, TaskDefinition parentTask, CancellationToken ct, RuntimeRunOptions options)
    {
        if (node.TaskCall == null)
            return NodeExecutionResult.Failed;

        // 检查调用栈深度，防止无限递归
        if (_context.CallStackDepth >= RuntimeContext.MaxCallStackDepth)
        {
            _context.RecordStepResult(node, NodeExecutionResult.Failed);
            return NodeExecutionResult.Failed;
        }

        // 如果没有任务加载器，跳过（CLI 模式）
        if (_taskLoader == null)
            return NodeExecutionResult.Passed;

        // 加载子任务
        var childTaskPath = node.TaskCall.TaskPath;
        if (string.IsNullOrWhiteSpace(childTaskPath))
        {
            _context.RecordStepResult(node, NodeExecutionResult.Failed);
            return NodeExecutionResult.Failed;
        }

        TaskDefinition? childTask;
        try
        {
            childTask = await _taskLoader(childTaskPath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _context.SetVariable("_lastError", $"无法加载子任务 '{childTaskPath}': {ex.Message}");
            _context.RecordStepResult(node, NodeExecutionResult.Failed);
            return NodeExecutionResult.Failed;
        }

        if (childTask == null)
        {
            _context.SetVariable("_lastError", $"子任务文件未找到: '{childTaskPath}'");
            _context.RecordStepResult(node, NodeExecutionResult.Failed);
            return NodeExecutionResult.Failed;
        }

        // 将父任务上下文压入调用栈
        _context.PushCallStack(childTask, node.Id, node.Title);

        try
        {
            // 将输入映射从父任务上下文导入子任务变量
            if (node.TaskCall.Inputs != null)
            {
                foreach (var input in node.TaskCall.Inputs)
                {
                    var value = EvaluateValueExpression(input.Value, _context);
                    _context.Variables[input.Key] = value;
                }
            }

            // 递归执行子任务（共享同一个 TaskRunner）
            await ExecuteAsync(childTask, options, ct).ConfigureAwait(false);

            return _context.StopRequested
                ? NodeExecutionResult.Passed
                : NodeExecutionResult.Passed;
        }
        catch (Exception ex)
        {
            _context.SetVariable("_lastError", $"子任务 '{childTask.Name}' 执行异常: {ex.Message}");
            return NodeExecutionResult.Failed;
        }
        finally
        {
            _context.PopCallStack();

            // 将输出映射写回父任务上下文
            if (node.TaskCall.Outputs != null)
            {
                foreach (var output in node.TaskCall.Outputs)
                {
                    _context.Outputs[output.Key] = output.Value.TargetId;
                }
            }
        }
    }

    /// <summary>
    /// 执行 TestStep 中的仪器调用列表。
    /// 按 <see cref="InstrumentCallDefinition.ExecutionOrder"/> 排序，
    /// 解析 <see cref="InstrumentCallDefinition.DependsOn"/> DAG 以支持同 order 内并行。
    /// </summary>
    /// <param name="calls">要执行的仪器调用列表。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>全部调用成功返回 <c>true</c>，任一关键调用失败返回 <c>false</c>。</returns>
    private async Task<bool> ExecuteInstrumentCallsAsync(
        List<InstrumentCallDefinition> calls, CancellationToken ct)
    {
        if (_instrumentManager == null)
            return true;

        // 按 ExecutionOrder 分组
        var groups = calls.GroupBy(c => c.ExecutionOrder).OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            // 同 order 内的调用可并发执行（需考虑 DependsOn）
            var tasks = group.Select(call => ExecuteSingleInstrumentCallAsync(call, ct));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // 检查关键调用
            foreach (var (call, success) in group.Zip(results))
            {
                if (!success && call.IsCritical)
                {
                    _context.SetVariable("_lastError",
                        $"仪器调用失败: InstrumentId={call.InstrumentId}, FunctionId={call.FunctionId}");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 执行单个仪器调用。
    /// </summary>
    private async Task<bool> ExecuteSingleInstrumentCallAsync(
        InstrumentCallDefinition call, CancellationToken ct)
    {
        if (_instrumentManager == null) return true;

        try
        {
            // 解析参数值
            var parameters = new Dictionary<string, object?>();
            if (call.Parameters != null)
            {
                foreach (var kv in call.Parameters)
                {
                    parameters[kv.Key] = EvaluateValueExpression(kv.Value, _context);
                }
            }

            // 获取驱动并执行（InstrumentId 直接对应 DriverId）
            var driverId = call.InstrumentId;
            var driver = _instrumentManager.GetDriver(driverId);
            if (driver == null)
            {
                // 尝试在已注册的驱动中模糊匹配
                driver = _instrumentManager.DiscoveredDrivers
                    .FirstOrDefault(d => d.Key.Contains(driverId, StringComparison.OrdinalIgnoreCase)
                        || d.Value.DisplayName.Contains(driverId, StringComparison.OrdinalIgnoreCase))
                    .Value;
            }
            if (driver == null)
            {
                _context.SetVariable("_lastError",
                    $"未找到仪器驱动: InstrumentId={call.InstrumentId}");
                return false;
            }

            var result = await driver.ExecuteAsync(call.FunctionId, parameters, ct).ConfigureAwait(false);

            if (result.Success)
            {
                // 将输出写入上下文变量
                if (result.Outputs != null)
                {
                    foreach (var kv in result.Outputs)
                    {
                        _context.SetVariable($"instr_{call.InstrumentId}_{kv.Key}", kv.Value);
                    }
                }
                return true;
            }
            else
            {
                _context.SetVariable("_lastError",
                    $"仪器返回错误: {result.Message ?? result.Status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _context.SetVariable("_lastError",
                $"仪器调用异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 在当前运行时上下文中求值一个 <see cref="ValueExpression"/>。
    /// 优先使用 <see cref="ValueExpression.LocalValue"/>；若为绑定，则根据
    /// <see cref="BindingDefinition.Source"/> 从上下文中解析动态值。
    /// </summary>
    /// <param name="expr">要求值的表达式。为 <c>null</c> 时返回 <c>null</c>。</param>
    /// <param name="ctx">运行时上下文。</param>
    /// <returns>求值结果；若表达式无值则返回 <c>null</c>。</returns>
    internal static object? EvaluateValueExpression(ValueExpression? expr, RuntimeContext ctx)
    {
        if (expr == null) return null;
        if (expr.LocalValue != null) return expr.LocalValue;
        if (expr.Binding != null)
        {
            var binding = expr.Binding;
            return binding.Source switch
            {
                BindingSource.TaskVariable => ctx.GetVariable(binding.SourceId),
                BindingSource.ProjectVariable => ctx.GetVariable(binding.SourceId),
                BindingSource.GlobalVariable => ctx.GetVariable(binding.SourceId),
                BindingSource.TaskInput => ctx.Inputs.TryGetValue(binding.SourceId, out var iv) ? iv : null,
                BindingSource.TaskOutput => ctx.Outputs.TryGetValue(binding.SourceId, out var ov) ? ov : null,
                BindingSource.ToolOutput => ctx.GetVariable(binding.SourceId), // 上一步骤输出存储在变量中
                BindingSource.DevicePoint => ctx.GetVariable(binding.SourceId), // IO 点位值存储在变量中
                BindingSource.RuntimeContext => binding.SourceId.ToLowerInvariant() switch
                {
                    "sn" or "serialnumber" => ctx.GetVariable("_serialNumber"),
                    "timestamp" or "time" => DateTime.UtcNow.ToString("O"),
                    "taskname" => ctx.GetVariable("_taskName"),
                    _ => null
                },
                _ => null
            };
        }
        return null;
    }

    /// <summary>
    /// 从运行选项中初始化调试断点集合。
    /// </summary>
    /// <param name="options">包含断点节点 ID 列表的运行选项。</param>
    public void InitializeDebug(RuntimeRunOptions options)
    {
        // Debug breakpoints loaded via DebugController externally
        // No-op: breakpoints managed by DebugController now
    }

    /// <summary>
    /// 根据当前节点和其执行结果解析下一个要执行的节点。
    /// 对于条件节点，使用存储的分支目标；对于多出边的节点，按链接的 <see cref="FlowLinkDefinition.Condition"/> 筛选；
    /// 对于普通节点，沿第一条匹配的出边查找下一个节点。
    /// </summary>
    /// <param name="task">包含节点和连接的任务定义。</param>
    /// <param name="fromNodeId">当前节点的 ID。</param>
    /// <param name="result">当前节点的执行结果。</param>
    /// <returns>下一个要执行的节点，如果没有则返回 <c>null</c>。</returns>
    private FlowNodeDefinition? ResolveNextNode(TaskDefinition task, string fromNodeId, NodeExecutionResult result)
    {
        if (_context.StopRequested) return null;

        // 对于条件节点，解析到存储的分支目标
        var conditionTarget = _context.GetVariable("_lastConditionTarget") as string;
        if (conditionTarget != null)
        {
            _context.SetVariable("_lastConditionTarget", null);
            return task.Nodes.FirstOrDefault(n => n.Id == conditionTarget);
        }

        // 查找从当前节点出发的所有出边
        var outLinks = task.Links.Where(l => l.FromNodeId == fromNodeId).ToList();
        if (outLinks.Count == 0) return null;

        // 只有一个出边：直接跟随
        if (outLinks.Count == 1)
            return task.Nodes.FirstOrDefault(n => n.Id == outLinks[0].ToNodeId);

        // 多个出边：根据结果或条件筛选
        FlowLinkDefinition? matchedLink = null;

        // 按结果端口匹配（如 TestStep 的 "pass" / "fail" 端口）
        var resultPort = result == NodeExecutionResult.Passed ? "pass" : "fail";
        matchedLink = outLinks.FirstOrDefault(l =>
            string.Equals(l.FromPort, resultPort, StringComparison.OrdinalIgnoreCase));

        // 按条件表达式匹配
        if (matchedLink == null)
        {
            var evaluator = new ExpressionEvaluator(_context);
            foreach (var link in outLinks)
            {
                if (string.IsNullOrWhiteSpace(link.Condition))
                    continue;
                try
                {
                    if (evaluator.Evaluate(link.Condition))
                    {
                        matchedLink = link;
                        break;
                    }
                }
                catch (ExpressionEvalException)
                {
                    // 条件表达式求值失败 → 跳过此出边
                }
            }
        }

        // 回退到第一条无条件出边
        matchedLink ??= outLinks.FirstOrDefault(l => string.IsNullOrWhiteSpace(l.Condition));
        // 仍无匹配：取第一条
        matchedLink ??= outLinks[0];

        return task.Nodes.FirstOrDefault(n => n.Id == matchedLink.ToNodeId);
    }

    /// <summary>
    /// 暂停当前正在运行的任务。仅在 <see cref="RuntimeState.Running"/> 状态下有效。
    /// </summary>
    public void Pause()
    {
        if (State == RuntimeState.Running)
            TransitionTo(RuntimeState.Paused);
    }

    /// <summary>
    /// 恢复已暂停的任务。完成 pause gate 以放行当前节点。
    /// </summary>
    public void Resume()
    {
        if (State == RuntimeState.Paused)
        {
            _debug?.Continue();
            _pauseGate?.TrySetResult(true);
        }
    }

    /// <summary>
    /// 单步执行：执行当前节点后在下一个节点前暂停。
    /// </summary>
    public void Step()
    {
        if (State == RuntimeState.Paused)
        {
            _debug?.Step();
            _pauseGate?.TrySetResult(true);
        }
    }

    /// <summary>
    /// 停止当前正在运行的任务。
    /// </summary>
    public void Stop()
    {
        _context.StopRequested = true;
        _pauseGate?.TrySetResult(true);
        TransitionTo(RuntimeState.Cancelled);
    }

    /// <summary>
    /// 将运行器状态迁移到指定状态。
    /// </summary>
    /// <param name="state">目标状态。</param>
    private void TransitionTo(RuntimeState state) => State = state;

    /// <summary>
    /// 获取与此运行器关联的运行时上下文。
    /// </summary>
    public RuntimeContext Context => _context;
}
