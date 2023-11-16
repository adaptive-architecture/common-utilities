using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.Redis.UnitTests.Serialization;

namespace AdaptArch.Common.Utilities.Redis.UnitTests;

[JsonSerializable(typeof(JsonDataSerializerSpecs.SerializationDataObject))]
internal partial class TestJsonSerializerContext : JsonSerializerContext;
