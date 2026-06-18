using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 工具箱视图。支持从工具箱拖拽节点到流程图编辑器。
/// </summary>
public partial class ToolboxView : UserControl
{
    private Point _dragStartPoint;
    private bool _isDragging;

    public ToolboxView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.NewValue is ToolboxViewModel vm)
                CategoryList.ItemsSource = vm.Categories;
        };
    }

    /// <summary>
    /// 记录拖拽起始位置。
    /// </summary>
    private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isDragging = false;
    }

    /// <summary>
    /// 当鼠标移动超过阈值时启动拖拽操作。
    /// 通过 DataObject 携带 NodeTypeDescriptor 数据。
    /// </summary>
    private void OnItemMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPos = e.GetPosition(null);
        var diff = _dragStartPoint - currentPos;

        if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            _isDragging = true;

            if (sender is Border border && border.DataContext is NodeTypeDescriptor descriptor)
            {
                var data = new DataObject("NodeTypeDescriptor", descriptor);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
            }
            _isDragging = false;
        }
    }
}
