// API Contract: HashRing<T> Public Interface
// This file defines the expected public API surface for contract testing

using System;
using System.Collections.Generic;

namespace AdaptArch.Common.Utilities.ConsistentHashing
{
    /// <summary>
    /// Consistent hash ring for distributed request routing
    /// </summary>
    /// <typeparam name="T">Server type that must implement IEquatable</typeparam>
    public class HashRing<T> where T : IEquatable<T>
    {
        /// <summary>
        /// Creates a new hash ring with default configuration
        /// </summary>
        public HashRing() { }

        /// <summary>
        /// Creates a new hash ring with specified virtual node count
        /// </summary>
        /// <param name="defaultVirtualNodes">Default number of virtual nodes per server</param>
        public HashRing(int defaultVirtualNodes) { }

        /// <summary>
        /// Creates a new hash ring with custom hash algorithm
        /// </summary>
        /// <param name="hashAlgorithm">Hash algorithm implementation</param>
        public HashRing(IHashAlgorithm hashAlgorithm) { }

        /// <summary>
        /// Creates a new hash ring with full configuration
        /// </summary>
        /// <param name="defaultVirtualNodes">Default virtual nodes per server</param>
        /// <param name="hashAlgorithm">Hash algorithm implementation</param>
        public HashRing(int defaultVirtualNodes, IHashAlgorithm hashAlgorithm) { }

        /// <summary>
        /// Gets all servers currently in the ring
        /// </summary>
        public IReadOnlyCollection<T> Servers { get; }

        /// <summary>
        /// Gets the total number of virtual nodes in the ring
        /// </summary>
        public int VirtualNodeCount { get; }

        /// <summary>
        /// Gets whether the ring is empty (no servers)
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// Adds a server to the ring with default virtual node count
        /// </summary>
        /// <param name="server">Server to add</param>
        /// <exception cref="ArgumentNullException">When server is null</exception>
        public void Add(T server) { }

        /// <summary>
        /// Adds a server to the ring with specified virtual node count
        /// </summary>
        /// <param name="server">Server to add</param>
        /// <param name="virtualNodes">Number of virtual nodes for this server</param>
        /// <exception cref="ArgumentNullException">When server is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">When virtualNodes &lt;= 0</exception>
        public void Add(T server, int virtualNodes) { }

        /// <summary>
        /// Removes a server and all its virtual nodes from the ring
        /// </summary>
        /// <param name="server">Server to remove</param>
        /// <returns>True if server was removed, false if not found</returns>
        public bool Remove(T server) { }

        /// <summary>
        /// Checks if a server is present in the ring
        /// </summary>
        /// <param name="server">Server to check</param>
        /// <returns>True if server is in ring</returns>
        public bool Contains(T server) { }

        /// <summary>
        /// Removes all servers from the ring
        /// </summary>
        public void Clear() { }

        /// <summary>
        /// Gets the server that should handle the given key
        /// </summary>
        /// <param name="key">Key to route</param>
        /// <returns>Server for the key</returns>
        /// <exception cref="ArgumentNullException">When key is null</exception>
        /// <exception cref="InvalidOperationException">When ring is empty</exception>
        public T GetServer(byte[] key) { }

        /// <summary>
        /// Tries to get the server for a key without throwing exceptions
        /// </summary>
        /// <param name="key">Key to route</param>
        /// <param name="server">Server result if found</param>
        /// <returns>True if server found, false if ring is empty</returns>
        public bool TryGetServer(byte[] key, out T server) { }

        /// <summary>
        /// Gets multiple servers in ring order starting from the key position
        /// </summary>
        /// <param name="key">Key to start from</param>
        /// <param name="count">Number of servers to return</param>
        /// <returns>Servers in ring order</returns>
        public IEnumerable<T> GetServers(byte[] key, int count) { }
    }

    /// <summary>
    /// Hash algorithm abstraction for pluggable implementations
    /// </summary>
    public interface IHashAlgorithm
    {
        /// <summary>
        /// Computes hash value for the given data
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>32-bit hash value</returns>
        uint ComputeHash(byte[] data);

        /// <summary>
        /// Gets the name of this hash algorithm
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Configuration options for hash ring
    /// </summary>
    public class HashRingOptions
    {
        /// <summary>
        /// Default number of virtual nodes per server
        /// </summary>
        public int DefaultVirtualNodes { get; set; } = 42;

        /// <summary>
        /// Hash algorithm to use
        /// </summary>
        public IHashAlgorithm HashAlgorithm { get; set; } = new Sha1HashAlgorithm();

        /// <summary>
        /// Whether to throw exception on empty ring lookup
        /// </summary>
        public bool ThrowOnEmptyRing { get; set; } = true;
    }

    /// <summary>
    /// SHA1-based hash algorithm implementation
    /// </summary>
    public class Sha1HashAlgorithm : IHashAlgorithm
    {
        public uint ComputeHash(byte[] data) => throw new NotImplementedException();
        public string Name => "SHA1";
    }

    /// <summary>
    /// MD5-based hash algorithm implementation
    /// </summary>
    public class Md5HashAlgorithm : IHashAlgorithm
    {
        public uint ComputeHash(byte[] data) => throw new NotImplementedException();
        public string Name => "MD5";
    }
}
