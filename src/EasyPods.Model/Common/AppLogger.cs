using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace EasyPods.Model.Common;

/// <summary>
/// Centralized high-performance logging facade for EasyPods powered by NLog.
/// Provides non-blocking asynchronous file and debugger targets.
/// </summary>
public static class AppLogger
{
    private static bool _isInitialized;
    private static readonly object _initLock = new();

    /// <summary>
    /// Initializes NLog configuration with async rolling file and debugger targets.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public static void Initialize(LogLevel? minLogLevel = null)
    {
        lock (_initLock)
        {
            if (_isInitialized) return;

            var config = new LoggingConfiguration();

            var logLayout = "${longdate}|${level:uppercase=true}|[${logger}] ${message}${onexception:inner= ${exception:format=tostring}}";

            // 1. Visual Studio / Debugger Target
            var debuggerTarget = new DebuggerTarget("debugger")
            {
                Layout = logLayout
            };
            config.AddTarget(debuggerTarget);

            // 2. Async Rolling File Target (in %LOCALAPPDATA%\EasyPods\Logs\)
            var fileTarget = new FileTarget("file")
            {
                FileName = "${specialfolder:folder=LocalApplicationData}/EasyPods/Logs/easypods-${shortdate}.log",
                ArchiveFileName = "${specialfolder:folder=LocalApplicationData}/EasyPods/Logs/archives/easypods-{#}.log",
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 7,
                KeepFileOpen = true,
                Layout = logLayout
            };

            var asyncFileTarget = new AsyncTargetWrapper(fileTarget, 5000, AsyncTargetWrapperOverflowAction.Discard)
            {
                Name = "asyncFile"
            };
            config.AddTarget(asyncFileTarget);

            // Enable logging rules
            var effectiveMinLevel = minLogLevel ?? LogLevel.Debug;
            config.AddRule(effectiveMinLevel, LogLevel.Fatal, debuggerTarget);
            config.AddRule(effectiveMinLevel, LogLevel.Fatal, asyncFileTarget);

            LogManager.Configuration = config;
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Logs a debug message for fine-grained tracing and state transitions.
    /// </summary>
    public static void D(string message, string tag = "EasyPods")
    {
        EnsureInitialized();
        LogManager.GetLogger(tag).Debug(message);
    }

    /// <summary>
    /// Logs an informational message for key lifecycle events.
    /// </summary>
    public static void I(string message, string tag = "EasyPods")
    {
        EnsureInitialized();
        LogManager.GetLogger(tag).Info(message);
    }

    /// <summary>
    /// Logs a warning message for non-fatal or recoverable issues.
    /// </summary>
    public static void W(string message, string tag = "EasyPods")
    {
        EnsureInitialized();
        LogManager.GetLogger(tag).Warn(message);
    }

    /// <summary>
    /// Logs an error message and optional exception.
    /// </summary>
    public static void E(string message, Exception? exception = null, string tag = "EasyPods")
    {
        EnsureInitialized();
        var logger = LogManager.GetLogger(tag);
        if (exception is not null)
        {
            logger.Error(exception, message);
        }
        else
        {
            logger.Error(message);
        }
    }

    /// <summary>
    /// Returns an NLog Logger instance for a specific tag or class.
    /// </summary>
    public static ILogger GetLogger(string tag)
    {
        EnsureInitialized();
        return LogManager.GetLogger(tag);
    }

    /// <summary>
    /// Flushes pending log entries (useful before application exit).
    /// </summary>
    public static void Shutdown()
    {
        LogManager.Shutdown();
    }

    private static void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
    }
}
