using System.Collections.ObjectModel;
using System.Windows.Input;
using Cc.IDE.Mvvm;

namespace Cc.IDE.App;

/// <summary>
/// 解决方案资源管理器 ViewModel。
/// 以树形结构展示解决方案、工程、Task、仪器和 IO 映射的层级关系。
/// 支持节点选择、展开和双击打开操作。
/// </summary>
public sealed class SolutionExplorerViewModel : ViewModelBase
{
    private string? _solutionPath;
    private SolutionTreeNode? _rootNode;
    private SolutionTreeNode? _selectedItem;

    /// <summary>
    /// 获取或设置当前解决方案文件路径。
    /// 为 <c>null</c> 表示尚未加载解决方案。
    /// </summary>
    public string? SolutionPath
    {
        get => _solutionPath;
        set => SetProperty(ref _solutionPath, value);
    }

    /// <summary>
    /// 获取或设置树形根节点。解决方案节点作为树的顶层节点，
    /// 其子节点包含工程、仪器配置和 IO 映射文件。
    /// </summary>
    public SolutionTreeNode? RootNode
    {
        get => _rootNode;
        set
        {
            if (SetProperty(ref _rootNode, value))
                OnPropertyChanged(nameof(HasSolution));
        }
    }

    /// <summary>
    /// 获取或设置当前选中的树节点。
    /// 属性面板根据选中的节点类型显示对应的编辑界面。
    /// </summary>
    public SolutionTreeNode? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    /// <summary>
    /// 获取是否已加载解决方案。当 <see cref="RootNode"/> 不为 <c>null</c> 时为 <c>true</c>。
    /// </summary>
    public bool HasSolution => RootNode != null;

    /// <summary>
    /// 获取刷新解决方案树的命令。从磁盘重新加载所有文件并重建树结构。
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 获取打开选定树节点对应编辑器（Task/仪器/IO 映射）的命令。
    /// 接收 <see cref="SolutionTreeNode"/> 作为命令参数。
    /// </summary>
    public ICommand OpenItemCommand { get; }

    /// <summary>
    /// 初始化 <see cref="SolutionExplorerViewModel"/> 的新实例。
    /// 创建刷新和打开项的命令绑定。
    /// </summary>
    public SolutionExplorerViewModel()
    {
        RefreshCommand = new RelayCommand(OnRefresh);
        OpenItemCommand = new RelayCommand(
            execute: p => OnOpenItem(p as SolutionTreeNode),
            canExecute: p => p is SolutionTreeNode);
    }

    /// <summary>
    /// 刷新解决方案树。从磁盘重新加载解决方案文件、工程和仪器配置，
    /// 重建完整的树形节点结构。
    /// </summary>
    private void OnRefresh()
    {
        // Phase 5 placeholder: 从磁盘重新加载
    }

    /// <summary>
    /// 打开指定树节点对应的编辑器。
    /// 根据节点类型（Task、Instrument、IOMap）打开对应的编辑器标签页。
    /// </summary>
    /// <param name="node">要打开的解决方案树节点。</param>
    private void OnOpenItem(SolutionTreeNode? node)
    {
        if (node == null) return;
        // Phase 5 placeholder: 根据 NodeType 打开对应编辑器
    }
}

/// <summary>
/// 解决方案树节点模型。表示解决方案资源管理器树形结构中的单个节点。
/// 支持任意深度的子节点嵌套，以表示解决方案、工程、Task、仪器和 IO 映射的层级关系。
/// </summary>
public sealed class SolutionTreeNode
{
    /// <summary>
    /// 获取或设置节点的唯一标识符。通常为文件的绝对路径或解决方案内部 ID。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置节点在树中显示的友好名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置节点类型标识。可选值："Solution"、"Project"、"Task"、"Instrument"、"IOMap"。
    /// </summary>
    public string NodeType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置节点关联的文件路径。对于纯逻辑节点可为 <c>null</c>。
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 获取或设置树节点图标（emoji 字符）。
    /// </summary>
    public string DisplayIcon { get; set; } = "📄";

    /// <summary>
    /// 获取或设置图标颜色（十六进制字符串，如 "#FFD700"）。
    /// </summary>
    public string IconColor { get; set; } = "#888";

    /// <summary>
    /// 获取或设置子节点集合。用于构建解决方案的层级树形结构。
    /// </summary>
    public ObservableCollection<SolutionTreeNode> Children { get; set; } = new();

    /// <summary>
    /// 获取或设置节点在树视图中是否已展开。
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// 获取或设置节点是否处于选中状态。
    /// </summary>
    public bool IsSelected { get; set; }
}
