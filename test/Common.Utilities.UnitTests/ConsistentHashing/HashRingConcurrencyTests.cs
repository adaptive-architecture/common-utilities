using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;
#pragma warning disable S2925 // SONAR: Do not use 'Thread.Sleep()' in a test.
/// <summary>
/// Tests for thread-safe concurrent operations (FR-018, FR-019, FR-020, AS-7)
/// </summary>
public sealed class HashRingConcurrencyTests
{
    [Fact]
    public async Task GetServer_ConcurrentReads_NoExceptions()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - 100 threads calling GetServer concurrently
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                var key = new byte[] { (byte)i, 2, 3, 4 };
                var server = ring.GetServer(key);
                Assert.NotNull(server);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert - No exceptions should occur
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task CreateConfigurationSnapshot_WhileConcurrentReads_NoExceptions()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Multiple threads reading, one thread creating snapshot
        var readTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < 10; j++)
                {
                    var key = new byte[] { (byte)i, (byte)j, 3, 4 };
                    var server = ring.GetServer(key);
                    Assert.NotNull(server);
                    Thread.Sleep(1); // Small delay to increase overlap
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        var snapshotTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(10, TestContext.Current.CancellationToken); // Let some reads start
                ring.Add("server3");
                ring.CreateConfigurationSnapshot(); // Should use lock
                await Task.Delay(10, TestContext.Current.CancellationToken);
                ring.Add("server4");
                ring.CreateConfigurationSnapshot();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(readTasks.Concat(new[] { snapshotTask }));

        // Assert - No exceptions should occur
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task CreateConfigurationSnapshot_MultipleWriters_Sequential()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        var snapshotCounts = new System.Collections.Concurrent.ConcurrentBag<int>();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Multiple threads creating snapshots concurrently
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            try
            {
                ring.CreateConfigurationSnapshot();
                // Record the history count after snapshot creation
                snapshotCounts.Add(ring.HistoryCount);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        // All snapshots should have been created (MaxHistorySize permitting)
        Assert.True(ring.HistoryCount > 0);
    }

    [Fact]
    public async Task GetServer_AfterSnapshotCreation_SeesNewSnapshot()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        _ = ring.GetServer([1, 2, 3, 4]);

        // Act - Create snapshot in thread A
        var snapshotTask = Task.Run(() =>
        {
            ring.Add("server2");
            ring.CreateConfigurationSnapshot();
        }, TestContext.Current.CancellationToken);

        await snapshotTask;

        // Wait a bit to ensure visibility
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // Get server in thread B - should see new snapshot (volatile visibility)
        var results = new HashSet<string>();
        await Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var key = new byte[] { (byte)i, 2, 3, 4 };
                results.Add(ring.GetServer(key));
            }
        }, TestContext.Current.CancellationToken);

        // Assert - Should include server2 (new snapshot visible)
        Assert.Contains("server2", results);
    }

    [Fact]
    public async Task TryGetServer_ConcurrentReads_NoExceptions()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var successCount = 0;

        // Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                var key = new byte[] { (byte)i, 2, 3, 4 };
                if (ring.TryGetServer(key, out var server))
                {
                    Interlocked.Increment(ref successCount);
                    Assert.NotNull(server);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(100, successCount); // All should succeed
    }

    [Fact]
    public async Task GetServers_ConcurrentReads_NoExceptions()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                var key = new byte[] { (byte)i, 2, 3, 4 };
                var servers = ring.GetServers(key, 2).ToList();
                Assert.NotEmpty(servers);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task ClearHistory_WhileConcurrentReads_HandlesGracefully()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Readers and history clearer running concurrently
        var readTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            try
            {
                for (int j = 0; j < 20; j++)
                {
                    var key = new byte[] { (byte)i, (byte)j, 3, 4 };
                    // TryGetServer to avoid exceptions after clear
                    ring.TryGetServer(key, out _);
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        })).ToArray();

        var clearTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
                ring.ClearHistory(); // Should use lock
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(readTasks.Concat(new[] { clearTask }));

        // Assert - No exceptions from concurrent operations
        Assert.Empty(exceptions);
    }
}
