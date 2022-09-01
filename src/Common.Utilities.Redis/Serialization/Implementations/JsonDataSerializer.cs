using System.Text.Json;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

/// <summary>
/// A <see cref="IDataSerializer"/> that uses <see cref="System.Text.Json"/>.
/// </summary>
public class JsonDataSerializer : IDataSerializer
{
    /// <inheritdoc />
    public RedisValue Serialize<T>(T data) => JsonSerializer.Serialize(data);

    /// <inheritdoc />
    public T? Deserialize<T>(RedisValue data)
    {
        if (data.IsNullOrEmpty)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return JsonSerializer.Deserialize<T>(data.ToString());
    }
}
