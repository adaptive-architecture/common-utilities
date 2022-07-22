namespace AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

/// <summary>
/// Abstraction for UUID provider.
/// </summary>
public interface IUuidProvider
{
    /// <summary>
    /// Generate a new UUID.
    /// </summary>
    string New();
}
