namespace AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

/// <summary>
/// Abstraction for date time provider.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets a <see cref="T:System.DateTime" /> object that is set to the current date and time on this computer, expressed as the Coordinated Universal Time (UTC).</summary>
    /// <returns>An object whose value is the current UTC date and time.</returns>
    DateTime UtcNow { get; }
}
