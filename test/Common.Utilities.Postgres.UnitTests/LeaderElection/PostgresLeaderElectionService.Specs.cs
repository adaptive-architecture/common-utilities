using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using Npgsql;
using NSubstitute;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.LeaderElection;

public class PostgresLeaderElectionServiceSpecs
{
    private readonly NpgsqlDataSource _mockDataSource;
    private readonly IStringDataSerializer _mockSerializer;
    private readonly ILogger _mockLogger;
    private readonly string _electionName = "test-election";
    private readonly string _participantId = "participant-1";
    private readonly string _testTableName = "test_leases";

    public PostgresLeaderElectionServiceSpecs()
    {
        _mockDataSource = NpgsqlDataSource.Create("Host=localhost;Database=testdb;Username=testuser;Password=testpassword");
        _mockSerializer = Substitute.For<IStringDataSerializer>();
        _mockLogger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task Constructor_WithDataSource_ShouldInitializeCorrectly()
    {
        // Act
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            null,
            _mockLogger);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(_electionName, service.ElectionName);
        Assert.Equal(_participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithLeaseStore_ShouldInitializeCorrectly()
    {
        // Arrange
        var leaseStore = new PostgresLeaseStore(_mockDataSource, _mockSerializer, _testTableName, _mockLogger);

        // Act
        var service = new PostgresLeaderElectionService(
            leaseStore,
            _electionName,
            _participantId,
            null,
            _mockLogger);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(_electionName, service.ElectionName);
        Assert.Equal(_participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
        leaseStore.Dispose();
    }

    [Fact]
    public async Task Constructor_WithOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30)
        };

        // Act
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            options,
            _mockLogger);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(_electionName, service.ElectionName);
        Assert.Equal(_participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public void Constructor_WithNullDataSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaderElectionService(
                null!,
                _mockSerializer,
                _electionName,
                _participantId,
                _testTableName,
                null,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullSerializer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaderElectionService(
                _mockDataSource,
                null!,
                _electionName,
                _participantId,
                _testTableName,
                null,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullElectionName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PostgresLeaderElectionService(
                _mockDataSource,
                _mockSerializer,
                null!,
                _participantId,
                _testTableName,
                null,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullParticipantId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new PostgresLeaderElectionService(
                _mockDataSource,
                _mockSerializer,
                _electionName,
                null!,
                _testTableName,
                null,
                _mockLogger));
    }

    [Fact]
    public async Task Constructor_WithNullTableName_ShouldUseDefaultTableName()
    {
        // Act
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            null,
            null,
            _mockLogger);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(_electionName, service.ElectionName);
        Assert.Equal(_participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithNullLogger_ShouldNotThrow()
    {
        // Act
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            null,
            null);

        // Assert
        Assert.NotNull(service);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public void Constructor_WithNullLeaseStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaderElectionService(
                (PostgresLeaseStore)null!,
                _electionName,
                _participantId,
                null,
                _mockLogger));
    }

    [Fact]
    public async Task Properties_ShouldReturnCorrectValues()
    {
        // Arrange
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            null,
            _mockLogger);

        // Act & Assert
        Assert.Equal(_electionName, service.ElectionName);
        Assert.Equal(_participantId, service.ParticipantId);
        Assert.False(service.IsLeader);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_WhenCalled_ShouldSetDisposedState()
    {
        // Arrange
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            null,
            _mockLogger);

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.False(service.IsLeader);
    }

    [Fact]
    public async Task Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new PostgresLeaderElectionService(
            _mockDataSource,
            _mockSerializer,
            _electionName,
            _participantId,
            _testTableName,
            null,
            _mockLogger);

        // Act & Assert
        await service.DisposeAsync();
        await service.DisposeAsync(); // Should not throw
    }
}
