using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Contracts;

/// <summary>
/// A data serialize for Redis.
/// </summary>
public interface IDataSerializer
{
    /// <summary>
    /// Serialize data to <see cref="RedisValue"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="data">The data to serialize.</param>
    RedisValue Serialize<T>(T data);
    /// <summary>
    /// Deserialize data from <see cref="RedisValue"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data.</typeparam>
    /// <param name="data">The data to deserialize.</param>
    T Deserialize<T>(RedisValue data);
}
