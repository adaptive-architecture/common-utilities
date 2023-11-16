using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

[JsonSerializable(typeof(Message<object>))]
[JsonSerializable(typeof(IMessage<object>))]
internal partial class MessagesJsonSerializerContext : JsonSerializerContext;
