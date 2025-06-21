using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

/// <summary>
/// A <see cref="IDataSerializer"/> that uses <see cref="System.Text.Json"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReflectionJsonDataSerializer"/> class.
/// </remarks>
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
public class ReflectionJsonDataSerializer : IDataSerializer
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

        return JsonSerializer.Deserialize<T?>(data.ToString());
    }
}
