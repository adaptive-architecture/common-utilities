using StackExchange.Redis;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests;

internal static class Utilities
{
    private const string RedisHostEnvVar = "REDIS_HOST";
    private const string RedisPortEnvVar = "REDIS_PORT";
    public static IConnectionMultiplexer GetDefaultConnectionMultiplexer()
    {
        var envVars = Environment.GetEnvironmentVariables();
        if (envVars.Contains(RedisHostEnvVar) && envVars.Contains(RedisPortEnvVar))
        {
            return ConnectionMultiplexer.Connect($"{envVars[RedisHostEnvVar]}:{envVars[RedisPortEnvVar]}");
        }
        else
        {
            return ConnectionMultiplexer.Connect("127.0.0.1:6379");
        }
    }
}
