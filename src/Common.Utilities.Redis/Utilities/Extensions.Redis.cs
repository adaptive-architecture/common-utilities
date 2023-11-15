using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.Utilities;

internal static class Extensions
{
    public static RedisChannel ToChannel(this string topic) => new(topic, RedisChannel.PatternMode.Auto);
}
