namespace Cc.IDE.Communication;

/// <summary>
/// 仪器物理或网络通信通道的抽象。
/// 实现类处理协议特定的连接、发送和接收数据的细节。
/// </summary>
/// <remarks>
/// 实现必须是线程安全的，以支持来自不同线程的并发 <see cref="SendAsync"/> 和
/// <see cref="ReceiveAsync"/> 调用。<see cref="ConnectAsync"/> 和 <see cref="DisconnectAsync"/>
/// 预期从单个生命周期线程调用。
/// </remarks>
public interface ICommunicationTransport : IDisposable
{
    /// <summary>
    /// 当传输层与仪器建立活动连接时为 <c>true</c>。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 打开与仪器的通信通道。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="TimeoutException">连接超时时抛出。</exception>
    /// <exception cref="InvalidOperationException">已处于连接状态时抛出。</exception>
    Task ConnectAsync(CancellationToken ct);

    /// <summary>
    /// 优雅关闭通信通道。已断开时调用不会产生副作用。
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 向仪器发送原始数据。数据格式（SCPI、二进制等）由调用者决定。
    /// </summary>
    /// <param name="data">要发送的原始字符串数据。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    Task SendAsync(string data, CancellationToken ct);

    /// <summary>
    /// 从仪器接收响应。阻塞直到收到完整消息（消息帧格式取决于具体传输实现）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>接收到的响应字符串。</returns>
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="TimeoutException">在超时时间内未收到响应时抛出。</exception>
    Task<string> ReceiveAsync(CancellationToken ct);

    /// <summary>
    /// 便捷方法：发送命令并等待响应。
    /// 等同于先调用 <see cref="SendAsync"/> 再调用 <see cref="ReceiveAsync"/>。
    /// </summary>
    /// <param name="command">要发送的命令字符串。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>来自仪器的响应字符串。</returns>
    Task<string> QueryAsync(string command, CancellationToken ct);
}
