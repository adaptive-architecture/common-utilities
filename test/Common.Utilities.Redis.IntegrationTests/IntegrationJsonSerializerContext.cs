using System.Text.Json.Serialization;
using static AdaptArch.Common.Utilities.Redis.IntegrationTests.PubSub.RedisMessageHubInt;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

[JsonSerializable(typeof(MyMessage))]
internal partial class IntegrationJsonSerializerContext : JsonSerializerContext;
