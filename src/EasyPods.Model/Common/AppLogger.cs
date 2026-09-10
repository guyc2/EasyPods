using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyPods.Model.Common;

/// <summary>
/// Centralized telemetry and logging interface for EasyPods.
/// </summary>
public static class AppLogger
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public static ILogger CreateLogger<T>() => _loggerFactory.CreateLogger<T>();
    public static ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);
}
