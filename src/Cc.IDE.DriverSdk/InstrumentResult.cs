namespace Cc.IDE.DriverSdk;

/// <summary>
/// 仪器 <see cref="IInstrumentDriver.ExecuteAsync"/> 调用的执行结果。
/// 封装成功/失败状态、输出值、计时和异常详情。
/// </summary>
public sealed class InstrumentResult
{
    /// <summary>
    /// 操作成功完成时为 <c>true</c>；否则为 <c>false</c>。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 高层次的状态分类："Success"（成功）、"Error"（错误）、"Timeout"（超时）或 "Cancelled"（已取消）。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 人类可读的结果或错误描述消息。
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 仪器操作产生的命名输出值字典。
    /// </summary>
    public Dictionary<string, object?> Outputs { get; set; } = new();

    /// <summary>
    /// 仪器返回的原始响应字符串（如适用）。
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// 操作总耗时（毫秒）。
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 失败操作期间捕获的异常（如有）。
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 创建成功结果，包含可选的输出值和原始响应。
    /// </summary>
    /// <param name="outputs">操作产生的命名输出值。</param>
    /// <param name="rawResponse">来自仪器的原始响应字符串。</param>
    /// <returns>表示成功操作的结果实例。</returns>
    public static InstrumentResult SuccessResult(Dictionary<string, object?>? outputs = null, string? rawResponse = null)
        => new()
        {
            Success = true,
            Status = "Success",
            Outputs = outputs ?? new(),
            RawResponse = rawResponse
        };

    /// <summary>
    /// 创建错误结果，包含错误消息和可选的异常。
    /// </summary>
    /// <param name="message">人类可读的错误描述。</param>
    /// <param name="ex">导致错误的可选异常。</param>
    /// <returns>表示错误操作的结果实例。</returns>
    public static InstrumentResult Error(string message, Exception? ex = null)
        => new()
        {
            Success = false,
            Status = "Error",
            Message = message,
            Exception = ex
        };

    /// <summary>
    /// 创建超时结果。
    /// </summary>
    /// <param name="message">可选的超时描述消息。</param>
    /// <returns>表示操作超时的结果实例。</returns>
    public static InstrumentResult Timeout(string message = "操作超时。")
        => new()
        {
            Success = false,
            Status = "Timeout",
            Message = message
        };

    /// <summary>
    /// 创建取消结果。
    /// </summary>
    /// <returns>表示操作被取消的结果实例。</returns>
    public static InstrumentResult Cancelled()
        => new()
        {
            Success = false,
            Status = "Cancelled",
            Message = "操作已被取消。"
        };
}
