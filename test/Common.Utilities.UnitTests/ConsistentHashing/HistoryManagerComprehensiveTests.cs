namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

/// <summary>
/// Comprehensive tests for HistoryManager class covering edge cases, boundary conditions,
/// and complex scenarios.
/// </summary>
public sealed class HistoryManagerComprehensiveTests
{
    private static readonly IHashAlgorithm DefaultHashAlgorithm = new Sha1HashAlgorithm();

    #region Constructor Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Constructor_WithValidMaxSize_InitializesCorrectly(int maxSize)
    {
        var manager = new HistoryManager<string>(maxSize);

        Assert.Equal(maxSize, manager.MaxSize);
        Assert.Equal(0, manager.Count);
        Assert.False(manager.IsFull);
        Assert.False(manager.HasSnapshots);
        Assert.Equal(maxSize, manager.GetRemainingCapacity());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    [InlineData(Int32.MinValue)]
    public void Constructor_WithInvalidMaxSize_ThrowsArgumentOutOfRangeException(int invalidMaxSize)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryManager<string>(invalidMaxSize));

        Assert.Equal("maxSize", exception.ParamName);
        Assert.Contains("must be a non-negative and", exception.Message);
    }

    #endregion

    #region Add Method Tests

    [Fact]
    public void Add_FirstSnapshot_UpdatesAllProperties()
    {
        var manager = new HistoryManager<string>(3);
        var snapshot = CreateTestSnapshot("server1");

        manager.Add(snapshot);

        Assert.Equal(1, manager.Count);
        Assert.True(manager.HasSnapshots);
        Assert.False(manager.IsFull);
        Assert.Equal(2, manager.GetRemainingCapacity());
    }

    [Fact]
    public void Add_WithNullSnapshot_ThrowsArgumentNullException()
    {
        var manager = new HistoryManager<string>(3);
        ConfigurationSnapshot<string> snapshot = null;

        var exception = Assert.Throws<ArgumentNullException>(() => manager.Add(snapshot!));
        Assert.Equal("snapshot", exception.ParamName);
    }

    [Fact]
    public void Add_MultipleDifferentSnapshots_AddsInOrder()
    {
        var manager = new HistoryManager<string>(5);
        var snapshots = new[]
        {
            CreateTestSnapshot("server1"),
            CreateTestSnapshot("server2"),
            CreateTestSnapshot("server3")
        };

        foreach (var snapshot in snapshots)
        {
            manager.Add(snapshot);
        }

        Assert.Equal(3, manager.Count);
        var retrievedSnapshots = manager.GetSnapshots();
        Assert.Equal(3, retrievedSnapshots.Count);

        // Verify order is maintained (chronological - oldest first)
        for (int i = 0; i < snapshots.Length; i++)
        {
            Assert.Equal(snapshots[i], retrievedSnapshots[i]);
        }
    }

    [Fact]
    public void Add_ToCapacity_BecomesFullButDoesNotThrow()
    {
        var manager = new HistoryManager<string>(2);

        manager.Add(CreateTestSnapshot("server1"));
        Assert.False(manager.IsFull);

        manager.Add(CreateTestSnapshot("server2"));
        Assert.True(manager.IsFull);
        Assert.Equal(0, manager.GetRemainingCapacity());
    }

    [Fact]
    public void Add_BeyondCapacity_ThrowsHashRingHistoryLimitExceededException()
    {
        var manager = new HistoryManager<string>(2);
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("server3")));

        Assert.Equal(2, exception.MaxHistorySize);
        Assert.Equal(2, exception.CurrentCount);

        // Verify state is unchanged after exception
        Assert.Equal(2, manager.Count);
        Assert.True(manager.IsFull);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Add_ExactlyToLimit_WorksCorrectly(int limit)
    {
        var manager = new HistoryManager<string>(limit);

        // Add exactly to the limit
        for (int i = 0; i < limit; i++)
        {
            manager.Add(CreateTestSnapshot($"server{i}"));
        }

        Assert.Equal(limit, manager.Count);
        Assert.True(manager.IsFull);
        Assert.Equal(0, manager.GetRemainingCapacity());

        // The next add should throw
        Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("overflow")));
    }

    #endregion

    #region Clear Method Tests

    [Fact]
    public void Clear_EmptyManager_RemainsEmpty()
    {
        var manager = new HistoryManager<string>(3);

        manager.Clear();

        Assert.Equal(0, manager.Count);
        Assert.False(manager.HasSnapshots);
        Assert.False(manager.IsFull);
        Assert.Equal(3, manager.GetRemainingCapacity());
    }

    [Fact]
    public void Clear_NonEmptyManager_BecomesEmpty()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));

        manager.Clear();

        Assert.Equal(0, manager.Count);
        Assert.False(manager.HasSnapshots);
        Assert.False(manager.IsFull);
        Assert.Equal(3, manager.GetRemainingCapacity());
        Assert.Empty(manager.GetSnapshots());
    }

    [Fact]
    public void Clear_FullManager_BecomesEmpty()
    {
        var manager = new HistoryManager<string>(2);
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));
        Assert.True(manager.IsFull);

        manager.Clear();

        Assert.Equal(0, manager.Count);
        Assert.False(manager.IsFull);
        Assert.Equal(2, manager.GetRemainingCapacity());

        // Should be able to add again after clear
        manager.Add(CreateTestSnapshot("server3"));
        Assert.Equal(1, manager.Count);
    }

    #endregion

    #region GetSnapshots Method Tests

    [Fact]
    public void GetSnapshots_EmptyManager_ReturnsEmptyList()
    {
        var manager = new HistoryManager<string>(3);

        var snapshots = manager.GetSnapshots();

        Assert.NotNull(snapshots);
        Assert.Empty(snapshots);
        Assert.IsType<IReadOnlyList<ConfigurationSnapshot<string>>>(snapshots, exactMatch: false);
    }

    [Fact]
    public void GetSnapshots_WithSnapshots_ReturnsChronologicalOrder()
    {
        var manager = new HistoryManager<string>(5);
        var snapshot1 = CreateTestSnapshotWithTime("server1", DateTime.UtcNow.AddMinutes(-10));
        var snapshot2 = CreateTestSnapshotWithTime("server2", DateTime.UtcNow.AddMinutes(-5));
        var snapshot3 = CreateTestSnapshotWithTime("server3", DateTime.UtcNow);

        manager.Add(snapshot1);
        manager.Add(snapshot2);
        manager.Add(snapshot3);

        var snapshots = manager.GetSnapshots();

        Assert.Equal(3, snapshots.Count);
        Assert.Equal(snapshot1, snapshots[0]); // Oldest first
        Assert.Equal(snapshot2, snapshots[1]);
        Assert.Equal(snapshot3, snapshots[2]); // Newest last
    }

    [Fact]
    public void GetSnapshots_ReturnsReadOnlyList_CannotModify()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"));

        var snapshots = manager.GetSnapshots();

        Assert.IsType<IReadOnlyList<ConfigurationSnapshot<string>>>(snapshots, exactMatch: false);
        // Verify it's actually read-only by checking the runtime type
        Assert.False(snapshots is List<ConfigurationSnapshot<string>>);
    }

    #endregion

    #region GetSnapshotsReverse Method Tests

    [Fact]
    public void GetSnapshotsReverse_EmptyManager_ReturnsEmptyList()
    {
        var manager = new HistoryManager<string>(3);

        var snapshots = manager.GetSnapshotsReverse();

        Assert.NotNull(snapshots);
        Assert.Empty(snapshots);
        Assert.IsType<IReadOnlyList<ConfigurationSnapshot<string>>>(snapshots, exactMatch: false);
    }

    [Fact]
    public void GetSnapshotsReverse_WithSnapshots_ReturnsReverseChronologicalOrder()
    {
        var manager = new HistoryManager<string>(5);
        var snapshot1 = CreateTestSnapshotWithTime("server1", DateTime.UtcNow.AddMinutes(-10));
        var snapshot2 = CreateTestSnapshotWithTime("server2", DateTime.UtcNow.AddMinutes(-5));
        var snapshot3 = CreateTestSnapshotWithTime("server3", DateTime.UtcNow);

        manager.Add(snapshot1);
        manager.Add(snapshot2);
        manager.Add(snapshot3);

        var snapshots = manager.GetSnapshotsReverse();

        Assert.Equal(3, snapshots.Count);
        Assert.Equal(snapshot3, snapshots[0]); // Newest first
        Assert.Equal(snapshot2, snapshots[1]);
        Assert.Equal(snapshot1, snapshots[2]); // Oldest last
    }

    [Fact]
    public void GetSnapshotsReverse_ComparedToGetSnapshots_AreOpposite()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));
        manager.Add(CreateTestSnapshot("server3"));

        var forward = manager.GetSnapshots();
        var reverse = manager.GetSnapshotsReverse();

        Assert.Equal(forward.Count, reverse.Count);
        for (int i = 0; i < forward.Count; i++)
        {
            Assert.Equal(forward[i], reverse[reverse.Count - 1 - i]);
        }
    }

    #endregion

    #region TryGetLatest Method Tests

    [Fact]
    public void TryGetLatest_EmptyManager_ReturnsFalse()
    {
        var manager = new HistoryManager<string>(3);

        var result = manager.TryGetLatest(out var snapshot);

        Assert.False(result);
        Assert.Null(snapshot);
    }

    [Fact]
    public void TryGetLatest_SingleSnapshot_ReturnsTrue()
    {
        var manager = new HistoryManager<string>(3);
        var testSnapshot = CreateTestSnapshot("server1");
        manager.Add(testSnapshot);

        var result = manager.TryGetLatest(out var snapshot);

        Assert.True(result);
        Assert.NotNull(snapshot);
        Assert.Equal(testSnapshot, snapshot);
    }

    [Fact]
    public void TryGetLatest_MultipleSnapshots_ReturnsNewest()
    {
        var manager = new HistoryManager<string>(3);
        var snapshot1 = CreateTestSnapshotWithTime("server1", DateTime.UtcNow.AddMinutes(-10));
        var snapshot2 = CreateTestSnapshotWithTime("server2", DateTime.UtcNow.AddMinutes(-5));
        var snapshot3 = CreateTestSnapshotWithTime("server3", DateTime.UtcNow);

        manager.Add(snapshot1);
        manager.Add(snapshot2);
        manager.Add(snapshot3);

        var result = manager.TryGetLatest(out var latest);

        Assert.True(result);
        Assert.NotNull(latest);
        Assert.Equal(snapshot3, latest);
    }

    #endregion

    #region GetRemainingCapacity Method Tests

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 0)]
    [InlineData(10, 0)]
    public void GetRemainingCapacity_EmptyManager_ReturnsMaxSize(int maxSize, int expectedRemaining)
    {
        var manager = new HistoryManager<string>(maxSize);

        var remaining = manager.GetRemainingCapacity();

        Assert.Equal(maxSize - expectedRemaining, remaining);
    }

    [Theory]
    [InlineData(5, 1, 4)]
    [InlineData(5, 3, 2)]
    [InlineData(10, 7, 3)]
    public void GetRemainingCapacity_PartiallyFilled_ReturnsCorrectValue(int maxSize, int added, int expectedRemaining)
    {
        var manager = new HistoryManager<string>(maxSize);

        for (int i = 0; i < added; i++)
        {
            manager.Add(CreateTestSnapshot($"server{i}"));
        }

        var remaining = manager.GetRemainingCapacity();

        Assert.Equal(expectedRemaining, remaining);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void GetRemainingCapacity_FullManager_ReturnsZero(int maxSize)
    {
        var manager = new HistoryManager<string>(maxSize);

        for (int i = 0; i < maxSize; i++)
        {
            manager.Add(CreateTestSnapshot($"server{i}"));
        }

        var remaining = manager.GetRemainingCapacity();

        Assert.Equal(0, remaining);
    }

    #endregion

    #region Property Consistency Tests

    [Fact]
    public void Properties_ConsistencyAfterOperations_AlwaysValid()
    {
        var manager = new HistoryManager<string>(3);

        // Initial state
        VerifyConsistentState(manager);

        // After adding one
        manager.Add(CreateTestSnapshot("server1"));
        VerifyConsistentState(manager);

        // After adding to full
        manager.Add(CreateTestSnapshot("server2"));
        manager.Add(CreateTestSnapshot("server3"));
        VerifyConsistentState(manager);

        // After clearing
        manager.Clear();
        VerifyConsistentState(manager);

        // After adding again
        manager.Add(CreateTestSnapshot("server4"));
        VerifyConsistentState(manager);
    }

    private static void VerifyConsistentState(HistoryManager<string> manager)
    {
        // Count should match snapshots
        Assert.Equal(manager.Count, manager.GetSnapshots().Count);

        // HasSnapshots should match Count > 0
        Assert.Equal(manager.Count > 0, manager.HasSnapshots);

        // IsFull should match Count >= MaxSize
        Assert.Equal(manager.Count >= manager.MaxSize, manager.IsFull);

        // Remaining capacity should be correct
        Assert.Equal(manager.MaxSize - manager.Count, manager.GetRemainingCapacity());

        // Counts should be non-negative
        Assert.True(manager.Count >= 0);
        Assert.True(manager.MaxSize >= 1);
        Assert.True(manager.GetRemainingCapacity() >= 0);

        // TryGetLatest consistency
        var hasLatest = manager.TryGetLatest(out var latest);
        Assert.Equal(manager.HasSnapshots, hasLatest);
        if (hasLatest)
        {
            Assert.NotNull(latest);
        }
        else
        {
            Assert.Null(latest);
        }
    }

    #endregion

    #region Edge Cases and Boundary Conditions

    [Fact]
    public void HistoryManager_WithSizeOne_WorksCorrectly()
    {
        var manager = new HistoryManager<string>(1);

        // Initially empty
        Assert.Equal(0, manager.Count);
        Assert.False(manager.IsFull);

        // Add first snapshot
        manager.Add(CreateTestSnapshot("server1"));
        Assert.Equal(1, manager.Count);
        Assert.True(manager.IsFull);
        Assert.Equal(0, manager.GetRemainingCapacity());

        // Adding second should throw
        Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("server2")));

        // Clear and verify can add again
        manager.Clear();
        Assert.Equal(0, manager.Count);
        Assert.False(manager.IsFull);

        manager.Add(CreateTestSnapshot("server3"));
        Assert.Equal(1, manager.Count);
        Assert.True(manager.IsFull);
    }

    [Fact]
    public void HistoryManager_WithLargeCapacity_HandlesCorrectly()
    {
        const int largeCapacity = 10000;
        var manager = new HistoryManager<string>(largeCapacity);

        // Add many snapshots
        const int snapshotsToAdd = 5000;
        for (int i = 0; i < snapshotsToAdd; i++)
        {
            manager.Add(CreateTestSnapshot($"server{i}"));
        }

        Assert.Equal(snapshotsToAdd, manager.Count);
        Assert.False(manager.IsFull);
        Assert.Equal(largeCapacity - snapshotsToAdd, manager.GetRemainingCapacity());

        var snapshots = manager.GetSnapshots();
        Assert.Equal(snapshotsToAdd, snapshots.Count);
    }

    [Fact]
    public void HistoryManager_SnapshotOrdering_MaintainedCorrectly()
    {
        var manager = new HistoryManager<string>(10);
        var timestamps = new List<DateTime>();

        // Add snapshots with specific timestamps
        for (int i = 0; i < 5; i++)
        {
            var timestamp = DateTime.UtcNow.AddMinutes(i);
            timestamps.Add(timestamp);
            manager.Add(CreateTestSnapshotWithTime($"server{i}", timestamp));
        }

        var snapshots = manager.GetSnapshots();
        for (int i = 0; i < snapshots.Count; i++)
        {
            Assert.Equal(timestamps[i], snapshots[i].CreatedAt);
        }

        var reverseSnapshots = manager.GetSnapshotsReverse();
        for (int i = 0; i < reverseSnapshots.Count; i++)
        {
            Assert.Equal(timestamps[timestamps.Count - 1 - i], reverseSnapshots[i].CreatedAt);
        }
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task HistoryManager_ConcurrentReads_ThreadSafe()
    {
        var manager = new HistoryManager<string>(100);

        // Pre-populate with some snapshots
        for (int i = 0; i < 50; i++)
        {
            manager.Add(CreateTestSnapshot($"server{i}"));
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Multiple concurrent read operations
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var snapshots = manager.GetSnapshots();
                        var reverseSnapshots = manager.GetSnapshotsReverse();
                        _ = manager.TryGetLatest(out var _);
                        var count = manager.Count;
                        var remaining = manager.GetRemainingCapacity();

                        // Basic validation
                        Assert.NotNull(snapshots);
                        Assert.NotNull(reverseSnapshots);
                        Assert.True(count >= 0);
                        Assert.True(remaining >= 0);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        if (exceptions.Count > 0)
        {
            throw new AggregateException("Concurrent read operations failed", exceptions);
        }
    }

    [Fact]
    public async Task HistoryManager_ConcurrentWriteOperations_RequireSynchronization()
    {
        // Note: This test demonstrates that HistoryManager is NOT thread-safe for writes
        // This is expected behavior - the HashRing class provides the necessary synchronization
        var manager = new HistoryManager<string>(1000);
        var tasks = new List<Task>();
        var successfulAdds = 0;

        // Multiple concurrent add operations
        for (int i = 0; i < 100; i++)
        {
            int taskId = i; // Capture loop variable
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    manager.Add(CreateTestSnapshot($"server{taskId}"));
                    Interlocked.Increment(ref successfulAdds);
                }
                catch (HashRingHistoryLimitExceededException)
                {
                    // Expected - race conditions may occur without external synchronization
                }
                catch (ArgumentException)
                {
                    // Expected - race conditions may cause various argument exceptions
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // The number of successful adds may be less than 100 due to race conditions
        // This demonstrates why external synchronization is needed
        Assert.True(successfulAdds > 0);
        Assert.True(successfulAdds <= 100);
        Assert.True(manager.Count > 0);
        Assert.True(manager.Count <= 1000); // Within capacity limits
    }

    #endregion

    #region Integration with Different Data Types

    [Fact]
    public void HistoryManager_WithIntegerServers_WorksCorrectly()
    {
        var manager = new HistoryManager<int>(5);
        var snapshot = CreateTestSnapshot(42);

        manager.Add(snapshot);

        Assert.Equal(1, manager.Count);
        var snapshots = manager.GetSnapshots();
        Assert.Single(snapshots);
        Assert.Contains(42, snapshots[0].Servers);
    }

    [Fact]
    public void HistoryManager_WithGuidServers_WorksCorrectly()
    {
        var manager = new HistoryManager<Guid>(5);
        var serverId = Guid.NewGuid();
        var snapshot = CreateTestSnapshot(serverId);

        manager.Add(snapshot);

        Assert.Equal(1, manager.Count);
        var snapshots = manager.GetSnapshots();
        Assert.Single(snapshots);
        Assert.Contains(serverId, snapshots[0].Servers);
    }

    #endregion

    #region Helper Methods

    private static ConfigurationSnapshot<string> CreateTestSnapshotWithTime(string server, DateTime timestamp)
    {
        var servers = new[] { server };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new((uint)server.GetHashCode(), server)
        };
        return new ConfigurationSnapshot<string>(servers, virtualNodes, timestamp, DefaultHashAlgorithm);
    }

    private static ConfigurationSnapshot<string> CreateTestSnapshot(string server)
    {
        return CreateTestSnapshotWithTime(server, DateTime.UtcNow);
    }

    private static ConfigurationSnapshot<T> CreateTestSnapshot<T>(T server) where T : IEquatable<T>
    {
        var servers = new[] { server };
        var virtualNodes = new List<VirtualNode<T>>
        {
            new((uint)server.GetHashCode(), server)
        };
        return new ConfigurationSnapshot<T>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);
    }

    #endregion
}
