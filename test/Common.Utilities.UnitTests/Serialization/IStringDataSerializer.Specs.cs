using AdaptArch.Common.Utilities.Serialization.Contracts;

namespace AdaptArch.Common.Utilities.UnitTests.Serialization;

public class IStringDataSerializerSpecs
{
    private readonly TestStringDataSerializer _serializer;

    public IStringDataSerializerSpecs()
    {
        _serializer = new TestStringDataSerializer();
    }

    [Fact]
    public void Serialize_WithNullObject_ShouldReturnNull()
    {
        // Arrange
        const string nullString = null;

        // Act
        var result = _serializer.Serialize(nullString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_WithValidObject_ShouldReturnString()
    {
        // Arrange
        var testObject = new TestClass { Id = 1, Name = "Test" };

        // Act
        var result = _serializer.Serialize(testObject);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestClass{Id=1,Name=Test}", result);
    }

    [Fact]
    public void Serialize_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var emptyString = String.Empty;

        // Act
        var result = _serializer.Serialize(emptyString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void Serialize_WithDictionary_ShouldReturnSerializedString()
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
        Assert.Contains("key1=value1", result);
        Assert.Contains("key2=value2", result);
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
    public void Deserialize_WithValidString_ShouldReturnDeserializedObject()
    {
        // Arrange
        const string serializedString = "TestClass{Id=42,Name=DeserializedTest}";

        // Act
        var result = _serializer.Deserialize<TestClass>(serializedString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("DeserializedTest", result.Name);
    }

    [Fact]
    public void Deserialize_WithValidStringForDictionary_ShouldReturnDeserializedDictionary()
    {
        // Arrange
        const string serializedString = "key1=value1;key2=value2";

        // Act
        var result = _serializer.Deserialize<Dictionary<string, string>>(serializedString);

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
    public void Serialize_WithComplexObject_ShouldHandleNestedProperties()
    {
        // Arrange
        var complexObject = new ComplexTestClass
        {
            Id = 1,
            Name = "Complex",
            Properties = new Dictionary<string, string>
            {
                { "prop1", "val1" },
                { "prop2", "val2" }
            }
        };

        // Act
        var result = _serializer.Serialize(complexObject);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Complex", result);
        Assert.Contains("prop1=val1", result);
        Assert.Contains("prop2=val2", result);
    }

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
    }

    private class ComplexTestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public Dictionary<string, string> Properties { get; set; } = [];
    }

    /// <summary>
    /// Test implementation of IStringDataSerializer for testing purposes
    /// </summary>
    private class TestStringDataSerializer : IStringDataSerializer
    {
        public string Serialize<T>(T data)
        {
            if (data == null)
                return null;

            return data switch
            {
                string s => s,
                TestClass tc => $"TestClass{{Id={tc.Id},Name={tc.Name}}}",
                ComplexTestClass ctc => $"ComplexTestClass{{Id={ctc.Id},Name={ctc.Name},Props={String.Join(";", ctc.Properties.Select(kvp => $"{kvp.Key}={kvp.Value}"))}}}",
                Dictionary<string, string> dict => String.Join(";", dict.Select(kvp => $"{kvp.Key}={kvp.Value}")),
                _ => data.ToString()
            };
        }

        public T Deserialize<T>(string data)
        {
            if (String.IsNullOrWhiteSpace(data))
                return default;

            var targetType = typeof(T);

            if (targetType == typeof(string))
                return (T)(object)data;

            if (targetType == typeof(TestClass))
            {
                // Parse "TestClass{Id=42,Name=DeserializedTest}"
                var content = data.Trim("TestClass{".ToCharArray()).TrimEnd('}');
                var parts = content.Split(',');
                var id = Int32.Parse(parts[0].Split('=')[1]);
                var name = parts[1].Split('=')[1];
                return (T)(object)new TestClass { Id = id, Name = name };
            }

            if (targetType == typeof(Dictionary<string, string>))
            {
                var dict = new Dictionary<string, string>();
                foreach (var pair in data.Split(';'))
                {
                    var kvp = pair.Split('=');
                    if (kvp.Length == 2)
                        dict[kvp[0]] = kvp[1];
                }
                return (T)(object)dict;
            }

            return default;
        }
    }
}
