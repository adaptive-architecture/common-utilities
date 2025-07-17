using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.UnitTests.Synchronization.LeaderElection.TestHelpers;

/// <summary>
/// Mock lease store that throws exceptions to simulate various error conditions.
/// </summary>
internal class FaultyLeaseStore : ILeaseStore, IDisposable
{
    private readonly Exception _exception;
    private readonly int _failCount;
    private int _callCount;

    public int CallCount => _callCount;

    public FaultyLeaseStore(Exception exception, int failCount = Int32.MaxValue)
    {
        _exception = exception;
        _failCount = failCount;
    }

    public Task<LeaderInfo> TryAcquireLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        if (_callCount <= _failCount)
            throw _exception;
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<LeaderInfo> TryRenewLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        if (_callCount <= _failCount)
            throw _exception;
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<bool> ReleaseLeaseAsync(string electionName, string participantId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        if (_callCount <= _failCount)
            throw _exception;
        return Task.FromResult(false);
    }

    public Task<LeaderInfo> GetCurrentLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        if (_callCount <= _failCount)
            throw _exception;
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<bool> HasValidLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        if (_callCount <= _failCount)
            throw _exception;
        return Task.FromResult(false);
    }

    public void Dispose() { }
}

/// <summary>
/// Mock lease store that introduces delays to simulate slow operations and test timeout scenarios.
/// </summary>
internal class SlowLeaseStore : ILeaseStore, IDisposable
{
    private readonly TimeSpan _delay;

    public SlowLeaseStore(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task<LeaderInfo> TryAcquireLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return new LeaderInfo
        {
            ParticipantId = participantId,
            AcquiredAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(leaseDuration),
            Metadata = metadata
        };
    }

    public async Task<LeaderInfo> TryRenewLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return null;
    }

    public async Task<bool> ReleaseLeaseAsync(string electionName, string participantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return true;
    }

    public async Task<LeaderInfo> GetCurrentLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return null;
    }

    public async Task<bool> HasValidLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return false;
    }

    public void Dispose() { }
}

/// <summary>
/// Mock lease store that fails intermittently to test resilience and retry logic.
/// </summary>
internal class IntermittentFaultyLeaseStore : ILeaseStore, IDisposable
{
    private int _getCurrentLeaseCallCount;
    private int _totalCallCount;

    public int GetCurrentLeaseCallCount => _getCurrentLeaseCallCount;

    public Task<LeaderInfo> TryAcquireLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalCallCount);
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<LeaderInfo> TryRenewLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalCallCount);
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<bool> ReleaseLeaseAsync(string electionName, string participantId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalCallCount);
        return Task.FromResult(false);
    }

    public Task<LeaderInfo> GetCurrentLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _getCurrentLeaseCallCount);
        Interlocked.Increment(ref _totalCallCount);

        // Fail every other call
        if (_totalCallCount % 2 == 0)
            throw new InvalidOperationException("Intermittent error");

        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<bool> HasValidLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _totalCallCount);
        return Task.FromResult(false);
    }

    public void Dispose() { }
}

/// <summary>
/// Mock lease store that always returns Task.FromException to test exception handling behavior.
/// </summary>
internal class ExceptionLeaseStore : ILeaseStore, IDisposable
{
    private readonly Exception _exception;
    private int _callCount;

    public int CallCount => _callCount;

    public ExceptionLeaseStore(Exception exception)
    {
        _exception = exception;
    }

    public Task<LeaderInfo> TryAcquireLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromException<LeaderInfo>(_exception);
    }

    public Task<LeaderInfo> TryRenewLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromException<LeaderInfo>(_exception);
    }

    public Task<bool> ReleaseLeaseAsync(string electionName, string participantId, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromException<bool>(_exception);
    }

    public Task<LeaderInfo> GetCurrentLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromException<LeaderInfo>(_exception);
    }

    public Task<bool> HasValidLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromException<bool>(_exception);
    }

    public void Dispose() { }
}

/// <summary>
/// Mock lease store that succeeds on acquire but throws exceptions on renewal.
/// Used to test scenarios where leadership is acquired but then renewal fails.
/// </summary>
internal class AcquireSuccessRenewalFailLeaseStore : ILeaseStore, IDisposable
{
    private readonly Exception _renewalException;
    private readonly IDateTimeProvider _dateTimeProvider;
    private int _acquireCallCount;
    private int _renewCallCount;
    private bool _hasAcquired;

    public int AcquireCallCount => _acquireCallCount;
    public int RenewCallCount => _renewCallCount;

    public AcquireSuccessRenewalFailLeaseStore(Exception renewalException, IDateTimeProvider dateTimeProvider = null)
    {
        _renewalException = renewalException;
        _dateTimeProvider = dateTimeProvider ?? new AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.DateTimeProvider();
    }

    public Task<LeaderInfo> TryAcquireLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _acquireCallCount);

        if (!_hasAcquired)
        {
            _hasAcquired = true;
            var now = _dateTimeProvider.UtcNow;
            return Task.FromResult(new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = now,
                ExpiresAt = now.Add(leaseDuration),
                Metadata = metadata
            });
        }

        // Subsequent acquire attempts fail (lease already held)
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<LeaderInfo> TryRenewLeaseAsync(string electionName, string participantId, TimeSpan leaseDuration, IReadOnlyDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _renewCallCount);
        return Task.FromException<LeaderInfo>(_renewalException);
    }

    public Task<bool> ReleaseLeaseAsync(string electionName, string participantId, CancellationToken cancellationToken = default)
    {
        _hasAcquired = false;
        return Task.FromResult(true);
    }

    public Task<LeaderInfo> GetCurrentLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        // Always return null after renewal failures to simulate lease loss
        return Task.FromResult<LeaderInfo>(null);
    }

    public Task<bool> HasValidLeaseAsync(string electionName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_hasAcquired);
    }

    public void Dispose() { }
}
