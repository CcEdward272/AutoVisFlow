namespace Cc.IDE.Communication;

/// <summary>
/// 通信传输工厂，根据协议名称和配置创建对应的传输实例。
/// 创建的传输实例已预配置但尚未建立连接。
/// </summary>
/// <remarks>
/// <para>
/// 支持的协议名称（不区分大小写）：
/// <list type="bullet">
///   <item><c>"serial"</c> — RS-232/RS-485 串口通信，对应 <see cref="SerialTransport"/>。</item>
///   <item><c>"tcp"</c> — TCP/IP 套接字通信，对应 <see cref="TcpTransport"/>。</item>
/// </list>
/// </para>
/// </remarks>
public static class CommunicationFactory
{
    /// <summary>
    /// 为指定的协议名称创建对应的通信传输实例（使用默认配置）。
    /// </summary>
    /// <param name="protocol">协议标识符（如 "serial"、"tcp"）。</param>
    /// <returns>已就绪待连接的 <see cref="ICommunicationTransport"/> 实例。</returns>
    /// <exception cref="NotSupportedException">
    /// 当请求的 <paramref name="protocol"/> 没有已注册的实现时抛出。
    /// </exception>
    public static ICommunicationTransport Create(string protocol)
    {
        return protocol.ToLowerInvariant() switch
        {
            "serial" => new SerialTransport(),
            "tcp" => new TcpTransport(),
            _ => throw new NotSupportedException($"不支持的通信协议：'{protocol}'。")
        };
    }

    /// <summary>
    /// 使用配置对象创建传输实例。支持 <see cref="SerialTransportConfig"/> 和 <see cref="TcpTransportConfig"/>。
    /// </summary>
    /// <param name="protocol">协议标识符。</param>
    /// <param name="config">协议相关的配置对象。</param>
    /// <returns>已预配置的 <see cref="ICommunicationTransport"/> 实例。</returns>
    public static ICommunicationTransport Create(string protocol, object config)
    {
        return protocol.ToLowerInvariant() switch
        {
            "serial" => config is SerialTransportConfig sc ? new SerialTransport(sc)
                : throw new ArgumentException($"串口协议需要 {nameof(SerialTransportConfig)} 类型的配置。", nameof(config)),
            "tcp" => config is TcpTransportConfig tc ? new TcpTransport(tc)
                : throw new ArgumentException($"TCP 协议需要 {nameof(TcpTransportConfig)} 类型的配置。", nameof(config)),
            _ => throw new NotSupportedException($"不支持的通信协议：'{protocol}'。")
        };
    }
}
