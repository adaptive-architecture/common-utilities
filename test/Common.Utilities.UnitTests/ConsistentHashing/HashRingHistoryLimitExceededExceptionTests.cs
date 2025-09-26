namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using Xunit;

public sealed class HashRingHistoryLimitExceededExceptionTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        const int maxHistorySize = 3;
        const int currentCount = 3;

        var exception = new HashRingHistoryLimitExceededException(maxHistorySize, currentCount);

        Assert.Equal(maxHistorySize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains($"History limit of {maxHistorySize}", exception.Message);
        Assert.Contains($"Current count: {currentCount}", exception.Message);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    public void Constructor_WithVariousLimits_FormatsMessageCorrectly(int maxSize, int currentCount)
    {
        var exception = new HashRingHistoryLimitExceededException(maxSize, currentCount);

        Assert.Equal(maxSize, exception.MaxHistorySize);
        Assert.Equal(currentCount, exception.CurrentCount);
        Assert.Contains($"History limit of {maxSize}", exception.Message);
        Assert.Contains($"Current count: {currentCount}", exception.Message);
        Assert.Contains("Cannot create configuration snapshot", exception.Message);
        Assert.Contains("would be exceeded", exception.Message);
    }

    [Fact]
    public void Exception_InheritsFromInvalidOperationException()
    {
        var exception = new HashRingHistoryLimitExceededException(3, 3);

        Assert.IsType<InvalidOperationException>(exception, exactMatch: false);
    }

}
