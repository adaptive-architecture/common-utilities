using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

/// <summary>
/// Tests for snapshot-only GetServer behavior (FR-003, FR-007)
/// Verifies that GetServer throws when no snapshots exist
/// </summary>
public sealed class HashRingSnapshotOnlyTests
{
    private static readonly string[] ServerOneTwo = ["server1", "server2"];
    private static readonly string[] ServerOneTwoThree = ["server1", "server2", "server3"];
    [Fact]
    public void GetServer_WithNoSnapshots_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        // Note: NOT creating a snapshot

        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));
        Assert.Contains("snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetServer_WithServersButNoSnapshots_ThrowsClearError()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        // Note: Servers added but no snapshot created

        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));

        // Verify error message mentions snapshots and suggests CreateConfigurationSnapshot
        Assert.Contains("snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CreateConfigurationSnapshot", exception.Message);
    }

    [Fact]
    public void TryGetServer_WithNoSnapshots_ReturnsFalse()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        // Note: NOT creating a snapshot

        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void GetServer_AfterClearHistory_ThrowsException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot(); // Create snapshot first

        // Act - Clear the history
        ring.ClearHistory();

        var key = new byte[] { 1, 2, 3, 4 };

        // Assert - Should throw because no snapshots exist anymore
        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));
        Assert.Contains("snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryGetServer_AfterClearHistory_ReturnsFalse()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();
        ring.ClearHistory();

        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void GetServer_EmptyRingWithNoSnapshots_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();
        // No servers, no snapshots

        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));
        Assert.Contains("snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetServer_WithSnapshot_Succeeds()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot(); // Create snapshot

        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        var server = ring.GetServer(key);

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ServerOneTwo);
    }

    [Fact]
    public void TryGetServer_WithSnapshot_ReturnsTrue()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Equal("server1", server);
    }

    [Fact]
    public void GetServers_WithNoSnapshots_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        // No snapshot created

        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServers(key, 2).ToList());
        Assert.Contains("snapshot", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetServers_WithSnapshot_Succeeds()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        var servers = ring.GetServers(key, 2).ToList();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ServerOneTwoThree));
    }
}
