using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// IO 执行服务接口。将 <see cref="IOActionDefinition"/> 翻译为对底层
/// PLC/CAN IO 服务的调用，执行 Set/Pulse/Read/ReadVerify/WaitFor/Reset 六种动作。
/// </summary>
public interface IIOExecutionService
{
    /// <summary>
    /// 根据 <see cref="IOActionDefinition"/> 执行单个 I/O 动作。
    /// </summary>
    /// <param name="action">要执行的 I/O 动作定义。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含成功/失败状态和可选值的 I/O 动作执行结果。</returns>
    Task<IOActionResult> ExecuteAsync(IOActionDefinition action, CancellationToken ct);
}
