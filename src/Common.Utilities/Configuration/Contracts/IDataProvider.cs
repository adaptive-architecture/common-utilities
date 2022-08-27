namespace AdaptArch.Common.Utilities.Configuration.Contracts;

/// <summary>
/// A data configuration data provider.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Read the raw configuration data.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<IReadOnlyDictionary<string, string?>> ReadDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Read the hash for the current state of the configuration data.
    /// If the hash code is the same as the previous version the call to <see cref="ReadDataAsync"/> should not be done.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<string> GetHashAsync(CancellationToken cancellationToken = default);
}
