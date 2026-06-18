namespace Cc.IDE.Mvvm;

/// <summary>
/// Base class for observable objects with INotifyPropertyChanged support.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// 当属性值发生更改时发生。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 引发指定属性的 PropertyChanged 事件。
    /// </summary>
    /// <param name="propertyName">属性名称（自动填充）。</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 设置属性值，并在值发生变化时自动引发 PropertyChanged 事件。
    /// </summary>
    /// <typeparam name="T">属性的类型。</typeparam>
    /// <param name="field">对后备字段的引用。</param>
    /// <param name="value">新的属性值。</param>
    /// <param name="propertyName">属性名称（自动填充）。</param>
    /// <returns>若值发生变更则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 设置属性值，并在值发生变化时自动引发 PropertyChanged 事件，同时执行指定的回调。
    /// </summary>
    /// <typeparam name="T">属性的类型。</typeparam>
    /// <param name="field">对后备字段的引用。</param>
    /// <param name="value">新的属性值。</param>
    /// <param name="onChanged">值发生变更时调用的回调，参数为旧值。</param>
    /// <param name="propertyName">属性名称（自动填充）。</param>
    /// <returns>若值发生变更则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
    protected bool SetProperty<T>(ref T field, T value, Action<T> onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        var oldValue = field;
        field = value;
        onChanged(oldValue);
        OnPropertyChanged(propertyName);
        return true;
    }
}
