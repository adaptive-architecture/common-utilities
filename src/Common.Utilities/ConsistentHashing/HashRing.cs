using System.Collections.Concurrent;

namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// A consistent hash ring implementation for distributing keys across servers.
/// <para>
/// <strong>Snapshot-Based Lookups:</strong> All key lookups (<see cref="GetServer(global::System.Byte[])"/>, <see cref="TryGetServer"/>, <see cref="GetServers"/>)
/// use configuration snapshots created via <see cref="CreateConfigurationSnapshot"/>.
/// The current ring configuration is not used for lookups until a snapshot is created.
/// </para>
/// <para>
/// <strong>Always-On History:</strong> Snapshot history management is always enabled.
/// When the history limit (<see cref="MaxHistorySize"/>) is reached, behavior is determined by
/// <see cref="HistoryLimitBehavior"/> (default: FIFO removal of oldest snapshots).
/// </para>
/// </summary>
/// <typeparam name="T">The type of server identifiers.</typeparam>
public sealed class HashRing<T> where T : IEquatable<T>
{
    private readonly IHashAlgorithm _hashAlgorithm;
    private readonly int _defaultVirtualNodes;
    private readonly Lock _lock = new();
    private readonly ConcurrentDictionary<T, int> _serverVirtualNodes = new();
    private volatile List<VirtualNode<T>> _sortedVirtualNodes = [];

    // Snapshot management - always enabled
    private readonly HistoryManager<T> _historyManager;
    private readonly HistoryLimitBehavior _historyLimitBehavior;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with SHA-1 hash algorithm.
    /// Uses default snapshot history settings: max 3 snapshots with FIFO removal.
    /// </summary>
    /// <remarks>
    /// Snapshot history is always enabled. Call <see cref="CreateConfigurationSnapshot"/> after
    /// adding servers to enable key lookups.
    /// </remarks>
    public HashRing() : this(new Sha1HashAlgorithm(), 42)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with a custom hash algorithm.
    /// Uses default snapshot history settings: max 3 snapshots with FIFO removal.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <remarks>
    /// Snapshot history is always enabled. Call <see cref="CreateConfigurationSnapshot"/> after
    /// adding servers to enable key lookups.
    /// </remarks>
    public HashRing(IHashAlgorithm hashAlgorithm) : this(hashAlgorithm, 42)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with a custom hash algorithm and default virtual nodes.
    /// Uses default snapshot history settings: max 3 snapshots with FIFO removal.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <param name="defaultVirtualNodes">The default number of virtual nodes per server.</param>
    /// <remarks>
    /// Snapshot history is always enabled. Call <see cref="CreateConfigurationSnapshot"/> after
    /// adding servers to enable key lookups.
    /// </remarks>
    public HashRing(IHashAlgorithm hashAlgorithm, int defaultVirtualNodes)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithm);
        _hashAlgorithm = hashAlgorithm;
        _defaultVirtualNodes = defaultVirtualNodes;

        // Initialize snapshot management with defaults
        _historyManager = new HistoryManager<T>(maxSize: 3);
        _historyLimitBehavior = HistoryLimitBehavior.RemoveOldest;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRing{T}"/> class with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the hash ring.</param>
    /// <remarks>
    /// Snapshot history is always enabled. Configure <see cref="HashRingOptions.MaxHistorySize"/> and
    /// <see cref="HashRingOptions.HistoryLimitBehavior"/> to control snapshot management.
    /// Call <see cref="CreateConfigurationSnapshot"/> after adding servers to enable key lookups.
    /// </remarks>
    public HashRing(HashRingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _hashAlgorithm = options.HashAlgorithm;
        _defaultVirtualNodes = options.DefaultVirtualNodes;

        // Snapshot management is always enabled
        _historyManager = new HistoryManager<T>(options.MaxHistorySize);
        _historyLimitBehavior = options.HistoryLimitBehavior;
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
    /// Gets the current number of configuration snapshots stored in history.
    /// Snapshot history is always enabled for all HashRing instances.
    /// </summary>
    public int HistoryCount => _historyManager.Count;

    /// <summary>
    /// Gets the maximum number of configuration snapshots that can be stored.
    /// Snapshot history is always enabled for all HashRing instances.
    /// </summary>
    public int MaxHistorySize => _historyManager.MaxSize;

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
    /// Atomically adds multiple servers to the hash ring with their respective virtual node counts.
    /// </summary>
    /// <param name="servers">A collection of key-value pairs where the key is the server and the value is the number of virtual nodes.</param>
    /// <exception cref="ArgumentNullException">Thrown when servers collection is null or contains null servers.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any virtual node count is less than 1.</exception>
    public void AddRange(IEnumerable<KeyValuePair<T, int>> servers)
    {
        ArgumentNullException.ThrowIfNull(servers);

        // Validate all inputs before making any changes
        var serverList = servers.ToList();
        foreach (var kvp in serverList)
        {
            ArgumentNullException.ThrowIfNull(kvp.Key);
            ArgumentOutOfRangeException.ThrowIfLessThan(kvp.Value, 1);
        }

        if (serverList.Count == 0)
            return;

        lock (_lock)
        {
            foreach (var kvp in serverList)
            {
                _serverVirtualNodes.AddOrUpdate(kvp.Key, kvp.Value, (_, _) => kvp.Value);
            }
            RebuildVirtualNodes();
        }
    }

    /// <summary>
    /// Atomically adds multiple servers to the hash ring with the default number of virtual nodes.
    /// </summary>
    /// <param name="servers">The servers to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when servers collection is null or contains null servers.</exception>
    public void AddRange(IEnumerable<T> servers)
    {
        ArgumentNullException.ThrowIfNull(servers);

        var serversWithDefaults = servers.Select(server =>
        {
            ArgumentNullException.ThrowIfNull(server);
            return new KeyValuePair<T, int>(server, _defaultVirtualNodes);
        });

        AddRange(serversWithDefaults);
    }

    /// <summary>
    /// Atomically removes multiple servers from the hash ring.
    /// </summary>
    /// <param name="servers">The servers to remove.</param>
    /// <returns>The number of servers that were actually removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when servers collection is null or contains null servers.</exception>
    public int RemoveRange(IEnumerable<T> servers)
    {
        ArgumentNullException.ThrowIfNull(servers);

        var serverList = servers.ToList();
        foreach (var server in serverList)
        {
            ArgumentNullException.ThrowIfNull(server);
        }

        if (serverList.Count == 0)
            return 0;

        lock (_lock)
        {
            int removedCount = 0;
            foreach (var server in serverList)
            {
                if (_serverVirtualNodes.TryRemove(server, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                RebuildVirtualNodes();
            }

            return removedCount;
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
    /// This method ONLY uses configuration snapshots created via <see cref="CreateConfigurationSnapshot"/>.
    /// The current ring configuration is ignored until a snapshot is created.
    /// </summary>
    /// <param name="key">The key to find a server for.</param>
    /// <returns>The server that should handle the key from available snapshots.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no configuration snapshots are available.
    /// Call <see cref="CreateConfigurationSnapshot"/> after adding servers to enable lookups.
    /// </exception>
    public T GetServer(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_historyManager.HasSnapshots)
        {
            throw new InvalidOperationException(
                "No configuration snapshots available. Call CreateConfigurationSnapshot() after adding servers.");
        }

        return _historyManager.GetSnapshotsReverse()
            .Select(snapshot => snapshot.GetServer(key))
            .First();
    }

    /// <summary>
    /// Tries to get the server that should handle the specified key from available snapshots.
    /// This method ONLY uses configuration snapshots, not the current ring configuration.
    /// </summary>
    /// <param name="key">The key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found in snapshots; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
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
    /// This method ONLY uses configuration snapshots, not the current ring configuration.
    /// </summary>
    /// <param name="key">The key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order from available snapshots.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no configuration snapshots are available.</exception>
    public IEnumerable<T> GetServers(byte[] key, int count)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        if (!_historyManager.HasSnapshots)
        {
            throw new InvalidOperationException(
                "No configuration snapshots available. Call CreateConfigurationSnapshot() after adding servers.");
        }

        return GetServersCore(key, count);
    }

    private List<T> GetServersCore(byte[] key, int count)
    {
        if (count == 0)
        {
            return [];
        }

        // Get servers from the latest snapshot by walking the ring
        var virtualNodes = _historyManager.GetSnapshotsReverse()[0].VirtualNodes;
        if (virtualNodes.Count == 0)
        {
            return [];
        }

        var keyHash = ComputeKeyHash(key);
        var seenServers = new HashSet<T>();
        var result = new List<T>();

        // Find the starting index (first virtual node with hash >= keyHash)
        int startIndex = FindServerIndex([.. virtualNodes], keyHash);

        // Walk the ring starting from startIndex to find distinct servers
        for (int i = 0; i < virtualNodes.Count && result.Count < count; i++)
        {
            int index = (startIndex + i) % virtualNodes.Count;
            var server = virtualNodes[index].Server;

            if (seenServers.Add(server))
            {
                result.Add(server);
            }
        }

        return result;
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

    internal static int FindServerIndex(List<VirtualNode<T>> virtualNodes, uint hash)
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
    /// If the current configuration is identical to the most recent snapshot, no new snapshot is created.
    /// Snapshot history is always enabled. When the history limit is reached, behavior depends on
    /// the <see cref="HistoryLimitBehavior"/> setting (FIFO removal or exception).
    /// </summary>
    /// <exception cref="HashRingHistoryLimitExceededException">
    /// Thrown when creating the snapshot would exceed the history limit and
    /// <see cref="HistoryLimitBehavior"/> is set to <see cref="ConsistentHashing.HistoryLimitBehavior.ThrowError"/>.
    /// </exception>
    public void CreateConfigurationSnapshot()
    {
        lock (_lock)
        {
            var servers = _serverVirtualNodes.Keys.ToArray();
            var virtualNodes = _sortedVirtualNodes.ToList();

            // Check if current state matches the most recent snapshot (deduplication)
            if (_historyManager.TryGetLatest(out var latestSnapshot)
                && latestSnapshot != null
                && latestSnapshot.Servers.Count == servers.Length
                && latestSnapshot.VirtualNodes.Count == virtualNodes.Count
                && new HashSet<T>(latestSnapshot.Servers).SetEquals(servers))
            {
                // Configuration hasn't changed, skip creating duplicate snapshot
                return;
            }

            var snapshot = new ConfigurationSnapshot<T>(servers, virtualNodes, DateTime.UtcNow, _hashAlgorithm);
            _historyManager.Add(snapshot, _historyLimitBehavior);
        }
    }

    /// <summary>
    /// Clears all configuration snapshots from history.
    /// After calling this method, <see cref="GetServer(global::System.Byte[])"/> and related methods will throw
    /// <see cref="InvalidOperationException"/> until a new snapshot is created with <see cref="CreateConfigurationSnapshot"/>.
    /// </summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _historyManager.Clear();
        }
    }

}
