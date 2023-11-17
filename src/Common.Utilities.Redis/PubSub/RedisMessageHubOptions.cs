using System.Text.Json;
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
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/></param>
    public RedisMessageHubOptions(JsonSerializerOptions jsonSerializerOptions)
    {
        ArgumentNullException.ThrowIfNull(jsonSerializerOptions, nameof(jsonSerializerOptions));
        DataSerializer = new JsonDataSerializer(jsonSerializerOptions);
    }

    /// <summary>
    /// The <see cref="IDataSerializer"/> to read/write the data to Redis.
    /// </summary>
    public IDataSerializer DataSerializer { get; set; }
}
