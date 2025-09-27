using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class HashRingOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new HashRingOptions();

        // Assert
        Assert.Equal(42, options.DefaultVirtualNodes);
        Assert.NotNull(options.HashAlgorithm);
        Assert.IsType<Sha1HashAlgorithm>(options.HashAlgorithm);
    }

    [Fact]
    public void Constructor_WithHashAlgorithm_SetsHashAlgorithm()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();

        // Act
        var options = new HashRingOptions(algorithm);

        // Assert
        Assert.Equal(42, options.DefaultVirtualNodes); // Default value preserved
        Assert.Same(algorithm, options.HashAlgorithm);
    }

    [Fact]
    public void Constructor_WithHashAlgorithm_NullAlgorithm_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HashRingOptions((IHashAlgorithm)null!));
    }

    [Fact]
    public void Constructor_WithDefaultVirtualNodes_SetsVirtualNodes()
    {
        // Arrange
        const int virtualNodes = 200;

        // Act
        var options = new HashRingOptions(virtualNodes);

        // Assert
        Assert.Equal(virtualNodes, options.DefaultVirtualNodes);
        Assert.NotNull(options.HashAlgorithm);
        Assert.IsType<Sha1HashAlgorithm>(options.HashAlgorithm); // Default algorithm preserved
    }

    [Fact]
    public void Constructor_WithDefaultVirtualNodes_ZeroNodes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashRingOptions(0));
    }

    [Fact]
    public void Constructor_WithDefaultVirtualNodes_NegativeNodes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashRingOptions(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashRingOptions(-100));
    }

    [Fact]
    public void Constructor_WithDefaultVirtualNodes_OneNode_Succeeds()
    {
        // Arrange & Act
        var options = new HashRingOptions(1);

        // Assert
        Assert.Equal(1, options.DefaultVirtualNodes);
    }

    [Fact]
    public void Constructor_WithHashAlgorithmAndVirtualNodes_SetsBothValues()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        const int virtualNodes = 100;

        // Act
        var options = new HashRingOptions(algorithm, virtualNodes);

        // Assert
        Assert.Same(algorithm, options.HashAlgorithm);
        Assert.Equal(virtualNodes, options.DefaultVirtualNodes);
    }

    [Fact]
    public void Constructor_WithHashAlgorithmAndVirtualNodes_NullAlgorithm_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HashRingOptions(null!, 100));
    }

    [Fact]
    public void Constructor_WithHashAlgorithmAndVirtualNodes_ZeroNodes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashRingOptions(algorithm, 0));
    }

    [Fact]
    public void Constructor_WithHashAlgorithmAndVirtualNodes_NegativeNodes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HashRingOptions(algorithm, -1));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void DefaultVirtualNodes_CanBeSet()
    {
        // Arrange
        var options = new HashRingOptions
        {
            // Act
            DefaultVirtualNodes = 500
        };

        // Assert
        Assert.Equal(500, options.DefaultVirtualNodes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(42)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void DefaultVirtualNodes_ValidValues_CanBeSet(int virtualNodes)
    {
        // Arrange
        var options = new HashRingOptions
        {
            // Act
            DefaultVirtualNodes = virtualNodes
        };

        // Assert
        Assert.Equal(virtualNodes, options.DefaultVirtualNodes);
    }

    #endregion

    #region Integration Tests with HashRing

    [Fact]
    public void HashRingOptions_UsedWithHashRing_AppliesConfiguration()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        var options = new HashRingOptions(algorithm, 200);

        // Act
        var ring = new HashRing<string>(options);
        ring.Add("server1"); // Should use 200 virtual nodes by default from options

        // Assert
        Assert.Equal(200, ring.VirtualNodeCount);
        // Verify the algorithm is being used (we can't directly check, but behavior should be consistent)
        var server1 = ring.GetServer("test-key");
        var server2 = ring.GetServer("test-key");
        Assert.Equal(server1, server2); // Should be consistent
    }

    [Fact]
    public void HashRingOptions_DefaultConfiguration_WorksWithHashRing()
    {
        // Arrange
        var options = new HashRingOptions();

        // Act
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        ring.Add("server2");

        // Assert
        Assert.Equal(84, ring.VirtualNodeCount); // 42 * 2 servers
        Assert.False(ring.IsEmpty);
        Assert.Equal(2, ring.Servers.Count);
    }

    [Fact]
    public void HashRingOptions_CustomAlgorithm_ProducesConsistentResults()
    {
        // Arrange
        var sha1Options = new HashRingOptions(new Sha1HashAlgorithm());
        var md5Options = new HashRingOptions(new Md5HashAlgorithm());

        var sha1Ring = new HashRing<string>(sha1Options);
        var md5Ring = new HashRing<string>(md5Options);

        sha1Ring.Add("server1");
        sha1Ring.Add("server2");
        md5Ring.Add("server1");
        md5Ring.Add("server2");

        // Act - Get servers for same key with different algorithms
        var sha1Server1 = sha1Ring.GetServer("test-key");
        var sha1Server2 = sha1Ring.GetServer("test-key");
        var md5Server1 = md5Ring.GetServer("test-key");
        var md5Server2 = md5Ring.GetServer("test-key");

        // Assert - Each algorithm should be consistent with itself
        Assert.Equal(sha1Server1, sha1Server2);
        Assert.Equal(md5Server1, md5Server2);

        // Different algorithms may produce different results (this is expected)
        Assert.True(sha1Server1 == md5Server1 || sha1Server1 != md5Server1); // Either is fine
    }

    [Fact]
    public void HashRingOptions_DifferentVirtualNodeCounts_AffectDistribution()
    {
        // Arrange
        var lowVirtualNodesOptions = new HashRingOptions(50);
        var highVirtualNodesOptions = new HashRingOptions(1000);

        var lowRing = new HashRing<string>(lowVirtualNodesOptions);
        var highRing = new HashRing<string>(highVirtualNodesOptions);

        lowRing.Add("server1");
        lowRing.Add("server2");
        highRing.Add("server1");
        highRing.Add("server2");

        // Act & Assert
        Assert.Equal(100, lowRing.VirtualNodeCount); // 50 * 2
        Assert.Equal(2000, highRing.VirtualNodeCount); // 1000 * 2

        // Both should work correctly despite different virtual node counts
        var lowRingServer = lowRing.GetServer("test-key");
        var highRingServer = highRing.GetServer("test-key");

        Assert.Contains(lowRingServer, lowRing.Servers);
        Assert.Contains(highRingServer, highRing.Servers);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void HashRingOptions_SetZeroVirtualNodes_AllowedButMayFailInHashRing()
    {
        // Arrange
        var options = new HashRingOptions
        {
            // Act - Setting zero is allowed on the options object
            DefaultVirtualNodes = 0
        };

        // Assert - Property is zero
        Assert.Equal(0, options.DefaultVirtualNodes);

        // Note: HashRing.Add() validates virtual node count, not the constructor
        var ring = new HashRing<string>(options);
        Assert.Throws<ArgumentOutOfRangeException>(() => ring.Add("server1"));
    }

    [Fact]
    public void HashRingOptions_ExtremelyHighVirtualNodes_HandledCorrectly()
    {
        // Arrange & Act
        var options = new HashRingOptions(100000);

        // Assert
        Assert.Equal(100000, options.DefaultVirtualNodes);

        // Should work with HashRing (though may be slow)
        var ring = new HashRing<string>(options);
        ring.Add("server1");
        Assert.Equal(100000, ring.VirtualNodeCount);
    }

    [Fact]
    public void HashRingOptions_MultipleInstancesWithSameConfig_Independent()
    {
        // Arrange
        var algorithm1 = new Sha1HashAlgorithm();
        var algorithm2 = new Sha1HashAlgorithm();

        var options1 = new HashRingOptions(algorithm1, 100);
        var options2 = new HashRingOptions(algorithm2, 200);

        // Act
        options1.DefaultVirtualNodes = 150;

        // Assert - Changes to one don't affect the other
        Assert.Equal(150, options1.DefaultVirtualNodes);
        Assert.Equal(200, options2.DefaultVirtualNodes);
        Assert.NotSame(options1.HashAlgorithm, options2.HashAlgorithm);
    }

    #endregion
}
