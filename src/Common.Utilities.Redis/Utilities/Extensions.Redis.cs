using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Utilities;

internal static class Extensions
{
    // Literal (not Auto): a topic that happens to contain '*' must map to a single
    // channel, never an implicit pattern subscription across the keyspace.
    public static RedisChannel ToChannel(this string topic) => new(topic, RedisChannel.PatternMode.Literal);
}
