using System;
using System.Text.Json.Serialization;
using Cc.IDE.Mvvm;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程图中单条连线的 ViewModel。
/// 表示两个 <see cref="FlowNodeViewModel"/> 之间的有向边，
/// 携带端口信息、标签和可选的条件表达式。
/// </summary>
public class FlowLinkViewModel : ViewModelBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private FlowNodeViewModel? _fromNode;
    private FlowNodeViewModel? _toNode;
    private string _fromPort = string.Empty;
    private string _toPort = string.Empty;
    private string _label = string.Empty;
    private string _condition = string.Empty;
    private bool _isSelected;

    /// <summary>连线在画布上的唯一标识符。</summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>连线的源节点。</summary>
    public FlowNodeViewModel? FromNode
    {
        get => _fromNode;
        set => SetProperty(ref _fromNode, value);
    }

    /// <summary>连线的目标节点。</summary>
    public FlowNodeViewModel? ToNode
    {
        get => _toNode;
        set => SetProperty(ref _toNode, value);
    }

    /// <summary>
    /// 源节点上的输出端口（例如 "out"、"success"、"failure"）。
    /// "out" 是标准的单输出端口。
    /// </summary>
    public string FromPort
    {
        get => _fromPort;
        set => SetProperty(ref _fromPort, value);
    }

    /// <summary>目标节点上的输入端口，通常为 "in"。</summary>
    public string ToPort
    {
        get => _toPort;
        set => SetProperty(ref _toPort, value);
    }

    /// <summary>连线上显示的可选标签文本。</summary>
    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    /// <summary>
    /// 可选的条件表达式。当非空时，仅当此布尔表达式求值为 <c>true</c> 时才会遍历此连线。
    /// </summary>
    public string Condition
    {
        get => _condition;
        set => SetProperty(ref _condition, value);
    }

    /// <summary>连线是否在画布上被选中。</summary>
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
