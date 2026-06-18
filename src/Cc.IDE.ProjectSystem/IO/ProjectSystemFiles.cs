using System.Text.Json;
using Cc.IDE.ProjectSystem.Models;
using Cc.IDE.ProjectSystem.Serialization;

namespace Cc.IDE.ProjectSystem.IO;

/// <summary>
/// 提供所有项目系统文件类型的类型安全文件 I/O 门面。
/// 每个方法均使用绝对文件路径操作，并提供取消令牌支持。
/// </summary>
public static class ProjectSystemFiles
{
    // ─── 解决方案 (.yoursln) ─────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载解决方案定义文件 (.yoursln)。
    /// </summary>
    /// <param name="path">解决方案文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="SolutionDefinition"/> 实例。</returns>
    /// <exception cref="InvalidOperationException">当反序列化结果为空时抛出。</exception>
    public static async Task<SolutionDefinition> LoadSolutionAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<SolutionDefinition>(json, ProjectSystemJson.Options)
               ?? throw new InvalidOperationException("Deserialized solution is null.");
    }

    /// <summary>
    /// 将解决方案定义保存到指定路径 (.yoursln)，同时自动更新 <c>UpdatedAt</c> 时间戳。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="SolutionDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveSolutionAsync(string path, SolutionDefinition value, CancellationToken ct = default)
    {
        value.UpdatedAt = DateTime.UtcNow.ToString("O");
        var json = JsonSerializer.Serialize(value, ProjectSystemJson.Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    // ─── 工程 (.yoursproj) ────────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载工程定义文件 (.yoursproj)。
    /// </summary>
    /// <param name="path">工程文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="ProjectDefinition"/> 实例。</returns>
    /// <exception cref="InvalidOperationException">当反序列化结果为空时抛出。</exception>
    public static async Task<ProjectDefinition> LoadProjectAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<ProjectDefinition>(json, ProjectSystemJson.Options)
               ?? throw new InvalidOperationException("Deserialized project is null.");
    }

    /// <summary>
    /// 将工程定义保存到指定路径 (.yoursproj)。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="ProjectDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveProjectAsync(string path, ProjectDefinition value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, ProjectSystemJson.Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    // ─── 任务 (.yourtask) ─────────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载任务定义文件 (.yourtask)。
    /// </summary>
    /// <param name="path">任务文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="TaskDefinition"/> 实例。</returns>
    /// <exception cref="InvalidOperationException">当反序列化结果为空时抛出。</exception>
    public static async Task<TaskDefinition> LoadTaskAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<TaskDefinition>(json, ProjectSystemJson.Options)
               ?? throw new InvalidOperationException("Deserialized task is null.");
    }

    /// <summary>
    /// 将任务定义保存到指定路径 (.yourtask)。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="TaskDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveTaskAsync(string path, TaskDefinition value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, ProjectSystemJson.Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    // ─── 仪器 (.yourinst) ─────────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载仪器实例配置文件 (.yourinst)。
    /// </summary>
    /// <param name="path">仪器配置文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="InstrumentDefinition"/> 实例。</returns>
    /// <exception cref="InvalidOperationException">当反序列化结果为空时抛出。</exception>
    public static async Task<InstrumentDefinition> LoadInstrumentAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<InstrumentDefinition>(json, ProjectSystemJson.Options)
               ?? throw new InvalidOperationException("Deserialized instrument is null.");
    }

    /// <summary>
    /// 将仪器实例配置保存到指定路径 (.yourinst)。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="InstrumentDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveInstrumentAsync(string path, InstrumentDefinition value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, ProjectSystemJson.Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    // ─── IO 映射 (.yourmap) ───────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载 I/O 映射定义文件 (.yourmap)。
    /// </summary>
    /// <param name="path">I/O 映射文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="IOMappingDefinition"/> 实例。</returns>
    /// <exception cref="InvalidOperationException">当反序列化结果为空时抛出。</exception>
    public static async Task<IOMappingDefinition> LoadIOMappingAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<IOMappingDefinition>(json, ProjectSystemJson.Options)
               ?? throw new InvalidOperationException("Deserialized I/O mapping is null.");
    }

    /// <summary>
    /// 将 I/O 映射定义保存到指定路径 (.yourmap)。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="IOMappingDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveIOMappingAsync(string path, IOMappingDefinition value, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, ProjectSystemJson.Options);
        await File.WriteAllTextAsync(path, json, ct);
    }

    // ─── 模板 (.yourtpl) ──────────────────────────────────────────────────

    /// <summary>
    /// 从指定路径加载任务模板文件 (.yourtpl)。模板本质上是参数化的 Task 定义。
    /// </summary>
    /// <param name="path">模板文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>反序列化后的 <see cref="TaskDefinition"/> 实例。</returns>
    public static async Task<TaskDefinition> LoadTemplateAsync(string path, CancellationToken ct = default)
    {
        return await LoadTaskAsync(path, ct);
    }

    /// <summary>
    /// 将任务模板保存到指定路径 (.yourtpl)。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="value">待保存的 <see cref="TaskDefinition"/> 实例。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task SaveTemplateAsync(string path, TaskDefinition value, CancellationToken ct = default)
    {
        await SaveTaskAsync(path, value, ct);
    }

    // ─── 通用辅助方法 ─────────────────────────────────────────────────────

    /// <summary>
    /// 从文件加载任意项目系统实体，根据文件扩展名自动检测类型。
    /// </summary>
    /// <param name="path">待加载文件的绝对路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>根据扩展名返回对应的领域模型实例。</returns>
    /// <exception cref="NotSupportedException">当文件扩展名不受支持时抛出。</exception>
    public static async Task<object> LoadAnyAsync(string path, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".yoursln" => await LoadSolutionAsync(path, ct),
            ".yoursproj" => await LoadProjectAsync(path, ct),
            ".yourtask" => await LoadTaskAsync(path, ct),
            ".yourinst" => await LoadInstrumentAsync(path, ct),
            ".yourmap" => await LoadIOMappingAsync(path, ct),
            ".yourtpl" => await LoadTemplateAsync(path, ct),
            _ => throw new NotSupportedException($"Unknown project system file extension: '{ext}'")
        };
    }
}
