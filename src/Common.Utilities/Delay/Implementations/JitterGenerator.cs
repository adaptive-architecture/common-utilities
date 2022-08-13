using AdaptArch.Common.Utilities.Delay.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.Delay.Implementations;

/// <summary>
/// Simple jitter generator.
/// </summary>
public class JitterGenerator: IJitterGenerator
{
    private const float Percent100 = 100;
    private readonly IRandomGenerator _randomGenerator;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="randomGenerator">A random generator.</param>
    public JitterGenerator(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }

    /// <inheritdoc />
    public TimeSpan New(TimeSpan baseValue, float lowerBoundary, float upperBoundary)
    {
        if (lowerBoundary < 0 || lowerBoundary > 1)
            throw new ArgumentOutOfRangeException(nameof(lowerBoundary), "The lower boundary should be in the [0, 1] interval.");

        if (upperBoundary < 0 || upperBoundary > 1)
            throw new ArgumentOutOfRangeException(nameof(upperBoundary), "The upper boundary should be in the [0, 1] interval");

        if (lowerBoundary > upperBoundary)
            throw new ArgumentOutOfRangeException(nameof(lowerBoundary), "The lower boundary should not be larger than the upper boundary.");

        return baseValue * GetJitterPercentage(lowerBoundary, upperBoundary);
    }

    private float GetJitterPercentage(float lowerBoundary, float upperBoundary)
    {
        var percentage = _randomGenerator.Next(Convert.ToInt32(lowerBoundary * Percent100), Convert.ToInt32(upperBoundary * Percent100));
        var sign = _randomGenerator.Next(0, 1) == 0 ? -1 : 1;
        return percentage * sign / Percent100;
    }
}
