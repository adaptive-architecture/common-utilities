namespace AdaptArch.Common.Utilities.Synchronization.LeaderElection.Contracts;

/// <summary>
/// Configuration options for leader election.
/// </summary>
public sealed record LeaderElectionOptions
{
    /// <summary>
    /// Gets the duration of the leadership lease.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the interval at which the leader renews its lease.
    /// Default is 10 seconds (1/3 of lease duration).
    /// </summary>
    public TimeSpan RenewalInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the interval at which non-leaders check for leadership opportunities.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan RetryInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the timeout for individual lease operations.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets optional metadata to associate with leadership.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enable continuous checking for leadership opportunities.
    /// Default is true.
    /// </summary>
    public bool EnableContinuousCheck { get; init; } = true;

    /// <summary>
    /// Validates the options and returns a new instance with corrected values if needed.
    /// </summary>
    /// <returns>A validated options instance.</returns>
    public LeaderElectionOptions Validate()
    {
        var leaseDuration = LeaseDuration < TimeSpan.FromSeconds(5)
            ? TimeSpan.FromSeconds(30)
            : LeaseDuration;

        var renewalInterval = RenewalInterval >= leaseDuration
            ? TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 3)
            : RenewalInterval;

        var retryInterval = RetryInterval >= leaseDuration
            ? TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 6)
            : RetryInterval;

        var operationTimeout = OperationTimeout >= leaseDuration
            ? TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 6)
            : OperationTimeout;

        return this with
        {
            LeaseDuration = leaseDuration,
            RenewalInterval = renewalInterval,
            RetryInterval = retryInterval,
            OperationTimeout = operationTimeout
        };
    }

    /// <summary>
    /// Creates a new instance of <see cref="LeaderElectionOptions"/> with specified lease duration and optional metadata.
    /// The other properties will be set to default values based on the lease duration:
    /// <list type="bullet">
    ///   <item>
    ///     <description>The renewal interval will be set to one-third of the lease duration.</description>
    ///   </item>
    ///   <item>
    ///     <description>The retry interval will be set to one-sixth of the lease duration.</description>
    ///   </item>
    ///   <item>
    ///     <description>The operation timeout will also be set to one-sixth of the lease duration.</description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="leaseDuration"> The duration of the leadership lease.</param>
    /// <param name="metadata">The optional metadata to associate with leadership.</param>
    /// <returns>A new instance of <see cref="LeaderElectionOptions"/> with the specified lease duration and metadata.</returns>
    public static LeaderElectionOptions Create(TimeSpan leaseDuration, IReadOnlyDictionary<string, string>? metadata)
    {
        return new LeaderElectionOptions
        {
            LeaseDuration = leaseDuration,
            RenewalInterval = TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 3),
            RetryInterval = TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 6),
            OperationTimeout = TimeSpan.FromMilliseconds(leaseDuration.TotalMilliseconds / 6),
            Metadata = metadata
        }.Validate();
    }
}
