namespace Cc.IDE.Mvvm;

/// <summary>
/// A lightweight publish/subscribe event aggregator for loosely coupled
/// communication between ViewModels and services.
///
/// Subscribers are held with weak references to prevent memory leaks.
/// </summary>
public sealed class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<WeakReference>> _subscribers = new();
    private readonly object _lock = new();

    /// <summary>
    /// 发布指定类型的事件，通知所有订阅者。
    /// 订阅者通过弱引用持有，不会阻止垃圾回收。
    /// </summary>
    /// <typeparam name="TEvent">要发布的事件类型。</typeparam>
    /// <param name="event">要发布的事件数据。</param>
    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        List<WeakReference>? snapshot;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var list))
                return;
            snapshot = new List<WeakReference>(list);
        }

        foreach (var weakRef in snapshot)
        {
            if (weakRef.Target is Action<TEvent> handler)
            {
                handler(@event);
            }
        }

        // Prune dead weak references
        lock (_lock)
        {
            if (_subscribers.TryGetValue(typeof(TEvent), out var list))
            {
                list.RemoveAll(wr => !wr.IsAlive);
            }
        }
    }

    /// <summary>
    /// 订阅指定类型的事件。
    /// 订阅者以弱引用形式存储，不会阻止垃圾回收。
    /// </summary>
    /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
    /// <param name="handler">事件处理委托。</param>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var list))
            {
                list = new List<WeakReference>();
                _subscribers[typeof(TEvent)] = list;
            }

            list.Add(new WeakReference(handler));
        }
    }

    /// <summary>
    /// 取消订阅指定类型的事件处理委托。
    /// </summary>
    /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
    /// <param name="handler">要移除的事件处理委托。</param>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var list))
                return;

            list.RemoveAll(wr => wr.Target is Action<TEvent> h && h == handler);
        }
    }
}

/// <summary>
/// Lightweight publish/subscribe event aggregator interface.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// 发布指定类型的事件，通知所有订阅者。
    /// </summary>
    /// <typeparam name="TEvent">要发布的事件类型。</typeparam>
    /// <param name="event">要发布的事件数据。</param>
    void Publish<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// 订阅指定类型的事件。
    /// </summary>
    /// <typeparam name="TEvent">要订阅的事件类型。</typeparam>
    /// <param name="handler">事件处理委托。</param>
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

    /// <summary>
    /// 取消订阅指定类型的事件处理委托。
    /// </summary>
    /// <typeparam name="TEvent">要取消订阅的事件类型。</typeparam>
    /// <param name="handler">要移除的事件处理委托。</param>
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}
