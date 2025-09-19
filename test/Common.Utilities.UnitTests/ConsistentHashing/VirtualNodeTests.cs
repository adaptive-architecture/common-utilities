using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class VirtualNodeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange
        const uint hash = 12345u;
        const string server = "server1";

        // Act
        var node = new VirtualNode<string>(hash, server);

        // Assert
        Assert.Equal(hash, node.Hash);
        Assert.Equal(server, node.Server);
    }

    [Fact]
    public void Constructor_NullServer_ThrowsArgumentNullException()
    {
        // Arrange
        const uint hash = 12345u;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VirtualNode<string>(hash, null!));
    }

    [Fact]
    public void Constructor_ZeroHash_ValidServer_Succeeds()
    {
        // Arrange
        const uint hash = 0u;
        const string server = "server1";

        // Act
        var node = new VirtualNode<string>(hash, server);

        // Assert
        Assert.Equal(hash, node.Hash);
        Assert.Equal(server, node.Server);
    }

    [Fact]
    public void Constructor_MaxHash_ValidServer_Succeeds()
    {
        // Arrange
        const uint hash = UInt32.MaxValue;
        const string server = "server1";

        // Act
        var node = new VirtualNode<string>(hash, server);

        // Assert
        Assert.Equal(hash, node.Hash);
        Assert.Equal(server, node.Server);
    }

    #endregion

    #region IComparable Tests

    [Fact]
    public void CompareTo_SameHash_ReturnsZero()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2"); // Different server, same hash

        // Act
        int result = node1.CompareTo(node2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareTo_SmallerHash_ReturnsNegative()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act
        int result = node1.CompareTo(node2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareTo_LargerHash_ReturnsPositive()
    {
        // Arrange
        var node1 = new VirtualNode<string>(200u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act
        int result = node1.CompareTo(node2);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CompareTo_NullOther_ReturnsPositive()
    {
        // Arrange
        var node = new VirtualNode<string>(100u, "server1");

        // Act
        int result = node.CompareTo(null);

        // Assert
        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(1u, 2u)]
    [InlineData(UInt32.MaxValue - 1, UInt32.MaxValue)]
    [InlineData(1000u, 2000u)]
    public void CompareTo_ConsistentOrdering_WorksCorrectly(uint smallerHash, uint largerHash)
    {
        // Arrange
        var smallerNode = new VirtualNode<string>(smallerHash, "server1");
        var largerNode = new VirtualNode<string>(largerHash, "server2");

        // Act & Assert
        Assert.True(smallerNode.CompareTo(largerNode) < 0);
        Assert.True(largerNode.CompareTo(smallerNode) > 0);
        Assert.Equal(0, smallerNode.CompareTo(smallerNode));
        Assert.Equal(0, largerNode.CompareTo(largerNode));
    }

    #endregion

    #region Equals Tests

    [Fact]
    public void Equals_SameHashAndServer_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.True(node1.Equals(node2));
        Assert.True(node2.Equals(node1));
    }

    [Fact]
    public void Equals_SameHashDifferentServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.False(node1.Equals(node2));
        Assert.False(node2.Equals(node1));
    }

    [Fact]
    public void Equals_DifferentHashSameServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server1");

        // Act & Assert
        Assert.False(node1.Equals(node2));
        Assert.False(node2.Equals(node1));
    }

    [Fact]
    public void Equals_DifferentHashAndServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act & Assert
        Assert.False(node1.Equals(node2));
        Assert.False(node2.Equals(node1));
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var node = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.False(node.Equals(null));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var node = new VirtualNode<string>(100u, "server1");
        const string other = "not a virtual node";

        // Act & Assert
        Assert.False(node.Equals(other));
    }

    [Fact]
    public void Equals_SelfReference_ReturnsTrue()
    {
        // Arrange
        var node = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.True(node.Equals(node));
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameHashAndServer_ReturnsSameHashCode()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act
        int hashCode1 = node1.GetHashCode();
        int hashCode2 = node2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_DifferentNodes_MayReturnDifferentHashCodes()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act
        int hashCode1 = node1.GetHashCode();
        int hashCode2 = node2.GetHashCode();

        // Assert
        // Note: Hash codes MAY be different, but they don't have to be
        // We just verify the method doesn't throw
        Assert.IsType<int>(hashCode1);
        Assert.IsType<int>(hashCode2);
    }

    [Fact]
    public void GetHashCode_SameHashDifferentServer_ReturnsDifferentHashCodes()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act
        int hashCode1 = node1.GetHashCode();
        int hashCode2 = node2.GetHashCode();

        // Assert
        // These should be different since the servers are different
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ConsistentAcrossMultipleCalls()
    {
        // Arrange
        var node = new VirtualNode<string>(100u, "server1");

        // Act
        int hashCode1 = node.GetHashCode();
        int hashCode2 = node.GetHashCode();
        int hashCode3 = node.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
        Assert.Equal(hashCode2, hashCode3);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public void VirtualNodes_SortCorrectly()
    {
        // Arrange
        var nodes = new List<VirtualNode<string>>
        {
            new(300u, "server3"),
            new(100u, "server1"),
            new(200u, "server2"),
            new(50u, "server0"),
            new(400u, "server4")
        };

        var expectedOrder = new uint[] { 50u, 100u, 200u, 300u, 400u };

        // Act
        nodes.Sort();

        // Assert
        for (int i = 0; i < nodes.Count; i++)
        {
            Assert.Equal(expectedOrder[i], nodes[i].Hash);
        }
    }

    [Fact]
    public void VirtualNodes_WithDuplicateHashes_SortStably()
    {
        // Arrange
        var nodes = new List<VirtualNode<string>>
        {
            new(100u, "server1"),
            new(100u, "server2"),
            new(200u, "server3"),
            new(100u, "server4")
        };

        // Act
        nodes.Sort();

        // Assert
        // All nodes with hash 100 should come before the node with hash 200
        Assert.Equal(100u, nodes[0].Hash);
        Assert.Equal(100u, nodes[1].Hash);
        Assert.Equal(100u, nodes[2].Hash);
        Assert.Equal(200u, nodes[3].Hash);
    }

    #endregion

    #region Different Generic Types Tests

    [Fact]
    public void VirtualNode_WithIntegerServer_WorksCorrectly()
    {
        // Arrange
        var node1 = new VirtualNode<int>(100u, 1);
        var node2 = new VirtualNode<int>(100u, 1);
        var node3 = new VirtualNode<int>(100u, 2);

        // Act & Assert
        Assert.Equal(1, node1.Server);
        Assert.True(node1.Equals(node2));
        Assert.False(node1.Equals(node3));
        Assert.Equal(0, node1.CompareTo(node2));
    }

    [Fact]
    public void VirtualNode_WithGuidServer_WorksCorrectly()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var node1 = new VirtualNode<Guid>(100u, guid1);
        var node2 = new VirtualNode<Guid>(100u, guid1);
        var node3 = new VirtualNode<Guid>(100u, guid2);

        // Act & Assert
        Assert.Equal(guid1, node1.Server);
        Assert.True(node1.Equals(node2));
        Assert.False(node1.Equals(node3));
        Assert.Equal(0, node1.CompareTo(node2));
    }

    [Fact]
    public void VirtualNode_WithCustomEquatableType_WorksCorrectly()
    {
        // Arrange
        var server1 = new ServerInfo("host1", 8080);
        var server2 = new ServerInfo("host1", 8080); // Same values
        var server3 = new ServerInfo("host2", 8080); // Different values

        var node1 = new VirtualNode<ServerInfo>(100u, server1);
        var node2 = new VirtualNode<ServerInfo>(100u, server2);
        var node3 = new VirtualNode<ServerInfo>(100u, server3);

        // Act & Assert
        Assert.Equal(server1, node1.Server);
        Assert.True(node1.Equals(node2)); // Same server values
        Assert.False(node1.Equals(node3)); // Different server values
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void VirtualNode_WithEmptyStringServer_WorksCorrectly()
    {
        // Arrange & Act
        var node = new VirtualNode<string>(100u, "");

        // Assert
        Assert.Equal("", node.Server);
        Assert.Equal(100u, node.Hash);
    }

    [Fact]
    public void VirtualNode_WithMinMaxHashValues_WorksCorrectly()
    {
        // Arrange
        var minNode = new VirtualNode<string>(UInt32.MinValue, "server1");
        var maxNode = new VirtualNode<string>(UInt32.MaxValue, "server2");

        // Act & Assert
        Assert.Equal(UInt32.MinValue, minNode.Hash);
        Assert.Equal(UInt32.MaxValue, maxNode.Hash);
        Assert.True(minNode.CompareTo(maxNode) < 0);
        Assert.True(maxNode.CompareTo(minNode) > 0);
    }

    [Fact]
    public void VirtualNode_HashCodeDistribution_IsReasonable()
    {
        // Arrange
        var nodes = new List<VirtualNode<string>>();
        var hashCodes = new HashSet<int>();

        // Create many different nodes
        for (uint i = 0; i < 1000; i += 10)
        {
            for (int j = 1; j <= 5; j++)
            {
                var node = new VirtualNode<string>(i, $"server{j}");
                nodes.Add(node);
                hashCodes.Add(node.GetHashCode());
            }
        }

        // Act & Assert
        // We should have a reasonable distribution of hash codes (most should be unique)
        double uniquePercentage = (double)hashCodes.Count / nodes.Count;
        Assert.True(uniquePercentage > 0.95, $"Hash code distribution is poor: {uniquePercentage:P}");
    }

    #endregion

    #region Comparison Operators Tests

    [Fact]
    public void EqualityOperator_SameHashAndServer_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.True(node1 == node2);
        Assert.True(node2 == node1);
    }

    [Fact]
    public void EqualityOperator_SameHashDifferentServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.False(node1 == node2);
        Assert.False(node2 == node1);
    }

    [Fact]
    public void EqualityOperator_DifferentHashSameServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server1");

        // Act & Assert
        Assert.False(node1 == node2);
        Assert.False(node2 == node1);
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.True(node1 == node2);
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.False(node1 == node2);
        Assert.False(node2 == node1);
    }

    [Fact]
    public void EqualityOperator_SameReference_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = node1;

        // Act & Assert
        Assert.True(node1 == node2);
        Assert.True(node2 == node1);
    }

    [Fact]
    public void InequalityOperator_SameHashAndServer_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.False(node1 != node2);
        Assert.False(node2 != node1);
    }

    [Fact]
    public void InequalityOperator_DifferentNodes_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act & Assert
        Assert.True(node1 != node2);
        Assert.True(node2 != node1);
    }

    [Fact]
    public void InequalityOperator_OneNull_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.True(node1 != node2);
        Assert.True(node2 != node1);
    }

    [Fact]
    public void InequalityOperator_BothNull_ReturnsFalse()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.False(node1 != node2);
    }

    [Fact]
    public void LessThanOperator_SmallerHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act & Assert
        Assert.True(node1 < node2);
        Assert.False(node2 < node1);
    }

    [Fact]
    public void LessThanOperator_SameHash_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.False(node1 < node2);
        Assert.False(node2 < node1);
    }

    [Fact]
    public void LessThanOperator_LeftNull_ReturnsCorrectValue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.True(node1 < node2);   // null < non-null is true
        Assert.False(node2 < node1);  // non-null < null is false
    }

    [Fact]
    public void LessThanOperator_BothNull_ReturnsFalse()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.False(node1 < node2);
    }

    [Fact]
    public void LessThanOrEqualOperator_SmallerHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");

        // Act & Assert
        Assert.True(node1 <= node2);
        Assert.False(node2 <= node1);
    }

    [Fact]
    public void LessThanOrEqualOperator_SameHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.True(node1 <= node2);
        Assert.True(node2 <= node1);
    }

    [Fact]
    public void LessThanOrEqualOperator_LeftNull_ReturnsTrue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.True(node1 <= node2);
        Assert.False(node2 <= node1);
    }

    [Fact]
    public void LessThanOrEqualOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.True(node1 <= node2);
    }

    [Fact]
    public void GreaterThanOperator_LargerHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(200u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.True(node1 > node2);
        Assert.False(node2 > node1);
    }

    [Fact]
    public void GreaterThanOperator_SameHash_ReturnsFalse()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.False(node1 > node2);
        Assert.False(node2 > node1);
    }

    [Fact]
    public void GreaterThanOperator_LeftNull_ReturnsFalse()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.False(node1 > node2);
        Assert.True(node2 > node1);
    }

    [Fact]
    public void GreaterThanOperator_BothNull_ReturnsFalse()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.False(node1 > node2);
    }

    [Fact]
    public void GreaterThanOrEqualOperator_LargerHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(200u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.True(node1 >= node2);
        Assert.False(node2 >= node1);
    }

    [Fact]
    public void GreaterThanOrEqualOperator_SameHash_ReturnsTrue()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server2");

        // Act & Assert
        Assert.True(node1 >= node2);
        Assert.True(node2 >= node1);
    }

    [Fact]
    public void GreaterThanOrEqualOperator_LeftNull_ReturnsCorrectValue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        var node2 = new VirtualNode<string>(100u, "server1");

        // Act & Assert
        Assert.False(node1 >= node2);  // null >= non-null is false
        Assert.True(node2 >= node1);   // non-null >= null is true
    }

    [Fact]
    public void GreaterThanOrEqualOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        VirtualNode<string> node1 = null!;
        VirtualNode<string> node2 = null!;

        // Act & Assert
        Assert.True(node1 >= node2);
    }

    [Theory]
    [InlineData(0u, 1u)]
    [InlineData(1u, 2u)]
    [InlineData(100u, 200u)]
    [InlineData(UInt32.MaxValue - 1, UInt32.MaxValue)]
    public void ComparisonOperators_ConsistentWithCompareTo(uint smallerHash, uint largerHash)
    {
        // Arrange
        var smallerNode = new VirtualNode<string>(smallerHash, "server1");
        var largerNode = new VirtualNode<string>(largerHash, "server2");

        // Act & Assert - Verify all operators are consistent with CompareTo
        Assert.True(smallerNode < largerNode);
        Assert.True(smallerNode <= largerNode);
        Assert.False(smallerNode > largerNode);
        Assert.False(smallerNode >= largerNode);
        Assert.True(smallerNode != largerNode);
        Assert.False(smallerNode == largerNode);

        Assert.False(largerNode < smallerNode);
        Assert.False(largerNode <= smallerNode);
        Assert.True(largerNode > smallerNode);
        Assert.True(largerNode >= smallerNode);
        Assert.True(largerNode != smallerNode);
        Assert.False(largerNode == smallerNode);
    }

    [Fact]
    public void ComparisonOperators_EdgeCaseWithMinMaxValues()
    {
        // Arrange
        var minNode = new VirtualNode<string>(UInt32.MinValue, "server1");
        var maxNode = new VirtualNode<string>(UInt32.MaxValue, "server2");

        // Act & Assert
        Assert.True(minNode < maxNode);
        Assert.True(minNode <= maxNode);
        Assert.False(minNode > maxNode);
        Assert.False(minNode >= maxNode);
        Assert.True(minNode != maxNode);
        Assert.False(minNode == maxNode);

        Assert.False(maxNode < minNode);
        Assert.False(maxNode <= minNode);
        Assert.True(maxNode > minNode);
        Assert.True(maxNode >= minNode);
        Assert.True(maxNode != minNode);
        Assert.False(maxNode == minNode);
    }

    [Fact]
    public void ComparisonOperators_WithDifferentGenericTypes_WorkCorrectly()
    {
        // Arrange
        var intNode1 = new VirtualNode<int>(100u, 1);
        var intNode2 = new VirtualNode<int>(200u, 2);
        var intNode3 = new VirtualNode<int>(100u, 1); // Same as node1

        // Act & Assert
        Assert.True(intNode1 < intNode2);
        Assert.False(intNode2 < intNode1);
        Assert.True(intNode1 == intNode3);
        Assert.False(intNode1 != intNode3);

        // Test with GUIDs
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guidNode1 = new VirtualNode<Guid>(100u, guid1);
        var guidNode2 = new VirtualNode<Guid>(200u, guid2);
        var guidNode3 = new VirtualNode<Guid>(100u, guid1);

        Assert.True(guidNode1 < guidNode2);
        Assert.False(guidNode2 < guidNode1);
        Assert.True(guidNode1 == guidNode3);
        Assert.False(guidNode1 != guidNode3);
    }

    [Fact]
    public void ComparisonOperators_TransitivityProperty()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(200u, "server2");
        var node3 = new VirtualNode<string>(300u, "server3");

        // Act & Assert - Test transitivity: if a < b and b < c, then a < c
        Assert.True(node1 < node2);
        Assert.True(node2 < node3);
        Assert.True(node1 < node3); // Transitivity

        // Test with <= operator
        Assert.True(node1 <= node2);
        Assert.True(node2 <= node3);
        Assert.True(node1 <= node3); // Transitivity

        // Test with > operator
        Assert.True(node3 > node2);
        Assert.True(node2 > node1);
        Assert.True(node3 > node1); // Transitivity

        // Test with >= operator
        Assert.True(node3 >= node2);
        Assert.True(node2 >= node1);
        Assert.True(node3 >= node1); // Transitivity
    }

    [Fact]
    public void ComparisonOperators_ReflexivityProperty()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = node1; // Same reference

        // Act & Assert - Test reflexivity: a == a, a <= a, a >= a
        Assert.True(node1 == node2);
        Assert.False(node1 != node2);
        Assert.True(node1 <= node2);
        Assert.True(node1 >= node2);
        Assert.False(node1 < node2);
        Assert.False(node1 > node2);
    }

    [Fact]
    public void ComparisonOperators_SymmetryProperty()
    {
        // Arrange
        var node1 = new VirtualNode<string>(100u, "server1");
        var node2 = new VirtualNode<string>(100u, "server1");
        var node3 = new VirtualNode<string>(200u, "server2");

        // Act & Assert - Test symmetry: if a == b, then b == a
        Assert.True(node1 == node2);
        Assert.True(node2 == node1);

        // Test symmetry with inequality
        Assert.True(node1 != node3);
        Assert.True(node3 != node1);

        // Test with ordering operators
        Assert.True(node1 < node3);
        Assert.True(node3 > node1);
    }

    #endregion

    // Helper record for testing with custom equatable types
    private record ServerInfo(string Host, int Port) : IEquatable<ServerInfo>;
}
