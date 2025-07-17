using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class InProcessLeaderElectionServiceProviderSpecs
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";

    [Fact]
    public async Task CreateElection_WithValidParameters_ShouldReturnService()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithOptions_ShouldReturnServiceWithOptions()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(10),
            RenewalInterval = TimeSpan.FromMinutes(3),
            RetryInterval = TimeSpan.FromSeconds(30),
            AutoStart = false
        };

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId, options);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithCustomDependencies_ShouldReturnServiceWithDependencies()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var logger = NullLogger.Instance;
        var provider = new InProcessLeaderElectionServiceProvider(dateTimeProvider, logger);

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithNullLogger_ShouldReturnServiceWithNullLogger()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider(null, null);

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithNullDateTimeProvider_ShouldReturnServiceWithDefaultProvider()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider(null, null);

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_MultipleTimes_ShouldReturnDifferentInstances()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();

        // Act
        var service1 = provider.CreateElection(DefaultElectionName, DefaultParticipantId);
        var service2 = provider.CreateElection(DefaultElectionName, DefaultParticipantId);

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2);
        Assert.Equal(service1.ElectionName, service2.ElectionName);
        Assert.Equal(service1.ParticipantId, service2.ParticipantId);

        await service1.DisposeAsync();
        await service2.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithDifferentElectionNames_ShouldReturnServicesWithCorrectNames()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();
        const string election1 = "election-1";
        const string election2 = "election-2";

        // Act
        var service1 = provider.CreateElection(election1, DefaultParticipantId);
        var service2 = provider.CreateElection(election2, DefaultParticipantId);

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal(election1, service1.ElectionName);
        Assert.Equal(election2, service2.ElectionName);
        Assert.Equal(DefaultParticipantId, service1.ParticipantId);
        Assert.Equal(DefaultParticipantId, service2.ParticipantId);

        await service1.DisposeAsync();
        await service2.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithDifferentParticipantIds_ShouldReturnServicesWithCorrectIds()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";

        // Act
        var service1 = provider.CreateElection(DefaultElectionName, participant1);
        var service2 = provider.CreateElection(DefaultElectionName, participant2);

        // Assert
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal(DefaultElectionName, service1.ElectionName);
        Assert.Equal(DefaultElectionName, service2.ElectionName);
        Assert.Equal(participant1, service1.ParticipantId);
        Assert.Equal(participant2, service2.ParticipantId);

        await service1.DisposeAsync();
        await service2.DisposeAsync();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateElection_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.CreateElection(electionName, DefaultParticipantId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateElection_WithInvalidParticipantId_ShouldThrowArgumentException(string participantId)
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            provider.CreateElection(DefaultElectionName, participantId));
    }

    [Fact]
    public async Task CreateElection_WithInvalidOptions_ShouldReturnServiceWithValidatedOptions()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();
        var invalidOptions = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromSeconds(1), // Too short
            RenewalInterval = TimeSpan.FromMinutes(10), // Longer than lease duration
            RetryInterval = TimeSpan.FromMinutes(10), // Longer than lease duration
            OperationTimeout = TimeSpan.FromMinutes(10) // Longer than lease duration
        };

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId, invalidOptions);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task CreateElection_WithMetadata_ShouldReturnServiceWithMetadata()
    {
        // Arrange
        var provider = new InProcessLeaderElectionServiceProvider();
        var metadata = new Dictionary<string, string> { ["key"] = "value" };
        var options = new LeaderElectionOptions { Metadata = metadata };

        // Act
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId, options);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<InProcessLeaderElectionService>(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var logger = NullLogger.Instance;

        // Act
        var provider = new InProcessLeaderElectionServiceProvider(dateTimeProvider, logger);

        // Assert
        Assert.NotNull(provider);

        // Verify functionality by creating a service
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);
        Assert.NotNull(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithNullParameters_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var provider = new InProcessLeaderElectionServiceProvider(null, null);

        // Assert
        Assert.NotNull(provider);

        // Verify functionality by creating a service
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);
        Assert.NotNull(service);

        await service.DisposeAsync();
    }

    [Fact]
    public async Task Constructor_WithDefaultParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var provider = new InProcessLeaderElectionServiceProvider();

        // Assert
        Assert.NotNull(provider);

        // Verify functionality by creating a service
        var service = provider.CreateElection(DefaultElectionName, DefaultParticipantId);
        Assert.NotNull(service);

        await service.DisposeAsync();
    }
}
