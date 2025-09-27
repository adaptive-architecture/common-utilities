namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Configuration options for creating HashRing instances.
/// </summary>
public sealed class HashRingOptions
{
    /// <summary>
    /// Gets or sets the default number of virtual nodes per server.
    /// Default value is 42.
    /// </summary>
    public int DefaultVirtualNodes { get; set; } = 42;

    /// <summary>
    /// Gets or sets whether version history is enabled for data migration scenarios.
    /// When enabled, the hash ring can maintain previous server configurations.
    /// Default value is false.
    /// </summary>
    public bool EnableVersionHistory { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of historical configurations to retain.
    /// Only used when EnableVersionHistory is true.
    /// Default value is 3. Minimum value is 1.
    /// </summary>
    public int MaxHistorySize { get; set; } = 3;

    /// <summary>
    /// Gets or sets the hash algorithm to use.
    /// Default is SHA1.
    /// </summary>
    public IHashAlgorithm HashAlgorithm { get; } = new Sha1HashAlgorithm();

    /// <summary>
    /// Creates a new instance of HashRingOptions with default values.
    /// </summary>
    public HashRingOptions()
    {
    }

    /// <summary>
    /// Creates a new instance of HashRingOptions with the specified hash algorithm.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when hashAlgorithm is null.</exception>
    public HashRingOptions(IHashAlgorithm hashAlgorithm)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithm);
        HashAlgorithm = hashAlgorithm;
    }

    /// <summary>
    /// Creates a new instance of HashRingOptions with the specified default virtual nodes count.
    /// </summary>
    /// <param name="defaultVirtualNodes">The default number of virtual nodes per server.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when defaultVirtualNodes is less than 1.</exception>
    public HashRingOptions(int defaultVirtualNodes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultVirtualNodes);
        DefaultVirtualNodes = defaultVirtualNodes;
    }

    /// <summary>
    /// Creates a new instance of HashRingOptions with the specified hash algorithm and default virtual nodes count.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm to use.</param>
    /// <param name="defaultVirtualNodes">The default number of virtual nodes per server.</param>
    /// <exception cref="ArgumentNullException">Thrown when hashAlgorithm is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when defaultVirtualNodes is less than 1.</exception>
    public HashRingOptions(IHashAlgorithm hashAlgorithm, int defaultVirtualNodes)
    {
        ArgumentNullException.ThrowIfNull(hashAlgorithm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(defaultVirtualNodes);
        HashAlgorithm = hashAlgorithm;
        DefaultVirtualNodes = defaultVirtualNodes;
    }
}
