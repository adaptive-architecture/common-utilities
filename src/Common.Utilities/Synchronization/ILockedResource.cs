namespace AdaptArch.Common.Utilities.Synchronization;

/// <summary>
/// A locked resource.
/// </summary>
/// <typeparam name="T">The type of the resource.</typeparam>
public interface ILockedResource<out T> : IDisposable
{
    /// <summary>
    /// The value of the resource.
    /// </summary>
    T Value { get; }
}
