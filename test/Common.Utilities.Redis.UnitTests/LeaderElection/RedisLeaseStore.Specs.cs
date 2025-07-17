using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Redis.LeaderElection;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using NSubstitute;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.LeaderElection;

public class RedisLeaseStoreSpecs
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisLeaseStore _leaseStore;
    private readonly Dictionary<string, RedisValue> _redisStorage = [];

    public RedisLeaseStoreSpecs()
    {
        _database = Substitute.For<IDatabase>();
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _connectionMultiplexer.GetDatabase(-1, null).Returns(_database);

        var serializer = new ReflectionJsonDataSerializer();
        _leaseStore = new RedisLeaseStore(_connectionMultiplexer, serializer, NullLogger.Instance);

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

        // Mock ScriptEvaluateAsync for Lua scripts (renewal and release)
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
                        return Task.FromResult(RedisResult.Create(RedisValue.Null)); // No current lease
                    }

                    // Simulate parsing JSON to check participant ID
                    if (currentLease.ToString().Contains($"\"ParticipantId\":\"{participantId}\""))
                    {
                        _redisStorage[key!] = newLeaseData;
                        return Task.FromResult(RedisResult.Create(newLeaseData));
                    }

                    return Task.FromResult(RedisResult.Create(RedisValue.Null)); // Wrong participant
                }
                else if (script.Contains("return redis.call('DEL', key)")) // Release script
                {
                    var key = keys[0];
                    var participantId = values[0];

                    if (!_redisStorage.TryGetValue(key!, out var currentLease))
                    {
                        return Task.FromResult(RedisResult.Create(0)); // No current lease
                    }

                    // Simulate parsing JSON to check participant ID
                    if (currentLease.ToString().Contains($"\"ParticipantId\":\"{participantId}\""))
                    {
                        _redisStorage.Remove(key!);
                        return Task.FromResult(RedisResult.Create(1)); // Successfully deleted
                    }

                    return Task.FromResult(RedisResult.Create(0)); // Wrong participant
                }

                return Task.FromResult(RedisResult.Create(RedisValue.Null));
            });

        // Mock KeyDeleteAsync for cleanup
        _database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                var key = callInfo.ArgAt<RedisKey>(0);
                return _redisStorage.Remove(key!);
            });
    }

    [Fact]
    public void Constructor_Should_Throw_When_Arguments_Are_Null()
    {
        var serializer = new ReflectionJsonDataSerializer();

        Assert.Throws<ArgumentNullException>(() => new RedisLeaseStore(null!, serializer));
        Assert.Throws<ArgumentNullException>(() => new RedisLeaseStore(_connectionMultiplexer, null!));
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Acquire_Lease_When_None_Exists()
    {
        // Arrange
        ResetStorage(); // Clear any previous test data
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);
        var metadata = new Dictionary<string, string> { ["version"] = "1.0" };

        // Act
        var result = await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(participantId, result.ParticipantId);
        Assert.Equal(metadata, result.Metadata);
        Assert.True(result.IsValid);
        Assert.True(result.TimeToExpiry > TimeSpan.Zero);
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Fail_When_Lease_Already_Exists()
    {
        // Arrange
        const string electionName = "test-election";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First participant acquires lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participant1, leaseDuration);

        // Act - Second participant tries to acquire
        var result = await _leaseStore.TryAcquireLeaseAsync(electionName, participant2, leaseDuration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRenewLeaseAsync_Should_Renew_When_Current_Holder()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);
        var newLeaseDuration = TimeSpan.FromMinutes(10);

        // First acquire lease
        var originalLease = await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration);
        Assert.NotNull(originalLease);

        // Act - Renew lease
        var renewedLease = await _leaseStore.TryRenewLeaseAsync(electionName, participantId, newLeaseDuration);

        // Assert
        Assert.NotNull(renewedLease);
        Assert.Equal(participantId, renewedLease.ParticipantId);
        Assert.True(renewedLease.AcquiredAt >= originalLease.AcquiredAt);
    }

    [Fact]
    public async Task TryRenewLeaseAsync_Should_Fail_When_Not_Current_Holder()
    {
        // Arrange
        const string electionName = "test-election";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First participant acquires lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participant1, leaseDuration);

        // Act - Second participant tries to renew
        var result = await _leaseStore.TryRenewLeaseAsync(electionName, participant2, leaseDuration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryRenewLeaseAsync_Should_Fail_When_No_Lease_Exists()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // Act - Try to renew non-existent lease
        var result = await _leaseStore.TryRenewLeaseAsync(electionName, participantId, leaseDuration);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReleaseLeaseAsync_Should_Release_When_Current_Holder()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First acquire lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration);

        // Act - Release lease
        var result = await _leaseStore.ReleaseLeaseAsync(electionName, participantId);

        // Assert
        Assert.True(result);

        // Verify lease is gone
        var currentLease = await _leaseStore.GetCurrentLeaseAsync(electionName);
        Assert.Null(currentLease);
    }

    [Fact]
    public async Task ReleaseLeaseAsync_Should_Fail_When_Not_Current_Holder()
    {
        // Arrange
        const string electionName = "test-election";
        const string participant1 = "participant-1";
        const string participant2 = "participant-2";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First participant acquires lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participant1, leaseDuration);

        // Act - Second participant tries to release
        var result = await _leaseStore.ReleaseLeaseAsync(electionName, participant2);

        // Assert
        Assert.False(result);

        // Verify lease still exists
        var currentLease = await _leaseStore.GetCurrentLeaseAsync(electionName);
        Assert.NotNull(currentLease);
        Assert.Equal(participant1, currentLease.ParticipantId);
    }

    [Fact]
    public async Task ReleaseLeaseAsync_Should_Return_False_When_No_Lease_Exists()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Act - Try to release non-existent lease
        var result = await _leaseStore.ReleaseLeaseAsync(electionName, participantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Return_Lease_When_Exists()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);
        var metadata = new Dictionary<string, string> { ["version"] = "1.0" };

        // First acquire lease
        var originalLease = await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration, metadata);

        // Act
        var currentLease = await _leaseStore.GetCurrentLeaseAsync(electionName);

        // Assert
        Assert.NotNull(currentLease);
        Assert.Equal(participantId, currentLease.ParticipantId);
        Assert.Equal(metadata, currentLease.Metadata);
        Assert.True(currentLease.IsValid);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Return_Null_When_No_Lease_Exists()
    {
        // Arrange
        const string electionName = "test-election";

        // Act
        var currentLease = await _leaseStore.GetCurrentLeaseAsync(electionName);

        // Assert
        Assert.Null(currentLease);
    }

    [Fact]
    public async Task HasValidLeaseAsync_Should_Return_True_When_Valid_Lease_Exists()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First acquire lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration);

        // Act
        var hasValidLease = await _leaseStore.HasValidLeaseAsync(electionName);

        // Assert
        Assert.True(hasValidLease);
    }

    [Fact]
    public async Task HasValidLeaseAsync_Should_Return_False_When_No_Lease_Exists()
    {
        // Arrange
        const string electionName = "test-election";

        // Act
        var hasValidLease = await _leaseStore.HasValidLeaseAsync(electionName);

        // Assert
        Assert.False(hasValidLease);
    }

    [Fact]
    public void Dispose_Should_Not_Throw()
    {
        // Act & Assert
        _leaseStore.Dispose();
        _leaseStore.Dispose(); // Should be safe to call multiple times
        Assert.True(true); // If we reach here, the test passes
    }

    [Fact]
    public async Task Operations_Should_Throw_After_Disposal()
    {
        // Arrange
        _leaseStore.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _leaseStore.TryAcquireLeaseAsync("test", "participant", TimeSpan.FromMinutes(1)));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _leaseStore.TryRenewLeaseAsync("test", "participant", TimeSpan.FromMinutes(1)));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _leaseStore.ReleaseLeaseAsync("test", "participant"));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _leaseStore.GetCurrentLeaseAsync("test"));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            _leaseStore.HasValidLeaseAsync("test"));
    }

    #region Error Handling Tests

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Propagate_Redis_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var redisException = new RedisException("Redis connection failed");
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns<Task<bool>>(_ => throw redisException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisException>(() =>
            _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Redis connection failed", exception.Message);
    }

    [Fact]
    public async Task TryRenewLeaseAsync_Should_Propagate_Redis_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var redisException = new RedisException("Redis script execution failed");
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisResult>>(_ => throw redisException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisException>(() =>
            _leaseStore.TryRenewLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Redis script execution failed", exception.Message);
    }

    [Fact]
    public async Task ReleaseLeaseAsync_Should_Return_False_On_Redis_Exception()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";

        var redisException = new RedisException("Redis script execution failed");
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisResult>>(_ => throw redisException);

        // Act
        var result = await _leaseStore.ReleaseLeaseAsync(electionName, participantId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Propagate_Redis_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";

        var redisException = new RedisException("Redis get operation failed");
        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisValue>>(_ => throw redisException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisException>(() =>
            _leaseStore.GetCurrentLeaseAsync(electionName));

        Assert.Equal("Redis get operation failed", exception.Message);
    }

    [Fact]
    public async Task HasValidLeaseAsync_Should_Propagate_Redis_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";

        var redisException = new RedisException("Redis get operation failed");
        _database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisValue>>(_ => throw redisException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisException>(() =>
            _leaseStore.HasValidLeaseAsync(electionName));

        Assert.Equal("Redis get operation failed", exception.Message);
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Handle_Serialization_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var mockSerializer = Substitute.For<IDataSerializer>();
        mockSerializer.Serialize(Arg.Any<LeaderInfo>())
            .Returns(_ => throw new InvalidOperationException("Serialization failed"));

        using var leaseStore = new RedisLeaseStore(_connectionMultiplexer, mockSerializer, NullLogger.Instance);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Serialization failed", exception.Message);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Handle_Deserialization_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";

        // Setup Redis to return valid data
        _redisStorage["leader_election:lease:test-election"] = "{\"invalid\":\"json\"}";

        var mockSerializer = Substitute.For<IDataSerializer>();
        mockSerializer.When(x => x.Deserialize<LeaderInfo>(Arg.Any<RedisValue>()))
            .Do(_ => throw new InvalidOperationException("Deserialization failed"));

        using var leaseStore = new RedisLeaseStore(_connectionMultiplexer, mockSerializer, NullLogger.Instance);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            leaseStore.GetCurrentLeaseAsync(electionName));

        Assert.Equal("Deserialization failed", exception.Message);
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Handle_Timeout_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var timeoutException = new RedisTimeoutException("Operation timed out", CommandStatus.Unknown);
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns<Task<bool>>(_ => throw timeoutException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisTimeoutException>(() =>
            _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Operation timed out", exception.Message);
    }

    [Fact]
    public async Task TryRenewLeaseAsync_Should_Handle_Connection_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var connectionException = new RedisConnectionException(ConnectionFailureType.SocketFailure, "Connection lost");
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisResult>>(_ => throw connectionException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RedisConnectionException>(() =>
            _leaseStore.TryRenewLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Connection lost", exception.Message);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Handle_Null_Or_Empty_Redis_Values()
    {
        // Arrange
        const string electionName = "test-election";

        // Test with empty string
        _redisStorage["leader_election:lease:test-election"] = "";

        // Act
        var result1 = await _leaseStore.GetCurrentLeaseAsync(electionName);

        // Assert
        Assert.Null(result1);

        // Test with null value (key doesn't exist)
        _redisStorage.Remove("leader_election:lease:test-election");

        // Act
        var result2 = await _leaseStore.GetCurrentLeaseAsync(electionName);

        // Assert
        Assert.Null(result2);
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_Should_Handle_Database_GetDatabase_Exceptions()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        var mockConnectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        mockConnectionMultiplexer.GetDatabase(-1, null)
            .Returns(_ => throw new InvalidOperationException("Database unavailable"));

        var serializer = new ReflectionJsonDataSerializer();
        using var leaseStore = new RedisLeaseStore(mockConnectionMultiplexer, serializer, NullLogger.Instance);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration));

        Assert.Equal("Database unavailable", exception.Message);
    }

    [Fact]
    public async Task ReleaseLeaseAsync_Should_Handle_Script_Evaluation_Returning_Unexpected_Type()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";

        // Setup script to return unexpected result type
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(RedisResult.Create((RedisValue)"unexpected_string")));

        // Act & Assert - Should handle gracefully and return false
        var result = await _leaseStore.ReleaseLeaseAsync(electionName, participantId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_Should_Clean_Up_Expired_Lease_On_KeyDelete_Exception()
    {
        // Arrange
        const string electionName = "test-election";
        const string participantId = "participant-1";
        var leaseDuration = TimeSpan.FromMinutes(5);

        // First acquire a lease
        await _leaseStore.TryAcquireLeaseAsync(electionName, participantId, leaseDuration);

        // Modify the lease to be expired (hack the stored JSON)
        var expiredTime = DateTime.UtcNow.AddMinutes(-10);
        var expiredLease = new LeaderInfo
        {
            ParticipantId = participantId,
            AcquiredAt = expiredTime.AddMinutes(-5),
            ExpiresAt = expiredTime,
            Metadata = null
        };
        var serializer = new ReflectionJsonDataSerializer();
        _redisStorage["leader_election:lease:test-election"] = serializer.Serialize(expiredLease);

        // Setup KeyDeleteAsync to throw an exception
        _database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromException<bool>(new RedisException("Delete operation failed")));

        // Act & Assert - Should propagate the exception from KeyDeleteAsync
        var exception = await Assert.ThrowsAsync<RedisException>(() =>
            _leaseStore.GetCurrentLeaseAsync(electionName));

        Assert.Equal("Delete operation failed", exception.Message);
    }

    #endregion
}
