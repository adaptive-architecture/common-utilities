using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.Redis.UnitTests;

[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IMessage<object>))]
internal partial class TestJsonSerializerContext : JsonSerializerContext;
