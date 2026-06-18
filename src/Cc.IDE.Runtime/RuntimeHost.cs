using Cc.IDE.ProjectSystem.Models;
using Cc.IDE.DriverSdk;

namespace Cc.IDE.Runtime;

/// <summary>
/// 运行时主机 — 任务执行的主要入口点。
/// 管理运行时状态机，协调 TaskRunner 执行测试任务。
/// </summary>
public sealed class RuntimeHost : IRuntimeHost
{
    private readonly IIOExecutionService _ioService;
    private readonly IInstrumentManager? _instrumentManager;
    private TaskRunner? _currentRunner;
    private CancellationTokenSource? _currentCts;
    private readonly object _lock = new();

    /// <summary>
    /// 初始化运行时主机的新实例。
    /// </summary>
    /// <param name="ioService">用于执行真实 PLC/CAN I/O 操作的 IO 执行服务。</param>
    /// <param name="instrumentManager">可选的仪器管理器，支持真实仪器驱动调用。</param>
    public RuntimeHost(IIOExecutionService ioService, IInstrumentManager? instrumentManager = null)
    {
        _ioService = ioService ?? throw new ArgumentNullException(nameof(ioService));
        _instrumentManager = instrumentManager;
    }

    /// <summary>获取当前运行时状态。</summary>
    public RuntimeState State
    {
        get
        {
            lock (_lock)
                return _currentRunner?.State ?? RuntimeState.Idle;
        }
    }

    /// <summary>
    /// 运行指定的任务定义。创建运行时上下文和 TaskRunner 实例，
    /// 按流程图遍历执行所有节点，收集步骤结果和输出。
    /// </summary>
    /// <param name="task">要运行的任务定义。</param>
    /// <param name="options">运行时运行选项，包含超时、调试等配置。</param>
    /// <param name="ct">取消令牌，用于取消运行操作。</param>
    /// <returns>任务的测试结果。</returns>
    public async Task<TestResult> RunAsync(TaskDefinition task, RuntimeRunOptions options, CancellationToken ct)
    {
        var result = new TestResult
        {
            TaskId = task.Id,
            TaskName = task.Name,
            Status = "NotRun",
            StartTime = DateTime.UtcNow
        };

        var context = new RuntimeContext();

        // 将输入参数写入上下文
        foreach (var input in task.InputArguments)
        {
            context.Inputs[input.Name] = input.DefaultValue;
        }

        // 将任务变量写入上下文
        foreach (var variable in task.Variables)
        {
            context.Variables[variable.Name] = variable.DefaultValue;
        }

        CancellationTokenSource? timeoutCts = null;

        try
        {
            if (options.GlobalTimeoutMs > 0)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(options.GlobalTimeoutMs);
            }

            var effectiveCt = timeoutCts?.Token ?? ct;

            // 创建调试控制器（仅在启用调试时）
            DebugController? debug = null;
            if (options.EnableDebug)
            {
                debug = new DebugController();
                if (options.BreakpointNodeIds != null)
                {
                    foreach (var id in options.BreakpointNodeIds)
                        debug.AddBreakpoint(id);
                }
            }

            lock (_lock)
            {
                _currentRunner = new TaskRunner(context, options, _ioService,
                    _instrumentManager, taskLoader: null, debug: debug);
                _currentCts = timeoutCts ?? new CancellationTokenSource();
            }

            await _currentRunner.ExecuteAsync(task, options, effectiveCt);

            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
            result.StepResults = context.StepResults;
            result.Outputs = context.Outputs;

            if (effectiveCt.IsCancellationRequested)
            {
                result.Status = "Aborted";
                result.FailureReason = "任务被取消或超时。";
            }
            else if (context.StopRequested)
            {
                result.Status = "Passed";
            }
            else
            {
                result.Status = "Passed";
            }
        }
        catch (OperationCanceledException)
        {
            result.Status = "Aborted";
            result.FailureReason = "任务执行被取消。";
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            result.Status = "Error";
            result.FailureReason = ex.Message;
            result.EndTime = DateTime.UtcNow;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;
        }
        finally
        {
            lock (_lock)
            {
                _currentRunner = null;
                timeoutCts?.Dispose();
                _currentCts?.Dispose();
                _currentCts = null;
            }
        }

        return result;
    }

    /// <summary>
    /// 暂停当前正在运行的任务。
    /// </summary>
    public void Pause()
    {
        lock (_lock)
        {
            _currentRunner?.Pause();
        }
    }

    /// <summary>
    /// 恢复已暂停的任务。
    /// </summary>
    public void Resume()
    {
        lock (_lock)
        {
            _currentRunner?.Resume();
        }
    }

    /// <inheritdoc/>
    public void Step()
    {
        lock (_lock)
        {
            _currentRunner?.Step();
        }
    }

    /// <summary>
    /// 停止当前正在运行的任务。
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            _currentRunner?.Stop();
            _currentCts?.Cancel();
        }
    }
}
