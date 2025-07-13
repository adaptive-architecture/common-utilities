using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.Redis.Serialization.Implementations;

/// <summary>
/// A <see cref="JsonSerializerContext"/> that provides serialization support for common types.
/// <see cref="System.Text.Json"/>.
/// </summary>
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(DateOnly))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(sbyte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(TimeOnly))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(Uri))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(Version))]
[JsonSerializable(typeof(Message<object>))]
[JsonSerializable(typeof(IMessage<object>))]
public partial class DefaultJsonSerializerContext : JsonSerializerContext;
