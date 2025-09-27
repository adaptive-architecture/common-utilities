using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class HashRingExtensionsContractTests
{
    #region String Key Extension Tests

    [Fact]
    public void GetServer_StringKey_ReturnsServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        string server = ring.GetServer("user123");

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void GetServer_SameStringKeyMultipleTimes_ReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        string server1 = ring.GetServer("user123");
        string server2 = ring.GetServer("user123");
        string server3 = ring.GetServer("user123");

        // Assert
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_StringKey_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.GetServer((string)null));
    }

    [Fact]
    public void GetServer_StringKey_EmptyRing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ring.GetServer("user123"));
    }

    [Fact]
    public void TryGetServer_StringKey_WithValidKey_ReturnsTrueAndServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act
        bool success = ring.TryGetServer("user123", out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_StringKey_EmptyRing_ReturnsFalseAndNull()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        bool success = ring.TryGetServer("user123", out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    #endregion

    #region Guid Key Extension Tests

    [Fact]
    public void GetServer_GuidKey_ReturnsServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        var guid = Guid.NewGuid();

        // Act
        string server = ring.GetServer(guid);

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void GetServer_SameGuidKeyMultipleTimes_ReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        var guid = Guid.NewGuid();

        // Act
        string server1 = ring.GetServer(guid);
        string server2 = ring.GetServer(guid);
        string server3 = ring.GetServer(guid);

        // Assert
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_GuidKey_EmptyRing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();
        var guid = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ring.GetServer(guid));
    }

    [Fact]
    public void TryGetServer_GuidKey_WithValidKey_ReturnsTrueAndServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        var guid = Guid.NewGuid();

        // Act
        bool success = ring.TryGetServer(guid, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_GuidKey_EmptyRing_ReturnsFalseAndNull()
    {
        // Arrange
        var ring = new HashRing<string>();
        var guid = Guid.NewGuid();

        // Act
        bool success = ring.TryGetServer(guid, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    #endregion

    #region Integer Key Extension Tests

    [Fact]
    public void GetServer_IntKey_ReturnsServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        string server = ring.GetServer(12345);

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void GetServer_SameIntKeyMultipleTimes_ReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        string server1 = ring.GetServer(12345);
        string server2 = ring.GetServer(12345);
        string server3 = ring.GetServer(12345);

        // Assert
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_IntKey_EmptyRing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ring.GetServer(12345));
    }

    #endregion

    #region Long Key Extension Tests

    [Fact]
    public void GetServer_LongKey_ReturnsServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        string server = ring.GetServer(1234567890L);

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void GetServer_SameLongKeyMultipleTimes_ReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        string server1 = ring.GetServer(1234567890L);
        string server2 = ring.GetServer(1234567890L);
        string server3 = ring.GetServer(1234567890L);

        // Assert
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_LongKey_EmptyRing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ring.GetServer(1234567890L));
    }

    #endregion

    #region GetServers Multiple Extension Tests

    [Fact]
    public void GetServers_StringKey_WithCount_ReturnsSpecifiedNumberOfServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        var servers = ring.GetServers("user123", 2).ToList();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
    }

    [Fact]
    public void GetServers_StringKey_SameKeyMultipleTimes_ReturnsSameServersInSameOrder()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        var servers1 = ring.GetServers("user123", 2).ToList();
        var servers2 = ring.GetServers("user123", 2).ToList();

        // Assert
        Assert.Equal(servers1.Count, servers2.Count);
        for (int i = 0; i < servers1.Count; i++)
        {
            Assert.Equal(servers1[i], servers2[i]);
        }
    }

    [Fact]
    public void GetServers_StringKey_CountGreaterThanAvailableServers_ReturnsAllServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        var servers = ring.GetServers("user123", 5).ToList();

        // Assert
        Assert.Equal(2, servers.Count); // Should return only available servers
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
    }

    #endregion

    #region Extension Methods Cross-Type Consistency

    [Fact]
    public void ExtensionMethods_DifferentKeyTypes_CanReturnDifferentServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act
        string stringKeyServer = ring.GetServer("123");
        string intKeyServer = ring.GetServer(123);
        string guidKeyServer = ring.GetServer(Guid.Parse("12345678-1234-1234-1234-123456789012"));

        // Assert - Different key types may map to different servers (this is expected)
        Assert.NotNull(stringKeyServer);
        Assert.NotNull(intKeyServer);
        Assert.NotNull(guidKeyServer);
    }

    [Fact]
    public void ExtensionMethods_AllTypesWorkWithSameRing()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act & Assert - All should work without throwing exceptions
        Assert.NotNull(ring.GetServer("test"));
        Assert.NotNull(ring.GetServer(42));
        Assert.NotNull(ring.GetServer(42L));
        Assert.NotNull(ring.GetServer(Guid.NewGuid()));
    }

    #endregion
}
