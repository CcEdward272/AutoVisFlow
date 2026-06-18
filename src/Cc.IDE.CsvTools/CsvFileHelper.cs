using System.Globalization;
using System.Text;

namespace Cc.IDE.CsvTools;

/// <summary>
/// Convenience helpers for reading and writing CSV files.
/// </summary>
public static class CsvFileHelper
{
    /// <summary>
    /// 读取 CSV 文件的所有行，第一行作为表头。
    /// </summary>
    /// <param name="path">CSV 文件的绝对路径。</param>
    /// <param name="delimiter">字段分隔符，默认为逗号。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>字典列表，每个字典表示一行，键为表头、值为对应字段。</returns>
    public static async Task<List<Dictionary<string, string>>> ReadAsync(
        string path,
        char delimiter = ',',
        CancellationToken ct = default)
    {
        var lines = await File.ReadAllLinesAsync(path, ct);
        if (lines.Length == 0)
            return new List<Dictionary<string, string>>();

        var headers = lines[0].Split(delimiter);
        var rows = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(delimiter);
            var row = new Dictionary<string, string>();
            for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                row[headers[j].Trim()] = values[j].Trim();
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// 将行数据写入 CSV 文件。
    /// </summary>
    /// <param name="path">目标文件的绝对路径。</param>
    /// <param name="rows">要写入的行数据，每行为列名到值的映射。</param>
    /// <param name="delimiter">字段分隔符，默认为逗号。</param>
    /// <param name="ct">取消令牌。</param>
    public static async Task WriteAsync(
        string path,
        List<Dictionary<string, object?>> rows,
        char delimiter = ',',
        CancellationToken ct = default)
    {
        if (rows.Count == 0)
        {
            await File.WriteAllTextAsync(path, string.Empty, ct);
            return;
        }

        var sb = new StringBuilder();
        var headers = rows[0].Keys.ToList();

        sb.AppendLine(string.Join(delimiter, headers));
        foreach (var row in rows)
        {
            var values = new List<string>();
            foreach (var h in headers)
            {
                var val = row.TryGetValue(h, out var v) ? v : null;
                values.Add(EscapeCsvValue(val?.ToString() ?? string.Empty, delimiter));
            }
            sb.AppendLine(string.Join(delimiter, values));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), ct);
    }

    private static string EscapeCsvValue(string value, char delimiter)
    {
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
