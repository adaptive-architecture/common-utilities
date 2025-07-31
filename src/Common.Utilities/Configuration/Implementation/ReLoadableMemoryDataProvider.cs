
using System.Collections.Frozen;
using AdaptArch.Common.Utilities.Configuration.Contracts;

namespace AdaptArch.Common.Utilities.Configuration.Implementation;

/// <summary>
/// An <see cref="IDataProvider"/> implementation that allows for reloading configuration data in memory.
/// This is useful for scenarios where configuration data may change at runtime and needs to be updated without
/// restarting the application.
/// It provides thread-safe access to the configuration data and allows setting new data while ensuring
/// that existing data can be retrieved safely.
/// </summary>
public class ReLoadableMemoryDataProvider : IDataProvider
{
    private FrozenDictionary<string, string?>? _data;
    private string? _hash;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReLoadableMemoryDataProvider"/> class with the specified initial data.
    /// The initial data is converted to a frozen dictionary for thread-safe access.
    /// </summary>
    /// <param name="initialData">The initial configuration data to be stored in the provider.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="initialData"/> is null.</exception>
    public ReLoadableMemoryDataProvider(IReadOnlyDictionary<string, string?> initialData)
    {
        ReloadDataCore(initialData);
    }

    /// <summary>
    /// Reloads the configuration data with new data.
    /// This method replaces the existing data with the new data provided.
    /// </summary>
    /// <param name="data">The new configuration data to be set.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> is null.</exception>
    public void ReloadData(IReadOnlyDictionary<string, string?> data) => ReloadDataCore(data);

    private void ReloadDataCore(IReadOnlyDictionary<string, string?> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data.ToFrozenDictionary();
        _hash = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc/>
    public Task<string> GetHashAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_hash!);

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, string?>> ReadDataAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<string, string?>>(_data!);
}
