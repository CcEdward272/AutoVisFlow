using System.Windows.Controls;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 属性面板视图。绑定到 <see cref="FlowCanvasViewModel.SelectedNode"/>，
/// 显示当前选中节点的可编辑属性。
/// </summary>
public partial class PropertyPaneView : UserControl
{
    public PropertyPaneView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is FlowCanvasViewModel vm)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(FlowCanvasViewModel.SelectedNode))
                        UpdateVisibility(vm);
                };
                UpdateVisibility(vm);
            }
        };
    }

    private void UpdateVisibility(FlowCanvasViewModel vm)
    {
        var hasSelection = vm.SelectedNode != null;
        EmptySelection.Visibility = hasSelection
            ? System.Windows.Visibility.Collapsed
            : System.Windows.Visibility.Visible;
    }
}
