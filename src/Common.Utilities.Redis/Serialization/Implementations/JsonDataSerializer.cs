using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;
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
    private readonly JsonSerializerContext _jsonSerializerContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataSerializer"/> class.
    /// <param name="jsonSerializerContext">The <see cref="JsonSerializerContext"/>.</param>
    /// </summary>
    public JsonDataSerializer(JsonSerializerContext jsonSerializerContext)
    {
        _jsonSerializerContext = jsonSerializerContext
            ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

        _jsonSerializerContext.Options.TypeInfoResolverChain.Add(InternalJsonSerializerContext.Default);
    }

    /// <inheritdoc />
    public RedisValue Serialize<T>(T data) => JsonSerializer.Serialize(data, typeof(T), _jsonSerializerContext);

    /// <inheritdoc />
    public T? Deserialize<T>(RedisValue data)
    {
        if (data.IsNullOrEmpty)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return (T?)JsonSerializer.Deserialize(data.ToString(), typeof(T), _jsonSerializerContext);
    }
}

[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(Message<object>))]
[JsonSerializable(typeof(IMessage<object>))]
internal partial class InternalJsonSerializerContext : JsonSerializerContext;
