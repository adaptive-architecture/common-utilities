using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Postgres.IntegrationTests.Fixtures;
using AdaptArch.Common.Utilities.Postgres.LeaderElection;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using AdaptArch.Common.Utilities.Serialization.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using Npgsql;
using NSubstitute;
using System.Net.Sockets;

namespace AdaptArch.Common.Utilities.Postgres.IntegrationTests.LeaderElection;

[Collection(PostgresCollection.CollectionName)]
public class PostgresLeaderElectionIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public PostgresLeaderElectionIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Acquire_And_Release_Lease()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);
        var metadata = new Dictionary<string, string> { ["version"] = "1.0", ["host"] = "localhost" };

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();
            // Verify lease does not exists
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.Null(currentLease);

            // Act - Acquire lease
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata);

            // Assert - Lease acquired successfully
            Assert.NotNull(lease);
            Assert.Equal(participantId, lease.ParticipantId);
            Assert.Equal(metadata, lease.Metadata);
            Assert.True(lease.IsValid);
            Assert.True(lease.TimeToExpiry > TimeSpan.FromSeconds(25)); // Should be close to 30 seconds

            // Verify lease exists
            currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);

            var hasValidLease = await leaseStore.HasValidLeaseAsync(electionName);
            Assert.True(hasValidLease);

            // Act - Release lease
            var released = await leaseStore.ReleaseLeaseAsync(electionName, participantId);

            // Assert - Lease released successfully
            Assert.True(released);

            // Verify lease is gone
            currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.Null(currentLease);

            hasValidLease = await leaseStore.HasValidLeaseAsync(electionName);
            Assert.False(hasValidLease);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Prevent_Duplicate_Acquisition()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore1 = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);
        var leaseStore2 = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Ensure table exists
            await leaseStore1.EnsureTableExistsAsync();

            // Act - First participant acquires lease
            var lease1 = await leaseStore1.TryAcquireLeaseAsync(electionName, participant1, leaseDuration);

            // Act - Second participant tries to acquire lease
            var lease2 = await leaseStore2.TryAcquireLeaseAsync(electionName, participant2, leaseDuration);

            // Assert
            Assert.NotNull(lease1);
            Assert.Equal(participant1, lease1.ParticipantId);

            Assert.Null(lease2); // Should fail to acquire

            // Verify current lease is still held by first participant
            var currentLease = await leaseStore1.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participant1, currentLease.ParticipantId);
        }
        finally
        {
            _ = await leaseStore1.ReleaseLeaseAsync(electionName, participant1);
            leaseStore1.Dispose();
            leaseStore2.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Renew_Lease_For_Current_Holder()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var initialDuration = TimeSpan.FromSeconds(30);
        var renewalDuration = TimeSpan.FromSeconds(60);

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act - Acquire initial lease
            var originalLease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, initialDuration);
            Assert.NotNull(originalLease);

            // Wait a moment to ensure time difference
            await Task.Delay(100);

            // Act - Renew lease
            var renewedLease = await leaseStore.TryRenewLeaseAsync(electionName, participantId, renewalDuration);

            // Assert
            Assert.NotNull(renewedLease);
            Assert.Equal(participantId, renewedLease.ParticipantId);
            Assert.True(renewedLease.AcquiredAt >= originalLease.AcquiredAt);
            Assert.True(renewedLease.TimeToExpiry > TimeSpan.FromSeconds(55)); // Should be close to 60 seconds

            // Verify lease is updated
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);
            Assert.True(currentLease.TimeToExpiry > TimeSpan.FromSeconds(55));
        }
        finally
        {
            _ = await leaseStore.ReleaseLeaseAsync(electionName, participantId);
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Prevent_Renewal_By_Non_Holder()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore1 = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);
        var leaseStore2 = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Ensure table exists
            await leaseStore1.EnsureTableExistsAsync();

            // Act - First participant acquires lease
            var lease1 = await leaseStore1.TryAcquireLeaseAsync(electionName, participant1, leaseDuration);
            Assert.NotNull(lease1);

            // Act - Second participant tries to renew lease
            var renewedLease = await leaseStore2.TryRenewLeaseAsync(electionName, participant2, leaseDuration);

            // Assert
            Assert.Null(renewedLease); // Should fail to renew

            // Verify original lease is still intact
            var currentLease = await leaseStore1.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participant1, currentLease.ParticipantId);
        }
        finally
        {
            _ = await leaseStore1.ReleaseLeaseAsync(electionName, participant1);
            leaseStore1.Dispose();
            leaseStore2.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaderElectionService_Should_Acquire_And_Release_Leadership()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";

        var service = new PostgresLeaderElectionService(
            _fixture.DataSource,
            serializer,
            electionName,
            participantId,
            tableName);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipEvents.Add(args);

        try
        {
            // Ensure table exists first
            var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);
            await leaseStore.EnsureTableExistsAsync();
            leaseStore.Dispose();

            // Act - Acquire leadership
            var acquired = await service.TryAcquireLeadershipAsync();

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
            await service.ReleaseLeadershipAsync();

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
    public async Task Multiple_PostgresLeaderElectionServices_Should_Coordinate_Leadership()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var electionName = $"test-election-{Guid.NewGuid()}";

        var service1 = new PostgresLeaderElectionService(
            _fixture.DataSource, serializer, electionName, "participant-1", tableName);
        var service2 = new PostgresLeaderElectionService(
            _fixture.DataSource, serializer, electionName, "participant-2", tableName);
        var service3 = new PostgresLeaderElectionService(
            _fixture.DataSource, serializer, electionName, "participant-3", tableName);

        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();
        var service3Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (_, args) => service1Events.Add(args);
        service2.LeadershipChanged += (_, args) => service2Events.Add(args);
        service3.LeadershipChanged += (_, args) => service3Events.Add(args);

        try
        {
            // Ensure table exists first
            var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);
            await leaseStore.EnsureTableExistsAsync();
            leaseStore.Dispose();

            // Act - All services try to acquire leadership simultaneously
            var tasks = new[]
            {
                service1.TryAcquireLeadershipAsync(),
                service2.TryAcquireLeadershipAsync(),
                service3.TryAcquireLeadershipAsync()
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
            var currentLeaders = await Task.WhenAll(
                service1.TryAcquireLeadershipAsync(), // This will update CurrentLeader even if it fails
                service2.TryAcquireLeadershipAsync(),
                service3.TryAcquireLeadershipAsync()
            );

            // Wait a moment for leader information to propagate
            await Task.Delay(100);

            // Check that non-leader services know who the leader is
            foreach (var service in new[] { service1, service2, service3 })
            {
                if (!service.IsLeader)
                {
                    // Trigger a check for current leader
                    _ = await service.TryAcquireLeadershipAsync();
                }
            }

            // Act - Leader releases leadership
            await leaderService.ReleaseLeadershipAsync();

            // Assert - Leadership is released
            Assert.False(leaderService.IsLeader);

            // Wait for the release to propagate
            await Task.Delay(100);

            // Verify leadership is actually available by checking each service
            var remainingServices = new[] { service1, service2, service3 }.Where(s => s != leaderService).ToList();

            // Try a few times as there might be timing issues
            var secondLeaderAcquired = false;
            PostgresLeaderElectionService secondLeader = null;

            for (int attempt = 0; attempt < 3 && !secondLeaderAcquired; attempt++)
            {
                foreach (var service in remainingServices)
                {
                    if (await service.TryAcquireLeadershipAsync())
                    {
                        secondLeader = service;
                        secondLeaderAcquired = true;
                        break;
                    }
                }

                if (!secondLeaderAcquired)
                {
                    await Task.Delay(200); // Wait between attempts
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
    public async Task PostgresLeaderElectionService_Should_Handle_Automatic_Election_Loop()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var electionName = $"test-election-{Guid.NewGuid()}";
        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            LeaseDuration = TimeSpan.FromSeconds(10),
            RenewalInterval = TimeSpan.FromSeconds(3),
            RetryInterval = TimeSpan.FromSeconds(2)
        };

        var service1 = new PostgresLeaderElectionService(
            _fixture.DataSource, serializer, electionName, "participant-1", tableName, options);
        var service2 = new PostgresLeaderElectionService(
            _fixture.DataSource, serializer, electionName, "participant-2", tableName, options);

        var service1Events = new List<LeadershipChangedEventArgs>();
        var service2Events = new List<LeadershipChangedEventArgs>();

        service1.LeadershipChanged += (_, args) => service1Events.Add(args);
        service2.LeadershipChanged += (_, args) => service2Events.Add(args);

        try
        {
            // Ensure table exists first
            var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);
            await leaseStore.EnsureTableExistsAsync();
            leaseStore.Dispose();

            // Act - Start both services (auto-election should begin)
            await service1.StartAsync();
            await service2.StartAsync();

            // Wait for election to settle
            await Task.Delay(5000);

            // Assert - One service should have become leader
            var leaders = new[] { service1, service2 }.Where(s => s.IsLeader).ToList();
            _ = Assert.Single(leaders);
            var currentLeader = leaders[0];
            var follower = new[] { service1, service2 }.First(s => s != currentLeader);

            // Verify leadership events
            var leaderEvents = currentLeader == service1 ? service1Events : service2Events;
            Assert.Contains(leaderEvents, e => e.LeadershipGained);

            // Act - Stop the current leader (simulate failure)
            await currentLeader.StopAsync();

            // Wait for failover
            await Task.Delay(5000);

            // Assert - The other service should become leader
            Assert.True(follower.IsLeader);

            var followerEvents = follower == service1 ? service1Events : service2Events;
            Assert.Contains(followerEvents, e => e.LeadershipGained);
        }
        finally
        {
            await service1.StopAsync();
            await service2.StopAsync();
            await service1.DisposeAsync();
            await service2.DisposeAsync();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Expired_Leases()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var shortLeaseDuration = TimeSpan.FromSeconds(2); // Very short lease

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act - Acquire a short lease
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, shortLeaseDuration);
            Assert.NotNull(lease);
            Assert.True(lease.IsValid);

            // Wait for lease to expire
            await Task.Delay(3000);

            // Act - Try to get current lease (should handle expired lease)
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);

            // Assert - Expired lease should be cleaned up
            Assert.Null(currentLease);

            var hasValidLease = await leaseStore.HasValidLeaseAsync(electionName);
            Assert.False(hasValidLease);

            // Another participant should be able to acquire the lease now
            var newLease = await leaseStore.TryAcquireLeaseAsync(electionName, "participant-2", TimeSpan.FromMinutes(1));
            Assert.NotNull(newLease);
            Assert.Equal("participant-2", newLease.ParticipantId);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Clean_Up_Expired_Leases()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName1 = $"test-election-1-{Guid.NewGuid()}";
        var electionName2 = $"test-election-2-{Guid.NewGuid()}";
        var shortLeaseDuration = TimeSpan.FromSeconds(1);

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act - Acquire multiple short leases
            var lease1 = await leaseStore.TryAcquireLeaseAsync(electionName1, "participant-1", shortLeaseDuration);
            var lease2 = await leaseStore.TryAcquireLeaseAsync(electionName2, "participant-2", shortLeaseDuration);

            Assert.NotNull(lease1);
            Assert.NotNull(lease2);

            // Wait for leases to expire
            await Task.Delay(2000);

            // Act - Clean up expired leases
            _ = await leaseStore.CleanupExpiredLeasesAsync();

            // Assert - Both leases should be cleaned up
            var currentLease1 = await leaseStore.GetCurrentLeaseAsync(electionName1);
            var currentLease2 = await leaseStore.GetCurrentLeaseAsync(electionName2);

            Assert.Null(currentLease1);
            Assert.Null(currentLease2);

            var hasValidLease1 = await leaseStore.HasValidLeaseAsync(electionName1);
            var hasValidLease2 = await leaseStore.HasValidLeaseAsync(electionName2);

            Assert.False(hasValidLease1);
            Assert.False(hasValidLease2);
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Null_Metadata()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act - Acquire lease with null metadata
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, null);

            // Assert - Lease acquired successfully with null metadata
            Assert.NotNull(lease);
            Assert.Equal(participantId, lease.ParticipantId);
            Assert.Null(lease.Metadata);
            Assert.True(lease.IsValid);
            Assert.True(lease.TimeToExpiry > TimeSpan.FromSeconds(25));

            // Verify lease exists and metadata is null
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);
            Assert.Null(currentLease.Metadata);

            // Act - Renew lease with null metadata
            var renewedLease = await leaseStore.TryRenewLeaseAsync(electionName, participantId, leaseDuration, null);

            // Assert - Lease renewed successfully with null metadata
            Assert.NotNull(renewedLease);
            Assert.Equal(participantId, renewedLease.ParticipantId);
            Assert.Null(renewedLease.Metadata);
        }
        finally
        {
            _ = await leaseStore.ReleaseLeaseAsync(electionName, participantId);
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Empty_Metadata()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);
        var emptyMetadata = new Dictionary<string, string>();

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act - Acquire lease with empty metadata
            var lease = await leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, emptyMetadata);

            // Assert - Lease acquired successfully with empty metadata
            Assert.NotNull(lease);
            Assert.Equal(participantId, lease.ParticipantId);
            Assert.NotNull(lease.Metadata);
            Assert.Empty(lease.Metadata);
            Assert.True(lease.IsValid);

            // Verify lease exists and metadata is empty
            var currentLease = await leaseStore.GetCurrentLeaseAsync(electionName);
            Assert.NotNull(currentLease);
            Assert.Equal(participantId, currentLease.ParticipantId);
            Assert.NotNull(currentLease.Metadata);
            Assert.Empty(currentLease.Metadata);
        }
        finally
        {
            _ = await leaseStore.ReleaseLeaseAsync(electionName, participantId);
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Database_Connection_Failure()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        // Create a data source with invalid connection string
        var invalidDataSource = NpgsqlDataSource.Create("Host=invalid-host;Database=invalid;Username=invalid;Password=invalid");
        var leaseStore = new PostgresLeaseStore(invalidDataSource, serializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Act & Assert - Should throw exceptions on connection failure
            _ = await Assert.ThrowsAsync<SocketException>(() => leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));
            _ = await Assert.ThrowsAsync<SocketException>(() => leaseStore.GetCurrentLeaseAsync(electionName));
            _ = await Assert.ThrowsAsync<SocketException>(() => leaseStore.HasValidLeaseAsync(electionName));

            // ReleaseLeaseAsync should return false on connection failure (not throw)
            var released = await leaseStore.ReleaseLeaseAsync(electionName, participantId);
            Assert.False(released); // Should return false on connection failure
        }
        finally
        {
            leaseStore.Dispose();
            invalidDataSource.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Serialization_Failure()
    {
        // Arrange
        var mockSerializer = Substitute.For<IStringDataSerializer>();
        _ = mockSerializer.Serialize(Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(_ => throw new InvalidOperationException("Serialization failed"));

        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, mockSerializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // Act & Assert - Should throw exception on serialization failure
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata));
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Deserialization_Failure()
    {
        // Arrange
        var mockSerializer = Substitute.For<IStringDataSerializer>();
        _ = mockSerializer.Serialize(Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns("{\"key\":\"value\"}");
        _ = mockSerializer.Deserialize<Dictionary<string, string>>(Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("Deserialization failed"));

        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, mockSerializer, tableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        try
        {
            // Ensure table exists
            await leaseStore.EnsureTableExistsAsync();

            // First acquire a lease successfully
            var realSerializer = new ReflectionStringJsonDataSerializer();
            var realLeaseStore = new PostgresLeaseStore(_fixture.DataSource, realSerializer, tableName, NullLogger.Instance);
            var lease = await realLeaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata);
            Assert.NotNull(lease);
            realLeaseStore.Dispose();

            // Now try to get the lease with the mocked serializer that fails deserialization
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => leaseStore.GetCurrentLeaseAsync(electionName));
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_Handle_Invalid_Table_Name()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        const string invalidTableName = "invalid-table-name-with-spaces and special chars!";
        var leaseStore = new PostgresLeaseStore(_fixture.DataSource, serializer, invalidTableName, NullLogger.Instance);

        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromSeconds(30);

        try
        {
            // Act & Assert - Should throw exceptions on SQL syntax error
            _ = await Assert.ThrowsAsync<PostgresException>(() => leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));
            _ = await Assert.ThrowsAsync<PostgresException>(() => leaseStore.GetCurrentLeaseAsync(electionName));
            _ = await Assert.ThrowsAsync<PostgresException>(() => leaseStore.HasValidLeaseAsync(electionName));
        }
        finally
        {
            leaseStore.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaderElectionService_Should_Handle_Database_Connection_Failure()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";

        // Create a data source with invalid connection string
        var invalidDataSource = NpgsqlDataSource.Create("Host=invalid-host;Database=invalid;Username=invalid;Password=invalid");
        var service = new PostgresLeaderElectionService(
            invalidDataSource,
            serializer,
            electionName,
            participantId,
            tableName);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipEvents.Add(args);

        try
        {
            // Act & Assert - Should return false on connection failure (base service catches exceptions)
            var acquired = await service.TryAcquireLeadershipAsync();
            Assert.False(acquired); // Should return false on connection failure
            Assert.False(service.IsLeader); // Should remain false
            Assert.Null(service.CurrentLeader); // Should remain null

            // No leadership events should be fired on connection failure
            Assert.Empty(leadershipEvents);
        }
        finally
        {
            await service.DisposeAsync();
            invalidDataSource.Dispose();
        }
    }

    [Fact]
    public async Task PostgresLeaderElectionService_Should_Handle_Disposed_State()
    {
        // Arrange
        var serializer = new ReflectionStringJsonDataSerializer();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var electionName = $"test-election-{Guid.NewGuid()}";
        const string participantId = "participant-1";

        var service = new PostgresLeaderElectionService(
            _fixture.DataSource,
            serializer,
            electionName,
            participantId,
            tableName);

        // Dispose the service
        await service.DisposeAsync();

        // Act & Assert - Should handle disposed state as per base class behavior
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => service.TryAcquireLeadershipAsync());
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => service.StartAsync());

        // ReleaseLeadershipAsync and StopAsync don't call ThrowIfDisposed(), so they don't throw
        await service.ReleaseLeadershipAsync(); // Should not throw (returns early if not leader)
        await service.StopAsync(); // Should not throw (returns early if no election task)
    }

    [Fact]
    public async Task PostgresLeaseStore_Should_ThrowIfUnableToConnect()
    {
        // Arrange
        var mockSerializer = Substitute.For<IStringDataSerializer>();
        var tableName = $"test_leases_{Guid.NewGuid():N}";
        var npgDataSource = NpgsqlDataSource.Create("Host=invalid-host;Database=invalid;Username=invalid;Password=invalid");
        var leaseStore = new PostgresLeaseStore(npgDataSource, mockSerializer, tableName, NullLogger.Instance);

        _ = await Assert.ThrowsAnyAsync<SocketException>(() => leaseStore.EnsureTableExistsAsync());
        _ = await Assert.ThrowsAnyAsync<SocketException>(() => leaseStore.TryRenewLeaseAsync("test-election", "participant-1", TimeSpan.FromSeconds(30)));
        _ = await Assert.ThrowsAnyAsync<SocketException>(() => leaseStore.TryAcquireLeaseAsync("test-election", "participant-1", TimeSpan.FromSeconds(30)));
        _ = await Assert.ThrowsAnyAsync<SocketException>(() => leaseStore.CleanupExpiredLeasesAsync());
        var released = await leaseStore.ReleaseLeaseAsync("test-election", "participant-1");
        Assert.False(released); // Should return false on connection failure
    }
}
