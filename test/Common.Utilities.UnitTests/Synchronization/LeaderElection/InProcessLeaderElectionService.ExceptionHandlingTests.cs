using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;
using AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection.TestHelpers;
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection;

/// <summary>
/// Tests for verifying that the leader election service handles exceptions from lease stores gracefully
/// and doesn't let exceptions propagate outside the service.
/// </summary>
public class InProcessLeaderElectionServiceExceptionHandlingTests
{
    private const string DefaultElectionName = "test-election";
    private const string DefaultParticipantId = "participant-1";

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithExceptionLeaseStore_ShouldReturnFalseAndNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Act
        var result = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);
        Assert.True(exceptionLeaseStore.CallCount > 0);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task TryAcquireLeadershipAsync_WithDifferentExceptionTypes_ShouldHandleGracefully()
    {
        // Test various exception types
        var exceptions = new Exception[]
        {
            new ArgumentException("Argument exception"),
            new InvalidOperationException("Invalid operation"),
            new TimeoutException("Timeout exception"),
            new NotSupportedException("Not supported"),
            new UnauthorizedAccessException("Unauthorized access")
        };

        foreach (var exception in exceptions)
        {
            // Arrange
            var exceptionLeaseStore = new ExceptionLeaseStore(exception);

            await using var service = new InProcessLeaderElectionService(
                exceptionLeaseStore,
                DefaultElectionName,
                DefaultParticipantId);

            // Act
            var result = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result);
            Assert.False(service.IsLeader);
            Assert.Null(service.CurrentLeader);

            exceptionLeaseStore.Dispose();
        }
    }

    [Fact]
    public async Task ReleaseLeadershipAsync_WithExceptionLeaseStore_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        // Act & Assert - Should not throw even if lease store throws
        await service.ReleaseLeadershipAsync(TestContext.Current.CancellationToken);

        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task StartStopAsync_WithExceptionLeaseStore_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(50),
            RenewalInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Should not throw even with continuous exceptions
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Let it run for a bit to try multiple operations
        await Task.Delay(200, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(service.IsLeader);
        Assert.True(exceptionLeaseStore.CallCount > 0);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task ElectionLoop_WithExceptionLeaseStore_ShouldContinueRetrying()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(25),
            RenewalInterval = TimeSpan.FromMilliseconds(25)
        };

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Let it run for a bit to verify it keeps retrying despite exceptions
        await Task.Delay(150, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(service.IsLeader);
        // Should have made multiple attempts despite exceptions
        Assert.True(exceptionLeaseStore.CallCount >= 3,
            $"Expected multiple retry attempts, but got {exceptionLeaseStore.CallCount}");

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipChanged_WithExceptionLeaseStore_ShouldNotFireForFailedOperations()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (sender, args) => leadershipEvents.Add(args);

        // Act
        _ = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);

        // Assert - No leadership events should be fired for failed operations
        Assert.Empty(leadershipEvents);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task GetCurrentLeaseAsync_WithExceptionLeaseStore_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Should not throw even when GetCurrentLeaseAsync fails
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Let it run for a bit to trigger GetCurrentLeaseAsync calls
        await Task.Delay(150, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        Assert.False(service.IsLeader);
        Assert.True(exceptionLeaseStore.CallCount > 0);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task CancellationDuringExceptions_ShouldHandleGracefully()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        using var cts = new CancellationTokenSource(100);

        // Start with cancellation token
        await service.StartAsync(cts.Token);

        // Let it run until cancellation
        await Task.Delay(150, TestContext.Current.CancellationToken);

        // Stop should complete gracefully
        var exception2 = await Record.ExceptionAsync(() => service.StopAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception2);
        Assert.False(service.IsLeader);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task MultipleServicesWithExceptionLeaseStore_ShouldAllHandleExceptionsGracefully()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var service1 = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            "participant-1",
            options);

        await using var service2 = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            "participant-2",
            options);

        // Act
        await service1.StartAsync(TestContext.Current.CancellationToken);
        await service2.StartAsync(TestContext.Current.CancellationToken);

        // Let them run for a bit
        await Task.Delay(150, TestContext.Current.CancellationToken);

        await service1.StopAsync(TestContext.Current.CancellationToken);
        await service2.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(service1.IsLeader);
        Assert.False(service2.IsLeader);
        Assert.True(exceptionLeaseStore.CallCount > 0);

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task OperationCanceledException_ShouldBeHandledSpecially()
    {
        // Arrange
        var cancellationException = new OperationCanceledException("Operation was cancelled");
        var exceptionLeaseStore = new ExceptionLeaseStore(cancellationException);

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId);

        using var cts = new CancellationTokenSource();

        // Act
        var result = await service.TryAcquireLeadershipAsync(cts.Token);

        // Assert - Should handle OperationCanceledException like any other exception
        Assert.False(result);
        Assert.False(service.IsLeader);
        Assert.Null(service.CurrentLeader);

        exceptionLeaseStore.Dispose();
    }

    [RetryFact]
    public async Task ExceptionInRenewalLoop_ShouldNotStopRetries()
    {
        // Arrange
        var exception = new InvalidOperationException("Renewal failed");
        var exceptionLeaseStore = new ExceptionLeaseStore(exception);

        var options = new LeaderElectionOptions
        {
            EnableContinuousCheck = true,
            RetryInterval = TimeSpan.FromMilliseconds(30),
            RenewalInterval = TimeSpan.FromMilliseconds(30)
        };

        await using var service = new InProcessLeaderElectionService(
            exceptionLeaseStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        await service.StartAsync(TestContext.Current.CancellationToken);

        var initialCallCount = exceptionLeaseStore.CallCount;

        // Let it run for multiple retry intervals
        await Task.Delay(200, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        var finalCallCount = exceptionLeaseStore.CallCount;

        // Assert
        Assert.False(service.IsLeader);
        // Should have made multiple attempts despite continuous exceptions
        Assert.True(finalCallCount > initialCallCount + 3,
            $"Expected multiple retry attempts, initial: {initialCallCount}, final: {finalCallCount}");

        exceptionLeaseStore.Dispose();
    }

    [Fact]
    public async Task LeadershipAcquired_ThenRenewalFails_ShouldLoseLeadership()
    {
        // Arrange
        var renewalException = new InvalidOperationException("Renewal failed");
        var acquireSuccessRenewalFailStore = new AcquireSuccessRenewalFailLeaseStore(renewalException);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(200),
            RenewalInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = true
        };

        await using var service = new InProcessLeaderElectionService(
            acquireSuccessRenewalFailStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        var leadershipEvents = new List<LeadershipChangedEventArgs>();
        service.LeadershipChanged += (sender, args) => leadershipEvents.Add(args);

        // Act
        // First acquire leadership manually to ensure we start as leader
        var acquireResult = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(acquireResult);
        Assert.True(service.IsLeader);

        // Start the election loop which will trigger renewal attempts
        await service.StartAsync(TestContext.Current.CancellationToken);

        // Wait for renewal attempts to fail and leadership to be lost
        await Task.Delay(300, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(service.IsLeader); // Should have lost leadership due to renewal failure
        Assert.True(acquireSuccessRenewalFailStore.AcquireCallCount > 0); // Should have acquired initially
        Assert.True(acquireSuccessRenewalFailStore.RenewCallCount > 0); // Should have attempted renewal

        // Should have leadership gained event initially
        Assert.Contains(leadershipEvents, e => e.LeadershipGained);

        // Should have leadership lost event when renewal fails
        Assert.Contains(leadershipEvents, e => e.LeadershipLost);

        acquireSuccessRenewalFailStore.Dispose();
    }

    [Fact]
    public async Task LeadershipAcquired_ThenRenewalFails_ShouldNotThrowException()
    {
        // Arrange
        var renewalException = new InvalidOperationException("Renewal failed");
        var acquireSuccessRenewalFailStore = new AcquireSuccessRenewalFailLeaseStore(renewalException);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(200),
            RenewalInterval = TimeSpan.FromMilliseconds(50),
            EnableContinuousCheck = true
        };

        await using var service = new InProcessLeaderElectionService(
            acquireSuccessRenewalFailStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act & Assert - Should not throw any exceptions
        var acquireResult = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(acquireResult);
        Assert.True(service.IsLeader);

        // Start the election loop - this should not throw despite renewal failures
        var startException = await Record.ExceptionAsync(() => service.StartAsync(TestContext.Current.CancellationToken));
        Assert.Null(startException);

        // Let renewal attempts fail
        await Task.Delay(300, TestContext.Current.CancellationToken);

        // Stop should also not throw
        var stopException = await Record.ExceptionAsync(() => service.StopAsync(TestContext.Current.CancellationToken));
        Assert.Null(stopException);

        // Should have lost leadership due to renewal failure
        Assert.False(service.IsLeader);
        Assert.True(acquireSuccessRenewalFailStore.RenewCallCount > 0);

        acquireSuccessRenewalFailStore.Dispose();
    }

    [RetryFact]
    public async Task LeadershipAcquired_ThenRenewalFails_ShouldContinueRetrying()
    {
        // Arrange
        var renewalException = new InvalidOperationException("Renewal failed");
        var acquireSuccessRenewalFailStore = new AcquireSuccessRenewalFailLeaseStore(renewalException);

        var options = new LeaderElectionOptions
        {
            LeaseDuration = TimeSpan.FromMilliseconds(200),
            RenewalInterval = TimeSpan.FromMilliseconds(30),
            RetryInterval = TimeSpan.FromMilliseconds(30),
            EnableContinuousCheck = true
        };

        await using var service = new InProcessLeaderElectionService(
            acquireSuccessRenewalFailStore,
            DefaultElectionName,
            DefaultParticipantId,
            options);

        // Act
        var acquireResult = await service.TryAcquireLeadershipAsync(TestContext.Current.CancellationToken);
        Assert.True(acquireResult);
        Assert.True(service.IsLeader);

        await service.StartAsync(TestContext.Current.CancellationToken);

        var initialTotalCallCount = acquireSuccessRenewalFailStore.AcquireCallCount + acquireSuccessRenewalFailStore.RenewCallCount;

        // Let it run for multiple intervals - it will try to renew first, fail, lose leadership,
        // then try to acquire again multiple times
        await Task.Delay(250, TestContext.Current.CancellationToken);

        await service.StopAsync(TestContext.Current.CancellationToken);

        var finalTotalCallCount = acquireSuccessRenewalFailStore.AcquireCallCount + acquireSuccessRenewalFailStore.RenewCallCount;

        // Assert
        Assert.False(service.IsLeader); // Should have lost leadership

        // Should have tried to renew at least once (and failed)
        Assert.True(acquireSuccessRenewalFailStore.RenewCallCount > 0,
            $"Expected at least one renewal attempt, got: {acquireSuccessRenewalFailStore.RenewCallCount}");

        // Should have made multiple attempts (renew + acquire retries) after starting
        Assert.True(finalTotalCallCount > initialTotalCallCount + 2,
            $"Expected multiple retry attempts after starting, initial: {initialTotalCallCount}, final: {finalTotalCallCount}");

        acquireSuccessRenewalFailStore.Dispose();
    }
}
