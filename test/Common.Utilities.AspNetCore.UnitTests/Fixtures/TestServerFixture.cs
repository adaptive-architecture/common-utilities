using AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseTransform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Fixtures;

public sealed class TestServerFixture : IDisposable
{
    private readonly IHost _testServer;

    public TestServerFixture()
    {
        _testServer = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddResponseTransformServiceFactory<ResponseTransformServiceFactory>()
                            .AddRouting();
                    })
                    .Configure(app =>
                    {
                        app
                            .UseRouting()
                            .UseResponseTransformer()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/ping", async context => await context.Response.WriteAsync("pong"));
                                endpoints.MapGet("/transformed", async context => await context.Response.WriteAsync("NOT transformed"));
                            });
                    });
            })
            .Start();
    }

    public HttpClient GetClient() => _testServer.GetTestClient();

    public void Dispose()
    {
        _testServer.Dispose();
    }
}

[CollectionDefinition(CollectionName)]
public class TestServerCollection : ICollectionFixture<TestServerFixture>
{
    public const string CollectionName = "TestServer collection";
}
