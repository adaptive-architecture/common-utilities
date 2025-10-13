namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Manages the history of configuration snapshots with a configurable maximum size limit.
/// Implements a circular buffer behavior where adding beyond the limit throws an exception.
/// </summary>
/// <typeparam name="T">The type of server identifiers.</typeparam>
internal sealed class HistoryManager<T> where T : IEquatable<T>
{
    private readonly List<ConfigurationSnapshot<T>> _snapshots;

    /// <summary>
    /// Gets the maximum number of snapshots that can be stored.
    /// </summary>
    public int MaxSize { get; }

    /// <summary>
    /// Gets the current number of snapshots stored.
    /// </summary>
    public int Count => _snapshots.Count;

    /// <summary>
    /// Gets a value indicating whether the history manager has reached its maximum capacity.
    /// </summary>
    public bool IsFull => _snapshots.Count >= MaxSize;

    /// <summary>
    /// Gets a value indicating whether there are any snapshots stored.
    /// </summary>
    public bool HasSnapshots => _snapshots.Count > 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryManager{T}"/> class.
    /// </summary>
    /// <param name="maxSize">The maximum number of snapshots to store.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxSize is less than 1.</exception>
    public HistoryManager(int maxSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);
        MaxSize = maxSize;
        _snapshots = new List<ConfigurationSnapshot<T>>(maxSize);
    }

    /// <summary>
    /// Adds a configuration snapshot to the history.
    /// </summary>
    /// <param name="snapshot">The snapshot to add.</param>
    /// <param name="behavior">The behavior to apply when the history is at maximum capacity.</param>
    /// <exception cref="ArgumentNullException">Thrown when snapshot is null.</exception>
    /// <exception cref="HashRingHistoryLimitExceededException">
    /// Thrown when adding would exceed the maximum size limit and <paramref name="behavior"/> is <see cref="HistoryLimitBehavior.ThrowError"/>.
    /// </exception>
    public void Add(ConfigurationSnapshot<T> snapshot, HistoryLimitBehavior behavior)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (IsFull)
        {
            if (behavior == HistoryLimitBehavior.RemoveOldest)
            {
                // FIFO removal: remove the oldest snapshot (index 0)
                _snapshots.RemoveAt(0);
            }
            else // ThrowError
            {
                throw new HashRingHistoryLimitExceededException(MaxSize, _snapshots.Count);
            }
        }

        _snapshots.Add(snapshot);
    }

    /// <summary>
    /// Clears all snapshots from the history.
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
    }

    /// <summary>
    /// Gets all snapshots in the history in chronological order (oldest first).
    /// </summary>
    /// <returns>A read-only list of configuration snapshots.</returns>
    public IReadOnlyList<ConfigurationSnapshot<T>> GetSnapshots()
    {
        return _snapshots.AsReadOnly();
    }

    /// <summary>
    /// Gets snapshots in reverse chronological order (newest first).
    /// </summary>
    /// <returns>A read-only list of configuration snapshots in reverse order.</returns>
    public IReadOnlyList<ConfigurationSnapshot<T>> GetSnapshotsReverse()
    {
        return _snapshots.AsEnumerable().Reverse().ToList().AsReadOnly();
    }

    /// <summary>
    /// Tries to get the most recent snapshot.
    /// </summary>
    /// <param name="snapshot">When this method returns, contains the most recent snapshot if found; otherwise, null.</param>
    /// <returns>true if a snapshot was found; otherwise, false.</returns>
    public bool TryGetLatest(out ConfigurationSnapshot<T>? snapshot)
    {
        if (_snapshots.Count > 0)
        {
            snapshot = _snapshots[^1];
            return true;
        }

        snapshot = null;
        return false;
    }

    /// <summary>
    /// Gets the available space remaining in the history.
    /// </summary>
    /// <returns>The number of additional snapshots that can be added.</returns>
    public int GetRemainingCapacity()
    {
        return MaxSize - _snapshots.Count;
    }
}
