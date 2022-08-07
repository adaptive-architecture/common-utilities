using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

internal static class Utilities
{
    public static IConnectionMultiplexer GetDefaultConnectionMultiplexer()
    {
        return ConnectionMultiplexer.Connect("localhost:6379");
    }
}
