using System.Windows;
using System.Windows.Threading;
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
        SetupCrashHandlers();

        AppLogger.Info("EasyPods application starting up...", tag: nameof(App));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Info("EasyPods application shutting down...", tag: nameof(App));
        AppLogger.Shutdown();

        base.OnExit(e);
    }

    private void SetupCrashHandlers()
    {
        // 1. UI Thread unhandled exceptions
        DispatcherUnhandledException += (sender, args) =>
        {
            AppLogger.Fatal("Unhandled UI Dispatcher exception encountered.", args.Exception, tag: "CrashReporter");
            // Ensure log is written immediately before potential process crash
            AppLogger.Shutdown();
        };

        // 2. Non-UI / Background Thread unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            AppLogger.Fatal($"Unhandled AppDomain exception. IsTerminating={args.IsTerminating}", ex, tag: "CrashReporter");
            AppLogger.Shutdown();
        };

        // 3. Unobserved background tasks
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            AppLogger.Fatal("Unobserved background Task exception encountered.", args.Exception, tag: "CrashReporter");
            args.SetObserved();
        };
    }
}
