namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Collections.Generic;
using Xunit;

public sealed class ServerCandidateResultTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var servers = new List<string> { "server1", "server2" };
        const int configurationCount = 2;
        const bool hasHistory = true;

        var result = new ServerCandidateResult<string>(servers, configurationCount, hasHistory);

        Assert.Equal(servers, result.Servers);
        Assert.Equal(configurationCount, result.ConfigurationCount);
        Assert.Equal(hasHistory, result.HasHistory);
    }

    [Fact]
    public void Constructor_WithNullServers_ThrowsArgumentNullException()
    {
        List<string> servers = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServerCandidateResult<string>(servers, 1, true));

        Assert.Equal("servers", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyServers_SetsEmptyList()
    {
        var servers = new List<string>();

        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.Empty(result.Servers);
        Assert.Equal(1, result.ConfigurationCount);
        Assert.False(result.HasHistory);
    }

    [Fact]
    public void Constructor_WithSingleServer_WorksCorrectly()
    {
        var servers = new List<string> { "server1" };

        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.Single(result.Servers);
        Assert.Equal("server1", result.Servers[0]);
        Assert.Equal(1, result.ConfigurationCount);
        Assert.False(result.HasHistory);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void Constructor_WithVariousConfigurationCounts_SetsCorrectly(int configCount, bool expectedHistory)
    {
        var servers = new List<string> { "server1" };

        var result = new ServerCandidateResult<string>(servers, configCount, expectedHistory);

        Assert.Equal(configCount, result.ConfigurationCount);
        Assert.Equal(expectedHistory, result.HasHistory);
    }
}
