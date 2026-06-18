using System.IO.Ports;

namespace Cc.IDE.Communication;

/// <summary>
/// RS-232/RS-485 串口通信的配置参数。
/// </summary>
public sealed class SerialTransportConfig
{
    /// <summary>
    /// 串口端口名称（如 "COM1"、"/dev/ttyUSB0"）。
    /// </summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>
    /// 波特率（bps）。默认 9600。
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 数据位（5/6/7/8）。默认 8。
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 校验位。默认 None。
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 停止位。默认 One。
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 读取超时时间（毫秒）。默认 5000。
    /// </summary>
    public int ReadTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 写入超时时间（毫秒）。默认 5000。
    /// </summary>
    public int WriteTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 是否启用握手信号（RTS/CTS）。
    /// </summary>
    public bool EnableHandshake { get; set; }

    /// <summary>
    /// 终止字符。用于标记消息结束。默认 "\n"。
    /// </summary>
    public string TerminationChar { get; set; } = "\n";

    /// <summary>
    /// 是否启用终止字符的消息帧定界。
    /// </summary>
    public bool EnableTerminationChar { get; set; } = true;
}
