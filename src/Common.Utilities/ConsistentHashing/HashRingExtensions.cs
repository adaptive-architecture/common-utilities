namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Extension methods for <see cref="HashRing{T}"/> to support common key types.
/// </summary>
public static class HashRingExtensions
{
    /// <summary>
    /// Gets the server that should handle the specified string key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The string key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    public static T GetServer<T>(this HashRing<T> ring, string key) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(key);

        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        return ring.GetServer(keyBytes);
    }

    /// <summary>
    /// Tries to get the server that should handle the specified string key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The string key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found; otherwise, false.</returns>
    public static bool TryGetServer<T>(this HashRing<T> ring, string key, out T? server) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(key);

        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        return ring.TryGetServer(keyBytes, out server);
    }

    /// <summary>
    /// Gets the server that should handle the specified GUID key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The GUID key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    public static T GetServer<T>(this HashRing<T> ring, Guid key) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = key.ToByteArray();
        return ring.GetServer(keyBytes);
    }

    /// <summary>
    /// Tries to get the server that should handle the specified GUID key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The GUID key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found; otherwise, false.</returns>
    public static bool TryGetServer<T>(this HashRing<T> ring, Guid key, out T? server) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = key.ToByteArray();
        return ring.TryGetServer(keyBytes, out server);
    }

    /// <summary>
    /// Gets the server that should handle the specified integer key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The integer key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    public static T GetServer<T>(this HashRing<T> ring, int key) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.GetServer(keyBytes);
    }

    /// <summary>
    /// Tries to get the server that should handle the specified integer key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The integer key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found; otherwise, false.</returns>
    public static bool TryGetServer<T>(this HashRing<T> ring, int key, out T? server) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.TryGetServer(keyBytes, out server);
    }

    /// <summary>
    /// Gets the server that should handle the specified long integer key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The long integer key to find a server for.</param>
    /// <returns>The server that should handle the key.</returns>
    public static T GetServer<T>(this HashRing<T> ring, long key) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.GetServer(keyBytes);
    }

    /// <summary>
    /// Tries to get the server that should handle the specified long integer key.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The long integer key to find a server for.</param>
    /// <param name="server">When this method returns, contains the server if found; otherwise, the default value.</param>
    /// <returns>true if a server was found; otherwise, false.</returns>
    public static bool TryGetServer<T>(this HashRing<T> ring, long key, out T? server) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.TryGetServer(keyBytes, out server);
    }

    /// <summary>
    /// Gets multiple servers that should handle the specified string key, in preference order.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The string key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order.</returns>
    public static IEnumerable<T> GetServers<T>(this HashRing<T> ring, string key, int count) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(key);

        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        return ring.GetServers(keyBytes, count);
    }

    /// <summary>
    /// Gets multiple servers that should handle the specified GUID key, in preference order.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The GUID key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order.</returns>
    public static IEnumerable<T> GetServers<T>(this HashRing<T> ring, Guid key, int count) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = key.ToByteArray();
        return ring.GetServers(keyBytes, count);
    }

    /// <summary>
    /// Gets multiple servers that should handle the specified integer key, in preference order.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The integer key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order.</returns>
    public static IEnumerable<T> GetServers<T>(this HashRing<T> ring, int key, int count) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.GetServers(keyBytes, count);
    }

    /// <summary>
    /// Gets multiple servers that should handle the specified long integer key, in preference order.
    /// </summary>
    /// <typeparam name="T">The type of server identifiers.</typeparam>
    /// <param name="ring">The hash ring instance.</param>
    /// <param name="key">The long integer key to find servers for.</param>
    /// <param name="count">The maximum number of servers to return.</param>
    /// <returns>An enumerable of servers in preference order.</returns>
    public static IEnumerable<T> GetServers<T>(this HashRing<T> ring, long key, int count) where T : IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(ring);

        byte[] keyBytes = BitConverter.GetBytes(key);
        return ring.GetServers(keyBytes, count);
    }
}
