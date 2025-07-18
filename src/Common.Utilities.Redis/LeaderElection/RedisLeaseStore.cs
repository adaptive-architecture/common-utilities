using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.LeaderElection;

/// <summary>
/// Redis-based implementation of <see cref="ILeaseStore"/> for distributed leader election.
/// </summary>
public class RedisLeaseStore : ILeaseStore, IDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDataSerializer _serializer;
    private readonly ILogger _logger;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLeaseStore"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer.</param>
    /// <param name="serializer">The data serializer for lease information.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public RedisLeaseStore(
        IConnectionMultiplexer connectionMultiplexer,
        IDataSerializer serializer,
        ILogger? logger = null)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> TryAcquireLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var leaseKey = GetLeaseKey(electionName);
            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);

            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = acquiredAt,
                ExpiresAt = expiresAt,
                Metadata = metadata
            };
            var serializedLease = _serializer.Serialize(leaderInfo);

            // Use SET with NX (only if not exists) and EX (expiration) for atomic operation
            var acquired = await database.StringSetAsync(
                leaseKey,
                serializedLease,
                leaseDuration,
                When.NotExists).ConfigureAwait(false);

            if (acquired)
            {
                _logger.LogDebug("Acquired lease for election {ElectionName} by participant {ParticipantId}",
                    electionName, participantId);
                return leaderInfo;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            throw;
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
        ThrowIfDisposed();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var leaseKey = GetLeaseKey(electionName);

            // Lua script for atomic renewal - only renew if the current holder is this participant
            const string renewScript = """
                local key = KEYS[1]
                local participantId = ARGV[1]
                local newLeaseData = ARGV[2]
                local ttlSeconds = tonumber(ARGV[3])

                local currentLease = redis.call('GET', key)
                if currentLease == false then
                    return nil
                end

                local currentParticipantId = cjson.decode(currentLease).ParticipantId
                if currentParticipantId ~= participantId then
                    return nil
                end

                redis.call('SET', key, newLeaseData, 'EX', ttlSeconds)
                return newLeaseData
                """;

            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);
            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = acquiredAt,
                ExpiresAt = expiresAt,
                Metadata = metadata
            };
            var serializedLease = _serializer.Serialize(leaderInfo);

            var result = await database.ScriptEvaluateAsync(
                renewScript,
                [leaseKey],
                [participantId, serializedLease, (int)leaseDuration.TotalSeconds]
            ).ConfigureAwait(false);

            if (!result.IsNull)
            {
                _logger.LogTrace("Renewed lease for election {ElectionName} by participant {ParticipantId}",
                    electionName, participantId);
                return leaderInfo;
            }

            _logger.LogDebug("Failed to renew lease for election {ElectionName} by participant {ParticipantId} - not current holder",
                electionName, participantId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ReleaseLeaseAsync(
        string electionName,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var leaseKey = GetLeaseKey(electionName);

            // Lua script for atomic release - only release if the current holder is this participant
            const string releaseScript = """
                local key = KEYS[1]
                local participantId = ARGV[1]

                local currentLease = redis.call('GET', key)
                if currentLease == false then
                    return 0
                end

                local currentParticipantId = cjson.decode(currentLease).ParticipantId
                if currentParticipantId == participantId then
                    return redis.call('DEL', key)
                end

                return 0
                """;

            var deleted = await database.ScriptEvaluateAsync(
                releaseScript,
                [leaseKey],
                [participantId]
            ).ConfigureAwait(false);

            var wasReleased = (int)deleted > 0;

            if (wasReleased)
            {
                _logger.LogDebug("Released lease for election {ElectionName} by participant {ParticipantId}",
                    electionName, participantId);
            }
            else
            {
                _logger.LogDebug("No lease to release for election {ElectionName} by participant {ParticipantId}",
                    electionName, participantId);
            }

            return wasReleased;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> GetCurrentLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var database = _connectionMultiplexer.GetDatabase();
            var leaseKey = GetLeaseKey(electionName);

            var leaseData = await database.StringGetAsync(leaseKey).ConfigureAwait(false);

            if (!leaseData.HasValue)
            {
                return null;
            }
            var leaderInfo = _serializer.Deserialize<LeaderInfo>(leaseData!);
            // Check if the lease has expired (Redis might not have cleaned it up yet)
            if (leaderInfo!.IsValid)
            {
                return leaderInfo;
            }
            else
            {
                // Clean up expired lease
                _ = await database.KeyDeleteAsync(leaseKey).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current lease for election {ElectionName}",
                electionName);
            throw;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<bool> HasValidLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var currentLease = await GetCurrentLeaseAsync(electionName, cancellationToken).ConfigureAwait(false);
            return currentLease?.IsValid == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check valid lease for election {ElectionName}",
                electionName);
            throw;
        }
    }

    private static string GetLeaseKey(string electionName)
    {
        return $"leader_election:lease:{electionName}";
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(RedisLeaseStore));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="RedisLeaseStore"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Note: We don't dispose the connection multiplexer as it's typically shared
            // and owned by the DI container or application lifetime
            _disposed = true;
        }
    }
}
