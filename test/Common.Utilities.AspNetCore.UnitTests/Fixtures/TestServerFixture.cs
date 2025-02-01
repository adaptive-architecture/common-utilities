using AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;
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
                            .AddResponseRewriterFactory<ResponseRewriterFactory>()
                            .AddRouting();
                    })
                    .Configure(app =>
                    {
                        app
                            .UseRouting()
                            .UseResponseRewrite()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/ping", () => Results.Text("pong"));
                                endpoints.MapGet("/transformed", () => Results.Text("NOT transformed"));
                                endpoints.MapGet("/no-content", () => Results.NoContent());
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
