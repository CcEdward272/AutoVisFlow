using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Cc.IDE.Mvvm;
using Cc.IDE.Runtime;
using Cc.IDE.PLC;
using Cc.IDE.CAN;
using Cc.IDE.DriverSdk;

namespace Cc.IDE.App;

/// <summary>
/// IDE 壳层的中央依赖注入组装点。
/// 所有服务注册和跨模块连线在此集中完成。
/// </summary>
public static class StudioComposer
{
    /// <summary>
    /// 构建并返回 IDE 的依赖注入服务提供器，注册所有核心服务。
    /// </summary>
    /// <returns>配置完成的 <see cref="ServiceProvider"/> 实例。</returns>
    public static ServiceProvider Compose()
    {
        var services = new ServiceCollection();

        // ─── 基础设施 ─────────────────────────────────────────────────

        services.AddSingleton<IEventAggregator, EventAggregator>();

        // ─── 运行时引擎 ──────────────────────────────────────────────

        services.AddSingleton<IRuntimeHost, RuntimeHost>();

        // ─── 仪器驱动管理 ────────────────────────────────────────────

        services.AddSingleton<IInstrumentManager, InstrumentManager>();

        // ─── PLC 服务 ────────────────────────────────────────────────

        services.AddSingleton<IPLCService, PLCService>();

        // ─── CAN 服务 ────────────────────────────────────────────────

        services.AddSingleton<ICANService, CANService>();

        // ─── 统一 IO 服务 ────────────────────────────────────────────

        services.AddSingleton<PLCIOService>();
        services.AddSingleton<CANIOService>();

        // ─── IO 执行服务 ─────────────────────────────────────────────

        services.AddSingleton<IIOExecutionService, IOExecutionService>();

        // ─── 项目系统 ─────────────────────────────────────────────────
        // (Phase 6: 注册 ISolutionService, IProjectService)

        // ─── 文档管理 ────────────────────────────────────────────────
        // (Phase 6: 注册 IDocumentManager, INavigationService)

        // ─── 协调器 ──────────────────────────────────────────────────
        // (Phase 6: 注册 WorkspaceLoadCoordinator 等)

        // ─── 报告与追溯（Phase 9）────────────────────────────────────
        services.AddSingleton<ITestRecordRepository>(sp =>
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CcIDE", "test_records.db");
            return new SqliteTestRecordRepository(dbPath);
        });

        // ─── Player 操作员界面（Phase 10）─────────────────────────────
        // Player 是独立进程，由 App.xaml 自行组装；此处注册跨进程共享的服务。

        // ─── 量产准备（Phase 11）──────────────────────────────────────

        return services.BuildServiceProvider();
    }
}
