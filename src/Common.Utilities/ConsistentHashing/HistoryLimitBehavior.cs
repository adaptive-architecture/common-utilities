namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Specifies the behavior when <see cref="HashRing{T}.CreateConfigurationSnapshot"/> is called
/// and the snapshot history has reached <see cref="HashRingOptions.MaxHistorySize"/>.
/// </summary>
public enum HistoryLimitBehavior
{
    /// <summary>
    /// Throws a <see cref="HashRingHistoryLimitExceededException"/> when the history limit is reached.
    /// Use this mode when you want strict control over snapshot history management.
    /// </summary>
    ThrowError = 0,

    /// <summary>
    /// Automatically removes the oldest snapshot (FIFO) when the history limit is reached.
    /// This is the default behavior, allowing continuous snapshot creation without manual history management.
    /// </summary>
    RemoveOldest = 1
}
