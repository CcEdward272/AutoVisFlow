using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Serialization;

/// <summary>
/// 所有项目系统文件的集中式 JSON 序列化配置。
/// 提供统一的 <see cref="JsonSerializerOptions"/> 实例与便捷的序列化/反序列化方法。
/// </summary>
public static class ProjectSystemJson
{
    /// <summary>
    /// 预配置的 JSON 序列化选项，采用 camelCase 命名策略、
    /// 缩进输出、忽略 null 值、宽松 Unicode 编码及字符串枚举转换器。
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// 基于默认选项创建一份克隆，并追加额外的自定义转换器。
    /// </summary>
    /// <param name="extraConverters">待追加到克隆选项的额外 JSON 转换器。</param>
    /// <returns>包含额外转换器的新 <see cref="JsonSerializerOptions"/> 实例。</returns>
    public static JsonSerializerOptions CreateOptions(params JsonConverter[] extraConverters)
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        foreach (var c in extraConverters)
            opts.Converters.Add(c);
        return opts;
    }

    /// <summary>
    /// 将指定值序列化为缩进格式的 JSON 字符串。
    /// </summary>
    /// <typeparam name="T">待序列化的值的类型。</typeparam>
    /// <param name="value">待序列化的值。</param>
    /// <returns>缩进格式的 JSON 字符串。</returns>
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// 将 JSON 字符串反序列化为指定类型的实例。
    /// </summary>
    /// <typeparam name="T">目标反序列化类型。</typeparam>
    /// <param name="json">JSON 源字符串。</param>
    /// <returns>反序列化后的类型实例；若 JSON 为 null 则返回 default。</returns>
    public static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options);
}
