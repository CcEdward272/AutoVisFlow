using System.Collections.ObjectModel;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.IOMappingEditor;

/// <summary>
/// IO 映射列表 ViewModel。
/// 管理项目中所有 IO 映射的列表，支持选择、添加和删除操作。
/// </summary>
public class IOMappingListViewModel : ViewModelBase
{
    private IOMappingDefinition? _selectedMapping;

    /// <summary>项目中所有 IO 映射的可观察集合。</summary>
    public ObservableCollection<IOMappingDefinition> Mappings { get; } = new();

    /// <summary>当前选中的 IO 映射。</summary>
    public IOMappingDefinition? SelectedMapping
    {
        get => _selectedMapping;
        set
        {
            if (SetProperty(ref _selectedMapping, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    /// <summary>获取是否有映射被选中。</summary>
    public bool HasSelection => SelectedMapping != null;
}
