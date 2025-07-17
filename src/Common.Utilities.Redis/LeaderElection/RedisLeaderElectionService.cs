using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.LeaderElection;

/// <summary>
/// Redis-based implementation of <see cref="ILeaderElectionService"/> for distributed leader election.
/// </summary>
public class RedisLeaderElectionService : LeaderElectionServiceBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLeaderElectionService"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    /// <param name="serializer">The data serializer for lease information.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public RedisLeaderElectionService(
        IConnectionMultiplexer connectionMultiplexer,
        IDataSerializer serializer,
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null,
        ILogger? logger = null)
        : base(
            new RedisLeaseStore(connectionMultiplexer, serializer, logger),
            electionName,
            participantId,
            options,
            logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLeaderElectionService"/> class with an existing lease store.
    /// </summary>
    /// <param name="leaseStore">The Redis lease store to use for coordination.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public RedisLeaderElectionService(
        RedisLeaseStore leaseStore,
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null,
        ILogger? logger = null)
        : base(leaseStore, electionName, participantId, options, logger)
    {
    }
}
