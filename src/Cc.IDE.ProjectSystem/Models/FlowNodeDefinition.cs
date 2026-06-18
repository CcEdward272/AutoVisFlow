using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// 任务流程图中单个节点的定义。每个节点通过 <see cref="NodeType"/> 表示其类型，
/// 不同的节点类型使用不同的子结构（PreIO、InstrumentCalls、TaskCall、Branches 等）。
/// </summary>
public sealed class FlowNodeDefinition
{
    /// <summary>节点在所属任务中的唯一标识符。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 节点类型：Start | End | TestStep | CallTask | Condition | Loop | Delay | IOAction | Comment
    /// </summary>
    public string NodeType { get; set; } = "Comment";

    /// <summary>节点上显示的标题文本。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>可选的较长描述文本，显示为工具提示或详情面板。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>当为 <c>false</c> 时，此节点在执行期间被跳过。</summary>
    public bool Enabled { get; set; } = true;

    // ─── TestStep 字段（PreIO / InstrumentCalls / PostIO） ────────

    /// <summary>在执行仪器调用之前执行的 I/O 动作列表。</summary>
    public List<IOActionDefinition>? PreIOActions { get; set; }

    /// <summary>此节点的仪器调用列表。</summary>
    public List<InstrumentCallDefinition>? InstrumentCalls { get; set; }

    /// <summary>在执行仪器调用之后执行的 I/O 动作列表（始终执行，包括失败时）。</summary>
    public List<IOActionDefinition>? PostIOActions { get; set; }

    // ─── CallTask 字段 ────────────────────────────────────────────

    /// <summary>子任务调用定义。当 NodeType 为 "CallTask" 时有效。</summary>
    public TaskCallDefinition? TaskCall { get; set; }

    // ─── Condition 字段 ───────────────────────────────────────────

    /// <summary>有序的条件分支列表。当 NodeType 为 "Condition" 时有效。</summary>
    public List<ConditionBranchDefinition>? Branches { get; set; }

    /// <summary>当所有条件分支均不匹配时的默认跳转目标节点 ID。</summary>
    public string? DefaultBranchTargetId { get; set; }

    // ─── Loop 字段 ────────────────────────────────────────────────

    /// <summary>循环配置。当 NodeType 为 "Loop" 时有效。</summary>
    public LoopDefinition? Loop { get; set; }

    /// <summary>循环体内部第一个节点的 ID。</summary>
    public string? LoopBodyStartNodeId { get; set; }

    /// <summary>循环结束后跳转的目标节点 ID。</summary>
    public string? LoopExitNodeId { get; set; }

    // ─── Delay 字段 ───────────────────────────────────────────────

    /// <summary>延时持续时间（毫秒）。当 NodeType 为 "Delay" 时有效。</summary>
    public int DelayMs { get; set; }

    // ─── 独立 IOAction 字段 ───────────────────────────────────────

    /// <summary>待执行的独立 I/O 动作。当 NodeType 为 "IOAction" 时有效。</summary>
    public IOActionDefinition? IOAction { get; set; }

    // ─── 画布位置 ─────────────────────────────────────────────────

    /// <summary>节点在流程图画布上的 X 坐标。</summary>
    public double X { get; set; }

    /// <summary>节点在流程图画布上的 Y 坐标。</summary>
    public double Y { get; set; }

    /// <summary>节点在流程图画布上的宽度。</summary>
    public double Width { get; set; }

    /// <summary>节点在流程图画布上的高度。</summary>
    public double Height { get; set; }

    // ─── 节点级执行策略覆盖 ───────────────────────────────────────

    /// <summary>
    /// 可选的节点级执行策略。当不为 null 时，覆盖任务级别的 <see cref="TaskExecutionPolicy"/>。
    /// </summary>
    public TaskExecutionPolicy? ExecutionPolicy { get; set; }

    // ─── 计算辅助属性 ─────────────────────────────────────────────

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
}
