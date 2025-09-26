namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

using AdaptArch.Common.Utilities.ConsistentHashing;
using System;
using System.Collections.Generic;
using Xunit;

public sealed class ConfigurationSnapshotTests
{
    private static readonly IHashAlgorithm DefaultHashAlgorithm = new Sha1HashAlgorithm();
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        var servers = new[] { "server1", "server2" };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(123456789, "server1"),
            new(987654321, "server2")
        };
        var timestamp = DateTime.UtcNow;

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, timestamp, DefaultHashAlgorithm);

        Assert.Equal(servers, snapshot.Servers);
        Assert.Equal(virtualNodes, snapshot.VirtualNodes);
        Assert.Equal(timestamp, snapshot.CreatedAt);
    }

    [Fact]
    public void Constructor_WithNullServers_ThrowsArgumentNullException()
    {
        string[] servers = null!;
        var virtualNodes = new List<VirtualNode<string>>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm));

        Assert.Equal("servers", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullVirtualNodes_ThrowsArgumentNullException()
    {
        var servers = new[] { "server1" };
        List<VirtualNode<string>> virtualNodes = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm));

        Assert.Equal("virtualNodes", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyServers_CreatesValidSnapshot()
    {
        var servers = Array.Empty<string>();
        var virtualNodes = new List<VirtualNode<string>>();

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.Empty(snapshot.Servers);
        Assert.Empty(snapshot.VirtualNodes);
    }

    [Fact]
    public void Constructor_WithEmptyVirtualNodes_CreatesValidSnapshot()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>();

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.Single(snapshot.Servers);
        Assert.Empty(snapshot.VirtualNodes);
    }

    [Fact]
    public void Servers_IsReadOnly()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>();

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.IsType<IReadOnlyList<string>>(snapshot.Servers, exactMatch: false);
    }

    [Fact]
    public void VirtualNodes_IsReadOnly()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(123456789, "server1")
        };

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.IsType<IReadOnlyList<VirtualNode<string>>>(snapshot.VirtualNodes, exactMatch: false);
    }

    [Fact]
    public void GetServer_WithValidKey_ReturnsExpectedServer()
    {
        var servers = new[] { "server1", "server2" };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(200, "server2")
        };

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);
        var key = new byte[] { 1, 2, 3 };

        var result = snapshot.GetServer(key);

        Assert.NotNull(result);
        Assert.Contains(result, servers);
    }

    [Fact]
    public void GetServer_WithEmptyVirtualNodes_ThrowsInvalidOperationException()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>();

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);
        var key = new byte[] { 1, 2, 3 };

        Assert.Throws<InvalidOperationException>(() => snapshot.GetServer(key));
    }

    [Fact]
    public void GetServer_WithNullKey_ThrowsArgumentNullException()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1")
        };

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);
        byte[] key = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => snapshot.GetServer(key));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public void ServerCount_ReturnsCorrectCount()
    {
        var servers = new[] { "server1", "server2", "server3" };
        var virtualNodes = new List<VirtualNode<string>>();

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.Equal(3, snapshot.ServerCount);
    }

    [Fact]
    public void VirtualNodeCount_ReturnsCorrectCount()
    {
        var servers = new[] { "server1" };
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(200, "server1"),
            new(300, "server1")
        };

        var snapshot = new ConfigurationSnapshot<string>(servers, virtualNodes, DateTime.UtcNow, DefaultHashAlgorithm);

        Assert.Equal(3, snapshot.VirtualNodeCount);
    }
}
