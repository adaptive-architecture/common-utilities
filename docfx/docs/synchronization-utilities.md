# Synchronization Utilities

The Common.Utilities package provides thread-safe synchronization utilities for managing concurrent access to resources and ensuring data consistency in multi-threaded applications.

## Overview

Synchronization utilities help you:
- **Control concurrent access**: Ensure only one thread can access a resource at a time
- **Prevent race conditions**: Avoid data corruption from simultaneous modifications
- **Implement thread-safe patterns**: Provide safe access to shared resources
- **Handle timeouts**: Prevent indefinite waiting for resource access

## ExclusiveAccess&lt;T&gt;

A thread-safe wrapper that provides exclusive access to a resource using lock semantics.

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.Synchronization;

// Create exclusive access wrapper around your resource (T must be a reference type)
var list = new ExclusiveAccess<List<int>>(new List<int>(), TimeSpan.FromSeconds(5));

// Thread-safe operations
await Task.Run(() =>
{
    using (var locked = list.Lock())
    {
        // Only one thread can execute this block at a time
        locked.Value.Add(42);
        Console.WriteLine($"Count: {locked.Value.Count}");
    }
});
```

### With Timeout

```csharp
var sharedResource = new ExclusiveAccess<List<string>>(new List<string>(), TimeSpan.FromSeconds(5));

try
{
    // Timeout was configured in the constructor
    using (var locked = sharedResource.Lock())
    {
        locked.Value.Add("New item");
        // Simulate some work
        await Task.Delay(1000);
    }
}
catch (TimeoutException)
{
    Console.WriteLine("Could not acquire lock within timeout period");
}
```

### Async Operations

```csharp
var asyncResource = new ExclusiveAccess<Dictionary<string, object>>(new Dictionary<string, object>(), TimeSpan.FromSeconds(5));

using (var locked = asyncResource.Lock())
{
    // Perform async operations while holding the lock
    var data = await FetchDataAsync();
    locked.Value["timestamp"] = DateTime.UtcNow;
    locked.Value["data"] = data;
}
```

## ILockedResource&lt;T&gt;

The interface returned by `ExclusiveAccess<T>.Lock()` that provides access to the protected resource.

```csharp
public interface ILockedResource<out T> : IDisposable
{
    T Value { get; }
}
```

### Key Features

- **Automatic disposal**: Lock is automatically released when disposed
- **Resource access**: Provides safe access to the underlying resource
- **Exception safety**: Lock is released even if exceptions occur

## Real-World Examples

### Thread-Safe Cache

```csharp
public class ThreadSafeCache<TKey, TValue>
{
    private readonly ExclusiveAccess<Dictionary<TKey, TValue>> _cache;

    public ThreadSafeCache()
    {
        _cache = new ExclusiveAccess<Dictionary<TKey, TValue>>(new Dictionary<TKey, TValue>(), TimeSpan.FromSeconds(5));
    }

    public void Set(TKey key, TValue value)
    {
        using (var locked = _cache.Lock())
        {
            locked.Value[key] = value;
        }
    }

    public bool TryGet(TKey key, out TValue value)
    {
        using (var locked = _cache.Lock())
        {
            return locked.Value.TryGetValue(key, out value);
        }
    }

    public void Remove(TKey key)
    {
        using (var locked = _cache.Lock())
        {
            locked.Value.Remove(key);
        }
    }

    public int Count
    {
        get
        {
            using (var locked = _cache.Lock())
            {
                return locked.Value.Count;
            }
        }
    }
}
```

### Thread-Safe Counter with Statistics

```csharp
public class StatisticsCounter
{
    private readonly ExclusiveAccess<CounterState> _state;

    public StatisticsCounter()
    {
        _state = new ExclusiveAccess<CounterState>(new CounterState(), TimeSpan.FromSeconds(5));
    }

    public void Increment(int value = 1)
    {
        using (var locked = _state.Lock())
        {
            var state = locked.Value;
            state.Count += value;
            state.Total += value;
            state.LastUpdated = DateTime.UtcNow;
            
            if (value > state.MaxIncrement)
                state.MaxIncrement = value;
        }
    }

    public CounterSnapshot GetSnapshot()
    {
        using (var locked = _state.Lock())
        {
            var state = locked.Value;
            return new CounterSnapshot
            {
                Count = state.Count,
                Total = state.Total,
                MaxIncrement = state.MaxIncrement,
                LastUpdated = state.LastUpdated
            };
        }
    }

    public void Reset()
    {
        using (var locked = _state.Lock())
        {
            locked.Value.Count = 0;
            locked.Value.Total = 0;
            locked.Value.MaxIncrement = 0;
            locked.Value.LastUpdated = DateTime.UtcNow;
        }
    }

    private class CounterState
    {
        public long Count { get; set; }
        public long Total { get; set; }
        public int MaxIncrement { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class CounterSnapshot
    {
        public long Count { get; set; }
        public long Total { get; set; }
        public int MaxIncrement { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
```

### Coordinated Resource Pool

```csharp
public class ResourcePool<T> where T : class
{
    private readonly ExclusiveAccess<Queue<T>> _availableResources;
    private readonly Func<T> _resourceFactory;
    private readonly Action<T> _resourceDisposer;
    private readonly int _maxSize;

    public ResourcePool(Func<T> resourceFactory, Action<T> resourceDisposer = null, int maxSize = 10)
    {
        _availableResources = new ExclusiveAccess<Queue<T>>(new Queue<T>(), TimeSpan.FromSeconds(30));
        _resourceFactory = resourceFactory;
        _resourceDisposer = resourceDisposer ?? (_ => { });
        _maxSize = maxSize;
    }

    public T Acquire()
    {
        using (var locked = _availableResources.Lock())
        {
            var queue = locked.Value;
            
            if (queue.Count > 0)
            {
                return queue.Dequeue();
            }
        }

        // No available resources, create a new one
        return _resourceFactory();
    }

    public void Release(T resource)
    {
        if (resource == null) return;

        using (var locked = _availableResources.Lock())
        {
            var queue = locked.Value;
            
            if (queue.Count < _maxSize)
            {
                queue.Enqueue(resource);
            }
            else
            {
                // Pool is full, dispose the resource
                _resourceDisposer(resource);
            }
        }
    }

    public void Clear()
    {
        using (var locked = _availableResources.Lock())
        {
            var queue = locked.Value;
            
            while (queue.Count > 0)
            {
                var resource = queue.Dequeue();
                _resourceDisposer(resource);
            }
        }
    }
}
```

## Advanced Patterns

### Conditional Access

```csharp
public class ConditionalAccess<T>
{
    private readonly ExclusiveAccess<T> _resource;
    private readonly Func<T, bool> _condition;

    public ConditionalAccess(T resource, Func<T, bool> condition)
    {
        _resource = new ExclusiveAccess<T>(resource, TimeSpan.FromSeconds(5));
        _condition = condition;
    }

    public bool TryExecute(Action<T> action)
    {
        try
        {
            using (var locked = _resource.Lock())
            {
                if (_condition(locked.Value))
                {
                    action(locked.Value);
                    return true;
                }
                return false;
            }
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}

// Usage
var conditionalResource = new ConditionalAccess<List<string>>(
    new List<string>(), 
    list => list.Count < 100); // Only allow access if list has less than 100 items

bool success = conditionalResource.TryExecute(list =>
{
    list.Add("New item");
});
```

### Read-Write Separation Pattern

```csharp
public class ReadWriteResource<T>
{
    private readonly ExclusiveAccess<T> _resource;

    public ReadWriteResource(T resource)
    {
        _resource = new ExclusiveAccess<T>(resource, TimeSpan.FromSeconds(5));
    }

    public TResult Read<TResult>(Func<T, TResult> reader)
    {
        using (var locked = _resource.Lock())
        {
            return reader(locked.Value);
        }
    }

    public void Write(Action<T> writer)
    {
        using (var locked = _resource.Lock())
        {
            writer(locked.Value);
        }
    }

    public TResult ReadWrite<TResult>(Func<T, TResult> operation)
    {
        using (var locked = _resource.Lock())
        {
            return operation(locked.Value);
        }
    }
}

// Usage
var readWriteList = new ReadWriteResource<List<string>>(new List<string>());

// Read operation
int count = readWriteList.Read(list => list.Count);

// Write operation
readWriteList.Write(list => list.Add("New item"));

// Combined read-write operation
string lastItem = readWriteList.ReadWrite(list => 
{
    list.Add("Another item");
    return list.LastOrDefault();
});
```

## Performance Considerations

### Lock Duration

```csharp
// ❌ Bad: Long-running operation holding lock
using (var locked = resource.Lock())
{
    // This blocks all other threads for a long time
    await SomeSlowDatabaseOperation(locked.Value);
}

// ✅ Good: Minimize lock duration
var data = PrepareData();
using (var locked = resource.Lock())
{
    // Quick operation while holding lock
    locked.Value.Update(data);
}
await SomeSlowDatabaseOperation(data);
```

### Avoid Nested Locks

```csharp
// ❌ Bad: Risk of deadlock
using (var locked1 = resource1.Lock())
{
    using (var locked2 = resource2.Lock()) // Potential deadlock
    {
        // Operations
    }
}

// ✅ Good: Acquire all locks at once or use ordering
using (var locked1 = resource1.Lock())
using (var locked2 = resource2.Lock())
{
    // Operations
}
```

## Best Practices

1. **Keep lock duration minimal**: Only hold locks for the shortest time necessary
2. **Use timeouts**: Always specify timeouts to prevent indefinite blocking
3. **Dispose properly**: Use `using` statements to ensure locks are released
4. **Avoid complex operations**: Don't perform I/O or long computations while holding locks
5. **Consider alternatives**: For read-heavy scenarios, consider `ReaderWriterLockSlim` or concurrent collections
6. **Handle exceptions**: Ensure locks are released even when exceptions occur
7. **Document locking strategy**: Make thread-safety guarantees clear in your APIs
8. **Test under load**: Verify behavior under concurrent access scenarios

These synchronization utilities provide a foundation for building thread-safe applications while maintaining simplicity and avoiding common concurrency pitfalls.

## Related Documentation

- [Extension Methods](extension-methods.md)
- [Leader Election](leader-election.md)