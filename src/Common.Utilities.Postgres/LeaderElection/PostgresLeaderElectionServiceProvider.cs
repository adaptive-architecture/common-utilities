using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Serialization.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using Npgsql;

namespace AdaptArch.Common.Utilities.Postgres.LeaderElection;

/// <summary>
/// PostgreSQL-based implementation of <see cref="ILeaderElectionServiceProvider"/> for creating distributed leader election services.
/// </summary>
public class PostgresLeaderElectionServiceProvider : ILeaderElectionServiceProvider
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IStringDataSerializer _serializer;
    private readonly string _tableName;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresLeaderElectionServiceProvider"/> class.
    /// </summary>
    /// <param name="dataSource">The PostgreSQL data source.</param>
    /// <param name="serializer">The data serializer for lease metadata.</param>
    /// <param name="tableName">The table name for storing leases. Defaults to "leader_election_leases".</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public PostgresLeaderElectionServiceProvider(
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
        _logger = logger;
    }

    /// <inheritdoc/>
    public ILeaderElectionService CreateElection(
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null)
    {
        return new PostgresLeaderElectionService(
            _dataSource,
            _serializer,
            electionName,
            participantId,
            _tableName,
            options,
            _logger);
    }
}
