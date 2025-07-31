using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class InProcessLeaseStoreSpecs
{
    private readonly DateTime _baseTime = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly TimeSpan _defaultLeaseDuration = TimeSpan.FromMinutes(5);
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";
    private const string AlternateParticipantId = "participant-2";

    [Fact]
    public async Task TryAcquireLeaseAsync_WithValidParameters_ShouldSucceedAndReturnLeaderInfo()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var result = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, metadata, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DefaultParticipantId, result.ParticipantId);
        Assert.Equal(_baseTime, result.AcquiredAt);
        Assert.Equal(_baseTime.Add(_defaultLeaseDuration), result.ExpiresAt);
        Assert.Equal(metadata, result.Metadata);
        Assert.True(result.ExpiresAt > _baseTime, "Lease should expire after the base time");

        store.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_WhenLeaseAlreadyExists_ShouldReturnNull()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        var firstResult = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var secondResult = await store.TryAcquireLeaseAsync(DefaultElectionName, AlternateParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstResult);
        Assert.Equal(DefaultParticipantId, firstResult.ParticipantId);
        Assert.Null(secondResult);

        store.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_WhenLeaseExpired_ShouldSucceedAndReturnNewLeaderInfo()
    {
        // Arrange
        var expiredTime = _baseTime.Add(_defaultLeaseDuration).AddSeconds(1);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, expiredTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        var firstResult = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var secondResult = await store.TryAcquireLeaseAsync(DefaultElectionName, AlternateParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstResult);
        Assert.Equal(DefaultParticipantId, firstResult.ParticipantId);
        Assert.NotNull(secondResult);
        Assert.Equal(AlternateParticipantId, secondResult.ParticipantId);
        Assert.Equal(expiredTime, secondResult.AcquiredAt);

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryAcquireLeaseAsync_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryAcquireLeaseAsync(electionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryAcquireLeaseAsync_WithInvalidParticipantId_ShouldThrowArgumentException(string participantId)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, participantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public async Task TryAcquireLeaseAsync_WithInvalidLeaseDuration_ShouldThrowArgumentException(int secondsFromZero)
    {
        // Arrange
        var store = new InProcessLeaseStore();
        var invalidDuration = TimeSpan.FromSeconds(secondsFromZero);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, invalidDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var store = new InProcessLeaseStore();
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: cancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var store = new InProcessLeaseStore();
        store.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryRenewLeaseAsync_WithValidLease_ShouldSucceedAndUpdateExpiry()
    {
        // Arrange
        var renewTime = _baseTime.AddMinutes(2);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, renewTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);
        var newMetadata = new Dictionary<string, string> { ["renewed"] = "true" };

        // Act
        var acquiredLease = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var renewedLease = await store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, newMetadata, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(acquiredLease);
        Assert.NotNull(renewedLease);
        Assert.Equal(DefaultParticipantId, renewedLease.ParticipantId);
        Assert.Equal(_baseTime, renewedLease.AcquiredAt); // Should preserve original acquisition time
        Assert.Equal(renewTime.Add(_defaultLeaseDuration), renewedLease.ExpiresAt);
        Assert.Equal(newMetadata, renewedLease.Metadata);

        store.Dispose();
    }

    [Fact]
    public async Task TryRenewLeaseAsync_WithNonExistentLease_ShouldReturnNull()
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act
        var result = await store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        store.Dispose();
    }

    [Fact]
    public async Task TryRenewLeaseAsync_WithDifferentParticipant_ShouldReturnNull()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.TryRenewLeaseAsync(DefaultElectionName, AlternateParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        store.Dispose();
    }

    [Fact]
    public async Task TryRenewLeaseAsync_WithExpiredLease_ShouldReturnNull()
    {
        // Arrange
        var expiredTime = _baseTime.Add(_defaultLeaseDuration).AddSeconds(1);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, expiredTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        store.Dispose();
    }

    [Fact]
    public async Task TryRenewLeaseAsync_WithNullMetadata_ShouldPreserveOriginalMetadata()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);
        var originalMetadata = new Dictionary<string, string> { ["original"] = "value" };

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, originalMetadata, TestContext.Current.CancellationToken);

        var renewedLease = await store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(renewedLease);
        Assert.Equal(originalMetadata, renewedLease.Metadata);

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryRenewLeaseAsync_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryRenewLeaseAsync(electionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TryRenewLeaseAsync_WithInvalidParticipantId_ShouldThrowArgumentException(string participantId)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryRenewLeaseAsync(DefaultElectionName, participantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public async Task TryRenewLeaseAsync_WithInvalidLeaseDuration_ShouldThrowArgumentException(int secondsFromZero)
    {
        // Arrange
        var store = new InProcessLeaseStore();
        var invalidDuration = TimeSpan.FromSeconds(secondsFromZero);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, invalidDuration, cancellationToken: TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task ReleaseLeaseAsync_WithValidLease_ShouldSucceed()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.ReleaseLeaseAsync(DefaultElectionName, DefaultParticipantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        // Verify lease is actually released
        var currentLease = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);
        Assert.Null(currentLease);

        store.Dispose();
    }

    [Fact]
    public async Task ReleaseLeaseAsync_WithNonExistentLease_ShouldReturnFalse()
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act
        var result = await store.ReleaseLeaseAsync(DefaultElectionName, DefaultParticipantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        store.Dispose();
    }

    [Fact]
    public async Task ReleaseLeaseAsync_WithDifferentParticipant_ShouldReturnFalse()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.ReleaseLeaseAsync(DefaultElectionName, AlternateParticipantId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReleaseLeaseAsync_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.ReleaseLeaseAsync(electionName, DefaultParticipantId, TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReleaseLeaseAsync_WithInvalidParticipantId_ShouldThrowArgumentException(string participantId)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.ReleaseLeaseAsync(DefaultElectionName, participantId, TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_WithValidLease_ShouldReturnLeaderInfo()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        var acquiredLease = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var currentLease = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(acquiredLease);
        Assert.NotNull(currentLease);
        Assert.Equal(acquiredLease.ParticipantId, currentLease.ParticipantId);
        Assert.Equal(acquiredLease.AcquiredAt, currentLease.AcquiredAt);
        Assert.Equal(acquiredLease.ExpiresAt, currentLease.ExpiresAt);

        store.Dispose();
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_WithNonExistentLease_ShouldReturnNull()
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act
        var result = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        store.Dispose();
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_WithExpiredLease_ShouldReturnNull()
    {
        // Arrange
        var expiredTime = _baseTime.Add(_defaultLeaseDuration).AddSeconds(1);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, expiredTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentLeaseAsync_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.GetCurrentLeaseAsync(electionName, TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task HasValidLeaseAsync_WithValidLease_ShouldReturnTrue()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.HasValidLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        store.Dispose();
    }

    [Fact]
    public async Task HasValidLeaseAsync_WithNonExistentLease_ShouldReturnFalse()
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act
        var result = await store.HasValidLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        store.Dispose();
    }

    [Fact]
    public async Task HasValidLeaseAsync_WithExpiredLease_ShouldReturnFalse()
    {
        // Arrange
        var expiredTime = _baseTime.Add(_defaultLeaseDuration).AddSeconds(1);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, expiredTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        var result = await store.HasValidLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        store.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasValidLeaseAsync_WithInvalidElectionName_ShouldThrowArgumentException(string electionName)
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentException>(() =>
            store.HasValidLeaseAsync(electionName, TestContext.Current.CancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task MultipleElections_ShouldBeIsolated()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, _baseTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);
        const string election1 = "election-1";
        const string election2 = "election-2";

        // Act
        var lease1 = await store.TryAcquireLeaseAsync(election1, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);
        var lease2 = await store.TryAcquireLeaseAsync(election2, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(lease1);
        Assert.NotNull(lease2);
        Assert.Equal(DefaultParticipantId, lease1.ParticipantId);
        Assert.Equal(DefaultParticipantId, lease2.ParticipantId);

        // Verify they are independent
        var hasLease1 = await store.HasValidLeaseAsync(election1, TestContext.Current.CancellationToken);
        var hasLease2 = await store.HasValidLeaseAsync(election2, TestContext.Current.CancellationToken);
        Assert.True(hasLease1);
        Assert.True(hasLease2);

        store.Dispose();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var dateTimeProvider = new DateTimeProvider(); // Use real provider for concurrency test
        var store = new InProcessLeaseStore(dateTimeProvider);
        const int participantCount = 10;
        const int iterationCount = 100;

        // Act
        var tasks = Enumerable.Range(0, participantCount)
            .Select(i => Task.Run(async () =>
            {
                var participantId = $"participant-{i}";
                var successCount = 0;

                for (int j = 0; j < iterationCount; j++)
                {
                    var electionName = $"election-{j % 5}"; // Multiple elections
                    var lease = await store.TryAcquireLeaseAsync(electionName, participantId, TimeSpan.FromMilliseconds(100));
                    if (lease != null)
                    {
                        successCount++;
                        await Task.Delay(50); // Hold lease briefly
                        _ = await store.ReleaseLeaseAsync(electionName, participantId);
                    }
                }

                return successCount;
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, count => Assert.True(count >= 0));
        Assert.True(results.Sum() > 0, "At least some operations should succeed");

        store.Dispose();
    }

    [Fact]
    public async Task CancellationToken_ShouldBePropagated()
    {
        // Arrange
        var store = new InProcessLeaseStore();
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: cancellationToken));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: cancellationToken));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.ReleaseLeaseAsync(DefaultElectionName, DefaultParticipantId, cancellationToken));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.GetCurrentLeaseAsync(DefaultElectionName, cancellationToken));

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            store.HasValidLeaseAsync(DefaultElectionName, cancellationToken));

        store.Dispose();
    }

    [Fact]
    public async Task ExpiredLeaseCleanup_ShouldRemoveExpiredEntries()
    {
        // Arrange
        var expiredTime = _baseTime.Add(_defaultLeaseDuration).AddSeconds(1);
        var dateTimeProvider = new DateTimeMockProvider([_baseTime, expiredTime, expiredTime]);
        var store = new InProcessLeaseStore(dateTimeProvider);

        // Act
        _ = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Trigger cleanup by calling GetCurrentLeaseAsync
        var currentLease = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);

        // Try to acquire with different participant (should succeed if cleanup worked)
        var newLease = await store.TryAcquireLeaseAsync(DefaultElectionName, AlternateParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(currentLease);
        Assert.NotNull(newLease);
        Assert.Equal(AlternateParticipantId, newLease.ParticipantId);

        store.Dispose();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var store = new InProcessLeaseStore();

        // Act & Assert - Should not throw
        store.Dispose();
        store.Dispose();
        store.Dispose();
#pragma warning disable S1215 // Suppress warning for GC.Collect in tests
        GC.Collect();
#pragma warning restore S1215
        Assert.True(true); // If we reach here, the test passes
    }

    [Fact]
    public async Task AllMethods_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var store = new InProcessLeaseStore();
        store.Dispose();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, _defaultLeaseDuration, cancellationToken: TestContext.Current.CancellationToken));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.ReleaseLeaseAsync(DefaultElectionName, DefaultParticipantId, TestContext.Current.CancellationToken));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken));

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            store.HasValidLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LeaseExpiration_ShouldWorkWithRealTime()
    {
        // Arrange
        var store = new InProcessLeaseStore(); // Use real DateTimeProvider
        var shortLease = TimeSpan.FromMilliseconds(50);

        // Act
        var lease = await store.TryAcquireLeaseAsync(DefaultElectionName, DefaultParticipantId, shortLease, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(lease);
        Assert.True(lease.IsValid);
        Assert.True(lease.TimeToExpiry > TimeSpan.Zero);

        // Wait for expiration
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert
        var currentLease = await store.GetCurrentLeaseAsync(DefaultElectionName, TestContext.Current.CancellationToken);
        Assert.Null(currentLease);

        // Should be able to acquire again
        var newLease = await store.TryAcquireLeaseAsync(DefaultElectionName, AlternateParticipantId, shortLease, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(newLease);
        Assert.Equal(AlternateParticipantId, newLease.ParticipantId);

        store.Dispose();
    }
}
