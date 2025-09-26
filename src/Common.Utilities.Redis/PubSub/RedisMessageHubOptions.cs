using System.Diagnostics.CodeAnalysis;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

namespace AdaptArch.Common.Utilities.Redis.PubSub;

/// <summary>
/// Configuration options for <see cref="RedisMessageHub"/>.
/// </summary>
/// <remarks>
/// <para>For common scenarios, use the parameterless <see cref="RedisMessageHubOptions()"/> constructor.</para>
/// <para>For AoT scenarios, use the <see cref="RedisMessageHubOptions(IDataSerializer)"/> constructor with a <see cref="JsonDataSerializer"/> instance.</para>
/// </remarks>
public class RedisMessageHubOptions : MessageHubOptions
{
    /// <summary>
    /// Create a new instance for common scenarios.
    /// Uses <see cref="ReflectionJsonDataSerializer"/> which requires runtime reflection.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public RedisMessageHubOptions()
    {
        DataSerializer = new ReflectionJsonDataSerializer();
    }

    /// <summary>
    /// Create a new instance for AoT scenarios.
    /// Use this constructor with <see cref="JsonDataSerializer"/> for ahead-of-time compilation compatibility.
    /// </summary>
    /// <param name="dataSerializer">The data serializer to use. For AoT scenarios, use <see cref="JsonDataSerializer"/>.</param>
    public RedisMessageHubOptions(IDataSerializer dataSerializer)
    {
        ArgumentNullException.ThrowIfNull(dataSerializer);
        DataSerializer = dataSerializer;
    }

    /// <summary>
    /// The <see cref="IDataSerializer"/> to read/write the data to Redis.
    /// </summary>
    public IDataSerializer DataSerializer { get; set; }
}
