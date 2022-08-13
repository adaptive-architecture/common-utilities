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
    Task<IReadOnlyDictionary<string, string>> ReadDataAsync(CancellationToken cancellationToken = default);
}
