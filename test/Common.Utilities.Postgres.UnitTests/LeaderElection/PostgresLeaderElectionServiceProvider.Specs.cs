using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using Npgsql;
using NSubstitute;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.LeaderElection;

public class PostgresLeaderElectionServiceProviderSpecs
{
    private readonly NpgsqlDataSource _mockDataSource;
    private readonly IStringDataSerializer _mockSerializer;
    private readonly ILogger _mockLogger;
    private readonly string _testTableName = "test_leases";

    public PostgresLeaderElectionServiceProviderSpecs()
    {
        _mockDataSource = NpgsqlDataSource.Create("Host=localhost;Database=testdb;Username=testuser;Password=testpassword");
        _mockSerializer = Substitute.For<IStringDataSerializer>();
        _mockLogger = Substitute.For<ILogger>();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullDataSource_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaderElectionServiceProvider(
                null!,
                _mockSerializer,
                _testTableName,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullSerializer_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new PostgresLeaderElectionServiceProvider(
                _mockDataSource,
                null!,
                _testTableName,
                _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullTableName_ShouldUseDefaultTableName()
    {
        // Act
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            null,
            _mockLogger);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldNotThrow()
    {
        // Act
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            null);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task CreateElection_WithValidParameters_ShouldReturnPostgresLeaderElectionService()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        var service = provider.CreateElection(electionName, participantId);

        // Assert
        Assert.NotNull(service);
        _ = Assert.IsType<PostgresLeaderElectionService>(service);
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithOptions_ShouldReturnPostgresLeaderElectionService()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(2),
            RenewalInterval = TimeSpan.FromSeconds(30)
        };

        // Act
        var service = provider.CreateElection(electionName, participantId, options);

        // Assert
        Assert.NotNull(service);
        _ = Assert.IsType<PostgresLeaderElectionService>(service);
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithNullOptions_ShouldReturnPostgresLeaderElectionService()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        var service = provider.CreateElection(electionName, participantId, null);

        // Assert
        Assert.NotNull(service);
        _ = Assert.IsType<PostgresLeaderElectionService>(service);
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);

        // Cleanup
        await service.DisposeAsync();
    }

    [Fact]
    public void CreateElection_WithNullElectionName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string participantId = "participant-1";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            provider.CreateElection(null!, participantId));
    }

    [Fact]
    public void CreateElection_WithNullParticipantId_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            provider.CreateElection(electionName, null!));
    }

    [Fact]
    public void CreateElection_WithEmptyElectionName_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string participantId = "participant-1";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            provider.CreateElection("", participantId));
    }

    [Fact]
    public void CreateElection_WithEmptyParticipantId_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            provider.CreateElection(electionName, ""));
    }

    [Fact]
    public async Task CreateElection_MultipleTimes_ShouldReturnDifferentInstances()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        var service1 = provider.CreateElection(electionName, participantId);
        var service2 = provider.CreateElection(electionName, participantId);

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2);

        // Cleanup
        await service1.DisposeAsync();
        await service2.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithDifferentParameters_ShouldReturnCorrectServices()
    {
        // Arrange
        var provider = new PostgresLeaderElectionServiceProvider(
            _mockDataSource,
            _mockSerializer,
            _testTableName,
            _mockLogger);
        const string electionName1 = "test-election-1";
        const string participantId1 = "participant-1";
        const string electionName2 = "test-election-2";
        const string participantId2 = "participant-2";

        // Act
        var service1 = provider.CreateElection(electionName1, participantId1);
        var service2 = provider.CreateElection(electionName2, participantId2);

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal(electionName1, service1.ElectionName);
        Assert.Equal(participantId1, service1.ParticipantId);
        Assert.Equal(electionName2, service2.ElectionName);
        Assert.Equal(participantId2, service2.ParticipantId);

        // Cleanup
        await service1.DisposeAsync();
        await service2.DisposeAsync();
    }
}
