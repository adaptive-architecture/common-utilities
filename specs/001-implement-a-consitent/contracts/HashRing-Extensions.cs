// API Contract: HashRing Extension Methods
// Convenience extensions for common key types

using System;

namespace AdaptArch.Common.Utilities.ConsistentHashing
{
    /// <summary>
    /// Extension methods for HashRing to support common key types
    /// </summary>
    public static class HashRingExtensions
    {
        /// <summary>
        /// Gets the server for a string key
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">String key to route</param>
        /// <returns>Server for the key</returns>
        /// <exception cref="ArgumentNullException">When key is null</exception>
        /// <exception cref="InvalidOperationException">When ring is empty</exception>
        public static T GetServer<T>(this HashRing<T> hashRing, string key)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the server for a Guid key
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">Guid key to route</param>
        /// <returns>Server for the key</returns>
        /// <exception cref="InvalidOperationException">When ring is empty</exception>
        public static T GetServer<T>(this HashRing<T> hashRing, Guid key)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the server for an integer key
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">Integer key to route</param>
        /// <returns>Server for the key</returns>
        /// <exception cref="InvalidOperationException">When ring is empty</exception>
        public static T GetServer<T>(this HashRing<T> hashRing, int key)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the server for a long key
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">Long key to route</param>
        /// <returns>Server for the key</returns>
        /// <exception cref="InvalidOperationException">When ring is empty</exception>
        public static T GetServer<T>(this HashRing<T> hashRing, long key)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to get server for string key without throwing exceptions
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">String key to route</param>
        /// <param name="server">Server result if found</param>
        /// <returns>True if server found</returns>
        public static bool TryGetServer<T>(this HashRing<T> hashRing, string key, out T server)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to get server for Guid key without throwing exceptions
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">Guid key to route</param>
        /// <param name="server">Server result if found</param>
        /// <returns>True if server found</returns>
        public static bool TryGetServer<T>(this HashRing<T> hashRing, Guid key, out T server)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets multiple servers for string key in ring order
        /// </summary>
        /// <typeparam name="T">Server type</typeparam>
        /// <param name="hashRing">Hash ring instance</param>
        /// <param name="key">String key to start from</param>
        /// <param name="count">Number of servers to return</param>
        /// <returns>Servers in ring order</returns>
        public static System.Collections.Generic.IEnumerable<T> GetServers<T>(this HashRing<T> hashRing, string key, int count)
            where T : IEquatable<T>
        {
            throw new NotImplementedException();
        }
    }
}