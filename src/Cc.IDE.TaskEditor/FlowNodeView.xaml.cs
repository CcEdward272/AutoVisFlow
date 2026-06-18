using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程图中单个节点的可视化控件。根据 NodeType 显示不同数量的端口：
/// - Start: 仅右侧输出
/// - End: 仅左侧输入
/// - Condition: 左侧输入 + 右上 True 输出 + 右下 False 输出
/// - Comment: 无端口
/// - 其他: 左侧输入 + 右侧输出
/// </summary>
public partial class FlowNodeView : UserControl
{
    public FlowNodeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // 端口引用供 FlowCanvas 拖拽连线使用
    public FrameworkElement InputPort => InputPortHit;
    public FrameworkElement OutputPort => OutputPortHit1;
    public FrameworkElement OutputPort2 => OutputPortHit2;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is FlowNodeViewModel vm)
        {
            ApplyNodeStyle(vm.NodeType);
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(FlowNodeViewModel.NodeType))
                    ApplyNodeStyle(vm.NodeType);
                if (args.PropertyName == nameof(FlowNodeViewModel.IsSelected))
                    UpdateSelection(vm.IsSelected);
            };
            UpdateSelection(vm.IsSelected);
        }
    }

    private void ApplyNodeStyle(string nodeType)
    {
        var (fill, stroke, corner) = nodeType switch
        {
            "Start" => ("#388E3C", "#4CAF50", 4.0),
            "End" => ("#C62828", "#F44336", 4.0),
            "TestStep" => ("#1565C0", "#2196F3", 2.0),
            "CallTask" => ("#6A1B9A", "#9C27B0", 2.0),
            "Condition" => ("#E65100", "#FF9800", 0.0),
            "Loop" => ("#00838F", "#00BCD4", 2.0),
            "IOAction" => ("#37474F", "#607D8B", 2.0),
            "Delay" => ("#4E342E", "#795548", 2.0),
            "Comment" => ("#F9A825", "#FBC02D", 2.0),
            _ => ("#424242", "#757575", 2.0)
        };

        NodeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fill));
        NodeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(stroke));
        NodeBorder.CornerRadius = new CornerRadius(corner);
        TitleBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(stroke));

        // ===== 端口可见性规则 =====
        // 输入端口
        var hasInput = nodeType is not "Start" and not "Comment";
        InputPortGrid.Visibility = hasInput ? Visibility.Visible : Visibility.Collapsed;

        // 输出端口 1（默认）
        var hasOutput1 = nodeType is not "End" and not "Comment";
        OutputPortGrid1.Visibility = hasOutput1 ? Visibility.Visible : Visibility.Collapsed;

        // 输出端口 2（仅条件节点有 False 分支）
        var hasOutput2 = nodeType == "Condition";
        if (OutputPortGrid2 != null)
            OutputPortGrid2.Visibility = hasOutput2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSelection(bool isSelected)
    {
        if (SelectionBorder != null)
        {
            SelectionBorder.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
            SelectionBorder.BorderThickness = isSelected ? new Thickness(2) : new Thickness(0);
        }
    }
}

/// <summary>
/// 保留用于 XAML 资源引用的样式选择器。
/// </summary>
public class NodeStyleSelector : StyleSelector
{
    public override Style? SelectStyle(object item, DependencyObject container) => null;
}
