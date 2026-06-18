using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Cc.IDE.Mvvm;

/// <summary>
/// 导航服务接口。提供 ViewModel 优先的导航能力，
/// 管理导航历史栈，支持前进/后退。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 当前显示的 ViewModel。
    /// </summary>
    IViewModel? CurrentViewModel { get; }

    /// <summary>
    /// 是否可以向后导航。
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 是否可以向前导航。
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// 导航到指定的 ViewModel。
    /// </summary>
    /// <typeparam name="TViewModel">目标 ViewModel 类型。</typeparam>
    /// <param name="parameter">可选的导航参数。</param>
    /// <returns>表示异步导航操作的任务。</returns>
    Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : IViewModel;

    /// <summary>
    /// 向后导航到历史栈中的上一页。
    /// </summary>
    /// <returns>表示异步导航操作的任务。</returns>
    Task GoBackAsync();

    /// <summary>
    /// 向前导航到历史栈中的下一页。
    /// </summary>
    /// <returns>表示异步导航操作的任务。</returns>
    Task GoForwardAsync();

    /// <summary>
    /// 关闭当前 ViewModel 并导航到指定 ViewModel（不保留历史）。
    /// </summary>
    /// <typeparam name="TViewModel">目标 ViewModel 类型。</typeparam>
    /// <param name="parameter">可选的导航参数。</param>
    /// <returns>表示异步导航操作的任务。</returns>
    Task ReplaceAsync<TViewModel>(object? parameter = null) where TViewModel : IViewModel;

    /// <summary>
    /// 当导航成功完成时触发的事件。
    /// 参数为导航到的 ViewModel。
    /// </summary>
    event Action<IViewModel>? Navigated;
}

/// <summary>
/// 导航服务实现。维护一个简单的后进先出导航历史栈。
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly Stack<IViewModel> _backStack = new();
    private readonly Stack<IViewModel> _forwardStack = new();
    private readonly IServiceProvider _serviceProvider;
    private IViewModel? _current;

    /// <summary>
    /// 使用 DI 容器创建导航服务。
    /// ViewModel 类型通过 <see cref="IServiceProvider"/> 解析。
    /// </summary>
    /// <param name="serviceProvider">用于解析 ViewModel 实例的 DI 容器。</param>
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取当前显示的 ViewModel。
    /// </summary>
    public IViewModel? CurrentViewModel => _current;

    /// <summary>
    /// 获取是否可以向后导航。
    /// </summary>
    public bool CanGoBack => _backStack.Count > 0;

    /// <summary>
    /// 获取是否可以向前导航。
    /// </summary>
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <summary>
    /// 当导航成功完成时触发的事件。
    /// 参数为导航到的 ViewModel。
    /// </summary>
    public event Action<IViewModel>? Navigated;

    /// <summary>
    /// 导航到指定的 ViewModel 类型。
    /// 当前 ViewModel 被停用并压入后退栈，前进栈被清空。
    /// </summary>
    /// <typeparam name="TViewModel">目标 ViewModel 类型。</typeparam>
    /// <param name="parameter">可选的导航参数。</param>
    /// <returns>表示异步导航操作的任务。</returns>
    public async Task NavigateToAsync<TViewModel>(object? parameter = null) where TViewModel : IViewModel
    {
        if (_current != null)
        {
            await _current.DeactivateAsync();
            _backStack.Push(_current);
            _forwardStack.Clear();
        }

        var viewModel = (TViewModel)_serviceProvider.GetRequiredService(typeof(TViewModel));
        await viewModel.InitializeAsync();
        await viewModel.ActivateAsync();

        _current = viewModel;
        Navigated?.Invoke(_current);
    }

    /// <summary>
    /// 向后导航到历史栈中的上一页。
    /// 当前 ViewModel 被停用并压入前进栈。
    /// </summary>
    /// <returns>表示异步导航操作的任务。</returns>
    public async Task GoBackAsync()
    {
        if (!CanGoBack) return;

        if (_current != null)
        {
            await _current.DeactivateAsync();
            _forwardStack.Push(_current);
        }

        _current = _backStack.Pop();
        await _current.ActivateAsync();
        Navigated?.Invoke(_current);
    }

    /// <summary>
    /// 向前导航到历史栈中的下一页。
    /// 当前 ViewModel 被停用并压入后退栈。
    /// </summary>
    /// <returns>表示异步导航操作的任务。</returns>
    public async Task GoForwardAsync()
    {
        if (!CanGoForward) return;

        if (_current != null)
        {
            await _current.DeactivateAsync();
            _backStack.Push(_current);
        }

        _current = _forwardStack.Pop();
        await _current.ActivateAsync();
        Navigated?.Invoke(_current);
    }

    /// <summary>
    /// 关闭当前 ViewModel 并导航到指定 ViewModel，清除所有导航历史。
    /// </summary>
    /// <typeparam name="TViewModel">目标 ViewModel 类型。</typeparam>
    /// <param name="parameter">可选的导航参数。</param>
    /// <returns>表示异步导航操作的任务。</returns>
    public async Task ReplaceAsync<TViewModel>(object? parameter = null) where TViewModel : IViewModel
    {
        if (_current != null)
        {
            await _current.DeactivateAsync();
            _current.Cleanup();
        }

        _backStack.Clear();
        _forwardStack.Clear();

        var viewModel = (TViewModel)_serviceProvider.GetRequiredService(typeof(TViewModel));
        await viewModel.InitializeAsync();
        await viewModel.ActivateAsync();

        _current = viewModel;
        Navigated?.Invoke(_current);
    }
}
