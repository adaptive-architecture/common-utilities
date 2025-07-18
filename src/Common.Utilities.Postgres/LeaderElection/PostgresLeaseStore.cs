using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Serialization.Contracts;
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
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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
        ThrowIfDisposed();

        try
        {
            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);

            var leaderInfo = new LeaderInfo
            {
                ParticipantId = participantId,
                AcquiredAt = acquiredAt,
                ExpiresAt = expiresAt,
                Metadata = metadata
            };

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            // Use INSERT ... ON CONFLICT DO NOTHING for atomic lease acquisition
            const string sql = """
                INSERT INTO {0} (election_name, participant_id, acquired_at, expires_at, metadata)
                VALUES (@electionName, @participantId, @acquiredAt, @expiresAt, @metadata)
                ON CONFLICT (election_name) DO UPDATE SET
                    participant_id = @participantId,
                    acquired_at = @acquiredAt,
                    expires_at = @expiresAt,
                    metadata = @metadata
                WHERE {0}.expires_at < @now
                RETURNING participant_id, acquired_at, expires_at, metadata;
                """;

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            command.Parameters.AddWithValue("electionName", electionName);
            command.Parameters.AddWithValue("participantId", participantId);
            command.Parameters.AddWithValue("acquiredAt", acquiredAt);
            command.Parameters.AddWithValue("expiresAt", expiresAt);
            command.Parameters.AddWithValue("now", DateTime.UtcNow);
            AddMetadataParameter(command, "metadata", metadata, _serializer);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var returnedParticipantId = reader.GetString(0);
                if (returnedParticipantId == participantId)
                {
                    _logger.LogDebug("Acquired lease for election {ElectionName} by participant {ParticipantId}",
                        electionName, participantId);
                    return leaderInfo;
                }
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
            var acquiredAt = DateTime.UtcNow;
            var expiresAt = acquiredAt.Add(leaseDuration);

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            const string sql = """
                UPDATE {0} SET
                    acquired_at = @acquiredAt,
                    expires_at = @expiresAt,
                    metadata = @metadata
                WHERE election_name = @electionName
                  AND participant_id = @participantId
                  AND expires_at > @now
                RETURNING participant_id, acquired_at, expires_at, metadata;
                """;

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            command.Parameters.AddWithValue("electionName", electionName);
            command.Parameters.AddWithValue("participantId", participantId);
            command.Parameters.AddWithValue("acquiredAt", acquiredAt);
            command.Parameters.AddWithValue("expiresAt", expiresAt);
            command.Parameters.AddWithValue("now", DateTime.UtcNow);
            AddMetadataParameter(command, "metadata", metadata, _serializer);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var leaderInfo = new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };

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
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            const string sql = """
                DELETE FROM {0}
                WHERE election_name = @electionName
                  AND participant_id = @participantId;
                """;

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            command.Parameters.AddWithValue("electionName", electionName);
            command.Parameters.AddWithValue("participantId", participantId);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            var wasReleased = rowsAffected > 0;

            _logger.LogDebug("Released lease result is {WasReleased} for election {ElectionName} by participant {ParticipantId}",
                wasReleased, electionName, participantId);

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
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            const string sql = """
                SELECT participant_id, acquired_at, expires_at, metadata
                FROM {0}
                WHERE election_name = @electionName
                  AND expires_at > @now;
                """;

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            command.Parameters.AddWithValue("electionName", electionName);
            command.Parameters.AddWithValue("now", DateTime.UtcNow);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var participantId = reader.GetString(0);
                var acquiredAt = reader.GetDateTime(1);
                var expiresAt = reader.GetDateTime(2);
                var metadataJson = reader.IsDBNull(3) ? null : reader.GetString(3);

                IReadOnlyDictionary<string, string>? metadata = null;
                if (!String.IsNullOrEmpty(metadataJson))
                {
                    metadata = _serializer.Deserialize<Dictionary<string, string>>(metadataJson);
                }

                return new LeaderInfo
                {
                    ParticipantId = participantId,
                    AcquiredAt = acquiredAt,
                    ExpiresAt = expiresAt,
                    Metadata = metadata
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current lease for election {ElectionName}",
                electionName);
            throw;
        }
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

    /// <summary>
    /// Ensures the lease table exists in the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

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

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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
        ThrowIfDisposed();

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

            const string sql = """
                DELETE FROM {0}
                WHERE expires_at < @now;
                """;

            var formattedSql = String.Format(sql, _tableName);

            await using var command = new NpgsqlCommand(formattedSql, connection);
            command.Parameters.AddWithValue("now", DateTime.UtcNow);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PostgresLeaseStore));
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
            command.Parameters.AddWithValue(parameterName, DBNull.Value);
        }
    }
}
