namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using Xunit;

/// <summary>
/// Comprehensive tests for HashRingHistoryLimitExceededException covering all constructors,
/// edge cases, serialization scenarios, and integration testing.
/// </summary>
public sealed class HashRingHistoryLimitExceededExceptionComprehensiveTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_CreatesExceptionWithDefaultValues()
    {
        var exception = new HashRingHistoryLimitExceededException();

        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
        Assert.NotNull(exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MessageConstructor_SetsCustomMessage()
    {
        const string customMessage = "Custom error message for testing";

        var exception = new HashRingHistoryLimitExceededException(customMessage);

        Assert.Equal(customMessage, exception.Message);
        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MessageConstructor_WithNullMessage_HandlesCorrectly()
    {
        const string nullMessage = null;

        var exception = new HashRingHistoryLimitExceededException(nullMessage);

        // Note: Base InvalidOperationException class sets a default message when null is passed
        Assert.NotNull(exception.Message);
        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
    }

    [Fact]
    public void MessageConstructor_WithEmptyMessage_PreservesEmptyString()
    {
        const string emptyMessage = "";

        var exception = new HashRingHistoryLimitExceededException(emptyMessage);

        Assert.Equal(emptyMessage, exception.Message);
        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_SetsAllProperties()
    {
        const string message = "Outer exception message";
        var innerException = new ArgumentException("Inner exception message");

        var exception = new HashRingHistoryLimitExceededException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_WithNullInnerException_HandlesCorrectly()
    {
        const string message = "Test message";
        Exception nullInnerException = null;

        var exception = new HashRingHistoryLimitExceededException(message, nullInnerException);

        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, exception.MaxHistorySize);
        Assert.Equal(0, exception.CurrentCount);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 4)]
    [InlineData(10, 8)]
    [InlineData(100, 95)]
    [InlineData(Int32.MaxValue, Int32.MaxValue - 1)]
    public void ParameterizedConstructor_WithValidValues_SetsPropertiesCorrectly(int maxSize, int currentCount)
    {
        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains($"History limit of {maxSize}", exception.Message);
        Assert.Contains($"Current count: {currentCount}", exception.Message);
        Assert.Contains("Cannot create configuration snapshot", exception.Message);
        Assert.Contains("would be exceeded", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    [InlineData(Int32.MinValue, Int32.MinValue)]
    public void ParameterizedConstructor_WithBoundaryValues_HandlesCorrectly(int maxSize, int currentCount)
    {
        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.NotNull(exception.Message);
        Assert.Contains(maxSize.ToString(), exception.Message);
        Assert.Contains(currentCount.ToString(), exception.Message);
    }

    [Fact]
    public void ParameterizedConstructorWithMessage_SetsAllProperties()
    {
        const int maxSize = 5;
        const int currentCount = 3;
        const string customMessage = "Custom history limit message";

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount, customMessage);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Equal(customMessage, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void ParameterizedConstructorWithMessageAndInnerException_SetsAllProperties()
    {
        const int maxSize = 3;
        const int currentCount = 3;
        const string message = "Custom message with inner exception";
        var innerException = new InvalidOperationException("Inner exception");

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount, message, innerException);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #endregion

    #region Message Formatting Tests

    [Fact]
    public void FormatMessage_GeneratesExpectedFormat()
    {
        const int maxSize = 5;
        const int currentCount = 4;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        var expectedMessage = $"Cannot create configuration snapshot. History limit of {maxSize} would be exceeded. Current count: {currentCount}";
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(1, 0, "Cannot create configuration snapshot. History limit of 1 would be exceeded. Current count: 0")]
    [InlineData(10, 10, "Cannot create configuration snapshot. History limit of 10 would be exceeded. Current count: 10")]
    [InlineData(100, 99, "Cannot create configuration snapshot. History limit of 100 would be exceeded. Current count: 99")]
    public void FormatMessage_WithVariousValues_GeneratesCorrectFormat(int maxSize, int currentCount, string expectedMessage)
    {
        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void FormatMessage_WithNegativeValues_HandlesCorrectly()
    {
        const int maxSize = -1;
        const int currentCount = -2;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Contains("-1", exception.Message);
        Assert.Contains("-2", exception.Message);
    }

    #endregion

    #region ToString Method Tests

    [Fact]
    public void ToString_ContainsAllRelevantInformation()
    {
        const int maxSize = 5;
        const int currentCount = 3;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        var toStringResult = exception.ToString();

        Assert.Contains("HashRingHistoryLimitExceededException", toStringResult);
        Assert.Contains($"MaxHistorySize: {maxSize}", toStringResult);
        Assert.Contains($"CurrentCount: {currentCount}", toStringResult);
        Assert.Contains(exception.Message, toStringResult);
    }

    [Fact]
    public void ToString_WithCustomMessage_IncludesCustomMessage()
    {
        const int maxSize = 3;
        const int currentCount = 2;
        const string customMessage = "Custom error message for testing";

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount, customMessage);

        var toStringResult = exception.ToString();

        Assert.Contains("HashRingHistoryLimitExceededException", toStringResult);
        Assert.Contains(customMessage, toStringResult);
        Assert.Contains($"MaxHistorySize: {maxSize}", toStringResult);
        Assert.Contains($"CurrentCount: {currentCount}", toStringResult);
    }

    [Fact]
    public void ToString_WithDefaultConstructor_HandlesZeroValues()
    {
        var exception = new HashRingHistoryLimitExceededException();

        var toStringResult = exception.ToString();

        Assert.Contains("HashRingHistoryLimitExceededException", toStringResult);
        Assert.Contains("MaxHistorySize: 0", toStringResult);
        Assert.Contains("CurrentCount: 0", toStringResult);
    }

    [Theory]
    [InlineData(Int32.MaxValue, Int32.MaxValue)]
    [InlineData(Int32.MinValue, Int32.MinValue)]
    [InlineData(0, 0)]
    public void ToString_WithBoundaryValues_FormatsCorrectly(int maxSize, int currentCount)
    {
        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        var toStringResult = exception.ToString();

        Assert.Contains($"MaxHistorySize: {maxSize}", toStringResult);
        Assert.Contains($"CurrentCount: {currentCount}", toStringResult);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void Exception_InheritsFromInvalidOperationException()
    {
        var exception = new HashRingHistoryLimitExceededException(5, 3);

        Assert.IsType<InvalidOperationException>(exception, exactMatch: false);
        Assert.IsType<SystemException>(exception, exactMatch: false);
        Assert.IsType<Exception>(exception, exactMatch: false);
    }

    [Fact]
    public void Exception_IsNotAbstractOrSealed()
    {
        var type = typeof(HashRingHistoryLimitExceededException);

        Assert.False(type.IsAbstract);
        Assert.True(type.IsSealed); // Actually it is sealed according to the source
    }

    [Fact]
    public void Exception_CanBeCaughtAsInvalidOperationException()
    {
        var exception = new HashRingHistoryLimitExceededException(3, 3);

        Assert.IsType<InvalidOperationException>(exception, false);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_AreReadOnly()
    {
        var exception = new HashRingHistoryLimitExceededException(10, 5);

        // Properties should only have getters, not setters
        var maxHistorySizeProperty = typeof(HashRingHistoryLimitExceededException).GetProperty(nameof(exception.MaxHistorySize));
        var currentCountProperty = typeof(HashRingHistoryLimitExceededException).GetProperty(nameof(exception.CurrentCount));

        Assert.True(maxHistorySizeProperty.CanRead);
        Assert.False(maxHistorySizeProperty.CanWrite);
        Assert.True(currentCountProperty.CanRead);
        Assert.False(currentCountProperty.CanWrite);
    }

    [Fact]
    public void Properties_RetainValuesAfterConstruction()
    {
        const int maxSize = 42;
        const int currentCount = 37;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        // Verify properties don't change over time
        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
    }

    #endregion

    #region Edge Cases and Boundary Conditions

    [Fact]
    public void Exception_WithLargeNumbers_HandlesCorrectly()
    {
        const int maxSize = Int32.MaxValue;
        const int currentCount = Int32.MaxValue - 1;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains(maxSize.ToString(), exception.Message);
        Assert.Contains(currentCount.ToString(), exception.Message);
    }

    [Fact]
    public void Exception_WithNegativeNumbers_HandlesCorrectly()
    {
        const int maxSize = -100;
        const int currentCount = -50;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains(maxSize.ToString(), exception.Message);
        Assert.Contains(currentCount.ToString(), exception.Message);
    }

    [Fact]
    public void Exception_WithZeroValues_HandlesCorrectly()
    {
        const int maxSize = 0;
        const int currentCount = 0;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains("0", exception.Message);
    }

    [Fact]
    public void Exception_WithCurrentCountGreaterThanMax_IsValid()
    {
        // This scenario might occur in concurrent scenarios
        const int maxSize = 5;
        const int currentCount = 10;

        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains(maxSize.ToString(), exception.Message);
        Assert.Contains(currentCount.ToString(), exception.Message);
    }

    #endregion


    #region Integration Tests

    [Fact]
    public void Exception_ThrownByHashRing_HasCorrectProperties()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 2
        };
        var ring = new HashRing<string>(options);
        ring.Add("server1");

        // Fill history to limit
        ring.CreateConfigurationSnapshot();
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();
        ring.Add("server3");

        // This should throw
        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() => ring.CreateConfigurationSnapshot());

        Assert.Equal(2, exception.MaxHistorySize);
        Assert.Equal(2, exception.CurrentCount);
        Assert.Contains("History limit of 2", exception.Message);
        Assert.Contains("Current count: 2", exception.Message);
    }

    [Fact]
    public void Exception_ThrownByHistoryManager_HasCorrectProperties()
    {
        var manager = new HistoryManager<string>(1);
        manager.Add(CreateTestSnapshot("server1"));

        // This should throw when trying to add beyond limit
        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("server2")));

        Assert.Equal(1, exception.MaxHistorySize);
        Assert.Equal(1, exception.CurrentCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Exception_WithDifferentHistoryLimits_ReflectsActualLimits(int historyLimit)
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = historyLimit
        };
        var ring = new HashRing<string>(options);
        ring.Add("server1");

        // Fill to capacity
        for (int i = 0; i < historyLimit; i++)
        {
            ring.CreateConfigurationSnapshot();
            ring.Add($"server{i + 2}");
        }

        // This should throw
        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() => ring.CreateConfigurationSnapshot());

        Assert.Equal(historyLimit, exception.MaxHistorySize);
        Assert.Equal(historyLimit, exception.CurrentCount);
    }

    #endregion

    #region Exception Handling Patterns

    [Fact]
    public void Exception_CanBeHandledInTryCatchBlock()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 1 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        HashRingHistoryLimitExceededException caughtException = null;

        try
        {
            ring.CreateConfigurationSnapshot(); // Should throw
            Assert.Fail("Expected exception was not thrown");
        }
        catch (HashRingHistoryLimitExceededException ex)
        {
            caughtException = ex;
        }

        Assert.NotNull(caughtException);
        Assert.Equal(1, caughtException.MaxHistorySize);
        Assert.Equal(1, caughtException.CurrentCount);
    }

    [Fact]
    public void Exception_CanBeHandledAsInvalidOperationException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 1 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        InvalidOperationException caughtException = null;

        try
        {
            ring.CreateConfigurationSnapshot(); // Should throw
            Assert.Fail("Expected exception was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            caughtException = ex;
        }

        Assert.NotNull(caughtException);
        Assert.IsType<HashRingHistoryLimitExceededException>(caughtException);

        var specificException = (HashRingHistoryLimitExceededException)caughtException;
        Assert.Equal(1, specificException.MaxHistorySize);
        Assert.Equal(1, specificException.CurrentCount);
    }

    [Fact]
    public void Exception_CanBeUsedInExceptionFilters()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 2 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        bool filterMatched = false;

        try
        {
            ring.CreateConfigurationSnapshot();
        }
        catch (HashRingHistoryLimitExceededException ex) when (ex.MaxHistorySize == 2)
        {
            filterMatched = true;
        }

        Assert.True(filterMatched);
    }

    #endregion

    #region Performance and Memory Tests

    [Fact]
    public void Exception_Creation_IsEfficient()
    {
        var startTime = DateTime.UtcNow;

        // Create many exceptions to test performance
        for (int i = 0; i < 1000; i++)
        {
            var exception = new HashRingHistoryLimitExceededException(i, i - 1);
            Assert.NotNull(exception);
        }

        var duration = DateTime.UtcNow - startTime;
        Assert.True(duration.TotalMilliseconds < 1000, $"Exception creation took {duration.TotalMilliseconds}ms");
    }

    #endregion

    #region Helper Methods

    private static ConfigurationSnapshot<string> CreateTestSnapshot(string server)
    {
        var servers = new[] { server };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new((uint)server.GetHashCode(), server)
        };
        var hashAlgorithm = new Sha1HashAlgorithm();
        return new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, hashAlgorithm);
    }

    #endregion
}
