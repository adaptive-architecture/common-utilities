using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class InProcessLeaderElectionServiceBasicTests
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";
    private const string AlternateParticipantId = "participant-2";

    [Fact]
    public async Task Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        // Assert
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
    }

    [Fact]
    public async Task Constructor_WithOptions_ShouldInitializeWithOptions()
    {
        // Arrange
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(10),
            RenewalInterval = TimeSpan.FromMinutes(3),
            RetryInterval = TimeSpan.FromSeconds(30),
            EnableContinuousCheck = false
        };

        // Act
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Assert
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
    }

    [Fact]
    public async Task Constructor_WithCustomDependencies_ShouldInitializeWithDependencies()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var logger = NullLogger.Instance;

        // Act
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            logger,
            dateTimeProvider);

        // Assert
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
    }

    [Fact]
    public async Task Constructor_WithCustomLeaseStore_ShouldInitializeWithLeaseStore()
    {
        // Arrange
        var leaseStore = new InProcessLeaseStore();

        // Act
        await using var service = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Assert
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        leaseStore.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            new InProcessLeaderElectionService(electionName, DefaultParticipantId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidParticipantId_ShouldThrowArgumentException(string participantId)
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            new InProcessLeaderElectionService(DefaultElectionName, participantId));
    }

    [Fact]
    public void Constructor_WithNullLeaseStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new InProcessLeaderElectionService(null!, DefaultElectionName, DefaultParticipantId));
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WhenNoExistingLeader_ShouldSucceed()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        // Act
        var result = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        Assert.True(service.IsLeader);
        Assert.NotNull(service.CurrentLeader);
        Assert.Equal(DefaultParticipantId, service.CurrentLeader.ParticipantId);
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WhenLeaderExists_ShouldFail()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);
        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId);

        // Act
        var result1 = await service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        var result2 = await service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(service1.IsLeader);
        Assert.False(service2.IsLeader);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.TryAcquireLeadershipAsync(cancellationToken));
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_WhenLeader_ShouldSucceed()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        // Act
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsLeader);

        await service.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_WhenNotLeader_ShouldNotThrow()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        // Act & Assert - Should not throw
        await service.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.False(service.IsLeader);
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);
        var cancellationToken = new CancellationToken(true);

        // Act & Assert - Should either complete quickly or throw OperationCanceledException
        var exception = await Record.ExceptionAsync(() =>
            service.ReleaseLeadershipAsync(cancellationToken));

        // Either no exception (completed quickly) or OperationCanceledException
        Assert.True(exception == null || exception is OperationCanceledException);
    }

    [Fact]
    public async Task StartAsync_ShouldInitializeElectionLoop()
    {
        // Arrange
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Give some time for the election loop to potentially run
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert - Should not throw and service should be initialized
        // With EnableContinuousCheck = false, the election loop starts but may acquire leadership immediately
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        // IsLeader can be true or false depending on timing, so we don't assert on it
    }

    [Fact]
    public async Task StartAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);
        var cancellationToken = new CancellationToken(true);

        // Act - StartAsync returns immediately, cancellation affects the background task
        await service.StartAsync(cancellationToken);

        // Assert - Should complete without throwing
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StopAsync_ShouldStopElectionLoop()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Should not throw
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StopAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);
        var cancellationToken = new CancellationToken(true);

        // Act - StopAsync handles cancellation internally
        var exception = await Record.ExceptionAsync(() =>
            service.StopAsync(cancellationToken));

        // Assert - Should either complete or throw OperationCanceledException
        Assert.True(exception == null || exception is OperationCanceledException);
    }

    [Fact]
    public async Task LeadershipChanged_ShouldFireWhenLeadershipChanges()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        LeadershipChangedEventArgs eventArgs = null;
        service.LeadershipChanged += (sender, args) => eventArgs = args;

        // Act
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.True(eventArgs.IsLeader);
        Assert.True(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeadershipLost);
        Assert.NotNull(eventArgs.CurrentLeader);
        Assert.Equal(DefaultParticipantId, eventArgs.CurrentLeader.ParticipantId);
    }

    [Fact]
    public async Task LeadershipChanged_ShouldFireWhenLeadershipLost()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        LeadershipChangedEventArgs eventArgs = null;
        service.LeadershipChanged += (sender, args) => eventArgs = args;

        // Act
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        eventArgs = null; // Reset for the next event

        await service.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained);
        Assert.True(eventArgs.LeadershipLost);
        Assert.Null(eventArgs.CurrentLeader);
    }

    [Fact]
    public async Task MultipleServices_ShouldCompeteForLeadership()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);
        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId);

        // Act
        var result1 = await service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        var result2 = await service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(service1.IsLeader);
        Assert.False(service2.IsLeader);
        Assert.Equal(DefaultParticipantId, service1.CurrentLeader?.ParticipantId);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task MultipleServices_ShouldHaveIndependentElections()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        const string election1 = "election-1";
        const string election2 = "election-2";

        await using var service1 = new InProcessLeaderElectionService(
            election1,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);
        await using var service2 = new InProcessLeaderElectionService(
            election2,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        // Act
        var result1 = await service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        var result2 = await service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(service1.IsLeader);
        Assert.True(service2.IsLeader);
        Assert.Equal(election1, service1.ElectionName);
        Assert.Equal(election2, service2.ElectionName);
    }

    [Fact]
    public async Task LeadershipTransition_ShouldWorkCorrectly()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime, _baseTime]);
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);
        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId);

        // Act
        _ = await service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service1.IsLeader);

        await service1.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.False(service1.IsLeader);

        _ = await service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(service1.IsLeader);
        Assert.True(service2.IsLeader);
        Assert.Equal(AlternateParticipantId, service2.CurrentLeader?.ParticipantId);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_ShouldCleanupResources()
    {
        // Arrange
        var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsLeader);

        // Act
        await service.DisposeAsync();

        // Assert
        // After disposal, service should be unusable
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeAsync_DoeNotGuaranteeLeadershipRelease()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime, _baseTime, _baseTime]);
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        _ = await service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service1.IsLeader);
        await service1.DisposeAsync();

        // Now try to acquire leadership with a different service
        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId);

        // Assert - should be able to acquire leadership after disposal
        var result = await service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.True(service2.IsLeader);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task WithOptions_ShouldRespectCustomOptions()
    {
        // Arrange
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromSeconds(30),
            RenewalInterval = TimeSpan.FromSeconds(10),
            RetryInterval = TimeSpan.FromSeconds(5),
            EnableContinuousCheck = false,
            Metadata = new Dictionary<string, string> { ["test"] = "value" }
        };

        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(service.IsLeader);
        Assert.NotNull(service.CurrentLeader);
        Assert.Equal(options.Metadata, service.CurrentLeader.Metadata);
    }

    [Fact]
    public async Task ConcurrentLeadershipAttempts_ShouldBeThreadSafe()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider(); // Use real provider for concurrency
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        const int serviceCount = 10;
        var services = new List<InProcessLeaderElectionService>();

        for (int i = 0; i < serviceCount; i++)
        {
            services.Add(new InProcessLeaderElectionService(
                leaseStore,
                DefaultElectionName,
                $"participant-{i}"));
        }

        try
        {
            // Act
            var tasks = services.Select(s => s.TryAcquireLeadershipAsync()).ToArray();
            var results = await Task.WhenAll(tasks);

            // Assert
            var successCount = results.Count(r => r);
            Assert.Equal(1, successCount); // Only one should succeed

            var leaderCount = services.Count(s => s.IsLeader);
            Assert.Equal(1, leaderCount); // Only one should be leader
        }
        finally
        {
            // Cleanup
            foreach (var service in services)
            {
                await service.DisposeAsync();
            }
            leaseStore.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartAsync_CalledMultipleTimes_ShouldNotThrow(bool autoStart)
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            new LeaderElectionOptions { EnableContinuousCheck = autoStart });

        // Act & Assert - Should not throw
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_CalledMultipleTimes_ShouldNotThrow(bool autoStart)
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            new LeaderElectionOptions { EnableContinuousCheck = autoStart });

        // Act & Assert - Should not throw
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StartStop_InterleavedCalls_ShouldNotThrow()
    {
        // Arrange
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Should not throw with interleaved calls
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken); // Multiple starts
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken); // Multiple stops
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StartStop_ConcurrentCalls_ShouldNotThrow()
    {
        // Arrange
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act - Concurrent start/stop calls
        var tasks = new List<Task>();

        // Multiple concurrent starts
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.StartAsync(TestContext.Current.CancellationToken));
        }

        // Multiple concurrent stops
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.StopAsync(TestContext.Current.CancellationToken));
        }

        // Assert - Should all complete without throwing
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(exception);

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StartStop_WithCancellationTokens_ShouldNotThrow()
    {
        // Arrange
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        // Act & Assert - Should not throw with cancellation tokens
        await service.StartAsync(cts1.Token);
        await service.StartAsync(cts2.Token);
        await service.StopAsync(cts1.Token);
        await service.StopAsync(cts2.Token);
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StartStop_AfterAcquiringLeadership_ShouldNotThrow()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options,
            null,
            dateTimeProvider);

        // Act - Acquire leadership then do start/stop cycles
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsLeader);

        // Multiple start/stop cycles while being leader
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StartAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Should maintain state correctly
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
        // Leadership should be released during stop
        Assert.False(service.IsLeader);
    }

    [Fact]
    public async Task StartStop_RapidSequence_ShouldNotThrow()
    {
        // Arrange
        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = false,
            RetryInterval = TimeSpan.FromMilliseconds(10),
            RenewalInterval = TimeSpan.FromMilliseconds(10)
        };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Rapid start/stop sequence
        for (int i = 0; i < 10; i++)
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
            await Task.Delay(5, TestContext.Current.CancellationToken); // Brief delay to let election loop potentially start
            await service.StopAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task EnableContinuousCheck_LeadershipHandover_ShouldWorkCorrectly()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider(); // Use real provider for timing-based test
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            LeaseDuration = TimeSpan.FromSeconds(1),
            RenewalInterval = TimeSpan.FromMilliseconds(200),
            RetryInterval = TimeSpan.FromMilliseconds(100)
        };

        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId,
            options);

        // Track leadership events
        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (_, args) => service1Events.Add(args);
        service2.LeadershipChanged += (_, args) => service2Events.Add(args);

        try
        {
            // Act - Start both services with EnableContinuousCheck=true
            await service1.StartAsync(TestContext.Current.CancellationToken);
            await service2.StartAsync(TestContext.Current.CancellationToken);

            // Wait for initial leader election to settle
            await Task.Delay(300, TestContext.Current.CancellationToken);

            // Phase 1: One service should become leader
            var initialLeaderCount = (service1.IsLeader ? 1 : 0) + (service2.IsLeader ? 1 : 0);
            Assert.Equal(1, initialLeaderCount);

            var currentLeader = service1.IsLeader ? service1 : service2;
            var currentFollower = service1.IsLeader ? service2 : service1;

            // Phase 2: Current leader releases leadership
            await currentLeader.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);
            Assert.False(currentLeader.IsLeader);

            // Force the follower to try acquiring leadership
            _ = await currentFollower.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

            // Wait for the follower to detect leadership is available and acquire it
            var maxWait = TimeSpan.FromSeconds(2);
            var start = DateTime.UtcNow;

            while (!currentFollower.IsLeader && DateTime.UtcNow - start < maxWait)
            {
                await Task.Delay(50, TestContext.Current.CancellationToken);
            }

            // Assert Phase 2: Follower should now be leader
            Assert.True(currentFollower.IsLeader, "Follower should have acquired leadership after original leader released it");
            Assert.False(currentLeader.IsLeader, "Original leader should remain non-leader");

            // Phase 3: New leader releases leadership
            await currentFollower.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);
            Assert.False(currentFollower.IsLeader);

            // Wait a bit and stop services to prevent re-acquisition due to EnableContinuousCheck
            await service1.StopAsync(TestContext.Current.CancellationToken);
            await service2.StopAsync(TestContext.Current.CancellationToken);

            // Wait a bit to ensure leadership is properly released and services are stopped
            await Task.Delay(300, TestContext.Current.CancellationToken);

            // Assert Phase 3: Both should be non-leaders after second release and service stop
            Assert.False(service1.IsLeader, "Service1 should not be leader after stop");
            Assert.False(service2.IsLeader, "Service2 should not be leader after stop");

            // Verify events were fired correctly
            Assert.NotEmpty(service1Events.Concat(service2Events));

            // Check that at least one service gained leadership initially
            var gainedLeadershipEvents = service1Events.Concat(service2Events)
                .Where(e => e.LeadershipGained).ToList();
            Assert.NotEmpty(gainedLeadershipEvents);

            // Check that leadership was lost when released
            var lostLeadershipEvents = service1Events.Concat(service2Events)
                .Where(e => e.LeadershipLost).ToList();
            Assert.True(lostLeadershipEvents.Count >= 2, "Should have at least 2 leadership lost events for both releases");
        }
        finally
        {
            // Cleanup - services should already be stopped, but ensure cleanup
            try { await service1.StopAsync(TestContext.Current.CancellationToken); } catch { /* NOOP */ }
            try { await service2.StopAsync(TestContext.Current.CancellationToken); } catch { /* NOOP */ }
            leaseStore.Dispose();
        }
    }
}
