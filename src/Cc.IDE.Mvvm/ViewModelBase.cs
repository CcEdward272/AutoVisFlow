using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cc.IDE.Mvvm;

/// <summary>
/// ViewModel 基类，继承自 <see cref="ObservableObject"/>，提供视图模型通用的
/// 生命周期管理（初始化、激活、停用、清理）和错误处理支持。
/// </summary>
public abstract class ViewModelBase : ObservableObject, IViewModel
{
    private bool _isInitialized;
    private bool _isActive;
    private bool _isBusy;
    private string? _lastError;

    /// <summary>
    /// 获取是否已完成初始化。
    /// </summary>
    public bool IsInitialized
    {
        get => _isInitialized;
        private set => SetProperty(ref _isInitialized, value);
    }

    /// <summary>
    /// 获取当前 ViewModel 是否处于激活状态（可见且可交互）。
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// 获取或设置 ViewModel 是否正在执行异步操作。
    /// 绑定到 UI 可显示忙碌指示器。
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// 获取最近一次操作的错误信息；为 <c>null</c> 表示无错误。
    /// </summary>
    public string? LastError
    {
        get => _lastError;
        protected set => SetProperty(ref _lastError, value);
    }

    /// <summary>
    /// 获取此 ViewModel 的唯一显示标题。
    /// 子类可重写以提供具体标题。
    /// </summary>
    public virtual string DisplayTitle => GetType().Name.Replace("ViewModel", "");

    /// <summary>
    /// 初始化 ViewModel。通常在首次显示前调用一次。
    /// </summary>
    /// <returns>表示异步初始化操作的任务。</returns>
    public virtual async Task InitializeAsync()
    {
        IsInitialized = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 当 ViewModel 被激活（获得焦点或变为可见）时调用。
    /// </summary>
    /// <returns>表示异步激活操作的任务。</returns>
    public virtual async Task ActivateAsync()
    {
        IsActive = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 当 ViewModel 被停用（失去焦点或隐藏）时调用。
    /// </summary>
    /// <returns>表示异步停用操作的任务。</returns>
    public virtual async Task DeactivateAsync()
    {
        IsActive = false;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 清理 ViewModel 资源。在 ViewModel 不再需要时调用。
    /// </summary>
    public virtual void Cleanup()
    {
        IsActive = false;
        IsInitialized = false;
    }

    /// <summary>
    /// 设置错误信息。传入 <c>null</c> 可清除错误。
    /// </summary>
    /// <param name="error">错误消息；为 <c>null</c> 则清除。</param>
    protected void SetError(string? error) => LastError = error;

    /// <summary>
    /// 以忙碌状态执行异步操作，自动管理 <see cref="IsBusy"/> 和异常捕获。
    /// </summary>
    /// <param name="operation">要执行的异步委托。</param>
    /// <param name="errorMessage">失败时的默认错误消息。</param>
    protected async Task ExecuteBusyAsync(Func<CancellationToken, Task> operation, string errorMessage = "操作失败。")
    {
        if (IsBusy) return;

        IsBusy = true;
        SetError(null);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await operation(cts.Token);
        }
        catch (OperationCanceledException)
        {
            SetError("操作已取消。");
        }
        catch (Exception ex)
        {
            SetError($"{errorMessage}：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// 视图模型生命周期接口。
/// </summary>
public interface IViewModel
{
    /// <summary>
    /// 获取唯一显示标题。
    /// </summary>
    string DisplayTitle { get; }

    /// <summary>
    /// 获取是否已完成初始化。
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 获取是否处于激活状态。
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// 获取是否忙碌。
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// 获取最近错误。
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// 初始化 ViewModel。
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 激活 ViewModel。
    /// </summary>
    Task ActivateAsync();

    /// <summary>
    /// 停用 ViewModel。
    /// </summary>
    Task DeactivateAsync();

    /// <summary>
    /// 清理 ViewModel 资源。
    /// </summary>
    void Cleanup();
}
