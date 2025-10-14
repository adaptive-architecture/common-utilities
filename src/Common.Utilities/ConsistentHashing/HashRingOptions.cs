namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Configuration options for creating HashRing instances.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Snapshot Management:</strong> All HashRing instances have snapshot history enabled by default.
/// Configure <see cref="MaxHistorySize"/> and <see cref="HistoryLimitBehavior"/> to control how snapshots
/// are managed when the limit is reached.
/// </para>
/// <para>
/// <strong>Default Behavior:</strong> By default, HashRing uses FIFO (First-In-First-Out) removal with a
/// maximum of 3 snapshots. This allows continuous operation without manual snapshot cleanup.
/// </para>
/// </remarks>
public sealed class HashRingOptions
{
    /// <summary>
    /// Gets or sets the default number of virtual nodes per server.
    /// Default value is 42.
    /// </summary>
    public int DefaultVirtualNodes { get; set; } = 42;

    /// <summary>
    /// Gets or sets the maximum number of historical configurations to retain.
    /// Snapshot history is always enabled for all HashRing instances.
    /// Default value is 3. Minimum value is 1.
    /// </summary>
    public int MaxHistorySize { get; set; } = 3;

    /// <summary>
    /// Gets or sets the behavior when <see cref="HashRing{T}.CreateConfigurationSnapshot"/> is called
    /// and the snapshot history has reached <see cref="MaxHistorySize"/>.
    /// </summary>
    /// <value>
    /// Default value is <see cref="ConsistentHashing.HistoryLimitBehavior.RemoveOldest"/> (FIFO removal).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>RemoveOldest (default):</strong> When the limit is reached, the oldest snapshot is automatically
    /// removed to make room for the new one. This provides FIFO (First-In-First-Out) behavior and allows
    /// continuous snapshot creation without manual intervention.
    /// </para>
    /// <para>
    /// <strong>ThrowError:</strong> When the limit is reached, <see cref="HashRing{T}.CreateConfigurationSnapshot"/>
    /// throws <see cref="HashRingHistoryLimitExceededException"/>. Use this when you need explicit control
    /// over snapshot management or want to detect when the limit is reached.
    /// </para>
    /// <para>
    /// See <see cref="HistoryLimitBehavior"/> for more details on each behavior.
    /// </para>
    /// </remarks>
    public HistoryLimitBehavior HistoryLimitBehavior { get; set; } = HistoryLimitBehavior.RemoveOldest;

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
