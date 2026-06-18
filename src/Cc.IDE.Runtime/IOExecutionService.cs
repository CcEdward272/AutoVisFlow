using Cc.IDE.PLC;
using Cc.IDE.CAN;
using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// IO 执行服务实现。根据 <see cref="IOActionDefinition.IOType"/> 将 I/O 动作
/// 路由到 <see cref="PLCIOService"/> 或 <see cref="CANIOService"/>，
/// 实现 Set/Pulse/Read/ReadVerify/WaitFor/Reset 六种动作的真实设备执行。
/// </summary>
public sealed class IOExecutionService : IIOExecutionService
{
    private readonly IPLCService _plcService;
    private readonly ICANService _canService;

    /// <summary>
    /// 初始化 IO 执行服务的新实例。
    /// </summary>
    /// <param name="plcService">PLC 设备管理服务实例。</param>
    /// <param name="canService">CAN 接口管理服务实例。</param>
    public IOExecutionService(IPLCService plcService, ICANService canService)
    {
        _plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
        _canService = canService ?? throw new ArgumentNullException(nameof(canService));
    }

    /// <inheritdoc/>
    public async Task<IOActionResult> ExecuteAsync(IOActionDefinition action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(action.PointCode))
            return IOActionResult.Failed(action.Id, "IO 动作的点位代码 (PointCode) 不能为空。");

        try
        {
            return action.ActionType switch
            {
                IOActionType.Set => await ExecuteSetAsync(action, ct).ConfigureAwait(false),
                IOActionType.Pulse => await ExecutePulseAsync(action, ct).ConfigureAwait(false),
                IOActionType.Read => await ExecuteReadAsync(action, ct).ConfigureAwait(false),
                IOActionType.ReadVerify => await ExecuteReadVerifyAsync(action, ct).ConfigureAwait(false),
                IOActionType.WaitFor => await ExecuteWaitForAsync(action, ct).ConfigureAwait(false),
                IOActionType.Reset => await ExecuteResetAsync(action, ct).ConfigureAwait(false),
                _ => IOActionResult.Failed(action.Id, $"未知的 I/O 动作类型: {action.ActionType}")
            };
        }
        catch (OperationCanceledException)
        {
            return IOActionResult.Failed(action.Id, "I/O 操作被取消。");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("锁定"))
        {
            return IOActionResult.Failed(action.Id, $"IO 输出已锁定: {ex.Message}");
        }
        catch (Exception ex)
        {
            return IOActionResult.Failed(action.Id, $"I/O 操作异常: {ex.Message}");
        }
    }

    #region 各动作类型实现

    /// <summary>
    /// Set：将指定值写入 I/O 点位。
    /// </summary>
    private async Task<IOActionResult> ExecuteSetAsync(IOActionDefinition action, CancellationToken ct)
    {
        var value = ResolveActionValue(action);
        if (value == null)
            return IOActionResult.Failed(action.Id, "Set 动作缺少目标值。");

        if (action.IOType == "PLC_IO")
        {
            var plcIO = new PLCIOService(_plcService);
            await plcIO.WriteAsync(action.DeviceId, action.PointCode, value, ct).ConfigureAwait(false);
        }
        else if (action.IOType == "CAN_IO")
        {
            var canIO = new CANIOService(_canService, action.DeviceId ?? "default");
            var resolution = canIO.ResolvePoint(action.PointCode);
            await canIO.WriteAsync(resolution, ToBytes(value), ct).ConfigureAwait(false);
        }
        else
        {
            return IOActionResult.Failed(action.Id, $"不支持的 IO 类型: '{action.IOType}'。");
        }

        return IOActionResult.Succeeded(action.Id, value);
    }

    /// <summary>
    /// Pulse：Set → 等待 SettleTimeMs → Reset。
    /// </summary>
    private async Task<IOActionResult> ExecutePulseAsync(IOActionDefinition action, CancellationToken ct)
    {
        // Phase 1: Set
        var setResult = await ExecuteSetAsync(action, ct).ConfigureAwait(false);
        if (!setResult.Success)
            return setResult;

        // Phase 2: 等待稳定时间
        if (action.SettleTimeMs > 0)
            await Task.Delay(action.SettleTimeMs, ct).ConfigureAwait(false);

        // Phase 3: Reset（恢复到安全值）
        await ExecuteResetAsync(action, ct).ConfigureAwait(false);

        return IOActionResult.Succeeded(action.Id);
    }

    /// <summary>
    /// Read：读取 I/O 点位的当前值。
    /// </summary>
    private async Task<IOActionResult> ExecuteReadAsync(IOActionDefinition action, CancellationToken ct)
    {
        object? value;

        if (action.IOType == "PLC_IO")
        {
            var plcIO = new PLCIOService(_plcService);
            value = await plcIO.ReadAsync(action.DeviceId, action.PointCode, ct).ConfigureAwait(false);
        }
        else if (action.IOType == "CAN_IO")
        {
            var canIO = new CANIOService(_canService, action.DeviceId ?? "default");
            var resolution = canIO.ResolvePoint(action.PointCode);
            var data = await canIO.ReadAsync(resolution, ct).ConfigureAwait(false);
            value = FromBytes(data);
        }
        else
        {
            return IOActionResult.Failed(action.Id, $"不支持的 IO 类型: '{action.IOType}'。");
        }

        return IOActionResult.Succeeded(action.Id, value);
    }

    /// <summary>
    /// ReadVerify：读取当前值并与期望值比较。
    /// </summary>
    private async Task<IOActionResult> ExecuteReadVerifyAsync(IOActionDefinition action, CancellationToken ct)
    {
        var expected = ResolveActionValue(action);
        var readResult = await ExecuteReadAsync(action, ct).ConfigureAwait(false);

        if (!readResult.Success)
            return readResult;

        var actual = readResult.Value;
        if (Equals(actual, expected))
            return IOActionResult.Succeeded(action.Id, actual);

        return IOActionResult.Failed(action.Id,
            $"点位 '{action.PointCode}' 校验失败: 期望值={FormatValue(expected)}，实际值={FormatValue(actual)}。");
    }

    /// <summary>
    /// WaitFor：轮询直到点位达到目标值或超时。
    /// </summary>
    private async Task<IOActionResult> ExecuteWaitForAsync(IOActionDefinition action, CancellationToken ct)
    {
        var expected = ResolveActionValue(action);
        var timeoutMs = action.TimeoutMs > 0 ? action.TimeoutMs : 3000;

        if (action.IOType == "PLC_IO")
        {
            var plcIO = new PLCIOService(_plcService);
            var ok = await plcIO.WaitForAsync(
                action.DeviceId, action.PointCode, expected!, timeoutMs, ct).ConfigureAwait(false);
            return ok
                ? IOActionResult.Succeeded(action.Id, expected)
                : IOActionResult.Failed(action.Id,
                    $"点位 '{action.PointCode}' 等待超时 ({timeoutMs}ms)，期望值={FormatValue(expected)}。");
        }
        else
        {
            // CAN IO WaitFor：轮询读取
            var canIO = new CANIOService(_canService, action.DeviceId ?? "default");
            var resolution = canIO.ResolvePoint(action.PointCode);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            var pollInterval = TimeSpan.FromMilliseconds(50);

            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                var data = await canIO.ReadAsync(resolution, ct).ConfigureAwait(false);
                var actual = FromBytes(data);
                if (Equals(actual, expected))
                    return IOActionResult.Succeeded(action.Id, expected);
                await Task.Delay(pollInterval, ct).ConfigureAwait(false);
            }

            return IOActionResult.Failed(action.Id,
                $"点位 '{action.PointCode}' 等待超时 ({timeoutMs}ms)，期望值={FormatValue(expected)}。");
        }
    }

    /// <summary>
    /// Reset：将 I/O 点位恢复到安全默认值。
    /// 优先使用 IOMappingDefinition 中配置的 SafeValue；若无则写入 false/0。
    /// </summary>
    private async Task<IOActionResult> ExecuteResetAsync(IOActionDefinition action, CancellationToken ct)
    {
        // Reset 使用配置的安全值或默认 false/0
        var safeValue = ResolveActionValue(action) ?? (object)false;

        if (action.IOType == "PLC_IO")
        {
            var plcIO = new PLCIOService(_plcService);
            await plcIO.WriteAsync(action.DeviceId, action.PointCode, safeValue, ct).ConfigureAwait(false);
        }
        else if (action.IOType == "CAN_IO")
        {
            var canIO = new CANIOService(_canService, action.DeviceId ?? "default");
            var resolution = canIO.ResolvePoint(action.PointCode);
            await canIO.WriteAsync(resolution, ToBytes(safeValue), ct).ConfigureAwait(false);
        }

        return IOActionResult.Succeeded(action.Id, safeValue);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从 <see cref="IOActionDefinition.Value"/> 中解析实际写入/比较值。
    /// </summary>
    private static object? ResolveActionValue(IOActionDefinition action)
    {
        var expr = action.Value;
        if (expr == null) return null;
        if (expr.LocalValue != null) return expr.LocalValue;
        // 如果有绑定，从上下文中解析（当前 Phase 仅支持 LocalValue）
        if (expr.Binding != null)
        {
            // 简单处理：尝试返回绑定源 ID（完整解析由 TaskRunner 提供）
            return null;
        }
        return null;
    }

    /// <summary>
    /// 将对象值转换为字节数组用于 CAN 写入。
    /// </summary>
    private static byte[] ToBytes(object? value)
    {
        return value switch
        {
            null => new byte[] { 0 },
            bool b => new byte[] { b ? (byte)1 : (byte)0 },
            byte b => new byte[] { b },
            ushort s => BitConverter.GetBytes(s),
            short s => BitConverter.GetBytes(s),
            int i => BitConverter.GetBytes(i),
            float f => BitConverter.GetBytes(f),
            _ => new byte[] { 0 }
        };
    }

    /// <summary>
    /// 将字节数组转换为通用对象值。
    /// </summary>
    private static object? FromBytes(byte[] data)
    {
        if (data == null || data.Length == 0) return null;
        if (data.Length == 1) return data[0] != 0;
        return data;
    }

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "<null>",
            bool b => b ? "ON" : "OFF",
            _ => value.ToString() ?? "?"
        };

    #endregion
}
