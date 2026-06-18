using System;
using System.Collections.ObjectModel;
using Cc.IDE.Mvvm;

namespace Cc.IDE.IOMappingEditor;

/// <summary>
/// IO 分组的 ViewModel。
/// 表示一个命名的 IO 点分组，按寄存器类型和偏移量组织。
/// 每个分组包含一组相关的 <see cref="IOPointViewModel"/>。
/// </summary>
public class IOGroupViewModel : ViewModelBase
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = string.Empty;
    private bool _enabled = true;
    private string _registerKind = "HoldingRegister";
    private int _offset;
    private bool _isSelected;

    /// <summary>分组在当前 IO 映射中的唯一标识符。</summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>分组名称（例如 "Digital Inputs Slot 1"、"Analog Outputs"）。</summary>
    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value)) MarkDirtyAction?.Invoke(); }
    }

    /// <summary>当为 <c>false</c> 时，此分组中的所有点在执行期间被跳过。</summary>
    public bool Enabled
    {
        get => _enabled;
        set { if (SetProperty(ref _enabled, value)) MarkDirtyAction?.Invoke(); }
    }

    /// <summary>
    /// 寄存器类型：HoldingRegister | InputRegister | Coil | DiscreteInput |
    /// InputByte | OutputByte。
    /// </summary>
    public string RegisterKind
    {
        get => _registerKind;
        set { if (SetProperty(ref _registerKind, value)) MarkDirtyAction?.Invoke(); }
    }

    /// <summary>
    /// 此分组的基础寄存器偏移量。分组内各点的偏移量在此基础上累加
    /// 以计算出绝对寄存器地址。
    /// </summary>
    public int Offset
    {
        get => _offset;
        set { if (SetProperty(ref _offset, value)) MarkDirtyAction?.Invoke(); }
    }

    /// <summary>分组是否在 UI 中被选中。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>分组内 IO 点的可观察集合。</summary>
    public ObservableCollection<IOPointViewModel> Points { get; } = new();

    /// <summary>当任何属性被修改时触发，用于通知父 ViewModel 标记为脏。</summary>
    public Action? MarkDirtyAction { get; set; }
}
