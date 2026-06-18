namespace Cc.IDE.DriverSdk;

/// <summary>
/// 与仪器建立传输链接所需的连接参数。
/// 这是 DriverSdk 内部的自包含模型（不依赖 ProjectSystem），以避免循环项目引用。
/// </summary>
public sealed class DriverConnectionData
{
    /// <summary>
    /// 传输协议："Serial"、"TCP"、"GPIB"、"USB" 等。
    /// </summary>
    public string Protocol { get; set; } = "Serial";

    /// <summary>
    /// 仪器的逻辑或网络地址。串口为 COM 端口名称，TCP 为 IP 地址或主机名。
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 串口连接的 COM 端口名称（如 "COM3"、"/dev/ttyUSB0"）。
    /// </summary>
    public string? PortName { get; set; }

    /// <summary>
    /// 串口连接的波特率（bps）。默认 9600。
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 串口连接的数据位（5/6/7/8）。默认 8。
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 串口连接的校验位设置："None"、"Odd"、"Even"、"Mark"、"Space"。默认 "None"。
    /// </summary>
    public string Parity { get; set; } = "None";

    /// <summary>
    /// 串口连接的停止位："1"、"1.5"、"2"。默认 "1"。
    /// </summary>
    public string StopBits { get; set; } = "1";

    /// <summary>
    /// TCP 连接的主机名或 IP 地址。
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// TCP 连接的端口号。
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 通信超时时间（毫秒）。默认 5000（5 秒）。
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 失败操作的重试次数。默认 2。
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// 若为 <c>true</c> 则启用基于终止字符的消息帧定界。
    /// </summary>
    public bool EnableTerminationChar { get; set; } = true;

    /// <summary>
    /// 消息帧定界使用的终止字符。默认 "\n"。
    /// </summary>
    public string TerminationChar { get; set; } = "\n";

    /// <summary>
    /// 额外的协议或驱动特定配置键值对。
    /// </summary>
    public Dictionary<string, object?>? Extra { get; set; }
}
