namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Provides access to static assets (version files and directories).
/// Implementations can use disk, blob storage, or other backends.
/// </summary>
public interface IStaticAssetsProvider
{
    /// <summary>
    /// Reads the version.json file from the specified target directory.
    /// </summary>
    /// <param name="targetDirectory">The target directory (e.g., "app")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The version file payload, or null if not found</returns>
    Task<VersionFilePayload?> ReadVersionFileAsync(string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the target directory and its version.json exist.
    /// For disk: checks/creates directory.
    /// For blob: may fetch version.json from remote storage.
    /// </summary>
    /// <param name="targetDirectory">The target directory (e.g., "app")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureDirectoryExistsAsync(string targetDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a specific version subdirectory exists.
    /// For disk: checks/creates directory.
    /// For blob: may fetch version assets from remote storage.
    /// </summary>
    /// <param name="targetDirectory">The target directory (e.g., "app")</param>
    /// <param name="version">The version (e.g., "v1.0.0")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureVersionDirectoryExistsAsync(string targetDirectory, string version, CancellationToken cancellationToken = default);
}
