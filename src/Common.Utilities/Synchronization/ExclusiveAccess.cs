namespace AdaptArch.Common.Utilities.Synchronization;

/// <summary>
/// A class to provide exclusive access to a resource.
/// </summary>
/// <typeparam name="T">The type of the resource.</typeparam>
public sealed class ExclusiveAccess<T> : IDisposable
    where T : class
{
    private readonly T _value;
    private readonly TimeSpan _waitTimeout;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="value">The value to wrap</param>
    /// <param name="waitTimeout">The time to wait when trying to acquire the lock.</param>
    public ExclusiveAccess(T value, TimeSpan waitTimeout)
    {
        _value = value;
        _waitTimeout = waitTimeout;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Lock the resource.
    /// </summary>
    public LockedResource Lock()
    {
        return new LockedResource(_value, _semaphore, _waitTimeout);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// A class to represent a locked resource.
    /// </summary>
    public sealed class LockedResource : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private T? _value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value to wrap</param>
        /// <param name="semaphore">The semaphore used for locking.</param>
        /// <param name="timeout">The time to wait when trying to acquire the lock.</param>
        public LockedResource(T value, SemaphoreSlim semaphore, TimeSpan timeout)
        {
            _value = value;
            _semaphore = semaphore;
            if (!_semaphore.Wait(timeout))
            {
                throw new TimeoutException("Could not acquire the lock in the given time.");
            }
        }

        /// <summary>
        /// The value.
        /// </summary>
        public T Value => _value ?? throw new ObjectDisposedException(nameof(LockedResource));

        /// <summary>
        /// Dispose the resource.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _value = null;
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~LockedResource()
        {
            Dispose(false);
        }
    }
}
