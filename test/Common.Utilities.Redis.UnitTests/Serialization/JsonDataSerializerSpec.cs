﻿using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.UnitTests.Serialization;

[JsonSerializable(typeof(JsonDataSerializerSpecs.SerializationDataObject))]
public partial class UnitTestJsonSerializerContext : JsonSerializerContext;

public class JsonDataSerializerSpecs
{
    private static readonly JsonDataSerializer s_serializer = new(new UnitTestJsonSerializerContext());

    public record SerializationDataObject
    {
        public string Id { get; init; }
        public DateTime Date { get; init; }
    }

    [Fact]
    public void Should_Serialize_And_Deserialize()
    {
        var data = new SerializationDataObject { Id = "someId", Date = DateTime.UtcNow };

        var json = s_serializer.Serialize(data);

        Assert.NotNull((string)json);

        var dataCopy = s_serializer.Deserialize<SerializationDataObject>(json);
        Assert.Equal(data, dataCopy);
        Assert.Equal(data.Id, dataCopy.Id);
        Assert.Equal(data.Date, dataCopy.Date);
    }

    [Fact]
    public void Should_Throw_When_Deserializing_Null()
    {
        var serializer = new JsonDataSerializer();

        _ = Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<object>(RedisValue.Null));
    }

    [Fact]
    public void Should_Throw_When_Deserializing_Empty()
    {
        var nullValue = s_serializer.Deserialize<SerializationDataObject>("null");
        Assert.Null(nullValue);
    }
}
