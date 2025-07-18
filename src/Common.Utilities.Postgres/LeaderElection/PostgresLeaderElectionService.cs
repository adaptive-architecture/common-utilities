using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;
using AdaptArch.Common.Utilities.Synchronization.LeaderElection.Implementations;
using Npgsql;
using AdaptArch.Common.Utilities.Serialization.Contracts;

namespace AdaptArch.Common.Utilities.Postgres.LeaderElection;

/// <summary>
/// PostgreSQL-based implementation of <see cref="ILeaderElectionService"/> for distributed leader election.
/// </summary>
public class PostgresLeaderElectionService : LeaderElectionServiceBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresLeaderElectionService"/> class.
    /// </summary>
    /// <param name="dataSource">The PostgreSQL data source.</param>
    /// <param name="serializer">The data serializer for lease metadata.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="tableName">The table name for storing leases. Defaults to "leader_election_leases".</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public PostgresLeaderElectionService(
        NpgsqlDataSource dataSource,
        IStringDataSerializer serializer,
        string electionName,
        string participantId,
        string? tableName = null,
        LeaderElectionOptions? options = null,
        ILogger? logger = null)
        : base(
            new PostgresLeaseStore(dataSource, serializer, tableName, logger),
            electionName,
            participantId,
            options,
            logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresLeaderElectionService"/> class with an existing lease store.
    /// </summary>
    /// <param name="leaseStore">The PostgreSQL lease store to use for coordination.</param>
    /// <param name="electionName">The name of the election.</param>
    /// <param name="participantId">The unique identifier for this participant.</param>
    /// <param name="options">Configuration options for the election.</param>
    /// <param name="logger">Logger instance. If null, a no-operation logger will be used.</param>
    public PostgresLeaderElectionService(
        PostgresLeaseStore leaseStore,
        string electionName,
        string participantId,
        LeaderElectionOptions? options = null,
        ILogger? logger = null)
        : base(leaseStore, electionName, participantId, options, logger)
    {
    }
}
