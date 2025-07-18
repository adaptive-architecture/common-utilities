namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Information about the current leader in an election.
/// </summary>
public sealed record LeaderInfo
{
    /// <summary>
    /// Gets the unique identifier of the leader.
    /// </summary>
    public required string ParticipantId { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when leadership was acquired.
    /// </summary>
    public required DateTime AcquiredAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the leadership lease expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Gets optional metadata associated with the leader.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether the leadership lease is still valid.
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Gets the remaining time until the lease expires.
    /// </summary>
    public TimeSpan TimeToExpiry => ExpiresAt - DateTime.UtcNow;
}
