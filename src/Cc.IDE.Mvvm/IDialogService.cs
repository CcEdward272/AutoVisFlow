using System.Threading.Tasks;

namespace Cc.IDE.Mvvm;

/// <summary>
/// 对话框服务接口。提供模态/非模态对话框的抽象，
/// 支持消息框、文件选择器和自定义内容对话框。
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 显示信息消息框。
    /// </summary>
    /// <param name="message">消息文本。</param>
    /// <param name="title">标题（可选）。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ShowInfoAsync(string message, string? title = null);

    /// <summary>
    /// 显示警告消息框。
    /// </summary>
    /// <param name="message">警告消息。</param>
    /// <param name="title">标题（可选）。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ShowWarningAsync(string message, string? title = null);

    /// <summary>
    /// 显示错误消息框。
    /// </summary>
    /// <param name="message">错误消息。</param>
    /// <param name="title">标题（可选）。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task ShowErrorAsync(string message, string? title = null);

    /// <summary>
    /// 显示确认对话框，返回用户的选择。
    /// </summary>
    /// <param name="message">确认消息。</param>
    /// <param name="title">标题（可选）。</param>
    /// <returns>用户选择确认时返回 <c>true</c>。</returns>
    Task<bool> ConfirmAsync(string message, string? title = null);

    /// <summary>
    /// 打开文件选择对话框。
    /// </summary>
    /// <param name="filter">文件过滤器（如 "JSON Files|*.json|All Files|*.*"）。</param>
    /// <param name="title">对话框标题（可选）。</param>
    /// <returns>选择的文件路径；取消时返回 <c>null</c>。</returns>
    Task<string?> OpenFileAsync(string filter, string? title = null);

    /// <summary>
    /// 打开保存文件对话框。
    /// </summary>
    /// <param name="filter">文件过滤器。</param>
    /// <param name="title">对话框标题（可选）。</param>
    /// <param name="defaultFileName">默认文件名（可选）。</param>
    /// <returns>保存路径；取消时返回 <c>null</c>。</returns>
    Task<string?> SaveFileAsync(string filter, string? title = null, string? defaultFileName = null);

    /// <summary>
    /// 打开文件夹选择对话框。
    /// </summary>
    /// <param name="title">对话框标题（可选）。</param>
    /// <returns>选择的文件夹路径；取消时返回 <c>null</c>。</returns>
    Task<string?> OpenFolderAsync(string? title = null);

    /// <summary>
    /// 显示自定义内容的模态对话框。
    /// </summary>
    /// <typeparam name="TViewModel">对话框内容的 ViewModel 类型。</typeparam>
    /// <param name="viewModel">对话框 ViewModel 实例。</param>
    /// <returns>对话框关闭时返回 <c>true</c> 表示用户确认。</returns>
    Task<bool> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : IViewModel;
}

/// <summary>
/// 对话框服务的基础实现（Phase 5 占位）。
/// 显示的消息通过 <see cref="IEventAggregator"/> 发布，供 UI 层订阅显示。
/// 后续将实现实际的 WPF 对话框窗口。
/// </summary>
/// <remarks>
/// 在 WPF 桌面应用中运行时，UI 层应注入实现了实际 MessageBox 和文件对话框的版本。
/// </remarks>
public sealed class DialogService : IDialogService
{
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    /// 使用事件聚合器创建对话框服务。
    /// </summary>
    /// <param name="eventAggregator">用于发布对话框事件的事件聚合器。</param>
    public DialogService(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc/>
    public Task ShowInfoAsync(string message, string? title = null)
    {
        _eventAggregator.Publish(new DialogRequestedEvent("Info", message, title));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ShowWarningAsync(string message, string? title = null)
    {
        _eventAggregator.Publish(new DialogRequestedEvent("Warning", message, title));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ShowErrorAsync(string message, string? title = null)
    {
        _eventAggregator.Publish(new DialogRequestedEvent("Error", message, title));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ConfirmAsync(string message, string? title = null)
    {
        _eventAggregator.Publish(new ConfirmDialogRequestedEvent(message, title));
        // Phase 5 placeholder: always return true
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<string?> OpenFileAsync(string filter, string? title = null)
    {
        _eventAggregator.Publish(new FileDialogRequestedEvent("Open", filter, title));
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<string?> SaveFileAsync(string filter, string? title = null, string? defaultFileName = null)
    {
        _eventAggregator.Publish(new FileDialogRequestedEvent("Save", filter, title, defaultFileName));
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<string?> OpenFolderAsync(string? title = null)
    {
        _eventAggregator.Publish(new FileDialogRequestedEvent("Folder", null, title));
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : IViewModel
    {
        _eventAggregator.Publish(new CustomDialogRequestedEvent(viewModel.DisplayTitle));
        return Task.FromResult(true);
    }
}

// ── Dialog Events ─────────────────────────────────────────────────

/// <summary>
/// 通用对话框请求事件。
/// 当需要显示信息、警告或错误对话框时发布此事件。
/// </summary>
public sealed class DialogRequestedEvent
{
    /// <summary>
    /// 获取对话框类型（Info、Warning、Error）。
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// 获取消息文本。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 获取对话框标题。
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// 初始化 <see cref="DialogRequestedEvent"/> 的新实例。
    /// </summary>
    /// <param name="type">对话框类型。</param>
    /// <param name="message">消息文本。</param>
    /// <param name="title">对话框标题。</param>
    public DialogRequestedEvent(string type, string message, string? title)
    { Type = type; Message = message; Title = title; }
}

/// <summary>
/// 确认对话框请求事件。
/// 当需要显示确认/取消对话框时发布此事件。
/// </summary>
public sealed class ConfirmDialogRequestedEvent
{
    /// <summary>
    /// 获取确认消息。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 获取对话框标题。
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// 初始化 <see cref="ConfirmDialogRequestedEvent"/> 的新实例。
    /// </summary>
    /// <param name="message">确认消息。</param>
    /// <param name="title">对话框标题。</param>
    public ConfirmDialogRequestedEvent(string message, string? title)
    { Message = message; Title = title; }
}

/// <summary>
/// 文件对话框请求事件。
/// 当需要显示打开/保存文件或选择文件夹对话框时发布此事件。
/// </summary>
public sealed class FileDialogRequestedEvent
{
    /// <summary>
    /// 获取对话框模式（Open、Save、Folder）。
    /// </summary>
    public string Mode { get; }

    /// <summary>
    /// 获取文件过滤器。
    /// </summary>
    public string? Filter { get; }

    /// <summary>
    /// 获取对话框标题。
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// 获取默认文件名。
    /// </summary>
    public string? DefaultFileName { get; }

    /// <summary>
    /// 初始化 <see cref="FileDialogRequestedEvent"/> 的新实例。
    /// </summary>
    /// <param name="mode">对话框模式。</param>
    /// <param name="filter">文件过滤器。</param>
    /// <param name="title">对话框标题。</param>
    /// <param name="defaultFileName">默认文件名。</param>
    public FileDialogRequestedEvent(string mode, string? filter, string? title, string? defaultFileName = null)
    { Mode = mode; Filter = filter; Title = title; DefaultFileName = defaultFileName; }
}

/// <summary>
/// 自定义内容对话框请求事件。
/// 当需要显示包含自定义 ViewModel 内容的模态对话框时发布此事件。
/// </summary>
public sealed class CustomDialogRequestedEvent
{
    /// <summary>
    /// 获取对话框标题。
    /// </summary>
    public string DialogTitle { get; }

    /// <summary>
    /// 初始化 <see cref="CustomDialogRequestedEvent"/> 的新实例。
    /// </summary>
    /// <param name="dialogTitle">对话框标题。</param>
    public CustomDialogRequestedEvent(string dialogTitle)
    { DialogTitle = dialogTitle; }
}
