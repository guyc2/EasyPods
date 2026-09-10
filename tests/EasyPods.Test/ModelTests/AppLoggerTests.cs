using EasyPods.Model.Common;
using Xunit;

namespace EasyPods.Test.ModelTests;

public class AppLoggerTests
{
    [Fact]
    public void AppLogger_Methods_DoNotThrowExceptions()
    {
        AppLogger.Initialize();

        // Exercise all log levels
        AppLogger.D("Debug message test", tag: "UnitTest");
        AppLogger.I("Info message test", tag: "UnitTest");
        AppLogger.W("Warn message test", tag: "UnitTest");
        AppLogger.E("Error message test", new InvalidOperationException("Test ex"), tag: "UnitTest");

        var logger = AppLogger.GetLogger("CustomTag");
        Assert.NotNull(logger);
        Assert.Equal("CustomTag", logger.Name);
    }
}
