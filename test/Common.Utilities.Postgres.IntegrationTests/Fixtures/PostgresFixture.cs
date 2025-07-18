using Npgsql;
using Testcontainers.PostgreSql;

namespace AdaptArch.Common.Utilities.Postgres.IntegrationTests.Fixtures;

public class PostgresFixture : IDisposable
{
    private readonly PostgreSqlContainer _container;

    public NpgsqlDataSource DataSource { get; init; }

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithPortBinding(5432, true)
            .Build();

        _container.StartAsync().Wait();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());
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
            DataSource.Dispose();
            _container.DisposeAsync().AsTask().Wait();
        }
    }

    ~PostgresFixture()
    {
        Dispose(false);
    }
}

[CollectionDefinition(CollectionName)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.

    public const string CollectionName = "Postgres collection";
}
