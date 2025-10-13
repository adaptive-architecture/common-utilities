using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

/// <summary>
/// End-to-end integration tests for history limit behavior (AS-6)
/// </summary>
public sealed class HashRingHistoryLimitIntegrationTests
{
    [Fact]
    public void FillHistory_WithFIFO_OldestSnapshotsRemoved()
    {
        // Arrange
        var options = new HashRingOptions { MaxHistorySize = 3, HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest };
        var ring = new HashRing<string>(options);

        // Act - Create 5 snapshots (more than MaxHistorySize)
        ring.Add("server-A");
        ring.CreateConfigurationSnapshot(); // Snapshot 1

        ring.Add("server-B");
        ring.CreateConfigurationSnapshot(); // Snapshot 2

        ring.Add("server-C");
        ring.CreateConfigurationSnapshot(); // Snapshot 3

        ring.Add("server-D");
        ring.CreateConfigurationSnapshot(); // Snapshot 4 (removes snapshot 1)

        ring.Add("server-E");
        ring.CreateConfigurationSnapshot(); // Snapshot 5 (removes snapshot 2)

        // Assert - Only last 3 snapshots should remain
        Assert.Equal(3, ring.HistoryCount);
        Assert.Equal(3, ring.MaxHistorySize);

        // Verify we can get servers (snapshots exist)
        var key = new byte[] { 1, 2, 3, 4 };
        var server = ring.GetServer(key);
        Assert.NotNull(server);
    }

    [Fact]
    public void FillHistory_WithErrorMode_ThrowsException()
    {
        // Arrange
        var options = new HashRingOptions
        {
            MaxHistorySize = 3,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var ring = new HashRing<string>(options);

        // Act - Fill history to limit
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        // Assert - History is full
        Assert.Equal(3, ring.HistoryCount);

        // Act & Assert - Attempt to exceed limit should throw
        ring.Add("server4");
        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            ring.CreateConfigurationSnapshot());

        Assert.Equal(3, exception.MaxHistorySize);
        Assert.Equal(3, exception.CurrentCount);
    }

    [Fact]
    public void ClearHistory_AfterLimit_AllowsNewSnapshots()
    {
        // Arrange
        var options = new HashRingOptions
        {
            MaxHistorySize = 3,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var ring = new HashRing<string>(options);

        // Fill history to limit
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        // Act - Clear history
        ring.ClearHistory();

        // Assert - Can now add new snapshots
        Assert.Equal(0, ring.HistoryCount);

        ring.Add("server4");
        ring.CreateConfigurationSnapshot(); // Should succeed

        Assert.Equal(1, ring.HistoryCount);
    }

    [Fact]
    public void FIFORemoval_MaintainsCorrectSnapshotOrder()
    {
        // Arrange
        var options = new HashRingOptions { MaxHistorySize = 2, HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest };
        var ring = new HashRing<string>(options);

        // Act - Create snapshots with distinct server sets
        ring.Add("A");
        ring.CreateConfigurationSnapshot(); // Snapshot 1: {A}

        ring.Add("B");
        ring.CreateConfigurationSnapshot(); // Snapshot 2: {A, B}

        // Before adding 3rd snapshot, get possible servers
        var serversBeforeThird = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            serversBeforeThird.Add(ring.GetServer(testKey));
        }

        // Should have A and B available (from 2 snapshots)
        Assert.Contains("A", serversBeforeThird);
        Assert.Contains("B", serversBeforeThird);

        // Add 3rd snapshot (should remove snapshot 1)
        ring.Add("C");
        ring.CreateConfigurationSnapshot(); // Snapshot 3: {A, B, C} (removes snapshot 1)

        // Now get possible servers
        var serversAfterThird = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            serversAfterThird.Add(ring.GetServer(testKey));
        }

        // Assert - Should have A, B, C from remaining 2 snapshots
        Assert.Equal(2, ring.HistoryCount);
        Assert.Contains("A", serversAfterThird);
        Assert.Contains("B", serversAfterThird);
        Assert.Contains("C", serversAfterThird);
    }

    [Fact]
    public void DefaultBehavior_IsFIFO()
    {
        // Arrange - Use default options (should default to FIFO)
        var ring = new HashRing<string>();
        Assert.Equal(3, ring.MaxHistorySize); // Default max size

        // Act - Fill beyond default limit
        for (int i = 0; i < 5; i++)
        {
            ring.Add($"server{i}");
            ring.CreateConfigurationSnapshot();
        }

        // Assert - Should not throw, should have removed oldest automatically
        Assert.Equal(3, ring.HistoryCount);

        // Verify we can still get servers
        var key = new byte[] { 1, 2, 3, 4 };
        var server = ring.GetServer(key);
        Assert.NotNull(server);
    }

    [Fact]
    public void HistoryLimit_WithOptionsConstructor_WorksCorrectly()
    {
        // Arrange
        var options = new HashRingOptions
        {
            MaxHistorySize = 5,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest,
            DefaultVirtualNodes = 100
        };
        var ring = new HashRing<string>(options);

        // Act - Create 7 snapshots
        for (int i = 0; i < 7; i++)
        {
            ring.Add($"server{i}");
            ring.CreateConfigurationSnapshot();
        }

        // Assert
        Assert.Equal(5, ring.HistoryCount); // Only last 5 remain
        Assert.Equal(5, ring.MaxHistorySize);
        Assert.Equal(700, ring.VirtualNodeCount); // 7 servers * 100 virtual nodes
    }

    [Fact]
    public void ErrorMode_PreservesHistory_WhenThrowingException()
    {
        // Arrange
        var options = new HashRingOptions
        {
            MaxHistorySize = 2,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var ring = new HashRing<string>(options);

        ring.Add("server1");
        ring.CreateConfigurationSnapshot();
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Act - Attempt to exceed limit
        ring.Add("server3");
        try
        {
            ring.CreateConfigurationSnapshot();
        }
        catch (HashRingHistoryLimitExceededException)
        {
            // Expected
        }

        // Assert - History should still have 2 snapshots (not modified)
        Assert.Equal(2, ring.HistoryCount);

        // Verify we can still get servers from existing snapshots
        var key = new byte[] { 1, 2, 3, 4 };
        var server = ring.GetServer(key);
        Assert.NotNull(server);
        Assert.Contains(server, new[] { "server1", "server2" });
    }

    [Fact]
    public void FIFORemoval_WithSingleCapacity_AlwaysKeepsLatest()
    {
        // Arrange
        var options = new HashRingOptions
        {
            MaxHistorySize = 1,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
        };
        var ring = new HashRing<string>(options);

        // Act - Create 3 snapshots
        ring.Add("A");
        ring.CreateConfigurationSnapshot();

        ring.Add("B");
        ring.CreateConfigurationSnapshot(); // Should remove previous

        ring.Add("C");
        ring.CreateConfigurationSnapshot(); // Should remove previous

        // Assert
        Assert.Equal(1, ring.HistoryCount);

        // Only latest snapshot should be available (C)
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var key = new byte[] { (byte)i, 2, 3, 4 };
            results.Add(ring.GetServer(key));
        }

        // Should only have servers from latest snapshot (A, B, C all added)
        Assert.Contains("C", results);
    }
}
