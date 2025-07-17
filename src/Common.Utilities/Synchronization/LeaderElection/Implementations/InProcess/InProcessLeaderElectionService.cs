using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

/// <summary>
/// In-process implementation of <see cref="ILeaderElectionService"/> using <see cref="InProcessLeaseStore"/>.
/// This implementation is suitable for leader election within the same application process.
/// </summary>
public class InProcessLeaderElectionService : LeaderElectionServiceBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessLeaderElectionService"/> class.
    /// </summary>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    /// <param name="dateTimeProvider">The date time provider. If null, a default provider will be used.</param>
    public InProcessLeaderElectionService(
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null,
        ILogger? logger = null,
        IDateTimeProvider? dateTimeProvider = null)
        : base(
            new InProcessLeaseStore(dateTimeProvider),
            electionName,
            participantId,
            options,
            logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessLeaderElectionService"/> class with a custom lease store.
    /// </summary>
    /// <param name="leaseStore">The lease store to use for coordination.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public InProcessLeaderElectionService(
        ILeaseStore leaseStore,
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null,
        ILogger? logger = null)
        : base(leaseStore, electionName, participantId, options, logger)
    {
    }
}
