namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using System;
using System.Text;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

public sealed class HashRingVersionAwareTests
{
    private readonly HashRingOptions _versionAwareOptions = new()
    {
        MaxHistorySize = 3
    };

    [Fact]
    public void Constructor_WithVersionHistoryEnabled_SetsPropertiesCorrectly()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);

        Assert.True(hashRing.MaxHistorySize > 0);
        Assert.Equal(3, hashRing.MaxHistorySize);
        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void CreateConfigurationSnapshot_WithServers_CreatesSnapshot()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.Add("server2");

        hashRing.CreateConfigurationSnapshot();

        Assert.Equal(1, hashRing.HistoryCount);
    }

    [Fact]
    public void CreateConfigurationSnapshot_AtMaxCapacity_ThrowsHashRingHistoryLimitExceededException()
    {
        var options = new HashRingOptions
        {
            MaxHistorySize = 1,
            HistoryLimitBehavior = HistoryLimitBehavior.ThrowError
        };
        var hashRing = new HashRing<string>(options);
        hashRing.Add("server1");

        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server2"); // Add server to create a different snapshot

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            hashRing.CreateConfigurationSnapshot());

        Assert.Equal(1, exception.MaxHistorySize);
        Assert.Equal(1, exception.CurrentCount);
    }

    [Fact]
    public void ClearHistory_WithSnapshots_ClearsAll()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server2");
        hashRing.CreateConfigurationSnapshot();

        hashRing.ClearHistory();

        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void ClearHistory_WhenEmpty_DoesNotThrow()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);

        hashRing.ClearHistory();

        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void GetServerCandidates_WithNoServers_ThrowsInvalidOperationException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        var key = Encoding.UTF8.GetBytes("test-key");

        Assert.Throws<InvalidOperationException>(() => hashRing.GetServer(key));
    }

    [Fact]
    public void GetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        byte[] key = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => hashRing.GetServer(key));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetServer_WithCurrentConfigurationOnly_ReturnsServer()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.CreateConfigurationSnapshot();
        var key = Encoding.UTF8.GetBytes("test-key");

        var result = hashRing.GetServer(key);

        Assert.NotNull(result);
        Assert.Equal("server1", result);
    }

    [Fact]
    public void GetServers_WithHistory_ReturnsMultipleServers()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server2");
        var key = Encoding.UTF8.GetBytes("test-key");

        var servers = hashRing.GetServers(key, 2);

        Assert.NotNull(servers);
        Assert.NotEmpty(servers);
    }

    [Fact]
    public void TryGetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        byte[] key = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            hashRing.TryGetServer(key, out _));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void TryGetServerCandidates_WithNoServers_ReturnsFalse()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        var key = Encoding.UTF8.GetBytes("test-key");

        var success = hashRing.TryGetServer(key, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetServerCandidates_WithServers_ReturnsTrue()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.CreateConfigurationSnapshot();
        var key = Encoding.UTF8.GetBytes("test-key");

        var success = hashRing.TryGetServer(key, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal("server1", result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void GetServers_WithCount_RespectsLimit(int count)
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.Add("server2");
        hashRing.CreateConfigurationSnapshot();
        var key = Encoding.UTF8.GetBytes("test-key");

        var servers = hashRing.GetServers(key, count);

        Assert.NotNull(servers);
        Assert.True(servers.Count() <= count);
    }

    [Fact]
    public void GetServers_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        var key = Encoding.UTF8.GetBytes("test-key");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            hashRing.GetServers(key, -1));

        Assert.Equal("count", exception.ParamName);
    }
}
