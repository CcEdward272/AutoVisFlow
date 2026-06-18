using Cc.IDE.Communication;
using Cc.IDE.DriverSdk;

namespace RigolDP832.Driver;

/// <summary>
/// Rigol DP832 三通道程控直流电源驱动。
/// 支持电压/电流设置、输出控制和实际输出测量。
/// </summary>
/// <remarks>
/// 通信协议：SCPI over Serial/TCP。
/// 多步序列封装：设置电压前自动解锁键盘、设置限流、最后开启输出。
/// 通道 1/2 为 30V/3A，通道 3 为 5V/3A。
/// </remarks>
public sealed class RigolDP832Driver : InstrumentDriverBase
{
    // ── 元数据 ──────────────────────────────────────────────────────

    public override string DriverId => "Rigol.DP832";
    public override string DisplayName => "Rigol DP832 程控直流电源";
    public override string Version => "1.0.0";
    public override string DeviceType => "PowerSupply";
    public override string Manufacturer => "Rigol";
    public override IReadOnlyList<string> SupportedTransports => new[] { "Serial", "TCP" };
    public override IReadOnlyList<DriverDependency> Dependencies => Array.Empty<DriverDependency>();

    // ── 能力声明 ────────────────────────────────────────────────────

    public override IReadOnlyList<InstrumentCapability> GetCapabilities()
    {
        return new[]
        {
            new InstrumentCapability
            {
                FunctionId = "SetVoltage",
                DisplayName = "设置电压",
                Category = "输出",
                Description = "设置指定通道的输出电压（V）。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 300,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        Description = "输出通道：1 / 2 / 3", EditorHint = "dropdown",
                        AllowedValues = new object[] { 1, 2, 3 }, MinValue = 1, MaxValue = 3 },
                    new CapabilityParameter { Name = "voltage", DisplayName = "电压", Type = "double", Unit = "V", DefaultValue = 5.0, Required = true,
                        Description = "目标电压值（V）。CH1/2：0-30V，CH3：0-5V。", MinValue = 0, MaxValue = 30 }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "channel", DisplayName = "通道", Type = "int", Description = "已设置的通道号。" },
                    new CapabilityOutput { Name = "voltage", DisplayName = "设定电压", Type = "double", Unit = "V", Description = "已设定的电压值。" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "SetCurrent",
                DisplayName = "设置电流",
                Category = "输出",
                Description = "设置指定通道的输出电流限制（A）。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 200,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        AllowedValues = new object[] { 1, 2, 3 } },
                    new CapabilityParameter { Name = "current", DisplayName = "电流", Type = "double", Unit = "A", DefaultValue = 1.0, Required = true,
                        Description = "电流限制值（A）。CH1/2：0-3A，CH3：0-3A。", MinValue = 0, MaxValue = 3 }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "OutputOn",
                DisplayName = "开启输出",
                Category = "输出",
                Description = "开启指定通道的输出。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 200,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        AllowedValues = new object[] { 1, 2, 3 } }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "OutputOff",
                DisplayName = "关闭输出",
                Category = "输出",
                Description = "关闭指定通道的输出。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 200,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        AllowedValues = new object[] { 1, 2, 3 } }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "MeasureOutput",
                DisplayName = "读取输出电压/电流",
                Category = "测量",
                Description = "读取指定通道的实际输出电压和电流。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 300,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        AllowedValues = new object[] { 1, 2, 3 } }
                },
                Outputs = new[]
                {
                    new CapabilityOutput { Name = "voltage", DisplayName = "实际电压", Type = "double", Unit = "V" },
                    new CapabilityOutput { Name = "current", DisplayName = "实际电流", Type = "double", Unit = "A" }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "QuickSet",
                DisplayName = "快速设置并输出",
                Category = "输出",
                Description = "一键设置电压、电流并开启输出。内部封装了解锁→设电压→设限流→开输出的完整序列。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 600,
                Parameters = new[]
                {
                    new CapabilityParameter { Name = "channel", DisplayName = "通道", Type = "int", DefaultValue = 1, Required = true,
                        AllowedValues = new object[] { 1, 2, 3 } },
                    new CapabilityParameter { Name = "voltage", DisplayName = "电压", Type = "double", Unit = "V", DefaultValue = 5.0, Required = true },
                    new CapabilityParameter { Name = "current", DisplayName = "电流", Type = "double", Unit = "A", DefaultValue = 1.0, Required = true }
                }
            },
            new InstrumentCapability
            {
                FunctionId = "AllOutputsOff",
                DisplayName = "全部输出关闭",
                Category = "输出",
                Description = "同时关闭所有三个通道的输出（安全急停）。",
                IsTestStepCapable = true,
                EstimatedDurationMs = 300
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

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = functionId switch
            {
                "SetVoltage" => await SetVoltageAsync(parameters, ct),
                "SetCurrent" => await SetCurrentAsync(parameters, ct),
                "OutputOn" => await OutputOnAsync(parameters, ct),
                "OutputOff" => await OutputOffAsync(parameters, ct),
                "MeasureOutput" => await MeasureOutputAsync(parameters, ct),
                "QuickSet" => await QuickSetAsync(parameters, ct),
                "AllOutputsOff" => await AllOutputsOffAsync(ct),
                _ => InstrumentResult.Error($"未知功能：{functionId}")
            };

            RecordSuccess(sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure();
            return InstrumentResult.Error($"执行 {functionId} 失败：{ex.Message}", ex);
        }
    }

    private int GetChannel(IReadOnlyDictionary<string, object?> p)
    {
        return p.TryGetValue("channel", out var ch) && ch is int ci ? ci : 1;
    }

    private double GetDouble(IReadOnlyDictionary<string, object?> p, string key, double defaultValue)
    {
        return p.TryGetValue(key, out var v) && v is double d ? d : defaultValue;
    }

    private async Task<InstrumentResult> SetVoltageAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        var voltage = GetDouble(p, "voltage", 5.0);
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync($"VOLT {voltage}", ct);
        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { ["channel"] = ch, ["voltage"] = voltage });
    }

    private async Task<InstrumentResult> SetCurrentAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        var current = GetDouble(p, "current", 1.0);
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync($"CURR {current}", ct);
        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { ["channel"] = ch, ["current"] = current });
    }

    private async Task<InstrumentResult> OutputOnAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync("OUTP ON", ct);
        return InstrumentResult.SuccessResult(new Dictionary<string, object?> { ["channel"] = ch, ["output"] = true });
    }

    private async Task<InstrumentResult> OutputOffAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync("OUTP OFF", ct);
        return InstrumentResult.SuccessResult(new Dictionary<string, object?> { ["channel"] = ch, ["output"] = false });
    }

    private async Task<InstrumentResult> MeasureOutputAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync("MEAS:VOLT?", ct);
        var vResp = await Transport.ReceiveAsync(ct);
        await Transport.SendAsync("MEAS:CURR?", ct);
        var iResp = await Transport.ReceiveAsync(ct);

        double.TryParse(vResp.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var voltage);
        double.TryParse(iResp.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var current);

        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { ["voltage"] = voltage, ["current"] = current, ["channel"] = ch });
    }

    private async Task<InstrumentResult> QuickSetAsync(IReadOnlyDictionary<string, object?> p, CancellationToken ct)
    {
        var ch = GetChannel(p);
        var voltage = GetDouble(p, "voltage", 5.0);
        var current = GetDouble(p, "current", 1.0);

        // 多步序列封装：选择通道 → 解锁键盘 → 设电压 → 设限流 → 开输出
        await Transport!.SendAsync($"INST:NSEL {ch}", ct);
        await Transport.SendAsync("SYST:KLOC OFF", ct);    // 解锁键盘
        await Transport.SendAsync($"VOLT {voltage}", ct);
        await Transport.SendAsync($"CURR {current}", ct);
        await Transport.SendAsync("OUTP ON", ct);

        return InstrumentResult.SuccessResult(
            new Dictionary<string, object?> { ["channel"] = ch, ["voltage"] = voltage, ["current"] = current, ["output"] = true });
    }

    private async Task<InstrumentResult> AllOutputsOffAsync(CancellationToken ct)
    {
        for (int ch = 1; ch <= 3; ch++)
        {
            await Transport!.SendAsync($"INST:NSEL {ch}", ct);
            await Transport.SendAsync("OUTP OFF", ct);
        }
        return InstrumentResult.SuccessResult(new Dictionary<string, object?> { ["output"] = false });
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
