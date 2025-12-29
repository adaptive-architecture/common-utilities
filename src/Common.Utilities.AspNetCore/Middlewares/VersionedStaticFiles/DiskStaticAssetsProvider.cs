using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Disk-based implementation of IStaticAssetsProvider.
/// Reads static assets from the local file system.
/// </summary>
public partial class DiskStaticAssetsProvider : IStaticAssetsProvider
{
    private readonly string _baseDirectory;
    private readonly ILogger<DiskStaticAssetsProvider> _logger;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Version file not found at path: {FilePath}")]
    private partial void LogVersionFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating directory: {DirectoryPath}")]
    private partial void LogCreatingDirectory(string directoryPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating version directory: {VersionDirectoryPath}")]
    private partial void LogCreatingVersionDirectory(string versionDirectoryPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error reading or deserializing version file at path: {FilePath}")]
    private partial void LogVersionFileError(Exception ex, string filePath);

    /// <summary>
    /// Initializes a new instance of the DiskStaticAssetsProvider class.
    /// </summary>
    /// <param name="options">The middleware options containing the base directory.</param>
    /// <param name="logger">The logger instance.</param>
    public DiskStaticAssetsProvider(MiddlewareOptions options, ILogger<DiskStaticAssetsProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _baseDirectory = options.StaticFilesDirectory;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<VersionFilePayload?> ReadVersionFileAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        var filePath = GetVersionFilePath(targetDirectory);

        if (!File.Exists(filePath))
        {
            LogVersionFileNotFound(filePath);
            return Task.FromResult<VersionFilePayload?>(null);
        }

        return ReadVersionAsync(filePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task EnsureDirectoryExistsAsync(string targetDirectory, CancellationToken cancellationToken = default)
    {
        var directoryPath = GetDirectoryPath(targetDirectory);

        if (!Directory.Exists(directoryPath))
        {
            LogCreatingDirectory(directoryPath);
            Directory.CreateDirectory(directoryPath);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnsureVersionDirectoryExistsAsync(string targetDirectory, string version, CancellationToken cancellationToken = default)
    {
        var versionDirectoryPath = GetVersionDirectoryPath(targetDirectory, version);

        if (!Directory.Exists(versionDirectoryPath))
        {
            LogCreatingVersionDirectory(versionDirectoryPath);
            Directory.CreateDirectory(versionDirectoryPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads and deserializes the version file.
    /// </summary>
    /// <param name="filePath">The full path to the version file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected async Task<VersionFilePayload?> ReadVersionAsync(string filePath, CancellationToken cancellationToken)
    {
        VersionFilePayload? result = null;
        try
        {
            await using var jsonStream = File.OpenRead(filePath);
            var payload = await JsonSerializer.DeserializeAsync(jsonStream,
                typeof(VersionFilePayload),
                DefaultJsonSerializerContext.Default,
                cancellationToken);

            result = payload == null
                ? null
                : (VersionFilePayload)payload;
        }
        catch (Exception ex)
        {
            LogVersionFileError(ex, filePath);
        }

        return result;
    }

    /// <summary>
    /// Gets the full path to the version file for the specified target directory.
    /// </summary>
    /// <param name="targetDirectory">The target directory.</param>
    protected string GetVersionFilePath(string targetDirectory) =>
        Path.Combine(_baseDirectory, targetDirectory, "version.json");

    /// <summary>
    /// Gets the full path to the target directory.
    /// </summary>
    /// <param name="targetDirectory">The target directory.</param>
    protected string GetDirectoryPath(string targetDirectory) =>
        Path.Combine(_baseDirectory, targetDirectory);

    /// <summary>
    /// Gets the full path to the versioned directory for the specified target directory and version.
    /// </summary>
    /// <param name="targetDirectory">The target directory.</param>
    /// <param name="version">The version string.</param>
    protected string GetVersionDirectoryPath(string targetDirectory, string version) =>
        Path.Combine(_baseDirectory, targetDirectory, version);
}
