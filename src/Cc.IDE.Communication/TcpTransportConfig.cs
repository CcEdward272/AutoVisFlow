namespace Cc.IDE.Communication;

/// <summary>
/// TCP/IP 套接字通信的配置参数。
/// </summary>
public sealed class TcpTransportConfig
{
    /// <summary>
    /// 目标主机名或 IP 地址。
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 目标 TCP 端口号。默认 5025（常见 SCPI 端口）。
    /// </summary>
    public int Port { get; set; } = 5025;

    /// <summary>
    /// 连接超时时间（毫秒）。默认 5000。
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 读取超时时间（毫秒）。默认 5000。
    /// </summary>
    public int ReadTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 写入超时时间（毫秒）。默认 5000。
    /// </summary>
    public int WriteTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// 终止字符。用于标记消息结束。默认 "\n"。
    /// </summary>
    public string TerminationChar { get; set; } = "\n";

    /// <summary>
    /// 是否启用终止字符的消息帧定界。
    /// </summary>
    public bool EnableTerminationChar { get; set; } = true;

    /// <summary>
    /// 是否启用 TCP Keep-Alive 探测。
    /// </summary>
    public bool UseKeepAlive { get; set; } = true;

    /// <summary>
    /// 是否禁用 Nagle 算法（启用 NoDelay 可降低延迟）。
    /// </summary>
    public bool NoDelay { get; set; } = true;
}
