namespace Cc.IDE.CAN;

/// <summary>
/// Represents a single CAN bus frame.
/// </summary>
public sealed class CanFrame
{
    /// <summary>CAN 标识符（11 位标准帧或 29 位扩展帧）。</summary>
    public uint CanId { get; set; }

    /// <summary>若为扩展帧（29 位标识符）则为 <c>true</c>。</summary>
    public bool IsExtended { get; set; }

    /// <summary>若为远程传输请求帧（RTR）则为 <c>true</c>。</summary>
    public bool IsRTR { get; set; }

    /// <summary>数据负载（CAN 2.0 为 0–8 字节，CAN FD 为 0–64 字节）。</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>接收到此帧时的时间戳（系统时钟计数）。</summary>
    public long Timestamp { get; set; }
}

/// <summary>
/// CAN interface abstraction. Implementations handle PCAN, SocketCAN, Kvaser, etc.
/// </summary>
public interface ICanInterface : IDisposable
{
    /// <summary>获取一个值，指示是否已连接到 CAN 接口。</summary>
    bool IsConnected { get; }

    /// <summary>
    /// 异步连接到 CAN 接口。
    /// </summary>
    /// <param name="ct">取消令牌，用于取消连接操作。</param>
    Task ConnectAsync(CancellationToken ct);

    /// <summary>
    /// 断开与 CAN 接口的连接。
    /// </summary>
    Task DisconnectAsync();

    /// <summary>发送单个 CAN 帧。</summary>
    /// <param name="frame">要发送的 CAN 帧。</param>
    /// <param name="ct">取消令牌。</param>
    Task SendFrameAsync(CanFrame frame, CancellationToken ct);

    /// <summary>读取待处理的已接收 CAN 帧。</summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>接收到的 CAN 帧只读列表。</returns>
    Task<IReadOnlyList<CanFrame>> ReadAsync(CancellationToken ct);

    /// <summary>当接收到 CAN 帧时引发的事件。</summary>
    event Action<CanFrame>? FrameReceived;
}
