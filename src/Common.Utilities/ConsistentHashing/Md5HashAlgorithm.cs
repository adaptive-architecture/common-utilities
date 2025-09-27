using System.Security.Cryptography;

namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// MD5 hash algorithm implementation for consistent hashing.
/// Uses the thread-safe static method for concurrent access.
/// </summary>
public sealed class Md5HashAlgorithm : IHashAlgorithm
{
    /// <summary>
    /// Computes an MD5 hash value for the specified key.
    /// </summary>
    /// <param name="key">The key to compute the hash for.</param>
    /// <returns>The computed MD5 hash as a byte array.</returns>
    public byte[] ComputeHash(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
#pragma warning disable S4790 // Using MD5 is acceptable here for non-cryptographic purposes.
        return MD5.HashData(key);
#pragma warning restore S4790
    }
}
