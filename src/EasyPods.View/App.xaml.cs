using System.Windows;
using EasyPods.Model.Common;

namespace EasyPods.View;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppLogger.Initialize();
        AppLogger.I("EasyPods application starting up...", tag: "App");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.I("EasyPods application shutting down...", tag: "App");
        AppLogger.Shutdown();

        base.OnExit(e);
    }
}
