using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class HashRingExtensionsComprehensiveTests
{
    #region TryGetServer Comprehensive Tests

    [Fact]
    public void TryGetServer_IntKey_WithValidKey_ReturnsTrueAndServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        bool success = ring.TryGetServer(12345, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_IntKey_EmptyRing_ReturnsFalseAndNull()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        bool success = ring.TryGetServer(12345, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void TryGetServer_IntKey_ConsistencyCheck()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        const int testKey = 67890;

        // Act
        bool success1 = ring.TryGetServer(testKey, out string server1);
        bool success2 = ring.TryGetServer(testKey, out string server2);
        bool success3 = ring.TryGetServer(testKey, out string server3);

        // Assert
        Assert.True(success1);
        Assert.True(success2);
        Assert.True(success3);
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void TryGetServer_LongKey_WithValidKey_ReturnsTrueAndServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        bool success = ring.TryGetServer(1234567890123L, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_LongKey_EmptyRing_ReturnsFalseAndNull()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        bool success = ring.TryGetServer(1234567890123L, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void TryGetServer_LongKey_ConsistencyCheck()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        const long testKey = 9876543210987L;

        // Act
        bool success1 = ring.TryGetServer(testKey, out string server1);
        bool success2 = ring.TryGetServer(testKey, out string server2);
        bool success3 = ring.TryGetServer(testKey, out string server3);

        // Assert
        Assert.True(success1);
        Assert.True(success2);
        Assert.True(success3);
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(Int32.MaxValue)]
    [InlineData(Int32.MinValue)]
    public void TryGetServer_IntKey_EdgeValues_WorksCorrectly(int key)
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(Int64.MaxValue)]
    [InlineData(Int64.MinValue)]
    public void TryGetServer_LongKey_EdgeValues_WorksCorrectly(long key)
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_StringKey_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.TryGetServer((string)null!, out string server));
    }

    [Fact]
    public void TryGetServer_AllKeyTypes_WithNullRing_ThrowsArgumentNullException()
    {
        // Arrange
        HashRing<string> ring = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.TryGetServer("test", out string server));
        Assert.Throws<ArgumentNullException>(() => ring.TryGetServer(123, out string server));
        Assert.Throws<ArgumentNullException>(() => ring.TryGetServer(123L, out string server));
        Assert.Throws<ArgumentNullException>(() => ring.TryGetServer(Guid.NewGuid(), out string server));
    }

    #endregion

    #region GetServers Comprehensive Tests

    [Fact]
    public void GetServers_StringKey_WithCount_ReturnsCorrectServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");

        // Act
        var servers = ring.GetServers("test-key", 3).ToList();

        // Assert
        Assert.Equal(3, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
        // Servers should be unique
        Assert.Equal(servers.Count, servers.Distinct().Count());
    }

    [Fact]
    public void GetServers_GuidKey_WithCount_ReturnsCorrectServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");
        var guid = Guid.Parse("12345678-9012-3456-7890-123456789012");

        // Act
        var servers = ring.GetServers(guid, 3).ToList();

        // Assert
        Assert.Equal(3, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
        Assert.Equal(servers.Count, servers.Distinct().Count());
    }

    [Fact]
    public void GetServers_IntKey_WithCount_ReturnsCorrectServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");

        // Act
        var servers = ring.GetServers(54321, 2).ToList();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
        Assert.Equal(servers.Count, servers.Distinct().Count());
    }

    [Fact]
    public void GetServers_LongKey_WithCount_ReturnsCorrectServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");

        // Act
        var servers = ring.GetServers(9876543210L, 2).ToList();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
        Assert.Equal(servers.Count, servers.Distinct().Count());
    }

    [Fact]
    public void GetServers_StringKey_CountExceedsAvailableServers_ReturnsAllServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        var servers = ring.GetServers("test-key", 10).ToList();

        // Assert
        Assert.Equal(2, servers.Count); // Only 2 servers available
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
        Assert.Equal(servers.Count, servers.Distinct().Count());
    }

    [Fact]
    public void GetServers_AllKeyTypes_EmptyRing_ReturnsEmptyEnumerable()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Empty(ring.GetServers("test", 1));
        Assert.Empty(ring.GetServers(123, 1));
        Assert.Empty(ring.GetServers(123L, 1));
        Assert.Empty(ring.GetServers(Guid.NewGuid(), 1));
    }

    [Fact]
    public void GetServers_StringKey_CountZero_ReturnsEmptyEnumerable()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        var servers = ring.GetServers("test-key", 0).ToList();

        // Assert
        Assert.Empty(servers);
    }

    [Fact]
    public void GetServers_StringKey_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers("test", -1).ToList());
    }

    [Fact]
    public void GetServers_AllKeyTypes_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers("test", -1).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers(123, -1).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers(123L, -1).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.GetServers(Guid.NewGuid(), -1).ToList());
    }

    [Fact]
    public void GetServers_StringKey_ConsistencyCheck()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        const string testKey = "consistency-test";

        // Act
        var servers1 = ring.GetServers(testKey, 2).ToList();
        var servers2 = ring.GetServers(testKey, 2).ToList();
        var servers3 = ring.GetServers(testKey, 2).ToList();

        // Assert - Results should be identical
        Assert.Equal(servers1.Count, servers2.Count);
        Assert.Equal(servers2.Count, servers3.Count);
        for (int i = 0; i < servers1.Count; i++)
        {
            Assert.Equal(servers1[i], servers2[i]);
            Assert.Equal(servers2[i], servers3[i]);
        }
    }

    [Fact]
    public void GetServers_AllKeyTypes_ReturnsInSamePreferenceOrder()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        ring.Add("server4");

        const string stringKey = "test";
        const int intKey = 12345;
        const long longKey = 1234567890L;
        var guidKey = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var stringServers = ring.GetServers(stringKey, 3).ToList();
        var intServers = ring.GetServers(intKey, 3).ToList();
        var longServers = ring.GetServers(longKey, 3).ToList();
        var guidServers = ring.GetServers(guidKey, 3).ToList();

        // Assert - Each key type should consistently return servers in the same order
        var stringServers2 = ring.GetServers(stringKey, 3).ToList();
        Assert.Equal(stringServers, stringServers2);

        var intServers2 = ring.GetServers(intKey, 3).ToList();
        Assert.Equal(intServers, intServers2);

        var longServers2 = ring.GetServers(longKey, 3).ToList();
        Assert.Equal(longServers, longServers2);

        var guidServers2 = ring.GetServers(guidKey, 3).ToList();
        Assert.Equal(guidServers, guidServers2);
    }

    [Fact]
    public void GetServers_StringKey_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.GetServers((string)null!, 1).ToList());
    }

    [Fact]
    public void GetServers_AllKeyTypes_NullRing_ThrowsArgumentNullException()
    {
        // Arrange
        HashRing<string> ring = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.GetServers("test", 1).ToList());
        Assert.Throws<ArgumentNullException>(() => ring.GetServers(123, 1).ToList());
        Assert.Throws<ArgumentNullException>(() => ring.GetServers(123L, 1).ToList());
        Assert.Throws<ArgumentNullException>(() => ring.GetServers(Guid.NewGuid(), 1).ToList());
    }

    #endregion

    #region GetServers Enumeration Behavior Tests

    [Fact]
    public void GetServers_StringKey_EnumeratedMultipleTimes_ReturnsSameResults()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        var serversEnumerable = ring.GetServers("test-key", 2);
        var firstEnumeration = serversEnumerable.ToList();
        var secondEnumeration = serversEnumerable.ToList();

        // Assert
        Assert.Equal(firstEnumeration, secondEnumeration);
    }

    [Fact]
    public void GetServers_StringKey_LazyEvaluation_WorksCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act - Get enumerable but don't enumerate
        var serversEnumerable = ring.GetServers("test-key", 2);

        // Add more servers after getting enumerable
        ring.Add("server3");
        ring.Add("server4");

        // Now enumerate
        var servers = serversEnumerable.ToList();

        // Assert - Should reflect the state when enumerated, not when created
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
    }

    [Fact]
    public void GetServers_StringKey_FirstServerMatchesGetServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        const string testKey = "consistency-check";

        // Act
        var singleServer = ring.GetServer(testKey);
        var multipleServers = ring.GetServers(testKey, 3).ToList();

        // Assert
        Assert.Equal(singleServer, multipleServers[0]);
    }

    [Fact]
    public void GetServers_AllKeyTypes_FirstServerMatchesGetServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        const string stringKey = "test";
        const int intKey = 42;
        const long longKey = 1234567890L;
        var guidKey = Guid.NewGuid();

        // Act & Assert
        Assert.Equal(ring.GetServer(stringKey), ring.GetServers(stringKey, 1).First());
        Assert.Equal(ring.GetServer(intKey), ring.GetServers(intKey, 1).First());
        Assert.Equal(ring.GetServer(longKey), ring.GetServers(longKey, 1).First());
        Assert.Equal(ring.GetServer(guidKey), ring.GetServers(guidKey, 1).First());
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void Extensions_WithSingleServer_AllMethodsWorkCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("only-server");

        const string stringKey = "test";
        const int intKey = 123;
        const long longKey = 123L;
        var guidKey = Guid.NewGuid();

        // Act & Assert - GetServer methods
        Assert.Equal("only-server", ring.GetServer(stringKey));
        Assert.Equal("only-server", ring.GetServer(intKey));
        Assert.Equal("only-server", ring.GetServer(longKey));
        Assert.Equal("only-server", ring.GetServer(guidKey));

        // Act & Assert - TryGetServer methods
        Assert.True(ring.TryGetServer(stringKey, out var stringServer));
        Assert.Equal("only-server", stringServer);

        Assert.True(ring.TryGetServer(intKey, out var intServer));
        Assert.Equal("only-server", intServer);

        Assert.True(ring.TryGetServer(longKey, out var longServer));
        Assert.Equal("only-server", longServer);

        Assert.True(ring.TryGetServer(guidKey, out var guidServer));
        Assert.Equal("only-server", guidServer);

        // Act & Assert - GetServers methods
        Assert.Single(ring.GetServers(stringKey, 1));
        Assert.Single(ring.GetServers(intKey, 1));
        Assert.Single(ring.GetServers(longKey, 1));
        Assert.Single(ring.GetServers(guidKey, 1));

        Assert.Equal("only-server", ring.GetServers(stringKey, 1).First());
        Assert.Equal("only-server", ring.GetServers(intKey, 1).First());
        Assert.Equal("only-server", ring.GetServers(longKey, 1).First());
        Assert.Equal("only-server", ring.GetServers(guidKey, 1).First());
    }

    [Fact]
    public void Extensions_SpecialStringValues_HandledCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        var specialStrings = new[]
        {
            "",                    // Empty string
            " ",                   // Space
            "\t",                  // Tab
            "\n",                  // Newline
            "\r\n",               // CRLF
            "🚀🌟💫",              // Emojis
            "Hello\0World",       // Null character
            "测试中文",             // Chinese characters
            "🏠🚗🎸🌮",            // Mixed emojis
            "a".PadRight(1000, 'x'), // Very long string
        };

        // Act & Assert
        foreach (var specialString in specialStrings)
        {
            var server = ring.GetServer(specialString);
            Assert.Contains(server, ring.Servers);

            Assert.True(ring.TryGetServer(specialString, out var tryServer));
            Assert.Equal(server, tryServer);

            var servers = ring.GetServers(specialString, 1).ToList();
            Assert.Single(servers);
            Assert.Equal(server, servers[0]);
        }
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")] // Empty GUID
    [InlineData("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")] // Max GUID
    [InlineData("12345678-1234-1234-1234-123456789012")] // Fixed GUID
    public void Extensions_SpecialGuidValues_HandledCorrectly(string guidString)
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        var guid = Guid.Parse(guidString);

        // Act
        var server = ring.GetServer(guid);
        var trySuccess = ring.TryGetServer(guid, out var tryServer);
        var servers = ring.GetServers(guid, 2).ToList();

        // Assert
        Assert.Contains(server, ring.Servers);
        Assert.True(trySuccess);
        Assert.Equal(server, tryServer);
        Assert.NotEmpty(servers);
        Assert.Equal(server, servers[0]);
    }

    [Fact]
    public void Extensions_AfterServerModifications_StillWorkCorrectly()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        const string testKey = "modification-test";

        // Act & Assert - Initial state
        var initialServer = ring.GetServer(testKey);
        Assert.Contains(initialServer, ring.Servers);

        // Add more servers
        ring.Add("server3");
        ring.Add("server4");

        var afterAddServer = ring.GetServer(testKey);
        Assert.Contains(afterAddServer, ring.Servers);

        // Remove a server
        ring.Remove("server1");

        var afterRemoveServer = ring.GetServer(testKey);
        Assert.Contains(afterRemoveServer, ring.Servers);
        Assert.DoesNotContain("server1", ring.Servers);

        // Clear and re-add
        ring.Clear();
        ring.Add("new-server");

        var afterClearServer = ring.GetServer(testKey);
        Assert.Equal("new-server", afterClearServer);
    }

    [Fact]
    public void Extensions_CustomEquatableType_WorksCorrectly()
    {
        // Arrange
        var ring = new HashRing<ServerRecord>();
        ring.Add(new ServerRecord("host1", 8080));
        ring.Add(new ServerRecord("host2", 8080));
        ring.Add(new ServerRecord("host3", 9090));

        const string testKey = "custom-type-test";

        // Act
        var server = ring.GetServer(testKey);
        var trySuccess = ring.TryGetServer(testKey, out var tryServer);
        var servers = ring.GetServers(testKey, 2).ToList();

        // Assert
        Assert.Contains(server, ring.Servers);
        Assert.True(trySuccess);
        Assert.Equal(server, tryServer);
        Assert.Equal(2, servers.Count);
        Assert.Equal(server, servers[0]);
        Assert.All(servers, s => Assert.Contains(s, ring.Servers));
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void Extensions_ManyKeys_PerformanceIsReasonable()
    {
        // Arrange
        var ring = new HashRing<string>();
        for (int i = 1; i <= 10; i++)
        {
            ring.Add($"server{i}");
        }

        const int keyCount = 1000;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Test different key types
        for (int i = 0; i < keyCount; i++)
        {
            ring.GetServer($"string-key-{i}");
            ring.GetServer(i);
            ring.GetServer((long)i);
            ring.TryGetServer($"try-string-{i}", out _);
            ring.TryGetServer(i + keyCount, out _);
            ring.GetServers($"multi-{i}", 3).Take(2).ToList();
        }

        stopwatch.Stop();

        // Assert - Should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Extension methods took too long: {stopwatch.ElapsedMilliseconds}ms for {keyCount * 6} operations");
    }

    [Fact]
    public void Extensions_Distribution_IsReasonablyBalanced()
    {
        // Arrange
        var ring = new HashRing<string>();
        var servers = new[] { "server1", "server2", "server3", "server4", "server5" };
        foreach (var server in servers)
        {
            ring.Add(server);
        }

        var distribution = new Dictionary<string, int>();
        foreach (var server in servers)
        {
            distribution[server] = 0;
        }

        const int sampleSize = 1000;

        // Act - Test distribution with string keys
        for (int i = 0; i < sampleSize; i++)
        {
            var server = ring.GetServer($"key-{i}");
            distribution[server]++;
        }

        // Assert - Each server should get roughly 20% of keys (within reasonable bounds)
        foreach (var kvp in distribution)
        {
            var percentage = (kvp.Value * 100.0) / sampleSize;
            Assert.True(percentage >= 5 && percentage <= 35,
                $"Server {kvp.Key} got {percentage:F1}% of keys, expected 10-30%");
        }
    }

    #endregion

    // Helper record for custom equatable type testing
    private record ServerRecord(string Host, int Port) : IEquatable<ServerRecord>;
}
