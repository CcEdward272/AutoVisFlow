using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程图编辑器的主 ViewModel。
/// 管理画布上所有节点和连线的 CRUD、选择、拖拽和缩放状态。
/// 提供撤销/重做支持，以及从 <see cref="TaskDefinition"/> 加载和保存的能力。
/// </summary>
public class FlowCanvasViewModel : ViewModelBase
{
    #region 字段

    private FlowNodeViewModel? _selectedNode;
    private IReadOnlyList<FlowNodeViewModel> _selectedNodes = Array.Empty<FlowNodeViewModel>();
    private double _zoom = 1.0;
    private double _offsetX;
    private double _offsetY;

    private readonly Stack<FlowEditAction> _undoStack = new();
    private readonly Stack<FlowEditAction> _redoStack = new();

    /// <summary>当前待添加的节点类型描述符。由 UI 在调用添加命令前设置。</summary>
    private NodeTypeDescriptor? _pendingNodeDescriptor;

    /// <summary>当前待创建的连线参数。由 UI 在调用连接命令前设置。</summary>
    private (FlowNodeViewModel from, FlowNodeViewModel to)? _pendingConnection;

    /// <summary>当前待移除的节点。由 UI 在调用移除命令前设置。</summary>
    private FlowNodeViewModel? _pendingRemoveNode;

    #endregion

    #region 属性

    /// <summary>画布上所有流程节点的可观察集合。</summary>
    public ObservableCollection<FlowNodeViewModel> Nodes { get; } = new();

    /// <summary>画布上所有连线的可观察集合。</summary>
    public ObservableCollection<FlowLinkViewModel> Links { get; } = new();

    /// <summary>当前选中的节点。单选时为单个节点；多选时返回最近选中的节点。</summary>
    public FlowNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                SelectedNodes = value != null
                    ? new List<FlowNodeViewModel> { value }
                    : new List<FlowNodeViewModel>();
            }
        }
    }

    /// <summary>当前选中的节点列表，支持框选多选。</summary>
    public IReadOnlyList<FlowNodeViewModel> SelectedNodes
    {
        get => _selectedNodes;
        private set => SetProperty(ref _selectedNodes, value);
    }

    /// <summary>
    /// 画布缩放比例。有效范围为 0.25 倍到 3.0 倍。
    /// 设置时自动钳制到有效范围。
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Clamp(value, 0.25, 3.0);
            SetProperty(ref _zoom, clamped);
        }
    }

    /// <summary>画布水平平移偏移量。</summary>
    public double OffsetX
    {
        get => _offsetX;
        set => SetProperty(ref _offsetX, value);
    }

    /// <summary>画布垂直平移偏移量。</summary>
    public double OffsetY
    {
        get => _offsetY;
        set => SetProperty(ref _offsetY, value);
    }

    /// <summary>获取撤销栈中的操作数量。</summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>获取重做栈中的操作数量。</summary>
    public int RedoCount => _redoStack.Count;

    #endregion

    #region 命令

    /// <summary>
    /// 添加新节点到画布的命令。调用前应先设置 <see cref="AddNodeDescriptor"/> 指定节点类型。
    /// </summary>
    public ICommand AddNodeCommand { get; }

    /// <summary>从画布移除节点的命令。调用前应先设置 <see cref="RemoveNodeTarget"/> 指定要移除的节点。</summary>
    public ICommand RemoveNodeCommand { get; }

    /// <summary>
    /// 连接两个节点的命令。调用前应先设置 <see cref="ConnectNodesTarget"/> 指定 (from, to) 对。
    /// </summary>
    public ICommand ConnectNodesCommand { get; }

    /// <summary>删除当前选中项的命令。</summary>
    public ICommand DeleteSelectedCommand { get; }

    /// <summary>撤销上一步操作的命令。</summary>
    public ICommand UndoCommand { get; }

    /// <summary>重做已撤销操作的命令。</summary>
    public ICommand RedoCommand { get; }

    #endregion

    #region 命令前置属性（视图层在调用命令前设置）

    /// <summary>设置或获取待添加的节点类型描述符。在调用 <see cref="AddNodeCommand"/> 之前设置。</summary>
    public NodeTypeDescriptor? AddNodeDescriptor
    {
        get => _pendingNodeDescriptor;
        set => _pendingNodeDescriptor = value;
    }

    /// <summary>设置或获取待连线的 (源节点, 目标节点) 对。在调用 <see cref="ConnectNodesCommand"/> 之前设置。</summary>
    public (FlowNodeViewModel from, FlowNodeViewModel to)? ConnectNodesTarget
    {
        get => _pendingConnection;
        set => _pendingConnection = value;
    }

    /// <summary>设置或获取待移除的节点。在调用 <see cref="RemoveNodeCommand"/> 之前设置。</summary>
    public FlowNodeViewModel? RemoveNodeTarget
    {
        get => _pendingRemoveNode;
        set => _pendingRemoveNode = value;
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="FlowCanvasViewModel"/> 的新实例。
    /// 创建所有命令并绑定到对应的处理方法。
    /// </summary>
    public FlowCanvasViewModel()
    {
        AddNodeCommand = new RelayCommand(OnAddNode);
        RemoveNodeCommand = new RelayCommand(OnRemoveNode);
        ConnectNodesCommand = new RelayCommand(OnConnectNodes);
        DeleteSelectedCommand = new RelayCommand(OnDeleteSelected);
        UndoCommand = new RelayCommand(OnUndo);
        RedoCommand = new RelayCommand(OnRedo);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 从 <see cref="TaskDefinition"/> 加载流程图表。
    /// 清除当前画布状态，然后根据任务定义创建所有节点和连线。
    /// </summary>
    /// <param name="task">要加载的任务定义。不能为 <c>null</c>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="task"/> 为 <c>null</c> 时抛出。</exception>
    public void LoadFromTask(TaskDefinition task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        Nodes.Clear();
        Links.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        SelectedNode = null;

        // 恢复布局信息
        if (task.Layout != null)
        {
            Zoom = task.Layout.Zoom;
            OffsetX = task.Layout.OffsetX;
            OffsetY = task.Layout.OffsetY;
        }
        else
        {
            Zoom = 1.0;
            OffsetX = 0;
            OffsetY = 0;
        }

        foreach (var node in task.Nodes)
        {
            var nodeVm = new FlowNodeViewModel
            {
                Id = node.Id,
                NodeType = node.NodeType,
                DisplayTitle = node.Title,
                Description = node.Description,
                Enabled = node.Enabled,
                X = node.X,
                Y = node.Y,
                Width = node.Width,
                Height = node.Height,
                DelayMs = node.DelayMs,
                DefaultBranchTargetId = node.DefaultBranchTargetId,
                LoopBodyStartNodeId = node.LoopBodyStartNodeId,
                LoopExitNodeId = node.LoopExitNodeId,
                ExecutionPolicy = node.ExecutionPolicy,
            };

            if (node.PreIOActions != null)
                nodeVm.PreIOActions = new List<IOActionDefinition>(node.PreIOActions);
            if (node.InstrumentCalls != null)
                nodeVm.InstrumentCalls = new List<InstrumentCallDefinition>(node.InstrumentCalls);
            if (node.PostIOActions != null)
                nodeVm.PostIOActions = new List<IOActionDefinition>(node.PostIOActions);
            if (node.TaskCall != null)
                nodeVm.TaskCall = CloneTaskCall(node.TaskCall);
            if (node.Branches != null)
                nodeVm.Branches = new List<ConditionBranchDefinition>(node.Branches);
            if (node.Loop != null)
                nodeVm.Loop = CloneLoop(node.Loop);
            if (node.IOAction != null)
                nodeVm.IOAction = CloneIOAction(node.IOAction);

            Nodes.Add(nodeVm);
        }

        foreach (var link in task.Links)
        {
            var fromNode = Nodes.FirstOrDefault(n => n.Id == link.FromNodeId);
            var toNode = Nodes.FirstOrDefault(n => n.Id == link.ToNodeId);
            if (fromNode != null && toNode != null)
            {
                Links.Add(new FlowLinkViewModel
                {
                    Id = Guid.NewGuid().ToString("N"),
                    FromNode = fromNode,
                    ToNode = toNode,
                    FromPort = link.FromPort,
                    ToPort = link.ToPort,
                    Label = link.Label,
                    Condition = link.Condition
                });
            }
        }
    }

    /// <summary>
    /// 将当前画布状态保存为 <see cref="TaskDefinition"/>。
    /// </summary>
    /// <returns>包含所有节点和连线的新建任务定义。</returns>
    public TaskDefinition SaveToTask()
    {
        var task = new TaskDefinition
        {
            Name = "Untitled",
            Mode = "FlowGraph",
        };

        foreach (var nodeVm in Nodes)
        {
            var nodeDef = new FlowNodeDefinition
            {
                Id = nodeVm.Id,
                NodeType = nodeVm.NodeType,
                Title = nodeVm.DisplayTitle,
                Description = nodeVm.Description,
                Enabled = nodeVm.Enabled,
                X = nodeVm.X,
                Y = nodeVm.Y,
                Width = nodeVm.Width,
                Height = nodeVm.Height,
                DelayMs = nodeVm.DelayMs,
                DefaultBranchTargetId = nodeVm.DefaultBranchTargetId,
                LoopBodyStartNodeId = nodeVm.LoopBodyStartNodeId,
                LoopExitNodeId = nodeVm.LoopExitNodeId,
                ExecutionPolicy = nodeVm.ExecutionPolicy,
                PreIOActions = nodeVm.PreIOActions != null
                    ? new List<IOActionDefinition>(nodeVm.PreIOActions)
                    : null,
                InstrumentCalls = nodeVm.InstrumentCalls != null
                    ? new List<InstrumentCallDefinition>(nodeVm.InstrumentCalls)
                    : null,
                PostIOActions = nodeVm.PostIOActions != null
                    ? new List<IOActionDefinition>(nodeVm.PostIOActions)
                    : null,
                TaskCall = nodeVm.TaskCall != null ? CloneTaskCall(nodeVm.TaskCall) : null,
                Branches = nodeVm.Branches != null
                    ? new List<ConditionBranchDefinition>(nodeVm.Branches)
                    : null,
                Loop = nodeVm.Loop != null ? CloneLoop(nodeVm.Loop) : null,
                IOAction = nodeVm.IOAction != null ? CloneIOAction(nodeVm.IOAction) : null,
            };

            task.Nodes.Add(nodeDef);
        }

        foreach (var linkVm in Links)
        {
            task.Links.Add(new FlowLinkDefinition
            {
                FromNodeId = linkVm.FromNode.Id,
                ToNodeId = linkVm.ToNode.Id,
                FromPort = linkVm.FromPort,
                ToPort = linkVm.ToPort,
                Label = linkVm.Label,
                Condition = linkVm.Condition
            });
        }

        task.Layout = new FlowLayoutInfo
        {
            Zoom = Zoom,
            OffsetX = OffsetX,
            OffsetY = OffsetY
        };

        return task;
    }

    #endregion

    #region 命令处理方法

    /// <summary>
    /// 向画布添加一个新节点。
    /// 在调用前应通过 <see cref="AddNodeDescriptor"/> 设置节点类型。
    /// </summary>
    private void OnAddNode()
    {
        var descriptor = _pendingNodeDescriptor;
        if (descriptor == null) return;

        var nodeVm = new FlowNodeViewModel
        {
            NodeType = descriptor.NodeType,
            DisplayTitle = descriptor.DisplayName,
            X = -OffsetX + 200,
            Y = -OffsetY + 200,
        };

        Nodes.Add(nodeVm);
        SelectedNode = nodeVm;

        var action = new FlowEditAction(
            FlowEditActionType.AddNode,
            oldState: null,
            newState: CloneNodeViewModel(nodeVm),
            description: $"添加节点「{descriptor.DisplayName}」");
        _undoStack.Push(action);
        _redoStack.Clear();

        _pendingNodeDescriptor = null;
        RefreshCommandStates();
    }

    /// <summary>
    /// 从画布移除一个节点及其所有关联连线。
    /// 在调用前应通过 <see cref="RemoveNodeTarget"/> 设置要移除的节点。
    /// </summary>
    private void OnRemoveNode()
    {
        var node = _pendingRemoveNode ?? SelectedNode;
        if (node == null) return;

        var affectedLinks = Links.Where(l => l.FromNode == node || l.ToNode == node).ToList();

        var oldState = new RemoveNodeState
        {
            Node = CloneNodeViewModel(node),
            RemovedLinks = affectedLinks.Select(CloneLinkViewModel).ToList()
        };

        foreach (var link in affectedLinks)
            Links.Remove(link);
        Nodes.Remove(node);

        if (SelectedNode == node)
            SelectedNode = null;

        var action = new FlowEditAction(
            FlowEditActionType.RemoveNode,
            oldState: oldState,
            newState: null,
            description: $"删除节点「{node.DisplayTitle}」");
        _undoStack.Push(action);
        _redoStack.Clear();

        _pendingRemoveNode = null;
        RefreshCommandStates();
    }

    /// <summary>
    /// 在两个节点之间创建连线。
    /// 在调用前应通过 <see cref="ConnectNodesTarget"/> 设置 (from, to) 对。
    /// </summary>
    private void OnConnectNodes()
    {
        var connection = _pendingConnection;
        if (connection == null) return;

        var (from, to) = connection.Value;
        if (from == null || to == null) return;

        if (Links.Any(l => l.FromNode == from && l.ToNode == to))
            return;

        var linkVm = new FlowLinkViewModel
        {
            FromNode = from,
            ToNode = to,
            FromPort = "out",
            ToPort = "in",
        };

        Links.Add(linkVm);

        var action = new FlowEditAction(
            FlowEditActionType.ConnectNodes,
            oldState: null,
            newState: CloneLinkViewModel(linkVm),
            description: $"连接「{from.DisplayTitle}」→「{to.DisplayTitle}」");
        _undoStack.Push(action);
        _redoStack.Clear();

        _pendingConnection = null;
        RefreshCommandStates();
    }

    /// <summary>删除当前选中的节点。</summary>
    private void OnDeleteSelected()
    {
        _pendingRemoveNode = SelectedNode;
        OnRemoveNode();
    }

    /// <summary>
    /// 撤销上一步编辑操作。
    /// 从撤销栈弹出操作并反向应用。
    /// </summary>
    private void OnUndo()
    {
        if (_undoStack.Count == 0) return;

        var action = _undoStack.Pop();
        _redoStack.Push(action);

        switch (action.ActionType)
        {
            case FlowEditActionType.AddNode:
                if (action.NewState is FlowNodeViewModel addedNode)
                {
                    var toRemove = Nodes.FirstOrDefault(n => n.Id == addedNode.Id);
                    if (toRemove != null)
                    {
                        var relatedLinks = Links.Where(l => l.FromNode == toRemove || l.ToNode == toRemove).ToList();
                        foreach (var link in relatedLinks)
                            Links.Remove(link);
                        Nodes.Remove(toRemove);
                        if (SelectedNode == toRemove)
                            SelectedNode = null;
                    }
                }
                break;

            case FlowEditActionType.RemoveNode:
                if (action.OldState is RemoveNodeState removeState && removeState.Node != null)
                {
                    var restored = removeState.Node;
                    Nodes.Add(restored);
                    if (removeState.RemovedLinks != null)
                    {
                        foreach (var link in removeState.RemovedLinks)
                        {
                            link.FromNode = Nodes.FirstOrDefault(n => n.Id == link.FromNode.Id) ?? link.FromNode;
                            link.ToNode = Nodes.FirstOrDefault(n => n.Id == link.ToNode.Id) ?? link.ToNode;
                            Links.Add(link);
                        }
                    }
                    SelectedNode = restored;
                }
                break;

            case FlowEditActionType.ConnectNodes:
                if (action.NewState is FlowLinkViewModel connectedLink)
                {
                    var toRemove = Links.FirstOrDefault(l =>
                        l.FromNode.Id == connectedLink.FromNode.Id &&
                        l.ToNode.Id == connectedLink.ToNode.Id);
                    if (toRemove != null)
                        Links.Remove(toRemove);
                }
                break;

            case FlowEditActionType.MoveNode:
                if (action.NewState is MoveNodeState moveNew && action.OldState is MoveNodeState moveOld)
                {
                    var node = Nodes.FirstOrDefault(n => n.Id == moveNew.NodeId);
                    if (node != null)
                    {
                        node.X = moveOld.X;
                        node.Y = moveOld.Y;
                    }
                }
                break;

            case FlowEditActionType.DisconnectNodes:
                if (action.NewState is FlowLinkViewModel disconnectedLink)
                {
                    Links.Add(disconnectedLink);
                }
                break;

            case FlowEditActionType.EditProperty:
                if (action.OldState is PropertyEditState propEdit)
                {
                    var node = Nodes.FirstOrDefault(n => n.Id == propEdit.NodeId);
                    if (node != null)
                    {
                        ApplyPropertyValue(node, propEdit.PropertyName, propEdit.OldValue);
                    }
                }
                break;
        }

        RefreshCommandStates();
    }

    /// <summary>
    /// 重做已撤销的编辑操作。
    /// 从重做栈弹出操作并重新应用。
    /// </summary>
    private void OnRedo()
    {
        if (_redoStack.Count == 0) return;

        var action = _redoStack.Pop();
        _undoStack.Push(action);

        switch (action.ActionType)
        {
            case FlowEditActionType.AddNode:
                if (action.NewState is FlowNodeViewModel redoAddedNode)
                {
                    Nodes.Add(redoAddedNode);
                    SelectedNode = redoAddedNode;
                }
                break;

            case FlowEditActionType.RemoveNode:
                if (action.OldState is RemoveNodeState redoRemoveState && redoRemoveState.Node != null)
                {
                    var node = Nodes.FirstOrDefault(n => n.Id == redoRemoveState.Node.Id);
                    if (node != null)
                    {
                        var relatedLinks = Links.Where(l => l.FromNode == node || l.ToNode == node).ToList();
                        foreach (var link in relatedLinks)
                            Links.Remove(link);
                        Nodes.Remove(node);
                        if (SelectedNode == node)
                            SelectedNode = null;
                    }
                }
                break;

            case FlowEditActionType.ConnectNodes:
                if (action.NewState is FlowLinkViewModel redoLink)
                {
                    Links.Add(redoLink);
                }
                break;

            case FlowEditActionType.MoveNode:
                if (action.NewState is MoveNodeState redoMove)
                {
                    var node = Nodes.FirstOrDefault(n => n.Id == redoMove.NodeId);
                    if (node != null)
                    {
                        node.X = redoMove.X;
                        node.Y = redoMove.Y;
                    }
                }
                break;

            case FlowEditActionType.DisconnectNodes:
                if (action.NewState is FlowLinkViewModel redoDisconnectLink)
                {
                    var toRemove = Links.FirstOrDefault(l =>
                        l.FromNode.Id == redoDisconnectLink.FromNode.Id &&
                        l.ToNode.Id == redoDisconnectLink.ToNode.Id);
                    if (toRemove != null)
                        Links.Remove(toRemove);
                }
                break;

            case FlowEditActionType.EditProperty:
                if (action.NewState is PropertyEditState redoProp)
                {
                    var node = Nodes.FirstOrDefault(n => n.Id == redoProp.NodeId);
                    if (node != null)
                    {
                        ApplyPropertyValue(node, redoProp.PropertyName, redoProp.NewValue);
                    }
                }
                break;
        }

        RefreshCommandStates();
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 记录节点的移动操作（用于撤销/重做）。
    /// 应在拖拽结束时调用。
    /// </summary>
    /// <param name="node">被移动的节点。</param>
    /// <param name="oldX">移动前的 X 坐标。</param>
    /// <param name="oldY">移动前的 Y 坐标。</param>
    public void RecordMoveNode(FlowNodeViewModel node, double oldX, double oldY)
    {
        var action = new FlowEditAction(
            FlowEditActionType.MoveNode,
            oldState: new MoveNodeState { NodeId = node.Id, X = oldX, Y = oldY },
            newState: new MoveNodeState { NodeId = node.Id, X = node.X, Y = node.Y },
            description: $"移动节点「{node.DisplayTitle}」");
        _undoStack.Push(action);
        _redoStack.Clear();
        RefreshCommandStates();
    }

    /// <summary>
    /// 记录节点的属性编辑操作（用于撤销/重做）。
    /// </summary>
    /// <param name="node">被编辑的节点。</param>
    /// <param name="propertyName">被修改的属性名称。</param>
    /// <param name="oldValue">修改前的属性值。</param>
    /// <param name="newValue">修改后的属性值。</param>
    public void RecordPropertyEdit(FlowNodeViewModel node, string propertyName, object? oldValue, object? newValue)
    {
        var action = new FlowEditAction(
            FlowEditActionType.EditProperty,
            oldState: new PropertyEditState { NodeId = node.Id, PropertyName = propertyName, OldValue = oldValue, NewValue = null },
            newState: new PropertyEditState { NodeId = node.Id, PropertyName = propertyName, OldValue = null, NewValue = newValue },
            description: $"编辑「{node.DisplayTitle}」的 {propertyName}");
        _undoStack.Push(action);
        _redoStack.Clear();
        RefreshCommandStates();
    }

    /// <summary>刷新命令的可执行状态通知。</summary>
    private void RefreshCommandStates()
    {
        OnPropertyChanged(nameof(UndoCount));
        OnPropertyChanged(nameof(RedoCount));
    }

    /// <summary>深拷贝一个 <see cref="FlowNodeViewModel"/>。</summary>
    private static FlowNodeViewModel CloneNodeViewModel(FlowNodeViewModel source)
    {
        var clone = new FlowNodeViewModel
        {
            Id = source.Id,
            NodeType = source.NodeType,
            DisplayTitle = source.DisplayTitle,
            Description = source.Description,
            Enabled = source.Enabled,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            DelayMs = source.DelayMs,
            DefaultBranchTargetId = source.DefaultBranchTargetId,
            LoopBodyStartNodeId = source.LoopBodyStartNodeId,
            LoopExitNodeId = source.LoopExitNodeId,
            ExecutionPolicy = source.ExecutionPolicy,
        };

        if (source.PreIOActions != null)
            clone.PreIOActions = new List<IOActionDefinition>(source.PreIOActions);
        if (source.InstrumentCalls != null)
            clone.InstrumentCalls = new List<InstrumentCallDefinition>(source.InstrumentCalls);
        if (source.PostIOActions != null)
            clone.PostIOActions = new List<IOActionDefinition>(source.PostIOActions);
        if (source.TaskCall != null)
            clone.TaskCall = CloneTaskCall(source.TaskCall);
        if (source.Branches != null)
            clone.Branches = new List<ConditionBranchDefinition>(source.Branches);
        if (source.Loop != null)
            clone.Loop = CloneLoop(source.Loop);
        if (source.IOAction != null)
            clone.IOAction = CloneIOAction(source.IOAction);

        return clone;
    }

    /// <summary>深拷贝一个 <see cref="FlowLinkViewModel"/> 的快照。</summary>
    private static FlowLinkViewModel CloneLinkViewModel(FlowLinkViewModel source)
    {
        return new FlowLinkViewModel
        {
            Id = source.Id,
            FromNode = source.FromNode,
            ToNode = source.ToNode,
            FromPort = source.FromPort,
            ToPort = source.ToPort,
            Label = source.Label,
            Condition = source.Condition,
        };
    }

    /// <summary>深拷贝一个 <see cref="TaskCallDefinition"/>。</summary>
    private static TaskCallDefinition CloneTaskCall(TaskCallDefinition source)
    {
        return new TaskCallDefinition
        {
            TaskId = source.TaskId,
            TaskPath = source.TaskPath,
            Inputs = source.Inputs != null
                ? new Dictionary<string, ValueExpression>(source.Inputs)
                : new(),
            Outputs = source.Outputs != null
                ? new Dictionary<string, BindingTargetDefinition>(source.Outputs)
                : new(),
        };
    }

    /// <summary>深拷贝一个 <see cref="LoopDefinition"/>。</summary>
    private static LoopDefinition CloneLoop(LoopDefinition source)
    {
        return new LoopDefinition
        {
            Type = source.Type,
            Expression = source.Expression,
            MaxIterations = source.MaxIterations,
        };
    }

    /// <summary>深拷贝一个 <see cref="IOActionDefinition"/>。</summary>
    private static IOActionDefinition CloneIOAction(IOActionDefinition source)
    {
        return new IOActionDefinition
        {
            Id = source.Id,
            Name = source.Name,
            IOType = source.IOType,
            DeviceId = source.DeviceId,
            PointCode = source.PointCode,
            ActionType = source.ActionType,
            Value = source.Value,
            SettleTimeMs = source.SettleTimeMs,
            TimeoutMs = source.TimeoutMs,
        };
    }

    /// <summary>通过反射设置属性的值，用于撤销/重做属性编辑。</summary>
    private static void ApplyPropertyValue(FlowNodeViewModel node, string propertyName, object? value)
    {
        var prop = typeof(FlowNodeViewModel).GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(node, value);
        }
    }

    #endregion

    #region 内部类型

    /// <summary>删除节点操作的撤销状态快照。</summary>
    private sealed class RemoveNodeState
    {
        /// <summary>被删除节点在移除前的完整快照。</summary>
        public FlowNodeViewModel? Node { get; set; }

        /// <summary>与被删除节点关联的所有连线快照。</summary>
        public List<FlowLinkViewModel>? RemovedLinks { get; set; }
    }

    /// <summary>移动节点操作的撤销状态快照。</summary>
    private sealed class MoveNodeState
    {
        /// <summary>被移动节点的唯一标识符。</summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>移动前后的 X 坐标。</summary>
        public double X { get; set; }

        /// <summary>移动前后的 Y 坐标。</summary>
        public double Y { get; set; }
    }

    /// <summary>属性编辑操作的撤销状态快照。</summary>
    private sealed class PropertyEditState
    {
        /// <summary>被编辑节点的唯一标识符。</summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>被修改的属性名称。</summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>修改前的属性值。</summary>
        public object? OldValue { get; set; }

        /// <summary>修改后的属性值。</summary>
        public object? NewValue { get; set; }
    }

    #endregion
}
