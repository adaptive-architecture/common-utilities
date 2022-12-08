using AdaptArch.Common.Utilities.Delay.Contracts;
using AdaptArch.Common.Utilities.Delay.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.Delay;

public class DelayGeneratorSpecs
{
    [Fact]
    public void Should_Throw_If_Unknown_Delay_Type()
    {
        var delayGenerator = new DelayGenerator(new DelayGeneratorOptions {
            DelayType = DelayType.Unknown
        });

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = delayGenerator.GetDelays().First());
    }

    [Fact]
    public void Should_Return_As_Many_Delays_As_Requested()
    {
        var options = new DelayGeneratorOptions
        {
            MaxIterations = 13,
            JitterLowerBoundary = 0,
            JitterUpperBoundary= 1
        };
        var delayGenerator = new DelayGenerator(options);

        var delays = delayGenerator.GetDelays().ToArray();

        Assert.Equal(options.MaxIterations, delays.Length);
    }

    [Fact]
    public void Should_Return_Constant_Delays()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(1),
            DelayType = DelayType.Constant
        };
        var generator = new DelayGenerator(options);

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval, delay);
        }
    }

    [Fact]
    public void Should_Return_Linear_Increasing_Delays()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(2),
            DelayType = DelayType.Linear
        };
        var generator = new DelayGenerator(options);
        var it = 0;

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval * it, delay);
            it++;
        }
    }

    [Fact]
    public void Should_Return_Power_Of_2_Increasing_Delays()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(3),
            DelayType = DelayType.PowerOf2
        };
        var generator = new DelayGenerator(options);
        var it = 0;

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval * Math.Pow(it, 2), delay);
            it++;
        }
    }

    [Fact]
    public void Should_Return_Power_Of_E_Increasing_Delays()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(4),
            DelayType = DelayType.PowerOfE
        };
        var generator = new DelayGenerator(options);
        var it = 0;

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval * Math.Pow(it, Math.E), delay);
            it++;
        }
    }

    [Fact]
    public void Should_Start_From_A_Given_Current_Iteration()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(10),
            DelayType = DelayType.Linear,
            Current = 1
        };
        var generator = new DelayGenerator(options);
        var it = 1;

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval * it, delay);
            it++;
        }
    }

    [Fact]
    public void Should_Return_Constant_Delays_With_Jitter()
    {
        var options = new DelayGeneratorOptions
        {
            DelayInterval = TimeSpan.FromSeconds(11),
            DelayType = DelayType.Constant,
            JitterGenerator = new ConstantJitterGenerator()
        };
        var generator = new DelayGenerator(options);

        foreach (var delay in generator.GetDelays())
        {
            Assert.Equal(options.DelayInterval + TimeSpan.FromSeconds(1), delay);
        }
    }

    private class ConstantJitterGenerator: IJitterGenerator
    {
        public TimeSpan New(TimeSpan baseValue, float lowerBoundary, float upperBoundary) => TimeSpan.FromSeconds(1);
    }
}
