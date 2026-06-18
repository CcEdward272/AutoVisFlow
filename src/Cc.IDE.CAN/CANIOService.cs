using System.Globalization;
using Cc.IDE.Communication;

namespace Cc.IDE.CAN;

/// <summary>
/// CAN 特定 IO 服务实现。
/// 基于 <see cref="ICANService"/> 提供符合 <see cref="IIOService"/> 接口的数据读写能力。
/// </summary>
/// <remarks>
/// <para>
/// CAN IO 服务通过 CAN 总线帧读写实现 IO 点的数据访问。
/// 点标识格式支持：
/// </para>
/// <list type="bullet">
///   <item><c>"0x201:3:0"</c> — CAN ID 0x201，第 3 字节，位 0</item>
///   <item><c>"0x201:3"</c>   — CAN ID 0x201，第 3 字节（整字节）</item>
///   <item><c>"0x201"</c>     — CAN ID 0x201，全帧数据（<see cref="CanFrame.Data"/>）</item>
/// </list>
/// </remarks>
public sealed class CANIOService : IIOService
{
    /// <summary>
    /// CAN 总线设备管理服务实例。
    /// </summary>
    private readonly ICANService _canService;

    /// <summary>
    /// CAN 接口名称，用于标识此 IO 服务绑定的 CAN 通道。
    /// </summary>
    private readonly string _interfaceName;

    /// <summary>
    /// 服务类型标识，固定为 "CAN"。
    /// </summary>
    public string ServiceType => "CAN";

    /// <summary>
    /// 使用指定的 CAN 服务实例和接口名称初始化 <see cref="CANIOService"/>。
    /// </summary>
    /// <param name="canService">CAN 总线设备管理服务实例。</param>
    /// <param name="interfaceName">要绑定的 CAN 接口名称。</param>
    /// <exception cref="ArgumentNullException"><paramref name="canService"/> 为 <c>null</c>。</exception>
    /// <exception cref="ArgumentException"><paramref name="interfaceName"/> 为 <c>null</c> 或空白。</exception>
    public CANIOService(ICANService canService, string interfaceName)
    {
        _canService = canService ?? throw new ArgumentNullException(nameof(canService), "CAN 服务实例不能为空。");

        if (string.IsNullOrWhiteSpace(interfaceName))
            throw new ArgumentException("CAN 接口名称不能为空。", nameof(interfaceName));

        _interfaceName = interfaceName;
    }

    /// <summary>
    /// 将字符串格式的 CAN 点标识解析为 <see cref="IOPointResolution"/>。
    /// </summary>
    /// <param name="pointCode">IO 点标识字符串。格式为 <c>"0xCANID:ByteOffset:BitOffset"</c>。</param>
    /// <returns>解析后的 IO 点信息。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pointCode"/> 为 <c>null</c> 或空白。</exception>
    /// <exception cref="FormatException">点标识格式无法解析时抛出。</exception>
    /// <remarks>
    /// <para>
    /// CAN IO 点标识格式说明：
    /// </para>
    /// <list type="bullet">
    ///   <item><c>"0x201:3:0"</c> — CAN ID 0x201，第 3 字节（0-based），位 0（LSB）</item>
    ///   <item><c>"0x201:3"</c>   — CAN ID 0x201，第 3 字节（0-based）</item>
    ///   <item><c>"0x201"</c>     — CAN ID 0x201，全帧 8 字节</item>
    /// </list>
    /// </remarks>
    public IOPointResolution ResolvePoint(string pointCode)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
            throw new ArgumentNullException(nameof(pointCode), "CAN IO 点标识不能为空。");

        try
        {
            var parts = pointCode.Split(':');

            // 解析 CAN ID（支持 0x 十六进制前缀）
            var canIdStr = parts[0];
            uint canId;

            if (canIdStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                || canIdStr.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
            {
                canId = uint.Parse(canIdStr.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            else
            {
                canId = uint.Parse(canIdStr, CultureInfo.InvariantCulture);
            }

            // 默认数据长度为 CAN 帧最大数据长度（8 字节标准帧）
            var byteOffset = -1;
            var bitOffset = -1;
            var dataLength = 8;

            if (parts.Length >= 2)
            {
                // 解析字节偏移
                byteOffset = int.Parse(parts[1], CultureInfo.InvariantCulture);
                dataLength = 1; // 单字节

                if (parts.Length >= 3)
                {
                    // 解析位偏移
                    bitOffset = int.Parse(parts[2], CultureInfo.InvariantCulture);
                    dataLength = 1; // 单个位，以字节读取
                }
            }

            return new IOPointResolution
            {
                Address = (int)canId,
                SubAddress = byteOffset,
                BitOffset = bitOffset,
                DataLength = dataLength,
                Metadata = new Dictionary<string, object>
                {
                    ["canId"] = canId,
                    ["interfaceName"] = _interfaceName,
                },
            };
        }
        catch (FormatException)
        {
            throw new FormatException($"CAN IO 点标识格式无效：'{pointCode}'。支持的格式: \"0xCANID:ByteOffset:BitOffset\"。");
        }
        catch (OverflowException ex)
        {
            throw new FormatException($"CAN IO 点标识中的数值超出范围：'{pointCode}'。", ex);
        }
    }

    /// <summary>
    /// 从指定 CAN IO 点读取数据。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>IO 点的当前数据字节数组。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolution"/> 为 <c>null</c>。</exception>
    /// <exception cref="InvalidOperationException">CAN 接口未连接。</exception>
    public async Task<byte[]> ReadAsync(IOPointResolution resolution, CancellationToken ct)
    {
        if (resolution is null)
            throw new ArgumentNullException(nameof(resolution), "IO 点解析结果不能为空。");

        // 确保接口已连接
        if (!_canService.IsInterfaceConnected(_interfaceName))
            throw new InvalidOperationException($"CAN 接口 '{_interfaceName}' 未连接，无法执行读操作。请先通过 CANService 连接接口。");

        // 读取 CAN 帧
        var frames = await _canService.ReadAsync(_interfaceName, ct).ConfigureAwait(false);

        // 查找匹配 CAN ID 的最新帧
        var targetCanId = (uint)resolution.Address;
        var matchingFrame = frames.LastOrDefault(f => f.CanId == targetCanId);

        if (matchingFrame is null)
        {
            // 未找到匹配帧，返回零填充数据
            return new byte[resolution.DataLength];
        }

        if (resolution.SubAddress < 0)
        {
            // 返回全帧数据
            var result = new byte[Math.Min(matchingFrame.Data.Length, resolution.DataLength)];
            Array.Copy(matchingFrame.Data, result, result.Length);
            return result;
        }

        if (resolution.SubAddress >= matchingFrame.Data.Length)
        {
            // 字节偏移超出数据长度，返回零
            return new byte[resolution.DataLength];
        }

        if (resolution.BitOffset >= 0)
        {
            // 读取单个位
            var byteValue = matchingFrame.Data[resolution.SubAddress];
            var bitValue = (byteValue >> resolution.BitOffset) & 1;
            return new[] { (byte)bitValue };
        }

        // 读取单个字节
        return new[] { matchingFrame.Data[resolution.SubAddress] };
    }

    /// <summary>
    /// 向指定 CAN IO 点写入数据。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="data">要写入的数据字节数组。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="ArgumentNullException"><paramref name="resolution"/> 或 <paramref name="data"/> 为 <c>null</c>。</exception>
    /// <exception cref="InvalidOperationException">CAN 接口未连接。</exception>
    /// <exception cref="ArgumentException">数据长度超过 CAN 帧负载限制（8 字节标准帧）。</exception>
    public async Task WriteAsync(IOPointResolution resolution, byte[] data, CancellationToken ct)
    {
        if (resolution is null)
            throw new ArgumentNullException(nameof(resolution), "IO 点解析结果不能为空。");

        if (data is null)
            throw new ArgumentNullException(nameof(data), "写入数据不能为空。");

        // 确保接口已连接
        if (!_canService.IsInterfaceConnected(_interfaceName))
            throw new InvalidOperationException($"CAN 接口 '{_interfaceName}' 未连接，无法执行写操作。请先通过 CANService 连接接口。");

        // 构建 CAN 帧
        var frameData = new byte[8]; // CAN 2.0 标准帧固定 8 字节
        var targetCanId = (uint)resolution.Address;

        if (resolution.SubAddress < 0)
        {
            // 写入全帧数据
            var copyLength = Math.Min(data.Length, frameData.Length);
            Array.Copy(data, 0, frameData, 0, copyLength);
        }
        else if (resolution.BitOffset >= 0)
        {
            // 写入单个位
            if (data.Length < 1)
                throw new ArgumentException("位写入至少需要 1 字节数据。", nameof(data));

            // 需要先读取当前帧的该字节以保留其他位
            // 使用零填充作为默认值
            frameData[resolution.SubAddress] = (byte)(data[0] & 1); // 仅取 LSB
        }
        else
        {
            // 写入单个字节
            if (data.Length < 1)
                throw new ArgumentException("字节写入至少需要 1 字节数据。", nameof(data));

            frameData[resolution.SubAddress] = data[0];
        }

        // 发送 CAN 帧
        var frame = new CanFrame
        {
            CanId = targetCanId,
            IsExtended = targetCanId > 0x7FF, // 11 位标准帧最大值为 0x7FF
            IsRTR = false,
            Data = frameData,
            Timestamp = DateTime.UtcNow.Ticks,
        };

        await _canService.SendFrameAsync(_interfaceName, frame, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 等待指定 CAN IO 点达到期望值（轮询模式）。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="expectedValue">期望的字节值。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="pollInterval">轮询间隔。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>若在超时内达到期望值则为 <c>true</c>，否则为 <c>false</c>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resolution"/> 或 <paramref name="expectedValue"/> 为 <c>null</c>。</exception>
    public async Task<bool> WaitForAsync(IOPointResolution resolution, byte[] expectedValue, TimeSpan timeout, TimeSpan pollInterval, CancellationToken ct)
    {
        if (resolution is null)
            throw new ArgumentNullException(nameof(resolution), "IO 点解析结果不能为空。");

        if (expectedValue is null)
            throw new ArgumentNullException(nameof(expectedValue), "期望值不能为空。");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            while (!timeoutCts.Token.IsCancellationRequested)
            {
                // 读取当前值
                var currentValue = await ReadAsync(resolution, timeoutCts.Token).ConfigureAwait(false);

                // 检查是否匹配期望值
                if (currentValue.AsSpan().SequenceEqual(expectedValue.AsSpan()))
                    return true;

                // 等待下一个轮询间隔
                await Task.Delay(pollInterval, timeoutCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // 超时或外部取消
        }

        return false;
    }
}
