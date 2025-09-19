using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class IHashAlgorithmInterfaceTests
{
    #region Interface Compliance Tests

    [Fact]
    public void Sha1HashAlgorithm_ImplementsIHashAlgorithm()
    {
        // Arrange & Act
        var algorithm = new Sha1HashAlgorithm();

        // Assert
        Assert.IsType<IHashAlgorithm>(algorithm, exactMatch: false);
    }

    [Fact]
    public void Md5HashAlgorithm_ImplementsIHashAlgorithm()
    {
        // Arrange & Act
        var algorithm = new Md5HashAlgorithm();

        // Assert
        Assert.IsType<IHashAlgorithm>(algorithm, exactMatch: false);
    }

    [Fact]
    public void IHashAlgorithm_HasCorrectMethodSignature()
    {
        // Arrange
        Sha1HashAlgorithm algorithm = new();

        // Act & Assert
        // Should be able to call ComputeHash with byte array
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        var hash = algorithm.ComputeHash(data);

        Assert.NotNull(hash);
        Assert.True(hash.Length > 0);
    }

    #endregion

    #region Polymorphic Usage Tests

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public void IHashAlgorithm_PolymorphicUsage_WorksCorrectly(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        var hash = algorithm.ComputeHash(data);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(expectedHashLength, hash.Length);
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public void IHashAlgorithm_ConsistentResults_AcrossMultipleCalls(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var data = System.Text.Encoding.UTF8.GetBytes("consistent test");

        // Act
        var hash1 = algorithm.ComputeHash(data);
        var hash2 = algorithm.ComputeHash(data);
        var hash3 = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
        Assert.Equal(expectedHashLength, hash1.Length);
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public void IHashAlgorithm_DifferentData_ProducesDifferentHashes(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var data1 = System.Text.Encoding.UTF8.GetBytes("data one");
        var data2 = System.Text.Encoding.UTF8.GetBytes("data two");

        // Act
        var hash1 = algorithm.ComputeHash(data1);
        var hash2 = algorithm.ComputeHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.Equal(expectedHashLength, hash1.Length);
        Assert.Equal(expectedHashLength, hash2.Length);
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public void IHashAlgorithm_EmptyData_ReturnsValidHash(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var emptyData = Array.Empty<byte>();

        // Act
        var hash = algorithm.ComputeHash(emptyData);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(expectedHashLength, hash.Length);
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm))]
    [InlineData(typeof(Md5HashAlgorithm))]
    public void IHashAlgorithm_NullData_ThrowsArgumentNullException(Type algorithmType)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => algorithm.ComputeHash(null!));
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public void IHashAlgorithm_LargeData_HandlesCorrectly(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var largeData = new byte[100000];
        new Random(42).NextBytes(largeData); // Deterministic data

        // Act
        var hash = algorithm.ComputeHash(largeData);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(expectedHashLength, hash.Length);
    }

    #endregion

    #region HashRing Integration Tests

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm))]
    [InlineData(typeof(Md5HashAlgorithm))]
    public void IHashAlgorithm_UsedInHashRing_WorksCorrectly(Type algorithmType)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var ring = new HashRing<string>(algorithm);
        ring.Add("server1");
        ring.Add("server2");

        // Act
        var server1 = ring.GetServer("test-key-1");
        var server2 = ring.GetServer("test-key-2");
        var server3 = ring.GetServer("test-key-1"); // Same as server1

        // Assert
        Assert.Contains(server1, ring.Servers);
        Assert.Contains(server2, ring.Servers);
        Assert.Equal(server1, server3); // Consistency
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm))]
    [InlineData(typeof(Md5HashAlgorithm))]
    public void IHashAlgorithm_UsedInHashRingOptions_WorksCorrectly(Type algorithmType)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        var options = new HashRingOptions(algorithm, 100);
        var ring = new HashRing<string>(options);
        ring.Add("server1");

        // Act
        var server = ring.GetServer("test-key");

        // Assert
        Assert.Equal("server1", server);
        Assert.Equal(100, ring.VirtualNodeCount);
    }

    #endregion

    #region Custom Implementation Tests

    [Fact]
    public void CustomHashAlgorithm_ImplementsInterface_WorksWithHashRing()
    {
        // Arrange
        var customAlgorithm = new TestHashAlgorithm();
        var ring = new HashRing<string>(customAlgorithm);
        ring.Add("server1");
        ring.Add("server2");

        // Act
        var server = ring.GetServer("test");

        // Assert
        Assert.Contains(server, ring.Servers);
        Assert.True(customAlgorithm.ComputeHashCallCount > 0);
    }

    [Fact]
    public void CustomHashAlgorithm_ReturnsConsistentResults()
    {
        // Arrange
        var algorithm = new TestHashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test");

        // Act
        var hash1 = algorithm.ComputeHash(data);
        var hash2 = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.Equal(2, algorithm.ComputeHashCallCount);
    }

    #endregion

    #region Performance and Thread Safety

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm), 20)]
    [InlineData(typeof(Md5HashAlgorithm), 16)]
    public async Task IHashAlgorithm_ConcurrentAccess_IsThreadSafe(Type algorithmType, int expectedHashLength)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        const int threadCount = 10;
        const int operationsPerThread = 100;
        var results = new ConcurrentBag<byte[]>();
        var testData = System.Text.Encoding.UTF8.GetBytes("concurrent test data");

        // Act
        var tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var hash = algorithm.ComputeHash(testData);
                    results.Add(hash);
                }
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(tasks);

        // Assert
        var resultArray = results.ToArray();
        Assert.Equal(threadCount * operationsPerThread, resultArray.Length);

        // All results should be identical (same input data)
        var firstResult = resultArray[0];
        Assert.All(resultArray, hash => Assert.Equal(firstResult, hash));
        Assert.Equal(expectedHashLength, firstResult.Length);
    }

    [Theory]
    [InlineData(typeof(Sha1HashAlgorithm))]
    [InlineData(typeof(Md5HashAlgorithm))]
    public void IHashAlgorithm_PerformanceBaseline_CompletesInReasonableTime(Type algorithmType)
    {
        // Arrange
        var algorithm = (IHashAlgorithm)Activator.CreateInstance(algorithmType)!;
        const int iterations = 10000;
        var testData = System.Text.Encoding.UTF8.GetBytes("performance test data");

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var hash = algorithm.ComputeHash(testData);
        }

        stopwatch.Stop();

        // Assert
        // Should complete within reasonable time (this is a smoke test, not a strict benchmark)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Algorithm took too long: {stopwatch.ElapsedMilliseconds}ms for {iterations} operations");
    }

    #endregion

    // Test implementation of IHashAlgorithm for testing custom implementations
    private class TestHashAlgorithm : IHashAlgorithm
    {
        public int ComputeHashCallCount { get; private set; }

        public byte[] ComputeHash(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            ComputeHashCallCount++;

            // Return a simple hash based on the key's length and content
            var hash = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                hash[i] = (byte)((key.Length + i + (key.Length > 0 ? key[0] : 0)) % 256);
            }
            return hash;
        }
    }
}
