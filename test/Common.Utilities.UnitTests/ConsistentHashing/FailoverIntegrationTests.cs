using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class FailoverIntegrationTests
{
    private static readonly string[] ExpectedServers = ["server2", "server4"];
    private static readonly string[] ExpectedFailoverServers = ["small", "large"];
    [Fact]
    public void HashRing_RemoveServer_RedistributesKeysToRemainingServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Find keys that were initially mapped to server2
        var server2Keys = testKeys.Where(key => initialMapping[key] == "server2").ToArray();

        // Act - Remove server2
        bool removed = ring.Remove("server2");

        // Assert removal was successful
        Assert.True(removed);
        Assert.DoesNotContain("server2", ring.Servers);
        Assert.Equal(3, ring.Servers.Count);

        // Get new mapping after removal
        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Assert - All keys previously on server2 are now redistributed
        foreach (var key in server2Keys)
        {
            Assert.NotEqual("server2", finalMapping[key]);
            Assert.Contains(finalMapping[key], ring.Servers);
        }

        // Assert - Keys not originally on server2 should remain stable
        foreach (var key in testKeys.Where(key => initialMapping[key] != "server2").ToArray())
        {
            Assert.Equal(initialMapping[key], finalMapping[key]);
        }

        // Assert - All keys are still mapped to valid servers
        foreach (var key in testKeys)
        {
            Assert.Contains(finalMapping[key], ring.Servers);
        }
    }

    [Fact]
    public void HashRing_RemoveNonExistentServer_DoesNotAffectExistingMappings()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        var testKeys = new[] { "key1", "key2", "key3", "key4", "key5" };
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Try to remove non-existent server
        bool removed = ring.Remove("server99");

        // Assert - Removal should return false
        Assert.False(removed);

        // Assert - Existing mappings should be unchanged
        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        foreach (var key in testKeys)
        {
            Assert.Equal(initialMapping[key], finalMapping[key]);
        }

        // Assert - Server count unchanged
        Assert.Equal(3, ring.Servers.Count);
    }

    [Fact]
    public void HashRing_RemoveLastServer_MakesRingEmpty()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        var testKey = new byte[] { 1, 2, 3 };

        // Verify server exists and can serve requests
        Assert.Single(ring.Servers);
        Assert.False(ring.IsEmpty);
        string server = ring.GetServer(testKey);
        Assert.Equal("server1", server);

        // Act - Remove the only server
        bool removed = ring.Remove("server1");

        // Assert - Ring should be empty
        Assert.True(removed);
        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);

        // Assert - Getting server from empty ring should throw
        Assert.Throws<InvalidOperationException>(() => ring.GetServer(testKey));
    }

    [Fact]
    public void HashRing_CascadingFailures_RedistributesCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        for (int i = 1; i <= 5; i++)
        {
            ring.Add($"server{i}");
        }

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();

        // Track distribution at each failure
        var distributionHistory = new List<Dictionary<string, int>>();

        // Act - Remove servers one by one (simulating cascading failures)
        foreach (var serverToRemove in new[] { "server1", "server3", "server5" })
        {
            // Get distribution before removal
            var beforeMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
            var beforeDistribution = new Dictionary<string, int>();
            CountServerDistribution(beforeMapping, beforeDistribution);

            // Remove server
            ring.Remove(serverToRemove);

            // Get distribution after removal
            var afterMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
            var afterDistribution = new Dictionary<string, int>();
            CountServerDistribution(afterMapping, afterDistribution);

            distributionHistory.Add(afterDistribution);

            // Assert - All keys still mapped to valid servers
            foreach (var key in testKeys)
            {
                Assert.Contains(afterMapping[key], ring.Servers);
            }

            // Assert - Removed server no longer receives any keys
            Assert.DoesNotContain(serverToRemove, ring.Servers);
            Assert.All(afterMapping.Values, server => Assert.NotEqual(serverToRemove, server));
        }

        // Assert - Final state has only server2 and server4
        Assert.Equal(2, ring.Servers.Count);
        Assert.Contains("server2", ring.Servers);
        Assert.Contains("server4", ring.Servers);

        // Assert - Both remaining servers should handle all keys
        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        Assert.All(finalMapping.Values, server => Assert.Contains(server, ExpectedServers));
    }

    [Fact]
    public void HashRing_FailoverWithDifferentVirtualNodes_RedistributesProportionally()
    {
        // Arrange - Create ring with servers having different capacities
        var ring = new HashRing<string>();
        ring.Add("small", 50);   // Small server
        ring.Add("medium", 100); // Medium server
        ring.Add("large", 200);  // Large server

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Find keys originally on medium server
        var mediumKeys = testKeys.Where(key => initialMapping[key] == "medium").ToArray();

        // Act - Remove medium server
        ring.Remove("medium");

        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        var finalDistribution = new Dictionary<string, int>();
        CountServerDistribution(finalMapping, finalDistribution);

        // Assert - Keys from medium server should be redistributed to small and large
        foreach (var key in mediumKeys)
        {
            Assert.Contains(finalMapping[key], ExpectedFailoverServers);
        }

        // Assert - Large server should get more keys than small server due to higher virtual nodes
        var smallPercentage = finalDistribution["small"] * 100.0 / testKeys.Length;
        var largePercentage = finalDistribution["large"] * 100.0 / testKeys.Length;

        // Large has 4x virtual nodes of small, so should get roughly 4x keys
        var ratio = largePercentage / smallPercentage;
        Assert.True(ratio >= 3.0 && ratio <= 5.0,
            $"Large server should get ~4x keys of small server, actual ratio: {ratio:F1}");
    }

    [Fact]
    public void HashRing_ServerFailureAndRecovery_MinimalReRedistribution()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Simulate server failure and recovery
        var failedKeys = testKeys.Where(key => initialMapping[key] == "server2").ToArray();

        // Server fails
        ring.Remove("server2");
        _ = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Server recovers
        ring.Add("server2");
        var recoveryMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Assert - After recovery, most keys should return to original mapping
        var backToOriginal = testKeys.Count(key => initialMapping[key] == recoveryMapping[key]);
        var recoveryPercentage = backToOriginal * 100.0 / testKeys.Length;

        // Should recover at least 80% of original mappings
        Assert.True(recoveryPercentage >= 80,
            $"Expected at least 80% recovery to original mapping, got {recoveryPercentage:F1}%");

        // Specifically check failed server keys
        var server2RecoveredKeys = failedKeys.Count(key => recoveryMapping[key] == "server2");
        var server2RecoveryPercentage = server2RecoveredKeys * 100.0 / failedKeys.Length;

        Assert.True(server2RecoveryPercentage >= 70,
            $"Server2 should recover at least 70% of its original keys, got {server2RecoveryPercentage:F1}%");
    }

    [Fact]
    public void HashRing_MultipleSimultaneousFailures_HandledCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        for (int i = 1; i <= 6; i++)
        {
            ring.Add($"server{i}");
        }

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();
        _ = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Remove multiple servers simultaneously
        ring.Remove("server1");
        ring.Remove("server3");
        ring.Remove("server5");

        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Assert - All keys are still mapped to valid remaining servers
        var remainingServers = new[] { "server2", "server4", "server6" };
        foreach (var key in testKeys)
        {
            Assert.Contains(finalMapping[key], remainingServers);
        }

        // Assert - Distribution is reasonably balanced among remaining servers
        var finalDistribution = new Dictionary<string, int>();
        CountServerDistribution(finalMapping, finalDistribution);

        var expectedPercentage = 100.0 / remainingServers.Length;
        const int tolerance = 20; // Allow 20% deviation

        foreach (var server in remainingServers)
        {
            var percentage = finalDistribution[server] * 100.0 / testKeys.Length;
            Assert.True(percentage >= expectedPercentage - tolerance && percentage <= expectedPercentage + tolerance,
                $"Server {server} has {percentage:F1}% of keys, expected ~{expectedPercentage:F1}% ± {tolerance}%");
        }

        Assert.Equal(3, ring.Servers.Count);
    }

    private static void CountServerDistribution(Dictionary<string, string> mapping, Dictionary<string, int> counts)
    {
        counts.Clear();
        foreach (var server in mapping.Values)
        {
            counts[server] = counts.GetValueOrDefault(server) + 1;
        }
    }
}
