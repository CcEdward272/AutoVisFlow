using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

#pragma warning disable CS0067 // Event is used via explicit add/remove

namespace Cc.IDE.Mvvm;

/// <summary>
/// An async ICommand implementation that supports cancellation,
/// running-state tracking, and exception handling.
/// </summary>
public sealed class AsyncRelayCommand : ICommand, INotifyPropertyChanged
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private EventHandler? _canExecuteChanged;

    /// <summary>
    /// 使用支持 CancellationToken 的异步执行委托初始化命令。
    /// </summary>
    /// <param name="execute">接收 CancellationToken 的异步操作。</param>
    /// <param name="canExecute">用于判断是否可以执行的可选函数。若为 <c>null</c>，则始终可执行。</param>
    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 使用无 CancellationToken 的异步执行委托初始化命令。
    /// </summary>
    /// <param name="execute">异步操作。</param>
    /// <param name="canExecute">用于判断是否可以执行的可选函数。若为 <c>null</c>，则始终可执行。</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute)
    {
    }

    /// <summary>
    /// Whether the command is currently executing.
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            OnPropertyChanged();
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 当 CanExecute 的状态发生更改时发生。
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    /// <summary>
    /// 当属性值发生更改时发生。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 确定命令是否可以在其当前状态下执行。
    /// </summary>
    /// <param name="parameter">命令使用的数据。该参数在此异步命令中通常不使用，但保留以符合 ICommand 接口的约定。</param>
    /// <returns>如果可以执行此命令，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    public bool CanExecute(object? parameter) => !IsRunning && (_canExecute?.Invoke() ?? true);

    /// <summary>
    /// 对当前命令目标执行命令。
    /// </summary>
    /// <param name="parameter">命令使用的数据。该参数在此异步命令中通常不使用，但保留以符合 ICommand 接口的约定。</param>
    public async void Execute(object? parameter)
    {
        if (IsRunning) return;

        IsRunning = true;
        _cts = new CancellationTokenSource();

        try
        {
            await _execute(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled — silently ignore.
        }
        catch (Exception ex)
        {
            OnExecutionFailed(ex);
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            IsRunning = false;
        }
    }

    /// <summary>
    /// Manual raise of CanExecuteChanged.
    /// </summary>
    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Requests cancellation of the currently running operation.
    /// </summary>
    public void Cancel()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// Raised when execution throws an unhandled exception.
    /// Subscribers can display error UI, log, etc.
    /// </summary>
    public event EventHandler<Exception>? ExecutionFailed;

    private void OnExecutionFailed(Exception ex)
    {
        ExecutionFailed?.Invoke(this, ex);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
