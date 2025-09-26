namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Represents the result of querying servers from current and historical configurations.
/// Contains unique servers in priority order with metadata about the query.
/// </summary>
/// <typeparam name="T">The type of server identifiers.</typeparam>
public sealed class ServerCandidateResult<T> where T : IEquatable<T>
{
    /// <summary>
    /// Gets the list of unique servers in priority order (current first, then historical).
    /// </summary>
    public IReadOnlyList<T> Servers { get; }

    /// <summary>
    /// Gets the number of configurations that were consulted for this result.
    /// This includes the current configuration plus any historical configurations.
    /// </summary>
    public int ConfigurationCount { get; }

    /// <summary>
    /// Gets whether historical configurations were available and consulted.
    /// </summary>
    public bool HasHistory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerCandidateResult{T}"/> class.
    /// </summary>
    /// <param name="servers">The list of unique servers in priority order.</param>
    /// <param name="configurationCount">The number of configurations consulted.</param>
    /// <param name="hasHistory">Whether historical configurations were consulted.</param>
    /// <exception cref="ArgumentNullException">Thrown when servers is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when configurationCount is negative.</exception>
    public ServerCandidateResult(IReadOnlyList<T> servers, int configurationCount, bool hasHistory)
    {
        Servers = servers ?? throw new ArgumentNullException(nameof(servers));

        if (configurationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(configurationCount), configurationCount,
                "Configuration count must be non-negative.");
        }

        ConfigurationCount = configurationCount;
        HasHistory = hasHistory;
    }

    /// <summary>
    /// Gets whether any servers were found.
    /// </summary>
    public bool HasServers => Servers.Count > 0;

    /// <summary>
    /// Gets the primary server (first in the priority list).
    /// </summary>
    /// <returns>The primary server, or default(T) if no servers are available.</returns>
    public T? GetPrimaryServer()
    {
        return Servers.Count > 0 ? Servers[0] : default;
    }

    /// <summary>
    /// Gets up to the specified number of servers from the candidate list.
    /// </summary>
    /// <param name="maxServers">The maximum number of servers to return.</param>
    /// <returns>A list containing up to maxServers servers from the candidate list.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxServers is negative.</exception>
    public IReadOnlyList<T> GetTopServers(int maxServers)
    {
        if (maxServers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxServers), maxServers,
                "Maximum servers must be non-negative.");
        }

        if (maxServers == 0)
        {
            return [];
        }

        if (maxServers >= Servers.Count)
        {
            return Servers;
        }

        var result = new T[maxServers];
        for (int i = 0; i < maxServers; i++)
        {
            result[i] = Servers[i];
        }

        return result;
    }

    /// <summary>
    /// Returns a string representation of the server candidate result.
    /// </summary>
    /// <returns>A string describing the result.</returns>
    public override string ToString()
    {
        return $"ServerCandidateResult: {Servers.Count} servers from {ConfigurationCount} configurations (HasHistory: {HasHistory})";
    }
}
