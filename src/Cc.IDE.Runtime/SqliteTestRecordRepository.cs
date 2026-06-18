using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Cc.IDE.Runtime;

/// <summary>
/// 基于 SQLite 的测试记录持久化仓储实现。
/// 使用 Microsoft.Data.Sqlite 读写本地 .db 文件。
/// </summary>
public sealed class SqliteTestRecordRepository : ITestRecordRepository, IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _initialized;

    /// <summary>
    /// 创建 SQLite 仓储实例。
    /// </summary>
    /// <param name="databasePath">SQLite 数据库文件路径。</param>
    public SqliteTestRecordRepository(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
        _connection = new SqliteConnection(connectionString);
    }

    /// <summary>
    /// 确保数据库表及索引已创建。
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        await _connection.OpenAsync();
        const string sql = @"
            CREATE TABLE IF NOT EXISTS TestRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId TEXT NOT NULL,
                TaskName TEXT NOT NULL,
                BatchId TEXT,
                SerialNumber TEXT,
                ProductModel TEXT,
                Status TEXT NOT NULL,
                FailureReason TEXT,
                StartTime TEXT NOT NULL,
                EndTime TEXT NOT NULL,
                DurationMs INTEGER NOT NULL,
                StepResultsJson TEXT,
                ReportFilePath TEXT,
                CreatedAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_records_serial ON TestRecords(SerialNumber);
            CREATE INDEX IF NOT EXISTS idx_records_batch ON TestRecords(BatchId);
            CREATE INDEX IF NOT EXISTS idx_records_status ON TestRecords(Status);
            CREATE INDEX IF NOT EXISTS idx_records_created ON TestRecords(CreatedAt);
        ";
        using var cmd = new SqliteCommand(sql, _connection);
        await cmd.ExecuteNonQueryAsync();
        _initialized = true;
    }

    /// <inheritdoc />
    public async Task<long> SaveAsync(TestResult result, string? reportFilePath, CancellationToken ct)
    {
        await EnsureInitializedAsync();
        var stepJson = JsonSerializer.Serialize(result.StepResults);
        var sql = @"INSERT INTO TestRecords
            (TaskId, TaskName, BatchId, SerialNumber, ProductModel, Status,
             FailureReason, StartTime, EndTime, DurationMs, StepResultsJson, ReportFilePath, CreatedAt)
            VALUES (@TaskId, @TaskName, @BatchId, @SerialNumber, @ProductModel, @Status,
                    @FailureReason, @StartTime, @EndTime, @DurationMs, @Steps, @ReportPath, @CreatedAt);
            SELECT last_insert_rowid();";
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@TaskId", result.TaskId);
        cmd.Parameters.AddWithValue("@TaskName", result.TaskName);
        cmd.Parameters.AddWithValue("@BatchId", result.Outputs.TryGetValue("BatchId", out var b) ? b?.ToString() : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SerialNumber", result.Outputs.TryGetValue("SerialNumber", out var s) ? s?.ToString() : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ProductModel", (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", result.Status);
        cmd.Parameters.AddWithValue("@FailureReason", result.FailureReason ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@StartTime", result.StartTime.ToString("O"));
        cmd.Parameters.AddWithValue("@EndTime", result.EndTime.ToString("O"));
        cmd.Parameters.AddWithValue("@DurationMs", result.DurationMs);
        cmd.Parameters.AddWithValue("@Steps", stepJson);
        cmd.Parameters.AddWithValue("@ReportPath", reportFilePath ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));
        return (long)(await cmd.ExecuteScalarAsync(ct))!;
    }

    /// <inheritdoc />
    public async Task<TestRecord?> GetByIdAsync(long id, CancellationToken ct)
    {
        await EnsureInitializedAsync();
        using var cmd = new SqliteCommand("SELECT * FROM TestRecords WHERE Id = @Id", _connection);
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
            return ReadRecord(reader);
        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TestRecord>> QueryAsync(TestRecordQuery query, CancellationToken ct)
    {
        await EnsureInitializedAsync();
        var where = new List<string>();
        var parameters = new List<SqliteParameter>();
        int paramIdx = 0;

        if (query.BatchId != null) { where.Add($"BatchId = @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.BatchId)); }
        if (query.SerialNumber != null) { where.Add($"SerialNumber = @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.SerialNumber)); }
        if (query.ProductModel != null) { where.Add($"ProductModel = @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.ProductModel)); }
        if (query.Status != null) { where.Add($"Status = @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.Status)); }
        if (query.FromDate != null) { where.Add($"CreatedAt >= @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.FromDate.Value.ToString("O"))); }
        if (query.ToDate != null) { where.Add($"CreatedAt <= @p{paramIdx}"); parameters.Add(new SqliteParameter($"@p{paramIdx++}", query.ToDate.Value.ToString("O"))); }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        var sql = $"SELECT * FROM TestRecords {whereClause} ORDER BY CreatedAt DESC LIMIT {query.MaxResults}";

        using var cmd = new SqliteCommand(sql, _connection);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        var results = new List<TestRecord>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add(ReadRecord(reader));
        return results;
    }

    /// <summary>
    /// 从 SqliteDataReader 当前行读取 TestRecord。
    /// </summary>
    private static TestRecord ReadRecord(SqliteDataReader reader)
    {
        return new TestRecord
        {
            Id = reader.GetInt64(0),
            TaskId = reader.GetString(1),
            TaskName = reader.GetString(2),
            BatchId = reader.IsDBNull(3) ? null : reader.GetString(3),
            SerialNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
            ProductModel = reader.IsDBNull(5) ? null : reader.GetString(5),
            Status = reader.GetString(6),
            FailureReason = reader.IsDBNull(7) ? null : reader.GetString(7),
            StartTime = DateTime.Parse(reader.GetString(8)),
            EndTime = DateTime.Parse(reader.GetString(9)),
            DurationMs = reader.GetInt64(10),
            StepResultsJson = reader.IsDBNull(11) ? null : reader.GetString(11),
            ReportFilePath = reader.IsDBNull(12) ? null : reader.GetString(12),
            CreatedAt = DateTime.Parse(reader.GetString(13))
        };
    }

    /// <summary>
    /// 释放数据库连接资源。
    /// </summary>
    public void Dispose() => _connection?.Dispose();
}
