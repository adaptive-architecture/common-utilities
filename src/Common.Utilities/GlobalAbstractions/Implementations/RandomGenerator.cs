using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

/// <summary>
/// An implementation of <see cref="IRandomGenerator"/> based on <see cref="Random"/>.
/// <remarks>This should not be used for cryptographically secure implementations.</remarks>
/// </summary>
public class RandomGenerator : IRandomGenerator
{
    private static readonly Lazy<IRandomGenerator> LazyInstance = new(() => new RandomGenerator(new Random()));
    private readonly Random _random;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="random">Random generator.</param>
    public RandomGenerator(Random random)
    {
        _random = random;
    }

    /// <inheritdoc />
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    /// <summary>
    /// A global instance.
    /// </summary>
    public static IRandomGenerator Instance => LazyInstance.Value;
}
