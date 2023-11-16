using System.Text.Json.Serialization;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

[JsonSerializable(typeof(object))]
internal partial class IntegrationJsonSerializerContext : JsonSerializerContext;
