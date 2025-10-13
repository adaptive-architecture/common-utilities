namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class HashRingOptionsVersionTests
{
    [Fact]
    public void MaxHistorySize_DefaultsTo3()
    {
        var options = new HashRingOptions();

        Assert.Equal(3, options.MaxHistorySize);
    }

    [Fact]
    public void MaxHistorySize_CanBeSetToCustomValue()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 5
        };

        Assert.Equal(5, options.MaxHistorySize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void MaxHistorySize_AcceptsValidValues(int historySize)
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = historySize
        };

        Assert.Equal(historySize, options.MaxHistorySize);
    }
}
