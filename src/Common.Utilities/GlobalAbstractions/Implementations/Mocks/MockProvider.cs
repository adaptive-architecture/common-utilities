namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

/// <summary>
/// Base class for implementing a mock provider.
/// The implementation will loop through the items it receives as part of the constructor.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class MockProvider<T>
{
    private readonly T[] _items;
    private int _currentIndex = -1;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="items">The items to loop through.</param>
    protected MockProvider(T[] items) => _items = items;

    /// <summary>
    /// Get the next value from the collection.
    /// </summary>
    protected T GetNextValue()
    {
        var nextIndex = Interlocked.Increment(ref _currentIndex);
        return _items[nextIndex % _items.Length];
    }
}
