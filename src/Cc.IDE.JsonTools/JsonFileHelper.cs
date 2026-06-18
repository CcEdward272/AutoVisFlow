using System.Text.Json;

namespace Cc.IDE.JsonTools;

/// <summary>
/// Convenience helpers for reading and writing JSON files using System.Text.Json.
/// </summary>
public static class JsonFileHelper
{
    /// <summary>
    /// 默认的 JSON 序列化选项：缩进输出、驼峰命名、忽略 null 值。
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 从 JSON 文件读取并反序列化为指定类型的对象。
    /// </summary>
    /// <typeparam name="T">目标反序列化类型。</typeparam>
    /// <param name="path">JSON 文件的绝对路径。</param>
    /// <param name="options">可选的 JSON 序列化选项；为 null 时使用 <see cref="DefaultOptions"/>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的对象实例；如果文件为空则返回 default。</returns>
    public static async Task<T?> ReadAsync<T>(string path, JsonSerializerOptions? options = null, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    /// <summary>
    /// 将对象序列化为 JSON 并写入文件。
    /// </summary>
    /// <typeparam name="T">要序列化的对象类型。</typeparam>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">要序列化的对象。</param>
    /// <param name="options">可选的 JSON 序列化选项；为 null 时使用 <see cref="DefaultOptions"/>。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task WriteAsync<T>(string path, T value, JsonSerializerOptions? options = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, options ?? DefaultOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    /// <summary>
    /// 尝试读取 JSON 文件，失败时返回 default 而非抛出异常。
    /// </summary>
    /// <typeparam name="T">目标反序列化类型。</typeparam>
    /// <param name="path">JSON 文件的绝对路径。</param>
    /// <param name="options">可选的 JSON 序列化选项；为 null 时使用 <see cref="DefaultOptions"/>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的对象实例；读取失败时返回 default。</returns>
    public static async Task<T?> TryReadAsync<T>(string path, JsonSerializerOptions? options = null, CancellationToken ct = default)
    {
        try
        {
            return await ReadAsync<T>(path, options, ct);
        }
        catch
        {
            return default;
        }
    }
}
