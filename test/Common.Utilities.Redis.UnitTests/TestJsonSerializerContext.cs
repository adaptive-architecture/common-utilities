using System.Text.Json.Serialization;

namespace AdaptArch.Common.Utilities.Redis.UnitTests;

[JsonSerializable(typeof(object))]
internal partial class TestJsonSerializerContext : JsonSerializerContext;
