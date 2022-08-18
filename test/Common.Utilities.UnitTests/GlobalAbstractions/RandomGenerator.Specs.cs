using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.GlobalAbstractions;

public class RandomGeneratorSpecs
{
    [Fact]
    public void Should_Have_A_Singleton_Static_Instance()
    {
        Assert.Same(RandomGenerator.Instance, RandomGenerator.Instance);
    }

    [Fact]
    public void Should_Return_A_Random_Value_Within_The_Interval()
    {
        var generator = new RandomGenerator(new Random(0));
        Assert.Equal(72, generator.Next(0, 100));
    }
}
