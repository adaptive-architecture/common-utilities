using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.Postgres.Serialization.Implementations;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.Serialization.Implementations;

public partial class JsonDataSerializerSpecs
{
    [JsonSerializable(typeof(TestClass))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
    private partial class TestJsonSerializerContext : JsonSerializerContext;

    private readonly JsonDataSerializer _serializer;

    public JsonDataSerializerSpecs()
    {
        var context = new TestJsonSerializerContext(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _serializer = new JsonDataSerializer(context);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldUseDefaultContext()
    {
        // Act
        var serializer = new JsonDataSerializer(null);

        // Assert
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Constructor_WithCustomContext_ShouldUseProvidedContext()
    {
        // Arrange
        var customContext = new TestJsonSerializerContext();

        // Act
        var serializer = new JsonDataSerializer(customContext);

        // Assert
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Serialize_WithNullObject_ShouldReturnNull()
    {
        // Arrange
        TestClass nullObject = null;

        // Act
        var result = _serializer.Serialize(nullObject);

        // Assert
        Assert.Equal("null", result);
    }

    [Fact]
    public void Serialize_WithValidObject_ShouldReturnJsonString()
    {
        // Arrange
        var testObject = new TestClass { Id = 42, Name = "Test" };

        // Act
        var result = _serializer.Serialize(testObject);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"id\":42", result);
        Assert.Contains("\"name\":\"Test\"", result);
    }

    [Fact]
    public void Serialize_WithEmptyString_ShouldReturnQuotedEmptyString()
    {
        // Arrange
        var emptyString = String.Empty;

        // Act
        var result = _serializer.Serialize(emptyString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("\"\"", result);
    }

    [Fact]
    public void Serialize_WithString_ShouldReturnQuotedString()
    {
        // Arrange
        const string testString = "Hello World";

        // Act
        var result = _serializer.Serialize(testString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("\"Hello World\"", result);
    }

    [Fact]
    public void Serialize_WithDictionary_ShouldReturnJsonDictionary()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = _serializer.Serialize(dictionary);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"key1\":\"value1\"", result);
        Assert.Contains("\"key2\":\"value2\"", result);
    }

    [Fact]
    public void Deserialize_WithNullString_ShouldReturnDefault()
    {
        // Act
        var result = _serializer.Deserialize<TestClass>(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithEmptyString_ShouldReturnDefault()
    {
        // Act
        var result = _serializer.Deserialize<TestClass>(String.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithWhitespaceString_ShouldReturnDefault()
    {
        // Act
        var result = _serializer.Deserialize<TestClass>("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithValidJsonString_ShouldReturnDeserializedObject()
    {
        // Arrange
        const string jsonString = "{\"id\":42,\"name\":\"DeserializedTest\"}";

        // Act
        var result = _serializer.Deserialize<TestClass>(jsonString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("DeserializedTest", result.Name);
    }

    [Fact]
    public void Deserialize_WithValidJsonForDictionary_ShouldReturnDeserializedDictionary()
    {
        // Arrange
        const string jsonString = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Act
        var result = _serializer.Deserialize<Dictionary<string, string>>(jsonString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new TestClass { Id = 123, Name = "RoundTripTest" };

        // Act
        var serialized = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<TestClass>(serialized);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
    }

    [Fact]
    public void Serialize_Deserialize_Dictionary_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new Dictionary<string, string>
        {
            { "test1", "value1" },
            { "test2", "value2" },
            { "test3", "value3" }
        };

        // Act
        var serialized = _serializer.Serialize(original);
        var deserialized = _serializer.Deserialize<Dictionary<string, string>>(serialized);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal("value1", deserialized["test1"]);
        Assert.Equal("value2", deserialized["test2"]);
        Assert.Equal("value3", deserialized["test3"]);
    }

    [Fact]
    public void Deserialize_WithMalformedJson_ShouldThrowJsonException()
    {
        // Arrange
        const string malformedJson = "{\"id\":42,\"name\":\"Test\""; // Missing closing brace

        // Act & Assert
        _ = Assert.Throws<JsonException>(() =>
            _serializer.Deserialize<TestClass>(malformedJson));
    }

    [Fact]
    public void Serialize_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var testObject = new TestClass
        {
            Id = 1,
            Name = "Test with \"quotes\" and \n newlines"
        };

        // Act
        var serialized = _serializer.Serialize(testObject);
        var deserialized = _serializer.Deserialize<TestClass>(serialized);

        // Assert
        Assert.NotNull(serialized);
        Assert.NotNull(deserialized);
        Assert.Equal(testObject.Id, deserialized.Id);
        Assert.Equal(testObject.Name, deserialized.Name);
    }

    [Fact]
    public void Serialize_WithUnicodeCharacters_ShouldPreserveUnicode()
    {
        // Arrange
        var testObject = new TestClass
        {
            Id = 1,
            Name = "Unicode: 🚀 éñglish 中文"
        };

        // Act
        var serialized = _serializer.Serialize(testObject);
        var deserialized = _serializer.Deserialize<TestClass>(serialized);

        // Assert
        Assert.NotNull(serialized);
        Assert.NotNull(deserialized);
        Assert.Equal(testObject.Id, deserialized.Id);
        Assert.Equal(testObject.Name, deserialized.Name);
    }

    [Fact]
    public void Serialize_WithReadOnlyDictionary_ShouldReturnJsonString()
    {
        // Arrange
        IReadOnlyDictionary<string, string> readOnlyDictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var result = _serializer.Serialize(readOnlyDictionary);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("\"key1\":\"value1\"", result);
        Assert.Contains("\"key2\":\"value2\"", result);
    }

    [Fact]
    public void Deserialize_WithNullJsonValue_ShouldReturnDefault()
    {
        // Act
        var result = _serializer.Deserialize<TestClass>("null");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithJsonValueToString_ShouldReturnString()
    {
        // Act
        var result = _serializer.Deserialize<string>("\"test string\"");

        // Assert
        Assert.Equal("test string", result);
    }

    [Fact]
    public void Deserialize_WithJsonValueToInt_ShouldReturnInt()
    {
        // Act
        var result = _serializer.Deserialize<int>("42");

        // Assert
        Assert.Equal(42, result);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
    }
}
