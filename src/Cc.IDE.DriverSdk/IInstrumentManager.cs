using System.Collections.Concurrent;
using System.Reflection;

namespace Cc.IDE.DriverSdk;

/// <summary>
/// 仪器驱动管理器接口。
/// 负责发现、加载和管理所有可用的仪器驱动程序。
/// </summary>
public interface IInstrumentManager
{
    /// <summary>
    /// 已发现的驱动数量。
    /// </summary>
    int DiscoveredDriverCount { get; }

    /// <summary>
    /// 获取所有已发现驱动的只读字典（Key = DriverId, Value = 驱动实例）。
    /// </summary>
    IReadOnlyDictionary<string, IInstrumentDriver> DiscoveredDrivers { get; }

    /// <summary>
    /// 从指定目录扫描并加载所有仪器驱动程序。
    /// 遍历目录中的 *.dll 文件，反射查找 IInstrumentDriver 实现。
    /// </summary>
    /// <param name="directoryPath">待扫描的驱动目录路径。默认 "Drivers/"。</param>
    /// <returns>成功加载的驱动数量。</returns>
    int LoadDriversFrom(string directoryPath);

    /// <summary>
    /// 根据 DriverId 获取指定的驱动实例。
    /// </summary>
    /// <param name="driverId">驱动的全局唯一标识符。</param>
    /// <returns>匹配的驱动实例；未找到时返回 <c>null</c>。</returns>
    IInstrumentDriver? GetDriver(string driverId);

    /// <summary>
    /// 获取指定驱动 ID 的能力列表。
    /// </summary>
    /// <param name="driverId">驱动标识符。</param>
    /// <returns>仪器能力只读列表；驱动未找到时返回空列表。</returns>
    IReadOnlyList<InstrumentCapability> GetCapabilities(string driverId);

    /// <summary>
    /// 使用给定的驱动和连接配置连接到仪器。
    /// </summary>
    /// <param name="driverId">驱动标识符。</param>
    /// <param name="connection">连接配置参数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">驱动未找到或已连接时抛出。</exception>
    Task ConnectInstrumentAsync(string driverId, DriverConnectionData connection, CancellationToken ct);

    /// <summary>
    /// 断开指定驱动对应的仪器连接。
    /// </summary>
    /// <param name="driverId">驱动标识符。</param>
    Task DisconnectInstrumentAsync(string driverId);

    /// <summary>
    /// 断开所有已连接的仪器。
    /// </summary>
    Task DisconnectAllAsync();

    /// <summary>
    /// 向指定驱动注册一个程序化创建的驱动实例（用于测试或内置驱动）。
    /// </summary>
    /// <param name="driver">待注册的驱动实例。</param>
    /// <exception cref="ArgumentException">当 DriverId 重复时抛出。</exception>
    void RegisterDriver(IInstrumentDriver driver);

    /// <summary>
    /// 获取所有已注册驱动的能力目录（扁平化的能力列表，含所属驱动信息）。
    /// </summary>
    /// <returns>所有已发现能力的只读列表。</returns>
    IReadOnlyList<CapabilityCatalogEntry> GetCapabilityCatalog();
}

/// <summary>
/// 能力目录条目，将能力与其所属驱动关联。
/// </summary>
public sealed class CapabilityCatalogEntry
{
    /// <summary>
    /// 所属驱动的唯一标识符。
    /// </summary>
    public string DriverId { get; set; } = string.Empty;

    /// <summary>
    /// 驱动的显示名称。
    /// </summary>
    public string DriverDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 驱动的设备类型。
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// 驱动的能力定义。
    /// </summary>
    public InstrumentCapability Capability { get; set; } = null!;
}
