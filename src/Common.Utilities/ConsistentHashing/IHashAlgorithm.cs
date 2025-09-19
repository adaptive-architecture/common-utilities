namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Defines a contract for hash algorithms used in consistent hashing.
/// </summary>
public interface IHashAlgorithm
{
    /// <summary>
    /// Computes a hash value for the specified key.
    /// </summary>
    /// <param name="key">The key to compute the hash for.</param>
    /// <returns>The computed hash as a byte array.</returns>
    byte[] ComputeHash(byte[] key);
}
