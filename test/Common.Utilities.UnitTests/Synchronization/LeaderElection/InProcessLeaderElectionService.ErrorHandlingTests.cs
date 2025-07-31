using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;
using AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection.TestHelpers;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

public class InProcessLeaderElectionServiceErrorHandlingTests
{
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithLeaseStoreException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Lease store error"));
        await using var service = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Act
        var result = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result); // Should return false on exception
        Assert.False(service.IsLeader); // Should not be leader

        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithOperationCanceledException_ShouldThrow()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.TryAcquireLeadershipAsync(cts.Token));
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithTimeoutException_ShouldReturnFalse()
    {
        // Arrange
        var slowLeaseStore = new SlowLeaseStore(TimeSpan.FromSeconds(2));
        var options = new LeaderElectionOptions
        {
            OperationTimeout = TimeSpan.FromMilliseconds(100) // Very short timeout
        };

        await using var service = new InProcessLeaderElectionService(
            slowLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        var result = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result); // Should return false on timeout
        Assert.False(service.IsLeader);

        slowLeaseStore.Dispose();
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_WithLeaseStoreException_ShouldNotThrow()
    {
        // Arrange
        var leaseStore = new InProcessLeaseStore();
        await using var service = new InProcessLeaderElectionService(
            leaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // First acquire leadership
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(service.IsLeader);

        // Replace with faulty store
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Release error"));
        await using var faultyService = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Act & Assert - Should not throw despite lease store error
        var exception = await Record.ExceptionAsync(() => faultyService.ReleaseLeadershipAsync(TestContext.Current.CancellationToken));
        Assert.Null(exception);

        leaseStore.Dispose();
        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task ElectionLoop_WithRepeatedExceptions_ShouldContinueRetrying()
    {
        // Arrange
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Repeated error"), failCount: 3);
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = true
        };

        await using var service = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(300, TestContext.Current.CancellationToken); // Allow multiple retry cycles
        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Should not crash despite repeated exceptions
        Assert.False(service.IsLeader);
        Assert.True(faultyLeaseStore.CallCount >= 3); // Should have retried multiple times

        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task ElectionLoop_WithCancellationDuringOperation_ShouldStopGracefully()
    {
        // Arrange
        var slowLeaseStore = new SlowLeaseStore(TimeSpan.FromSeconds(1));
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(100),
            EnableContinuousCheck = false
        };

        await using var service = new InProcessLeaderElectionService(
            slowLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Stop quickly while operations might be in progress
        using var cts = new CancellationTokenSource(50);
        var exception = await Record.ExceptionAsync(() => service.StopAsync(cts.Token));

        // Assert - Should handle cancellation gracefully
        Assert.True(exception == null || exception is OperationCanceledException);

        slowLeaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipChangedEvent_WithExceptionInHandler_ShouldNotCrashService()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        var eventFiredCount = 0;
        service.LeadershipChanged += (sender, args) =>
        {
            eventFiredCount++;
            throw new InvalidOperationException("Event handler error");
        };

        // Act & Assert - Should not throw despite event handler exception
        var exception = await Record.ExceptionAsync(() => service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken));
        Assert.Null(exception);
        Assert.True(service.IsLeader); // Should still acquire leadership
        Assert.Equal(1, eventFiredCount); // Event should have fired
    }

    [Fact]
    public async Task CheckCurrentLeaderAsync_WithLeaseStoreException_ShouldNotCrashElectionLoop()
    {
        // Arrange
        var intermittentLeaseStore = new IntermittentFaultyLeaseStore();
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = true
        };

        await using var service = new InProcessLeaderElectionService(
            intermittentLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken); // Allow multiple election loop cycles
        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Should survive intermittent lease store errors
        Assert.True(intermittentLeaseStore.GetCurrentLeaseCallCount > 0);

        intermittentLeaseStore.Dispose();
    }

    [Fact]
    public async Task RenewLeadershipAsync_WithLeaseStoreException_ShouldFailGracefully()
    {
        // Arrange
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Renewal error"));

        // Act & Assert - Should handle exception gracefully
        var exception = await Record.ExceptionAsync(() =>
            faultyLeaseStore.TryRenewLeaseAsync(DefaultElectionName, DefaultParticipantId, TimeSpan.FromMinutes(5), cancellationToken: TestContext.Current.CancellationToken));

        // Assert - Should propagate the exception from the faulty store
        Assert.NotNull(exception);
        _ = Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("Renewal error", exception.Message);

        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_WithStopAsyncException_ShouldNotThrow()
    {
        // Arrange
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Stop error"));
        var service = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Act & Assert - Should not throw despite StopAsync exception
        var exception = await Record.ExceptionAsync(() => service.DisposeAsync().AsTask());
        Assert.Null(exception);

        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task ServiceOperations_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        await service.DisposeAsync();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken));
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => service.StartAsync(TestContext.Current.CancellationToken));
    }
}
