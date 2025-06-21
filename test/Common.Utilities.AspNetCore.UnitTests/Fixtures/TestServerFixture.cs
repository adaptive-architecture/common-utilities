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
                            .Use(async (ctx, next) =>
                            {
                                try
                                {
                                    await next(ctx);
                                }
                                catch
                                {
                                    ctx.Response.StatusCode = 500;
                                }
                            })
                            .UseResponseRewrite()
                            .UseRouting()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/ping", () => Results.Text("pong"));
                                endpoints.MapGet("/transformed", () => Results.Text("NOT transformed"));
                                endpoints.MapGet("/methods-tests", () => Results.Text("tests"));
                                endpoints.MapGet("/no-content", () => Results.NoContent());
                                endpoints.MapGet("/failure", _ => throw new InvalidOperationException("This is a failure route"));
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
