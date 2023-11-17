using AdaptArch.Common.Utilities.Redis.PubSub;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.PubSub;

public class RedisMessageHubOptionsSpecs
{
    [Fact]
    public void Should_Have_A_JsonSerialize_By_Default()
    {
        var options = new RedisMessageHubOptions(TestJsonSerializerContext.Default.Options);
        Assert.IsType<JsonDataSerializer>(options.DataSerializer);
    }

    [Fact]
    public void Should_Have_Allow_Replacing_The_Serializer()
    {
        var serializer = new JsonDataSerializer(TestJsonSerializerContext.Default.Options);
        var options = new RedisMessageHubOptions(TestJsonSerializerContext.Default.Options);
        Assert.NotSame(serializer, options.DataSerializer);

        options.DataSerializer = serializer;
        Assert.Same(serializer, options.DataSerializer);
    }
}
