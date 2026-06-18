using System;
using System.Windows.Input;
using System.Windows.Media;
using Cc.IDE.Mvvm;
using Cc.IDE.TaskEditor;

namespace Cc.IDE.App;

/// <summary>
/// IDE 主窗口的 ViewModel。管理所有面板、状态栏指示器和工具栏命令。
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private bool _isSolutionExplorerVisible = true;
    private bool _isToolboxVisible = true;
    private bool _isPropertyPaneVisible = true;
    private bool _isOutputPaneVisible = true;
    private string _statusText = "就绪";
    private bool _isPlcConnected;
    private bool _isCanConnected;
    private bool _isInstrumentConnected;

    // ─── 子 ViewModel ───────────────────────────────────────────

    public FlowCanvasViewModel FlowCanvas { get; } = new();
    public ToolboxViewModel Toolbox { get; } = new();
    public SolutionExplorerViewModel SolutionExplorer { get; } = new();

    // ─── 事件 ──────────────────────────────────────────────────

    /// <summary>输出日志事件。MainWindow 订阅此事件将日志追加到输出面板。</summary>
    public event Action<string>? OutputLogged;

    /// <summary>向输出面板写入一条日志消息。</summary>
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        OutputLogged?.Invoke($"[{timestamp}] {message}");
    }

    // ─── 面板可见性 ──────────────────────────────────────────────

    public bool IsSolutionExplorerVisible
    { get => _isSolutionExplorerVisible; set => SetProperty(ref _isSolutionExplorerVisible, value); }

    public bool IsToolboxVisible
    { get => _isToolboxVisible; set => SetProperty(ref _isToolboxVisible, value); }

    public bool IsPropertyPaneVisible
    { get => _isPropertyPaneVisible; set => SetProperty(ref _isPropertyPaneVisible, value); }

    public bool IsOutputPaneVisible
    { get => _isOutputPaneVisible; set => SetProperty(ref _isOutputPaneVisible, value); }

    // ─── 状态栏 ──────────────────────────────────────────────────

    public string StatusText
    { get => _statusText; set => SetProperty(ref _statusText, value); }

    public bool IsPlcConnected
    {
        get => _isPlcConnected;
        set { SetProperty(ref _isPlcConnected, value); UpdatePlcStatus(); }
    }

    public bool IsCanConnected
    {
        get => _isCanConnected;
        set { SetProperty(ref _isCanConnected, value); UpdateCanStatus(); }
    }

    public bool IsInstrumentConnected
    {
        get => _isInstrumentConnected;
        set { SetProperty(ref _isInstrumentConnected, value); UpdateInstrumentStatus(); }
    }

    private string _plcStatusText = "○ 未连接";
    private string _canStatusText = "○ 未连接";
    private string _instrumentStatusText = "○ 未连接";

    public string PlcStatusText
    { get => _plcStatusText; set => SetProperty(ref _plcStatusText, value); }

    public string CanStatusText
    { get => _canStatusText; set => SetProperty(ref _canStatusText, value); }

    public string InstrumentStatusText
    { get => _instrumentStatusText; set => SetProperty(ref _instrumentStatusText, value); }

    // 状态颜色（用字符串方便 XAML 绑定）
    private string _plcStatusColor = "#AAA";
    private string _canStatusColor = "#AAA";
    private string _instrumentStatusColor = "#AAA";

    public string PlcStatusColor
    { get => _plcStatusColor; set => SetProperty(ref _plcStatusColor, value); }

    public string CanStatusColor
    { get => _canStatusColor; set => SetProperty(ref _canStatusColor, value); }

    public string InstrumentStatusColor
    { get => _instrumentStatusColor; set => SetProperty(ref _instrumentStatusColor, value); }

    private void UpdatePlcStatus()
    {
        PlcStatusText = IsPlcConnected ? "● 已连接" : "○ 未连接";
        PlcStatusColor = IsPlcConnected ? "#4CAF50" : "#AAA";
    }

    private void UpdateCanStatus()
    {
        CanStatusText = IsCanConnected ? "● 已连接" : "○ 未连接";
        CanStatusColor = IsCanConnected ? "#4CAF50" : "#AAA";
    }

    private void UpdateInstrumentStatus()
    {
        InstrumentStatusText = IsInstrumentConnected ? "● 已连接" : "○ 未连接";
        InstrumentStatusColor = IsInstrumentConnected ? "#4CAF50" : "#AAA";
    }

    // ─── 命令 ──────────────────────────────────────────────────

    public ICommand ToggleSolutionExplorerCommand { get; }
    public ICommand ToggleToolboxCommand { get; }
    public ICommand TogglePropertyPaneCommand { get; }
    public ICommand ToggleOutputPaneCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PauseCommand { get; }

    public MainWindowViewModel()
    {
        ToggleSolutionExplorerCommand = new RelayCommand(
            () => IsSolutionExplorerVisible = !IsSolutionExplorerVisible);
        ToggleToolboxCommand = new RelayCommand(
            () => IsToolboxVisible = !IsToolboxVisible);
        TogglePropertyPaneCommand = new RelayCommand(
            () => IsPropertyPaneVisible = !IsPropertyPaneVisible);
        ToggleOutputPaneCommand = new RelayCommand(
            () => IsOutputPaneVisible = !IsOutputPaneVisible);

        StartCommand = new RelayCommand(() =>
        {
            StatusText = "运行中...";
            Log("▶ 任务开始执行");
        });

        StopCommand = new RelayCommand(() =>
        {
            StatusText = "已停止";
            Log("⏹ 任务已停止");
        });

        PauseCommand = new RelayCommand(() =>
        {
            StatusText = "已暂停";
            Log("⏸ 任务已暂停");
        });
    }
}
