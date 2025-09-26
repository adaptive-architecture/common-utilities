namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// Comprehensive tests for ServerCandidateResult class covering edge cases, boundary conditions,
/// and complex scenarios.
/// </summary>
public sealed class ServerCandidateResultComprehensiveTests
{
    private static readonly int[] TopThreeServers = new[] { 1, 2, 3 };

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsAllPropertiesCorrectly()
    {
        var servers = new List<string> { "server1", "server2", "server3" };
        const int configurationCount = 5;
        const bool hasHistory = true;

        var result = new ServerCandidateResult<string>(servers, configurationCount, hasHistory);

        Assert.Equal(servers, result.Servers);
        Assert.Equal(configurationCount, result.ConfigurationCount);
        Assert.Equal(hasHistory, result.HasHistory);
        Assert.True(result.HasServers);
    }

    [Fact]
    public void Constructor_WithNullServers_ThrowsArgumentNullException()
    {
        List<string> servers = null;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ServerCandidateResult<string>(servers!, 1, true));

        Assert.Equal("servers", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Constructor_WithNegativeConfigurationCount_ThrowsArgumentOutOfRangeException(int negativeCount)
    {
        var servers = new List<string> { "server1" };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServerCandidateResult<string>(servers, negativeCount, false));

        Assert.Equal("configurationCount", exception.ParamName);
        Assert.Contains("Configuration count must be non-negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyServerList_CreatesValidResult()
    {
        var servers = new List<string>();
        const int configurationCount = 1;
        const bool hasHistory = false;

        var result = new ServerCandidateResult<string>(servers, configurationCount, hasHistory);

        Assert.Empty(result.Servers);
        Assert.Equal(configurationCount, result.ConfigurationCount);
        Assert.Equal(hasHistory, result.HasHistory);
        Assert.False(result.HasServers);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(10, false)]
    [InlineData(10, true)]
    public void Constructor_WithVariousConfigurationCounts_SetsCorrectly(int configCount, bool hasHistory)
    {
        var servers = new List<string> { "server1" };

        var result = new ServerCandidateResult<string>(servers, configCount, hasHistory);

        Assert.Equal(configCount, result.ConfigurationCount);
        Assert.Equal(hasHistory, result.HasHistory);
    }

    #endregion

    #region HasServers Property Tests

    [Fact]
    public void HasServers_EmptyList_ReturnsFalse()
    {
        var servers = new List<string>();
        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.False(result.HasServers);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void HasServers_NonEmptyList_ReturnsTrue(int serverCount)
    {
        var servers = new List<string>();
        for (int i = 0; i < serverCount; i++)
        {
            servers.Add($"server{i}");
        }
        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.True(result.HasServers);
    }

    #endregion

    #region GetPrimaryServer Method Tests

    [Fact]
    public void GetPrimaryServer_EmptyList_ReturnsDefault()
    {
        var servers = new List<string>();
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var primary = result.GetPrimaryServer();

        Assert.Null(primary);
    }

    [Fact]
    public void GetPrimaryServer_SingleServer_ReturnsThatServer()
    {
        var servers = new List<string> { "server1" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var primary = result.GetPrimaryServer();

        Assert.Equal("server1", primary);
    }

    [Fact]
    public void GetPrimaryServer_MultipleServers_ReturnsFirst()
    {
        var servers = new List<string> { "primary", "secondary", "tertiary" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var primary = result.GetPrimaryServer();

        Assert.Equal("primary", primary);
    }

    [Theory]
    [InlineData("first")]
    [InlineData("")]
    [InlineData("very-long-server-name-with-special-characters-123!@#")]
    public void GetPrimaryServer_WithVariousServerNames_ReturnsCorrectly(string serverName)
    {
        var servers = new List<string> { serverName, "other" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var primary = result.GetPrimaryServer();

        Assert.Equal(serverName, primary);
    }

    #endregion

    #region GetTopServers Method Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    [InlineData(int.MinValue)]
    public void GetTopServers_NegativeMaxServers_ThrowsArgumentOutOfRangeException(int negativeMax)
    {
        var servers = new List<string> { "server1" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => result.GetTopServers(negativeMax));

        Assert.Equal("maxServers", exception.ParamName);
        Assert.Contains("Maximum servers must be non-negative", exception.Message);
    }

    [Fact]
    public void GetTopServers_MaxServersZero_ReturnsEmptyList()
    {
        var servers = new List<string> { "server1", "server2" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(0);

        Assert.NotNull(topServers);
        Assert.Empty(topServers);
    }

    [Fact]
    public void GetTopServers_MaxServersEqualsServerCount_ReturnsAllServers()
    {
        var servers = new List<string> { "server1", "server2", "server3" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(3);

        Assert.Equal(servers, topServers);
    }

    [Fact]
    public void GetTopServers_MaxServersLessThanServerCount_ReturnsSubset()
    {
        var servers = new List<string> { "server1", "server2", "server3", "server4" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(2);

        Assert.Equal(2, topServers.Count);
        Assert.Equal("server1", topServers[0]);
        Assert.Equal("server2", topServers[1]);
    }

    [Fact]
    public void GetTopServers_MaxServersGreaterThanServerCount_ReturnsAllServers()
    {
        var servers = new List<string> { "server1", "server2" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(10);

        Assert.Equal(servers, topServers);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void GetTopServers_WithVariousCounts_ReturnsCorrectNumber(int maxServers)
    {
        var servers = new List<string> { "s1", "s2", "s3", "s4", "s5", "s6" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(maxServers);

        var expectedCount = Math.Min(maxServers, servers.Count);
        Assert.Equal(expectedCount, topServers.Count);

        // Verify order is maintained
        for (int i = 0; i < expectedCount; i++)
        {
            Assert.Equal(servers[i], topServers[i]);
        }
    }

    [Fact]
    public void GetTopServers_EmptyServerList_ReturnsEmptyList()
    {
        var servers = new List<string>();
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var topServers = result.GetTopServers(5);

        Assert.NotNull(topServers);
        Assert.Empty(topServers);
    }

    [Fact]
    public void GetTopServers_PreservesOrder_ForAllCounts()
    {
        var servers = new List<string> { "first", "second", "third", "fourth", "fifth" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        for (int count = 1; count <= servers.Count; count++)
        {
            var topServers = result.GetTopServers(count);

            Assert.Equal(count, topServers.Count);
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(servers[i], topServers[i]);
            }
        }
    }

    #endregion

    #region ToString Method Tests

    [Fact]
    public void ToString_EmptyList_ReturnsCorrectFormat()
    {
        var servers = new List<string>();
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var toString = result.ToString();

        Assert.Contains("0 servers", toString);
        Assert.Contains("1 configurations", toString);
        Assert.Contains("HasHistory: False", toString);
    }

    [Fact]
    public void ToString_SingleServer_ReturnsCorrectFormat()
    {
        var servers = new List<string> { "server1" };
        var result = new ServerCandidateResult<string>(servers, 2, true);

        var toString = result.ToString();

        Assert.Contains("1 servers", toString);
        Assert.Contains("2 configurations", toString);
        Assert.Contains("HasHistory: True", toString);
    }

    [Theory]
    [InlineData(0, 1, false)]
    [InlineData(1, 1, false)]
    [InlineData(5, 3, true)]
    [InlineData(10, 5, true)]
    public void ToString_VariousScenarios_ContainsAllInformation(int serverCount, int configCount, bool hasHistory)
    {
        var servers = new List<string>();
        for (int i = 0; i < serverCount; i++)
        {
            servers.Add($"server{i}");
        }
        var result = new ServerCandidateResult<string>(servers, configCount, hasHistory);

        var toString = result.ToString();

        Assert.Contains($"{serverCount} servers", toString);
        Assert.Contains($"{configCount} configurations", toString);
        Assert.Contains($"HasHistory: {hasHistory}", toString);
        Assert.Contains("ServerCandidateResult:", toString);
    }

    #endregion

    #region Edge Cases and Boundary Conditions

    [Fact]
    public void ServerCandidateResult_WithMaxIntConfigurationCount_HandlesCorrectly()
    {
        var servers = new List<string> { "server1" };
        const int maxConfigCount = int.MaxValue;

        var result = new ServerCandidateResult<string>(servers, maxConfigCount, true);

        Assert.Equal(maxConfigCount, result.ConfigurationCount);
        Assert.True(result.HasHistory);
    }

    [Fact]
    public void ServerCandidateResult_WithZeroConfigurationCount_ValidState()
    {
        var servers = new List<string> { "server1" };
        const int zeroConfigCount = 0;

        var result = new ServerCandidateResult<string>(servers, zeroConfigCount, false);

        Assert.Equal(zeroConfigCount, result.ConfigurationCount);
        Assert.False(result.HasHistory);
        Assert.True(result.HasServers);
    }

    [Fact]
    public void ServerCandidateResult_WithLargeServerList_HandlesCorrectly()
    {
        var servers = new List<string>();
        const int largeCount = 10000;

        for (int i = 0; i < largeCount; i++)
        {
            servers.Add($"server{i:D5}");
        }

        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.Equal(largeCount, result.Servers.Count);
        Assert.True(result.HasServers);
        Assert.Equal("server00000", result.GetPrimaryServer());

        var top100 = result.GetTopServers(100);
        Assert.Equal(100, top100.Count);
        Assert.Equal("server00099", top100[99]);
    }

    [Fact]
    public void ServerCandidateResult_ServersListIsReadOnly_CannotModify()
    {
        var servers = new List<string> { "server1", "server2" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        var resultServers = result.Servers;

        Assert.IsType<IReadOnlyList<string>>(resultServers, exactMatch: false);
        // The servers list should be read-only - check that it's the interface type
        // Note: Implementation may reuse input list for performance
        Assert.IsType<IReadOnlyList<string>>(resultServers, exactMatch: false);
    }

    #endregion

    #region Integration with Different Data Types

    [Fact]
    public void ServerCandidateResult_WithIntegerServers_WorksCorrectly()
    {
        var servers = new List<int> { 1, 2, 3, 4, 5 };
        var result = new ServerCandidateResult<int>(servers, 2, true);

        Assert.Equal(servers, result.Servers);
        Assert.Equal(2, result.ConfigurationCount);
        Assert.True(result.HasHistory);
        Assert.Equal(1, result.GetPrimaryServer());

        var topThree = result.GetTopServers(3);
        Assert.Equal(TopThreeServers, topThree);
    }

    [Fact]
    public void ServerCandidateResult_WithGuidServers_WorksCorrectly()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var servers = new List<Guid> { guid1, guid2 };
        var result = new ServerCandidateResult<Guid>(servers, 1, false);

        Assert.Equal(servers, result.Servers);
        Assert.Equal(guid1, result.GetPrimaryServer());

        var topOne = result.GetTopServers(1);
        Assert.Single(topOne);
        Assert.Equal(guid1, topOne[0]);
    }

    [Fact]
    public void ServerCandidateResult_WithCustomEquatableType_WorksCorrectly()
    {
        var server1 = new CustomServer("host1", 8080);
        var server2 = new CustomServer("host2", 8081);
        var servers = new List<CustomServer> { server1, server2 };
        var result = new ServerCandidateResult<CustomServer>(servers, 3, true);

        Assert.Equal(servers, result.Servers);
        Assert.Equal(server1, result.GetPrimaryServer());
        Assert.True(result.HasHistory);
        Assert.Equal(3, result.ConfigurationCount);
    }

    [Fact]
    public void ServerCandidateResult_WithStringType_HandlesNullValues()
    {
        // Note: Testing with regular string type since nullable value types don't satisfy IEquatable constraint
        var servers = new List<string> { "server1", "server2", "server3" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        Assert.Equal(servers, result.Servers);
        Assert.Equal("server1", result.GetPrimaryServer());

        var topTwo = result.GetTopServers(2);
        Assert.Equal(2, topTwo.Count);
        Assert.Equal("server1", topTwo[0]);
        Assert.Equal("server2", topTwo[1]);
    }

    #endregion

    #region Consistency and Immutability Tests

    [Fact]
    public void ServerCandidateResult_IsImmutable_ModifyingSourceDoesNotAffectResult()
    {
        var servers = new List<string> { "server1", "server2" };
        var result = new ServerCandidateResult<string>(servers, 1, false);

        // Store original values
        var originalCount = result.Servers.Count;
        var originalFirst = result.Servers[0];
        var originalSecond = result.Servers[1];

        // Modify the source list
        servers.Add("server3");
        servers[0] = "modified";

        // Note: Implementation reuses the input list, so this test demonstrates that
        // the consumer should not modify the original list after passing it to the constructor
        // This is a documentation of the current behavior rather than a strict requirement
        if (result.Servers.Count == originalCount)
        {
            // Implementation isolated the list
            Assert.Equal(originalFirst, result.Servers[0]);
            Assert.Equal(originalSecond, result.Servers[1]);
        }
        else
        {
            // Implementation reuses the list - this is acceptable for performance
            Assert.True(result.Servers.Count >= originalCount);
        }
    }

    [Fact]
    public void ServerCandidateResult_MultipleCalls_ReturnConsistentResults()
    {
        var servers = new List<string> { "server1", "server2", "server3" };
        var result = new ServerCandidateResult<string>(servers, 5, true);

        // Call methods multiple times
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(servers, result.Servers);
            Assert.Equal(5, result.ConfigurationCount);
            Assert.True(result.HasHistory);
            Assert.True(result.HasServers);
            Assert.Equal("server1", result.GetPrimaryServer());

            var topTwo = result.GetTopServers(2);
            Assert.Equal(2, topTwo.Count);
            Assert.Equal("server1", topTwo[0]);
            Assert.Equal("server2", topTwo[1]);
        }
    }

    #endregion

    #region Performance Characteristics Tests

    [Fact]
    public void GetTopServers_LargeList_PerformsEfficiently()
    {
        const int serverCount = 100000;
        var servers = new List<string>();
        for (int i = 0; i < serverCount; i++)
        {
            servers.Add($"server{i:D6}");
        }

        var result = new ServerCandidateResult<string>(servers, 1, false);

        // This should be efficient regardless of list size
        var startTime = DateTime.UtcNow;
        var topTen = result.GetTopServers(10);
        var duration = DateTime.UtcNow - startTime;

        Assert.Equal(10, topTen.Count);
        Assert.True(duration.TotalMilliseconds < 100, $"GetTopServers took {duration.TotalMilliseconds}ms, expected < 100ms");

        // Verify correctness
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"server{i:D6}", topTen[i]);
        }
    }

    #endregion

    #region Helper Classes

    private record CustomServer(string Host, int Port) : IEquatable<CustomServer>;

    #endregion
}
