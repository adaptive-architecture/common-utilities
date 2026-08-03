using AdaptArch.Common.Utilities.Serialization.Contracts;
using Npgsql;

namespace AdaptArch.Common.Utilities.Postgres.LeaderElection;

/// <summary>
/// Configuration options for PostgreSQL-based leader election services.
/// </summary>
public class PostgresLeaderElectionOptions
{
    /// <summary>
    /// Gets or sets the PostgreSQL data source.
    /// </summary>
    public NpgsqlDataSource? DataSource { get; set; }

    /// <summary>
    /// Gets or sets the data serializer for lease metadata.
    /// If not specified, a default JSON serializer will be used.
    /// </summary>
    public IStringDataSerializer? Serializer { get; set; }

    /// <summary>
    /// Gets or sets the table name for storing leases.
    /// Defaults to "leader_election_leases".
    /// </summary>
    public string TableName { get; set; } = "leader_election_leases";

    /// <summary>
    /// Gets or sets a value indicating whether to automatically create the lease table if it doesn't exist.
    /// Defaults to true.
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for automatic cleanup of expired leases.
    /// Set to null to disable automatic cleanup.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan? CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the connection timeout for database operations.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the command timeout for database operations.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>The validated options instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required options are missing or invalid.</exception>
    public PostgresLeaderElectionOptions Validate()
    {
        if (DataSource == null)
        {
            throw new InvalidOperationException("DataSource is required for PostgreSQL leader election.");
        }

        if (Serializer == null)
        {
            throw new InvalidOperationException("Serializer is required for PostgreSQL leader election.");
        }

        if (String.IsNullOrEmpty(TableName))
        {
            throw new InvalidOperationException("TableName cannot be null or empty.");
        }

        // Validate table name to prevent SQL injection
        if (!IsValidTableName(TableName))
        {
            throw new InvalidOperationException("TableName contains invalid characters. It must be a valid PostgreSQL identifier: start with a letter or underscore, contain only letters, digits or underscores, and be at most 63 characters long.");
        }

        if (ConnectionTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("ConnectionTimeout must be greater than zero.");
        }

        if (CommandTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("CommandTimeout must be greater than zero.");
        }

        if (CleanupInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("CleanupInterval must be greater than zero when specified.");
        }

        return this;
    }

    private static bool IsValidTableName(string tableName) => PostgresIdentifier.IsValid(tableName);
}
