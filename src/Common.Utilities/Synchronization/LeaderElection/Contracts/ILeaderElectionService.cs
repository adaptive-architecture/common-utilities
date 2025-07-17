namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Represents a leader election mechanism for distributed systems.
/// </summary>
public interface ILeaderElectionService : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this election participant.
    /// </summary>
    string ParticipantId { get; }

    /// <summary>
    /// Gets the name of the election (used to group participants).
    /// </summary>
    string ElectionName { get; }

    /// <summary>
    /// Gets a value indicating whether this participant is currently the leader.
    /// </summary>
    bool IsLeader { get; }

    /// <summary>
    /// Gets the current leader information, if available.
    /// </summary>
    LeaderInfo? CurrentLeader { get; }

    /// <summary>
    /// Event raised when leadership status changes.
    /// </summary>
    event EventHandler<LeadershipChangedEventArgs> LeadershipChanged;

    /// <summary>
    /// Starts participating in the leader election process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the election process.</param>
    /// <returns>A task representing the election process.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops participating in the leader election and releases any held leadership.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the stop operation.</param>
    /// <returns>A task representing the stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire leadership immediately.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if leadership was acquired, false otherwise.</returns>
    Task<bool> TryAcquireLeadershipAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases leadership if currently held.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the release operation.</returns>
    Task ReleaseLeadershipAsync(CancellationToken cancellationToken = default);
}
