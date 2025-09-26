namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Collections.Generic;
using System.Linq;
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

        manager.Add(snapshot);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Add_WithNullSnapshot_ThrowsArgumentNullException()
    {
        var manager = new HistoryManager<string>(3);
        ConfigurationSnapshot<string> snapshot = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => manager.Add(snapshot));

        Assert.Equal("snapshot", exception.ParamName);
    }

    [Fact]
    public void Add_WhenAtMaxCapacity_ThrowsHashRingHistoryLimitExceededException()
    {
        var manager = new HistoryManager<string>(2);
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            manager.Add(CreateTestSnapshot("server3")));

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

        manager.Add(snapshot1);
        manager.Add(snapshot2);
        manager.Add(snapshot3);

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
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));

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
        manager.Add(CreateTestSnapshot("server1"));

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
        manager.Add(CreateTestSnapshot("server1"));

        Assert.True(manager.IsFull);
    }

    [Fact]
    public void IsFull_WhenBelowCapacity_ReturnsFalse()
    {
        var manager = new HistoryManager<string>(3);
        manager.Add(CreateTestSnapshot("server1"));

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
        manager.Add(CreateTestSnapshot("server1"));

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
        manager.Add(CreateTestSnapshot("server1"));
        manager.Add(CreateTestSnapshot("server2"));

        manager.Clear();

        manager.Add(CreateTestSnapshot("server3"));

        Assert.Equal(1, manager.Count);
        Assert.Same("server3", manager.GetSnapshots()[0].Servers[0]);
    }

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
