using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.Serialization.Contracts;

namespace AdaptArch.Common.Utilities.Postgres.Serialization.Implementations;

/// <summary>
/// A <see cref="IStringDataSerializer"/> that uses <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonDataSerializer"/> class.
/// </remarks>
public class JsonDataSerializer : IStringDataSerializer
{
    private readonly JsonSerializerContext? _jsonSerializerContext;

    /// <inheritdoc />
    public JsonDataSerializer(JsonSerializerContext? jsonSerializerContext = null)
    {
        _jsonSerializerContext = jsonSerializerContext ?? new DefaultPostgresJsonSerializerContext();
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string? data)
    {
        if (String.IsNullOrWhiteSpace(data))
        {
            return default;
        }
        var obj = JsonSerializer.Deserialize(data!, typeof(T), _jsonSerializerContext!);
        if (obj == default)
        {
            return default;
        }

        return (T)obj;
    }

    /// <inheritdoc />
    public string Serialize<T>(T data) => JsonSerializer.Serialize(data, typeof(T), _jsonSerializerContext!);
}
