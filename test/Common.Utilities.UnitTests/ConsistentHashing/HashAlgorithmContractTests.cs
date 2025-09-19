using AdaptArch.Common.Utilities.ConsistentHashing;

namespace AdaptArch.Common.Utilities.UnitTests.ConsistentHashing;

public class HashAlgorithmContractTests
{
    #region SHA1 Algorithm Tests

    [Fact]
    public void Sha1HashAlgorithm_ComputeHash_ReturnsConsistentResults()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] hash1 = algorithm.ComputeHash(data);
        byte[] hash2 = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Sha1HashAlgorithm_ComputeHash_DifferentDataReturnsDifferentHashes()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();
        var data1 = System.Text.Encoding.UTF8.GetBytes("test data 1");
        var data2 = System.Text.Encoding.UTF8.GetBytes("test data 2");

        // Act
        byte[] hash1 = algorithm.ComputeHash(data1);
        byte[] hash2 = algorithm.ComputeHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Sha1HashAlgorithm_ComputeHash_ReturnsCorrectHashLength()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] hash = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(20, hash.Length); // SHA1 produces 160-bit (20-byte) hash
    }

    [Fact]
    public void Sha1HashAlgorithm_ComputeHash_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => algorithm.ComputeHash(null));
    }

    [Fact]
    public void Sha1HashAlgorithm_ComputeHash_EmptyData_ReturnsValidHash()
    {
        // Arrange
        var algorithm = new Sha1HashAlgorithm();
        var data = Array.Empty<byte>();

        // Act
        byte[] hash = algorithm.ComputeHash(data);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(20, hash.Length); // Should return a valid 20-byte SHA1 hash even for empty data
    }

    #endregion

    #region MD5 Algorithm Tests

    [Fact]
    public void Md5HashAlgorithm_ComputeHash_ReturnsConsistentResults()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] hash1 = algorithm.ComputeHash(data);
        byte[] hash2 = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Md5HashAlgorithm_ComputeHash_DifferentDataReturnsDifferentHashes()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        var data1 = System.Text.Encoding.UTF8.GetBytes("test data 1");
        var data2 = System.Text.Encoding.UTF8.GetBytes("test data 2");

        // Act
        byte[] hash1 = algorithm.ComputeHash(data1);
        byte[] hash2 = algorithm.ComputeHash(data2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Md5HashAlgorithm_ComputeHash_ReturnsCorrectHashLength()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] hash = algorithm.ComputeHash(data);

        // Assert
        Assert.Equal(16, hash.Length); // MD5 produces 128-bit (16-byte) hash
    }

    [Fact]
    public void Md5HashAlgorithm_ComputeHash_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => algorithm.ComputeHash(null));
    }

    [Fact]
    public void Md5HashAlgorithm_ComputeHash_EmptyData_ReturnsValidHash()
    {
        // Arrange
        var algorithm = new Md5HashAlgorithm();
        var data = Array.Empty<byte>();

        // Act
        byte[] hash = algorithm.ComputeHash(data);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(16, hash.Length); // Should return a valid 16-byte MD5 hash even for empty data
    }

    #endregion

    #region Algorithm Comparison Tests

    [Fact]
    public void Sha1AndMd5_SameData_ReturnDifferentHashes()
    {
        // Arrange
        var sha1 = new Sha1HashAlgorithm();
        var md5 = new Md5HashAlgorithm();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        // Act
        byte[] sha1Hash = sha1.ComputeHash(data);
        byte[] md5Hash = md5.ComputeHash(data);

        // Assert
        Assert.NotEqual(sha1Hash, md5Hash);
    }

    [Fact]
    public void HashAlgorithms_ImplementIHashAlgorithm()
    {
        // Arrange & Act
        var sha1 = new Sha1HashAlgorithm();
        var md5 = new Md5HashAlgorithm();

        // Assert
        Assert.IsAssignableFrom<IHashAlgorithm>(sha1);
        Assert.IsAssignableFrom<IHashAlgorithm>(md5);
    }

    #endregion

    #region Performance and Distribution Tests

    [Fact]
    public void HashAlgorithms_LargeData_ProducesValidHash()
    {
        // Arrange
        var sha1 = new Sha1HashAlgorithm();
        var md5 = new Md5HashAlgorithm();
        var largeData = new byte[10000];
        new Random(42).NextBytes(largeData);

        // Act
        byte[] sha1Hash = sha1.ComputeHash(largeData);
        byte[] md5Hash = md5.ComputeHash(largeData);

        // Assert
        Assert.NotNull(sha1Hash);
        Assert.NotNull(md5Hash);
        Assert.Equal(20, sha1Hash.Length);
        Assert.Equal(16, md5Hash.Length);
        Assert.NotEqual(sha1Hash, md5Hash);
    }

    [Fact]
    public void HashAlgorithms_MultipleSmallInputs_ProduceDistinctHashes()
    {
        // Arrange
        var sha1 = new Sha1HashAlgorithm();
        var inputs = new[]
        {
            "server1", "server2", "server3", "server4", "server5",
            "user123", "user456", "user789", "session_abc", "session_def"
        };

        // Act & Assert
        var hashes = new HashSet<string>();
        foreach (var input in inputs)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = sha1.ComputeHash(data);
            string hashString = Convert.ToHexString(hash);
            Assert.True(hashes.Add(hashString), $"Hash collision detected for input: {input}");
        }
    }

    #endregion
}
