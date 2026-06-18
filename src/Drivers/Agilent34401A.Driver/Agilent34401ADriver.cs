using System.Globalization;
using System.Text.RegularExpressions;
using Cc.IDE.Communication;
using Cc.IDE.DriverSdk;

namespace Agilent34401A.Driver;

/// <summary>
/// Agilent/Keysight 34401A 6½ 位数字万用表驱动。
/// 支持直流/交流电压测量、电阻测量、直流电流测量、自检和重置等 SCPI 操作。
/// </summary>
/// <remarks>
/// 通信协议：SCPI over Serial/TCP。
/// 该驱动封装了 34401A 专用的 SCPI 指令格式和响应解析逻辑。
/// </remarks>
public sealed class Agilent34401ADriver : InstrumentDriverBase
{
    // ── 元数据 ──────────────────────────────────────────────────────

    public override string DriverId => "Keysight.Agilent.34401A";
    public override string DisplayName => "Agilent 34401A 数字万用表";
    public override string Version => "1.0.0";
    public override string DeviceType => "DMM";
    public override string Manufacturer => "Keysight";
    public override IReadOnlyList<string> SupportedTransports => new[] { "Serial", "TCP" };
    public override IReadOnlyList<DriverDependency> Dependencies => Array.Empty<DriverDependency>();

    // SCPI measurement response pattern: +1.2345E-03 V
    private static readonly Regex MeasurementPattern = new(
        @"^([+-]?\d+\.?\d*(?:E[+-]?\d+)?)\s*(V|A|OHM|Hz|S)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // ── 能力声明 ────────────────────────────────────────────────────

    public override IReadOnlyList<InstrumentCapability> GetCapabilities()
    {
        return new[]
        {
            new InstrumentCapability
            {
                FunctionId = "MeasureDCVoltage",
                DisplayName = "直流电压测量",
                Category = "测量",
                Description = "测量直流电压，可指定量程和分辨率。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 500,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "range", DisplayName = "量程", Type = "double", Unit = "V", DefaultValue = 10.0, Required = false,
                        Description = "量程：0.1 / 0.2 / 1 / 10 / 100 / 1000 V", EditorHint = "dropdown",
                        AllowedValues = new[] { "0.1", "0.2", "1.0", "10.0", "100.0", "1000.0" } },
                    new CapabilityParameter { Name = "resolution", DisplayName = "分辨率", Type = "string", DefaultValue = "0.1mV", Required = false,
                        Description = "分辨率：100nV / 1uV / 10uV / 0.1mV / 1mV / 10mV",
                        AllowedValues = new[] { "100nV", "1uV", "10uV", "0.1mV", "1mV", "10mV" } }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "voltage", DisplayName = "电压值", Type = "double", Unit = "V", Description = "测量到的直流电压值。" },
                    new CapabilityOutput { Name = "unit", DisplayName = "单位", Type = "string", Description = "测量值单位。" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "MeasureResistance",
                DisplayName = "电阻测量",
                Category = "测量",
                Description = "测量电阻值，支持 2 线制。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 400,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "range", DisplayName = "量程", Type = "double", Unit = "Ω", DefaultValue = 1000.0, Required = false,
                        Description = "量程：100 / 1000 / 10000 / 100000 / 1000000 / 10000000 Ω" }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "resistance", DisplayName = "电阻值", Type = "double", Unit = "Ω", Description = "测量到的电阻值。" },
                    new CapabilityOutput { Name = "unit", DisplayName = "单位", Type = "string", Description = "测量值单位。" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "MeasureDCCurrent",
                DisplayName = "直流电流测量",
                Category = "测量",
                Description = "测量直流电流。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 500,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "range", DisplayName = "量程", Type = "double", Unit = "A", DefaultValue = 1.0, Required = false,
                        Description = "量程：0.01 / 0.1 / 1 / 3 A" }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "current", DisplayName = "电流值", Type = "double", Unit = "A", Description = "测量到的直流电流值。" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "MeasureACVoltage",
                DisplayName = "交流电压测量",
                Category = "测量",
                Description = "测量交流电压（RMS）。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 800,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "range", DisplayName = "量程", Type = "double", Unit = "V", DefaultValue = 10.0, Required = false }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "voltage", DisplayName = "电压值", Type = "double", Unit = "VAC", Description = "测量到的交流电压有效值。" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "GetIdentification",
                DisplayName = "获取仪器标识",
                Category = "诊断",
                Description = "查询仪器的 *IDN? 标识字符串。",
                IsTestStepCapable = false,
                EstimatedDurationMs = 200,
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "identification", DisplayName = "标识字符串", Type = "string", Description = "*IDN? 返回的仪器标识。" }
                }
            },
        };
    }

    // ── 命令执行 ────────────────────────────────────────────────────

    public override async Task<InstrumentResult> ExecuteAsync(
        string functionId,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct)
    {
        if (Transport is not { IsConnected: true })
            return InstrumentResult.Error("仪器未连接。");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            return functionId switch
            {
                "MeasureDCVoltage" => await MeasureAsync("VOLT:DC", parameters, ct),
                "MeasureResistance" => await MeasureAsync("RES", parameters, ct),
                "MeasureDCCurrent" => await MeasureAsync("CURR:DC", parameters, ct),
                "MeasureACVoltage" => await MeasureAsync("VOLT:AC", parameters, ct),
                "GetIdentification" => await GetIdentificationInternalAsync(ct),
                _ => InstrumentResult.Error($"未知功能：{functionId}")
            };
        }
        catch (Exception ex)
        {
            RecordFailure();
            return InstrumentResult.Error($"执行 {functionId} 失败：{ex.Message}", ex);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private async Task<InstrumentResult> MeasureAsync(
        string function, IReadOnlyDictionary<string, object?> parameters, CancellationToken ct)
    {
        // Construct SCPI: CONF:<function> <range>,<resolution>
        var range = parameters.TryGetValue("range", out var r) && r is double dr ? dr : (double?)null;
        var resolution = parameters.TryGetValue("resolution", out var res) && res is string rs ? rs : null;

        var confCmd = range.HasValue
            ? $"CONF:{function} {range.Value}"
            : $"CONF:{function}";

        if (resolution != null)
            confCmd += $",{resolution}";

        await Transport!.SendAsync(confCmd, ct);
        await Transport.SendAsync("READ?", ct);
        var response = await Transport.ReceiveAsync(ct);

        // Parse response: "+1.2345E-03 V" → { value: 0.0012345, unit: "V" }
        var match = MeasurementPattern.Match(response.Trim());
        if (!match.Success)
            return new InstrumentResult
            {
                Success = false,
                Status = "Error",
                Message = $"无法解析万用表读数：\"{response}\"",
                RawResponse = response
            };

        var value = double.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;

        RecordSuccess(0);

        var outputKey = function switch
        {
            "RES" => "resistance",
            "CURR:DC" => "current",
            _ => "voltage"
        };

        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { [outputKey] = value, ["unit"] = unit },
            response);
    }

    private async Task<InstrumentResult> GetIdentificationInternalAsync(CancellationToken ct)
    {
        await Transport!.SendAsync("*IDN?", ct);
        var response = await Transport.ReceiveAsync(ct);
        RecordSuccess(0);
        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { ["identification"] = response.Trim() },
            response);
    }

    // ── 标识与诊断 ──────────────────────────────────────────────────

    public override async Task<string> GetIdentificationAsync()
    {
        if (Transport is not { IsConnected: true })
            throw new InvalidOperationException("仪器未连接。");
        await Transport.SendAsync("*IDN?", CancellationToken.None);
        return (await Transport.ReceiveAsync(CancellationToken.None)).Trim();
    }

    public override async Task ResetAsync()
    {
        if (Transport is not { IsConnected: true })
            throw new InvalidOperationException("仪器未连接。");
        await Transport.SendAsync("*RST", CancellationToken.None);
    }

    public override async Task<InstrumentSelfTestResult> SelfTestAsync()
    {
        if (Transport is not { IsConnected: true })
            throw new InvalidOperationException("仪器未连接。");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await Transport.SendAsync("*TST?", CancellationToken.None);
        var response = await Transport.ReceiveAsync(CancellationToken.None);
        sw.Stop();

        var resultCode = response.Trim();
        return new InstrumentSelfTestResult
        {
            Passed = resultCode == "0",
            Message = resultCode == "0" ? "自检通过。" : $"自检失败，错误码：{resultCode}",
            Details = new List<string> { $"返回码：{resultCode}" },
            ElapsedMs = sw.ElapsedMilliseconds
        };
    }
}
