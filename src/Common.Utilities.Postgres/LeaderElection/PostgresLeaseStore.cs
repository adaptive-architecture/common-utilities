using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using Npgsql;

namespace AdaptArch.Common.Utilities.Postgres.LeaderElection;

/// <summary>
/// PostgreSQL-based implementation of <see cref="ILeaseStore"/> for distributed leader election.
/// </summary>
public class PostgresLeaseStore : ILeaseStore, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IStringDataSerializer _serializer;
    private readonly string _tableName;
    private readonly ILogger _logger;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresLeaseStore"/> class.
    /// </summary>
    /// <param name="dataSource">The PostgreSQL data source.</param>
    /// <param name="serializer">The data serializer for lease metadata.</param>
    /// <param name="tableName">The table name for storing leases. Defaults to "leader_election_leases".</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public PostgresLeaseStore(
        NpgsqlDataSource dataSource,
        IStringDataSerializer serializer,
        string? tableName = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(serializer);
        _dataSource = dataSource;
        _serializer = serializer;
        _tableName = tableName ?? "leader_election_leases";
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
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        LeaderInfo? leaderInfo;

        try
        {
            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);

            // Use INSERT ... ON CONFLICT DO NOTHING for atomic lease acquisition
            /*
            Possible Outcomes:
              * Lease acquired from new lease created: If no lease exists for this election_name, a new record is inserted and its details are returned.
              * Lease acquired from expired holder: If a lease exists but has expired, it gets updated with the new participant's information and returns the new lease details.
              * Lease NOT acquired: If a lease exists and hasn't expired yet, the WHERE condition fails, no update occurs, and the query returns nothing (no rows).
            */
            const string sql = """
                INSERT INTO {0} (election_name, participant_id, acquired_at, expires_at, metadata)
                VALUES (@election_name, @participant_id, @acquired_at, @expires_at, @metadata_json)
                ON CONFLICT (election_name) DO UPDATE SET
                    participant_id = @participant_id,
                    acquired_at = @acquired_at,
                    expires_at = @expires_at,
                    metadata = @metadata_json
                WHERE {0}.expires_at < @now
                RETURNING participant_id, acquired_at, expires_at, metadata;
                """;

#pragma warning disable S1192 // Define a constant instead of using this literal
            var parameters = new Dictionary<string, object>
            {
                ["election_name"] = electionName,
                ["participant_id"] = participantId,
                ["acquired_at"] = acquiredAt,
                ["expires_at"] = expiresAt,
                ["now"] = DateTime.UtcNow
            };
#pragma warning restore S1192 // Define a constant instead of using this literal
            await using var reader = await ExecuteReaderAsync(sql, parameters, metadata, cancellationToken).ConfigureAwait(false);

            // If the lease was acquired, the reader will have a row
            // If the lease was not acquired, the reader will be empty
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogDebug("Acquired lease for election {ElectionName} by participant {ParticipantId}",
                    electionName, participantId);

                leaderInfo = new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }
            else
            {
                _logger.LogDebug("Failed to acquire lease for election {ElectionName} by participant {ParticipantId} - already held by another participant",
                    electionName, participantId);
                leaderInfo = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            throw;
        }
        return leaderInfo;
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> TryRenewLeaseAsync(
        string electionName,
        string participantId,
        TimeSpan leaseDuration,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        LeaderInfo? leaderInfo;

        try
        {
            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);

            const string sql = """
                UPDATE {0} SET
                    acquired_at = @acquired_at,
                    expires_at = @expires_at,
                    metadata = @metadata_json
                WHERE election_name = @election_name
                    AND participant_id = @participant_id
                    AND expires_at > @now
                RETURNING participant_id, acquired_at, expires_at, metadata;
                """;

#pragma warning disable S1192 // Define a constant instead of using this literal
            var parameters = new Dictionary<string, object>
            {
                ["election_name"] = electionName,
                ["participant_id"] = participantId,
                ["acquired_at"] = acquiredAt,
                ["expires_at"] = expiresAt,
                ["now"] = DateTime.UtcNow
            };
#pragma warning restore S1192 // Define a constant instead of using this literal

            await using var reader = await ExecuteReaderAsync(sql, parameters, metadata, cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogTrace("Renewed lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);

                leaderInfo = new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }
            else
            {
                _logger.LogDebug("Failed to renew lease for election {ElectionName} by participant {ParticipantId} - not current holder",
                    electionName, participantId);
                leaderInfo = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            throw;
        }

        return leaderInfo;
    }

    /// <inheritdoc/>
    public async Task<bool> ReleaseLeaseAsync(
        string electionName,
        string participantId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));
        bool wasReleased;

        try
        {
            const string sql = """
                DELETE FROM {0}
                WHERE election_name = @election_name
                  AND participant_id = @participant_id;
                """;

#pragma warning disable S1192 // Define a constant instead of using this literal
            var parameters = new Dictionary<string, object>
            {
                ["election_name"] = electionName,
                ["participant_id"] = participantId
            };
#pragma warning restore S1192 // Define a constant instead of using this literal

            var rowsAffected = await ExecuteNonQueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            wasReleased = rowsAffected > 0;

            _logger.LogDebug("Released lease result is {WasReleased} for election {ElectionName} by participant {ParticipantId}",
                wasReleased, electionName, participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lease for election {ElectionName} by participant {ParticipantId}",
                electionName, participantId);
            wasReleased = false;
        }

        return wasReleased;
    }

    /// <inheritdoc/>
    public async Task<LeaderInfo?> GetCurrentLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        LeaderInfo? leaderInfo;

        try
        {
            const string sql = """
                SELECT participant_id, acquired_at, expires_at, metadata
                FROM {0}
                WHERE election_name = @election_name
                    AND expires_at > @now;
                """;

#pragma warning disable S1192 // Define a constant instead of using this literal
            var parameters = new Dictionary<string, object>
            {
                ["election_name"] = electionName,
                ["now"] = DateTime.UtcNow
            };
#pragma warning restore S1192 // Define a constant instead of using this literal

            await using var reader = await ExecuteReaderAsync(sql, parameters, null, cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var participantId = reader.GetString(0);
                var acquiredAt = reader.GetDateTime(1);
                var expiresAt = reader.GetDateTime(2);
                var metadataJson = await reader.IsDBNullAsync(3, cancellationToken)
                    .ConfigureAwait(false)
                    ? null
                    : reader.GetString(3);

                IReadOnlyDictionary<string, string>? metadata = null;
                if (!String.IsNullOrEmpty(metadataJson))
                {
                    metadata = _serializer.Deserialize<Dictionary<string, string>>(metadataJson);
                }

                leaderInfo = new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }
            else
            {
                _logger.LogDebug("No current lease found for election {ElectionName}", electionName);
                leaderInfo = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current lease for election {ElectionName}", electionName);
            throw;
        }

        return leaderInfo;
    }

    /// <inheritdoc/>
    public async Task<bool> HasValidLeaseAsync(
        string electionName,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        var hasValidLease = false;

        try
        {
            var currentLease = await GetCurrentLeaseAsync(electionName, cancellationToken).ConfigureAwait(false);
            hasValidLease = currentLease?.IsValid == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check valid lease for election {ElectionName}", electionName);
            throw;
        }
        return hasValidLease;
    }

    /// <summary>
    /// Ensures the lease table exists in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        try
        {
            const string sql = """
                CREATE TABLE IF NOT EXISTS {0} (
                    election_name VARCHAR(255) PRIMARY KEY,
                    participant_id VARCHAR(255) NOT NULL,
                    acquired_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
                    metadata JSONB
                );

                CREATE INDEX IF NOT EXISTS idx_{0}_expires_at ON {0}(expires_at);
                """;

            _ = await ExecuteNonQueryAsync(sql, [], cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Ensured lease table {TableName} exists", _tableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure lease table {TableName} exists", _tableName);
            throw;
        }
    }

    /// <summary>
    /// Cleans up expired leases from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The number of expired leases that were cleaned up.</returns>
    public async Task<int> CleanupExpiredLeasesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));

        try
        {
            const string sql = """
                DELETE FROM {0}
                WHERE expires_at < @now;
                """;

            var parameters = new Dictionary<string, object>
            {
                ["now"] = DateTime.UtcNow
            };

            var rowsAffected = await ExecuteNonQueryAsync(sql, parameters, cancellationToken).ConfigureAwait(false);

            if (rowsAffected > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired leases from table {TableName}", rowsAffected, _tableName);
            }

            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired leases from table {TableName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="PostgresLeaseStore"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Note: We don't dispose the data source as it's typically shared
            // and owned by the DI container or application lifetime
            _disposed = true;
        }
    }

    private async Task<NpgsqlDataReader> ExecuteReaderAsync(
        string sql,
        Dictionary<string, object> parameters,
        IReadOnlyDictionary<string, string>? metadata,
        CancellationToken cancellationToken)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var formattedSql = String.Format(sql, _tableName);
        var command = new NpgsqlCommand(formattedSql, connection);

        AddParameters(command, parameters);
        AddMetadataParameter(command, "metadata_json", metadata, _serializer);

        return await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var formattedSql = String.Format(sql, _tableName);
        await using var command = new NpgsqlCommand(formattedSql, connection);

        AddParameters(command, parameters);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddParameters(NpgsqlCommand command, Dictionary<string, object> parameters)
    {
        foreach (var (key, value) in parameters)
        {
            _ = command.Parameters.AddWithValue(key, value);
        }
    }

    private static void AddMetadataParameter(
        NpgsqlCommand command,
        string parameterName,
        IReadOnlyDictionary<string, string>? metadata,
        IStringDataSerializer serializer)
    {
        if (metadata != null)
        {
            var serializedMetadata = serializer.Serialize(metadata)!;
            var param = command.Parameters.AddWithValue(parameterName, serializedMetadata);
            param.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
        }
        else
        {
            _ = command.Parameters.AddWithValue(parameterName, DBNull.Value);
        }
    }
}
