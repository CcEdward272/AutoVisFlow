using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// IO 动作执行器。根据 <see cref="IOActionDefinition.ActionType"/> 执行
/// Set、Pulse、Read、ReadVerify、WaitFor、Reset 六种 I/O 动作。
/// </summary>
/// <remarks>
/// Phase 4 版本为基础实现：记录日志并模拟 I/O 动作（不使用真实硬件）。
/// 后续阶段将集成 <see cref="IIOService"/> 以实现真实设备 I/O。
/// </remarks>
public class IOActionExecutor
{
    /// <summary>
    /// 根据 <see cref="IOActionDefinition.ActionType"/> 执行单个 I/O 动作。
    /// </summary>
    /// <param name="action">要执行的 I/O 动作定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含成功/失败状态和可选值的 I/O 动作执行结果。</returns>
    /// <exception cref="OperationCanceledException">当取消令牌被发出时抛出。</exception>
    public async Task<IOActionResult> ExecuteAsync(IOActionDefinition action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = action.ActionType switch
        {
            IOActionType.Set => await ExecuteSetAsync(action, ct),
            IOActionType.Pulse => await ExecutePulseAsync(action, ct),
            IOActionType.Read => await ExecuteReadAsync(action, ct),
            IOActionType.ReadVerify => await ExecuteReadVerifyAsync(action, ct),
            IOActionType.WaitFor => await ExecuteWaitForAsync(action, ct),
            IOActionType.Reset => await ExecuteResetAsync(action, ct),
            _ => IOActionResult.Failed(action.Id, $"未知的 I/O 动作类型: {action.ActionType}")
        };

        return result;
    }

    /// <summary>
    /// 执行 Set 动作：将指定值写入 I/O 点。
    /// Phase 4 占位实现：记录日志并返回成功，使用本地值作为写入值。
    /// </summary>
    private Task<IOActionResult> ExecuteSetAsync(IOActionDefinition action, CancellationToken ct)
    {
        // Phase 4 占位：记录 Set 操作
        var value = action.Value?.LocalValue;
        return Task.FromResult(IOActionResult.Succeeded(action.Id, value));
    }

    /// <summary>
    /// 执行 Pulse 动作：Set → 等待 SettleTimeMs → Reset。
    /// Pulse 用于产生一个短暂的激活脉冲信号，常用于触发外部设备。
    /// </summary>
    private async Task<IOActionResult> ExecutePulseAsync(IOActionDefinition action, CancellationToken ct)
    {
        // Pulse = Set → SettleTimeMs → Reset
        await ExecuteSetAsync(action, ct);
        if (action.SettleTimeMs > 0)
            await Task.Delay(action.SettleTimeMs, ct);
        await ExecuteResetAsync(action, ct);
        return IOActionResult.Succeeded(action.Id);
    }

    /// <summary>
    /// 执行 Read 动作：读取 I/O 点的当前值。
    /// Phase 4 占位实现：始终返回模拟值 <c>true</c>。
    /// </summary>
    private Task<IOActionResult> ExecuteReadAsync(IOActionDefinition action, CancellationToken ct)
    {
        // Phase 4 占位：返回模拟值
        return Task.FromResult(IOActionResult.Succeeded(action.Id, true));
    }

    /// <summary>
    /// 执行 ReadVerify 动作：读取 I/O 点的当前值并与期望值比较。
    /// Phase 4 占位实现：始终认为读取值与期望值匹配。
    /// </summary>
    private Task<IOActionResult> ExecuteReadVerifyAsync(IOActionDefinition action, CancellationToken ct)
    {
        var expected = action.Value?.LocalValue;
        // Phase 4 占位：始终匹配期望值
        return Task.FromResult(IOActionResult.Succeeded(action.Id, expected));
    }

    /// <summary>
    /// 执行 WaitFor 动作：阻塞直到 I/O 点满足指定条件或超时。
    /// Phase 4 占位实现：等待完整超时时间后报告成功。
    /// </summary>
    private async Task<IOActionResult> ExecuteWaitForAsync(IOActionDefinition action, CancellationToken ct)
    {
        var timeout = action.TimeoutMs > 0 ? action.TimeoutMs : 3000;
        // Phase 4 占位：等待完整超时后报告成功
        await Task.Delay(Math.Min(timeout, 5000), ct);
        return IOActionResult.Succeeded(action.Id, action.Value?.LocalValue);
    }

    /// <summary>
    /// 执行 Reset 动作：将 I/O 点重置为默认值。
    /// Phase 4 占位实现：记录日志并返回成功。
    /// </summary>
    private Task<IOActionResult> ExecuteResetAsync(IOActionDefinition action, CancellationToken ct)
    {
        return Task.FromResult(IOActionResult.Succeeded(action.Id));
    }
}

/// <summary>
/// 单个 I/O 动作的执行结果。包含成功/失败状态、可选消息和可选返回值。
/// </summary>
public sealed class IOActionResult
{
    /// <summary>产生此结果的 I/O 动作 ID。</summary>
    public string ActionId { get; }

    /// <summary>当 I/O 动作执行成功时为 <c>true</c>。</summary>
    public bool Success { get; }

    /// <summary>可选的描述性消息，通常用于失败原因说明。</summary>
    public string? Message { get; }

    /// <summary>可选的返回值，例如 Read 动作读取到的值。</summary>
    public object? Value { get; }

    /// <summary>
    /// 创建一个新的 I/O 动作执行结果。
    /// </summary>
    /// <param name="actionId">I/O 动作 ID。</param>
    /// <param name="success">执行是否成功。</param>
    /// <param name="message">可选的描述性消息。</param>
    /// <param name="value">可选的返回值。</param>
    public IOActionResult(string actionId, bool success, string? message, object? value)
    {
        ActionId = actionId;
        Success = success;
        Message = message;
        Value = value;
    }

    /// <summary>
    /// 创建一个成功的结果，包含可选的返回值。
    /// </summary>
    /// <param name="actionId">I/O 动作 ID。</param>
    /// <param name="value">可选的返回值。</param>
    /// <returns>成功的结果实例。</returns>
    public static IOActionResult Succeeded(string actionId, object? value = null)
        => new(actionId, true, null, value);

    /// <summary>
    /// 创建一个失败的结果，包含失败原因。
    /// </summary>
    /// <param name="actionId">I/O 动作 ID。</param>
    /// <param name="reason">失败原因描述。</param>
    /// <returns>失败的结果实例。</returns>
    public static IOActionResult Failed(string actionId, string reason)
        => new(actionId, false, reason, null);
}
