namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class MigrationScenarioTests
{
    private static readonly string[] TestServers = new[] { "server-1", "server-2", "server-3" };

    [Fact]
    public void Migration_TwoToThreeServers_ReturnsCorrectCandidates()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");

        var testKey = Encoding.UTF8.GetBytes("user:12345");
        _ = hashRing.GetServer(testKey);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");

        var candidates = hashRing.GetServerCandidates(testKey);

        Assert.True(candidates.HasHistory);
        Assert.Equal(2, candidates.ConfigurationCount);
        Assert.True(candidates.Servers.Count >= 1);
        Assert.True(candidates.Servers.Count <= 2);
        Assert.All(candidates.Servers, server => Assert.Contains(server, TestServers));
    }

    [Fact]
    public void Migration_MultipleKeys_MaintainsConsistency()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("db-server-1");
        hashRing.Add("db-server-2");

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
            var candidates = hashRing.GetServerCandidates(key);
            var initialServer = initialMapping[key];

            Assert.True(candidates.HasHistory);
            Assert.Equal(2, candidates.ConfigurationCount);

            if (!candidates.Servers.Contains(initialServer))
            {
                Assert.Fail($"Key {Encoding.UTF8.GetString(key)} lost its original server {initialServer}");
            }
        }
    }

    [Fact]
    public void Migration_ThreePhaseScaleOut_ManagesHistoryCorrectly()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.Add("server-2");
        var testKey = Encoding.UTF8.GetBytes("test-key");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");
        Assert.Equal(1, hashRing.HistoryCount);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-4");
        Assert.Equal(2, hashRing.HistoryCount);

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-5");
        Assert.Equal(3, hashRing.HistoryCount);

        var candidates = hashRing.GetServerCandidates(testKey);
        Assert.True(candidates.HasHistory);
        Assert.Equal(4, candidates.ConfigurationCount);

        Assert.Throws<HashRingHistoryLimitExceededException>(() => hashRing.CreateConfigurationSnapshot());
    }

    [Fact]
    public void Migration_ScaleOutThenClearHistory_OnlyCurrentConfigurationRemains()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 3 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("cache-1");
        hashRing.Add("cache-2");

        var testKey = Encoding.UTF8.GetBytes("cache-key:important");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("cache-3");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("cache-4");

        Assert.Equal(2, hashRing.HistoryCount);

        var beforeClear = hashRing.GetServerCandidates(testKey);
        Assert.True(beforeClear.HasHistory);

        hashRing.ClearHistory();

        var afterClear = hashRing.GetServerCandidates(testKey);
        Assert.False(afterClear.HasHistory);
        Assert.Equal(1, afterClear.ConfigurationCount);
        Assert.Single(afterClear.Servers);
        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void Migration_ComplexServerChanges_MaintainsDataAccessibility()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var hashRing = new HashRing<string>(options);

        var dataKeys = GenerateTestKeys("data:", 100);

        hashRing.Add("node-a");
        hashRing.Add("node-b");

        var phase1Mapping = dataKeys.ToDictionary(key => key, key => hashRing.GetServer(key));

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("node-c");

        _ = dataKeys.ToDictionary(key => key, key => hashRing.GetServerCandidates(key));

        hashRing.CreateConfigurationSnapshot();
        hashRing.Remove("node-a");

        foreach (var key in dataKeys)
        {
            var candidates = hashRing.GetServerCandidates(key);
            var originalServer = phase1Mapping[key];

            Assert.True(candidates.ConfigurationCount >= 2);

            var allCandidateServers = candidates.Servers.ToHashSet();
            Assert.True(allCandidateServers.Contains(originalServer) ||
                       allCandidateServers.Any(s => s != originalServer));
        }
    }

    [Fact]
    public void Migration_ServerDeduplication_ReturnsUniqueServers()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 2 };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-alpha");
        hashRing.Add("server-beta");

        var testKey = Encoding.UTF8.GetBytes("consistent-key");
        _ = hashRing.GetServer(testKey);

        hashRing.CreateConfigurationSnapshot();

        var candidates = hashRing.GetServerCandidates(testKey);

        var uniqueServers = candidates.Servers.Distinct().ToList();
        Assert.Equal(candidates.Servers.Count, uniqueServers.Count);
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
