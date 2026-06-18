namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程编辑操作的类型枚举。用于标识撤销/重做栈中每个操作的动作种类。
/// </summary>
public enum FlowEditActionType
{
    /// <summary>添加了一个新节点。</summary>
    AddNode,

    /// <summary>移除了一个现有节点及其关联连线。</summary>
    RemoveNode,

    /// <summary>移动了一个节点的画布位置。</summary>
    MoveNode,

    /// <summary>在两个节点之间创建了连线。</summary>
    ConnectNodes,

    /// <summary>断开了两个节点之间的连线。</summary>
    DisconnectNodes,

    /// <summary>编辑了某个节点的属性值。</summary>
    EditProperty,
}

/// <summary>
/// 表示流程图画布上的一次编辑操作，用于撤销/重做系统。
/// 记录操作类型、操作前后的状态快照以及人可读的描述。
/// </summary>
public sealed class FlowEditAction
{
    /// <summary>获取此操作的动作类型。</summary>
    public FlowEditActionType ActionType { get; }

    /// <summary>获取操作前的状态快照。对于添加操作，此值为 <c>null</c>。</summary>
    public object? OldState { get; }

    /// <summary>获取操作后的状态快照。对于删除操作，此值为 <c>null</c>。</summary>
    public object? NewState { get; }

    /// <summary>获取此操作的人可读描述，用于在 UI 中显示撤销/重做提示。</summary>
    public string Description { get; }

    /// <summary>
    /// 初始化 <see cref="FlowEditAction"/> 的新实例。
    /// </summary>
    /// <param name="actionType">操作的动作类型。</param>
    /// <param name="oldState">操作前的状态快照。</param>
    /// <param name="newState">操作后的状态快照。</param>
    /// <param name="description">操作的人可读描述。</param>
    public FlowEditAction(FlowEditActionType actionType, object? oldState, object? newState, string description)
    {
        ActionType = actionType;
        OldState = oldState;
        NewState = newState;
        Description = description;
    }
}
