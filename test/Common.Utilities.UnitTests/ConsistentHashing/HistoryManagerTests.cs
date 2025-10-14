namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System;
using System.Collections.Generic;
using System.Linq;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class HistoryManagerTests
{
    private static readonly IHashAlgorithm DefaultHashAlgorithm = new Sha1HashAlgorithm();
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_WithValidMaxSize_SetsMaxSizeCorrectly(int maxSize)
    {
        var manager = new HistoryManager<string>(maxSize);

        Assert.Equal(maxSize, manager.MaxSize);
        Assert.Equal(0, manager.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidMaxSize_ThrowsArgumentOutOfRangeException(int invalidMaxSize)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryManager<string>(invalidMaxSize));

        Assert.Equal("maxSize", exception.ParamName);
    }

    [Fact]
    public void Add_WithFirstSnapshot_AddsSuccessfully()
    {
        var manager = new HistoryManager<string>(3);
        var snapshot = CreateTestSnapshot("server1");

        manager.Add(snapshot, HistoryLimitBehavior.ThrowError);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Add_WithNullSnapshot_ThrowsArgumentNullException()
    {
        var manager = new HistoryManager<string>(3);
        ConfigurationSnapshot<string> snapshot = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => manager.Add(snapshot, HistoryLimitBehavior.ThrowError));

        Assert.Equal("snapshot", exception.ParamName);
    }

    [Fact]
    public void Add_WhenAtMaxCapacity_ThrowsHashRingHistoryLimitExceededException()
    {
        var manager = new HistoryManager<string>(2);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);
        manager.Add(CreateTestSnapshot("server2"), HistoryLimitBehavior.ThrowError);

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("server3"), HistoryLimitBehavior.ThrowError));

        Assert.Equal(2, exception.MaxHistorySize);
        Assert.Equal(2, exception.CurrentCount);
    }

    [Fact]
    public void Add_MultipleSnapshots_MaintainsInsertionOrder()
    {
        var manager = new HistoryManager<string>(5);
        var snapshot1 = CreateTestSnapshot("server1");
        var snapshot2 = CreateTestSnapshot("server2");
        var snapshot3 = CreateTestSnapshot("server3");

        manager.Add(snapshot1, HistoryLimitBehavior.ThrowError);
        manager.Add(snapshot2, HistoryLimitBehavior.ThrowError);
        manager.Add(snapshot3, HistoryLimitBehavior.ThrowError);

        var snapshots = manager.GetSnapshots().ToArray();
        Assert.Equal(3, snapshots.Length);
        Assert.Same(snapshot1, snapshots[0]);
        Assert.Same(snapshot2, snapshots[1]);
        Assert.Same(snapshot3, snapshots[2]);
    }

    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);
        manager.Add(CreateTestSnapshot("server2"), HistoryLimitBehavior.ThrowError);

        manager.Clear();

        Assert.Equal(0, manager.Count);
        Assert.Empty(manager.GetSnapshots());
    }

    [Fact]
    public void Clear_WhenEmpty_DoesNotThrow()
    {
        var manager = new HistoryManager<string>(3);

        manager.Clear();

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void GetSnapshots_ReturnsReadOnlyCollection()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);

        var snapshots = manager.GetSnapshots();

        Assert.IsType<IReadOnlyList<ConfigurationSnapshot<string>>>(snapshots, exactMatch: false);
    }

    [Fact]
    public void GetSnapshots_WhenEmpty_ReturnsEmptyCollection()
    {
        var manager = new HistoryManager<string>(3);

        var snapshots = manager.GetSnapshots();

        Assert.Empty(snapshots);
    }

    [Fact]
    public void IsFull_WhenAtCapacity_ReturnsTrue()
    {
        var manager = new HistoryManager<string>(1);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);

        Assert.True(manager.IsFull);
    }

    [Fact]
    public void IsFull_WhenBelowCapacity_ReturnsFalse()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);

        Assert.False(manager.IsFull);
    }

    [Fact]
    public void IsFull_WhenEmpty_ReturnsFalse()
    {
        var manager = new HistoryManager<string>(3);

        Assert.False(manager.IsFull);
    }

    [Fact]
    public void HasSnapshots_WhenHasSnapshots_ReturnsTrue()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);

        Assert.True(manager.HasSnapshots);
    }

    [Fact]
    public void HasSnapshots_WhenEmpty_ReturnsFalse()
    {
        var manager = new HistoryManager<string>(3);

        Assert.False(manager.HasSnapshots);
    }

    [Fact]
    public void Can_Add_After_Clearing()
    {
        var manager = new HistoryManager<string>(2);
        manager.Add(CreateTestSnapshot("server1"), HistoryLimitBehavior.ThrowError);
        manager.Add(CreateTestSnapshot("server2"), HistoryLimitBehavior.ThrowError);

        manager.Clear();

        manager.Add(CreateTestSnapshot("server3"), HistoryLimitBehavior.ThrowError);

        Assert.Equal(1, manager.Count);
        Assert.Same("server3", manager.GetSnapshots()[0].Servers[0]);
    }

    #region FIFO Removal Behavior Tests (T005)

    [Fact]
    public void Add_WhenFull_WithRemoveOldestBehavior_RemovesOldestSnapshot()
    {
        // Arrange
        var manager = new HistoryManager<string>(2);
        var snapshot1 = CreateTestSnapshot("server1");
        var snapshot2 = CreateTestSnapshot("server2");
        var snapshot3 = CreateTestSnapshot("server3");

        manager.Add(snapshot1, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot2, HistoryLimitBehavior.RemoveOldest);

        // Act - Adding 3rd snapshot should remove the oldest (snapshot1)
        manager.Add(snapshot3, HistoryLimitBehavior.RemoveOldest);

        // Assert
        Assert.Equal(2, manager.Count);
        var snapshots = manager.GetSnapshots().ToArray();
        Assert.Same(snapshot2, snapshots[0]); // Oldest remaining
        Assert.Same(snapshot3, snapshots[1]); // Newest
        Assert.DoesNotContain(snapshot1, snapshots); // First snapshot removed
    }

    [Fact]
    public void Add_WhenNotFull_WithRemoveOldestBehavior_AddsNormally()
    {
        // Arrange
        var manager = new HistoryManager<string>(3);
        var snapshot1 = CreateTestSnapshot("server1");
        var snapshot2 = CreateTestSnapshot("server2");

        // Act
        manager.Add(snapshot1, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot2, HistoryLimitBehavior.RemoveOldest);

        // Assert
        Assert.Equal(2, manager.Count);
        Assert.False(manager.IsFull);
        var snapshots = manager.GetSnapshots().ToArray();
        Assert.Same(snapshot1, snapshots[0]);
        Assert.Same(snapshot2, snapshots[1]);
    }

    [Fact]
    public void Add_FIFORemoval_MaintainsCorrectOrder()
    {
        // Arrange
        var manager = new HistoryManager<string>(3);
        var snapshot1 = CreateTestSnapshot("server1");
        var snapshot2 = CreateTestSnapshot("server2");
        var snapshot3 = CreateTestSnapshot("server3");
        var snapshot4 = CreateTestSnapshot("server4");
        var snapshot5 = CreateTestSnapshot("server5");

        // Act - Fill to capacity, then add more with FIFO
        manager.Add(snapshot1, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot2, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot3, HistoryLimitBehavior.RemoveOldest);
        // Now add 2 more - should remove snapshot1 and snapshot2
        manager.Add(snapshot4, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot5, HistoryLimitBehavior.RemoveOldest);

        // Assert
        Assert.Equal(3, manager.Count);
        var snapshots = manager.GetSnapshots().ToArray();
        Assert.Same(snapshot3, snapshots[0]); // Oldest remaining
        Assert.Same(snapshot4, snapshots[1]);
        Assert.Same(snapshot5, snapshots[2]); // Newest
    }

    [Fact]
    public void Add_RemoveOldest_WithSingleCapacity_AlwaysKeepsLatest()
    {
        // Arrange
        var manager = new HistoryManager<string>(1);
        var snapshot1 = CreateTestSnapshot("server1");
        var snapshot2 = CreateTestSnapshot("server2");
        var snapshot3 = CreateTestSnapshot("server3");

        // Act
        manager.Add(snapshot1, HistoryLimitBehavior.RemoveOldest);
        manager.Add(snapshot2, HistoryLimitBehavior.RemoveOldest); // Removes snapshot1
        manager.Add(snapshot3, HistoryLimitBehavior.RemoveOldest); // Removes snapshot2

        // Assert
        Assert.Equal(1, manager.Count);
        var snapshots = manager.GetSnapshots().ToArray();
        Assert.Same(snapshot3, snapshots[0]); // Only latest remains
    }

    #endregion

    private static ConfigurationSnapshot<string> CreateTestSnapshot(string server)
    {
        var servers = new[] { server };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new((uint)server.GetHashCode(), server)
        };
        return new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);
    }
}
