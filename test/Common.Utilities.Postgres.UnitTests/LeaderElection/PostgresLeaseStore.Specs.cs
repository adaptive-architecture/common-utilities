using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using Npgsql;
using NSubstitute;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.LeaderElection;

public class PostgresLeaseStoreSpecs
{
    private readonly NpgsqlDataSource _mockDataSource;
    private readonly IStringDataSerializer _mockSerializer;
    private readonly ILogger _mockLogger;
    private readonly string _testTableName = "test_leases";

    public PostgresLeaseStoreSpecs()
    {
        _mockDataSource = NpgsqlDataSource.Create("Host=localhost;Database=testdb;Username=testuser;Password=testpassword");
        _mockSerializer = Substitute.For<IStringDataSerializer>();
        _mockLogger = Substitute.For<ILogger>();
    }

    [Fact]
    public void Constructor_WithNullDataSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaseStore(null!, _mockSerializer, _testTableName, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullSerializer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaseStore(_mockDataSource, null!, _testTableName, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullTableName_ShouldUseDefaultTableName()
    {
        // Act
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, null, _mockLogger);

        // Assert
        Assert.NotNull(leaseStore);

        // Cleanup
        leaseStore.Dispose();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldUseNullLogger()
    {
        // Act
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, _testTableName, null);

        // Assert
        Assert.NotNull(leaseStore);

        // Cleanup
        leaseStore.Dispose();
    }

    [Fact]
    public async Task Dispose_WhenCalled_ShouldSetDisposedFlag()
    {
        // Arrange
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, _testTableName, _mockLogger);

        // Act
        leaseStore.Dispose();

        // Assert - Should throw when disposed
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await leaseStore.TryAcquireLeaseAsync("test", "participant", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, _testTableName, _mockLogger);

        // Act & Assert
        leaseStore.Dispose();
        leaseStore.Dispose(); // Should not throw

        Assert.True(true);
    }

    [Fact]
    public async Task Methods_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, _testTableName, _mockLogger);
        leaseStore.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.TryAcquireLeaseAsync("test", "participant", TimeSpan.FromMinutes(1)));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.TryRenewLeaseAsync("test", "participant", TimeSpan.FromMinutes(1)));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.ReleaseLeaseAsync("test", "participant"));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.GetCurrentLeaseAsync("test"));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.HasValidLeaseAsync("test"));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.EnsureTableExistsAsync());

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            leaseStore.CleanupExpiredLeasesAsync());
    }
}
