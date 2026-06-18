using System.Windows.Input;

namespace Cc.IDE.Mvvm;

/// <summary>
/// A synchronous ICommand implementation that delegates to an Action.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;
    private EventHandler? _canExecuteChanged;

    /// <summary>
    /// 使用无参执行委托和可选的无参判断委托初始化命令。
    /// </summary>
    /// <param name="execute">要执行的操作。</param>
    /// <param name="canExecute">用于判断是否可以执行的可选函数。若为 <c>null</c>，则始终可执行。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    /// <summary>
    /// 使用带参执行委托和可选的带参判断委托初始化命令。
    /// </summary>
    /// <param name="execute">带有参数的要执行的操作。</param>
    /// <param name="canExecute">用于判断是否可以执行的可选谓词。若为 <c>null</c>，则始终可执行。</param>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
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
    /// 确定命令是否可以在其当前状态下执行。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递参数，则可以设置为 <c>null</c>。</param>
    /// <returns>如果可以执行此命令，则为 <c>true</c>；否则为 <c>false</c>。</returns>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <summary>
    /// 对当前命令目标执行命令。
    /// </summary>
    /// <param name="parameter">命令使用的数据。如果命令不需要传递参数，则可以设置为 <c>null</c>。</param>
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Manually triggers a re-evaluation of CanExecute.
    /// </summary>
    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}
