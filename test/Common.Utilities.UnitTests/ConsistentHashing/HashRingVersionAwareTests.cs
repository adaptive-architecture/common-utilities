namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Text;
using Xunit;

public sealed class HashRingVersionAwareTests
{
    private readonly HashRingOptions _versionAwareOptions = new()
    {
        EnableVersionHistory = true,
        MaxHistorySize = 3
    };

    private readonly HashRingOptions _noHistoryOptions = new()
    {
        EnableVersionHistory = false
    };

    [Fact]
    public void Constructor_WithVersionHistoryEnabled_SetsPropertiesCorrectly()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);

        Assert.True(hashRing.IsVersionHistoryEnabled);
        Assert.Equal(3, hashRing.MaxHistorySize);
        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void Constructor_WithVersionHistoryDisabled_SetsPropertiesCorrectly()
    {
        var hashRing = new HashRing<string>(_noHistoryOptions);

        Assert.False(hashRing.IsVersionHistoryEnabled);
        Assert.Equal(0, hashRing.MaxHistorySize);
        Assert.Equal(0, hashRing.HistoryCount);
    }

    [Fact]
    public void CreateConfigurationSnapshot_WithHistoryDisabled_ThrowsInvalidOperationException()
    {
        var hashRing = new HashRing<string>(_noHistoryOptions);

        var exception = Assert.Throws<InvalidOperationException>(() => hashRing.CreateConfigurationSnapshot());

        Assert.Contains("version history is not enabled", exception.Message);
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
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 1 };
        var hashRing = new HashRing<string>(options);
        hashRing.Add("server1");

        hashRing.CreateConfigurationSnapshot();

        var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() =>
            hashRing.CreateConfigurationSnapshot());

        Assert.Equal(1, exception.MaxHistorySize);
        Assert.Equal(1, exception.CurrentCount);
    }

    [Fact]
    public void ClearHistory_WithHistoryDisabled_ThrowsInvalidOperationException()
    {
        var hashRing = new HashRing<string>(_noHistoryOptions);

        var exception = Assert.Throws<InvalidOperationException>(() => hashRing.ClearHistory());

        Assert.Contains("version history is not enabled", exception.Message);
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

        Assert.Throws<InvalidOperationException>(() => hashRing.GetServerCandidates(key));
    }

    [Fact]
    public void GetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        byte[] key = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => hashRing.GetServerCandidates(key));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetServerCandidates_WithCurrentConfigurationOnly_ReturnsOneServer()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        var key = Encoding.UTF8.GetBytes("test-key");

        var result = hashRing.GetServerCandidates(key);

        Assert.Single(result.Servers);
        Assert.Equal("server1", result.Servers[0]);
        Assert.Equal(1, result.ConfigurationCount);
        Assert.False(result.HasHistory);
    }

    [Fact]
    public void GetServerCandidates_WithHistory_ReturnsMultipleServers()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.CreateConfigurationSnapshot();
        hashRing.Add("server2");
        var key = Encoding.UTF8.GetBytes("test-key");

        var result = hashRing.GetServerCandidates(key);

        Assert.True(result.Servers.Count >= 1);
        Assert.Equal(2, result.ConfigurationCount);
        Assert.True(result.HasHistory);
    }

    [Fact]
    public void TryGetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        byte[] key = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            hashRing.TryGetServerCandidates(key, out _));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void TryGetServerCandidates_WithNoServers_ReturnsFalse()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        var key = Encoding.UTF8.GetBytes("test-key");

        var success = hashRing.TryGetServerCandidates(key, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetServerCandidates_WithServers_ReturnsTrue()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        var key = Encoding.UTF8.GetBytes("test-key");

        var success = hashRing.TryGetServerCandidates(key, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result.Servers);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void GetServerCandidates_WithMaxCandidates_RespectsLimit(int maxCandidates)
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        hashRing.Add("server2");
        var key = Encoding.UTF8.GetBytes("test-key");

        var result = hashRing.GetServerCandidates(key, maxCandidates);

        Assert.True(result.Servers.Count <= maxCandidates);
    }

    [Fact]
    public void GetServerCandidates_WithNegativeMaxCandidates_ThrowsArgumentOutOfRangeException()
    {
        var hashRing = new HashRing<string>(_versionAwareOptions);
        hashRing.Add("server1");
        var key = Encoding.UTF8.GetBytes("test-key");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            hashRing.GetServerCandidates(key, -1));

        Assert.Equal("maxCandidates", exception.ParamName);
    }
}
