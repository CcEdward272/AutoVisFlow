using Cc.IDE.Mvvm;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 属性面板 ViewModel。
/// 当用户在画布上选中节点时，显示该节点的可编辑属性。
/// 根据选中节点的类型（TestStep、Condition、Loop 等）动态显示/隐藏对应的属性区域。
/// </summary>
public class PropertyPaneViewModel : ViewModelBase
{
    private FlowNodeViewModel? _selectedNode;

    /// <summary>
    /// 当前在属性面板中显示和编辑的节点。
    /// 当值为 <c>null</c> 时，表示画布上无选中节点，面板显示为空。
    /// </summary>
    public FlowNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(IsTestStep));
                OnPropertyChanged(nameof(IsCallTask));
                OnPropertyChanged(nameof(IsCondition));
                OnPropertyChanged(nameof(IsLoop));
                OnPropertyChanged(nameof(IsDelay));
                OnPropertyChanged(nameof(IsIOAction));
            }
        }
    }

    /// <summary>获取是否有节点被选中。</summary>
    public bool HasSelection => SelectedNode != null;

    /// <summary>当前选中节点是否属于 TestStep 类型。</summary>
    public bool IsTestStep => SelectedNode?.NodeType == "TestStep";

    /// <summary>当前选中节点是否属于 CallTask 类型。</summary>
    public bool IsCallTask => SelectedNode?.NodeType == "CallTask";

    /// <summary>当前选中节点是否属于 Condition 类型。</summary>
    public bool IsCondition => SelectedNode?.NodeType == "Condition";

    /// <summary>当前选中节点是否属于 Loop 类型。</summary>
    public bool IsLoop => SelectedNode?.NodeType == "Loop";

    /// <summary>当前选中节点是否属于 Delay 类型。</summary>
    public bool IsDelay => SelectedNode?.NodeType == "Delay";

    /// <summary>当前选中节点是否属于 IOAction 类型。</summary>
    public bool IsIOAction => SelectedNode?.NodeType == "IOAction";
}
