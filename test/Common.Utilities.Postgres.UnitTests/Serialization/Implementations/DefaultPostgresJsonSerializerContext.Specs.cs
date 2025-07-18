using AdaptArch.Common.Utilities.Postgres.Serialization.Implementations;
using System.Text.Json;

namespace AdaptArch.Common.Utilities.Postgres.UnitTests.Serialization.Implementations;

public class DefaultPostgresJsonSerializerContextSpecs
{
    private readonly DefaultPostgresJsonSerializerContext _context;

    public DefaultPostgresJsonSerializerContextSpecs()
    {
        _context = new DefaultPostgresJsonSerializerContext();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var context = new DefaultPostgresJsonSerializerContext();

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void GetTypeInfo_ForDictionary_ShouldReturnValidTypeInfo()
    {
        // Act
        var typeInfo = _context.GetTypeInfo(typeof(Dictionary<string, string>));

        // Assert
        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(Dictionary<string, string>), typeInfo.Type);
    }

    [Fact]
    public void GetTypeInfo_ForReadOnlyDictionary_ShouldReturnValidTypeInfo()
    {
        // Act
        var typeInfo = _context.GetTypeInfo(typeof(IReadOnlyDictionary<string, string>));

        // Assert
        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(IReadOnlyDictionary<string, string>), typeInfo.Type);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldSerializeDictionary()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var json = JsonSerializer.Serialize(dictionary, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"key1\":\"value1\"", json);
        Assert.Contains("\"key2\":\"value2\"", json);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldDeserializeDictionary()
    {
        // Arrange
        const string json = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Act
        var dictionary = JsonSerializer.Deserialize(json, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(dictionary);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("value1", dictionary["key1"]);
        Assert.Equal("value2", dictionary["key2"]);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldSerializeReadOnlyDictionary()
    {
        // Arrange
        IReadOnlyDictionary<string, string> readOnlyDictionary = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var json = JsonSerializer.Serialize(readOnlyDictionary, _context.IReadOnlyDictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"key1\":\"value1\"", json);
        Assert.Contains("\"key2\":\"value2\"", json);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldDeserializeReadOnlyDictionary()
    {
        // Arrange
        const string json = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Act
        var readOnlyDictionary = JsonSerializer.Deserialize(json, _context.IReadOnlyDictionaryStringString);

        // Assert
        Assert.NotNull(readOnlyDictionary);
        Assert.Equal(2, readOnlyDictionary.Count);
        Assert.Equal("value1", readOnlyDictionary["key1"]);
        Assert.Equal("value2", readOnlyDictionary["key2"]);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldHandleEmptyDictionary()
    {
        // Arrange
        var emptyDictionary = new Dictionary<string, string>();

        // Act
        var json = JsonSerializer.Serialize(emptyDictionary, _context.DictionaryStringString);
        var deserializedDictionary = JsonSerializer.Deserialize(json, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.Equal("{}", json);
        Assert.NotNull(deserializedDictionary);
        Assert.Empty(deserializedDictionary);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldHandleNullDictionary()
    {
        // Arrange
        Dictionary<string, string> nullDictionary = null;

        // Act
        var json = JsonSerializer.Serialize(nullDictionary, _context.DictionaryStringString);
        var deserializedDictionary = JsonSerializer.Deserialize(json, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.Equal("null", json);
        Assert.Null(deserializedDictionary);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldHandleSpecialCharactersInValues()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            { "key1", "value with \"quotes\"" },
            { "key2", "value with \n newlines" },
            { "key3", "value with \t tabs" }
        };

        // Act
        var json = JsonSerializer.Serialize(dictionary, _context.DictionaryStringString);
        var deserializedDictionary = JsonSerializer.Deserialize(json, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserializedDictionary);
        Assert.Equal(3, deserializedDictionary.Count);
        Assert.Equal("value with \"quotes\"", deserializedDictionary["key1"]);
        Assert.Equal("value with \n newlines", deserializedDictionary["key2"]);
        Assert.Equal("value with \t tabs", deserializedDictionary["key3"]);
    }

    [Fact]
    public void JsonSerializer_WithContext_ShouldHandleUnicodeCharacters()
    {
        // Arrange
        var dictionary = new Dictionary<string, string>
        {
            { "unicode", "🚀 éñglish 中文" },
            { "emoji", "👍✨🎉" }
        };

        // Act
        var json = JsonSerializer.Serialize(dictionary, _context.DictionaryStringString);
        var deserializedDictionary = JsonSerializer.Deserialize(json, _context.DictionaryStringString);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserializedDictionary);
        Assert.Equal(2, deserializedDictionary.Count);
        Assert.Equal("🚀 éñglish 中文", deserializedDictionary["unicode"]);
        Assert.Equal("👍✨🎉", deserializedDictionary["emoji"]);
    }

    [Fact]
    public void Context_ShouldProvideOptionsForDictionaryTypes()
    {
        // Act
        var dictionaryTypeInfo = _context.DictionaryStringString;
        var readOnlyDictionaryTypeInfo = _context.IReadOnlyDictionaryStringString;

        // Assert
        Assert.NotNull(dictionaryTypeInfo);
        Assert.NotNull(readOnlyDictionaryTypeInfo);
        Assert.Equal(typeof(Dictionary<string, string>), dictionaryTypeInfo.Type);
        Assert.Equal(typeof(IReadOnlyDictionary<string, string>), readOnlyDictionaryTypeInfo.Type);
    }
}
