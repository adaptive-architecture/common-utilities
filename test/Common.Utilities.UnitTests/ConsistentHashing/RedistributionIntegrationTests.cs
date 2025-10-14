using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class RedistributionIntegrationTests
{
    [Fact]
    public void HashRing_AddingServer_RedistributesMinimalKeys()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"user{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Add a new server
        ring.Add("server4");
        ring.CreateConfigurationSnapshot();

        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Calculate redistribution
        var redistributedKeys = testKeys.Where(key => initialMapping[key] != finalMapping[key]).ToArray();

        // Assert
        // With 4 servers, ideally ~25% of keys would be redistributed to the new server
        var redistributionPercentage = redistributedKeys.Length * 100.0 / testKeys.Length;

        // Allow for some variance in redistribution (15%-40% is reasonable)
        Assert.True(redistributionPercentage >= 15 && redistributionPercentage <= 40,
            $"Expected 15-40% redistribution, got {redistributionPercentage:F1}%");

        // All redistributed keys should now map to the new server
        foreach (var key in redistributedKeys)
        {
            Assert.Equal("server4", finalMapping[key]);
        }

        // Verify all keys are still mapped to valid servers
        foreach (var key in testKeys)
        {
            Assert.Contains(finalMapping[key], ring.Servers);
        }
    }

    [Fact]
    public void HashRing_AddingMultipleServers_DistributesKeysEvenly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();

        // Act - Add servers one by one and track distribution
        var serverCounts = new Dictionary<string, int>();

        // Add second server
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();
        var mapping2 = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        CountServerDistribution(mapping2, serverCounts);

        // With 2 servers, both should have roughly equal distribution
        Assert.True(Math.Abs(serverCounts["server1"] - serverCounts["server2"]) < testKeys.Length * 0.3,
            "Two servers should have roughly equal distribution");

        // Add third server
        serverCounts.Clear();
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();
        var mapping3 = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        CountServerDistribution(mapping3, serverCounts);

        // With 3 servers, each should get roughly 1/3 of the keys
        foreach (var server in ring.Servers)
        {
            var percentage = serverCounts[server] * 100.0 / testKeys.Length;
            Assert.True(percentage >= 20 && percentage <= 50,
                $"Server {server} has {percentage:F1}% of keys, expected ~33%");
        }
    }

    [Fact]
    public void HashRing_WithHighVirtualNodes_BetterRedistribution()
    {
        // Arrange - Create two rings: one with low virtual nodes, one with high
        var lowVNodeRing = new HashRing<string>();
        var highVNodeRing = new HashRing<string>();

        // Add initial servers
        lowVNodeRing.Add("server1", 1);
        lowVNodeRing.Add("server2", 1);
        lowVNodeRing.Add("server3", 1);
        lowVNodeRing.CreateConfigurationSnapshot();

        highVNodeRing.Add("server1", 100);
        highVNodeRing.Add("server2", 100);
        highVNodeRing.Add("server3", 100);
        highVNodeRing.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();

        // Get initial mappings
        var lowVNInitial = testKeys.ToDictionary(key => key, key => lowVNodeRing.GetServer(key));
        var highVNInitial = testKeys.ToDictionary(key => key, key => highVNodeRing.GetServer(key));

        // Act - Add fourth server to both rings
        lowVNodeRing.Add("server4", 1);
        lowVNodeRing.CreateConfigurationSnapshot();
        highVNodeRing.Add("server4", 100);
        highVNodeRing.CreateConfigurationSnapshot();

        var lowVNFinal = testKeys.ToDictionary(key => key, key => lowVNodeRing.GetServer(key));
        var highVNFinal = testKeys.ToDictionary(key => key, key => highVNodeRing.GetServer(key));

        // Calculate redistributions
        var lowVNRedistributed = testKeys.Count(key => lowVNInitial[key] != lowVNFinal[key]);
        var highVNRedistributed = testKeys.Count(key => highVNInitial[key] != highVNFinal[key]);

        // Assert - High virtual nodes should have redistribution closer to ideal 25%
        var lowVNPercentage = lowVNRedistributed * 100.0 / testKeys.Length;
        var highVNPercentage = highVNRedistributed * 100.0 / testKeys.Length;

        // High VN should be closer to the ideal 25%
        var lowVNDeviation = Math.Abs(lowVNPercentage - 25);
        var highVNDeviation = Math.Abs(highVNPercentage - 25);

        Assert.True(highVNDeviation <= lowVNDeviation,
            $"High VN deviation ({highVNDeviation:F1}%) should be <= Low VN deviation ({lowVNDeviation:F1}%)");
    }

    [Fact]
    public void HashRing_AddingServerWithDifferentVirtualNodes_RedistributesProportionally()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1", 100);
        ring.Add("server2", 100);
        ring.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();

        // Act - Add server with more virtual nodes (should get more keys)
        ring.Add("server3", 200); // Double the virtual nodes
        ring.CreateConfigurationSnapshot();

        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
        var serverCounts = new Dictionary<string, int>();
        CountServerDistribution(finalMapping, serverCounts);

        // Assert - Server3 should get approximately double the keys of server1/server2
        var server3Percentage = serverCounts["server3"] * 100.0 / testKeys.Length;
        var server1Percentage = serverCounts["server1"] * 100.0 / testKeys.Length;
        var server2Percentage = serverCounts["server2"] * 100.0 / testKeys.Length;

        // Server3 has 200 VN, others have 100 VN each = 50% vs 25% each
        Assert.True(server3Percentage >= 40 && server3Percentage <= 60,
            $"Server3 with 2x virtual nodes should get ~50% of keys, got {server3Percentage:F1}%");

        Assert.True(server1Percentage >= 15 && server1Percentage <= 35,
            $"Server1 should get ~25% of keys, got {server1Percentage:F1}%");

        Assert.True(server2Percentage >= 15 && server2Percentage <= 35,
            $"Server2 should get ~25% of keys, got {server2Percentage:F1}%");
    }

    [Fact]
    public void HashRing_AddingServersSequentially_MaintainsBalance()
    {
        // Arrange
        var ring = new HashRing<string>();
        var testKeys = Enumerable.Range(1, 500).Select(i => $"key{i}").ToArray();

        var distributionHistory = new List<Dictionary<string, int>>();

        // Act - Add servers one by one and track distribution at each step
        for (int i = 1; i <= 5; i++)
        {
            ring.Add($"server{i}");
            ring.CreateConfigurationSnapshot();

            var mapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));
            var distribution = new Dictionary<string, int>();
            CountServerDistribution(mapping, distribution);
            distributionHistory.Add(distribution);

            // Assert - At each step, distribution should be reasonably balanced
            var expectedPercentage = 100.0 / i;
            var tolerance = Math.Max(15, expectedPercentage * 0.5); // At least 15% tolerance

            foreach (var serverCount in distribution)
            {
                var percentage = serverCount.Value * 100.0 / testKeys.Length;
                Assert.True(percentage >= expectedPercentage - tolerance && percentage <= expectedPercentage + tolerance,
                    $"Step {i}: Server {serverCount.Key} has {percentage:F1}% of keys, expected ~{expectedPercentage:F1}% ± {tolerance:F1}%");
            }
        }
    }

    [Fact]
    public void HashRing_KeyRedistribution_OnlyAffectsNewServerKeys()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.CreateConfigurationSnapshot();

        var testKeys = Enumerable.Range(1, 1000).Select(i => $"key{i}").ToArray();
        var initialMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Act - Add new server
        ring.Add("server4");
        ring.CreateConfigurationSnapshot();
        var finalMapping = testKeys.ToDictionary(key => key, key => ring.GetServer(key));

        // Analyze redistribution
        var redistributedKeys = testKeys.Where(key => initialMapping[key] != finalMapping[key]).ToArray();
        var stableKeys = testKeys.Where(key => initialMapping[key] == finalMapping[key]).ToArray();

        // Assert - All redistributed keys should go to the new server
        foreach (var key in redistributedKeys)
        {
            Assert.Equal("server4", finalMapping[key]);
        }

        // Assert - Stable keys should maintain their original mapping
        foreach (var key in stableKeys)
        {
            Assert.Equal(initialMapping[key], finalMapping[key]);
            Assert.NotEqual("server4", finalMapping[key]);
        }

        // Assert - No keys should have been redistributed between existing servers
        var originalServers = new[] { "server1", "server2", "server3" };
        foreach (var key in redistributedKeys)
        {
            Assert.Contains(initialMapping[key], originalServers);
        }
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
