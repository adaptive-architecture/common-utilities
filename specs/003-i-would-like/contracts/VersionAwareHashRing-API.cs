// Version-Aware HashRing API Contract
// This file defines the public API extensions for version-aware consistent hashing

namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Extended HashRing options to support version-aware operations
/// </summary>
public class HashRingOptions
{
    // Existing properties remain unchanged...

    /// <summary>
    /// Gets or sets whether version history is enabled for data migration scenarios.
    /// Default: false
    /// </summary>
    public bool EnableVersionHistory { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of historical configurations to retain.
    /// Only used when EnableVersionHistory is true.
    /// Default: 3, Minimum: 1
    /// </summary>
    public int MaxHistorySize { get; set; } = 3;
}

/// <summary>
/// Represents the result of querying servers from current and historical configurations
/// </summary>
/// <typeparam name="T">The type of server identifiers</typeparam>
public sealed class ServerCandidateResult<T> where T : IEquatable<T>
{
    /// <summary>
    /// Gets the list of unique servers in priority order (current first, then historical)
    /// </summary>
    public IReadOnlyList<T> Servers { get; }

    /// <summary>
    /// Gets the number of configurations that were consulted for this result
    /// </summary>
    public int ConfigurationCount { get; }

    /// <summary>
    /// Gets whether historical configurations were available and consulted
    /// </summary>
    public bool HasHistory { get; }

    internal ServerCandidateResult(IReadOnlyList<T> servers, int configurationCount, bool hasHistory)
    {
        ArgumentNullException.ThrowIfNull(servers);
        Servers = servers;
        ConfigurationCount = configurationCount;
        HasHistory = hasHistory;
    }
}

/// <summary>
/// Exception thrown when attempting to create a configuration snapshot that would exceed the history limit
/// </summary>
public sealed class HashRingHistoryLimitExceededException : InvalidOperationException
{
    /// <summary>
    /// Gets the maximum allowed history size
    /// </summary>
    public int MaxHistorySize { get; }

    /// <summary>
    /// Gets the current history count
    /// </summary>
    public int CurrentCount { get; }

    internal HashRingHistoryLimitExceededException(int maxHistorySize, int currentCount)
        : base($"Cannot create configuration snapshot. History limit of {maxHistorySize} would be exceeded. Current count: {currentCount}")
    {
        MaxHistorySize = maxHistorySize;
        CurrentCount = currentCount;
    }
}

/// <summary>
/// Extensions to HashRing<T> for version-aware operations
/// </summary>
public static partial class HashRing<T> where T : IEquatable<T>
{
    // Existing methods remain unchanged...

    /// <summary>
    /// Gets whether version history is enabled for this hash ring
    /// </summary>
    public bool IsVersionHistoryEnabled { get; }

    /// <summary>
    /// Gets the current number of historical configurations stored
    /// </summary>
    public int HistoryCount { get; }

    /// <summary>
    /// Gets the maximum number of historical configurations that can be stored
    /// </summary>
    public int MaxHistorySize { get; }

    /// <summary>
    /// Creates a snapshot of the current configuration and stores it in history
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when version history is not enabled</exception>
    /// <exception cref="HashRingHistoryLimitExceededException">Thrown when creating the snapshot would exceed the history limit</exception>
    public void CreateConfigurationSnapshot();

    /// <summary>
    /// Clears all historical configurations, retaining only the current configuration
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when version history is not enabled</exception>
    public void ClearHistory();

    /// <summary>
    /// Gets server candidates that should handle the specified key, including servers from historical configurations
    /// </summary>
    /// <param name="key">The key to find servers for</param>
    /// <returns>Server candidates with current server first, followed by unique servers from historical configurations</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no servers are available in any configuration</exception>
    public ServerCandidateResult<T> GetServerCandidates(byte[] key);

    /// <summary>
    /// Tries to get server candidates that should handle the specified key from current and historical configurations
    /// </summary>
    /// <param name="key">The key to find servers for</param>
    /// <param name="result">When this method returns, contains the server candidates if found; otherwise, null</param>
    /// <returns>true if server candidates were found; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    public bool TryGetServerCandidates(byte[] key, out ServerCandidateResult<T>? result);

    /// <summary>
    /// Gets multiple server candidates that should handle the specified key from current and historical configurations
    /// </summary>
    /// <param name="key">The key to find servers for</param>
    /// <param name="maxCandidates">The maximum number of unique server candidates to return</param>
    /// <returns>Server candidates in priority order, with deduplication across configurations</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxCandidates is less than 0</exception>
    public ServerCandidateResult<T> GetServerCandidates(byte[] key, int maxCandidates);
}