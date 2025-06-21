using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

/// <summary>
/// A <see cref="IDataSerializer"/> that uses <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonDataSerializer"/> class.
/// </remarks>
public class JsonDataSerializer : IDataSerializer
{
    private readonly JsonSerializerContext? _jsonSerializerContext;

    /// <inheritdoc />
    public JsonDataSerializer(JsonSerializerContext? jsonSerializerContext = null)
    {
        _jsonSerializerContext = jsonSerializerContext ?? new DefaultJsonSerializerContext();
    }

    /// <inheritdoc />
    public RedisValue Serialize<T>(T data) => JsonSerializer.Serialize(data, typeof(T), _jsonSerializerContext!);

    /// <inheritdoc />
    public T? Deserialize<T>(RedisValue data)
    {
        if (data.IsNullOrEmpty)
        {
            throw new ArgumentNullException(nameof(data));
        }

        using var ms = new MemoryStream(data!);
        var obj = JsonSerializer.Deserialize(ms, typeof(T), _jsonSerializerContext!);
        if (obj == default)
        {
            return default;
        }

        return (T)obj;
    }
}
