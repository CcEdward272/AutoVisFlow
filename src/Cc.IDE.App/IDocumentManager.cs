namespace Cc.IDE.App;

/// <summary>
/// 文档管理器接口。管理 IDE 中打开的文档（TabControl 标签页），
/// 跟踪脏状态，提供保存/全部保存/关闭功能。
/// 通过 <see cref="ActiveDocumentChanged"/> 事件通知 UI 层活动文档的切换。
/// </summary>
public interface IDocumentManager
{
    /// <summary>
    /// 获取当前打开的文档数量。
    /// </summary>
    int OpenDocumentCount { get; }

    /// <summary>
    /// 获取当前活动的文档（具有键盘焦点的标签页）。
    /// 若没有打开的文档则为 <c>null</c>。
    /// </summary>
    IDocument? ActiveDocument { get; }

    /// <summary>
    /// 获取所有已打开文档的只读列表。按最近激活顺序排列。
    /// </summary>
    IReadOnlyList<IDocument> OpenDocuments { get; }

    /// <summary>
    /// 打开指定路径的文档并激活其标签页。
    /// 若文档已在 <see cref="OpenDocuments"/> 中则直接激活。
    /// </summary>
    /// <param name="filePath">文件绝对路径。</param>
    /// <returns>一个任务，其结果为已打开的 <see cref="IDocument"/> 实例。</returns>
    Task<IDocument> OpenDocumentAsync(string filePath);

    /// <summary>
    /// 关闭指定文档。若有未保存更改则通过对话框提示用户保存。
    /// </summary>
    /// <param name="document">要关闭的文档。</param>
    /// <returns>一个任务，其结果为文档是否成功关闭。</returns>
    Task<bool> CloseDocumentAsync(IDocument document);

    /// <summary>
    /// 关闭所有已打开的文档。每个脏文档均会提示保存。
    /// </summary>
    /// <returns>表示异步关闭全部操作的任务。</returns>
    Task CloseAllDocumentsAsync();

    /// <summary>
    /// 保存指定文档到其 <see cref="IDocument.FilePath"/>。
    /// 保存成功后调用 <see cref="IDocument.MarkClean"/> 清除脏标记。
    /// </summary>
    /// <param name="document">要保存的文档。</param>
    /// <returns>表示异步保存操作的任务。</returns>
    Task SaveDocumentAsync(IDocument document);

    /// <summary>
    /// 保存所有已打开且有未保存更改的文档。
    /// </summary>
    /// <returns>表示异步全部保存操作的任务。</returns>
    Task SaveAllDocumentsAsync();

    /// <summary>
    /// 当活动文档发生更改时触发。参数为新的活动文档；
    /// 当所有文档关闭时为 <c>null</c>。
    /// </summary>
    event Action<IDocument?>? ActiveDocumentChanged;
}

/// <summary>
/// 文档抽象。每个在 IDE 中打开的编辑器文件实现此接口。
/// 封装文件的路径、标题、类型、脏状态和关联的视图内容。
/// </summary>
public interface IDocument
{
    /// <summary>
    /// 获取文档的唯一标识符（GUID）。在整个文档管理生命周期中保持不变。
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 获取文件在磁盘上的绝对路径。
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// 获取文档在标签页标题中显示的标题。通常为不带扩展名的文件名。
    /// 当 <see cref="IsDirty"/> 为 <c>true</c> 时 UI 应在标题后附加 "*"。
    /// </summary>
    string Title { get; }

    /// <summary>
    /// 获取文档类型标识符。可选值："Task"、"Instrument"、"IOMap" 等。
    /// 用于确定应激活哪个编辑器控件来显示 <see cref="Content"/>。
    /// </summary>
    string DocumentType { get; }

    /// <summary>
    /// 获取文档是否有未保存的更改。
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// 获取关联的视图内容对象。通常为编辑器 ViewModel 或编辑器 UserControl 实例。
    /// 用于 IDE 文档区域的 ContentControl 绑定。
    /// </summary>
    object? Content { get; }

    /// <summary>
    /// 标记文档为脏状态（有未保存更改）。
    /// 应同时触发 <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> 事件。
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// 标记文档为已保存状态（无未保存更改）。
    /// 应同时触发 <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> 事件。
    /// </summary>
    void MarkClean();
}
