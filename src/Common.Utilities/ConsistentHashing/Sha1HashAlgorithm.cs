using System.Security.Cryptography;

namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// SHA-1 hash algorithm implementation for consistent hashing.
/// Uses the thread-safe static method for concurrent access.
/// </summary>
public sealed class Sha1HashAlgorithm : IHashAlgorithm
{
    /// <summary>
    /// Computes a SHA-1 hash value for the specified key.
    /// </summary>
    /// <param name="key">The key to compute the hash for.</param>
    /// <returns>The computed SHA-1 hash as a byte array.</returns>
    public byte[] ComputeHash(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
#pragma warning disable S4790 // Using SHA1 is acceptable here for non-cryptographic purposes.
        return SHA1.HashData(key);
#pragma warning restore S4790
    }
}
