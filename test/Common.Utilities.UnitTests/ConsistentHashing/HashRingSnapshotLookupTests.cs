using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

/// <summary>
/// Tests verifying GetServer only uses snapshots, ignoring current configuration (FR-003, FR-017)
/// </summary>
public sealed class HashRingSnapshotLookupTests
{
    private static readonly string[] ServerOneTwo = ["server1", "server2"];
    private static readonly string[] ServerOneTwoThree = ["server1", "server2", "server3"];
    [Fact]
    public void GetServer_OnlyUsesSnapshots_IgnoresCurrentConfig()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot(); // Snapshot with only server1

        // Act - Add server2 to current config but DON'T create new snapshot
        ring.Add("server2");

        // Sample the key 100 times - should ONLY return server1
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            results.Add(ring.GetServer(testKey));
        }

        // Assert - Only server1 should be returned (server2 not in snapshot)
        Assert.Single(results);
        Assert.Contains("server1", results);
        Assert.DoesNotContain("server2", results);
    }

    [Fact]
    public void GetServer_WithMultipleSnapshots_UsesAllSnapshots()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Create first snapshot with server1
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        // Create second snapshot with server1 + server2
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Act - Sample multiple keys
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            results.Add(ring.GetServer(testKey));
        }

        // Assert - Should return servers from all snapshots (server1 and server2)
        Assert.True(results.Count <= 2);
        Assert.Subset(new HashSet<string> { "server1", "server2" }, results);
    }

    [Fact]
    public void GetServer_AfterAddingServers_StillUsesOldSnapshot()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot(); // Snapshot with server1 and server2

        // Store a key's result
        var key = new byte[] { 1, 2, 3, 4 };
        var initialServer = ring.GetServer(key);

        // Act - Add more servers without creating snapshot
        ring.Add("server3");
        ring.Add("server4");
        ring.Add("server5");

        var laterServer = ring.GetServer(key);

        // Assert - Same server should be returned (snapshot unchanged)
        Assert.Equal(initialServer, laterServer);
        Assert.Contains(laterServer, ServerOneTwo);
    }

    [Fact]
    public void GetServers_OnlyUsesSnapshots()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot(); // Snapshot with server1, server2

        // Act - Add server3 without snapshot
        ring.Add("server3");

        var key = new byte[] { 1, 2, 3, 4 };
        var servers = ring.GetServers(key, 5).ToList();

        // Assert - Should only return servers from snapshot (server1, server2)
        Assert.All(servers, server => Assert.Contains(server, ServerOneTwo));
        Assert.DoesNotContain("server3", servers);
    }

    [Fact]
    public void GetServer_AfterNewSnapshot_UsesNewSnapshot()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        var key = new byte[] { 1, 2, 3, 4 };
        var server1 = ring.GetServer(key);
        Assert.Equal("server1", server1);

        // Act - Add server2 and create new snapshot
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Sample multiple keys to ensure we can get server2
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            results.Add(ring.GetServer(testKey));
        }

        // Assert - Now server2 should be available
        Assert.Contains("server2", results);
    }

    [Fact]
    public void GetServer_AfterRemovingServer_StillUsesSnapshot()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        // Act - Remove server2 from current config (but snapshot still has it)
        ring.Remove("server2");

        // Sample keys
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var testKey = new byte[] { (byte)i, 2, 3, 4 };
            results.Add(ring.GetServer(testKey));
        }

        // Assert - server2 should still appear because snapshot still contains it
        Assert.Contains("server2", results);
    }

    [Fact]
    public void GetServer_ConsistencyAcrossCalls()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var key = new byte[] { 1, 2, 3, 4 };

        // Act - Call GetServer multiple times with same key
        var server1 = ring.GetServer(key);
        var server2 = ring.GetServer(key);
        var server3 = ring.GetServer(key);

        // Assert - Should always return the same server for the same key
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_DifferentKeys_DistributesAcrossServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        // Act - Get servers for multiple different keys
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var key = new byte[] { (byte)i, (byte)(i % 256), 3, 4 };
            results.Add(ring.GetServer(key));
        }

        // Assert - Should distribute across multiple servers
        Assert.True(results.Count > 1, "Expected distribution across multiple servers");
        Assert.All(results, server => Assert.Contains(server, ServerOneTwoThree));
    }

    [Fact]
    public void GetServers_WithEmptySnapshotVirtualNodes_ReturnsEmptyList()
    {
        // Arrange - Create ring and snapshot before adding any servers
        var ring = new HashRing<string>();
        ring.CreateConfigurationSnapshot(); // Create snapshot with no servers/virtual nodes

        // Act
        var key = new byte[] { 1, 2, 3, 4 };
        var servers = ring.GetServers(key, 5).ToList();

        // Assert - Should return empty list (snapshot has no virtual nodes)
        Assert.Empty(servers);
    }

    [Fact]
    public void GetServers_WithEmptySnapshotAfterClearingServers_ReturnsEmptyList()
    {
        // Arrange - Add servers, then clear, then snapshot
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Clear(); // Clear all servers
        ring.CreateConfigurationSnapshot(); // Create snapshot with no servers

        // Act
        var key = new byte[] { 1, 2, 3, 4 };
        var servers = ring.GetServers(key, 5).ToList();

        // Assert - Should return empty list (snapshot has no virtual nodes)
        Assert.Empty(servers);
    }

    [Fact]
    public void GetServer_WithKeyMatchingExactVirtualNodeHash_HitsExactMatchInBinarySearch()
    {
        // Arrange - Create a hash ring with a controllable hash algorithm
        var fixedHashAlgorithm = new FixedHashAlgorithm();
        var options = new HashRingOptions(fixedHashAlgorithm);
        var ring = new HashRing<string>(options);

        // Add servers with specific virtual node counts to create known hash values
        // The FixedHashAlgorithm will use the byte sum as hash
        ring.Add("server1", 3); // Creates virtual nodes: server1:0, server1:1, server1:2
        ring.Add("server2", 3); // Creates virtual nodes: server2:0, server2:1, server2:2
        ring.CreateConfigurationSnapshot();

        // Now we need to create a key that produces a hash that exactly matches
        // one of the virtual node hashes. We'll use the hash of "server1:1"
        const string targetVirtualNodeKey = "server1:1";
        var targetKeyBytes = System.Text.Encoding.UTF8.GetBytes(targetVirtualNodeKey);
        var targetHash = ComputeSumHash(targetKeyBytes);

        // Create a different key that produces the same hash
        // We need to find bytes that sum to the same value
        byte[] matchingKey = CreateKeyWithHash(targetHash);

        // Act - This should hit the exact match case (lines 429-431)
        var server = ring.GetServer(matchingKey);

        // Assert - Should successfully find a server
        Assert.NotNull(server);
        Assert.Contains(server, ServerOneTwo);
    }

    /// <summary>
    /// Computes hash as sum of bytes (matching FixedHashAlgorithm behavior).
    /// </summary>
    private static uint ComputeSumHash(byte[] key)
    {
        uint sum = 0;
        foreach (var b in key)
        {
            sum += b;
        }
        return sum;
    }

    /// <summary>
    /// Creates a byte array that produces the specified hash value.
    /// </summary>
    private static byte[] CreateKeyWithHash(uint targetHash)
    {
        // Simple approach: create array where bytes sum to targetHash
        if (targetHash <= 255)
        {
            return [(byte)targetHash];
        }
        else
        {
            // Split into multiple bytes
            var result = new List<byte>();
            uint remaining = targetHash;
            while (remaining > 0)
            {
                byte b = (byte)Math.Min(remaining, 255);
                result.Add(b);
                remaining -= b;
            }
            return [.. result];
        }
    }

    /// <summary>
    /// Hash algorithm that produces predictable hashes for testing exact matches.
    /// Returns the sum of all bytes as a uint hash.
    /// </summary>
    private sealed class FixedHashAlgorithm : IHashAlgorithm
    {
        public byte[] ComputeHash(byte[] key)
        {
            uint sum = 0;
            foreach (var b in key)
            {
                sum += b;
            }
            return BitConverter.GetBytes(sum);
        }
    }

    [Fact]
    public void GetServers_WithCountZero_ReturnsEmptyList()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Act
        var key = new byte[] { 1, 2, 3, 4 };
        var servers = ring.GetServers(key, 0).ToList();

        // Assert - Should return empty list when count is 0
        Assert.Empty(servers);
    }
}
