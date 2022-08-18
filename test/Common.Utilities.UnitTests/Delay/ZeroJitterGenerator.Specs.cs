using AdaptArch.Common.Utilities.Delay.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.Delay;

public class ZeroJitterGeneratorSpec
{
    [Fact]
    public void Should_Return_Zero()
    {
        var generator = new ZeroJitterGenerator();

        Assert.Equal(TimeSpan.Zero, generator.New(TimeSpan.FromSeconds(120), 0.3f, 0.9f));
    }

    [Fact]
    public void Should_Have_A_Singleton_Static_Instance()
    {
        Assert.Same(ZeroJitterGenerator.Instance, ZeroJitterGenerator.Instance);
    }
}
