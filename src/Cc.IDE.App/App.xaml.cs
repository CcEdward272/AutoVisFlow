using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Cc.IDE.Mvvm;
using Cc.IDE.Runtime;
using Cc.IDE.PLC;
using Cc.IDE.CAN;

namespace Cc.IDE.App;

/// <summary>
/// IDE application entry point. Handles DI composition and startup.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// 应用程序启动时执行，配置依赖注入容器并显示主窗口。
    /// </summary>
    /// <param name="e">包含启动参数的事件数据。</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = StudioComposer.Compose();

        var window = new MainWindow();
        window.Show();
    }

    /// <summary>
    /// 应用程序退出时执行，释放依赖注入容器资源。
    /// </summary>
    /// <param name="e">包含退出事件数据。</param>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
