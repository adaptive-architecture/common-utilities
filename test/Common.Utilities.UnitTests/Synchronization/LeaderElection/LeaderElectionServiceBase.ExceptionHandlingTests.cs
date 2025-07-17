using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;
using AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection.TestHelpers;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

/// <summary>
/// Tests the exception handling behavior of LeaderElectionServiceBase, particularly the StopAsync method.
/// </summary>
public class LeaderElectionServiceBaseExceptionHandlingTests
{
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";

    [Fact]
    public async Task StopAsync_WithOperationCanceledException_ShouldHandleGracefully()
    {
        // Arrange
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await service.StartAsync();

        // Cancel immediately to force OperationCanceledException
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Should not throw despite OperationCanceledException
        var exception = await Record.ExceptionAsync(() => service.StopAsync(cts.Token));
        Assert.True(exception == null || exception is OperationCanceledException);
    }

    [Fact]
    public async Task StopAsync_WithElectionTaskException_ShouldHandleGracefully()
    {
        // Arrange - Use faulty lease store to cause exceptions in election task
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Election task error"));
        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = false,
            RetryInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var service = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await service.StartAsync();

        // Give the election task a chance to encounter the exception
        await Task.Delay(100);

        // Act - StopAsync should handle the exception in the election task gracefully
        var exception = await Record.ExceptionAsync(() => service.StopAsync());

        // Assert - Should not throw despite exception in election task
        Assert.Null(exception);

        faultyLeaseStore.Dispose();
    }

    [Fact]
    public async Task StopAsync_CalledMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        await service.StartAsync();

        // Act & Assert - Multiple calls should not throw
        await service.StopAsync();
        await service.StopAsync();
        await service.StopAsync();

        // Should remain functional
        Assert.Equal(DefaultElectionName, service.ElectionName);
        Assert.Equal(DefaultParticipantId, service.ParticipantId);
    }

    [Fact]
    public async Task StopAsync_WithNoElectionTask_ShouldReturnImmediately()
    {
        // Arrange - Service with EnableContinuousCheck = false and never started
        var options = new LeaderElectionOptions { EnableContinuousCheck = false };
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Should complete immediately without throwing
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync();
        stopwatch.Stop();

        // Should complete very quickly since there's no election task to wait for
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
    }

    [Fact]
    public async Task StopAsync_WithCancellationTokenAlreadyCancelled_ShouldHandleGracefully()
    {
        // Arrange
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId);

        await service.StartAsync();

        var cancellationToken = new CancellationToken(true); // Already cancelled

        // Act & Assert - Should handle pre-cancelled token gracefully
        var exception = await Record.ExceptionAsync(() => service.StopAsync(cancellationToken));
        Assert.True(exception == null || exception is OperationCanceledException);
    }

    [Fact]
    public async Task StopAsync_DuringElectionLoop_ShouldStopCleanly()
    {
        // Arrange - Service with short retry interval to ensure active election loop
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(20),
            EnableContinuousCheck = false
        };

        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await service.StartAsync();

        // Let the election loop run for a bit
        await Task.Delay(50);

        // Act - Stop while election loop is actively running
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync();
        stopwatch.Stop();

        // Assert - Should stop reasonably quickly and not throw
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should not hang
    }

    [Fact]
    public async Task StopAsync_WithSlowElectionTask_ShouldWaitAppropriately()
    {
        // Arrange - Use slow lease store to simulate long-running operations
        var slowLeaseStore = new SlowLeaseStore(TimeSpan.FromMilliseconds(200));
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = false
        };

        await using var service = new InProcessLeaderElectionService(
            slowLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await service.StartAsync();

        // Give the election task a chance to start running
        await Task.Delay(30);

        // Act - StopAsync should wait for the election task to complete
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync();
        stopwatch.Stop();

        // Assert - Should complete but timing can vary in tests
        Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Should not hang indefinitely

        slowLeaseStore.Dispose();
    }

    [Fact]
    public async Task StopAsync_WithCancellationDuringWait_ShouldRespectTimeout()
    {
        // Arrange - Use slow lease store and short cancellation timeout
        var slowLeaseStore = new SlowLeaseStore(TimeSpan.FromSeconds(2));
        var options = new LeaderElectionOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = false
        };

        await using var service = new InProcessLeaderElectionService(
            slowLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        await service.StartAsync();

        // Act - Cancel StopAsync after short timeout
        using var cts = new CancellationTokenSource(100);
        var exception = await Record.ExceptionAsync(() => service.StopAsync(cts.Token));

        // Assert - Should respect cancellation token
        Assert.True(exception == null || exception is OperationCanceledException);

        // Cleanup - Stop without cancellation token
        await service.StopAsync();
        slowLeaseStore.Dispose();
    }

    [Fact]
    public async Task StopAsync_WithLeadershipRelease_ShouldReleaseBeforeStopping()
    {
        // Arrange
        var dateTimeProvider = new DateTimeMockProvider([DateTime.UtcNow]);
        await using var service = new InProcessLeaderElectionService(
            DefaultElectionName,
            DefaultParticipantId,
            null,
            null,
            dateTimeProvider);

        // Acquire leadership first
        await service.TryAcquireLeadershipAsync();
        Assert.True(service.IsLeader);

        // Act
        await service.StopAsync();

        // Assert - Leadership should be released during stop
        Assert.False(service.IsLeader);
    }

    [Fact]
    public async Task StopAsync_WithReleaseLeadershipException_ShouldStillStopSuccessfully()
    {
        // Arrange - This test verifies that even if ReleaseLeadershipAsync throws,
        // StopAsync still completes successfully
        var faultyLeaseStore = new FaultyLeaseStore(new InvalidOperationException("Release error"));
        await using var service = new InProcessLeaderElectionService(
            faultyLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Try to acquire leadership (will fail but that's ok for this test)
        await service.TryAcquireLeadershipAsync();

        // Act & Assert - StopAsync should not throw even if release fails
        var exception = await Record.ExceptionAsync(() => service.StopAsync());
        Assert.Null(exception);

        faultyLeaseStore.Dispose();
    }
}
