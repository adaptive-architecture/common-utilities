using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using MyMessage =  AdaptArch.Common.Utilities.Redis.IntegrationTests.PubSub.RedisMessageHubInt.MyMessage;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

[JsonSerializable(typeof(MyMessage))]
[JsonSerializable(typeof(IMessage<MyMessage>))]
[JsonSerializable(typeof(Message<MyMessage>))]
internal partial class IntegrationJsonSerializerContext : JsonSerializerContext;
