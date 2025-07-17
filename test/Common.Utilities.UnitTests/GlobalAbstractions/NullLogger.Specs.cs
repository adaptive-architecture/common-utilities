using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.GlobalAbstractions;

public class NullLoggerSpecs
{
    [Fact]
    public void Should_Have_Singleton_Instance()
    {
        var instance1 = NullLogger.Instance;
        var instance2 = NullLogger.Instance;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void LogTrace_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogTrace("Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogTrace_With_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogTrace("Test message {0}", "arg1"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogDebug_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogDebug("Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogDebug_With_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogDebug("Test message {0}", "arg1"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogInformation_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogInformation("Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogInformation_With_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogInformation("Test message {0}", "arg1"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogWarning_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogWarning(null, "Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogWarning_With_Exception_And_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var testException = new InvalidOperationException("Test exception");
        var exception = Record.Exception(() => logger.LogWarning(testException, "Test message {0}", "arg1"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogError_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogError(null, "Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogError_With_Exception_And_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var testException = new InvalidOperationException("Test exception");
        var exception = Record.Exception(() => logger.LogError(testException, "Test message {0}", "arg1"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogCritical_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var exception = Record.Exception(() => logger.LogCritical(null, "Test message"));

        Assert.Null(exception);
    }

    [Fact]
    public void LogCritical_With_Exception_And_Args_Should_Not_Throw_Exception()
    {
        var logger = NullLogger.Instance;
        var testException = new InvalidOperationException("Test exception");
        var exception = Record.Exception(() => logger.LogCritical(testException, "Test message {0}", "arg1"));

        Assert.Null(exception);
    }
}
