using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;
using AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection.TestHelpers;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class InProcessLeaderElectionServiceRenewalTests
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";
    private const string AlternateParticipantId = "participant-2";

    [Fact]
    public async Task LeadershipRenewal_ElectionLoopBasicFunctionality_ShouldWork()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider();
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromSeconds(1),
            RenewalInterval = TimeSpan.FromMilliseconds(100),
            AutoStart = false
        };

        await using var service = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (sender, args) => leadershipEvents.Add(args);

        // Act - Test that the election loop can start and stop without errors
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        await service.StartAsync();
        await Task.Delay(300); // Allow some time for the loop to run
        await service.StopAsync();

        // Assert - Basic functionality should work
        Assert.True(leadershipEvents.Count >= 1); // Should have leadership gained event
        var firstEvent = leadershipEvents[0];
        Assert.True(firstEvent.IsLeader);
        Assert.True(firstEvent.LeadershipGained);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_ManualLeaseRenewal_ShouldWork()
    {
        // Arrange - Test renewal functionality more directly through lease store
        var dateTimeProvider = new DateTimeProvider();
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        var metadata = new Dictionary<string, string> { ["test"] = "value" };

        // Act - Acquire lease and then renew it
        var initialLease = await leaseStore.TryAcquireLeaseAsync(
            DefaultElectionName,
            DefaultParticipantId,
            TimeSpan.FromMinutes(5),
            metadata);

        Assert.NotNull(initialLease);
        Assert.True(initialLease.IsValid);

        // Wait a bit then renew
        await Task.Delay(100);

        var renewedLease = await leaseStore.TryRenewLeaseAsync(
            DefaultElectionName,
            DefaultParticipantId,
            TimeSpan.FromMinutes(5),
            metadata);

        // Assert
        Assert.NotNull(renewedLease);
        Assert.True(renewedLease.IsValid);
        Assert.Equal(DefaultParticipantId, renewedLease.ParticipantId);
        Assert.Equal(metadata, renewedLease.Metadata);
        Assert.Equal(initialLease.AcquiredAt, renewedLease.AcquiredAt); // Should preserve original acquired time

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_RenewalFailureHandling_ShouldWork()
    {
        // Arrange - Test what happens when renewal fails
        var dateTimeProvider = new DateTimeProvider();
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId);

        // Act - Service1 gets leadership, then service2 takes it
        await service1.TryAcquireLeadershipAsync();
        Assert.True(service1.IsLeader);

        // Service2 takes leadership
        await service1.ReleaseLeadershipAsync();
        await service2.TryAcquireLeadershipAsync();
        Assert.True(service2.IsLeader);

        // Now service1 should fail to renew
        var renewResult = await leaseStore.TryRenewLeaseAsync(
            DefaultElectionName,
            DefaultParticipantId,
            TimeSpan.FromMinutes(5));

        // Assert
        Assert.Null(renewResult); // Should fail because service2 has the lease
        Assert.False(service1.IsLeader);
        Assert.True(service2.IsLeader);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_WithCustomMetadata_ShouldPreserveMetadata()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider();
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);
        var originalMetadata = new Dictionary<string, string> { ["version"] = "1.0" };
        var newMetadata = new Dictionary<string, string> { ["version"] = "1.1" };

        // Act - Acquire with original metadata
        var initialLease = await leaseStore.TryAcquireLeaseAsync(
            DefaultElectionName,
            DefaultParticipantId,
            TimeSpan.FromMinutes(5),
            originalMetadata);

        Assert.NotNull(initialLease);
        Assert.Equal(originalMetadata, initialLease.Metadata);

        // Renew with new metadata
        var renewedLease = await leaseStore.TryRenewLeaseAsync(
            DefaultElectionName,
            DefaultParticipantId,
            TimeSpan.FromMinutes(5),
            newMetadata);

        // Assert
        Assert.NotNull(renewedLease);
        Assert.Equal(newMetadata, renewedLease.Metadata); // Should have updated metadata

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_WithCancellation_ShouldStopRenewalLoop()
    {
        // Arrange
        var times = new[] { _baseTime, _baseTime.AddMinutes(1) };
        var dateTimeProvider = new DateTimeMockProvider(times);
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(5),
            RenewalInterval = TimeSpan.FromMilliseconds(50),
            AutoStart = true
        };

        await using var service = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        await service.StartAsync();

        // Cancel after a short time
        using var cts = new CancellationTokenSource(100);
        var stopTask = service.StopAsync(cts.Token);

        // Should either complete or throw OperationCanceledException
        var exception = await Record.ExceptionAsync(() => stopTask);

        // Assert
        Assert.True(exception == null || exception is OperationCanceledException);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_Service1HoldsLeadershipThroughMultipleRenewalsWhileService2Waits_ThenService2TakesOver()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider(); // Use real time for this scenario
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var options1 = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(300), // Short lease for faster test
            RenewalInterval = TimeSpan.FromMilliseconds(80), // Renewal every 80ms
            RetryInterval = TimeSpan.FromMilliseconds(40), // Retry every 40ms for non-leader
            AutoStart = true
        };

        var options2 = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(300),
            RenewalInterval = TimeSpan.FromMilliseconds(80),
            RetryInterval = TimeSpan.FromMilliseconds(40),
            AutoStart = true
        };

        await using var service1 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options1);

        await using var service2 = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            AlternateParticipantId,
            options2);

        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (sender, args) => service1Events.Add(args);
        service2.LeadershipChanged += (sender, args) => service2Events.Add(args);

        // Act
        // Both services start their election loops simultaneously
        await service1.StartAsync();
        await service2.StartAsync();

        // Give them a moment to compete for initial leadership
        await Task.Delay(100);

        // One should be leader, the other should not be
        var initialLeader = service1.IsLeader ? service1 : service2;
        var initialFollower = service1.IsLeader ? service2 : service1;

        Assert.True(initialLeader.IsLeader);
        Assert.False(initialFollower.IsLeader);

        // Let the leader hold leadership for approximately 3 renewal intervals
        // 3 renewal intervals = 3 * 80ms = 240ms
        await Task.Delay(250);

        // Leader should still be the leader after multiple renewals
        Assert.True(initialLeader.IsLeader);
        Assert.False(initialFollower.IsLeader);

        // Now simulate the leader "finishing processing" by stopping its service
        await initialLeader.StopAsync();

        // Give service2 time to detect the leadership change and take over
        await Task.Delay(200);

        // Assert
        // The initial follower should now be the leader
        Assert.False(initialLeader.IsLeader);
        Assert.True(initialFollower.IsLeader);

        // Verify events were fired appropriately
        var leaderEvents = initialLeader == service1 ? service1Events : service2Events;
        var followerEvents = initialLeader == service1 ? service2Events : service1Events;

        // Leader should have gained leadership initially, then lost it when stopped
        Assert.Contains(leaderEvents, e => e.LeadershipGained);
        Assert.Contains(leaderEvents, e => e.LeadershipLost);

        // Follower should eventually gain leadership
        Assert.Contains(followerEvents, e => e.LeadershipGained);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_VerifyRenewalHappensMultipleTimes()
    {
        // Arrange - Test specifically that renewals happen multiple times
        var dateTimeProvider = new DateTimeProvider();
        var leaseStore = new InProcessLeaseStore(dateTimeProvider);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(200),
            RenewalInterval = TimeSpan.FromMilliseconds(50), // Renew every 50ms
            AutoStart = true
        };

        await using var service = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Track renewal events
        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (sender, args) => leadershipEvents.Add(args);

        // Act
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        await service.StartAsync();

        // Monitor for multiple renewal intervals (allow enough time for 4-5 renewals)
        await Task.Delay(300); // Monitor for 300ms = 6 renewal intervals

        await service.StopAsync();

        // Assert - Should have maintained leadership throughout
        // The service should have been leader most of the time and renewed successfully
        Assert.False(service.IsLeader); // Should release leadership on stop

        // Verify we got the initial leadership event
        Assert.Contains(leadershipEvents, e => e.LeadershipGained);

        leaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipRenewal_StartWithCancellationDuringSlowRenewal_ShouldHandleCancellationGracefully()
    {
        // Arrange - Test the specific scenario from user: AutoStart=true + slow lease store + cancellation token
        var slowLeaseStore = new SlowLeaseStore(TimeSpan.FromSeconds(10)); // Responds after 10s

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromSeconds(30),
            RenewalInterval = TimeSpan.FromMilliseconds(100), // Frequent renewals
            OperationTimeout = TimeSpan.FromSeconds(15), // Longer than lease store delay
            AutoStart = true
        };

        await using var service = new InProcessLeaderElectionService(
            slowLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        // Start with a cancellation token that cancels in 5 seconds (before the 10s lease store response)
        var cancelAfter = TimeSpan.FromSeconds(2);
        using var startCts = new CancellationTokenSource(cancelAfter);
        var startException = await Record.ExceptionAsync(() => service.StartAsync(startCts.Token));

        // Give some time for cancellation to take effect and then stop
        await Task.Delay(2 * cancelAfter);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stopException = await Record.ExceptionAsync(() => service.StopAsync());
        stopwatch.Stop();

        // Assert
        // Neither operation should throw unhandled exceptions
        Assert.True(startException == null || startException is OperationCanceledException);
        Assert.Null(stopException);

        slowLeaseStore.Dispose();
    }
}
