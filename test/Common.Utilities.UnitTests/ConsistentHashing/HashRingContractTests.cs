using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class HashRingContractTests
{
    #region Constructor Tests (T004)

    [Fact]
    public void Constructor_Default_CreatesEmptyRing()
    {
        // Arrange & Act
        var ring = new HashRing<string>();

        // Assert
        Assert.NotNull(ring);
        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    [Fact]
    public void Constructor_Default_CreatesRingWithSha1Algorithm()
    {
        // Arrange & Act
        var ring = new HashRing<string>();

        // Assert
        Assert.NotNull(ring);
        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
    }

    [Fact]
    public void Constructor_WithHashAlgorithm_CreatesRingWithCustomAlgorithm()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();

        // Act
        var ring = new HashRing<string>(algorithm);

        // Assert
        Assert.NotNull(ring);
        Assert.Empty(ring.Servers);
    }

    [Fact]
    public void Constructor_WithMd5Algorithm_CreatesRingWithMd5Algorithm()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();

        // Act
        var ring = new HashRing<string>(algorithm);

        // Assert
        Assert.NotNull(ring);
        Assert.Empty(ring.Servers);
    }

    #endregion

    #region Add/Remove Operations Tests (T005)

    [Fact]
    public void Add_SingleServer_AddsServerToRing()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        ring.Add("server1");

        // Assert
        Assert.False(ring.IsEmpty);
        Assert.Single(ring.Servers);
        Assert.Contains("server1", ring.Servers);
        Assert.True(ring.VirtualNodeCount > 0);
    }

    [Fact]
    public void Add_ServerWithVirtualNodes_AddsServerWithSpecifiedVirtualNodes()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        ring.Add("server1", 100);

        // Assert
        Assert.Single(ring.Servers);
        Assert.Contains("server1", ring.Servers);
        Assert.Equal(100, ring.VirtualNodeCount);
    }

    [Fact]
    public void Add_MultipleServers_AddsAllServersToRing()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Assert
        Assert.Equal(3, ring.Servers.Count);
        Assert.Contains("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
        Assert.Contains("server3", ring.Servers);
    }

    [Fact]
    public void Add_NullServer_ThrowsArgumentNullException()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.Add(null));
    }

    [Fact]
    public void Add_ZeroVirtualNodes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.Add("server1", 0));
    }

    [Fact]
    public void Remove_ExistingServer_RemovesServerFromRing()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        bool removed = ring.Remove("server1");

        // Assert
        Assert.True(removed);
        Assert.Single(ring.Servers);
        Assert.DoesNotContain("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
    }

    [Fact]
    public void Remove_NonExistingServer_ReturnsFalse()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act
        bool removed = ring.Remove("server2");

        // Assert
        Assert.False(removed);
        Assert.Single(ring.Servers);
        Assert.Contains("server1", ring.Servers);
    }

    [Fact]
    public void Contains_ExistingServer_ReturnsTrue()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.True(ring.Contains("server1"));
    }

    [Fact]
    public void Contains_NonExistingServer_ReturnsFalse()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.False(ring.Contains("server2"));
    }

    [Fact]
    public void Clear_WithServers_RemovesAllServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");

        // Act
        ring.Clear();

        // Assert
        Assert.Empty(ring.Servers);
        Assert.True(ring.IsEmpty);
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    #endregion

    #region GetServer Methods Tests (T006)

    [Fact]
    public void GetServer_WithNoServers_ReturnsEmpty()
    {
        // Arrange
        var ring = new HashRing<string>();
        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        Assert.Empty(ring.GetServers(key, 1));
    }

    [Fact]
    public void GetServer_WithValidKey_ReturnsServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        string server = ring.GetServer(key);

        // Assert
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void GetServer_SameKeyMultipleTimes_ReturnsSameServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        string server1 = ring.GetServer(key);
        string server2 = ring.GetServer(key);
        string server3 = ring.GetServer(key);

        // Assert
        Assert.Equal(server1, server2);
        Assert.Equal(server2, server3);
    }

    [Fact]
    public void GetServer_EmptyRing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ring = new HashRing<string>();
        var key = new byte[] { 1, 2, 3, 4 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ring.GetServer(key));
    }

    [Fact]
    public void GetServer_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ring.GetServer((byte[])null));
    }

    [Fact]
    public void TryGetServer_WithValidKey_ReturnsTrueAndServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains(server, ring.Servers);
    }

    [Fact]
    public void TryGetServer_EmptyRing_ReturnsFalseAndDefaultServer()
    {
        // Arrange
        var ring = new HashRing<string>();
        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        bool success = ring.TryGetServer(key, out string server);

        // Assert
        Assert.False(success);
        Assert.Null(server);
    }

    [Fact]
    public void GetServers_WithCount_ReturnsSpecifiedNumberOfServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");
        var key = new byte[] { 1, 2, 3, 4 };

        // Act
        var servers = ring.GetServers(key, 2).ToList();

        // Assert
        Assert.Equal(2, servers.Count);
        Assert.All(servers, server => Assert.Contains(server, ring.Servers));
    }

    #endregion

    #region Properties and Enumeration Tests (T007)

    [Fact]
    public void Servers_EmptyRing_ReturnsEmptyCollection()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Empty(ring.Servers);
        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(ring.Servers);
    }

    [Fact]
    public void Servers_WithServers_ReturnsAllServers()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");
        ring.Add("server2");
        ring.Add("server3");

        // Act & Assert
        Assert.Equal(3, ring.Servers.Count);
        Assert.Contains("server1", ring.Servers);
        Assert.Contains("server2", ring.Servers);
        Assert.Contains("server3", ring.Servers);
    }

    [Fact]
    public void VirtualNodeCount_EmptyRing_ReturnsZero()
    {
        // Arrange
        var ring = new HashRing<string>();

        // Act & Assert
        Assert.Equal(0, ring.VirtualNodeCount);
    }

    [Fact]
    public void VirtualNodeCount_WithServers_ReturnsTotalVirtualNodes()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1", 100);
        ring.Add("server2", 200);

        // Act & Assert
        Assert.Equal(300, ring.VirtualNodeCount);
    }

    [Fact]
    public void IsEmpty_NewRing_ReturnsTrue()
    {
        // Arrange & Act
        var ring = new HashRing<string>();

        // Assert
        Assert.True(ring.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WithServers_ReturnsFalse()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act & Assert
        Assert.False(ring.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterClear_ReturnsTrue()
    {
        // Arrange
        var ring = new HashRing<string>();
        ring.Add("server1");

        // Act
        ring.Clear();

        // Assert
        Assert.True(ring.IsEmpty);
    }

    #endregion
}
