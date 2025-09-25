using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// A consistent hash ring implementation for distributing keys across servers.
/// </summary>
/// <typeparam name="T">The type of server identifiers.</typeparam>
public sealed class HashRing<T> where T : IEquatable<T>
{
    private readonly IHashAlgorithm _hashAlgorithm;
    private readonly int _defaultVirtualNodes;
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<T, int> _serverVirtualNodes = new();
    private volatile List<VirtualNode<T>> _sortedVirtualNodes = [];

    // Version-aware fields
    private readonly HistoryManager<T>? _historyManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with SHA-1 hash algorithm.
    /// </summary>
    public HashRing() : this(new Sha1HashAlgorithm(), 42)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with a custom hash algorithm.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    public HashRing(IHashAlgorithm hashAlgorithm) : this(hashAlgorithm, 42)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with a custom hash algorithm and default virtual nodes.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <param name="defaultVirtualNodes">The default number of virtual nodes per server.</param>
    public HashRing(IHashAlgorithm hashAlgorithm, int defaultVirtualNodes)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithm);
        _hashAlgorithm = hashAlgorithm;
        _defaultVirtualNodes = defaultVirtualNodes;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the hash ring.</param>
    public HashRing(HashRingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _hashAlgorithm = options.HashAlgorithm;
        _defaultVirtualNodes = options.DefaultVirtualNodes;

        // Initialize version-aware features
        IsVersionHistoryEnabled = options.EnableVersionHistory;
        _historyManager = IsVersionHistoryEnabled ? new HistoryManager<T>(options.MaxHistorySize) : null;
    }

    /// <summary>
    /// Gets the collection of servers in the hash ring.
    /// </summary>
    public IReadOnlyCollection<T> Servers => _serverVirtualNodes.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether the hash ring is empty.
    /// </summary>
    public bool IsEmpty => _serverVirtualNodes.IsEmpty;

    /// <summary>
    /// Gets the total number of virtual nodes in the hash ring.
    /// </summary>
    public int VirtualNodeCount => _sortedVirtualNodes.Count;

    /// <summary>
    /// Gets whether version history is enabled for this hash ring.
    /// </summary>
    public bool IsVersionHistoryEnabled { get; }

    /// <summary>
    /// Gets the current number of historical configurations stored.
    /// Returns 0 if version history is not enabled.
    /// </summary>
    public int HistoryCount => _historyManager?.Count ?? 0;

    /// <summary>
    /// Gets the maximum number of historical configurations that can be stored.
    /// Returns 0 if version history is not enabled.
    /// </summary>
    public int MaxHistorySize => _historyManager?.MaxSize ?? 0;

    /// <summary>
    /// Adds a server to the hash ring with the default number of virtual nodes.
    /// </summary>
    /// <param name="server">The server to add.</param>
    public void Add(T server)
    {
        Add(server, _defaultVirtualNodes);
    }

    /// <summary>
    /// Adds a server to the hash ring with the specified number of virtual nodes.
    /// </summary>
    /// <param name="server">The server to add.</param>
    /// <param name="virtualNodes">The number of virtual nodes for the server.</param>
    public void Add(T server, int virtualNodes)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentOutOfRangeException.ThrowIfLessThan(virtualNodes, 1);

        lock (_lock)
        {
            _serverVirtualNodes.AddOrUpdate(server, virtualNodes, (_, _) => virtualNodes);
            RebuildVirtualNodes();
        }
    }

    /// <summary>
    /// Removes a server from the hash ring.
    /// </summary>
    /// <param name="server">The server to remove.</param>
    /// <returns>true if the server was removed; otherwise, false.</returns>
    public bool Remove(T server)
    {
        ArgumentNullException.ThrowIfNull(server);

        lock (_lock)
        {
            bool removed = _serverVirtualNodes.TryRemove(server, out _);
            if (removed)
            {
                RebuildVirtualNodes();
            }
            return removed;
        }
    }

    /// <summary>
    /// Determines whether the hash ring contains the specified server.
    /// </summary>
    /// <param name="server">The server to check.</param>
    /// <returns>true if the server exists in the ring; otherwise, false.</returns>
    public bool Contains(T server)
    {
        ArgumentNullException.ThrowIfNull(server);
        return _serverVirtualNodes.ContainsKey(server);
    }

    /// <summary>
    /// Removes all servers from the hash ring.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _serverVirtualNodes.Clear();
            _sortedVirtualNodes = [];
        }
    }

    /// <summary>
    /// Gets the server that should handle the specified key.
    /// </summary>
    /// <param name="key">The key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no servers are available.</exception>
    public T GetServer(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var virtualNodes = _sortedVirtualNodes;
        if (virtualNodes.Count == 0)
        {
            throw new InvalidOperationException("No servers available in the hash ring.");
        }

        uint hash = ComputeKeyHash(key);
        int index = FindServerIndex(virtualNodes, hash);
        return virtualNodes[index].Server;
    }

    /// <summary>
    /// Tries to get the server that should handle the specified key.
    /// </summary>
    /// <param name="key">The key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found; otherwise, false.</returns>
    public bool TryGetServer(byte[] key, out T? server)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            server = GetServer(key);
            return true;
        }
        catch (InvalidOperationException)
        {
            server = default;
            return false;
        }
    }

    /// <summary>
    /// Gets multiple servers that should handle the specified key, in preference order.
    /// </summary>
    /// <param name="key">The key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order.</returns>
    public IEnumerable<T> GetServers(byte[] key, int count)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        return GetServersCore(key, count);
    }

    private IEnumerable<T> GetServersCore(byte[] key, int count)
    {
        var virtualNodes = _sortedVirtualNodes;
        if (virtualNodes.Count == 0)
        {
            yield break;
        }

        uint hash = ComputeKeyHash(key);
        int startIndex = FindServerIndex(virtualNodes, hash);
        var seenServers = new HashSet<T>();

        for (int i = 0; i < virtualNodes.Count && seenServers.Count < count; i++)
        {
            int currentIndex = (startIndex + i) % virtualNodes.Count;
            T server = virtualNodes[currentIndex].Server;

            if (seenServers.Add(server))
            {
                yield return server;
            }
        }
    }

    private void RebuildVirtualNodes()
    {
        var newVirtualNodes = new List<VirtualNode<T>>();

        foreach (var kvp in _serverVirtualNodes)
        {
            T server = kvp.Key;
            int virtualNodeCount = kvp.Value;

            for (int i = 0; i < virtualNodeCount; i++)
            {
                string virtualNodeKey = $"{server}:{i}";
                byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(virtualNodeKey);
                uint hash = ComputeKeyHash(keyBytes);
                newVirtualNodes.Add(new VirtualNode<T>(hash, server));
            }
        }

        newVirtualNodes.Sort();
        _sortedVirtualNodes = newVirtualNodes;
    }

    private uint ComputeKeyHash(byte[] key)
    {
        byte[] hashBytes = _hashAlgorithm.ComputeHash(key);
        return BitConverter.ToUInt32(hashBytes, 0);
    }

    private static int FindServerIndex(List<VirtualNode<T>> virtualNodes, uint hash)
    {
        int left = 0;
        int right = virtualNodes.Count - 1;

        while (left <= right)
        {
            int mid = left + ((right - left) / 2);
            uint midHash = virtualNodes[mid].Hash;

            if (midHash == hash)
            {
                return mid;
            }
            else if (midHash < hash)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return left >= virtualNodes.Count ? 0 : left;
    }

    /// <summary>
    /// Creates a snapshot of the current configuration and stores it in history.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when version history is not enabled.</exception>
    /// <exception cref="HashRingHistoryLimitExceededException">Thrown when creating the snapshot would exceed the history limit.</exception>
    public void CreateConfigurationSnapshot()
    {
        if (!IsVersionHistoryEnabled)
        {
            throw new InvalidOperationException("Cannot create configuration snapshot because version history is not enabled. " +
                "Enable version history by setting EnableVersionHistory to true in HashRingOptions.");
        }

        lock (_lock)
        {
            var servers = _serverVirtualNodes.Keys.ToArray();
            var virtualNodes = _sortedVirtualNodes.ToList();
            var snapshot = new ConfigurationSnapshot<T>(servers, virtualNodes, DateTime.UtcNow);

            _historyManager!.Add(snapshot);
        }
    }

    /// <summary>
    /// Clears all historical configurations, retaining only the current configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when version history is not enabled.</exception>
    public void ClearHistory()
    {
        if (!IsVersionHistoryEnabled)
        {
            throw new InvalidOperationException("Cannot clear history because version history is not enabled. " +
                "Enable version history by setting EnableVersionHistory to true in HashRingOptions.");
        }

        lock (_lock)
        {
            _historyManager!.Clear();
        }
    }

    /// <summary>
    /// Gets server candidates that should handle the specified key, including servers from historical configurations.
    /// </summary>
    /// <param name="key">The key to find servers for.</param>
    /// <returns>Server candidates with current server first, followed by unique servers from historical configurations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no servers are available in any configuration.</exception>
    public ServerCandidateResult<T> GetServerCandidates(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            var candidates = new List<T>();
            var configurationCount = 1; // Current configuration always counts
            var hasHistory = IsVersionHistoryEnabled && _historyManager!.HasSnapshots;

            // Get server from current configuration
            if (!IsEmpty)
            {
                var currentServer = GetServer(key);
                candidates.Add(currentServer);
            }
            else if (!hasHistory)
            {
                throw new InvalidOperationException("No servers are available in the current configuration and no history exists.");
            }

            // Get servers from historical configurations
            if (hasHistory)
            {
                var snapshots = _historyManager!.GetSnapshots();
                configurationCount += snapshots.Count;

                foreach (var snapshot in snapshots)
                {
                    try
                    {
                        var historicalServer = snapshot.GetServer(key);

                        // Add only if not already in candidates (deduplication)
                        if (!candidates.Contains(historicalServer))
                        {
                            candidates.Add(historicalServer);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Skip snapshots with no servers
                        continue;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException("No servers are available in any configuration.");
            }

            return new ServerCandidateResult<T>(candidates.AsReadOnly(), configurationCount, hasHistory);
        }
    }

    /// <summary>
    /// Gets multiple server candidates that should handle the specified key from current and historical configurations.
    /// </summary>
    /// <param name="key">The key to find servers for.</param>
    /// <param name="maxCandidates">The maximum number of unique server candidates to return.</param>
    /// <returns>Server candidates in priority order, with deduplication across configurations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxCandidates is less than 0.</exception>
    public ServerCandidateResult<T> GetServerCandidates(byte[] key, int maxCandidates)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (maxCandidates < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCandidates), maxCandidates,
                "Maximum candidates must be non-negative.");
        }

        if (maxCandidates == 0)
        {
            lock (_lock)
            {
                var configurationCount = 1; // Current configuration always counts
                var hasHistory = IsVersionHistoryEnabled && _historyManager!.HasSnapshots;

                if (hasHistory)
                {
                    configurationCount += _historyManager!.Count;
                }

                return new ServerCandidateResult<T>([], configurationCount, hasHistory);
            }
        }

        // Get all candidates first, then limit
        var allCandidates = GetServerCandidates(key);

        if (maxCandidates >= allCandidates.Servers.Count)
        {
            return allCandidates;
        }

        // Take only the first maxCandidates
        var limitedServers = new T[maxCandidates];
        for (int i = 0; i < maxCandidates; i++)
        {
            limitedServers[i] = allCandidates.Servers[i];
        }

        return new ServerCandidateResult<T>(limitedServers, allCandidates.ConfigurationCount, allCandidates.HasHistory);
    }

    /// <summary>
    /// Tries to get server candidates that should handle the specified key from current and historical configurations.
    /// </summary>
    /// <param name="key">The key to find servers for.</param>
    /// <param name="result">When this method returns, contains the server candidates if found; otherwise, null.</param>
    /// <returns>true if server candidates were found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public bool TryGetServerCandidates(byte[] key, out ServerCandidateResult<T>? result)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            result = GetServerCandidates(key);
            return true;
        }
        catch (InvalidOperationException)
        {
            result = null;
            return false;
        }
    }
}
