using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.LeaderElection;

/// <summary>
/// Redis-based implementation of <see cref="ILeaderElectionServiceProvider"/> for creating distributed leader election services.
/// </summary>
public class RedisLeaderElectionServiceProvider : ILeaderElectionServiceProvider
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDataSerializer _serializer;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLeaderElectionServiceProvider"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    /// <param name="serializer">The data serializer for lease information.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public RedisLeaderElectionServiceProvider(
        IConnectionMultiplexer connectionMultiplexer,
        IDataSerializer serializer,
        ILogger? logger = null)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger;
    }

    /// <inheritdoc/>
    public ILeaderElectionService CreateElection(
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null)
    {
        return new RedisLeaderElectionService(
            _connectionMultiplexer,
            _serializer,
            electionName,
            participantId,
            options,
            _logger);
    }
}
