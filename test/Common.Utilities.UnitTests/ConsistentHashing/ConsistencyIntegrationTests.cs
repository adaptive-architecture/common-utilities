using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class ConsistencyIntegrationTests
{
    [Fact]
    public void HashRing_SameKeyMultipleCallsAfterAddingServers_AlwaysReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        const string testKey = "user123";

        // Act - Get server multiple times
        var results = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(ring.GetServer(testKey));
        }

        // Assert - All results should be identical
        var distinctResults = results.Distinct().ToList();
        Assert.Single(distinctResults);
        Assert.Contains(distinctResults[0], ring.Servers);
    }

    [Fact]
    public void HashRing_MultipleKeysWithSameServers_ConsistentMapping()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var testKeys = new[] { "user1", "user2", "user3", "user4", "user5" };

        // Act - Create mapping baseline
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Get mapping multiple times
        for (int attempt = 0; attempt < 10; attempt++)
        {
            var currentMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

            // Assert - Each attempt should produce identical mapping
            foreach (var key in testKeys)
            {
                Assert.Equal(initialMapping[key], currentMapping[key]);
            }
        }
    }

    [Fact]
    public void HashRing_AfterServerRemovalAndReAddition_MaintainsConsistencyForUnaffectedKeys()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 100).Select(i => $"user{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Remove and re-add server
        ring.Remove("server2");
        ring.CreateConfigurationSnapshot();
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Get new mapping
        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Assert - Most keys should map to the same servers (some redistribution is expected)
        var unchangedKeys = testKeys.Where(key => initialMapping[key] == finalMapping[key]).ToArray();

        // At least 60% of keys should remain unchanged (this is a practical expectation)
        Assert.True(unchangedKeys.Length >= testKeys.Length * 0.6,
            $"Expected at least 60% consistency, got {unchangedKeys.Length}/{testKeys.Length} ({unchangedKeys.Length * 100.0 / testKeys.Length:F1}%)");
    }

    [Fact]
    public void HashRing_WithDifferentVirtualNodeCounts_MaintainsConsistency()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1", 100);
        ring.Add("server2", 200);
        ring.Add("server3", 300);
        ring.CreateConfigurationSnapshot();

        const string testKey = "consistent_key";

        // Act - Get server multiple times
        var results = new HashSet<string>();
        for (int i = 0; i < 50; i++)
        {
            results.Add(ring.GetServer(testKey));
        }

        // Assert - Should always return the same server
        Assert.Single(results);
    }

    [Fact]
    public void HashRing_WithCustomHashAlgorithm_MaintainsConsistency()
    {
        // Arrange
        var sha1Ring = new HashRing<string>(new Sha1HashAlgorithm());
        var md5Ring = new HashRing<string>(new Md5HashAlgorithm());

        sha1Ring.Add("server1");
        sha1Ring.Add("server2");
        sha1Ring.Add("server3");
        sha1Ring.CreateConfigurationSnapshot();

        md5Ring.Add("server1");
        md5Ring.Add("server2");
        md5Ring.Add("server3");
        md5Ring.CreateConfigurationSnapshot();

        const string testKey = "algorithm_test";

        // Act - Get server multiple times for each algorithm
        var sha1Results = new HashSet<string>();
        var md5Results = new HashSet<string>();

        for (int i = 0; i < 20; i++)
        {
            sha1Results.Add(sha1Ring.GetServer(testKey));
            md5Results.Add(md5Ring.GetServer(testKey));
        }

        // Assert - Each algorithm should be consistent with itself
        Assert.Single(sha1Results);
        Assert.Single(md5Results);

        // Different algorithms may produce different results (this is expected)
        Assert.Contains(sha1Results.Single(), sha1Ring.Servers);
        Assert.Contains(md5Results.Single(), md5Ring.Servers);
    }

    [Fact]
    public void HashRing_EmptyKeyBytes_HandlesConsistently()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        var emptyKey = Array.Empty<byte>();

        // Act - Get server multiple times with empty key
        var results = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            results.Add(ring.GetServer(emptyKey));
        }

        // Assert - Should always return the same server even for empty key
        Assert.Single(results);
        Assert.Contains(results.Single(), ring.Servers);
    }

    [Fact]
    public void HashRing_LargeKeyBytes_HandlesConsistently()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        var largeKey = new byte[10000];
        new Random(42).NextBytes(largeKey); // Deterministic random data

        // Act - Get server multiple times with large key
        var results = new HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            results.Add(ring.GetServer(largeKey));
        }

        // Assert - Should always return the same server
        Assert.Single(results);
        Assert.Contains(results.Single(), ring.Servers);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void HashRing_WithVaryingVirtualNodeCounts_MaintainsKeyConsistency(int virtualNodes)
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1", virtualNodes);
        ring.Add("server2", virtualNodes);
        ring.CreateConfigurationSnapshot();

        var testKeys = new[] { "key1", "key2", "key3", "key4", "key5" };

        // Act - Create initial mapping
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Verify consistency over multiple attempts
        for (int attempt = 0; attempt < 10; attempt++)
        {
            foreach (var key in testKeys)
            {
                var server = ring.GetServer(key);
                Assert.Equal(initialMapping[key], server);
            }
        }
    }

    [Fact]
    public void HashRing_ServersWithSameIdentity_HandledCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server1"); // Adding the same server again should replace
        ring.CreateConfigurationSnapshot();

        const string testKey = "duplicate_test";

        // Act
        var server = ring.GetServer(testKey);

        // Assert
        Assert.Equal("server1", server);
        Assert.Single(ring.Servers);
        Assert.Contains("server1", ring.Servers);
    }
}
