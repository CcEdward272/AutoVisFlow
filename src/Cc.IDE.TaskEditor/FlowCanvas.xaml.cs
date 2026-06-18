using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cc.IDE.TaskEditor;

/// <summary>
/// 流程图编辑器可视化控件。
/// 所有节点和连线绘制在统一的 MainCanvas 上，确保鼠标坐标系一致。
/// 每个 FlowNodeView 自行处理鼠标事件（点击选择、拖拽移动）。
/// 画布背景处理滚轮缩放、中键平移和背景点击取消选择。
/// </summary>
public partial class FlowCanvas : UserControl
{
    // 交互状态
    private bool _isPanning;
    private Point _panStart;
    private bool _isSelecting;
    private Point _selStart;

    // 连线拖拽状态
    private bool _isConnecting;
    private FlowNodeViewModel? _connectFrom;
    private string _connectFromPort = "out";
    private Line? _rubberBand;

    public FlowCanvas()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;

        // 背景事件
        MainCanvas.MouseLeftButtonDown += OnBackgroundLeftDown;
        MainCanvas.MouseLeftButtonUp += OnBackgroundLeftUp;
        MainCanvas.MouseMove += OnCanvasMouseMove;
        MainCanvas.MouseWheel += OnCanvasMouseWheel;
        MainCanvas.MouseRightButtonDown += (s, e) =>
        {
            if (_isConnecting) { CancelConnection(); e.Handled = true; }
        };
        MainCanvas.MouseDown += (s, e) =>
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _panStart = e.GetPosition(this);
                MainCanvas.Cursor = Cursors.ScrollAll;
                MainCanvas.CaptureMouse();
            }
        };
        MainCanvas.MouseUp += (s, e) =>
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;
                MainCanvas.Cursor = Cursors.Arrow;
                MainCanvas.ReleaseMouseCapture();
            }
        };

        // Escape 取消连线
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape && _isConnecting)
                CancelConnection();
        };

        // 拖放支持：从工具箱拖入新节点
        MainCanvas.DragOver += OnCanvasDragOver;
        MainCanvas.Drop += OnCanvasDrop;
    }

    private void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("NodeTypeDescriptor"))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void OnCanvasDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData("NodeTypeDescriptor") is not NodeTypeDescriptor descriptor)
            return;

        var dropPos = e.GetPosition(MainCanvas);
        AddNodeAt(descriptor, dropPos.X, dropPos.Y);
        e.Handled = true;
    }

    /// <summary>
    /// 在指定画布位置添加一个新节点（从工具箱拖放调用）。
    /// </summary>
    public void AddNodeAt(NodeTypeDescriptor descriptor, double x, double y)
    {
        if (VM == null) return;

        var nodeVm = new FlowNodeViewModel
        {
            NodeType = descriptor.NodeType,
            DisplayTitle = descriptor.DisplayName,
            X = Math.Round(x / 30) * 30 - 70, // 居中到网格
            Y = Math.Round(y / 30) * 30 - 28,
            Width = descriptor.NodeType switch { "Start" or "End" => 80, "Condition" => 100, _ => 140 },
            Height = descriptor.NodeType switch { "Start" or "End" => 40, "Condition" => 70, _ => 56 }
        };

        VM.Nodes.Add(nodeVm);
        VM.SelectedNode = nodeVm;
        nodeVm.IsSelected = true;

        // 记录撤销操作
        VM.RecordMoveNode(nodeVm, nodeVm.X, nodeVm.Y);
    }

    #region DataContext & Loaded

    private FlowCanvasViewModel? VM => DataContext as FlowCanvasViewModel;
    private readonly Dictionary<FlowNodeViewModel, FlowNodeView> _nodeViews = new();

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is FlowCanvasViewModel oldVm)
        {
            oldVm.Nodes.CollectionChanged -= OnNodesChanged;
            oldVm.Links.CollectionChanged -= OnLinksChanged;
        }
        if (e.NewValue is FlowCanvasViewModel newVm)
        {
            newVm.Nodes.CollectionChanged += OnNodesChanged;
            newVm.Links.CollectionChanged += OnLinksChanged;
            newVm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(FlowCanvasViewModel.Zoom))
                    ZoomIndicator.Text = $"{newVm.Zoom:P0}";
            };
            RebuildAllNodes();
            RedrawAllLinks();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DrawGrid();
        if (VM != null)
        {
            RebuildAllNodes();
            RedrawAllLinks();
        }
    }

    #endregion

    #region 节点管理

    private void OnNodesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            ClearAllNodes();
            RebuildAllNodes();
        }
        else
        {
            if (e.OldItems != null)
                foreach (FlowNodeViewModel vm in e.OldItems) RemoveNodeView(vm);
            if (e.NewItems != null)
                foreach (FlowNodeViewModel vm in e.NewItems) AddNodeView(vm);
        }
        RedrawAllLinks();
    }

    private void OnLinksChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => RedrawAllLinks();

    private void RebuildAllNodes()
    {
        ClearAllNodes();
        if (VM == null) return;
        foreach (var nodeVm in VM.Nodes)
            AddNodeView(nodeVm);
    }

    private void AddNodeView(FlowNodeViewModel nodeVm)
    {
        var view = new FlowNodeView { DataContext = nodeVm };
        Canvas.SetLeft(view, nodeVm.X);
        Canvas.SetTop(view, nodeVm.Y);
        Panel.SetZIndex(view, 1);

        // 节点鼠标事件
        view.MouseLeftButtonDown += OnNodeMouseDown;
        view.MouseMove += OnNodeMouseMove;
        view.MouseLeftButtonUp += OnNodeMouseUp;
        view.MouseRightButtonDown += OnNodeRightClick;

        // 监听 ViewModel 的位置变化以更新 Canvas 坐标
        nodeVm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(FlowNodeViewModel.X))
                Canvas.SetLeft(view, nodeVm.X);
            if (args.PropertyName == nameof(FlowNodeViewModel.Y))
                Canvas.SetTop(view, nodeVm.Y);
            if (args.PropertyName is nameof(FlowNodeViewModel.X) or nameof(FlowNodeViewModel.Y))
                RedrawAllLinks();
        };

        MainCanvas.Children.Add(view);
        _nodeViews[nodeVm] = view;
    }

    private void RemoveNodeView(FlowNodeViewModel nodeVm)
    {
        if (_nodeViews.TryGetValue(nodeVm, out var view))
        {
            MainCanvas.Children.Remove(view);
            _nodeViews.Remove(nodeVm);
        }
    }

    private void ClearAllNodes()
    {
        foreach (var view in _nodeViews.Values)
            MainCanvas.Children.Remove(view);
        _nodeViews.Clear();
    }

    #endregion

    #region 节点鼠标事件

    private bool _isDraggingNode;
    private Point _nodeDragStart;
    private double _nodeStartX, _nodeStartY;
    private FlowNodeViewModel? _draggedVm;

    private void OnNodeMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FlowNodeView view || view.DataContext is not FlowNodeViewModel nodeVm)
            return;

        // ── 连线模式：点击目标节点完成连线 ──────────────────
        if (_isConnecting)
        {
            if (_connectFrom != null && _connectFrom != nodeVm && nodeVm.NodeType != "Start")
            {
                var link = new FlowLinkViewModel
                {
                    FromNode = _connectFrom,
                    ToNode = nodeVm,
                    FromPort = _connectFromPort,
                    ToPort = "in"
                };
                VM!.Links.Add(link);
                RedrawAllLinks();
            }
            CancelConnection();
            e.Handled = true;
            return;
        }

        // 选择
        if (VM != null)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                foreach (var n in VM.Nodes) n.IsSelected = false;
            nodeVm.IsSelected = true;
            VM.SelectedNode = nodeVm;
        }

        // 开始拖拽
        _isDraggingNode = true;
        _draggedVm = nodeVm;
        _nodeDragStart = e.GetPosition(MainCanvas);
        _nodeStartX = nodeVm.X;
        _nodeStartY = nodeVm.Y;
        view.CaptureMouse();
        e.Handled = true;
    }

    private void OnNodeMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingNode || _draggedVm == null) return;

        var pos = e.GetPosition(MainCanvas);
        var dx = pos.X - _nodeDragStart.X;
        var dy = pos.Y - _nodeDragStart.Y;
        _draggedVm.X = _nodeStartX + dx;
        _draggedVm.Y = _nodeStartY + dy;
    }

    private void OnNodeMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingNode || _draggedVm == null) return;

        // Snap to grid
        _draggedVm.X = Math.Round(_draggedVm.X / 30) * 30;
        _draggedVm.Y = Math.Round(_draggedVm.Y / 30) * 30;

        if (VM != null)
            VM.RecordMoveNode(_draggedVm, _nodeStartX, _nodeStartY);

        if (sender is FrameworkElement fe)
            fe.ReleaseMouseCapture();

        _isDraggingNode = false;
        _draggedVm = null;
        RedrawAllLinks();
    }

    private void OnNodeRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not FlowNodeViewModel nodeVm)
            return;

        // 如果正在连线模式，右键取消
        if (_isConnecting)
        {
            CancelConnection();
            return;
        }

        var menu = new ContextMenu();

        // ── 连线菜单项 ──────────────────────────────────────
        if (nodeVm.NodeType is not ("End" or "Comment"))
        {
            var connectItem = new MenuItem { Header = "🔗 从此连线" };
            connectItem.Click += (_, _) => StartConnection(nodeVm, "out");
            menu.Items.Add(connectItem);

            // 条件节点增加 False 分支连线
            if (nodeVm.NodeType == "Condition")
            {
                var connectFailItem = new MenuItem { Header = "🔗 连线到失败分支" };
                connectFailItem.Click += (_, _) => StartConnection(nodeVm, "fail");
                menu.Items.Add(connectFailItem);
            }
            menu.Items.Add(new Separator());
        }

        var deleteItem = new MenuItem { Header = "❌ 删除节点" };
        var bpItem = new MenuItem { Header = "🔴 切换断点" };
        deleteItem.Click += (_, _) =>
        {
            if (VM != null)
            {
                VM.RemoveNodeTarget = nodeVm;
                VM.DeleteSelectedCommand.Execute(null);
            }
        };
        menu.Items.Add(deleteItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(bpItem);
        menu.IsOpen = true;
        e.Handled = true;
    }

    /// <summary>
    /// 开始连线模式。从源节点拖出虚线跟随鼠标。
    /// </summary>
    private void StartConnection(FlowNodeViewModel fromNode, string port)
    {
        _isConnecting = true;
        _connectFrom = fromNode;
        _connectFromPort = port;

        var (sx, sy) = GetOutputPort(fromNode);
        _rubberBand = new Line
        {
            X1 = sx, Y1 = sy, X2 = sx, Y2 = sy,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700")),
            StrokeThickness = 2.5,
            StrokeDashArray = new DoubleCollection { 6, 3 }
        };
        MainCanvas.Children.Add(_rubberBand);
        // 不 CaptureMouse —— 让节点的点击事件正常触发
    }

    /// <summary>
    /// 取消连线模式。
    /// </summary>
    private void CancelConnection()
    {
        _isConnecting = false;
        _connectFrom = null;
        if (_rubberBand != null)
        {
            MainCanvas.Children.Remove(_rubberBand);
            _rubberBand = null;
        }
    }

    #endregion

    #region 背景事件（取消选择、框选、平移）

    private void OnBackgroundLeftDown(object sender, MouseButtonEventArgs e)
    {
        // 连线模式下点击空白取消连线
        if (_isConnecting)
        {
            CancelConnection();
            return;
        }

        // 只处理直接点击 MainCanvas 背景（非子控件）
        if (e.OriginalSource != MainCanvas) return;

        if (VM != null)
        {
            foreach (var n in VM.Nodes) n.IsSelected = false;
            VM.SelectedNode = null;
        }
        _isSelecting = true;
        _selStart = e.GetPosition(MainCanvas);
        MainCanvas.CaptureMouse();
    }

    private void OnBackgroundLeftUp(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = false;
        SelectionRect.Visibility = Visibility.Collapsed;
        MainCanvas.ReleaseMouseCapture();
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(MainCanvas);

        // 连线拖拽：更新橡皮筋
        if (_isConnecting && _rubberBand != null)
        {
            _rubberBand.X2 = pos.X;
            _rubberBand.Y2 = pos.Y;
            return;
        }

        // 中键平移
        if (_isPanning)
        {
            var current = e.GetPosition(this);
            var dx = current.X - _panStart.X;
            var dy = current.Y - _panStart.Y;
            _panStart = current;
            // 平移整个画布内容
            foreach (UIElement child in MainCanvas.Children)
            {
                Canvas.SetLeft(child, Canvas.GetLeft(child) + dx);
                Canvas.SetTop(child, Canvas.GetTop(child) + dy);
            }
            return;
        }

        // 框选
        if (_isSelecting)
        {
            var x = Math.Min(pos.X, _selStart.X);
            var y = Math.Min(pos.Y, _selStart.Y);
            var w = Math.Abs(pos.X - _selStart.X);
            var h = Math.Abs(pos.Y - _selStart.Y);
            SelectionRect.Margin = new Thickness(x, y, 0, 0);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
            SelectionRect.Visibility = Visibility.Visible;
        }
    }

    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (VM != null)
        {
            var delta = e.Delta > 0 ? 0.1 : -0.1;
            VM.Zoom = Math.Clamp(VM.Zoom + delta, 0.25, 3.0);
        }
        e.Handled = true;
    }

    #endregion

    #region 网格

    private void DrawGrid()
    {
        const double gridSize = 30;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
        for (var x = 0; x < 4000; x += (int)gridSize)
        {
            var line = new Line { X1 = x, Y1 = 0, X2 = x, Y2 = 4000,
                Stroke = brush, StrokeThickness = 0.5 };
            Panel.SetZIndex(line, -1);
            MainCanvas.Children.Add(line);
        }
        for (var y = 0; y < 4000; y += (int)gridSize)
        {
            var line = new Line { X1 = 0, Y1 = y, X2 = 4000, Y2 = y,
                Stroke = brush, StrokeThickness = 0.5 };
            Panel.SetZIndex(line, -1);
            MainCanvas.Children.Add(line);
        }
    }

    #endregion

    #region 连线

    private readonly List<UIElement> _linkElements = new();

    private void RedrawAllLinks()
    {
        // 清除旧连线
        foreach (var el in _linkElements)
            MainCanvas.Children.Remove(el);
        _linkElements.Clear();

        if (VM == null) return;

        foreach (var link in VM.Links)
        {
            if (link.FromNode == null || link.ToNode == null) continue;
            DrawLink(link.FromNode, link.ToNode, link);
        }
    }

    private void DrawLink(FlowNodeViewModel from, FlowNodeViewModel to, FlowLinkViewModel link)
    {
        var (x1, y1) = GetOutputPort(from);
        var (x2, y2) = GetInputPort(to);

        var dx = Math.Abs(x2 - x1) * 0.5;
        var color = link.IsSelected ? "#FFD700" : "#666";

        // 贝塞尔曲线
        var geom = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(x1, y1) };
        fig.Segments.Add(new BezierSegment(
            new Point(x1 + dx, y1),
            new Point(x2 - dx, y2),
            new Point(x2, y2), true));
        geom.Figures.Add(fig);

        var path = new Path
        {
            Data = geom,
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        Panel.SetZIndex(path, 0);
        MainCanvas.Children.Add(path);
        _linkElements.Add(path);

        // 箭头
        var arrowSize = 8.0;
        var angle = Math.Atan2(y2 - y1, x2 - x1);
        var ax1 = x2 - arrowSize * Math.Cos(angle - 0.5);
        var ay1 = y2 - arrowSize * Math.Sin(angle - 0.5);
        var ax2 = x2 - arrowSize * Math.Cos(angle + 0.5);
        var ay2 = y2 - arrowSize * Math.Sin(angle + 0.5);

        var arrowGeom = new PathGeometry();
        var arrowFig = new PathFigure { StartPoint = new Point(x2, y2) };
        arrowFig.Segments.Add(new LineSegment(new Point(ax1, ay1), true));
        arrowFig.Segments.Add(new LineSegment(new Point(ax2, ay2), true));
        arrowFig.IsClosed = true;
        arrowGeom.Figures.Add(arrowFig);

        var arrow = new Path
        {
            Data = arrowGeom,
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
        };
        Panel.SetZIndex(arrow, 0);
        MainCanvas.Children.Add(arrow);
        _linkElements.Add(arrow);
    }

    private static (double x, double y) GetOutputPort(FlowNodeViewModel n)
    {
        return n.NodeType switch
        {
            "Start" => (n.X + n.Width / 2, n.Y + n.Height + 4),
            "Condition" => (n.X + n.Width + 4, n.Y + n.Height / 2),
            _ => (n.X + n.Width + 4, n.Y + n.Height / 2)
        };
    }

    private static (double x, double y) GetInputPort(FlowNodeViewModel n)
        => n.NodeType == "Start"
            ? (n.X + n.Width / 2, n.Y - 4)
            : (n.X - 4, n.Y + n.Height / 2);

    #endregion
}
