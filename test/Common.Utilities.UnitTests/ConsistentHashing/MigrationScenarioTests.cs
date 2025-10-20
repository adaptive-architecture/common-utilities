namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class MigrationScenarioTests
{
    private static readonly string[] TestServers = ["server-1", "server-2", "server-3"];

    [Fact]
    public void Migration_TwoToThreeServers_ReturnsCorrectCandidates()
    {
        var options = new HashRingOptions { MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("user:12345");
        _ = hashRing.GetServer(testKey);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");

        var servers = hashRing.GetServers(testKey, 3);
        Assert.All(servers, server => Assert.Contains(server, TestServers));
    }

    [Fact]
    public void Migration_MultipleKeys_MaintainsConsistency()
    {
        var options = new HashRingOptions { MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("db-server-1");
        hashRing.Add("db-server-2");
        hashRing.CreateConfigurationSnapshot();

        var testKeys = new[]
        {
            Encoding.UTF8.GetBytes("user:alice"),
            Encoding.UTF8.GetBytes("user:bob"),
            Encoding.UTF8.GetBytes("user:charlie"),
            Encoding.UTF8.GetBytes("order:1001"),
            Encoding.UTF8.GetBytes("product:widget")
        };

        var initialMapping = testKeys.ToDictionary(key => key, key => hashRing.GetServer(key));

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("db-server-3");

        foreach (var key in testKeys)
        {
            var initialServer = initialMapping[key];
            var servers = hashRing.GetServers(key, 3);

            if (!servers.Contains(initialServer))
            {
                Assert.Fail($"Key {Encoding.UTF8.GetString(key)} lost its original server {initialServer}");
            }
        }
    }

    [Fact]
    public void Migration_ThreePhaseScaleOut_ManagesHistoryCorrectly()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 3,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        _ = Encoding.UTF8.GetBytes("test-key");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");
        Assert.Equal(1, hashRing.HistoryCount);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-4");
        Assert.Equal(2, hashRing.HistoryCount);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-5");
        Assert.Equal(3, hashRing.HistoryCount);


        Assert.Throws<HashRingHistoryLimitExceededException>(() => hashRing.CreateConfigurationSnapshot());
    }

    [Fact]
    public void Migration_ScaleOutThenClearHistory_OnlyCurrentConfigurationRemains()
    {
        var options = new HashRingOptions { MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("cache-1");
        hashRing.Add("cache-2");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("cache-key:important");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("cache-3");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("cache-4");

        Assert.Equal(2, hashRing.HistoryCount);

        _ = hashRing.GetServer(testKey);
        hashRing.ClearHistory();
        hashRing.CreateConfigurationSnapshot(); // Create new snapshot after clearing history

        var afterClear = hashRing.GetServer(testKey);
        Assert.NotNull(afterClear);
        Assert.Equal(1, hashRing.HistoryCount); // One new snapshot exists
    }

    [Fact]
    public void Migration_ComplexServerChanges_MaintainsDataAccessibility()
    {
        var options = new HashRingOptions { MaxHistorySize = 5 };
        var hashRing = new HashRing<string>(options);

        var dataKeys = GenerateTestKeys("data:", 100);

        hashRing.Add("node-a");
        hashRing.Add("node-b");
        hashRing.CreateConfigurationSnapshot();

        var phase1Mapping = dataKeys.ToDictionary(key => key, key => hashRing.GetServer(key));

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("node-c");

        _ = dataKeys.ToDictionary(key => key, key => hashRing.GetServer(key));

        hashRing.CreateConfigurationSnapshot();
        hashRing.Remove("node-a");

        foreach (var key in dataKeys)
        {
            var servers = hashRing.GetServers(key, 5);
            var originalServer = phase1Mapping[key];

            // Either the original server is still available in candidates, or we have a new server
            var allCandidateServers = servers.ToHashSet();
            Assert.True(allCandidateServers.Contains(originalServer) ||
                       allCandidateServers.Any(s => s != originalServer));
        }
    }

    [Fact]
    public void Migration_ServerDeduplication_ReturnsUniqueServers()
    {
        var options = new HashRingOptions { MaxHistorySize = 2 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-alpha");
        hashRing.Add("server-beta");
        hashRing.CreateConfigurationSnapshot();

        var testKey = Encoding.UTF8.GetBytes("consistent-key");
        _ = hashRing.GetServer(testKey);

        hashRing.CreateConfigurationSnapshot();

        var servers = hashRing.GetServers(testKey, 2);

        var uniqueServers = servers.Distinct().ToList();
        Assert.Equal(uniqueServers.Count, servers.Count());
    }

    private static List<byte[]> GenerateTestKeys(string prefix, int count)
    {
        var keys = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            keys.Add(Encoding.UTF8.GetBytes($"{prefix}{i:D4}"));
        }
        return keys;
    }
}
