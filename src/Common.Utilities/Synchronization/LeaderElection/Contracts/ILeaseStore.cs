namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Abstraction for storing and managing leadership leases.
/// </summary>
public interface ILeaseStore
{
    /// <summary>
    /// Attempts to acquire a lease for the specified election.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The participant attempting to acquire the lease.</param>
    /// <param name="leaseDuration">The duration of the lease.</param>
    /// <param name="metadata">Optional metadata to store with the lease.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The acquired lease information if successful, null otherwise.</returns>
    Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to renew an existing lease.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The participant attempting to renew the lease.</param>
    /// <param name="leaseDuration">The new duration of the lease.</param>
    /// <param name="metadata">Optional metadata to update with the lease.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The renewed lease information if successful, null otherwise.</returns>
    Task<LeaderInfo?> TryRenewLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a lease held by the specified participant.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The participant releasing the lease.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the lease was released, false if it wasn't held by the participant.</returns>
    Task<bool> ReleaseLeaseAsync(
        string electionName,
        string participantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current lease information for the specified election.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current lease information if it exists and is valid, null otherwise.</returns>
    Task<LeaderInfo?> GetCurrentLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a lease exists and is still valid for the specified election.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if a valid lease exists, false otherwise.</returns>
    Task<bool> HasValidLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default);
}
