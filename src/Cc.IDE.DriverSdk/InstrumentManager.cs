using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Cc.IDE.DriverSdk;

/// <summary>
/// 仪器驱动管理器的默认实现。
/// 负责从指定目录扫描 DLL 文件、反射加载 <see cref="IInstrumentDriver"/> 实现、
/// 管理驱动实例的生命周期，并提供能力目录的聚合查询。
/// </summary>
public sealed class InstrumentManager : IInstrumentManager
{
    // ── 存储字段 ──────────────────────────────────────────────────────────

    private readonly ConcurrentDictionary<string, IInstrumentDriver> _drivers = new();
    private readonly ConcurrentBag<CapabilityCatalogEntry> _capabilityCatalog = new();
    private readonly ILogger<InstrumentManager>? _logger;

    // ── 公开属性 ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public int DiscoveredDriverCount => _drivers.Count;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IInstrumentDriver> DiscoveredDrivers
        => new Dictionary<string, IInstrumentDriver>(_drivers);

    // ── 构造方法 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化 <see cref="InstrumentManager"/> 的新实例。
    /// </summary>
    /// <param name="logger">可选的日志记录器实例。</param>
    public InstrumentManager(ILogger<InstrumentManager>? logger = null)
    {
        _logger = logger;
    }

    // ── 驱动加载 ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="directoryPath"/> 为 <c>null</c> 时抛出。</exception>
    public int LoadDriversFrom(string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            _logger?.LogWarning("驱动目录不存在，跳过扫描：{Directory}", directoryPath);
            return 0;
        }

        var dllFiles = Directory.GetFiles(directoryPath, "*Driver*.dll", SearchOption.TopDirectoryOnly);

        if (dllFiles.Length == 0)
        {
            _logger?.LogInformation("在目录 {Directory} 中未找到匹配 *Driver*.dll 的文件。", directoryPath);
            return 0;
        }

        var loadedCount = 0;

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);

                var driverTypes = assembly.GetExportedTypes()
                    .Where(t => !t.IsAbstract && typeof(IInstrumentDriver).IsAssignableFrom(t))
                    .ToList();

                if (driverTypes.Count == 0)
                {
                    _logger?.LogDebug("程序集 {Assembly} 中未找到 IInstrumentDriver 实现。", assembly.GetName().Name);
                    continue;
                }

                foreach (var type in driverTypes)
                {
                    try
                    {
                        var driver = (IInstrumentDriver)Activator.CreateInstance(type)!;
                        RegisterDriver(driver);
                        loadedCount++;
                    }
                    catch (Exception innerEx)
                    {
                        _logger?.LogError(innerEx,
                            "实例化驱动类型 {Type} 失败，来自程序集 {Assembly}。",
                            type.FullName, dllPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载驱动程序集失败：{DllPath}", dllPath);
                // 继续尝试下一个 DLL
            }
        }

        _logger?.LogInformation("驱动加载完成。成功加载 {Count} 个驱动，总计扫描 {Total} 个 DLL 文件。",
            loadedCount, dllFiles.Length);

        return loadedCount;
    }

    // ── 驱动注册 ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="driver"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">
    /// 当 <paramref name="driver"/> 的 <see cref="IInstrumentDriver.DriverId"/> 为空或空白时抛出；
    /// 或者当 DriverId 在已注册驱动中重复时抛出。
    /// </exception>
    public void RegisterDriver(IInstrumentDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        if (string.IsNullOrWhiteSpace(driver.DriverId))
        {
            throw new ArgumentException(
                $"驱动的 {nameof(IInstrumentDriver.DriverId)} 不能为空。",
                nameof(driver));
        }

        if (!_drivers.TryAdd(driver.DriverId, driver))
        {
            throw new ArgumentException(
                $"DriverId \"{driver.DriverId}\" 已注册。每个 DriverId 必须是唯一的。",
                nameof(driver));
        }

        _logger?.LogInformation("驱动已注册：{DriverId} ({DisplayName})", driver.DriverId, driver.DisplayName);

        // 填充能力目录
        try
        {
            var capabilities = driver.GetCapabilities();
            foreach (var cap in capabilities)
            {
                _capabilityCatalog.Add(new CapabilityCatalogEntry
                {
                    DriverId = driver.DriverId,
                    DriverDisplayName = driver.DisplayName,
                    DeviceType = driver.DeviceType,
                    Capability = cap
                });
            }

            _logger?.LogDebug(
                "驱动 {DriverId} 贡献了 {Count} 个能力到目录。",
                driver.DriverId, capabilities.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "获取驱动 {DriverId} 的能力列表时发生异常。",
                driver.DriverId);
        }
    }

    // ── 驱动查询 ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public IInstrumentDriver? GetDriver(string driverId)
    {
        _drivers.TryGetValue(driverId, out var driver);
        return driver;
    }

    /// <inheritdoc />
    public IReadOnlyList<InstrumentCapability> GetCapabilities(string driverId)
    {
        if (_drivers.TryGetValue(driverId, out var driver))
        {
            try
            {
                return driver.GetCapabilities();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "获取驱动 {DriverId} 的能力列表时发生异常。返回空列表。",
                    driverId);
            }
        }

        return Array.Empty<InstrumentCapability>();
    }

    /// <inheritdoc />
    public IReadOnlyList<CapabilityCatalogEntry> GetCapabilityCatalog()
    {
        return _capabilityCatalog.ToArray();
    }

    // ── 连接管理 ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">驱动未找到时抛出。</exception>
    public async Task ConnectInstrumentAsync(
        string driverId,
        DriverConnectionData connection,
        CancellationToken ct)
    {
        if (!_drivers.TryGetValue(driverId, out var driver))
        {
            throw new InvalidOperationException(
                $"DriverId \"{driverId}\" 未注册，无法连接。");
        }

        _logger?.LogInformation("正在连接驱动 {DriverId}...", driverId);
        await driver.ConnectAsync(connection, ct);
        _logger?.LogInformation("驱动 {DriverId} 连接成功。", driverId);
    }

    /// <inheritdoc />
    public async Task DisconnectInstrumentAsync(string driverId)
    {
        if (_drivers.TryGetValue(driverId, out var driver))
        {
            if (driver.IsConnected)
            {
                _logger?.LogInformation("正在断开驱动 {DriverId}...", driverId);
                await driver.DisconnectAsync();
                _logger?.LogInformation("驱动 {DriverId} 已断开。", driverId);
            }
            else
            {
                _logger?.LogDebug("驱动 {DriverId} 未连接，跳过断开操作。", driverId);
            }
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAllAsync()
    {
        _logger?.LogInformation("正在断开所有已连接的仪器...");

        var connectedDrivers = _drivers.Values
            .Where(d => d.IsConnected)
            .ToList();

        foreach (var driver in connectedDrivers)
        {
            try
            {
                await driver.DisconnectAsync();
                _logger?.LogDebug("驱动 {DriverId} 已断开。", driver.DriverId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "断开驱动 {DriverId} 时发生异常。",
                    driver.DriverId);
                // 继续断开下一个驱动
            }
        }

        _logger?.LogInformation("已断开 {Count} 个驱动连接。", connectedDrivers.Count);
    }
}
