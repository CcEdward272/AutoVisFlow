namespace Cc.IDE.Runtime;

/// <summary>
/// 变量监视器。记录任务执行过程中变量的变化历史。
/// </summary>
public sealed class VariableWatcher
{
    private readonly List<VariableChangeRecord> _history = new();

    /// <summary>变量变更历史记录。</summary>
    public IReadOnlyList<VariableChangeRecord> History => _history;

    /// <summary>
    /// 记录一个变量变更。
    /// </summary>
    /// <param name="variableName">变量名称。</param>
    /// <param name="oldValue">变更前的值。</param>
    /// <param name="newValue">变更后的值。</param>
    /// <param name="nodeId">触发变更的节点 ID。</param>
    public void RecordChange(string variableName, object? oldValue, object? newValue, string nodeId)
    {
        _history.Add(new VariableChangeRecord
        {
            VariableName = variableName,
            OldValue = oldValue,
            NewValue = newValue,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>获取指定变量的所有变更记录。</summary>
    /// <param name="variableName">变量名称。</param>
    /// <returns>该变量的所有变更记录列表。</returns>
    public IReadOnlyList<VariableChangeRecord> GetChangesForVariable(string variableName)
    {
        return _history.Where(c => c.VariableName == variableName).ToList();
    }

    /// <summary>清除所有历史记录。</summary>
    public void Clear() => _history.Clear();
}

/// <summary>单次变量变更记录。</summary>
public sealed class VariableChangeRecord
{
    /// <summary>发生变更的变量名称。</summary>
    public string VariableName { get; set; } = string.Empty;

    /// <summary>变更前的值。</summary>
    public object? OldValue { get; set; }

    /// <summary>变更后的值。</summary>
    public object? NewValue { get; set; }

    /// <summary>触发变更的节点 ID。</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>变更发生的时间（UTC）。</summary>
    public DateTime Timestamp { get; set; }
}
