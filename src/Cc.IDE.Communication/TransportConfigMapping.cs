using System.IO.Ports;

namespace Cc.IDE.Communication;

/// <summary>
/// 提供各传输配置类型之间的映射扩展方法。
/// 由于 Cc.IDE.Communication 不能反向依赖 Cc.IDE.DriverSdk（避免循环引用），
/// 这些映射方法仅接受基本类型参数。
/// </summary>
public static class TransportConfigMapping
{
    /// <summary>
    /// 从连接参数创建串口传输配置。
    /// </summary>
    /// <param name="portName">端口名称。</param>
    /// <param name="baudRate">波特率。</param>
    /// <param name="dataBits">数据位。</param>
    /// <param name="parity">校验位字符串（"None"/"Odd"/"Even"/"Mark"/"Space"）。</param>
    /// <param name="stopBits">停止位字符串（"1"/"1.5"/"2"）。</param>
    /// <param name="readTimeoutMs">读取超时（毫秒）。</param>
    /// <param name="writeTimeoutMs">写入超时（毫秒）。</param>
    /// <param name="terminationChar">终止字符。</param>
    /// <param name="enableTerminationChar">是否启用终止字符。</param>
    /// <returns>对应的串口传输配置。</returns>
    public static SerialTransportConfig CreateSerialConfig(
        string portName,
        int baudRate = 9600,
        int dataBits = 8,
        string parity = "None",
        string stopBits = "1",
        int readTimeoutMs = 5000,
        int writeTimeoutMs = 5000,
        string terminationChar = "\n",
        bool enableTerminationChar = true)
    {
        return new SerialTransportConfig
        {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = dataBits,
            Parity = ParseParity(parity),
            StopBits = ParseStopBits(stopBits),
            ReadTimeoutMs = readTimeoutMs,
            WriteTimeoutMs = writeTimeoutMs,
            TerminationChar = terminationChar,
            EnableTerminationChar = enableTerminationChar
        };
    }

    /// <summary>
    /// 从连接参数创建 TCP 传输配置。
    /// </summary>
    /// <param name="host">主机名或 IP 地址。</param>
    /// <param name="port">TCP 端口号。</param>
    /// <param name="connectTimeoutMs">连接超时（毫秒）。</param>
    /// <param name="readTimeoutMs">读取超时（毫秒）。</param>
    /// <param name="writeTimeoutMs">写入超时（毫秒）。</param>
    /// <param name="terminationChar">终止字符。</param>
    /// <param name="enableTerminationChar">是否启用终止字符。</param>
    /// <param name="useKeepAlive">是否启用 Keep-Alive。</param>
    /// <param name="noDelay">是否禁用 Nagle 算法。</param>
    /// <returns>对应的 TCP 传输配置。</returns>
    public static TcpTransportConfig CreateTcpConfig(
        string host,
        int port = 5025,
        int connectTimeoutMs = 5000,
        int readTimeoutMs = 5000,
        int writeTimeoutMs = 5000,
        string terminationChar = "\n",
        bool enableTerminationChar = true,
        bool useKeepAlive = true,
        bool noDelay = true)
    {
        return new TcpTransportConfig
        {
            Host = host,
            Port = port,
            ConnectTimeoutMs = connectTimeoutMs,
            ReadTimeoutMs = readTimeoutMs,
            WriteTimeoutMs = writeTimeoutMs,
            TerminationChar = terminationChar,
            EnableTerminationChar = enableTerminationChar,
            UseKeepAlive = useKeepAlive,
            NoDelay = noDelay
        };
    }

    /// <summary>
    /// 将校验位字符串解析为 <see cref="Parity"/> 枚举。
    /// </summary>
    /// <param name="parity">校验位字符串（不区分大小写）。</param>
    /// <returns>对应的 <see cref="Parity"/> 枚举值。</returns>
    public static Parity ParseParity(string? parity)
    {
        return parity?.ToLowerInvariant() switch
        {
            "odd" => Parity.Odd,
            "even" => Parity.Even,
            "mark" => Parity.Mark,
            "space" => Parity.Space,
            _ => Parity.None
        };
    }

    /// <summary>
    /// 将停止位字符串解析为 <see cref="StopBits"/> 枚举。
    /// </summary>
    /// <param name="stopBits">停止位字符串。</param>
    /// <returns>对应的 <see cref="StopBits"/> 枚举值。</returns>
    public static StopBits ParseStopBits(string? stopBits)
    {
        return stopBits switch
        {
            "1.5" => StopBits.OnePointFive,
            "2" => StopBits.Two,
            _ => StopBits.One
        };
    }
}
