using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

/// <summary>
/// In-process implementation of <see cref="ILeaseStore"/> using concurrent collections.
/// This implementation is suitable for leader election within the same application process.
/// </summary>
public class InProcessLeaseStore : ILeaseStore, IDisposable
{
    private readonly ConcurrentDictionary<string, LeaseEntry> _leases = new();
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessLeaseStore"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The date time provider. If null, a default provider will be used.</param>
    public InProcessLeaseStore(IDateTimeProvider? dateTimeProvider = null)
    {
        _dateTimeProvider = dateTimeProvider ?? new DateTimeProvider();
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(electionName, participantId, leaseDuration);
        ValidateState(cancellationToken);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _dateTimeProvider.UtcNow;

            // Check if there's an existing valid lease
            if (_leases.TryGetValue(electionName, out var existingLease))
            {
                if (existingLease.ExpiresAt > now)
                {
                    // Lease is still valid, can't acquire
                    return null;
                }

                // Lease has expired, remove it
                _ = _leases.TryRemove(electionName, out _);
            }

            // Create new lease
            var expiresAt = now.Add(leaseDuration);
            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = now,
                ExpiresAt = expiresAt,
                Metadata = metadata
            };

            _leases[electionName] = new LeaseEntry
            {
                LeaderInfo = leaderInfo,
                ExpiresAt = expiresAt
            };
            return leaderInfo;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> TryRenewLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(electionName, participantId, leaseDuration);
        ValidateState(cancellationToken);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _dateTimeProvider.UtcNow;

            // Check if lease exists and is owned by the participant
            if (!_leases.TryGetValue(electionName, out var existingLease))
            {
                return null;
            }

            if (existingLease.LeaderInfo.ParticipantId != participantId)
            {
                return null;
            }

            // Check if lease has expired
            if (existingLease.ExpiresAt <= now)
            {
                _ = _leases.TryRemove(electionName, out _);
                return null;
            }

            // Renew the lease
            var expiresAt = now.Add(leaseDuration);
            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = existingLease.LeaderInfo.AcquiredAt,
                ExpiresAt = expiresAt,
                Metadata = metadata ?? existingLease.LeaderInfo.Metadata
            };

            _leases[electionName] = new LeaseEntry
            {
                LeaderInfo = leaderInfo,
                ExpiresAt = expiresAt
            };
            return leaderInfo;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReleaseLeaseAsync(
        string electionName,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(electionName, participantId);
        ValidateState(cancellationToken);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_leases.TryGetValue(electionName, out var existingLease))
            {
                return false;
            }

            if (existingLease.LeaderInfo.ParticipantId != participantId)
            {
                return false;
            }

            _ = _leases.TryRemove(electionName, out _);
            return true;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> GetCurrentLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(electionName);
        ValidateState(cancellationToken);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var lease = GetValidLease(electionName);
            return lease?.LeaderInfo;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasValidLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(electionName);
        ValidateState(cancellationToken);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return GetValidLease(electionName) is not null;
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="InProcessLeaseStore"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~InProcessLeaseStore()
    {
        Dispose(false);
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="InProcessLeaseStore"/>.
    /// This method can be overridden in derived classes to release additional resources.
    /// It is called by the Dispose method and can be called directly if needed.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is being called from the Dispose method (true) or from the finalizer (false).</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }

    private static void ValidateParameters(string electionName)
    {
        if (String.IsNullOrWhiteSpace(electionName))
            throw new ArgumentException(LeaderElectionServiceBase.ElectionNameExceptionMessage, nameof(electionName));
    }

    private static void ValidateParameters(string electionName, string participantId)
    {
        ValidateParameters(electionName);
        if (String.IsNullOrWhiteSpace(participantId))
            throw new ArgumentException("Participant ID cannot be null or whitespace.", nameof(participantId));
    }

    private static void ValidateParameters(string electionName, string participantId, TimeSpan leaseDuration)
    {
        ValidateParameters(electionName, participantId);
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentException("Lease duration must be positive.", nameof(leaseDuration));
    }

    private void ValidateState(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private LeaseEntry? GetValidLease(string electionName)
    {
        var now = _dateTimeProvider.UtcNow;

        if (!_leases.TryGetValue(electionName, out var existingLease))
        {
            return null;
        }

        if (existingLease.ExpiresAt <= now)
        {
            _ = _leases.TryRemove(electionName, out _);
            return null;
        }

        return existingLease;
    }

    private sealed class LeaseEntry
    {
        public required LeaderInfo LeaderInfo { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }
}
