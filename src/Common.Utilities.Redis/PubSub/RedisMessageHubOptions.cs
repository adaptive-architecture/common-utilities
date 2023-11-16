using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.Redis.Serialization.Contracts;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

namespace AdaptArch.Common.Utilities.Redis.PubSub;

/// <summary>
/// Configuration options for <see cref="RedisMessageHub"/>.
/// </summary>
public class RedisMessageHubOptions: MessageHubOptions
{
    /// <summary>
    /// Create a new instance.
    /// </summary>
    /// <param name="jsonSerializerContext">The <see cref="JsonSerializerContext"/></param>
    public RedisMessageHubOptions(JsonSerializerContext jsonSerializerContext)
    {
        var serializerContext = jsonSerializerContext
                                ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
        DataSerializer = new JsonDataSerializer(serializerContext);
    }

    /// <summary>
    /// The <see cref="IDataSerializer"/> to read/write the data to Redis.
    /// </summary>
    public IDataSerializer DataSerializer { get; set; }
}
