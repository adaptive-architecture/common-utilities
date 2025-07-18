using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations.InProcess;

/// <summary>
/// Factory for creating <see cref="InProcessLeaderElectionService"/> instances.
/// </summary>
public class InProcessLeaderElectionServiceProvider : ILeaderElectionServiceProvider
{
    private readonly IDateTimeProvider? _dateTimeProvider;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessLeaderElectionServiceProvider"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The date time provider. If null, a default provider will be used.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public InProcessLeaderElectionServiceProvider(
        IDateTimeProvider? dateTimeProvider = null,
        ILogger? logger = null)
    {
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ILeaderElectionService CreateElection(
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null)
    {
        return new InProcessLeaderElectionService(
            electionName,
            participantId,
            options,
            _logger,
            _dateTimeProvider);
    }
}
