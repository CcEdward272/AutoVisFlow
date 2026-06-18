using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程图单个节点的 ViewModel。
/// 封装画布上的位置、选中状态以及节点的所有业务属性。
/// 支持 TestStep、CallTask、Condition、Loop、Delay、IOAction 等节点类型的所有子结构。
/// </summary>
public class FlowNodeViewModel : ViewModelBase
{
    #region 核心属性

    private string _id = Guid.NewGuid().ToString("N");
    private string _nodeType = "Comment";
    private string _title = string.Empty;
    private string _description = string.Empty;
    private bool _enabled = true;
    private double _x;
    private double _y;
    private double _width = 160;
    private double _height = 60;
    private bool _isSelected;

    /// <summary>节点在所属任务中的唯一标识符。</summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// 节点类型：Start | End | TestStep | CallTask | Condition | Loop | Delay | IOAction | Comment。
    /// 更改节点类型时应重新评估可见的子结构属性。
    /// </summary>
    public string NodeType
    {
        get => _nodeType;
        set => SetProperty(ref _nodeType, value);
    }

    /// <summary>节点上显示的标题文本。</summary>
    public new string DisplayTitle
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>可选的较长描述文本，显示为工具提示或详情面板。</summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>当为 <c>false</c> 时，此节点在执行期间被跳过。</summary>
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    /// <summary>节点在流程图画布上的 X 坐标。</summary>
    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    /// <summary>节点在流程图画布上的 Y 坐标。</summary>
    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    /// <summary>节点在流程图画布上的宽度。</summary>
    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    /// <summary>节点在流程图画布上的高度。</summary>
    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    /// <summary>节点是否在画布上被选中。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    #endregion

    #region TestStep 字段（PreIO / InstrumentCalls / PostIO）

    private List<IOActionDefinition>? _preIOActions;
    private List<InstrumentCallDefinition>? _instrumentCalls;
    private List<IOActionDefinition>? _postIOActions;

    /// <summary>在执行仪器调用之前执行的 I/O 动作列表。当 NodeType 为 "TestStep" 时有效。</summary>
    [JsonIgnore]
    public List<IOActionDefinition>? PreIOActions
    {
        get => _preIOActions;
        set => SetProperty(ref _preIOActions, value);
    }

    /// <summary>此节点的仪器调用列表。当 NodeType 为 "TestStep" 时有效。</summary>
    [JsonIgnore]
    public List<InstrumentCallDefinition>? InstrumentCalls
    {
        get => _instrumentCalls;
        set => SetProperty(ref _instrumentCalls, value);
    }

    /// <summary>在执行仪器调用之后执行的 I/O 动作列表（始终执行，包括失败时）。当 NodeType 为 "TestStep" 时有效。</summary>
    [JsonIgnore]
    public List<IOActionDefinition>? PostIOActions
    {
        get => _postIOActions;
        set => SetProperty(ref _postIOActions, value);
    }

    #endregion

    #region CallTask 字段

    private TaskCallDefinition? _taskCall;

    /// <summary>子任务调用定义。当 NodeType 为 "CallTask" 时有效。</summary>
    [JsonIgnore]
    public TaskCallDefinition? TaskCall
    {
        get => _taskCall;
        set => SetProperty(ref _taskCall, value);
    }

    #endregion

    #region Condition 字段

    private List<ConditionBranchDefinition>? _branches;
    private string? _defaultBranchTargetId;

    /// <summary>有序的条件分支列表。当 NodeType 为 "Condition" 时有效。</summary>
    [JsonIgnore]
    public List<ConditionBranchDefinition>? Branches
    {
        get => _branches;
        set => SetProperty(ref _branches, value);
    }

    /// <summary>当所有条件分支均不匹配时的默认跳转目标节点 ID。当 NodeType 为 "Condition" 时有效。</summary>
    public string? DefaultBranchTargetId
    {
        get => _defaultBranchTargetId;
        set => SetProperty(ref _defaultBranchTargetId, value);
    }

    #endregion

    #region Loop 字段

    private LoopDefinition? _loop;
    private string? _loopBodyStartNodeId;
    private string? _loopExitNodeId;

    /// <summary>循环配置。当 NodeType 为 "Loop" 时有效。</summary>
    [JsonIgnore]
    public LoopDefinition? Loop
    {
        get => _loop;
        set => SetProperty(ref _loop, value);
    }

    /// <summary>循环体内部第一个节点的 ID。当 NodeType 为 "Loop" 时有效。</summary>
    public string? LoopBodyStartNodeId
    {
        get => _loopBodyStartNodeId;
        set => SetProperty(ref _loopBodyStartNodeId, value);
    }

    /// <summary>循环结束后跳转的目标节点 ID。当 NodeType 为 "Loop" 时有效。</summary>
    public string? LoopExitNodeId
    {
        get => _loopExitNodeId;
        set => SetProperty(ref _loopExitNodeId, value);
    }

    #endregion

    #region Delay 字段

    private int _delayMs;

    /// <summary>延时持续时间（毫秒）。当 NodeType 为 "Delay" 时有效。</summary>
    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, value);
    }

    #endregion

    #region 独立 IOAction 字段

    private IOActionDefinition? _ioAction;

    /// <summary>待执行的独立 I/O 动作。当 NodeType 为 "IOAction" 时有效。</summary>
    [JsonIgnore]
    public IOActionDefinition? IOAction
    {
        get => _ioAction;
        set => SetProperty(ref _ioAction, value);
    }

    #endregion

    #region 执行策略

    private TaskExecutionPolicy? _executionPolicy;

    /// <summary>
    /// 可选的节点级执行策略。当不为 null 时，覆盖任务级别的 <see cref="TaskExecutionPolicy"/>。
    /// </summary>
    [JsonIgnore]
    public TaskExecutionPolicy? ExecutionPolicy
    {
        get => _executionPolicy;
        set => SetProperty(ref _executionPolicy, value);
    }

    #endregion

    #region 计算属性

    /// <summary>当此节点为起始标记时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsStart => NodeType == "Start";

    /// <summary>当此节点为结束标记时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsEnd => NodeType == "End";

    /// <summary>
    /// 当此节点包含可执行逻辑（而非仅布局或注释节点）时返回 <c>true</c>。
    /// </summary>
    [JsonIgnore]
    public bool IsExecutable => NodeType is not ("Start" or "End" or "Comment");

    #endregion
}
