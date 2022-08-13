namespace AdaptArch.Common.Utilities.Delay.Contracts;

/// <summary>
/// A utility to generate delays.
/// </summary>
public interface IDelayGenerator
{
    /// <summary>
    /// Generate the delay durations.
    /// </summary>
    IEnumerable<TimeSpan> GetDelays();
}
