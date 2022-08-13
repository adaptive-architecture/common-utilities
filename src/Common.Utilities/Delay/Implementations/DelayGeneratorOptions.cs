using AdaptArch.Common.Utilities.Delay.Contracts;

namespace AdaptArch.Common.Utilities.Delay.Implementations;

/// <summary>
/// Configuration options for <see cref="DelayGenerator"/>.
/// </summary>
public class DelayGeneratorOptions
{
    /// <summary>
    /// The maximum number of iterations to generate delays.
    /// The default value is: 5.
    /// </summary>
    public int MaxIterations { get; set; } = 5;

    /// <summary>
    /// The current start delay.
    /// The default value is: 0. Which will return a delay close to <see cref="TimeSpan.Zero"/> depending on the jitter.
    /// </summary>
    public int Current { get; set; } = 0;

    /// <summary>
    /// The base interval to delay upon.
    /// </summary>
    public TimeSpan DelayInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The type of delay.
    /// </summary>
    public DelayType DelayType { get; set; } = DelayType.Constant;

    /// <summary>
    /// The jitter generator.
    /// </summary>
    public IJitterGenerator JitterGenerator { get; set; } = new ZeroJitterGenerator();

    /// <summary>
    /// The value used to the "lowerBoundary" parameter of the <see cref="IJitterGenerator.New"/> method.
    /// </summary>
    public float JitterLowerBoundary { get; set; } = 0.03f;

    /// <summary>
    /// The value used to the "upperBoundary" parameter of the <see cref="IJitterGenerator.New"/> method.
    /// </summary>
    public float JitterUpperBoundary { get; set; } = 0.4f;
}
