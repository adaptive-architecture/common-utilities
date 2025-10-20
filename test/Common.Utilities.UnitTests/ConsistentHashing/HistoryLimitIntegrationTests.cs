namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System.Text;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class HistoryLimitIntegrationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void HistoryLimit_EnforcedCorrectly(int maxHistorySize)
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = maxHistorySize,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-base");

        for (int i = 0; i < maxHistorySize; i++)
        {
            hashRing.CreateConfigurationSnapshot();
            hashRing.Add($"server-{i + 1}");
        }

        Assert.Equal(maxHistorySize, hashRing.HistoryCount);

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            hashRing.CreateConfigurationSnapshot());

        Assert.Equal(maxHistorySize, exception.MaxHistorySize);
        Assert.Equal(maxHistorySize, exception.CurrentCount);
    }

    [Fact]
    public void HistoryLimit_AfterClear_CanAddNewSnapshots()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 2,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-2");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-3");

        Assert.Equal(2, hashRing.HistoryCount);

        Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            hashRing.CreateConfigurationSnapshot());

        hashRing.ClearHistory();

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-4");

        Assert.Equal(1, hashRing.HistoryCount);
    }

    [Fact]
    public void HistoryLimit_ServerCandidatesAtCapacity_WorksCorrectly()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 2,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("primary-1");
        hashRing.Add("primary-2");

        var testKey = Encoding.UTF8.GetBytes("test-at-capacity");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("primary-3");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("primary-4");

        Assert.Equal(2, hashRing.HistoryCount);

        var server = hashRing.GetServer(testKey);

        Assert.NotNull(server);
    }

    [Fact]
    public void HistoryLimit_MaxCandidatesWithHistory_RespectsBothLimits()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 3,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
        };
        var hashRing = new HashRing<string>(options);

        for (int i = 1; i <= 5; i++)
        {
            hashRing.Add($"server-{i}");
        }

        var testKey = Encoding.UTF8.GetBytes("multi-limit-test");

        for (int i = 0; i < 3; i++)
        {
            hashRing.CreateConfigurationSnapshot();
            hashRing.Add($"new-server-{i}");
        }

        var servers = hashRing.GetServers(testKey, count: 2).ToList();

        Assert.NotNull(servers);
        Assert.True(servers.Count <= 2);
        Assert.Equal(3, hashRing.HistoryCount); // 3 snapshots in history
    }

    [Fact]
    public void HistoryLimit_ZeroMaxCandidates_ReturnsEmptyList()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 2,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-2");

        var testKey = Encoding.UTF8.GetBytes("zero-candidates");
        var servers = hashRing.GetServers(testKey, count: 0).ToList();

        Assert.Empty(servers);
        Assert.Equal(1, hashRing.HistoryCount); // 1 snapshot in history
    }

    [Fact]
    public void HistoryLimit_TryGetAfterLimit_ReturnsFalseWhenNoServers()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 1,
            HistoryLimitBehavior = HistoryLimitBehavior.RemoveOldest
        };
        var hashRing = new HashRing<string>(options);

        var testKey = Encoding.UTF8.GetBytes("try-get-test");

        var success = hashRing.TryGetServer(testKey, out var result);

        Assert.False(success);
        Assert.Null(result);

        hashRing.Add("server-1");
        hashRing.CreateConfigurationSnapshot();

        success = hashRing.TryGetServer(testKey, out result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(1, hashRing.HistoryCount);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(3, 20)]
    [InlineData(5, 50)]
    public void HistoryLimit_StressTest_MaintainsIntegrity(int maxHistorySize, int operationCount)
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = maxHistorySize,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("base-server");
        hashRing.CreateConfigurationSnapshot(); // Create initial snapshot
        var testKey = Encoding.UTF8.GetBytes("stress-test-key");

        int successfulSnapshots = 1; // Count the initial snapshot
        int rejectedSnapshots = 0;

        for (int i = 0; i < operationCount; i++)
        {
            try
            {
                hashRing.Add($"server-{i}");
                hashRing.CreateConfigurationSnapshot();
                successfulSnapshots++;
            }
            catch (HashRingHistoryLimitExceededException)
            {
                rejectedSnapshots++;
                hashRing.ClearHistory();
                hashRing.CreateConfigurationSnapshot(); // Create new snapshot after clearing
            }

            var candidates = hashRing.TryGetServer(testKey, out var result);
            Assert.True(candidates);
            Assert.NotNull(result);
            Assert.True(hashRing.HistoryCount <= maxHistorySize);
        }

        Assert.True(successfulSnapshots > 0);
        Assert.True(rejectedSnapshots > 0);
        Assert.True(successfulSnapshots + rejectedSnapshots >= operationCount);
    }
}
