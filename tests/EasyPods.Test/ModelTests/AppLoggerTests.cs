using EasyPods.Model.Common;
using Xunit;

namespace EasyPods.Test.ModelTests;

public class AppLoggerTests
{
    [Fact]
    public void AppLogger_Methods_DoNotThrowExceptions()
    {
        AppLogger.Initialize();

        // Exercise all requested log levels: Debug, Info, Warn/Warning, Error, Fatal
        AppLogger.Debug("Debug trace test", tag: "UnitTest");
        AppLogger.Info("Info lifecycle test", tag: "UnitTest");
        AppLogger.Warn("Warn warning test", tag: "UnitTest");
        AppLogger.Warning("Warning alias test", tag: "UnitTest");
        AppLogger.Error("Error failure test", new InvalidOperationException("Test ex"), tag: "UnitTest");
        AppLogger.Fatal("Fatal crash test", new AccessViolationException("Crash simulation"), tag: "UnitTest");

        var logger = AppLogger.GetLogger("CustomTag");
        Assert.NotNull(logger);
        Assert.Equal("CustomTag", logger.Name);
    }
}
