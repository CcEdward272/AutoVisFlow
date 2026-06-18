using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Cc.IDE.TaskEditor;

namespace Cc.IDE.App;

/// <summary>
/// IDE 主窗口。设置 DataContext 并加载演示流程图。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>布尔值到可见性的转换器，供 XAML 绑定使用。</summary>
    public static readonly IValueConverter BoolToVis =
        new BooleanToVisibilityConverter();

    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        // 订阅输出日志
        _viewModel.OutputLogged += (msg) =>
        {
            Dispatcher.Invoke(() =>
            {
                OutputLog.AppendText(msg + "\n");
                OutputLog.ScrollToEnd();
            });
        };

        Loaded += (_, _) => LoadDemoFlowGraph();
    }

    /// <summary>
    /// 加载一个演示用流程图，展示各种节点类型和连线。
    /// </summary>
    private void LoadDemoFlowGraph()
    {
        var vm = _viewModel.FlowCanvas;

        var startNode = CreateNode("start", "Start", "开始", 100, 300);
        var initNode = CreateNode("init", "TestStep", "初始化变量", 320, 300);
        var conditionNode = CreateNode("cond1", "Condition", "电压检查", 560, 300);
        var measureNode = CreateNode("measure", "TestStep", "测量电压", 800, 200);
        var adjustNode = CreateNode("adjust", "IOAction", "调整输出", 800, 400);
        var delayNode = CreateNode("d1", "Delay", "等待 500ms", 1040, 200);
        var callSubNode = CreateNode("callSub", "CallTask", "调用校准子任务", 1040, 400);
        var passEndNode = CreateNode("passEnd", "End", "通过 ✓", 1300, 200);
        var failEndNode = CreateNode("failEnd", "End", "失败 ✗", 800, 550);

        vm.Nodes.Add(startNode);
        vm.Nodes.Add(initNode);
        vm.Nodes.Add(conditionNode);
        vm.Nodes.Add(measureNode);
        vm.Nodes.Add(adjustNode);
        vm.Nodes.Add(delayNode);
        vm.Nodes.Add(callSubNode);
        vm.Nodes.Add(passEndNode);
        vm.Nodes.Add(failEndNode);

        AddLink(vm, startNode, initNode);
        AddLink(vm, initNode, conditionNode);
        AddLink(vm, conditionNode, measureNode, fromPort: "pass");
        AddLink(vm, conditionNode, failEndNode, fromPort: "fail");
        AddLink(vm, measureNode, delayNode);
        AddLink(vm, delayNode, passEndNode);
        AddLink(vm, adjustNode, callSubNode);
        AddLink(vm, callSubNode, passEndNode);

        // 同时设置演示用的解决方案树
        _viewModel.SolutionExplorer.RootNode = CreateDemoSolutionTree();

        _viewModel.Log("就绪 — 演示项目已加载");
    }

    private static FlowNodeViewModel CreateNode(string id, string nodeType, string title, double x, double y) =>
        new()
        {
            Id = id,
            NodeType = nodeType,
            DisplayTitle = title,
            X = x, Y = y,
            Width = nodeType switch { "Start" or "End" => 80, "Condition" => 100, _ => 140 },
            Height = nodeType switch { "Start" or "End" => 40, "Condition" => 70, _ => 56 }
        };

    private static void AddLink(FlowCanvasViewModel vm, FlowNodeViewModel from, FlowNodeViewModel to, string fromPort = "out")
    {
        vm.Links.Add(new FlowLinkViewModel { FromNode = from, ToNode = to, FromPort = fromPort, ToPort = "in" });
    }

    private static SolutionTreeNode CreateDemoSolutionTree()
    {
        var root = new SolutionTreeNode
        {
            Name = "DemoSolution", NodeType = "Solution",
            DisplayIcon = "📁", IconColor = "#FFD700",
            IsExpanded = true
        };
        var proj = new SolutionTreeNode
        {
            Name = "MainTest", NodeType = "Project",
            DisplayIcon = "📂", IconColor = "#81C784",
            IsExpanded = true
        };
        var tasks = new SolutionTreeNode
        {
            Name = "Tasks", NodeType = "Folder",
            DisplayIcon = "📋", IconColor = "#64B5F6",
            IsExpanded = true
        };
        tasks.Children.Add(new SolutionTreeNode
        { Name = "MainSequence.yourtask", NodeType = "Task", DisplayIcon = "⚡", IconColor = "#FFB74D" });
        tasks.Children.Add(new SolutionTreeNode
        { Name = "InitSub.yourtask", NodeType = "Task", DisplayIcon = "⚡", IconColor = "#FFB74D" });
        tasks.Children.Add(new SolutionTreeNode
        { Name = "Calibration.yourtask", NodeType = "Task", DisplayIcon = "⚡", IconColor = "#FFB74D" });

        var insts = new SolutionTreeNode
        {
            Name = "Instruments", NodeType = "Folder",
            DisplayIcon = "🔧", IconColor = "#CE93D8",
            IsExpanded = true
        };
        insts.Children.Add(new SolutionTreeNode
        { Name = "DMM_Agilent34401A.yourinst", NodeType = "Instrument", DisplayIcon = "📡", IconColor = "#E57373" });
        insts.Children.Add(new SolutionTreeNode
        { Name = "Power_RigolDP832.yourinst", NodeType = "Instrument", DisplayIcon = "🔌", IconColor = "#E57373" });

        var maps = new SolutionTreeNode
        {
            Name = "IO Mappings", NodeType = "Folder",
            DisplayIcon = "⚙", IconColor = "#80CBC4",
            IsExpanded = true
        };
        maps.Children.Add(new SolutionTreeNode
        { Name = "PLC_IO_map.yourmap", NodeType = "IOMap", DisplayIcon = "🔗", IconColor = "#AED581" });

        proj.Children.Add(tasks);
        proj.Children.Add(insts);
        proj.Children.Add(maps);
        root.Children.Add(proj);
        return root;
    }
}
