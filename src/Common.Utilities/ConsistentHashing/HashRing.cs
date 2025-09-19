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
}
