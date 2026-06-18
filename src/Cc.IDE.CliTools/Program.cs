using Cc.IDE.Communication;
using Cc.IDE.PLC;
using Cc.IDE.CAN;
using Cc.IDE.ProjectSystem.IO;
using Cc.IDE.ProjectSystem.Models;
using Cc.IDE.ProjectSystem.Serialization;
using Cc.IDE.Runtime;
using PIOConnectionConfig = Cc.IDE.PLC.IOConnectionConfig;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

var command = args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "smoke":
            await RunSmokeTestAsync();
            break;
        case "comm":
            await RunCommTestAsync(args.Skip(1).ToArray());
            break;
        case "modbus":
            await RunModbusTestAsync(args.Skip(1).ToArray());
            break;
        case "plc":
            await RunPlcServiceTestAsync(args.Skip(1).ToArray());
            break;
        case "can":
            RunCanServiceTest(args.Skip(1).ToArray());
            break;
        case "io":
            await RunIOServiceTestAsync(args.Skip(1).ToArray());
            break;
        case "eval":
            RunEvalTest(args.Skip(1).ToArray());
            break;
        case "runtime-test":
            await RunRuntimeIntegrationTestAsync(args.Skip(1).ToArray());
            break;
        case "debug-eval":
            RunDebugEvalTest();
            break;
        default:
            Console.WriteLine($"未知命令: {command}");
            PrintHelp();
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"执行失败: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

static void PrintHelp()
{
    Console.WriteLine("Cc.IDE CLI Tools — Phase A");
    Console.WriteLine("============================");
    Console.WriteLine();
    Console.WriteLine("Phase 1 命令:");
    Console.WriteLine("  smoke                     JSON 序列化冒烟测试");
    Console.WriteLine();
    Console.WriteLine("Phase 2 命令 (通讯):");
    Console.WriteLine("  comm serial              串口传输配置信息输出");
    Console.WriteLine("  comm tcp                 TCP 传输配置信息输出");
    Console.WriteLine("  modbus read-coil <addr>  读取单个 Modbus 线圈");
    Console.WriteLine("  modbus read-reg <addr>   读取单个 Modbus 保持寄存器");
    Console.WriteLine("  modbus protocol-info     Modbus TCP 协议帧格式说明");
    Console.WriteLine("  plc connect <host> <port> 通过 PLCService 连接设备");
    Console.WriteLine("  plc status               显示所有 PLC 设备状态");
    Console.WriteLine("  can list                 列出 CAN 接口状态");
    Console.WriteLine("  io resolve <code>        解析 IO 点位代码");
    Console.WriteLine("  io lock <deviceId>       锁定指定设备的 IO 输出");
    Console.WriteLine("  io unlock <deviceId>     解锁指定设备的 IO 输出");
    Console.WriteLine();
    Console.WriteLine("Phase A 命令 (运行时):");
    Console.WriteLine("  eval <expr>              求值表达式（多表达式用 ; 分隔）");
    Console.WriteLine("  eval --interactive       交互式表达式求值器");
    Console.WriteLine("  debug-eval               表达式求值器硬编码测试（绕过 shell 转义）");
    Console.WriteLine("  runtime-test             运行时集成测试（Task 执行）");
}

// ===== Smoke Test (Phase 1) =====
static async Task RunSmokeTestAsync()
{
    Console.WriteLine("=== Phase 1 冒烟测试 ===");
    Console.WriteLine();

    var solution = new SolutionDefinition
    {
        Name = "TestSolution",
        Description = "Phase 2 smoke test",
        StartupProject = "MainTest"
    };
    solution.Projects.Add("Project_MainTest/MainTest.yoursproj");
    solution.InstrumentRefs.Add("Shared/Instruments/DMM_Main.yourinst");
    solution.Globals["TestStation"] = "Station-01";

    var json = ProjectSystemJson.Serialize(solution);
    var deserialized = ProjectSystemJson.Deserialize<SolutionDefinition>(json);
    Console.WriteLine($"JSON Round-trip: Name='{deserialized!.Name}', Id={deserialized.Id}");

    var tempPath = Path.GetTempFileName().Replace(".tmp", ".yoursln");
    await ProjectSystemFiles.SaveSolutionAsync(tempPath, solution);
    var loaded = await ProjectSystemFiles.LoadSolutionAsync(tempPath);
    File.Delete(tempPath);
    Console.WriteLine($"File I/O: Name='{loaded.Name}', Projects={loaded.Projects.Count}");

    Console.WriteLine("Phase 1 smoke test passed ✓");
}

// ===== Communication Tests =====
static Task RunCommTestAsync(string[] args)
{
    var subCmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

    switch (subCmd)
    {
        case "serial":
            var sc = new SerialTransportConfig
            {
                PortName = "COM1",
                BaudRate = 9600,
                DataBits = 8,
                Parity = System.IO.Ports.Parity.None,
                StopBits = System.IO.Ports.StopBits.One,
                ReadTimeoutMs = 5000,
                WriteTimeoutMs = 5000,
                TerminationChar = "\n",
                EnableTerminationChar = true
            };
            Console.WriteLine("串口传输配置:");
            Console.WriteLine($"  端口: {sc.PortName}");
            Console.WriteLine($"  波特率: {sc.BaudRate}");
            Console.WriteLine($"  数据位: {sc.DataBits}");
            Console.WriteLine($"  校验: {sc.Parity}");
            Console.WriteLine($"  停止位: {sc.StopBits}");
            Console.WriteLine($"  读超时: {sc.ReadTimeoutMs}ms");
            Console.WriteLine($"  写超时: {sc.WriteTimeoutMs}ms");
            Console.WriteLine($"  终止符: \\n (启用={sc.EnableTerminationChar})");
            Console.WriteLine();
            Console.WriteLine("提示: 连接到物理串口请使用上位机软件进行测试。");
            break;

        case "tcp":
            var tc = new TcpTransportConfig
            {
                Host = "localhost",
                Port = 5025,
                ConnectTimeoutMs = 5000,
                ReadTimeoutMs = 5000,
                WriteTimeoutMs = 5000,
                TerminationChar = "\n",
                EnableTerminationChar = true,
                UseKeepAlive = true,
                NoDelay = true
            };
            Console.WriteLine("TCP 传输配置:");
            Console.WriteLine($"  主机: {tc.Host}:{tc.Port}");
            Console.WriteLine($"  连接超时: {tc.ConnectTimeoutMs}ms");
            Console.WriteLine($"  读超时: {tc.ReadTimeoutMs}ms");
            Console.WriteLine($"  写超时: {tc.WriteTimeoutMs}ms");
            Console.WriteLine($"  KeepAlive: {tc.UseKeepAlive}");
            Console.WriteLine($"  NoDelay: {tc.NoDelay}");
            Console.WriteLine($"  终止符: \\n (启用={tc.EnableTerminationChar})");
            Console.WriteLine();
            Console.WriteLine("提示: 使用 ModbusPal 或 diagslave 启动本地 Modbus TCP 模拟器后");
            Console.WriteLine("      运行 'Cc.IDE.CliTools modbus read-reg 0' 测试读写。");
            break;

        default:
            Console.WriteLine("用法: Cc.IDE.CliTools comm [serial|tcp]");
            break;
    }

    return Task.CompletedTask;
}

// ===== Modbus Tests =====
static async Task RunModbusTestAsync(string[] args)
{
    var subCmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

    switch (subCmd)
    {
        case "protocol-info":
            Console.WriteLine("Modbus TCP 协议帧格式:");
            Console.WriteLine("  MBAP Header (7 bytes):");
            Console.WriteLine("    TransactionId  2 bytes  (Big-Endian)");
            Console.WriteLine("    ProtocolId     2 bytes  (0x0000)");
            Console.WriteLine("    Length         2 bytes  (后续字节数)");
            Console.WriteLine("    UnitId         1 byte   (从站 ID)");
            Console.WriteLine();
            Console.WriteLine("  功能码:");
            Console.WriteLine("    01 (0x01)  Read Coils");
            Console.WriteLine("    02 (0x02)  Read Discrete Inputs");
            Console.WriteLine("    03 (0x03)  Read Holding Registers");
            Console.WriteLine("    04 (0x04)  Read Input Registers");
            Console.WriteLine("    05 (0x05)  Write Single Coil");
            Console.WriteLine("    06 (0x06)  Write Single Register");
            Console.WriteLine("    15 (0x0F)  Write Multiple Coils");
            Console.WriteLine("    16 (0x10)  Write Multiple Registers");
            Console.WriteLine();
            Console.WriteLine("  异常码 (function code | 0x80):");
            Console.WriteLine("    0x01  非法功能");
            Console.WriteLine("    0x02  非法数据地址");
            Console.WriteLine("    0x03  非法数据值");
            Console.WriteLine("    0x04  从站设备故障");
            break;

        case "read-coil":
        case "read-reg":
            if (args.Length < 2 || !int.TryParse(args[1], out var addr))
            {
                Console.WriteLine($"用法: Cc.IDE.CliTools modbus {subCmd} <address>");
                return;
            }

            var config = new PIOConnectionConfig
            {
                Host = "127.0.0.1",
                Port = 502,
                SlaveId = 1,
                TimeoutMs = 3000,
                RetryCount = 1
            };

            Console.WriteLine($"Modbus TCP 测试 — 连接 {config.Host}:{config.Port}...");
            Console.WriteLine($"  从站ID: {config.SlaveId}");
            Console.WriteLine($"  超时: {config.TimeoutMs}ms");

            {
                using var protocol = new ModbusTcpProtocol();
                try
                {
                    await protocol.ConnectAsync(config, CancellationToken.None);
                    Console.WriteLine("  连接成功!");

                    if (subCmd == "read-coil")
                    {
                        Console.WriteLine($"  读取线圈 @ {addr}...");
                        var result = await protocol.ReadCoilAsync(addr, CancellationToken.None);
                        Console.WriteLine($"  结果: {result}");
                    }
                    else
                    {
                        Console.WriteLine($"  读取寄存器 @ {addr}...");
                        var result = await protocol.ReadRegisterAsync(addr, CancellationToken.None);
                        Console.WriteLine($"  结果: {result} (0x{result:X4})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  连接/读取失败: {ex.Message}");
                    Console.WriteLine("  提示: 确保本地有 Modbus TCP 模拟器在端口 502 上运行。");
                }
                finally
                {
                    await protocol.DisconnectAsync();
                    Console.WriteLine("  已断开连接。");
                }
            }
            break;

        default:
            Console.WriteLine("用法: Cc.IDE.CliTools modbus [protocol-info|read-coil|read-reg]");
            break;
    }
}

// ===== PLC Service Tests =====
static async Task RunPlcServiceTestAsync(string[] args)
{
    var subCmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

    switch (subCmd)
    {
        case "connect":
            if (args.Length < 3)
            {
                Console.WriteLine("用法: Cc.IDE.CliTools plc connect <host> <port>");
                return;
            }

            var service = new PLCService();
            var deviceId = $"plc-{args[1]}:{args[2]}";
            var config = new PIOConnectionConfig
            {
                PlcProtocol = "ModbusTcp",
                Host = args[1],
                Port = int.Parse(args[2]),
                SlaveId = 1,
                TimeoutMs = 3000,
                RetryCount = 1
            };

            Console.WriteLine($"通过 PLCService 连接设备 '{deviceId}'...");
            try
            {
                await service.ConnectDeviceAsync(deviceId, config, CancellationToken.None);
                Console.WriteLine($"  连接成功! 当前设备数: {service.ConnectedDeviceCount}");

                var state = service.GetConnectionStates();
                foreach (var kv in state)
                {
                    Console.WriteLine($"  {kv.Key}: IsConnected={kv.Value.IsConnected}, " +
                                      $"Protocol={kv.Value.ProtocolType}, " +
                                      $"Host={kv.Value.Host}:{kv.Value.Port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  连接失败: {ex.Message}");
            }
            break;

        case "status":
            {
                var svc2 = new PLCService();
                var states2 = svc2.GetConnectionStates();
                if (states2.Count == 0)
                {
                    Console.WriteLine("当前没有已连接的 PLC 设备。");
                    Console.WriteLine("用法: Cc.IDE.CliTools plc connect <host> <port>");
                }
                else
                {
                    Console.WriteLine($"已连接设备: {states2.Count}");
                    foreach (var kv in states2)
                    {
                        Console.WriteLine($"  {kv.Key}: {kv.Value.ProtocolType} " +
                                          $"{kv.Value.Host}:{kv.Value.Port} " +
                                          $"Connected={kv.Value.IsConnected}");
                    }
                }
            }
            break;

        case "disconnect":
            if (args.Length < 2)
            {
                Console.WriteLine("用法: Cc.IDE.CliTools plc disconnect <deviceId>");
                return;
            }
            // Note: PLCService instances created in different commands don't share state.
            // This is a CLI tool limitation — actual state lives in the DI container at runtime.
            Console.WriteLine($"断开设备 '{args[1]}' (此命令仅在 DI 容器上下文中有效)。");
            break;

        default:
            Console.WriteLine("用法: Cc.IDE.CliTools plc [connect|status|disconnect]");
            break;
    }
}

// ===== CAN Service Tests =====
static void RunCanServiceTest(string[] args)
{
    var subCmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

    switch (subCmd)
    {
        case "list":
            var canService = new CANService();
            var states = canService.GetInterfaceStates();
            if (states.Count == 0)
            {
                Console.WriteLine("当前没有已注册的 CAN 接口。");
                Console.WriteLine("提示: CAN 接口在运行时通过 DI 容器管理。");
                Console.WriteLine("      使用 CANService.ConnectInterfaceAsync() 连接接口。");
            }
            else
            {
                Console.WriteLine($"CAN 接口: {states.Count}");
                foreach (var kv in states)
                {
                    Console.WriteLine($"  {kv.Key}: Type={kv.Value.InterfaceType} " +
                                      $"Connected={kv.Value.IsConnected}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("支持的 CAN 接口类型:");
            Console.WriteLine("  PCAN       — PEAK PCAN USB 适配器 (Phase 3 完整实现)");
            Console.WriteLine("  SocketCAN  — Linux SocketCAN (计划中)");
            Console.WriteLine("  Kvaser     — Kvaser CAN 适配器 (计划中)");
            break;

        default:
            Console.WriteLine("用法: Cc.IDE.CliTools can [list]");
            break;
    }
}

// ===== IO Service Tests =====
static async Task RunIOServiceTestAsync(string[] args)
{
    var subCmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

    switch (subCmd)
    {
        case "resolve":
            if (args.Length < 2)
            {
                Console.WriteLine("用法: Cc.IDE.CliTools io resolve <pointCode>");
                Console.WriteLine("示例: io resolve D100  (解析为 HoldingRegister @ 100)");
                Console.WriteLine("      io resolve Y0    (解析为 Coil @ 0)");
                Console.WriteLine("      io resolve X5    (解析为 DiscreteInput @ 5)");
                return;
            }

            var plcService = new PLCService();
            var ioService = new PLCIOService(plcService);

            var code = args[1];
            try
            {
                var resolution = ioService.ResolvePoint(code);
                Console.WriteLine($"点位代码解析: '{code}'");
                Console.WriteLine($"  RegisterKind:   {resolution.RegisterKind}");
                Console.WriteLine($"  RegisterOffset: {resolution.RegisterOffset}");
                Console.WriteLine($"  BitIndex:       {resolution.BitIndex}");
                Console.WriteLine($"  DataType:       {resolution.DataType}");
                Console.WriteLine($"  Access:         {resolution.Access}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析失败: {ex.Message}");
            }
            break;

        case "lock":
            if (args.Length < 2)
            {
                Console.WriteLine("用法: Cc.IDE.CliTools io lock <deviceId>");
                return;
            }
            {
                var ps = new PLCService();
                var ios = new PLCIOService(ps);
                ios.Lock(args[1]);
                Console.WriteLine($"设备 '{args[1]}' 的 IO 输出已锁定。");
            }
            break;

        case "unlock":
            if (args.Length < 2)
            {
                Console.WriteLine("用法: Cc.IDE.CliTools io unlock <deviceId>");
                return;
            }
            {
                var ps = new PLCService();
                var ios = new PLCIOService(ps);
                ios.Unlock(args[1]);
                Console.WriteLine($"设备 '{args[1]}' 的 IO 输出已解锁。");
            }
            break;

        default:
            Console.WriteLine("用法: Cc.IDE.CliTools io [resolve|lock|unlock]");
            break;
    }
}

// ===== Expression Evaluator Tests (Phase A.1) =====

static void RunEvalTest(string[] args)
{
    // 交互模式
    if (args.Length > 0 && args[0] == "--interactive")
    {
        RunInteractiveEval();
        return;
    }

    // 单次求值模式（多表达式用 ; 分隔）
    var expression = string.Join(" ", args);
    if (string.IsNullOrWhiteSpace(expression))
    {
        Console.WriteLine("用法: Cc.IDE.CliTools eval <表达式>");
        Console.WriteLine("示例: eval $voltage > 3.0");
        Console.WriteLine("      eval '$a > 5 && $b == true'");
        Console.WriteLine("      eval --interactive");
        return;
    }

    var ctx = CreateEvalContext();
    var evaluator = new ExpressionEvaluator(ctx);

    var expressions = expression.Split(';', StringSplitOptions.RemoveEmptyEntries);
    foreach (var expr in expressions)
    {
        var trimmed = expr.Trim();
        if (string.IsNullOrEmpty(trimmed)) continue;

        try
        {
            var result = evaluator.Evaluate(trimmed);
            Console.WriteLine($"  '{trimmed}' → {result}");
        }
        catch (ExpressionEvalException ex)
        {
            Console.WriteLine($"  '{trimmed}' → 错误: {ex.Message}");
        }
    }
}

static void RunInteractiveEval()
{
    var ctx = CreateEvalContext();
    var evaluator = new ExpressionEvaluator(ctx);

    Console.WriteLine("=== 表达式求值器 — 交互模式 ===");
    Console.WriteLine("输入表达式并按回车求值。输入 /quit 退出，/vars 查看变量。");
    Console.WriteLine("语法: $varName 引用变量，支持 == != > < >= <= && || ! ()");
    Console.WriteLine();

    while (true)
    {
        Console.Write("> ");
        var line = Console.ReadLine();
        if (line is null or "/quit" or "exit")
            break;

        if (line == "/vars")
        {
            Console.WriteLine("  当前变量:");
            foreach (var kv in ctx.Variables)
                Console.WriteLine($"    ${kv.Key} = {FormatValue(kv.Value)}");
            Console.WriteLine("  输入参数:");
            foreach (var kv in ctx.Inputs)
                Console.WriteLine($"    $input.{kv.Key} = {FormatValue(kv.Value)}");
            continue;
        }

        if (line.StartsWith("/set "))
        {
            var parts = line[5..].Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var value = ParseSimpleValue(parts[1]);
                ctx.SetVariable(parts[0], value);
                Console.WriteLine($"  已设置 ${parts[0]} = {FormatValue(value)}");
            }
            continue;
        }

        try
        {
            var result = evaluator.Evaluate(line);
            Console.ForegroundColor = result ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  {result}");
            Console.ResetColor();
        }
        catch (ExpressionEvalException ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  错误: {ex.Message}");
            Console.ResetColor();
        }
    }
}

static RuntimeContext CreateEvalContext()
{
    var ctx = new RuntimeContext();
    // 预置测试变量
    ctx.SetVariable("voltage", 5.0);
    ctx.SetVariable("current", 1.5);
    ctx.SetVariable("status", "OK");
    ctx.SetVariable("initOK", true);
    ctx.SetVariable("warmupDone", true);
    ctx.SetVariable("error", false);
    ctx.SetVariable("count", 3);
    ctx.SetVariable("forceContinue", false);
    ctx.SetVariable("temperature", 25.0);
    ctx.Inputs["targetVoltage"] = 5.0;
    ctx.Inputs["sn"] = "SN-2026-0001";
    return ctx;
}

static object? ParseSimpleValue(string text)
{
    if (bool.TryParse(text, out var b)) return b;
    if (int.TryParse(text, out var i)) return i;
    if (double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
    return text.Trim('\'', '"');
}

static string FormatValue(object? value) =>
    value switch
    {
        null => "null",
        bool b => b ? "true" : "false",
        double d => d.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null"
    };

// ===== Runtime Integration Test (Phase A.1/A.4) =====

static async Task RunRuntimeIntegrationTestAsync(string[] args)
{
    Console.WriteLine("=== 运行时集成测试 (Phase A) ===");
    Console.WriteLine();

    // 构造测试 Task
    var task = CreateIntegrationTestTask();

    Console.WriteLine($"任务: {task.Name}");
    Console.WriteLine($"节点数: {task.Nodes.Count}");
    Console.WriteLine($"连线数: {task.Links.Count}");
    Console.WriteLine();

    // 创建运行时主机并执行（使用真实 IO 服务）
    var ioService = new IOExecutionService(
        new PLCService(),
        new CANService());
    var host = new RuntimeHost(ioService);
    var options = new RuntimeRunOptions
    {
        GlobalTimeoutMs = 30_000,
        EnableDebug = false
    };

    var cts = new CancellationTokenSource();
    Console.WriteLine("启动运行时...");
    var result = await host.RunAsync(task, options, cts.Token);

    Console.WriteLine();
    Console.WriteLine($"=== 执行结果 ===");
    Console.WriteLine($"状态:   {result.Status}");
    Console.WriteLine($"耗时:   {result.DurationMs}ms");
    Console.WriteLine($"步骤数: {result.StepResults.Count}");
    if (result.FailureReason != null)
        Console.WriteLine($"失败原因: {result.FailureReason}");

    Console.WriteLine();
    Console.WriteLine("步骤详情:");
    foreach (var step in result.StepResults)
    {
        var icon = step.Status switch
        {
            "Passed" => "✔",
            "Failed" => "✘",
            "Skipped" => "→",
            _ => "?"
        };
        Console.WriteLine($"  {icon} [{step.Status}] {step.NodeTitle} ({step.NodeId})");
    }
}

static TaskDefinition CreateIntegrationTestTask()
{
    var task = new TaskDefinition
    {
        Id = "test-runtime-001",
        Name = "Phase A 运行时集成测试",
        Mode = "FlowGraph",
        Variables = new List<VariableDefinition>
        {
            new() { Name = "voltage", Type = "double", DefaultValue = 5.0 },
            new() { Name = "status", Type = "string", DefaultValue = "OK" },
            new() { Name = "initOK", Type = "bool", DefaultValue = true },
            new() { Name = "warmupDone", Type = "bool", DefaultValue = true },
        }
    };

    // 线性流程：Start → Init → Condition(电压检查) → PassEnd 或 FailEnd
    var start = new FlowNodeDefinition { Id = "start", NodeType = "Start", Title = "开始", X = 50, Y = 150 };
    var init = new FlowNodeDefinition
    {
        Id = "init",
        NodeType = "TestStep",
        Title = "初始化（设置变量）",
        X = 200, Y = 150,
    };
    var delay = new FlowNodeDefinition { Id = "d1", NodeType = "Delay", Title = "等待100ms", DelayMs = 100, X = 350, Y = 150 };
    var condition = new FlowNodeDefinition
    {
        Id = "cond1",
        NodeType = "Condition",
        Title = "检查电压和状态",
        X = 500, Y = 150,
        Branches = new List<ConditionBranchDefinition>
        {
            new() { Label = "通过", Expression = "$voltage > 3.0 && $status == 'OK'", TargetNodeId = "passEnd" },
            new() { Label = "电压低", Expression = "$voltage <= 3.0", TargetNodeId = "failLow" },
            new() { Label = "状态错", Expression = "$status != 'OK'", TargetNodeId = "failStatus" },
        },
        DefaultBranchTargetId = "failUnknown"
    };
    var passEnd = new FlowNodeDefinition { Id = "passEnd", NodeType = "End", Title = "通过 ✓", X = 700, Y = 80 };
    var failLow = new FlowNodeDefinition { Id = "failLow", NodeType = "End", Title = "失败: 电压过低", X = 700, Y = 200 };
    var failStatus = new FlowNodeDefinition { Id = "failStatus", NodeType = "End", Title = "失败: 状态异常", X = 700, Y = 300 };
    var failUnknown = new FlowNodeDefinition { Id = "failUnknown", NodeType = "End", Title = "失败: 未知原因", X = 700, Y = 400 };

    task.Nodes.AddRange(new[] { start, init, delay, condition, passEnd, failLow, failStatus, failUnknown });

    // 连线
    task.Links.AddRange(new[]
    {
        new FlowLinkDefinition { FromNodeId = "start", ToNodeId = "init" },
        new FlowLinkDefinition { FromNodeId = "init", ToNodeId = "d1" },
        new FlowLinkDefinition { FromNodeId = "d1", ToNodeId = "cond1" },
    });

    return task;
}

// ===== Debug Eval Test (hardcoded expressions, no shell escaping issues) =====

static void RunDebugEvalTest()
{
    Console.WriteLine("=== 表达式求值器 — 硬编码调试测试 ===");
    Console.WriteLine();

    var ctx = CreateEvalContext();
    var evaluator = new ExpressionEvaluator(ctx);

    // 打印当前变量
    Console.WriteLine("当前变量:");
    foreach (var kv in ctx.Variables)
        Console.WriteLine($"  ${kv.Key} = {FormatValue(kv.Value)} (type: {kv.Value?.GetType().Name ?? "null"})");
    Console.WriteLine();

    // 测试表达式列表
    var tests = new (string expr, bool expected)[]
    {
        ("$voltage > 3.0", true),
        ("$voltage > 10.0", false),
        ("$voltage <= 3.0", false),
        ("$status == 'OK'", true),
        ("$status != 'OK'", false),
        ("$status == 'FAIL'", false),
        ("$initOK && $warmupDone", true),
        ("$initOK && !$warmupDone", false),
        ("!$error || $forceContinue", true),
        ("$voltage > 3.0 && $status == 'OK'", true),
        ("$voltage > 3.0 && $status != 'OK'", false),
        ("($voltage > 4.0) && ($current < 2.0)", true),
        ("$count >= 10", false),
        ("$temperature <= 30", true),
        ("!$error", true),
        ("$error || $forceContinue", false),
        // edge cases
        ("true", true),
        ("false", false),
        ("!true", false),
        ("!!true", true),
        ("true && false", false),
        ("true || false", true),
        ("(true || false) && true", true),
    };

    var passed = 0;
    var failed = 0;

    foreach (var (expr, expected) in tests)
    {
        try
        {
            var result = evaluator.Evaluate(expr);
            var ok = result == expected;
            var icon = ok ? "✔" : "✘";
            Console.WriteLine($"  {icon} '{expr}' → {result} (期望: {expected})");
            if (ok) passed++; else failed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✘ '{expr}' → 异常: {ex.Message}");
            failed++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"通过: {passed}, 失败: {failed}, 总计: {passed + failed}");

    if (failed > 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {failed} 个测试失败!");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ 全部测试通过!");
        Console.ResetColor();
    }
}
