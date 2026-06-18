using Cc.IDE.ProjectSystem.Models;

namespace Cc.IDE.Runtime;

/// <summary>
/// 测试记录持久化仓储接口。将测试结果写入本地数据库并支持历史查询。
/// </summary>
public interface ITestRecordRepository
{
    /// <summary>
    /// 保存一个测试结果并返回数据库 ID。
    /// </summary>
    /// <param name="result">测试结果。</param>
    /// <param name="reportFilePath">可选的外部报告文件路径。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>数据库中的记录 ID。</returns>
    Task<long> SaveAsync(TestResult result, string? reportFilePath, CancellationToken ct);

    /// <summary>
    /// 根据 ID 获取单条记录。
    /// </summary>
    /// <param name="id">记录的主键 ID。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>匹配的测试记录，若未找到则返回 null。</returns>
    Task<TestRecord?> GetByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// 按条件查询记录（按批次号、序列号、产品型号、时间范围、结果状态）。
    /// </summary>
    /// <param name="query">查询条件对象。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>符合条件的测试记录只读列表。</returns>
    Task<IReadOnlyList<TestRecord>> QueryAsync(TestRecordQuery query, CancellationToken ct);
}

/// <summary>
/// 测试记录持久化模型。对应 SQLite TestRecords 表中的一行。
/// </summary>
public sealed class TestRecord
{
    /// <summary>自增主键 ID。</summary>
    public long Id { get; set; }

    /// <summary>关联的任务定义 ID。</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>关联的任务名称。</summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>生产批次号，用于按批次追溯。</summary>
    public string? BatchId { get; set; }

    /// <summary>产品序列号，用于按设备追溯。</summary>
    public string? SerialNumber { get; set; }

    /// <summary>产品型号。</summary>
    public string? ProductModel { get; set; }

    /// <summary>执行状态（Passed | Failed | Error | Aborted | NotRun）。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>失败原因描述。</summary>
    public string? FailureReason { get; set; }

    /// <summary>任务开始执行的时间。</summary>
    public DateTime StartTime { get; set; }

    /// <summary>任务执行结束的时间。</summary>
    public DateTime EndTime { get; set; }

    /// <summary>执行耗时（毫秒）。</summary>
    public long DurationMs { get; set; }

    /// <summary>各步骤结果的 JSON 序列化字符串。</summary>
    public string? StepResultsJson { get; set; }

    /// <summary>外部报告文件的路径。</summary>
    public string? ReportFilePath { get; set; }

    /// <summary>记录创建时间。</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 测试记录查询条件。所有属性均为可选，组合使用以缩小范围。
/// </summary>
public sealed class TestRecordQuery
{
    /// <summary>按批次号筛选。</summary>
    public string? BatchId { get; set; }

    /// <summary>按产品序列号筛选。</summary>
    public string? SerialNumber { get; set; }

    /// <summary>按产品型号筛选。</summary>
    public string? ProductModel { get; set; }

    /// <summary>按执行状态筛选。</summary>
    public string? Status { get; set; }

    /// <summary>查询起始时间（记录创建时间）。</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>查询截止时间（记录创建时间）。</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>最大返回条数，默认 100。</summary>
    public int MaxResults { get; set; } = 100;
}
