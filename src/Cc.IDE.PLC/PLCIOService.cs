using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cc.IDE.PLC;

/// <summary>
/// PLC IO 服务实现。
/// 将 PLC 协议层的读写操作抽象为统一的点位操作模型。
/// </summary>
public sealed class PLCIOService : IIOService
{
    /// <summary>
    /// PLC 设备管理服务实例。
    /// </summary>
    private readonly IPLCService _plcService;

    /// <summary>
    /// 点位解析器实例。
    /// </summary>
    private readonly IIOPointResolver _pointResolver;

    /// <summary>
    /// 数字输出（DO）锁定状态字典，键为设备标识符。
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _outputLocked = new();

    /// <summary>
    /// 轮询间隔（毫秒）。
    /// </summary>
    private const int DefaultPollIntervalMs = 50;

    /// <summary>
    /// 初始化 <see cref="PLCIOService"/> 的新实例。
    /// </summary>
    /// <param name="plcService">PLC 设备管理服务实例。</param>
    /// <exception cref="ArgumentNullException"><paramref name="plcService"/> 为 <c>null</c>。</exception>
    public PLCIOService(IPLCService plcService)
    {
        _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
        _pointResolver = new DefaultPlcPointResolver();
    }

    /// <summary>
    /// 初始化 <see cref="PLCIOService"/> 的新实例并指定自定义点位解析器。
    /// </summary>
    /// <param name="plcService">PLC 设备管理服务实例。</param>
    /// <param name="pointResolver">自定义点位解析器。</param>
    /// <exception cref="ArgumentNullException"><paramref name="plcService"/> 或 <paramref name="pointResolver"/> 为 <c>null</c>。</exception>
    public PLCIOService(IPLCService plcService, IIOPointResolver pointResolver)
    {
        _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
        _pointResolver = pointResolver ?? throw new ArgumentNullException(nameof(pointResolver));
    }

    /// <summary>
    /// 服务类型标识，始终返回 "PLC"。
    /// </summary>
    public string ServiceType => "PLC";

    /// <summary>
    /// 获取指定设备的数字输出（DO）是否已锁定。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <returns>如果已锁定则返回 <c>true</c>。</returns>
    public bool IsLocked(string deviceId) =>
        _outputLocked.TryGetValue(deviceId, out var locked) && locked;

    /// <summary>
    /// 锁定指定设备的数字输出（DO）写入操作。
    /// 锁定后，所有写入 DO 点位的操作将抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    public void Lock(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId));

        _outputLocked[deviceId] = true;
    }

    /// <summary>
    /// 解锁指定设备的数字输出（DO）写入操作。
    /// 解锁后，DO 写入恢复正常。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    public void Unlock(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId));

        _outputLocked.TryRemove(deviceId, out _);
    }

    /// <summary>
    /// 解析逻辑点位代码为物理地址信息。
    /// </summary>
    /// <param name="pointCode">逻辑点位代码（如 "D100"、"Y0"）。</param>
    /// <returns>物理地址解析结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pointCode"/> 为 <c>null</c> 或空白。</exception>
    /// <exception cref="ArgumentException">无法解析点位代码格式。</exception>
    public IOPointResolution ResolvePoint(string pointCode)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
            throw new ArgumentNullException(nameof(pointCode), "点位代码不能为空。");

        var resolution = _pointResolver.Resolve(pointCode);
        if (resolution is null)
            throw new ArgumentException($"无法解析点位代码 '{pointCode}'，格式不支持。", nameof(pointCode));

        return resolution;
    }

    /// <summary>
    /// 读取指定点位的当前值。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>点位的当前值。线圈/离散输入返回 <see cref="bool"/>，寄存器返回 <see cref="ushort"/>。</returns>
    public async Task<object> ReadAsync(string deviceId, string pointCode, CancellationToken ct)
    {
        var resolution = ResolvePoint(pointCode);

        return resolution.RegisterKind switch
        {
            "Coil" => (object)(await _plcService.ReadCoilAsync(deviceId, resolution.RegisterOffset, ct).ConfigureAwait(false)),
            "DiscreteInput" => (object)(await _plcService.ReadCoilAsync(deviceId, resolution.RegisterOffset, ct).ConfigureAwait(false)),
            "HoldingRegister" => (object)(await _plcService.ReadRegisterAsync(deviceId, resolution.RegisterOffset, ct).ConfigureAwait(false)),
            "InputRegister" => (object)(await _plcService.ReadRegisterAsync(deviceId, resolution.RegisterOffset, ct).ConfigureAwait(false)),
            _ => throw new InvalidOperationException($"不支持的寄存器类型 '{resolution.RegisterKind}'。"),
        };
    }

    /// <summary>
    /// 向指定点位写入值。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="value">要写入的值。线圈类型应为 <see cref="bool"/>，寄存器类型应为 <see cref="ushort"/>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">IO 输出已锁定且写入 DO 点位时抛出。</exception>
    public async Task WriteAsync(string deviceId, string pointCode, object value, CancellationToken ct)
    {
        var resolution = ResolvePoint(pointCode);

        // 检查输出锁定：数字输出（Coil）写入时需要检查锁定状态
        if (resolution.RegisterKind is "Coil" && IsLocked(deviceId))
            throw new InvalidOperationException("IO 输出已锁定，请先解锁。");

        switch (resolution.RegisterKind)
        {
            case "Coil":
                await _plcService.WriteCoilAsync(deviceId, resolution.RegisterOffset, Convert.ToBoolean(value), ct).ConfigureAwait(false);
                break;

            case "HoldingRegister":
                await _plcService.WriteRegisterAsync(deviceId, resolution.RegisterOffset, Convert.ToUInt16(value), ct).ConfigureAwait(false);
                break;

            case "InputRegister":
            case "DiscreteInput":
                throw new InvalidOperationException($"'{resolution.RegisterKind}' 类型为只读，不支持写入操作。");

            default:
                throw new InvalidOperationException($"不支持的寄存器类型 '{resolution.RegisterKind}'。");
        }
    }

    /// <summary>
    /// 等待点位达到目标值。
    /// 通过轮询读取点位值，直到达到目标值或超时。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="targetValue">期望的目标值。</param>
    /// <param name="timeoutMs">最大等待时间（毫秒）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>如果在超时前达到目标值则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    public async Task<bool> WaitForAsync(string deviceId, string pointCode, object targetValue, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0)
            throw new ArgumentException("超时时间必须大于 0。", nameof(timeoutMs));

        var stopwatch = Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            // 检查是否超时
            if (stopwatch.ElapsedMilliseconds >= timeoutMs)
                return false;

            try
            {
                var currentValue = await ReadAsync(deviceId, pointCode, ct).ConfigureAwait(false);

                // 比较当前值与目标值
                if (Equals(currentValue, targetValue))
                    return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // 读取失败时继续轮询，但短暂等待避免空转
                await Task.Delay(DefaultPollIntervalMs, ct).ConfigureAwait(false);
                continue;
            }

            // 等待下一次轮询
            await Task.Delay(DefaultPollIntervalMs, ct).ConfigureAwait(false);
        }

        return false;
    }
}

/// <summary>
/// 默认 PLC 点位代码解析器。
/// 支持常见 PLC 点位格式：
/// - X{数字}：离散输入（DiscreteInput）
/// - Y{数字}：线圈（Coil）
/// - D{数字}：保持寄存器（HoldingRegister）
/// - M{数字}：内部继电器（Coil）
/// </summary>
public sealed class DefaultPlcPointResolver : IIOPointResolver
{
    // 已编译的正则表达式，用于匹配点位代码格式
    private static readonly Regex XPointPattern = new(@"^X(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex YPointPattern = new(@"^Y(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DPointPattern = new(@"^D(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MPointPattern = new(@"^M(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 解析点位代码字符串为物理地址信息。
    /// </summary>
    /// <param name="pointCode">点位代码（如 "D100"、"X0"、"Y5"、"M0"）。</param>
    /// <returns>解析后的物理地址信息；无法解析时返回 <c>null</c>。</returns>
    public IOPointResolution? Resolve(string pointCode)
    {
        if (string.IsNullOrWhiteSpace(pointCode))
            return null;

        // X 点：离散输入（只读）
        var xMatch = XPointPattern.Match(pointCode);
        if (xMatch.Success)
        {
            return new IOPointResolution
            {
                RegisterKind = "DiscreteInput",
                RegisterOffset = int.Parse(xMatch.Groups[1].Value),
                BitIndex = -1,
                DataType = "Bool",
                Access = "Read",
            };
        }

        // Y 点：线圈（读写）
        var yMatch = YPointPattern.Match(pointCode);
        if (yMatch.Success)
        {
            return new IOPointResolution
            {
                RegisterKind = "Coil",
                RegisterOffset = int.Parse(yMatch.Groups[1].Value),
                BitIndex = -1,
                DataType = "Bool",
                Access = "ReadWrite",
            };
        }

        // M 点：内部继电器 / 辅助继电器（模拟为 Coil，读写）
        var mMatch = MPointPattern.Match(pointCode);
        if (mMatch.Success)
        {
            return new IOPointResolution
            {
                RegisterKind = "Coil",
                RegisterOffset = int.Parse(mMatch.Groups[1].Value),
                BitIndex = -1,
                DataType = "Bool",
                Access = "ReadWrite",
            };
        }

        // D 点：数据寄存器（保持寄存器，读写）
        var dMatch = DPointPattern.Match(pointCode);
        if (dMatch.Success)
        {
            return new IOPointResolution
            {
                RegisterKind = "HoldingRegister",
                RegisterOffset = int.Parse(dMatch.Groups[1].Value),
                BitIndex = -1,
                DataType = "UInt16",
                Access = "ReadWrite",
            };
        }

        // 无法匹配任何已知格式
        return null;
    }
}
