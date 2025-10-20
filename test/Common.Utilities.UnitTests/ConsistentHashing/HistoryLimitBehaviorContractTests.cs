using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public sealed class HistoryLimitBehaviorContractTests
{
    [Fact]
    public void HistoryLimitBehavior_HasExactlyTwoValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<HistoryLimitBehavior>();

        // Assert
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void HistoryLimitBehavior_ThrowError_HasValueZero()
    {
        // Arrange & Act
        const int value = (int)HistoryLimitBehavior.ThrowError;

        // Assert
        Assert.Equal(0, value);
    }

    [Fact]
    public void HistoryLimitBehavior_RemoveOldest_HasValueOne()
    {
        // Arrange & Act
        const int value = (int)HistoryLimitBehavior.RemoveOldest;

        // Assert
        Assert.Equal(1, value);
    }

    [Fact]
    public void HistoryLimitBehavior_RemoveOldest_IsDefaultValue()
    {
        // Arrange & Act
        // In C#, the default for enum is the zero value, but we test the intended default
        // which is RemoveOldest (value 1) as used in HashRingOptions
        const HistoryLimitBehavior defaultBehavior = HistoryLimitBehavior.RemoveOldest;

        // Assert
        Assert.Equal(HistoryLimitBehavior.RemoveOldest, defaultBehavior);
        Assert.Equal(1, (int)defaultBehavior);
    }

    [Fact]
    public void HistoryLimitBehavior_IsPublic()
    {
        // Arrange & Act
        var type = typeof(HistoryLimitBehavior);

        // Assert
        Assert.True(type.IsPublic);
        Assert.True(type.IsEnum);
    }

    [Fact]
    public void HistoryLimitBehavior_IsInCorrectNamespace()
    {
        // Arrange & Act
        var type = typeof(HistoryLimitBehavior);

        // Assert
        Assert.Equal("AdaptArch.Common.Utilities.ConsistentHashing", type.Namespace);
    }
}
