using System.Text.Json;
using System.Text;

namespace Cc.IDE.Runtime;

/// <summary>
/// 测试报告生成器。支持 JSON、HTML 和 CSV 三种格式的测试结果导出。
/// </summary>
public static class ReportGenerator
{
    /// <summary>
    /// 将测试结果导出为 JSON 格式。
    /// </summary>
    /// <param name="result">测试结果对象。</param>
    /// <returns>格式化的 JSON 字符串。</returns>
    public static string GenerateJsonReport(TestResult result)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// 将测试结果导出为 HTML 格式。包含嵌入式样式以便离线查看。
    /// </summary>
    /// <param name="result">测试结果对象。</param>
    /// <returns>完整的 HTML 文档字符串。</returns>
    public static string GenerateHtmlReport(TestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>测试报告</title>");
        sb.AppendLine("<style>body{font-family:Arial;margin:20px;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:8px;text-align:left;}");
        sb.AppendLine("th{background:#007ACC;color:white;}");
        sb.AppendLine(".passed{color:green;}.failed{color:red;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>测试报告</h1>");
        sb.AppendLine($"<p>任务: {result.TaskName} ({result.TaskId})</p>");
        sb.AppendLine($"<p>状态: <span class='{(result.Status == "Passed" ? "passed" : "failed")}'>{result.Status}</span></p>");
        sb.AppendLine($"<p>耗时: {result.DurationMs}ms</p>");
        if (result.FailureReason != null)
            sb.AppendLine($"<p>失败原因: {result.FailureReason}</p>");

        sb.AppendLine("<h2>步骤结果</h2>");
        sb.AppendLine("<table><tr><th>节点</th><th>标题</th><th>状态</th><th>耗时(ms)</th></tr>");
        foreach (var step in result.StepResults)
        {
            sb.AppendLine($"<tr><td>{step.NodeId}</td><td>{step.NodeTitle}</td>");
            sb.AppendLine($"<td class='{(step.Status == "Passed" ? "passed" : "failed")}'>{step.Status}</td>");
            sb.AppendLine($"<td>{step.ElapsedMs}</td></tr>");
        }
        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// 将测试结果导出为 CSV 格式。仅导出步骤级别数据。
    /// </summary>
    /// <param name="result">测试结果对象。</param>
    /// <returns>CSV 格式字符串，首行为列标题。</returns>
    public static string GenerateCsvReport(TestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NodeId,NodeTitle,Status,ElapsedMs");
        foreach (var step in result.StepResults)
        {
            sb.AppendLine($"{step.NodeId},{step.NodeTitle},{step.Status},{step.ElapsedMs}");
        }
        return sb.ToString();
    }
}
