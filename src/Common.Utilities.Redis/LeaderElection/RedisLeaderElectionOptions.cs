using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.LeaderElection;

/// <summary>
/// Configuration options for Redis-based leader election services.
/// </summary>
public class RedisLeaderElectionOptions
{
    /// <summary>
    /// Gets or sets the Redis connection multiplexer.
    /// </summary>
    public IConnectionMultiplexer? ConnectionMultiplexer { get; set; }

    /// <summary>
    /// Gets or sets the data serializer for lease information.
    /// If not specified, a default JSON serializer will be used.
    /// </summary>
    public IDataSerializer? Serializer { get; set; }

    /// <summary>
    /// Gets or sets the Redis database index to use for leader election.
    /// Defaults to -1 (use the default database).
    /// </summary>
    public int Database { get; set; } = -1;

    /// <summary>
    /// Gets or sets the key prefix for Redis leader election keys.
    /// Defaults to "leader_election".
    /// </summary>
    public string KeyPrefix { get; set; } = "leader_election";

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>The validated options instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing.</exception>
    public RedisLeaderElectionOptions Validate()
    {
        if (ConnectionMultiplexer == null)
        {
            throw new InvalidOperationException("ConnectionMultiplexer is required for Redis leader election.");
        }

        if (Serializer == null)
        {
            throw new InvalidOperationException("Serializer is required for Redis leader election.");
        }

        if (String.IsNullOrWhiteSpace(KeyPrefix))
        {
            throw new InvalidOperationException("KeyPrefix cannot be null or whitespace.");
        }

        return this;
    }
}
