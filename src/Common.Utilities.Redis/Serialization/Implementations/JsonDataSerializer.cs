using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataSerializer"/> class.
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    /// </summary>
    public JsonDataSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        ArgumentNullException.ThrowIfNull(jsonSerializerOptions, nameof(jsonSerializerOptions));
        _jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                MessagesJsonSerializerContext.Default, jsonSerializerOptions.TypeInfoResolver)
        };
    }

    /// <inheritdoc />
    public RedisValue Serialize<T>(T data) =>
        JsonSerializer.Serialize(data, _jsonSerializerOptions.GetTypeInfo(typeof(T)));

    /// <inheritdoc />
    public T? Deserialize<T>(RedisValue data)
    {
        if (data.IsNullOrEmpty)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return (T?)JsonSerializer.Deserialize(data.ToString(), _jsonSerializerOptions.GetTypeInfo(typeof(T?)));
    }
}
