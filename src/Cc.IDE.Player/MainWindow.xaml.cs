using System.Windows;
using Cc.IDE.Runtime;
using Cc.IDE.PLC;
using Cc.IDE.CAN;

namespace Cc.IDE.Player;

/// <summary>
/// Player 主窗口。创建 ViewModel 并设置 DataContext。
/// </summary>
public partial class MainWindow : Window
{
    private readonly PlayerViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = CreateViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// 创建 PlayerViewModel 并注入运行时服务。
    /// </summary>
    private static PlayerViewModel CreateViewModel()
    {
        var plcService = new PLCService();
        var canService = new CANService();
        var ioService = new IOExecutionService(plcService, canService);
        var runtime = new RuntimeHost(ioService, instrumentManager: null);
        return new PlayerViewModel(runtime);
    }
}
