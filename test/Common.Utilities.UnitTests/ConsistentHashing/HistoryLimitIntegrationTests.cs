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
            EnableVersionHistory = true,
            MaxHistorySize = maxHistorySize
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
            EnableVersionHistory = true,
            MaxHistorySize = 2
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
            EnableVersionHistory = true,
            MaxHistorySize = 2
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

        var candidates = hashRing.GetServerCandidates(testKey);

        Assert.True(candidates.HasHistory);
        Assert.Equal(3, candidates.ConfigurationCount);
        Assert.True(candidates.Servers.Count >= 1);
    }

    [Fact]
    public void HistoryLimit_MaxCandidatesWithHistory_RespectsBothLimits()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 3
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

        var candidates = hashRing.GetServerCandidates(testKey, maxCandidates: 2);

        Assert.True(candidates.HasHistory);
        Assert.True(candidates.Servers.Count <= 2);
        Assert.Equal(4, candidates.ConfigurationCount);
    }

    [Fact]
    public void HistoryLimit_ZeroMaxCandidates_ReturnsEmptyList()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 2
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("server-1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server-2");

        var testKey = Encoding.UTF8.GetBytes("zero-candidates");
        var candidates = hashRing.GetServerCandidates(testKey, maxCandidates: 0);

        Assert.Empty(candidates.Servers);
        Assert.True(candidates.HasHistory);
        Assert.Equal(2, candidates.ConfigurationCount);
    }

    [Fact]
    public void HistoryLimit_TryGetAfterLimit_ReturnsFalseWhenNoServers()
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = 1
        };
        var hashRing = new HashRing<string>(options);

        var testKey = Encoding.UTF8.GetBytes("try-get-test");

        var success = hashRing.TryGetServerCandidates(testKey, out var result);

        Assert.False(success);
        Assert.Null(result);

        hashRing.Add("server-1");
        hashRing.CreateConfigurationSnapshot();

        success = hashRing.TryGetServerCandidates(testKey, out result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result.Servers);
        Assert.True(result.HasHistory);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(3, 20)]
    [InlineData(5, 50)]
    public void HistoryLimit_StressTest_MaintainsIntegrity(int maxHistorySize, int operationCount)
    {
        var options = new HashRingOptions
        {
            EnableVersionHistory = true,
            MaxHistorySize = maxHistorySize
        };
        var hashRing = new HashRing<string>(options);

        hashRing.Add("base-server");
        var testKey = Encoding.UTF8.GetBytes("stress-test-key");

        int successfulSnapshots = 0;
        int rejectedSnapshots = 0;

        for (int i = 0; i < operationCount; i++)
        {
            try
            {
                hashRing.CreateConfigurationSnapshot();
                hashRing.Add($"server-{i}");
                successfulSnapshots++;
            }
            catch (HashRingHistoryLimitExceededException)
            {
                rejectedSnapshots++;
                hashRing.ClearHistory();
            }

            var candidates = hashRing.TryGetServerCandidates(testKey, out var result);
            Assert.True(candidates);
            Assert.NotNull(result);
            Assert.True(hashRing.HistoryCount <= maxHistorySize);
        }

        Assert.True(successfulSnapshots > 0);
        Assert.True(rejectedSnapshots > 0);
        Assert.Equal(operationCount, successfulSnapshots + rejectedSnapshots);
    }
}
