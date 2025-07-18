using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AdaptArch.Common.Utilities.Serialization.Contracts;

namespace AdaptArch.Common.Utilities.Serialization.Implementations;

/// <summary>
/// A <see cref="IStringDataSerializer"/> that uses reflection-based <see cref="System.Text.Json"/> serialization.
/// This implementation does not require source generation and is suitable for scenarios where AOT is not required.
/// </summary>
[RequiresDynamicCode("JSON serialization may require dynamic code generation")]
[RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
public class ReflectionStringJsonDataSerializer : IStringDataSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflectionStringJsonDataSerializer"/> class.
    /// </summary>
    /// <param name="options">The JSON serializer options. If null, default options will be used.</param>
    public ReflectionStringJsonDataSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public string? Serialize<T>(T data)
    {
        if (EqualityComparer<T>.Default.Equals(data, default(T)))
        {
            return null;
        }

        return JsonSerializer.Serialize(data, _options);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string? data)
    {
        if (String.IsNullOrWhiteSpace(data))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(data, _options);
    }
}
