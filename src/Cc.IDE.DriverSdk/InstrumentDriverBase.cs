using Cc.IDE.Communication;

namespace Cc.IDE.DriverSdk;

/// <summary>
/// 仪器驱动的抽象基类，为 <see cref="IInstrumentDriver"/> 的大部分成员提供默认实现，
/// 使派生类只需专注于特定型号的逻辑（能力声明、命令执行、标识查询、重置和自检）。
/// </summary>
/// <remarks>
/// <para>子类必须至少重写以下成员：</para>
/// <list type="bullet">
///   <item><see cref="GetCapabilities"/> — 返回仪器的能力列表。</item>
///   <item><see cref="ExecuteAsync"/> — 将功能 ID 映射到 SCPI/命令执行。</item>
///   <item><see cref="GetIdentificationAsync"/> — 发送 ID 查询命令（通常为 *IDN?）。</item>
///   <item><see cref="ResetAsync"/> — 发送重置命令（通常为 *RST）。</item>
///   <item><see cref="SelfTestAsync"/> — 发送自检命令（通常为 *TST?）。</item>
/// </list>
/// <para>虚钩子方法（<see cref="OnConnectedAsync"/>、<see cref="OnDisconnectingAsync"/>）
/// 允许子类添加初始化或清理逻辑，而无需重写完整的连接/断开流程。</para>
/// </remarks>
public abstract class InstrumentDriverBase : IInstrumentDriver
{
    // ── 内部字段 ──────────────────────────────────────────────────────

    /// <summary>
    /// 用于与仪器通信的传输层。在 <see cref="ConnectAsync"/> 中设置，在 <see cref="DisconnectAsync"/> 中清除。
    /// </summary>
    protected ICommunicationTransport? Transport { get; private set; }

    /// <summary>
    /// 上次成功 <see cref="ConnectAsync"/> 调用时传入的连接数据。供钩子方法和子类使用。
    /// </summary>
    protected DriverConnectionData? ConnectionData { get; private set; }

    /// <summary>
    /// 上次成功通信的时间戳，由 <see cref="GetHealthAsync"/> 使用。
    /// </summary>
    protected DateTime? LastSuccessfulCommunication { get; private set; }

    /// <summary>
    /// 连续通信失败的计数器，由 <see cref="GetHealthAsync"/> 使用。
    /// </summary>
    protected int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// 最近成功操作耗时（毫秒）的滚动列表。
    /// </summary>
    private readonly List<long> _recentDurations = new();

    private const int MaxRecentDurations = 20;

    // ── 标识信息（抽象） ───────────────────────────────────────────────

    /// <inheritdoc />
    public abstract string DriverId { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public abstract string Version { get; }

    /// <inheritdoc />
    public abstract string DeviceType { get; }

    /// <inheritdoc />
    public abstract string Manufacturer { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedTransports { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<DriverDependency> Dependencies { get; }

    // ── 连接状态 ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public bool IsConnected => Transport is { IsConnected: true };

    // ── 生命周期 ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual async Task ConnectAsync(DriverConnectionData connection, CancellationToken ct)
    {
        if (IsConnected)
            throw new InvalidOperationException("Driver is already connected. Disconnect first.");

        // Create the transport via the Communication factory
        Transport = CommunicationFactory.Create(connection.Protocol, connection);
        await Transport.ConnectAsync(ct);

        ConnectionData = connection;
        LastSuccessfulCommunication = DateTime.UtcNow;
        ConsecutiveFailures = 0;

        // Allow derived classes to perform init after transport is ready
        await OnConnectedAsync(ct);
    }

    /// <inheritdoc />
    public virtual async Task DisconnectAsync()
    {
        if (Transport is null)
            return;

        // Allow derived classes to clean up before tearing down transport
        await OnDisconnectingAsync();

        Transport.Dispose();
        Transport = null;
        ConnectionData = null;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        Transport?.Dispose();
        Transport = null;
        GC.SuppressFinalize(this);
    }

    // ── 能力发现（抽象） ───────────────────────────────────────────────

    /// <inheritdoc />
    public abstract IReadOnlyList<InstrumentCapability> GetCapabilities();

    // ── 命令执行（抽象） ───────────────────────────────────────────────

    /// <inheritdoc />
    public abstract Task<InstrumentResult> ExecuteAsync(
        string functionId,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct);

    // ── 健康状态 ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual async Task<InstrumentHealth> GetHealthAsync()
    {
        var health = new InstrumentHealth
        {
            IsConnected = IsConnected,
            Status = IsConnected ? "Healthy" : "Disconnected",
            LastError = ConsecutiveFailures > 0 ? await QueryErrorAsync() : null,
            LastSuccessfulCommunication = LastSuccessfulCommunication,
            ConsecutiveFailures = ConsecutiveFailures,
            AverageResponseMs = CalculateAverageResponseMs()
        };

        if (ConsecutiveFailures > 3)
            health.Status = "Degraded";
        if (!IsConnected)
            health.Status = "Disconnected";

        return health;
    }

    // ── 标识查询（抽象） ───────────────────────────────────────────────

    /// <inheritdoc />
    public abstract Task<string> GetIdentificationAsync();

    // ── 重置（抽象） ──────────────────────────────────────────────────

    /// <inheritdoc />
    public abstract Task ResetAsync();

    // ── 自检（抽象） ──────────────────────────────────────────────────

    /// <inheritdoc />
    public abstract Task<InstrumentSelfTestResult> SelfTestAsync();

    // ── Hook methods (virtual) ──────────────────────────────────────────

    /// <summary>
    /// 在传输连接建立后、<see cref="ConnectAsync"/> 返回前调用。
    /// 重写此方法可执行驱动特定的初始化（如清除状态、设置默认值等）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    protected virtual Task OnConnectedAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// 在传输断开连接前调用。重写此方法可执行清理操作或发送最后的命令。
    /// </summary>
    protected virtual Task OnDisconnectingAsync() => Task.CompletedTask;

    // ── 错误查询（虚方法） ────────────────────────────────────────────

    /// <summary>
    /// 查询仪器的错误队列。默认实现发送 "*ESR?" 或等效命令；
    /// 重写此方法可实现特定仪器的错误查询逻辑。
    /// </summary>
    /// <returns>仪器的错误消息；未连接时返回 <c>null</c>。</returns>
    public virtual async Task<string?> QueryErrorAsync()
    {
        if (Transport is not { IsConnected: true })
            return null;

        try
        {
            return await Transport.QueryAsync("SYST:ERR?", CancellationToken.None);
        }
        catch
        {
            return "Unable to query instrument error";
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// 记录一次成功操作的耗时，用于健康追踪。
    /// 在 <see cref="ExecuteAsync"/> 中命令成功执行后调用。
    /// </summary>
    /// <param name="elapsedMs">操作耗时（毫秒）。</param>
    protected void RecordSuccess(long elapsedMs)
    {
        LastSuccessfulCommunication = DateTime.UtcNow;
        ConsecutiveFailures = 0;

        _recentDurations.Add(elapsedMs);
        if (_recentDurations.Count > MaxRecentDurations)
            _recentDurations.RemoveAt(0);
    }

    /// <summary>
    /// 记录一次失败操作，用于健康追踪。
    /// 在 <see cref="ExecuteAsync"/> 中命令执行失败时调用。
    /// </summary>
    protected void RecordFailure()
    {
        ConsecutiveFailures++;
    }

    /// <summary>
    /// 根据最近成功的操作计算平均响应时间。
    /// </summary>
    /// <returns>平均响应时间（毫秒）；若无数据则返回 0。</returns>
    private double CalculateAverageResponseMs()
    {
        if (_recentDurations.Count == 0)
            return 0;
        return _recentDurations.Average();
    }
}
