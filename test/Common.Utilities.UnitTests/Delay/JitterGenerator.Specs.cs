using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.Delay;

public class JitterGeneratorSpec
{
    private readonly JitterGenerator _jitterGenerator = new JitterGenerator(new RandomGenerator(new Random(0)));


    [Fact]
    public void Should_Throw_ArgumentOutOfRange_Exceptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = _jitterGenerator.New(TimeSpan.Zero, -0.01f, 0.99f));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = _jitterGenerator.New(TimeSpan.Zero, 1.01f, 0.99f));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = _jitterGenerator.New(TimeSpan.Zero, 0.01f, -0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = _jitterGenerator.New(TimeSpan.Zero, 0.01f, 1.01f));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = _jitterGenerator.New(TimeSpan.Zero, 0.05f, 0.01f));
    }

    [Fact]
    public void Should_Return_A_Jitter()
    {
        Assert.Equal(
            72.000002899999998,
            _jitterGenerator.New(TimeSpan.FromSeconds(100), 0, 1).TotalSeconds
        );

        Assert.Equal(
            -75.999999000000003,
            _jitterGenerator.New(TimeSpan.FromSeconds(100), 0, 1).TotalSeconds
        );

        Assert.Equal(
            -20.0000003,
            _jitterGenerator.New(TimeSpan.FromSeconds(100), 0, 1).TotalSeconds
        );
    }
}
