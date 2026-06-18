using System;
using System.Text.Json.Serialization;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.IOMappingEditor;

/// <summary>
/// 单个 IO 点的 ViewModel。
/// 将物理 PLC 寄存器地址映射到逻辑名称、数据类型、访问模式、
/// 缩放配置、安全值和极性等属性。
/// </summary>
public class IOPointViewModel : ViewModelBase
{
    private bool _enabled = true;
    private string _code = string.Empty;
    private string _alias = string.Empty;
    private string _type = "bool";
    private string _access = "ReadWrite";
    private string _dataType = "Coil";
    private IoPointPolarity _polarity = IoPointPolarity.Normal;
    private int _registerOffset;
    private int _bitIndex;
    private AnalogScaleDefinition? _scale;
    private object? _safeValue;
    private string _unit = string.Empty;
    private string _description = string.Empty;
    private bool _isSelected;

    /// <summary>当为 <c>false</c> 时，此点在 IO 操作中被跳过。</summary>
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    /// <summary>点在所属分组内的唯一编码（例如 "DI_01"、"AO_Temp_Setpoint"）。</summary>
    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    /// <summary>此点的人可读别名（例如 "阀门 A 开启反馈"）。</summary>
    public string Alias
    {
        get => _alias;
        set => SetProperty(ref _alias, value);
    }

    /// <summary>
    /// 数据类型：bool | byte | short | ushort | int | uint | float | double | string。
    /// </summary>
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    /// <summary>访问模式：Read | Write | ReadWrite。</summary>
    public string Access
    {
        get => _access;
        set => SetProperty(ref _access, value);
    }

    /// <summary>
    /// Modbus 数据类型，用于寄存器解释：
    /// Coil | DiscreteInput | HoldingRegister | InputRegister | InputByte | OutputByte。
    /// </summary>
    public string DataType
    {
        get => _dataType;
        set => SetProperty(ref _dataType, value);
    }

    /// <summary>在缩放之前应用于原始值的极性。</summary>
    public IoPointPolarity Polarity
    {
        get => _polarity;
        set => SetProperty(ref _polarity, value);
    }

    /// <summary>相对于父分组基准偏移量的寄存器偏移。</summary>
    public int RegisterOffset
    {
        get => _registerOffset;
        set => SetProperty(ref _registerOffset, value);
    }

    /// <summary>
    /// 对于位级别类型（Coil、DiscreteInput），表示 16 位寄存器中的位索引（0-15）。
    /// 对于寄存器级别类型，此值被忽略。
    /// </summary>
    public int BitIndex
    {
        get => _bitIndex;
        set => SetProperty(ref _bitIndex, value);
    }

    /// <summary>
    /// 模拟量缩放定义。对于数字量点为 <c>null</c>；
    /// 对于模拟量点（AI/AO）为必需，用于将原始值映射到工程单位。
    /// </summary>
    public AnalogScaleDefinition? Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, value);
    }

    /// <summary>当安全条件触发时（例如急停），写入此点的安全/默认值。</summary>
    public object? SafeValue
    {
        get => _safeValue;
        set => SetProperty(ref _safeValue, value);
    }

    /// <summary>工程单位标签（例如 "V"、"mA"、"degC"、"bar"、"%"）。</summary>
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    /// <summary>此点的可选描述文本。</summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>点是否在 UI 中被选中。</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    #region 计算属性

    /// <summary>当此点支持读取时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool CanRead => Access is "Read" or "ReadWrite";

    /// <summary>当此点支持写入时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool CanWrite => Access is "Write" or "ReadWrite";

    /// <summary>当此点使用模拟量缩放时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsAnalog => Type is "short" or "ushort" or "int"
        or "uint" or "float" or "double";

    /// <summary>当此点是位级别类型（Coil 或 DiscreteInput）时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsBitLevel => DataType is "Coil" or "DiscreteInput";

    #endregion
}
