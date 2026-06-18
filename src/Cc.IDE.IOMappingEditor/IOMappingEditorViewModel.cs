using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.IOMappingEditor;

/// <summary>
/// IO 映射编辑器的主 ViewModel。
/// 管理一个 <see cref="IOMappingDefinition"/> 的完整编辑体验，
/// 包括 IO 分组、IO 点、连接配置的 CRUD 操作以及选择管理。
/// </summary>
public class IOMappingEditorViewModel : ViewModelBase
{
    #region 字段

    private string _mappingId = Guid.NewGuid().ToString("N");
    private string _mappingName = "New I/O Map";
    private string _ioType = "Mixed";
    private string _description = string.Empty;
    private IOGroupViewModel? _selectedGroup;
    private IOPointViewModel? _selectedPoint;
    private IOConnectionConfigViewModel _connectionConfig = new();
    private bool _isDirty;

    #endregion

    #region 属性

    /// <summary>IO 映射的唯一标识符。</summary>
    public string MappingId
    {
        get => _mappingId;
        set => SetProperty(ref _mappingId, value);
    }

    /// <summary>IO 映射的名称（例如 "Main PLC"、"Remote IO Station 3"）。</summary>
    public string MappingName
    {
        get => _mappingName;
        set => SetProperty(ref _mappingName, value);
    }

    /// <summary>
    /// IO 类型：DI（数字输入）、DO（数字输出）、AI（模拟输入）、
    /// AO（模拟输出）或 Mixed（混合类型）。
    /// </summary>
    public string IOType
    {
        get => _ioType;
        set => SetProperty(ref _ioType, value);
    }

    /// <summary>IO 映射的描述文本。</summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>IO 分组列表。每个分组包含一组相关的 IO 点。</summary>
    public ObservableCollection<IOGroupViewModel> Groups { get; } = new();

    /// <summary>当前选中的 IO 分组。</summary>
    public IOGroupViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                SelectedPoint = null;
                OnPropertyChanged(nameof(HasGroupSelection));
            }
        }
    }

    /// <summary>当前选中的 IO 点。</summary>
    public IOPointViewModel? SelectedPoint
    {
        get => _selectedPoint;
        set
        {
            if (SetProperty(ref _selectedPoint, value))
                OnPropertyChanged(nameof(HasPointSelection));
        }
    }

    /// <summary>连接配置的 ViewModel。</summary>
    public IOConnectionConfigViewModel ConnectionConfig
    {
        get => _connectionConfig;
        set => SetProperty(ref _connectionConfig, value);
    }

    /// <summary>获取是否有分组被选中。</summary>
    public bool HasGroupSelection => SelectedGroup != null;

    /// <summary>获取是否有 IO 点被选中。</summary>
    public bool HasPointSelection => SelectedPoint != null;

    /// <summary>获取或设置映射是否已被修改但未保存。</summary>
    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    #endregion

    #region 命令

    /// <summary>添加新的 IO 分组。</summary>
    public ICommand AddGroupCommand { get; }

    /// <summary>删除当前选中的 IO 分组。</summary>
    public ICommand RemoveGroupCommand { get; }

    /// <summary>向当前选中的分组添加新的 IO 点。</summary>
    public ICommand AddPointCommand { get; }

    /// <summary>删除当前选中的 IO 点。</summary>
    public ICommand RemovePointCommand { get; }

    /// <summary>从设备导入 IO 点配置。</summary>
    public ICommand ImportFromDeviceCommand { get; }

    /// <summary>验证当前 IO 映射的完整性。</summary>
    public ICommand ValidateMappingCommand { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 <see cref="IOMappingEditorViewModel"/> 的新实例。
    /// 创建所有命令并设置默认的空白映射。
    /// </summary>
    public IOMappingEditorViewModel()
    {
        AddGroupCommand = new RelayCommand(OnAddGroup);
        RemoveGroupCommand = new RelayCommand(OnRemoveGroup, () => HasGroupSelection);
        AddPointCommand = new RelayCommand(OnAddPoint, () => HasGroupSelection);
        RemovePointCommand = new RelayCommand(OnRemovePoint, () => HasPointSelection);
        ImportFromDeviceCommand = new AsyncRelayCommand(OnImportFromDeviceAsync);
        ValidateMappingCommand = new RelayCommand(OnValidateMapping);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 从 <see cref="IOMappingDefinition"/> 加载 IO 映射数据。
    /// 清除当前状态并根据定义创建所有分组、点和连接配置。
    /// </summary>
    /// <param name="mapping">要加载的 IO 映射定义。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="mapping"/> 为 <c>null</c> 时抛出。</exception>
    public void LoadFromMapping(IOMappingDefinition mapping)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));

        Groups.Clear();
        SelectedGroup = null;
        SelectedPoint = null;

        MappingId = mapping.Id;
        MappingName = mapping.Name;
        IOType = mapping.IOType;
        Description = mapping.Description;

        ConnectionConfig.LoadFromConfig(mapping.Connection);

        foreach (var group in mapping.Groups)
        {
            var groupVm = new IOGroupViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Enabled = group.Enabled,
                RegisterKind = group.RegisterKind,
                Offset = group.Offset,
            };

            foreach (var point in group.Points)
            {
                groupVm.Points.Add(new IOPointViewModel
                {
                    Enabled = point.Enabled,
                    Code = point.Code,
                    Alias = point.Alias,
                    Type = point.Type,
                    Access = point.Access,
                    DataType = point.DataType,
                    Polarity = point.Polarity,
                    RegisterOffset = point.RegisterOffset,
                    BitIndex = point.BitIndex,
                    Scale = point.Scale,
                    SafeValue = point.SafeValue,
                    Unit = point.Unit,
                    Description = point.Description,
                });
            }

            Groups.Add(groupVm);
        }

        IsDirty = false;
    }

    /// <summary>
    /// 将当前编辑状态保存为 <see cref="IOMappingDefinition"/>。
    /// </summary>
    /// <returns>包含所有分组、点和连接配置的新建 IO 映射定义。</returns>
    public IOMappingDefinition SaveToMapping()
    {
        var mapping = new IOMappingDefinition
        {
            Id = MappingId,
            Name = MappingName,
            IOType = IOType,
            Description = Description,
            DeviceId = ConnectionConfig.DeviceId ?? string.Empty,
            Connection = ConnectionConfig.SaveToConfig(),
        };

        foreach (var groupVm in Groups)
        {
            var group = new IOGroupDefinition
            {
                Id = groupVm.Id,
                Name = groupVm.Name,
                Enabled = groupVm.Enabled,
                RegisterKind = groupVm.RegisterKind,
                Offset = groupVm.Offset,
            };

            foreach (var pointVm in groupVm.Points)
            {
                group.Points.Add(new IOPointDefinition
                {
                    Enabled = pointVm.Enabled,
                    Code = pointVm.Code,
                    Alias = pointVm.Alias,
                    Type = pointVm.Type,
                    Access = pointVm.Access,
                    DataType = pointVm.DataType,
                    Polarity = pointVm.Polarity,
                    RegisterOffset = pointVm.RegisterOffset,
                    BitIndex = pointVm.BitIndex,
                    Scale = pointVm.Scale,
                    SafeValue = pointVm.SafeValue,
                    Unit = pointVm.Unit,
                    Description = pointVm.Description,
                });
            }

            mapping.Groups.Add(group);
        }

        IsDirty = false;
        return mapping;
    }

    /// <summary>
    /// 标记映射为已修改。
    /// 应在任何编辑操作后调用，以启用保存功能。
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    #endregion

    #region 命令处理方法

    /// <summary>添加一个新的空白 IO 分组。</summary>
    private void OnAddGroup()
    {
        var groupVm = new IOGroupViewModel
        {
            Name = $"Group {Groups.Count + 1}",
            RegisterKind = "HoldingRegister",
            Offset = Groups.Sum(g => g.Offset + g.Points.Count * 2),
        };

        groupVm.MarkDirtyAction = () => MarkDirty();
        Groups.Add(groupVm);
        SelectedGroup = groupVm;
        MarkDirty();
    }

    /// <summary>删除当前选中的 IO 分组。</summary>
    private void OnRemoveGroup()
    {
        if (SelectedGroup == null) return;
        Groups.Remove(SelectedGroup);
        SelectedGroup = null;
        MarkDirty();
    }

    /// <summary>向当前选中的分组添加一个新的 IO 点。</summary>
    private void OnAddPoint()
    {
        if (SelectedGroup == null) return;

        var pointVm = new IOPointViewModel
        {
            Code = $"P{SelectedGroup.Points.Count + 1:D2}",
            Alias = string.Empty,
            Type = "bool",
            Access = "ReadWrite",
            DataType = SelectedGroup.RegisterKind switch
            {
                "Coil" or "DiscreteInput" => SelectedGroup.RegisterKind,
                _ => "Coil",
            },
            RegisterOffset = SelectedGroup.Points.Count,
        };

        SelectedGroup.Points.Add(pointVm);
        SelectedPoint = pointVm;
        MarkDirty();
    }

    /// <summary>删除当前选中的 IO 点。</summary>
    private void OnRemovePoint()
    {
        if (SelectedPoint == null || SelectedGroup == null) return;
        SelectedGroup.Points.Remove(SelectedPoint);
        SelectedPoint = null;
        MarkDirty();
    }

    /// <summary>从连接的设备导入 IO 点配置。</summary>
    private async System.Threading.Tasks.Task OnImportFromDeviceAsync()
    {
        await ExecuteBusyAsync(async _ =>
        {
            // 占位：实际实现将通过 Communication 层扫描设备寄存器
            await System.Threading.Tasks.Task.Delay(100);

            if (SelectedGroup == null)
            {
                SelectedGroup = new IOGroupViewModel
                {
                    Name = "Imported",
                    RegisterKind = "HoldingRegister",
                    Offset = 0,
                };
                SelectedGroup.MarkDirtyAction = () => MarkDirty();
                Groups.Add(SelectedGroup);
            }

            // 示例：添加几个占位导入的点
            for (int i = 0; i < 8; i++)
            {
                SelectedGroup.Points.Add(new IOPointViewModel
                {
                    Code = $"DI_{i:D2}",
                    Alias = $"Digital Input {i}",
                    Type = "bool",
                    Access = "Read",
                    DataType = "DiscreteInput",
                    RegisterOffset = i / 16,
                    BitIndex = i % 16,
                });
            }

            MarkDirty();
        }, "从设备导入 IO 配置失败。");
    }

    /// <summary>验证当前 IO 映射的完整性。</summary>
    private void OnValidateMapping()
    {
        var errors = new System.Collections.Generic.List<string>();

        if (string.IsNullOrWhiteSpace(MappingName))
            errors.Add("映射名称不能为空。");

        if (string.IsNullOrWhiteSpace(ConnectionConfig.Host))
            errors.Add("设备主机地址不能为空。");

        if (Groups.Count == 0)
            errors.Add("IO 映射至少需要一个分组。");

        foreach (var group in Groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
                errors.Add($"分组 '{group.Id}' 的名称不能为空。");

            var duplicateCodes = group.Points
                .GroupBy(p => p.Code)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var code in duplicateCodes)
                errors.Add($"分组 '{group.Name}' 中存在重复的点编码 '{code}'。");

            foreach (var point in group.Points)
            {
                if (string.IsNullOrWhiteSpace(point.Code))
                {
                    errors.Add($"分组 '{group.Name}' 中存在未命名的 IO 点。");
                    break;
                }
            }
        }

        if (errors.Count > 0)
            SetError(string.Join("\n", errors));
        else
            SetError(null);
    }

    #endregion
}
