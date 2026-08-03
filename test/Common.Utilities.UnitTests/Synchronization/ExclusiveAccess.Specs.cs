namespace AdaptArch.Common.Utilities.Synchronization;

public class ExclusiveAccessSpecs
{
    [Fact]
    public void Only_One_Lock_Can_Be_Acquired_At_A_Time()
    {
        var value = new object();
        using var exclusiveAccess = new ExclusiveAccess<object>(value, TimeSpan.FromSeconds(1));

        var firstLock = exclusiveAccess.Lock();
        Assert.Same(value, firstLock.Value); // We have access to the same value

        _ = Assert.Throws<TimeoutException>(() => _ = exclusiveAccess.Lock());

        var capturedValue = firstLock.Value; // Known limitation. Capturing the value before disposing the lock will "leak" the value.
        firstLock.Dispose();
        _ = Assert.Throws<ObjectDisposedException>(() => _ = firstLock.Value);

        var thirdLock = exclusiveAccess.Lock();
        Assert.Same(value, thirdLock.Value); // We have access to the same value

        // Assert
        Assert.Same(value, thirdLock.Value); // We have access to the same value

        GC.Collect();
    }

    [Fact]
    public void Lock_Dispose_Should_Be_Idempotent()
    {
        var value = new object();
        using var exclusiveAccess = new ExclusiveAccess<object>(value, TimeSpan.FromMilliseconds(100));

        var firstLock = exclusiveAccess.Lock();
        firstLock.Dispose();
        firstLock.Dispose(); // A second dispose must be a no-op, not a second semaphore release.

        var secondLock = exclusiveAccess.Lock();

        // If the double dispose had over-released the semaphore, this would acquire a second "exclusive" lock.
        _ = Assert.Throws<TimeoutException>(() => _ = exclusiveAccess.Lock());

        secondLock.Dispose();
    }
}
