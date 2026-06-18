namespace Cc.IDE.Runtime;

/// <summary>
/// 调试控制器。管理断点、单步执行和运行时调试状态。
/// </summary>
public sealed class DebugController
{
    private readonly HashSet<string> _breakpoints = new();
    private DebugExecutionMode _mode = DebugExecutionMode.Run;
    private string? _skipToNodeId;

    /// <summary>当前调试执行模式。</summary>
    public DebugExecutionMode Mode => _mode;

    /// <summary>当前命中的断点节点 ID 集合。</summary>
    public IReadOnlySet<string> Breakpoints => _breakpoints;

    /// <summary>是否已暂停（等待用户继续或单步）。</summary>
    public bool IsPaused => _mode == DebugExecutionMode.Paused;

    /// <summary>
    /// 添加或移除断点。
    /// </summary>
    /// <param name="nodeId">节点 ID。</param>
    /// <returns>若断点已存在（被移除）则返回 <c>false</c>；若新增则返回 <c>true</c>。</returns>
    public bool ToggleBreakpoint(string nodeId)
    {
        if (_breakpoints.Contains(nodeId))
        {
            _breakpoints.Remove(nodeId);
            return false;
        }
        _breakpoints.Add(nodeId);
        return true;
    }

    /// <summary>添加断点。</summary>
    /// <param name="nodeId">要添加断点的节点 ID。</param>
    public void AddBreakpoint(string nodeId) => _breakpoints.Add(nodeId);

    /// <summary>移除断点。</summary>
    /// <param name="nodeId">要移除断点的节点 ID。</param>
    public void RemoveBreakpoint(string nodeId) => _breakpoints.Remove(nodeId);

    /// <summary>清除所有断点。</summary>
    public void ClearBreakpoints() => _breakpoints.Clear();

    /// <summary>检查指定节点是否设置了断点。</summary>
    /// <param name="nodeId">要检查的节点 ID。</param>
    /// <returns>若该节点设置了断点则返回 <c>true</c>。</returns>
    public bool HasBreakpoint(string nodeId) => _breakpoints.Contains(nodeId);

    /// <summary>继续执行。</summary>
    public void Continue() => _mode = DebugExecutionMode.Run;

    /// <summary>单步执行——执行当前节点后在下一个节点前自动暂停。</summary>
    public void Step() => _mode = DebugExecutionMode.StepOver;

    /// <summary>跳过当前节点，生成 Skipped 结果。</summary>
    /// <param name="nodeId">要检查的节点 ID。</param>
    /// <returns>若应跳过该节点则返回 <c>true</c>。</returns>
    public bool ShouldSkip(string nodeId)
    {
        if (_skipToNodeId != null && _skipToNodeId != nodeId) return true;
        _skipToNodeId = null;
        return false;
    }

    /// <summary>运行到指定节点前暂停。</summary>
    /// <param name="nodeId">目标节点 ID。</param>
    public void RunToCursor(string nodeId)
    {
        _skipToNodeId = nodeId;
        _mode = DebugExecutionMode.RunToCursor;
    }

    /// <summary>
    /// 在节点执行前检查调试状态。
    /// </summary>
    /// <param name="nodeId">当前节点 ID。</param>
    /// <returns>若应继续执行则返回 <c>true</c>；若应暂停则返回 <c>false</c>。</returns>
    public bool CheckBeforeExecution(string nodeId)
    {
        if (_mode == DebugExecutionMode.SkipToNext)
            return true;

        if (_mode == DebugExecutionMode.RunToCursor && nodeId == _skipToNodeId)
        {
            _mode = DebugExecutionMode.Paused;
            _skipToNodeId = null;
            return false;
        }

        if (_mode == DebugExecutionMode.StepOver)
        {
            _mode = DebugExecutionMode.Paused;
            return true; // 执行当前节点，下一次调用前暂停
        }

        if (_breakpoints.Contains(nodeId))
        {
            _mode = DebugExecutionMode.Paused;
            return false; // 断点命中，暂停
        }

        return true;
    }
}

/// <summary>调试执行模式枚举。</summary>
public enum DebugExecutionMode
{
    /// <summary>正常运行。</summary>
    Run,
    /// <summary>已暂停。</summary>
    Paused,
    /// <summary>单步执行——执行当前节点后暂停。</summary>
    StepOver,
    /// <summary>跳到下一个节点。</summary>
    SkipToNext,
    /// <summary>运行到指定节点。</summary>
    RunToCursor
}
