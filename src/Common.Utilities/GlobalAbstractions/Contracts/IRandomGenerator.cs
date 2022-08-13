namespace AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

/// <summary>
/// An abstraction for a random generator.
/// </summary>
public interface IRandomGenerator
{
    /// <summary>
    /// Generate a random value between the minimum and the maximum.
    /// </summary>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    int Next(int minValue, int maxValue);
}
