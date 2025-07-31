using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Redis.IntegrationTests.Fixtures;
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests.LeaderElection;

[Collection(RedisCollection.CollectionName)]
public class RedisLeaderElectionIntegrationTests
{
    private readonly RedisFixture _fixture;

    public RedisLeaderElectionIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Acquire_And_Release_Lease()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);
        var metadata = new Dictionary<string, string> { ["version"] = "1.0", ["host"] = "localhost" };

        try
        {
            // Act - Acquire lease
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata, TestContext.Current.CancellationToken);

            // Assert - Lease acquired successfully
            Assert.NotNull(lease);
            Assert.Equal(participantId, lease.ParticipantId);
            Assert.Equal(metadata, lease.Metadata);
            Assert.True(lease.IsValid);
            Assert.True(lease.TimeToExpiry > TimeSpan.FromSeconds(25)); // Should be close to 30 seconds

            // Verify lease exists
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);

            var hasValidLease = await leaseStore.HasValidLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.True(hasValidLease);

            // Act - Release lease
            var released = await leaseStore.ReleaseLeaseAsync(electionName, participantId, TestContext.Current.CancellationToken);

            // Assert - Lease released successfully
            Assert.True(released);

            // Verify lease is gone
            currentLease = await leaseStore.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.Null(currentLease);

            hasValidLease = await leaseStore.HasValidLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.False(hasValidLease);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Prevent_Duplicate_Acquisition()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore1 = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);
        var leaseStore2 = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Act - First participant acquires lease
            var lease1 = await leaseStore1.TryAcquireLeaseAsync(electionName, participant1, leaseDuration, cancellationToken: TestContext.Current.CancellationToken);

            // Act - Second participant tries to acquire lease
            var lease2 = await leaseStore2.TryAcquireLeaseAsync(electionName, participant2, leaseDuration, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(lease1);
            Assert.Equal(participant1, lease1.ParticipantId);

            Assert.Null(lease2); // Should fail to acquire

            // Verify current lease is still held by first participant
            var currentLease = await leaseStore1.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.NotNull(currentLease);
            Assert.Equal(participant1, currentLease.ParticipantId);
        }
        finally
        {
            _ = await leaseStore1.ReleaseLeaseAsync(electionName, participant1, TestContext.Current.CancellationToken);
            leaseStore1.Dispose();
            leaseStore2.Dispose();
        }
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Renew_Lease_For_Current_Holder()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var initialDuration = TimeSpan.FromSeconds(30);
        var renewalDuration = TimeSpan.FromSeconds(60);

        try
        {
            // Act - Acquire initial lease
            var originalLease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, initialDuration, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(originalLease);

            // Wait a moment to ensure time difference
            await Task.Delay(100, TestContext.Current.CancellationToken);

            // Act - Renew lease
            var renewedLease = await leaseStore.TryRenewLeaseAsync(electionName, participantId, renewalDuration, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(renewedLease);
            Assert.Equal(participantId, renewedLease.ParticipantId);
            Assert.True(renewedLease.AcquiredAt >= originalLease.AcquiredAt);
            Assert.True(renewedLease.TimeToExpiry > TimeSpan.FromSeconds(55)); // Should be close to 60 seconds

            // Verify lease is updated
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);
            Assert.True(currentLease.TimeToExpiry > TimeSpan.FromSeconds(55));
        }
        finally
        {
            _ = await leaseStore.ReleaseLeaseAsync(electionName, participantId, TestContext.Current.CancellationToken);
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Prevent_Renewal_By_Non_Holder()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore1 = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);
        var leaseStore2 = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Act - First participant acquires lease
            var lease1 = await leaseStore1.TryAcquireLeaseAsync(electionName, participant1, leaseDuration, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(lease1);

            // Act - Second participant tries to renew lease
            var renewedLease = await leaseStore2.TryRenewLeaseAsync(electionName, participant2, leaseDuration, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(renewedLease); // Should fail to renew

            // Verify original lease is still intact
            var currentLease = await leaseStore1.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.NotNull(currentLease);
            Assert.Equal(participant1, currentLease.ParticipantId);
        }
        finally
        {
            _ = await leaseStore1.ReleaseLeaseAsync(electionName, participant1, TestContext.Current.CancellationToken);
            leaseStore1.Dispose();
            leaseStore2.Dispose();
        }
    }

    [Fact]
    public async Task RedisLeaderElectionService_Should_Acquire_And_Release_Leadership()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";

        var service = new RedisLeaderElectionService(
            _fixture.Connection,
            serializer,
            electionName,
            participantId);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipEvents.Add(args);

        try
        {
            // Act - Acquire leadership
            var acquired = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.True(acquired);
            Assert.True(service.IsLeader);
            Assert.NotNull(service.CurrentLeader);
            Assert.Equal(participantId, service.CurrentLeader.ParticipantId);

            // Check events
            _ = Assert.Single(leadershipEvents);
            var acquiredEvent = leadershipEvents[0];
            Assert.True(acquiredEvent.IsLeader);
            Assert.True(acquiredEvent.LeadershipGained);
            Assert.False(acquiredEvent.LeadershipLost);

            // Act - Release leadership
            await service.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(service.IsLeader);
            Assert.Null(service.CurrentLeader);

            // Check events
            Assert.Equal(2, leadershipEvents.Count);
            var releasedEvent = leadershipEvents[1];
            Assert.False(releasedEvent.IsLeader);
            Assert.False(releasedEvent.LeadershipGained);
            Assert.True(releasedEvent.LeadershipLost);
        }
        finally
        {
            await service.DisposeAsync();
        }
    }

    [Fact]
    public async Task Multiple_RedisLeaderElectionServices_Should_Coordinate_Leadership()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var electionName = $"test-election-{Guid.NewGuid()}";

        var service1 = new RedisLeaderElectionService(
            _fixture.Connection, serializer, electionName, "participant-1");
        var service2 = new RedisLeaderElectionService(
            _fixture.Connection, serializer, electionName, "participant-2");
        var service3 = new RedisLeaderElectionService(
            _fixture.Connection, serializer, electionName, "participant-3");

        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();
        var service3Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (_, args) => service1Events.Add(args);
        service2.LeadershipChanged += (_, args) => service2Events.Add(args);
        service3.LeadershipChanged += (_, args) => service3Events.Add(args);

        try
        {
            // Act - All services try to acquire leadership simultaneously
            var tasks = new[]
            {
                service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken),
                service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken),
                service3.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken)
            };

            var results = await Task.WhenAll(tasks);

            // Assert - Only one should succeed
            var successCount = results.Count(r => r);
            Assert.Equal(1, successCount);

            // Identify the leader
            var leaders = new[] { service1, service2, service3 }.Where(s => s.IsLeader).ToList();
            _ = Assert.Single(leaders);
            var leaderService = leaders[0];

            // Verify all services see the same current leader
            _ = await Task.WhenAll(
                service1.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken), // This will update CurrentLeader even if it fails
                service2.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken),
                service3.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken)
            );

            // Wait a moment for leader information to propagate
            await Task.Delay(100, TestContext.Current.CancellationToken);

            // Check that non-leader services know who the leader is
            foreach (var service in new[] { service1, service2, service3 })
            {
                if (!service.IsLeader)
                {
                    // Trigger a check for current leader
                    _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
                }
            }

            // Act - Leader releases leadership
            await leaderService.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);

            // Assert - Leadership is released
            Assert.False(leaderService.IsLeader);

            // Wait for the release to propagate
            await Task.Delay(100, TestContext.Current.CancellationToken);

            // Verify leadership is actually available by checking each service
            var remainingServices = new[] { service1, service2, service3 }.Where(s => s != leaderService).ToList();

            // Try a few times as there might be timing issues
            var secondLeaderAcquired = false;
            RedisLeaderElectionService secondLeader = null;

            for (int attempt = 0; attempt < 3 && !secondLeaderAcquired; attempt++)
            {
                foreach (var service in remainingServices)
                {
                    if (await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken))
                    {
                        secondLeader = service;
                        secondLeaderAcquired = true;
                        break;
                    }
                }

                if (!secondLeaderAcquired)
                {
                    await Task.Delay(200, TestContext.Current.CancellationToken); // Wait between attempts
                }
            }

            Assert.True(secondLeaderAcquired, "No service was able to acquire leadership after the original leader released it");
            Assert.NotNull(secondLeader);
            Assert.True(secondLeader.IsLeader);
        }
        finally
        {
            await service1.DisposeAsync();
            await service2.DisposeAsync();
            await service3.DisposeAsync();
        }
    }

    [Fact]
    public async Task RedisLeaderElectionService_Should_Handle_Automatic_Election_Loop()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var electionName = $"test-election-{Guid.NewGuid()}";
        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            LeaseDuration = TimeSpan.FromSeconds(10),
            RenewalInterval = TimeSpan.FromSeconds(3),
            RetryInterval = TimeSpan.FromSeconds(2)
        };

        var service1 = new RedisLeaderElectionService(
            _fixture.Connection, serializer, electionName, "participant-1", options);
        var service2 = new RedisLeaderElectionService(
            _fixture.Connection, serializer, electionName, "participant-2", options);

        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (_, args) => service1Events.Add(args);
        service2.LeadershipChanged += (_, args) => service2Events.Add(args);

        try
        {
            // Act - Start both services (auto-election should begin)
            await service1.StartAsync(TestContext.Current.CancellationToken);
            await service2.StartAsync(TestContext.Current.CancellationToken);

            // Wait for election to settle
            await Task.Delay(5000, TestContext.Current.CancellationToken);

            // Assert - One service should have become leader
            var leaders = new[] { service1, service2 }.Where(s => s.IsLeader).ToList();
            _ = Assert.Single(leaders);
            var currentLeader = leaders[0];
            var follower = new[] { service1, service2 }.First(s => s != currentLeader);

            // Verify leadership events
            var leaderEvents = currentLeader == service1 ? service1Events : service2Events;
            Assert.Contains(leaderEvents, e => e.LeadershipGained);

            // Act - Stop the current leader (simulate failure)
            await currentLeader.StopAsync(TestContext.Current.CancellationToken);

            // Wait for failover
            await Task.Delay(5000, TestContext.Current.CancellationToken);

            // Assert - The other service should become leader
            Assert.True(follower.IsLeader);

            var followerEvents = follower == service1 ? service1Events : service2Events;
            Assert.Contains(followerEvents, e => e.LeadershipGained);
        }
        finally
        {
            await service1.StopAsync(TestContext.Current.CancellationToken);
            await service2.StopAsync(TestContext.Current.CancellationToken);
            await service1.DisposeAsync();
            await service2.DisposeAsync();
        }
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Handle_Expired_Leases()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var shortLeaseDuration = TimeSpan.FromSeconds(2); // Very short lease

        try
        {
            // Act - Acquire a short lease
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, shortLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(lease);
            Assert.True(lease.IsValid);

            // Wait for lease to expire
            await Task.Delay(3000, TestContext.Current.CancellationToken);

            // Act - Try to get current lease (should handle expired lease)
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);

            // Assert - Expired lease should be cleaned up
            Assert.Null(currentLease);

            var hasValidLease = await leaseStore.HasValidLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.False(hasValidLease);

            // Another participant should be able to acquire the lease now
            var newLease = await leaseStore.TryAcquireLeaseAsync(electionName, "participant-2", TimeSpan.FromMinutes(1), cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(newLease);
            Assert.Equal("participant-2", newLease.ParticipantId);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task RedisLeaseStore_Should_Handle_Expired_Leases_That_Were_Not_Cleaned()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore = new RedisLeaseStore(_fixture.Connection, serializer, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var shortLeaseDuration = TimeSpan.FromSeconds(2); // Very short lease

        try
        {
            // Act - Simulate a lease that was not cleaned up properly
            var database = _fixture.Connection.GetDatabase();
            var leaseKey = $"leader_election:lease:{electionName}";
            var acquiredAt = DateTime.UtcNow.AddMinutes(-1);
            var expiresAt = acquiredAt.Add(shortLeaseDuration);

            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = acquiredAt,
                ExpiresAt = expiresAt,
            };
            var serializedLease = serializer.Serialize(leaderInfo);

            // Use SET with NX (only if not exists) and EX (expiration) for atomic operation
            var acquired = await database.StringSetAsync(
                leaseKey,
                serializedLease,
                shortLeaseDuration,
                When.NotExists);

            Assert.True(acquired);

            // Act - Try to get current lease (should handle expired lease)
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken);

            // Assert - Expired lease that were not cleaned up should be ignored
            Assert.Null(currentLease);

            var hasValidLease = await leaseStore.HasValidLeaseAsync(electionName, TestContext.Current.CancellationToken);
            Assert.False(hasValidLease);

            // Another participant should be able to acquire the lease now
            var newLease = await leaseStore.TryAcquireLeaseAsync(electionName, "participant-2", TimeSpan.FromMinutes(1), cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(newLease);
            Assert.Equal("participant-2", newLease.ParticipantId);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }
}
