using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using NSubstitute;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.LeaderElection;

public class RedisLeaderElectionServiceSpecs
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly Dictionary<string, RedisValue> _redisStorage = [];

    public RedisLeaderElectionServiceSpecs()
    {
        _database = Substitute.For<IDatabase>();
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _connectionMultiplexer.GetDatabase(-1, null).Returns(_database);

        SetupDatabaseMocks();
    }

    // Clear storage before each test to ensure test isolation
    private void ResetStorage()
    {
        _redisStorage.Clear();
    }

    private void SetupDatabaseMocks()
    {
        // Mock StringSetAsync for lease acquisition (4-parameter overload)
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(callInfo =>
            {
                var key = callInfo.ArgAt<RedisKey>(0);
                var value = callInfo.ArgAt<RedisValue>(1);
                var when = callInfo.ArgAt<When>(3);

                if (when == When.NotExists && _redisStorage.ContainsKey(key!))
                {
                    return false; // Key already exists, can't set with NX
                }

                _redisStorage[key!] = value;
                return true;
            });

        // Mock StringGetAsync for getting current lease
        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                var key = callInfo.ArgAt<RedisKey>(0);
                return _redisStorage.TryGetValue(key!, out var value) ? value : RedisValue.Null;
            });

        // Mock ScriptEvaluateAsync for Lua scripts
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                var script = callInfo.ArgAt<string>(0);
                var keys = callInfo.ArgAt<RedisKey[]>(1);
                var values = callInfo.ArgAt<RedisValue[]>(2);

                if (script.Contains("redis.call('SET', key, newLeaseData, 'EX', ttlSeconds)")) // Renewal script
                {
                    var key = keys[0];
                    var participantId = values[0];
                    var newLeaseData = values[1];

                    if (!_redisStorage.TryGetValue(key!, out var currentLease))
                    {
                        return Task.FromResult(RedisResult.Create(RedisValue.Null));
                    }

                    if (currentLease.ToString().Contains($"\"ParticipantId\":\"{participantId}\""))
                    {
                        _redisStorage[key!] = newLeaseData;
                        return Task.FromResult(RedisResult.Create(newLeaseData));
                    }

                    return Task.FromResult(RedisResult.Create(RedisValue.Null));
                }
                else if (script.Contains("return redis.call('DEL', key)")) // Release script
                {
                    var key = keys[0];
                    var participantId = values[0];

                    if (!_redisStorage.TryGetValue(key!, out var currentLease))
                    {
                        return Task.FromResult(RedisResult.Create(0));
                    }

                    if (currentLease.ToString().Contains($"\"ParticipantId\":\"{participantId}\""))
                    {
                        _redisStorage.Remove(key!);
                        return Task.FromResult(RedisResult.Create(1));
                    }

                    return Task.FromResult(RedisResult.Create(0));
                }

                return Task.FromResult(RedisResult.Create(RedisValue.Null));
            });
    }

    [Fact]
    public void Constructor_Should_Throw_When_Arguments_Are_Null()
    {
        var serializer = new ReflectionJsonDataSerializer();

        Assert.Throws<ArgumentNullException>(() =>
            new RedisLeaderElectionService(null!, serializer, "election", "participant"));

        Assert.Throws<ArgumentNullException>(() =>
            new RedisLeaderElectionService(_connectionMultiplexer, null!, "election", "participant"));

        Assert.Throws<ArgumentException>(() =>
            new RedisLeaderElectionService(_connectionMultiplexer, serializer, "", "participant"));

        Assert.Throws<ArgumentException>(() =>
            new RedisLeaderElectionService(_connectionMultiplexer, serializer, "election", ""));
    }

    [Fact]
    public async Task Constructor_Should_Initialize_Properties_Correctly()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId);

        // Assert
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_Should_Acquire_Leadership_When_Available()
    {
        // Arrange
        ResetStorage(); // Clear any previous test data
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";

        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId);

        var leadershipChangedEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipChangedEvents.Add(args);

        // Act
        var result = await service.TryAcquireLeadershipAsync();

        // Assert
        Assert.True(result);
        Assert.True(service.IsLeader);
        Assert.NotNull(service.CurrentLeader);
        Assert.Equal(participantId, service.CurrentLeader.ParticipantId);

        // Check events
        Assert.Single(leadershipChangedEvents);
        var eventArgs = leadershipChangedEvents[0];
        Assert.True(eventArgs.IsLeader);
        Assert.True(eventArgs.LeadershipGained);
        Assert.False(eventArgs.LeadershipLost);
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_Should_Fail_When_Another_Leader_Exists()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";

        await using var service1 = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, "participant-1");
        await using var service2 = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, "participant-2");

        // First service acquires leadership
        await service1.TryAcquireLeadershipAsync();

        var leadershipChangedEvents = new List<LeadershipChangedEventArgs>();
        service2.LeadershipChanged += (_, args) => leadershipChangedEvents.Add(args);

        // Act - Second service tries to acquire
        var result = await service2.TryAcquireLeadershipAsync();

        // Assert
        Assert.False(result);
        Assert.False(service2.IsLeader);
        Assert.NotNull(service2.CurrentLeader); // Should know who the current leader is
        Assert.Equal("participant-1", service2.CurrentLeader.ParticipantId);

        // No leadership change events for service2
        Assert.Empty(leadershipChangedEvents);
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_Should_Release_Leadership_When_Leader()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";

        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId);

        // First acquire leadership
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        var leadershipChangedEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipChangedEvents.Add(args);

        // Act
        await service.ReleaseLeadershipAsync();

        // Assert
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        // Check events (only the release event, as we registered after acquire)
        Assert.Single(leadershipChangedEvents);
        var eventArgs = leadershipChangedEvents[0];
        Assert.False(eventArgs.IsLeader);
        Assert.False(eventArgs.LeadershipGained);
        Assert.True(eventArgs.LeadershipLost);
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_Should_Do_Nothing_When_Not_Leader()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";

        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId);

        var leadershipChangedEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipChangedEvents.Add(args);

        // Act - Try to release when not leader
        await service.ReleaseLeadershipAsync();

        // Assert
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        // No events should be fired
        Assert.Empty(leadershipChangedEvents);
    }

    [Fact]
    public async Task StartAsync_Should_Not_Start_Multiple_Times()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };

        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId, options);

        // Act - Start multiple times
        await service.StartAsync();
        await service.StartAsync(); // Should not throw or cause issues

        // Assert - Should complete without errors
        await service.StopAsync();
    }

    [Fact]
    public async Task StopAsync_Should_Release_Leadership_And_Stop_Service()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";

        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId);

        // Acquire leadership
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        var leadershipChangedEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (_, args) => leadershipChangedEvents.Add(args);

        // Act
        await service.StopAsync();

        // Assert
        Assert.False(service.IsLeader);

        // Should have fired leadership lost event
        Assert.Single(leadershipChangedEvents);
        var eventArgs = leadershipChangedEvents[0];
        Assert.True(eventArgs.LeadershipLost);
    }

    [Fact]
    public async Task Constructor_With_Existing_LeaseStore_Should_Work()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        var leaseStore = new RedisLeaseStore(_connectionMultiplexer, serializer);
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act
        await using var service = new RedisLeaderElectionService(leaseStore, electionName, participantId);

        // Assert
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);
        Assert.False(service.IsLeader);
    }

    [Fact]
    public async Task Constructor_Should_Accept_Custom_Options()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMinutes(10),
            RenewalInterval = TimeSpan.FromMinutes(3),
            EnableContinuousCheck = false
        };

        // Act
        await using var service = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, participantId, options);

        // Assert
        Assert.Equal(electionName, service.ElectionName);
        Assert.Equal(participantId, service.ParticipantId);
    }

    [Fact]
    public async Task Service_Should_Handle_Concurrent_Leadership_Attempts()
    {
        // Arrange
        var serializer = new ReflectionJsonDataSerializer();
        const string electionName = "test-election";

        await using var service1 = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, "participant-1");
        await using var service2 = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, "participant-2");
        await using var service3 = new RedisLeaderElectionService(_connectionMultiplexer, serializer, electionName, "participant-3");

        // Act - Multiple services try to acquire leadership simultaneously
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

        // Verify only one is leader
        var leaders = new[] { service1, service2, service3 }.Where(s => s.IsLeader).ToList();
        Assert.Single(leaders);
    }
}
