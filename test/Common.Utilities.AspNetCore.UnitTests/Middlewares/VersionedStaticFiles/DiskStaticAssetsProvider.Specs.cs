using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.VersionedStaticFiles;

public sealed class DiskStaticAssetsProviderSpecs : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly ILogger<DiskStaticAssetsProvider> _logger;
    private readonly MiddlewareOptions _options;

    public DiskStaticAssetsProviderSpecs()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), $"DiskStaticAssetsProviderTests_{Guid.NewGuid()}");

        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<DiskStaticAssetsProvider>>();

        _options = new MiddlewareOptions
        {
            StaticFilesDirectory = _testBaseDirectory
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBaseDirectory))
        {
            Directory.Delete(_testBaseDirectory, true);
        }
    }

    [Fact]
    public async Task EnsureDirectoryExistsAsync_Should_Create_Directory_When_It_Does_Not_Exist()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";
        var expectedPath = Path.Combine(_testBaseDirectory, targetDirectory);

        // Ensure directory doesn't exist
        if (Directory.Exists(expectedPath))
        {
            Directory.Delete(expectedPath, true);
        }

        // Act
        await provider.EnsureDirectoryExistsAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(Directory.Exists(expectedPath));
    }

    [Fact]
    public async Task EnsureDirectoryExistsAsync_Should_Not_Create_Directory_When_It_Already_Exists()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";
        var expectedPath = Path.Combine(_testBaseDirectory, targetDirectory);

        // Pre-create the directory
        Directory.CreateDirectory(expectedPath);
        var createdTime = Directory.GetCreationTimeUtc(expectedPath);

        // Act
        await provider.EnsureDirectoryExistsAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert - Directory still exists and hasn't been recreated
        Assert.True(Directory.Exists(expectedPath));
        Assert.Equal(createdTime, Directory.GetCreationTimeUtc(expectedPath));
    }

    [Fact]
    public async Task EnsureVersionDirectoryExistsAsync_Should_Create_Version_Directory_When_It_Does_Not_Exist()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";
        const string version = "v1.0.0";
        var expectedPath = Path.Combine(_testBaseDirectory, targetDirectory, version);

        // Ensure directory doesn't exist
        if (Directory.Exists(expectedPath))
        {
            Directory.Delete(expectedPath, true);
        }

        // Act
        await provider.EnsureVersionDirectoryExistsAsync(targetDirectory, version, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(Directory.Exists(expectedPath));
    }

    [Fact]
    public async Task EnsureVersionDirectoryExistsAsync_Should_Not_Create_Directory_When_It_Already_Exists()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";
        const string version = "v1.0.0";
        var expectedPath = Path.Combine(_testBaseDirectory, targetDirectory, version);

        // Pre-create the directory
        Directory.CreateDirectory(expectedPath);
        var createdTime = Directory.GetCreationTimeUtc(expectedPath);

        // Act
        await provider.EnsureVersionDirectoryExistsAsync(targetDirectory, version, TestContext.Current.CancellationToken);

        // Assert - Directory still exists and hasn't been recreated
        Assert.True(Directory.Exists(expectedPath));
        Assert.Equal(createdTime, Directory.GetCreationTimeUtc(expectedPath));
    }

    [Fact]
    public async Task ReadVersionFileAsync_Should_Return_Null_When_Version_File_Does_Not_Exist()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";

        // Act
        var result = await provider.ReadVersionFileAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadVersionFileAsync_Should_Return_Payload_When_Version_File_Exists()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";
        const string version = "v1.2.3";
        var timestamp = DateTime.UtcNow;

        // Create directory and version file
        var directoryPath = Path.Combine(_testBaseDirectory, targetDirectory);
        Directory.CreateDirectory(directoryPath);

        var versionFilePath = Path.Combine(directoryPath, "version.json");
        var versionContent = $$"""
        {
            "Version": "{{version}}",
            "Timestamp": "{{timestamp:O}}"
        }
        """;
        await File.WriteAllTextAsync(versionFilePath, versionContent, TestContext.Current.CancellationToken);

        // Act
        var result = await provider.ReadVersionFileAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(version, result.Version);
        Assert.Equal(timestamp.ToString("O"), result.Timestamp.ToString("O"));
    }

    [Fact]
    public async Task ReadVersionFileAsync_Should_Return_Null_When_File_Contains_Invalid_Json()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";

        // Create directory and invalid version file
        var directoryPath = Path.Combine(_testBaseDirectory, targetDirectory);
        Directory.CreateDirectory(directoryPath);

        var versionFilePath = Path.Combine(directoryPath, "version.json");
        await File.WriteAllTextAsync(versionFilePath, "invalid json content {{{", TestContext.Current.CancellationToken);

        // Act
        var result = await provider.ReadVersionFileAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadVersionFileAsync_Should_Return_Null_When_File_Contains_Null_Json()
    {
        // Arrange
        var provider = new DiskStaticAssetsProvider(_options, _logger);
        const string targetDirectory = "app";

        // Create directory and null content file
        var directoryPath = Path.Combine(_testBaseDirectory, targetDirectory);
        Directory.CreateDirectory(directoryPath);

        var versionFilePath = Path.Combine(directoryPath, "version.json");
        await File.WriteAllTextAsync(versionFilePath, "null", TestContext.Current.CancellationToken);

        // Act
        var result = await provider.ReadVersionFileAsync(targetDirectory, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DiskStaticAssetsProvider(null!, _logger));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DiskStaticAssetsProvider(_options, null!));

        Assert.Equal("logger", exception.ParamName);
    }
}
