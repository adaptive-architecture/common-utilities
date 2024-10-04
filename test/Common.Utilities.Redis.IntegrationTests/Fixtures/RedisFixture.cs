using StackExchange.Redis;
using Testcontainers.Redis;

namespace AdaptArch.Common.Utilities.Redis.IntegrationTests.Fixtures;

public class RedisFixture : IDisposable
{
    private readonly RedisContainer _container;

    public IConnectionMultiplexer Connection { get; init; }

    public RedisFixture()
    {
        _container = new RedisBuilder()
            //.WithImage("redis:7.2")
            .WithImage("valkey/valkey:8")
            .WithPortBinding(6379, true)
            .Build();

        _container.StartAsync().Wait();
        Connection = ConnectionMultiplexer.Connect(_container.GetConnectionString());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Dispose();
            _container.DisposeAsync().AsTask().Wait();
        }
    }

    ~RedisFixture()
    {
        Dispose(false);
    }
}

[CollectionDefinition(CollectionName)]
public class RedisCollection : ICollectionFixture<RedisFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.

    public const string CollectionName = "Redis collection";
}
