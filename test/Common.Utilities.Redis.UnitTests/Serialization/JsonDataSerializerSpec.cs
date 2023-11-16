using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.Serialization;

public class JsonDataSerializerSpecs
{
    public record SerializationDataObject
    {
        public string Id { get; init; }
        public DateTime Date { get; init; }
    }

    [Fact]
    public void Should_Serialize_And_Deserialize()
    {
        var serializer = new JsonDataSerializer(TestJsonSerializerContext.Default);

        var data = new SerializationDataObject { Id = "someId", Date = DateTime.UtcNow };

        var json = serializer.Serialize(data);

        Assert.NotNull((string)json);

        var dataCopy = serializer.Deserialize<SerializationDataObject>(json);
        Assert.Equal(data, dataCopy);
        Assert.Equal(data.Id, dataCopy.Id);
        Assert.Equal(data.Date, dataCopy.Date);
    }

    [Fact]
    public void Should_Throw_When_Deserializing_Null()
    {
        var serializer = new JsonDataSerializer(TestJsonSerializerContext.Default);

        Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<object>(RedisValue.Null));
    }

    [Fact]
    public void Should_Throw_When_Deserializing_Empty()
    {
        var serializer = new JsonDataSerializer(TestJsonSerializerContext.Default);

        var nullValue = serializer.Deserialize<SerializationDataObject>("null");
        Assert.Null(nullValue);
    }
}
