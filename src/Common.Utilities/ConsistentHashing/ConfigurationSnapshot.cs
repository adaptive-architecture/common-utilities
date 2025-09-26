namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Represents an immutable snapshot of a consistent hash ring configuration at a specific point in time.
/// </summary>
/// <typeparam name="T">The type of server identifiers.</typeparam>
internal sealed class ConfigurationSnapshot<T> where T : IEquatable<T>
{
    private readonly List<VirtualNode<T>> _sortedVirtualNodes;
    private readonly IHashAlgorithm _hashAlgorithm;

    /// <summary>
    /// Gets the servers in this configuration snapshot.
    /// </summary>
    public IReadOnlyList<T> Servers { get; }

    /// <summary>
    /// Gets the virtual nodes in this configuration snapshot.
    /// </summary>
    public IReadOnlyList<VirtualNode<T>> VirtualNodes { get; }

    /// <summary>
    /// Gets the timestamp when this snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the number of servers in this snapshot.
    /// </summary>
    public int ServerCount => Servers.Count;

    /// <summary>
    /// Gets the number of virtual nodes in this snapshot.
    /// </summary>
    public int VirtualNodeCount => VirtualNodes.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSnapshot{T}"/> class.
    /// </summary>
    /// <param name="servers">The servers in this configuration.</param>
    /// <param name="virtualNodes">The virtual nodes in this configuration.</param>
    /// <param name="createdAt">The timestamp when this snapshot was created.</param>
    /// <param name="hashAlgorithm">The hash algorithm used for key hashing.</param>
    /// <exception cref="ArgumentNullException">Thrown when servers, virtualNodes, or hashAlgorithm is null.</exception>
    public ConfigurationSnapshot(IReadOnlyList<T> servers, IReadOnlyList<VirtualNode<T>> virtualNodes, DateTime createdAt, IHashAlgorithm hashAlgorithm)
    {
        Servers = servers ?? throw new ArgumentNullException(nameof(servers));
        VirtualNodes = virtualNodes ?? throw new ArgumentNullException(nameof(virtualNodes));
        CreatedAt = createdAt;
        _hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm));

        _sortedVirtualNodes = [.. virtualNodes.OrderBy(node => node.Hash)];
    }

    /// <summary>
    /// Gets the server that should handle the specified key using this configuration snapshot.
    /// </summary>
    /// <param name="key">The key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no servers are available.</exception>
    public T GetServer(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_sortedVirtualNodes.Count == 0)
        {
            throw new InvalidOperationException("No servers available in this configuration snapshot.");
        }

        var keyHash = ComputeKeyHash(key);

        // Find the first virtual node with hash >= keyHash (clockwise traversal)
        var index = _sortedVirtualNodes.FindIndex(node => node.Hash >= keyHash);

        // If no node found, wrap around to the first node (ring property)
        if (index == -1)
        {
            index = 0;
        }

        return _sortedVirtualNodes[index].Server;
    }

    private uint ComputeKeyHash(byte[] key)
    {
        byte[] hashBytes = _hashAlgorithm.ComputeHash(key);
        return BitConverter.ToUInt32(hashBytes, 0);
    }
}
