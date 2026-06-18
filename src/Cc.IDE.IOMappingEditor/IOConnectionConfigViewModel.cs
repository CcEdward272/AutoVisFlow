using System;
using System.Text.Json.Serialization;
using Cc.IDE.Mvvm;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.IOMappingEditor;

/// <summary>
/// IO 连接配置的 ViewModel。
/// 封装与 PLC 或远程 IO 设备通信所需的连接参数，
/// 包括协议选择、地址、端口、从站 ID、CAN 总线参数和超时/重试设置。
/// </summary>
public class IOConnectionConfigViewModel : ViewModelBase
{
    private string _plcProtocol = "ModbusTCP";
    private string _host = "127.0.0.1";
    private int _port = 502;
    private int _slaveId = 1;
    private int _rack;
    private int _slot;
    private string _canInterface = string.Empty;
    private int _bitRate = 500000;
    private int _canChannel;
    private int _pollIntervalMs = 100;
    private int _timeoutMs = 3000;
    private int _retryCount = 2;
    private string _deviceId = string.Empty;

    /// <summary>
    /// PLC 通信协议：ModbusTCP | ModbusRTU | CANopen |
    /// EtherNetIP | Profinet | EtherCAT。
    /// </summary>
    public string PlcProtocol
    {
        get => _plcProtocol;
        set
        {
            if (SetProperty(ref _plcProtocol, value))
            {
                OnPropertyChanged(nameof(IsModbusTcp));
                OnPropertyChanged(nameof(IsModbusRtu));
                OnPropertyChanged(nameof(IsCanBus));
                OnPropertyChanged(nameof(IsEthernet));
                OnPropertyChanged(nameof(ConnectionString));
            }
        }
    }

    /// <summary>PLC 的主机名或 IP 地址（用于 TCP 协议）。</summary>
    public string Host
    {
        get => _host;
        set
        {
            if (SetProperty(ref _host, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    /// <summary>TCP 端口号。Modbus TCP 默认为 502。</summary>
    public int Port
    {
        get => _port;
        set
        {
            if (SetProperty(ref _port, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    /// <summary>Modbus 从站/单元 ID（1-247）。默认为 1。</summary>
    public int SlaveId
    {
        get => _slaveId;
        set => SetProperty(ref _slaveId, value);
    }

    /// <summary>PLC 机架号（用于使用机架/槽位寻址的协议）。</summary>
    public int Rack
    {
        get => _rack;
        set => SetProperty(ref _rack, value);
    }

    /// <summary>PLC 槽位号（用于使用机架/槽位寻址的协议）。</summary>
    public int Slot
    {
        get => _slot;
        set => SetProperty(ref _slot, value);
    }

    /// <summary>
    /// CAN 总线接口名称（例如 "can0"、"PCAN_USBBUS1"）。
    /// 当 <see cref="PlcProtocol"/> 为 "CANopen" 时有效。
    /// </summary>
    public string CanInterface
    {
        get => _canInterface;
        set
        {
            if (SetProperty(ref _canInterface, value))
                OnPropertyChanged(nameof(ConnectionString));
        }
    }

    /// <summary>CAN 总线比特率（bps）。默认为 500 kbps。</summary>
    public int BitRate
    {
        get => _bitRate;
        set => SetProperty(ref _bitRate, value);
    }

    /// <summary>CAN 通道号（用于多通道 CAN 适配器）。</summary>
    public int CanChannel
    {
        get => _canChannel;
        set => SetProperty(ref _canChannel, value);
    }

    /// <summary>循环数据读取的轮询间隔（毫秒）。默认为 100 ms（10 Hz）。</summary>
    public int PollIntervalMs
    {
        get => _pollIntervalMs;
        set => SetProperty(ref _pollIntervalMs, value);
    }

    /// <summary>通信超时时间（毫秒）。默认为 3000（3 秒）。</summary>
    public int TimeoutMs
    {
        get => _timeoutMs;
        set => SetProperty(ref _timeoutMs, value);
    }

    /// <summary>通信失败时的重试次数。默认为 2。</summary>
    public int RetryCount
    {
        get => _retryCount;
        set => SetProperty(ref _retryCount, value);
    }

    /// <summary>与此连接关联的设备标识符。</summary>
    public string? DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value);
    }

    #region 计算属性

    /// <summary>当协议为 Modbus TCP 时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsModbusTcp => PlcProtocol is "ModbusTCP";

    /// <summary>当协议为 Modbus RTU（串行）时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsModbusRtu => PlcProtocol is "ModbusRTU";

    /// <summary>当协议使用 CAN 总线时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsCanBus => PlcProtocol is "CANopen";

    /// <summary>当协议使用以太网时返回 <c>true</c>。</summary>
    [JsonIgnore]
    public bool IsEthernet => PlcProtocol is "ModbusTCP" or "EtherNetIP"
        or "Profinet" or "EtherCAT";

    /// <summary>
    /// 获取有效的连接字符串：
    /// TCP 协议返回 host:port，
    /// CAN 总线返回接口名称，
    /// RTU 返回主机名。
    /// </summary>
    [JsonIgnore]
    public string ConnectionString => IsEthernet
        ? $"{Host}:{Port}"
        : IsCanBus
            ? CanInterface
            : Host;

    #endregion

    #region 公共方法

    /// <summary>
    /// 从 <see cref="IOConnectionConfig"/> 模型加载连接参数。
    /// </summary>
    /// <param name="config">要加载的连接配置。</param>
    public void LoadFromConfig(IOConnectionConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        PlcProtocol = config.PlcProtocol;
        Host = config.Host;
        Port = config.Port;
        SlaveId = config.SlaveId;
        Rack = config.Rack;
        Slot = config.Slot;
        CanInterface = config.CanInterface;
        BitRate = config.BitRate;
        CanChannel = config.CanChannel;
        PollIntervalMs = config.PollIntervalMs;
        TimeoutMs = config.TimeoutMs;
        RetryCount = config.RetryCount;
    }

    /// <summary>
    /// 将当前连接参数保存为 <see cref="IOConnectionConfig"/>。
    /// </summary>
    /// <returns>包含当前连接参数的连接配置对象。</returns>
    public IOConnectionConfig SaveToConfig()
    {
        return new IOConnectionConfig
        {
            PlcProtocol = PlcProtocol,
            Host = Host,
            Port = Port,
            SlaveId = SlaveId,
            Rack = Rack,
            Slot = Slot,
            CanInterface = CanInterface,
            BitRate = BitRate,
            CanChannel = CanChannel,
            PollIntervalMs = PollIntervalMs,
            TimeoutMs = TimeoutMs,
            RetryCount = RetryCount,
        };
    }

    #endregion
}
