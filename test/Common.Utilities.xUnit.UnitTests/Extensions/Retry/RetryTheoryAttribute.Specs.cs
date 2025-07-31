using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using Xunit;

namespace AdaptArch.Common.Utilities.xUnit.UnitTests.Extensions.Retry;

public class RetryTheoryAttributeSpecs
{
    [Fact]
    public void Constructor_ShouldSetDefaultMaxRetries()
    {
        var attribute = new RetryTheoryAttribute();

        Assert.Equal(3, attribute.MaxRetries);
    }

    [Fact]
    public void MaxRetries_ShouldBeSettable()
    {
        var attribute = new RetryTheoryAttribute
        {
            MaxRetries = 5
        };

        Assert.Equal(5, attribute.MaxRetries);
    }

    [Fact]
    public void Constructor_WithSourceInfo_ShouldCallBaseConstructor()
    {
        var attribute = new RetryTheoryAttribute("test.cs", 42);

        Assert.NotNull(attribute);
        Assert.Equal(3, attribute.MaxRetries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void MaxRetries_ShouldAcceptValidValues(int maxRetries)
    {
        var attribute = new RetryTheoryAttribute
        {
            MaxRetries = maxRetries
        };

        Assert.Equal(maxRetries, attribute.MaxRetries);
    }

    [Fact]
    public void MaxRetries_WithNegativeValue_ShouldStillSetValue()
    {
        var attribute = new RetryTheoryAttribute
        {
            MaxRetries = -1
        };

        Assert.Equal(-1, attribute.MaxRetries);
    }
}
