namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;
#pragma warning disable S2925 // SONAR: Do not use 'Thread.Sleep()' in a test.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptArch.Common.Utilities.ConsistentHashing;
using Xunit;

/// <summary>
/// Comprehensive tests for HashRing class covering edge cases, boundary conditions,
/// and complex integration scenarios.
/// </summary>
public sealed class HashRingComprehensiveTests
{
    private static readonly int[] TestServers = new[] { 1, 2, 3 };
    private static readonly string[] TestServerNames = new[] { "server2", "server3" };

    #region Constructor and Configuration Tests

    [Fact]
    public void Constructor_WithOptions_SetsAllPropertiesCorrectly()
    {
        var options = new HashRingOptions
        {
            DefaultVirtualNodes = 100,
            EnableVersionHistory = true,
            MaxHistorySize = 5
        };

        var ring = new HashRing<string>(options);

        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
        Assert.True(ring.IsVersionHistoryEnabled);
        Assert.Equal(0, ring.HistoryCount);
        Assert.Equal(5, ring.MaxHistorySize);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        HashRingOptions options = null;

        var exception = Assert.Throws<ArgumentNullException>(() => new HashRing<string>(options!));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullHashAlgorithm_ThrowsArgumentNullException()
    {
        IHashAlgorithm algorithm = null;

        var exception = Assert.Throws<ArgumentNullException>(() => new HashRing<string>(algorithm!));
        Assert.Equal("hashAlgorithm", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Constructor_WithVariousDefaultVirtualNodes_CreatesValidRing(int virtualNodes)
    {
        var algorithm = new Sha1HashAlgorithm();

        // All values should be acceptable - the HashRing should handle them gracefully
        var ring = new HashRing<string>(algorithm, virtualNodes);

        Assert.NotNull(ring);
        Assert.True(ring.IsEmpty);
    }

    #endregion

    #region Add Method Comprehensive Tests

    [Fact]
    public void Add_WithNullServer_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        const string server = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.Add(server!));
        Assert.Equal("server", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Add_WithInvalidVirtualNodes_ThrowsArgumentOutOfRangeException(int invalidVirtualNodes)
    {
        var ring = new HashRing<string>();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ring.Add("server", invalidVirtualNodes));
        Assert.Equal("virtualNodes", exception.ParamName);
    }

    [Fact]
    public void Add_SameServerTwice_UpdatesVirtualNodeCount()
    {
        var ring = new HashRing<string>();

        ring.Add("server1", 10);
        Assert.Equal(10, ring.VirtualNodeCount);

        ring.Add("server1", 20);
        Assert.Equal(20, ring.VirtualNodeCount);
        Assert.Single(ring.Servers);
    }

    [Fact]
    public void Add_MultipleServersWithDifferentVirtualNodes_CalculatesCorrectTotalCount()
    {
        var ring = new HashRing<string>();

        ring.Add("server1", 10);
        ring.Add("server2", 15);
        ring.Add("server3", 25);

        Assert.Equal(50, ring.VirtualNodeCount);
        Assert.Equal(3, ring.Servers.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Add_WithVariousVirtualNodeCounts_CreatesCorrectNumberOfNodes(int virtualNodes)
    {
        var ring = new HashRing<string>();

        ring.Add("server1", virtualNodes);

        Assert.Equal(virtualNodes, ring.VirtualNodeCount);
    }

    #endregion

    #region Remove Method Comprehensive Tests

    [Fact]
    public void Remove_WithNullServer_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        const string server = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.Remove(server!));
        Assert.Equal("server", exception.ParamName);
    }

    [Fact]
    public void Remove_NonExistentServer_ReturnsFalse()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var removed = ring.Remove("server2");

        Assert.False(removed);
        Assert.Single(ring.Servers);
    }

    [Fact]
    public void Remove_ExistingServer_ReturnsTrueAndRemovesServer()
    {
        var ring = new HashRing<string>();
        ring.Add("server1", 10);
        ring.Add("server2", 20);

        var removed = ring.Remove("server1");

        Assert.True(removed);
        Assert.Single(ring.Servers);
        Assert.Equal(20, ring.VirtualNodeCount);
        Assert.DoesNotContain("server1", ring.Servers);
    }

    [Fact]
    public void Remove_LastServer_MakesRingEmpty()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var removed = ring.Remove("server1");

        Assert.True(removed);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    #endregion

    #region Contains Method Tests

    [Fact]
    public void Contains_WithNullServer_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        const string server = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.Contains(server!));
        Assert.Equal("server", exception.ParamName);
    }

    [Fact]
    public void Contains_ExistingServer_ReturnsTrue()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var contains = ring.Contains("server1");

        Assert.True(contains);
    }

    [Fact]
    public void Contains_NonExistentServer_ReturnsFalse()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var contains = ring.Contains("server2");

        Assert.False(contains);
    }

    #endregion

    #region Clear Method Tests

    [Fact]
    public void Clear_EmptyRing_RemainsEmpty()
    {
        var ring = new HashRing<string>();

        ring.Clear();

        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    [Fact]
    public void Clear_NonEmptyRing_BecomesEmpty()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        ring.Clear();

        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
        Assert.Empty(ring.Servers);
    }

    #endregion

    #region GetServer Method Comprehensive Tests

    [Fact]
    public void GetServer_WithNullKey_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        byte[] key = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.GetServer(key!));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetServer_EmptyRing_ThrowsInvalidOperationException()
    {
        var ring = new HashRing<string>();
        var key = Encoding.UTF8.GetBytes("test");

        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));
        Assert.Contains("No servers available", exception.Message);
    }

    [Fact]
    public void GetServer_SingleServer_AlwaysReturnsThatServer()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var keys = new[]
        {
            Encoding.UTF8.GetBytes("key1"),
            Encoding.UTF8.GetBytes("key2"),
            Encoding.UTF8.GetBytes("completely-different-key"),
            Encoding.UTF8.GetBytes(""),
            new byte[] { 0x00, 0xFF, 0x42, 0x13 }
        };

        foreach (var key in keys)
        {
            var server = ring.GetServer(key);
            Assert.Equal("server1", server);
        }
    }

    [Fact]
    public void GetServer_SameKey_ConsistentlyReturnsSameServer()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        var key = Encoding.UTF8.GetBytes("consistent-key");
        var server1 = ring.GetServer(key);

        // Call multiple times
        for (int i = 0; i < 10; i++)
        {
            var server = ring.GetServer(key);
            Assert.Equal(server1, server);
        }
    }

    [Fact]
    public void GetServer_EmptyKey_DoesNotThrow()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        var emptyKey = Array.Empty<byte>();

        var server = ring.GetServer(emptyKey);

        Assert.Equal("server1", server);
    }

    [Theory]
    [InlineData(new byte[] { 0x00 })]
    [InlineData(new byte[] { 0xFF })]
    [InlineData(new byte[] { 0x00, 0xFF })]
    [InlineData(new byte[] { 0x42, 0x13, 0x37 })]
    public void GetServer_BinaryKeys_HandlesCorrectly(byte[] binaryKey)
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        var server = ring.GetServer(binaryKey);

        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    #endregion

    #region TryGetServer Method Tests

    [Fact]
    public void TryGetServer_WithNullKey_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        byte[] key = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.TryGetServer(key!, out _));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void TryGetServer_EmptyRing_ReturnsFalse()
    {
        var ring = new HashRing<string>();
        var key = Encoding.UTF8.GetBytes("test");

        var result = ring.TryGetServer(key, out var server);

        Assert.False(result);
        Assert.Equal(default(string), server);
    }

    [Fact]
    public void TryGetServer_NonEmptyRing_ReturnsTrueWithServer()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        var key = Encoding.UTF8.GetBytes("test");

        var result = ring.TryGetServer(key, out var server);

        Assert.True(result);
        Assert.Equal("server1", server);
    }

    #endregion

    #region GetServers Method Tests

    [Fact]
    public void GetServers_WithNullKey_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        byte[] key = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.GetServers(key!, 1));
        Assert.Equal("key", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void GetServers_WithNegativeCount_ThrowsArgumentOutOfRangeException(int negativeCount)
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        var key = Encoding.UTF8.GetBytes("test");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers(key, negativeCount));
        Assert.Equal("count", exception.ParamName);
    }

    [Fact]
    public void GetServers_CountZero_ReturnsEmptyEnumerable()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        var key = Encoding.UTF8.GetBytes("test");

        var servers = ring.GetServers(key, 0).ToList();

        Assert.Empty(servers);
    }

    [Fact]
    public void GetServers_CountGreaterThanAvailable_ReturnsAllServers()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        var key = Encoding.UTF8.GetBytes("test");

        var servers = ring.GetServers(key, 10).ToList();

        Assert.Equal(2, servers.Count);
        Assert.Contains("server1", servers);
        Assert.Contains("server2", servers);
    }

    [Fact]
    public void GetServers_ReturnsServersInConsistentOrder()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        var key = Encoding.UTF8.GetBytes("test");

        var servers1 = ring.GetServers(key, 3).ToList();
        var servers2 = ring.GetServers(key, 3).ToList();

        Assert.Equal(servers1, servers2);
    }

    [Fact]
    public void GetServers_DoesNotReturnDuplicateServers()
    {
        var ring = new HashRing<string>();
        ring.Add("server1", 100); // Many virtual nodes
        ring.Add("server2", 100);
        ring.Add("server3", 100);
        var key = Encoding.UTF8.GetBytes("test");

        var servers = ring.GetServers(key, 10).ToList();

        var uniqueServers = servers.Distinct().ToList();
        Assert.Equal(uniqueServers.Count, servers.Count);
        Assert.True(uniqueServers.Count <= 3);
    }

    #endregion

    #region Version History Tests

    [Fact]
    public void CreateConfigurationSnapshot_VersionHistoryDisabled_ThrowsInvalidOperationException()
    {
        var ring = new HashRing<string>(); // Version history disabled by default

        var exception = Assert.Throws<InvalidOperationException>(() => ring.CreateConfigurationSnapshot());
        Assert.Contains("version history is not enabled", exception.Message);
    }

    [Fact]
    public void CreateConfigurationSnapshot_VersionHistoryEnabled_CreatesSnapshot()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");

        ring.CreateConfigurationSnapshot();

        Assert.Equal(1, ring.HistoryCount);
    }

    [Fact]
    public void ClearHistory_VersionHistoryDisabled_ThrowsInvalidOperationException()
    {
        var ring = new HashRing<string>();

        var exception = Assert.Throws<InvalidOperationException>(() => ring.ClearHistory());
        Assert.Contains("version history is not enabled", exception.Message);
    }

    [Fact]
    public void ClearHistory_VersionHistoryEnabled_ClearsHistory()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        ring.CreateConfigurationSnapshot();

        ring.ClearHistory();

        Assert.Equal(0, ring.HistoryCount);
    }

    #endregion

    #region Server Candidates Tests

    [Fact]
    public void GetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        byte[] key = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.GetServerCandidates(key!));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void GetServerCandidates_EmptyRingNoHistory_ThrowsInvalidOperationException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        var key = Encoding.UTF8.GetBytes("test");

        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServerCandidates(key));
        Assert.Contains("No servers are available", exception.Message);
    }

    [Fact]
    public void GetServerCandidates_WithMaxCandidatesNegative_ThrowsArgumentOutOfRangeException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        var key = Encoding.UTF8.GetBytes("test");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServerCandidates(key, -1));
        Assert.Equal("maxCandidates", exception.ParamName);
    }

    [Fact]
    public void GetServerCandidates_MaxCandidatesZero_ReturnsEmptyList()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        var key = Encoding.UTF8.GetBytes("test");

        var result = ring.GetServerCandidates(key, 0);

        Assert.Empty(result.Servers);
        Assert.Equal(1, result.ConfigurationCount);
        Assert.False(result.HasHistory);
    }

    [Fact]
    public void TryGetServerCandidates_WithNullKey_ThrowsArgumentNullException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        byte[] key = null;

        var exception = Assert.Throws<ArgumentNullException>(() => ring.TryGetServerCandidates(key!, out _));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void TryGetServerCandidates_EmptyRing_ReturnsFalse()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        var key = Encoding.UTF8.GetBytes("test");

        var success = ring.TryGetServerCandidates(key, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetServerCandidates_NonEmptyRing_ReturnsTrue()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        var key = Encoding.UTF8.GetBytes("test");

        var success = ring.TryGetServerCandidates(key, out var result);

        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result.Servers);
    }

    [Fact]
    public void GetServerCandidates_EmptyRingWithEmptyHistoricalSnapshots_ThrowsInvalidOperationException()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 5 };
        var ring = new HashRing<string>(options);
        var key = Encoding.UTF8.GetBytes("test");

        // Create empty snapshots by taking snapshots when the ring is empty
        // This is a legitimate scenario - the ring starts empty, we take a snapshot,
        // then it becomes empty again, we take another snapshot, etc.

        // Create first empty snapshot
        ring.CreateConfigurationSnapshot();

        // Add server, then remove it, then create another empty snapshot
        ring.Add("temp-server");
        ring.Remove("temp-server");
        ring.CreateConfigurationSnapshot();

        // Add server again, remove it, create third empty snapshot
        ring.Add("temp-server2");
        ring.Remove("temp-server2");
        ring.CreateConfigurationSnapshot();

        // At this point we have:
        // - Current configuration: empty (IsEmpty = true)
        // - Historical snapshots: 3 empty snapshots (hasHistory = true)
        // - All snapshot.GetServer(key) calls will throw InvalidOperationException
        // - candidates.Count will remain 0
        // - This should trigger the "No servers are available in any configuration" exception

        var exception = Assert.Throws<InvalidOperationException>(() => ring.GetServerCandidates(key));
        Assert.Equal("No servers are available in any configuration.", exception.Message);

        // Verify the ring state
        Assert.True(ring.IsEmpty);
        Assert.True(ring.IsVersionHistoryEnabled);
        Assert.Equal(3, ring.HistoryCount);
    }

    [Fact]
    public void GetServerCandidates_MaxCandidatesLowerThanServerCount_ReturnsLimitedCandidates()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 100 };
        var ring = new HashRing<string>(options);
        var key = Encoding.UTF8.GetBytes("test-key");

        // Create multiple configurations with different servers to build up candidates
        // Each configuration will provide 1 candidate for the key

        // Configuration 1: server1, server2
        ring.Add("server1");
        ring.Add("server2");
        ring.CreateConfigurationSnapshot();

        // Configuration 2: server3, server4
        ring.Remove("server1");
        ring.Remove("server2");
        ring.Add("server3");
        ring.Add("server4");
        ring.CreateConfigurationSnapshot();

        // Configuration 3: add server5 and server6
        ring.Remove("server3");
        ring.Remove("server4");
        ring.Add("server5");
        ring.Add("server6");
        ring.CreateConfigurationSnapshot();

        // Configuration 4: server7, server8
        ring.Remove("server5");
        ring.Remove("server6");
        ring.Add("server7");
        ring.Add("server8");
        ring.CreateConfigurationSnapshot();

        // Configuration 5: add server9 and server10
        ring.Remove("server7");
        ring.Remove("server8");
        ring.Add("server9");
        ring.Add("server10");

        // At this point we have current config + 4 historical snapshots = 5 configurations
        // Each configuration will map the key to one server, giving us up to 5 candidates

        // Get all candidates first to see what we're working with
        var allCandidates = ring.GetServerCandidates(key);

        // Now test limiting to 3 candidates when we potentially have 5
        var result = ring.GetServerCandidates(key, 3);

        Assert.True(result.Servers.Count <= 3, $"Expected at most 3 candidates, got {result.Servers.Count}");
        Assert.True(result.Servers.Count <= allCandidates.Servers.Count,
                   "Limited result should not have more servers than unlimited result");
        Assert.Equal(5, result.ConfigurationCount); // 1 current + 4 snapshots
        Assert.True(result.HasHistory);

        // Verify all returned servers are unique
        var uniqueServers = result.Servers.Distinct().ToList();
        Assert.Equal(result.Servers.Count, uniqueServers.Count);

        // Verify all returned servers exist in the ring or its history
        foreach (var server in result.Servers)
        {
            Assert.Contains(server, allCandidates.Servers);
        }
    }

    #endregion

    #region Atomic Operations Tests

    [Fact]
    public void AddRange_WithKeyValuePairs_AddsAllServersAtomically()
    {
        var ring = new HashRing<string>();
        var serversToAdd = new[]
        {
            new KeyValuePair<string, int>("server1", 10),
            new KeyValuePair<string, int>("server2", 20),
            new KeyValuePair<string, int>("server3", 15)
        };

        ring.AddRange(serversToAdd);

        Assert.Equal(3, ring.Servers.Count);
        Assert.Equal(45, ring.VirtualNodeCount); // 10 + 20 + 15
        Assert.Contains("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
        Assert.Contains("server3", ring.Servers);
    }

    [Fact]
    public void AddRange_WithServerList_AddsAllServersWithDefaultNodes()
    {
        var ring = new HashRing<string>();
        var serversToAdd = new[] { "server1", "server2", "server3" };

        ring.AddRange(serversToAdd);

        Assert.Equal(3, ring.Servers.Count);
        Assert.Equal(126, ring.VirtualNodeCount); // 3 * 42 (default)
        Assert.Contains("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
        Assert.Contains("server3", ring.Servers);
    }

    [Fact]
    public void AddRange_WithEmptyCollection_DoesNothing()
    {
        var ring = new HashRing<string>();
        ring.Add("existing-server");
        var initialCount = ring.Servers.Count;
        var initialVirtualNodes = ring.VirtualNodeCount;

        ring.AddRange(Array.Empty<string>());
        ring.AddRange(Array.Empty<KeyValuePair<string, int>>());

        Assert.Equal(initialCount, ring.Servers.Count);
        Assert.Equal(initialVirtualNodes, ring.VirtualNodeCount);
    }

    [Fact]
    public void AddRange_WithNullCollection_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();

        var exception1 = Assert.Throws<ArgumentNullException>(() => ring.AddRange((IEnumerable<string>)null!));
        Assert.Equal("servers", exception1.ParamName);

        var exception2 = Assert.Throws<ArgumentNullException>(() => ring.AddRange((IEnumerable<KeyValuePair<string, int>>)null!));
        Assert.Equal("servers", exception2.ParamName);
    }

    [Fact]
    public void AddRange_WithNullServer_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        var serversWithNull = new[] { "server1", null!, "server2" };

        var exception = Assert.Throws<ArgumentNullException>(() => ring.AddRange(serversWithNull));
        Assert.Equal("server", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void AddRange_WithInvalidVirtualNodeCount_ThrowsArgumentOutOfRangeException(int invalidCount)
    {
        var ring = new HashRing<string>();
        var serversWithInvalidCount = new[]
        {
            new KeyValuePair<string, int>("server1", 10),
            new KeyValuePair<string, int>("server2", invalidCount)
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ring.AddRange(serversWithInvalidCount));
        Assert.Contains("kvp.Value", exception.ParamName);
    }

    [Fact]
    public void AddRange_UpdatesExistingServers_ReplacesVirtualNodeCounts()
    {
        var ring = new HashRing<string>();
        ring.Add("server1", 10);
        ring.Add("server2", 20);

        var serversToUpdate = new[]
        {
            new KeyValuePair<string, int>("server1", 30), // Update existing
            new KeyValuePair<string, int>("server3", 15)  // Add new
        };

        ring.AddRange(serversToUpdate);

        Assert.Equal(3, ring.Servers.Count);
        Assert.Equal(65, ring.VirtualNodeCount); // 30 + 20 + 15
    }

    [Fact]
    public void RemoveRange_RemovesAllSpecifiedServers()
    {
        var ring = new HashRing<string>();
        ring.Add("server1", 10);
        ring.Add("server2", 20);
        ring.Add("server3", 15);
        ring.Add("server4", 25);

        var serversToRemove = new[] { "server1", "server3", "server5" }; // server5 doesn't exist

        var removedCount = ring.RemoveRange(serversToRemove);

        Assert.Equal(2, removedCount); // Only server1 and server3 were removed
        Assert.Equal(2, ring.Servers.Count);
        Assert.Equal(45, ring.VirtualNodeCount); // 20 + 25
        Assert.DoesNotContain("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
        Assert.DoesNotContain("server3", ring.Servers);
        Assert.Contains("server4", ring.Servers);
    }

    [Fact]
    public void RemoveRange_WithEmptyCollection_ReturnsZero()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");
        var initialCount = ring.Servers.Count;
        var initialVirtualNodes = ring.VirtualNodeCount;

        var removedCount = ring.RemoveRange([]);

        Assert.Equal(0, removedCount);
        Assert.Equal(initialCount, ring.Servers.Count);
        Assert.Equal(initialVirtualNodes, ring.VirtualNodeCount);
    }

    [Fact]
    public void RemoveRange_WithNullCollection_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();

        var exception = Assert.Throws<ArgumentNullException>(() => ring.RemoveRange((IEnumerable<string>)null!));
        Assert.Equal("servers", exception.ParamName);
    }

    [Fact]
    public void RemoveRange_WithNullServer_ThrowsArgumentNullException()
    {
        var ring = new HashRing<string>();
        var serversWithNull = new[] { "server1", null!, "server2" };

        var exception = Assert.Throws<ArgumentNullException>(() => ring.RemoveRange(serversWithNull));
        Assert.Equal("server", exception.ParamName);
    }

    [Fact]
    public void RemoveRange_WithNonExistentServers_ReturnsZero()
    {
        var ring = new HashRing<string>();
        ring.Add("server1");

        var removedCount = ring.RemoveRange(TestServerNames);

        Assert.Equal(0, removedCount);
        Assert.Single(ring.Servers);
        Assert.Contains("server1", ring.Servers);
    }

    [Fact]
    public void AtomicOperations_EnsureConsistency_NoPartialStates()
    {
        var ring = new HashRing<string>();
        var key = Encoding.UTF8.GetBytes("test-key");

        // Add some initial servers
        ring.Add("initial1");
        ring.Add("initial2");
        _ = ring.GetServer(key);

        // Add multiple servers atomically
        var serversToAdd = new[]
        {
            new KeyValuePair<string, int>("atomic1", 10),
            new KeyValuePair<string, int>("atomic2", 15),
            new KeyValuePair<string, int>("atomic3", 20)
        };

        ring.AddRange(serversToAdd);

        // Verify all were added
        Assert.Equal(5, ring.Servers.Count);
        Assert.Equal(129, ring.VirtualNodeCount); // 2*42 + 10 + 15 + 20

        // The key should still resolve to a server consistently
        var newServer = ring.GetServer(key);
        Assert.NotNull(newServer);

        // Remove multiple servers atomically
        var serversToRemove = new[] { "atomic1", "atomic3", "initial1" };
        var removedCount = ring.RemoveRange(serversToRemove);

        Assert.Equal(3, removedCount);
        Assert.Equal(2, ring.Servers.Count);
        Assert.Equal(57, ring.VirtualNodeCount); // 42 + 15

        // Verify the remaining servers are correct
        Assert.Contains("initial2", ring.Servers);
        Assert.Contains("atomic2", ring.Servers);
        Assert.DoesNotContain("atomic1", ring.Servers);
        Assert.DoesNotContain("atomic3", ring.Servers);
        Assert.DoesNotContain("initial1", ring.Servers);
    }

    [Fact]
    public async Task AtomicOperations_ThreadSafety_ConcurrentOperations()
    {
        var ring = new HashRing<string>();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Add initial servers
        for (int i = 0; i < 5; i++)
        {
            ring.Add($"initial-{i}");
        }

        // Concurrent AddRange operations
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var servers = Enumerable.Range(0, 3)
                        .Select(j => $"task{taskId}-server{j}")
                        .ToArray();
                    ring.AddRange(servers);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        // Concurrent RemoveRange operations
        for (int i = 0; i < 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var serversToRemove = new[] { $"task{taskId}-server0", $"task{taskId}-server1" };
                    ring.RemoveRange(serversToRemove);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        if (exceptions.Count > 0)
        {
            throw new AggregateException("Concurrent atomic operations failed", exceptions);
        }

        // Verify ring is in a consistent state
        Assert.True(ring.Servers.Count >= 5, "Should have at least the initial servers");
        foreach (var server in ring.Servers)
        {
            Assert.True(ring.Contains(server), $"Server {server} should be contained in the ring");
        }
    }

    #endregion

    #region Complex Integration Tests

    [Fact]
    public void ComplexWorkflow_AddRemoveWithHistory_MaintainsConsistency()
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = 10 };
        var ring = new HashRing<string>(options);
        var testKey = Encoding.UTF8.GetBytes("integration-test");

        // Phase 1: Initial setup
        ring.Add("server1");
        ring.Add("server2");
        var initialServer = ring.GetServer(testKey);
        ring.CreateConfigurationSnapshot();

        // Phase 2: Add server
        ring.Add("server3");
        var candidates1 = ring.GetServerCandidates(testKey);
        Assert.Contains(initialServer, candidates1.Servers); // Should still include original

        // Phase 3: Remove server
        ring.CreateConfigurationSnapshot();
        ring.Remove("server1");
        var candidates2 = ring.GetServerCandidates(testKey);

        // Verify history is maintained
        Assert.True(ring.HistoryCount > 0);
        Assert.True(candidates2.HasHistory);
        Assert.True(candidates2.ConfigurationCount > 1);
    }

    [Fact]
    public void LargeScale_ManyServersAndKeys_PerformsCorrectly()
    {
        var ring = new HashRing<string>();

        // Add 100 servers
        for (int i = 0; i < 100; i++)
        {
            ring.Add($"server-{i:D3}");
        }

        // Test 1000 different keys
        var serverCounts = new Dictionary<string, int>();
        for (int i = 0; i < 1000; i++)
        {
            var key = Encoding.UTF8.GetBytes($"key-{i}");
            var server = ring.GetServer(key);
            serverCounts[server] = serverCounts.GetValueOrDefault(server, 0) + 1;
        }

        // Verify distribution is reasonable (each server should get some keys)
        Assert.True(serverCounts.Count > 50, "Distribution should use many servers");

        // Verify no server gets more than 5% of keys (good distribution)
        var maxCount = serverCounts.Values.Max();
        Assert.True(maxCount <= 50, $"No server should handle more than 50 keys, max was {maxCount}");
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentOperations_NoExceptions()
    {
        var ring = new HashRing<string>();
        var exceptions = new List<Exception>();
        var tasks = new List<Task>();

        // Add initial servers
        for (int i = 0; i < 10; i++)
        {
            ring.Add($"initial-server-{i}");
        }

        // Concurrent read operations
        for (int i = 0; i < 50; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var key = Encoding.UTF8.GetBytes($"thread-{taskId}-key-{j}");
                        var server = ring.GetServer(key);
                        Assert.NotNull(server);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        // Concurrent write operations
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    ring.Add($"concurrent-server-{taskId}");
                    Thread.Sleep(10);
                    ring.Remove($"concurrent-server-{taskId}");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }, TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        if (exceptions.Count > 0)
        {
            throw new AggregateException("Concurrent operations failed", exceptions);
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 5)]
    [InlineData(100, 50)]
    public void HistoryLimit_ReachingLimit_ThrowsCorrectException(int maxSize, int snapshotsToCreate)
    {
        var options = new HashRingOptions { EnableVersionHistory = true, MaxHistorySize = maxSize };
        var ring = new HashRing<string>(options);
        ring.Add("server1");

        // Create snapshots up to the limit
        for (int i = 0; i < Math.Min(snapshotsToCreate, maxSize); i++)
        {
            ring.CreateConfigurationSnapshot();
            ring.Add($"server-{i + 2}");
        }

        if (snapshotsToCreate >= maxSize)
        {
            // Should throw when exceeding limit
            var exception = Assert.Throws<HashRingHistoryLimitExceededException>(() => ring.CreateConfigurationSnapshot());
            Assert.Equal(maxSize, exception.MaxHistorySize);
            Assert.Equal(maxSize, exception.CurrentCount);
        }
    }

    #endregion

    #region Edge Cases with Different Data Types

    [Fact]
    public void HashRing_WithIntegerType_WorksCorrectly()
    {
        var ring = new HashRing<int>();
        ring.Add(1);
        ring.Add(2);
        ring.Add(3);

        var key = Encoding.UTF8.GetBytes("test");
        var server = ring.GetServer(key);

        Assert.Contains(server, TestServers);
    }

    [Fact]
    public void HashRing_WithGuidType_WorksCorrectly()
    {
        var ring = new HashRing<Guid>();
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        ring.Add(guid1);
        ring.Add(guid2);

        var key = Encoding.UTF8.GetBytes("test");
        var server = ring.GetServer(key);

        Assert.Contains(server, new[] { guid1, guid2 });
    }

    [Fact]
    public void HashRing_WithCustomEquatableType_WorksCorrectly()
    {
        var ring = new HashRing<CustomServer>();
        var server1 = new CustomServer("server1", 8080);
        var server2 = new CustomServer("server2", 8081);

        ring.Add(server1);
        ring.Add(server2);

        var key = Encoding.UTF8.GetBytes("test");
        var server = ring.GetServer(key);

        Assert.Contains(server, new[] { server1, server2 });
    }

    #endregion

    #region Helper Classes

    private record CustomServer(string Host, int Port) : IEquatable<CustomServer>;

    #endregion
}
