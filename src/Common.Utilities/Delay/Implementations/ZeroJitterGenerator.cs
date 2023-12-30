using AdaptArch.Common.Utilities.Delay.Contracts;

namespace AdaptArch.Common.Utilities.Delay.Implementations;

/// <summary>
/// An implementation of <see cref="IJitterGenerator"/> that return <see cref="TimeSpan.Zero"/> all the time.
/// </summary>
public class ZeroJitterGenerator : IJitterGenerator
{
    /// <summary>
    /// A singleton instance of <see cref="ZeroJitterGenerator"/>.
    /// </summary>
    public static readonly IJitterGenerator Instance = new ZeroJitterGenerator();

    /// <inheritdoc />
    public TimeSpan New(TimeSpan baseValue, float lowerBoundary, float upperBoundary) => TimeSpan.Zero;
}
