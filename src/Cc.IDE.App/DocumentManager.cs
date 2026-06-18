namespace Cc.IDE.App;

/// <summary>
/// 文档管理器实现。使用字典维护打开的文档集合，
/// 支持文档激活、关闭和保存操作。当前为 Phase 5 占位实现，
/// 后续将集成实际的文件读取/写入和编辑器控件解析逻辑。
/// </summary>
public sealed class DocumentManager : IDocumentManager
{
    private readonly Dictionary<string, IDocument> _documents = new();
    private IDocument? _activeDocument;

    /// <summary>
    /// 获取当前打开的文档数量。
    /// </summary>
    public int OpenDocumentCount => _documents.Count;

    /// <summary>
    /// 获取当前活动的文档。若没有打开的文档则为 <c>null</c>。
    /// </summary>
    public IDocument? ActiveDocument => _activeDocument;

    /// <summary>
    /// 获取所有已打开文档的只读列表。按最近激活顺序排列。
    /// </summary>
    public IReadOnlyList<IDocument> OpenDocuments => _documents.Values.ToList();

    /// <summary>
    /// 当活动文档发生更改时触发。新的活动文档作为参数传递。
    /// </summary>
    public event Action<IDocument?>? ActiveDocumentChanged;

    /// <summary>
    /// 打开指定路径的文档并激活其标签页。
    /// 若文档已打开则直接激活现有标签页。
    /// </summary>
    /// <param name="filePath">文件绝对路径。</param>
    /// <returns>一个任务，其结果为已打开的文档实例。</returns>
    /// <exception cref="NotImplementedException">Phase 5 占位，尚未实现。</exception>
    public Task<IDocument> OpenDocumentAsync(string filePath)
    {
        // Phase 5 placeholder: 根据文件扩展名创建对应的编辑器文档
        throw new NotImplementedException();
    }

    /// <summary>
    /// 关闭指定文档。从内部字典中移除并触发活动文档变更事件。
    /// </summary>
    /// <param name="document">要关闭的文档。</param>
    /// <returns>一个任务，其结果为 <c>true</c> 表示文档已成功关闭。</returns>
    public Task<bool> CloseDocumentAsync(IDocument document)
    {
        if (_documents.Remove(document.Id))
        {
            if (_activeDocument == document)
            {
                _activeDocument = _documents.Values.LastOrDefault();
                ActiveDocumentChanged?.Invoke(_activeDocument);
            }
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <summary>
    /// 关闭所有已打开的文档。清空内部字典并触发活动文档为空的事件。
    /// </summary>
    /// <returns>表示异步关闭全部操作的任务。</returns>
    public Task CloseAllDocumentsAsync()
    {
        _documents.Clear();
        _activeDocument = null;
        ActiveDocumentChanged?.Invoke(null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存指定文档到其文件路径。保存成功后清除脏标记。
    /// </summary>
    /// <param name="document">要保存的文档。</param>
    /// <returns>表示异步保存操作的任务。</returns>
    public Task SaveDocumentAsync(IDocument document)
    {
        // Phase 5 placeholder: 实际的文件写入逻辑
        document.MarkClean();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存所有已打开且有未保存更改的文档。
    /// </summary>
    /// <returns>表示异步全部保存操作的任务。</returns>
    public Task SaveAllDocumentsAsync()
    {
        // Phase 5 placeholder: 遍历所有脏文档并保存
        return Task.CompletedTask;
    }
}
