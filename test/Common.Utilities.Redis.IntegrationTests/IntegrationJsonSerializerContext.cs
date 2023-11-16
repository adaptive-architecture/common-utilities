using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using static AdaptArch.Common.Utilities.Redis.IntegrationTests.PubSub.RedisMessageHubInt;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

[JsonSerializable(typeof(MyMessage))]
//[JsonSerializable(typeof(IMessage<MyMessage>))]
internal partial class IntegrationJsonSerializerContext : JsonSerializerContext;
