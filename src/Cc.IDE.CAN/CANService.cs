using System.Collections.Concurrent;
using Cc.IDE.Communication;

namespace Cc.IDE.CAN;

/// <summary>
/// CAN 总线设备管理中央服务实现。
/// 管理所有 CAN 接口实例的连接池，提供统一的帧发送/接收、事件订阅和健康检查入口。
/// </summary>
/// <remarks>
/// <para>
/// 使用 <see cref="ConcurrentDictionary{TKey,TValue}"/> 保证线程安全的接口实例并发访问。
/// 事件订阅管理使用弱引用（<see cref="WeakReference{T}"/>）避免外部订阅者阻止 GC 回收。
/// </para>
/// </remarks>
public sealed class CANService : ICANService
{
    /// <summary>
    /// CAN 接口实例池，线程安全。
    /// </summary>
    private readonly ConcurrentDictionary<string, ICanInterface> _interfacePool = new();

    /// <summary>
    /// CAN 接口连接状态跟踪，线程安全。
    /// </summary>
    private readonly ConcurrentDictionary<string, CANInterfaceState> _interfaceStates = new();

    /// <summary>
    /// 帧接收事件订阅管理器。
    /// 每个接口名称对应一个订阅者列表，使用弱引用避免内存泄漏。
    /// </summary>
    private readonly ConcurrentDictionary<string, List<WeakReference<Action<CanFrame>>>> _frameSubscriptions = new();

    /// <summary>
    /// 用于同步事件订阅列表访问的对象锁。
    /// </summary>
    private readonly object _subscriptionLock = new();

    /// <summary>
    /// 当前已连接的 CAN 接口数量。
    /// </summary>
    public int ConnectedInterfaceCount => _interfacePool.Count;

    /// <summary>
    /// 获取指定名称的 CAN 接口是否已连接。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <returns>如果接口已连接则返回 <c>true</c>。</returns>
    public bool IsInterfaceConnected(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return false;

        return _interfacePool.TryGetValue(interfaceName, out var iface) && iface.IsConnected;
    }

    /// <summary>
    /// 连接指定名称的 CAN 接口。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="interfaceType">接口类型（"PCAN"、"SocketCAN"、"Kvaser"）。</param>
    /// <param name="ct">取消令牌，用于取消连接操作。</param>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/> 或 <paramref name="interfaceType"/> 为 <c>null</c> 或空白。</exception>
    /// <exception cref="InvalidOperationException">接口已存在连接。</exception>
    /// <exception cref="NotSupportedException">不支持的接口类型。</exception>
    public async Task ConnectInterfaceAsync(string interfaceName, string interfaceType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            throw new ArgumentNullException(nameof(interfaceName), "接口名称不能为空。");

        if (string.IsNullOrWhiteSpace(interfaceType))
            throw new ArgumentNullException(nameof(interfaceType), "接口类型不能为空。");

        // 检查是否已存在连接
        if (_interfacePool.ContainsKey(interfaceName))
            throw new InvalidOperationException($"CAN 接口 '{interfaceName}' 已连接，请先断开再重试。");

        // 创建接口实例
        var canInterface = CreateInterface(interfaceType);

        try
        {
            // 建立连接
            await canInterface.ConnectAsync(ct).ConfigureAwait(false);

            // 订阅底层帧接收事件
            canInterface.FrameReceived += HandleFrameReceived;

            // 存储到连接池
            _interfacePool[interfaceName] = canInterface;

            // 更新连接状态
            _interfaceStates[interfaceName] = new CANInterfaceState
            {
                InterfaceName = interfaceName,
                InterfaceType = interfaceType,
                IsConnected = true,
                ConnectedAt = DateTime.UtcNow,
            };
        }
        catch
        {
            // 连接失败时清理部分初始化状态
            (canInterface as IDisposable)?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 断开指定名称的 CAN 接口。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <remarks>如果接口不存在于连接池中，此方法不执行任何操作。</remarks>
    public async Task DisconnectInterfaceAsync(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return;

        if (_interfacePool.TryRemove(interfaceName, out var canInterface))
        {
            try
            {
                // 取消订阅底层事件
                canInterface.FrameReceived -= HandleFrameReceived;

                await canInterface.DisconnectAsync().ConfigureAwait(false);
            }
            finally
            {
                (canInterface as IDisposable)?.Dispose();
                _interfaceStates.TryRemove(interfaceName, out _);
                _frameSubscriptions.TryRemove(interfaceName, out _);
            }
        }
    }

    /// <summary>
    /// 断开所有已连接的 CAN 接口。
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        // 收集所有接口名称并分批断开
        var interfaceNames = _interfacePool.Keys.ToArray();
        var tasks = interfaceNames.Select(name => DisconnectInterfaceAsync(name));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// 向指定 CAN 接口发送帧。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="frame">要发送的 CAN 帧。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">接口未连接或不存在。</exception>
    public async Task SendFrameAsync(string interfaceName, CanFrame frame, CancellationToken ct)
    {
        var canInterface = GetInterfaceOrThrow(interfaceName);
        await canInterface.SendFrameAsync(frame, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 从指定 CAN 接口读取已接收的帧。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已接收的 CAN 帧只读列表。</returns>
    /// <exception cref="InvalidOperationException">接口未连接或不存在。</exception>
    public async Task<IReadOnlyList<CanFrame>> ReadAsync(string interfaceName, CancellationToken ct)
    {
        var canInterface = GetInterfaceOrThrow(interfaceName);
        return await canInterface.ReadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取所有 CAN 接口的连接状态快照。
    /// </summary>
    /// <returns>接口连接状态的只读字典。</returns>
    public IReadOnlyDictionary<string, CANInterfaceState> GetInterfaceStates()
    {
        return new Dictionary<string, CANInterfaceState>(_interfaceStates);
    }

    /// <summary>
    /// 根据接口类型创建对应的 CAN 接口实例。
    /// </summary>
    /// <param name="interfaceType">接口类型（"PCAN"、"SocketCAN"、"Kvaser"）。</param>
    /// <returns>对应的 <see cref="ICanInterface"/> 实例。</returns>
    /// <exception cref="NotSupportedException">不支持的接口类型。</exception>
    public ICanInterface CreateInterface(string interfaceType)
    {
        return interfaceType switch
        {
            "PCAN" => new PCANInterface(),
            // 未来支持更多 CAN 接口类型：
            // "SocketCAN" => new SocketCANInterface(),
            // "Kvaser"    => new KvaserInterface(),
            _ => throw new NotSupportedException($"不支持的 CAN 接口类型：'{interfaceType}'。当前支持的接口类型: PCAN。"),
        };
    }

    /// <summary>
    /// 订阅指定 CAN 接口的帧接收事件。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="handler">接收到帧时的回调。</param>
    /// <remarks>
    /// 订阅者列表使用 <see cref="WeakReference{T}"/> 包装，允许订阅者被 GC 回收而不会造成内存泄漏。
    /// 订阅前不会检查接口是否已连接，允许在连接前注册事件处理器。
    /// </remarks>
    public void SubscribeFrames(string interfaceName, Action<CanFrame> handler)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return;

        if (handler is null)
            return;

        var subscriptions = _frameSubscriptions.GetOrAdd(interfaceName, _ => new List<WeakReference<Action<CanFrame>>>());

        lock (_subscriptionLock)
        {
            // 清理已失效的弱引用
            subscriptions.RemoveAll(wr => !wr.TryGetTarget(out _));
            subscriptions.Add(new WeakReference<Action<CanFrame>>(handler));
        }
    }

    /// <summary>
    /// 取消订阅指定 CAN 接口的帧接收事件。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="handler">要移除的回调。</param>
    public void UnsubscribeFrames(string interfaceName, Action<CanFrame> handler)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            return;

        if (handler is null)
            return;

        if (!_frameSubscriptions.TryGetValue(interfaceName, out var subscriptions))
            return;

        lock (_subscriptionLock)
        {
            subscriptions.RemoveAll(wr =>
            {
                if (wr.TryGetTarget(out var target))
                    return target == handler;
                return true; // 移除已失效的引用
            });
        }
    }

    /// <summary>
    /// 处理底层 <see cref="ICanInterface.FrameReceived"/> 事件，转发给所有已订阅的外部处理器。
    /// </summary>
    /// <param name="frame">接收到的 CAN 帧。</param>
    private void HandleFrameReceived(CanFrame frame)
    {
        // 遍历所有接口名称下的订阅者，将帧分发给匹配的处理器
        foreach (var kvp in _frameSubscriptions)
        {
            var subscriptions = kvp.Value;
            List<WeakReference<Action<CanFrame>>>? deadRefs = null;

            lock (_subscriptionLock)
            {
                foreach (var wr in subscriptions)
                {
                    if (wr.TryGetTarget(out var handler))
                    {
                        try
                        {
                            handler.Invoke(frame);
                        }
                        catch
                        {
                            // 单个订阅者异常不应影响其他订阅者
                            // 实际生产环境中应注入 ILogger 记录异常
                        }
                    }
                    else
                    {
                        // 标记失效引用，延迟清理
                        deadRefs ??= new List<WeakReference<Action<CanFrame>>>();
                        deadRefs.Add(wr);
                    }
                }
            }

            // 清理已失效的弱引用
            if (deadRefs is not null)
            {
                lock (_subscriptionLock)
                {
                    foreach (var dead in deadRefs)
                        subscriptions.Remove(dead);
                }
            }
        }
    }

    /// <summary>
    /// 从连接池获取指定接口的 <see cref="ICanInterface"/> 实例，如果不存在则抛出异常。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <returns>CAN 接口实例。</returns>
    /// <exception cref="InvalidOperationException">接口未连接或不存在。</exception>
    private ICanInterface GetInterfaceOrThrow(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
            throw new ArgumentNullException(nameof(interfaceName), "接口名称不能为空。");

        if (!_interfacePool.TryGetValue(interfaceName, out var canInterface))
            throw new InvalidOperationException($"CAN 接口 '{interfaceName}' 未连接或不存在，请先调用 ConnectInterfaceAsync 建立连接。");

        if (!canInterface.IsConnected)
            throw new InvalidOperationException($"CAN 接口 '{interfaceName}' 的连接已断开。");

        return canInterface;
    }
}
