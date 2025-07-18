namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Factory interface for creating leader election instances.
/// </summary>
public interface ILeaderElectionServiceProvider
{
    /// <summary>
    /// Creates a new leader election service instance.
    /// </summary>
    /// <param name="electionName">The name of the election (used to group participants).</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <returns>A new leader election service instance.</returns>
    ILeaderElectionService CreateElection(string electionName, string participantId, LeaderElectionOptions? options = null);
}
