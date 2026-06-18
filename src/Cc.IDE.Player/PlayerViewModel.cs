using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;
using Cc.IDE.Runtime;

namespace Cc.IDE.Player;

/// <summary>
/// Player 操作员界面的 ViewModel。
/// 管理运行控制、结果统计、序列号输入和扫码枪集成。
/// </summary>
public sealed class PlayerViewModel : ViewModelBase
{
    private readonly IRuntimeHost _runtime;
    private CancellationTokenSource? _runCts;

    // ─── 状态 ─────────────────────────────────────────────────

    private bool _isRunning;
    private bool _isPaused;
    private string _statusText = "就绪";
    private string _serialNumber = "";
    private string _stationId = "ST-01";
    private string _operatorName = "--";

    public bool IsRunning { get => _isRunning; set { SetProperty(ref _isRunning, value); OnPropertyChanged(nameof(IsNotRunning)); } }
    public bool IsNotRunning => !IsRunning;
    public bool IsPaused { get => _isPaused; set => SetProperty(ref _isPaused, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string SerialNumber { get => _serialNumber; set => SetProperty(ref _serialNumber, value); }
    public string StationId { get => _stationId; set => SetProperty(ref _stationId, value); }
    public string OperatorName { get => _operatorName; set => SetProperty(ref _operatorName, value); }

    // ─── 统计 ─────────────────────────────────────────────────

    private int _passedCount;
    private int _failedCount;
    private int _totalCount;

    public int PassedCount { get => _passedCount; set { SetProperty(ref _passedCount, value); UpdateStats(); } }
    public int FailedCount { get => _failedCount; set { SetProperty(ref _failedCount, value); UpdateStats(); } }
    public int TotalCount { get => _totalCount; set { SetProperty(ref _totalCount, value); UpdateStats(); } }
    public string PassRateText { get => _passRateText; set => SetProperty(ref _passRateText, value); }
    private string _passRateText = "--%";

    private void UpdateStats()
    {
        PassRateText = TotalCount > 0 ? $"{(double)PassedCount / TotalCount * 100:F1}%" : "--%";
    }

    // ─── PLC/CAN 状态 ────────────────────────────────────────

    private string _plcStatus = "○ 未连接";
    private string _instrumentStatus = "○ 未连接";
    private bool _isPlcConnected;
    private bool _isInstrumentConnected;

    public string PlcStatusText { get => _plcStatus; set => SetProperty(ref _plcStatus, value); }
    public string InstrumentStatusText { get => _instrumentStatus; set => SetProperty(ref _instrumentStatus, value); }
    public bool IsPlcConnected { get => _isPlcConnected; set { SetProperty(ref _isPlcConnected, value); PlcStatusText = value ? "● 已连接" : "○ 未连接"; } }
    public bool IsInstrumentConnected { get => _isInstrumentConnected; set { SetProperty(ref _isInstrumentConnected, value); InstrumentStatusText = value ? "● 已连接" : "○ 未连接"; } }

    // ─── 结果列表 ────────────────────────────────────────────

    public ObservableCollection<TestResultItem> Results { get; } = new();

    // ─── 命令 ─────────────────────────────────────────────────

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PauseCommand { get; }

    /// <summary>当前要运行的任务（由外部设置）。</summary>
    public TaskDefinition? CurrentTask { get; set; }

    public PlayerViewModel(IRuntimeHost runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

        StartCommand = new AsyncRelayCommand(OnStartAsync, () => IsNotRunning);
        StopCommand = new RelayCommand(OnStop, () => IsRunning);
        PauseCommand = new RelayCommand(OnPause, () => IsRunning);
    }

    private async Task OnStartAsync()
    {
        if (CurrentTask == null)
        {
            StatusText = "错误: 未加载任务";
            return;
        }
        if (string.IsNullOrWhiteSpace(SerialNumber))
        {
            StatusText = "错误: 请输入序列号";
            return;
        }

        IsRunning = true;
        IsPaused = false;
        StatusText = "测试运行中...";
        _runCts = new CancellationTokenSource();

        var options = new RuntimeRunOptions
        {
            GlobalTimeoutMs = 300_000, // 5 minutes
            EnableDebug = false
        };

        try
        {
            var result = await _runtime.RunAsync(CurrentTask, options, _runCts.Token);

            // 添加到结果列表
            var item = new TestResultItem
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                SerialNumber = this.SerialNumber,
                TaskName = CurrentTask.Name,
                Result = result.Status,
                Duration = $"{result.DurationMs}ms",
                Detail = result.FailureReason ?? ""
            };
            Results.Insert(0, item);

            // 更新统计
            TotalCount++;
            if (result.Status == "Passed") PassedCount++;
            else FailedCount++;

            StatusText = result.Status == "Passed" ? "测试通过 ✓" : $"测试失败: {result.FailureReason}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "测试已取消";
            TotalCount++;
        }
        catch (Exception ex)
        {
            StatusText = $"测试异常: {ex.Message}";
            TotalCount++; FailedCount++;
        }
        finally
        {
            IsRunning = false;
            IsPaused = false;
            _runCts?.Dispose();
            _runCts = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void OnStop()
    {
        _runCts?.Cancel();
        _runtime.Stop();
        StatusText = "已停止";
    }

    private void OnPause()
    {
        if (!IsPaused)
        {
            _runtime.Pause();
            IsPaused = true;
            StatusText = "已暂停";
        }
        else
        {
            _runtime.Resume();
            IsPaused = false;
            StatusText = "测试运行中...";
        }
    }

    // ─── 扫码枪集成 ──────────────────────────────────────────

    private SerialPort? _scannerPort;

    /// <summary>
    /// 启动串口扫码枪监听（默认波特率 9600，无校验）。
    /// 扫码枪发送数据后自动填入序列号并触发开始测试。
    /// </summary>
    public void StartScanner(string portName, bool autoStart = true)
    {
        try
        {
            _scannerPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
            {
                NewLine = "\r\n",
                ReadTimeout = -1 // 无限等待
            };
            _scannerPort.DataReceived += (_, _) =>
            {
                var data = _scannerPort.ReadLine().Trim();
                DispatcherHelper.Invoke(() =>
                {
                    SerialNumber = data;
                    if (autoStart && !IsRunning)
                        _ = OnStartAsync();
                });
            };
            _scannerPort.Open();
            StatusText = $"扫码枪已连接 ({portName})";
        }
        catch (Exception ex)
        {
            StatusText = $"扫码枪连接失败: {ex.Message}";
        }
    }

    public void StopScanner()
    {
        _scannerPort?.Close();
        _scannerPort?.Dispose();
        _scannerPort = null;
    }
}

/// <summary>
/// 测试结果列表项。
/// </summary>
public sealed class TestResultItem
{
    public string Time { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string TaskName { get; set; } = "";
    public string Result { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Detail { get; set; } = "";
}

/// <summary>
/// WPF 调度器辅助类（用于从非 UI 线程调度到 UI 线程）。
/// </summary>
internal static class DispatcherHelper
{
    internal static void Invoke(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher != null)
            System.Windows.Application.Current.Dispatcher.Invoke(action);
        else
            action();
    }
}
