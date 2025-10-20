using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

/// <summary>
/// Tests for the FindServerIndex method in HashRing.
/// This tests the binary search implementation directly.
/// </summary>
public sealed class HashRingFindServerIndexTests
{
    [Fact]
    public void FindServerIndex_WithExactHashMatch_ReturnsExactIndex()
    {
        // Arrange - Create virtual nodes with specific hash values
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(200, "server2"),
            new(300, "server3"),
            new(400, "server4"),
            new(500, "server5")
        };

        // Act - Search for exact hash matches (this hits lines 429-430)
        var index1 = HashRing<string>.FindServerIndex(virtualNodes, 100);
        var index2 = HashRing<string>.FindServerIndex(virtualNodes, 200);
        var index3 = HashRing<string>.FindServerIndex(virtualNodes, 300);
        var index4 = HashRing<string>.FindServerIndex(virtualNodes, 400);
        var index5 = HashRing<string>.FindServerIndex(virtualNodes, 500);

        // Assert - Should return exact indices
        Assert.Equal(0, index1);
        Assert.Equal(1, index2);
        Assert.Equal(2, index3);
        Assert.Equal(3, index4);
        Assert.Equal(4, index5);
    }

    [Fact]
    public void FindServerIndex_WithHashBetweenNodes_ReturnsNextHigherIndex()
    {
        // Arrange
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(300, "server2"),
            new(500, "server3")
        };


        // Act - Search for hashes between nodes
        var index1 = HashRing<string>.FindServerIndex(virtualNodes, 150); // Between 100 and 300
        var index2 = HashRing<string>.FindServerIndex(virtualNodes, 350); // Between 300 and 500

        // Assert - Should return index of next higher node
        Assert.Equal(1, index1); // Should return index of node with hash 300
        Assert.Equal(2, index2); // Should return index of node with hash 500
    }

    [Fact]
    public void FindServerIndex_WithHashLowerThanAll_ReturnsZero()
    {
        // Arrange
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(200, "server2"),
            new(300, "server3")
        };

        // Act
        var index = HashRing<string>.FindServerIndex(virtualNodes, 50);

        // Assert - Hash is lower than all nodes, should return 0
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindServerIndex_WithHashHigherThanAll_ReturnsZero()
    {
        // Arrange
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(100, "server1"),
            new(200, "server2"),
            new(300, "server3")
        };

        // Act
        var index = HashRing<string>.FindServerIndex(virtualNodes, 500);

        // Assert - Hash is higher than all nodes, should wrap to 0
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindServerIndex_WithSingleNode_ExactMatch_ReturnsZero()
    {
        // Arrange
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(250, "server1")
        };

        // Act - Exact match with single node (hits lines 429-430)
        var index = HashRing<string>.FindServerIndex(virtualNodes, 250);

        // Assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void FindServerIndex_WithSingleNode_NoMatch_ReturnsZero()
    {
        // Arrange
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(250, "server1")
        };

        // Act
        var indexBefore = HashRing<string>.FindServerIndex(virtualNodes, 100);
        var indexAfter = HashRing<string>.FindServerIndex(virtualNodes, 300);

        // Assert - Should return 0 for both cases
        Assert.Equal(0, indexBefore);
        Assert.Equal(0, indexAfter);
    }

    [Fact]
    public void FindServerIndex_WithManyNodes_ExactMatchInMiddle_ReturnsCorrectIndex()
    {
        // Arrange - Create many nodes to ensure binary search path
        var virtualNodes = new List<VirtualNode<string>>();
        for (int i = 0; i < 100; i++)
        {
            virtualNodes.Add(new VirtualNode<string>((uint)(i * 100), $"server{i}"));
        }

        // Act - Search for exact match in the middle (hits lines 429-430)
        var index = HashRing<string>.FindServerIndex(virtualNodes, 5000); // Should match server50

        // Assert
        Assert.Equal(50, index);
    }

    [Fact]
    public void FindServerIndex_BinarySearchEdgeCases()
    {
        // Arrange - Create nodes at specific positions to test binary search logic
        var virtualNodes = new List<VirtualNode<string>>
        {
            new(1, "server1"),
            new(2, "server2"),
            new(3, "server3"),
            new(4, "server4"),
            new(5, "server5"),
            new(6, "server6"),
            new(7, "server7")
        };

        // Act & Assert - Test exact matches at various positions
        Assert.Equal(0, HashRing<string>.FindServerIndex(virtualNodes, 1)); // First
        Assert.Equal(3, HashRing<string>.FindServerIndex(virtualNodes, 4)); // Middle
        Assert.Equal(6, HashRing<string>.FindServerIndex(virtualNodes, 7)); // Last

        // Test between values
        Assert.Equal(0, HashRing<string>.FindServerIndex(virtualNodes, 0)); // Before first
        Assert.Equal(0, HashRing<string>.FindServerIndex(virtualNodes, 10)); // After last (wrap)
    }
}
