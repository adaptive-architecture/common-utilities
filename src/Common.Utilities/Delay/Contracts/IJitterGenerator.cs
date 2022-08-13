namespace AdaptArch.Common.Utilities.Delay.Contracts;

/// <summary>
/// An utility to generate a jitter value.
/// </summary>
public interface IJitterGenerator
{
    /// <summary>
    /// Generate a jitter that has a value which is a percentage of the base value. The percentage (in absolute) is between the lower and the upper boundary.
    /// <remarks>This will return a positive or negative value.</remarks>
    /// </summary>
    /// <param name="baseValue">The base value.</param>
    /// <param name="lowerBoundary">The minimum value of the percentage. This must be a positive value between 0 and 1.</param>
    /// <param name="upperBoundary">The maximum value of the percentage. This must be a positive value between 0 and 1.</param>
    TimeSpan New(TimeSpan baseValue, float lowerBoundary, float upperBoundary);
}
