using AdaptArch.Common.Utilities.Delay.Contracts;
using AdaptArch.Common.Utilities.Delay.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.Delay;

public class DelayGeneratorOptionsSpecs
{
    [Fact]
    public void Should_Have_Default_Values()
    {
        var options = new DelayGeneratorOptions();

        Assert.Equal(5, options.MaxIterations);
        Assert.Equal(0, options.Current);
        Assert.Equal(TimeSpan.Zero, options.DelayInterval);
        Assert.Equal(DelayType.Constant, options.DelayType);
        Assert.Equal(ZeroJitterGenerator.Instance, options.JitterGenerator);
        Assert.Equal(0.02f, options.JitterLowerBoundary);
        Assert.Equal(0.27f, options.JitterUpperBoundary);
    }
}
