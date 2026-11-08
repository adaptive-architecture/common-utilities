#nullable enable
using System.Net;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ExtensionlessStaticFiles;

public sealed class ExtensionlessStaticFilesMiddlewareSpecs : IDisposable
{
    private readonly string _webRoot;

    public ExtensionlessStaticFilesMiddlewareSpecs()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), $"esf_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_webRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, true);
        }
    }

    [Fact]
    public async Task It_Should_Serve_An_Extensionless_Path_From_An_Html_File()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("about", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task It_Should_Leave_Paths_That_Already_Have_An_Extension_Alone()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.css.html"),
            "wrong",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about.css", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Pass_Through_When_No_Candidate_Exists()
    {
        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/missing", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Use_The_Configured_Extension_Order()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.htm"),
            "htm",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "html",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".htm", ".html"]);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about", TestContext.Current.CancellationToken);

        Assert.Equal("htm", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task It_Should_Not_Process_Post_Requests()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var content = new StringContent(String.Empty);
        using var response = await client.PostAsync("/about", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Process_Paths_Ending_With_A_Slash()
    {
        var capturedPath = await CapturePathAsync(
            options => options.Extensions = [".html"],
            "GET",
            "/docs/");

        Assert.Equal("/docs/", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Shadow_An_Existing_Extensionless_File()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about"),
            "exact",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "html",
            TestContext.Current.CancellationToken);

        var capturedPath = await CapturePathAsync(
            options => options.Extensions = [".html"],
            "GET",
            "/about");

        Assert.Equal("/about", capturedPath);
    }

    [Fact]
    public async Task It_Should_Scope_Processing_With_ShouldApply()
    {
        var apiDirectory = Path.Combine(_webRoot, "api");
        Directory.CreateDirectory(apiDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(apiDirectory, "status.html"),
            "status",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options =>
        {
            options.Extensions = [".html"];
            options.ShouldApply = context => !context.Request.Path.StartsWithSegments("/api");
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/api/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Serve_From_A_Scoped_Path()
    {
        var staticDirectory = Path.Combine(_webRoot, "static");
        Directory.CreateDirectory(staticDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(staticDirectory, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options =>
        {
            options.Extensions = [".html"];
            options.ShouldApply = context => context.Request.Path.StartsWithSegments("/static");
        });
        using var client = host.GetTestClient();

        using var scopedResponse = await client.GetAsync("/static/about", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, scopedResponse.StatusCode);
        Assert.Equal("about", await scopedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        using var unscopedResponse = await client.GetAsync("/about", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unscopedResponse.StatusCode);
    }

    [Fact]
    public async Task It_Should_Respect_RequestPath_And_FileProvider()
    {
        var staticDirectory = Path.Combine(_webRoot, "static");
        Directory.CreateDirectory(staticDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(staticDirectory, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var provider = new PhysicalFileProvider(staticDirectory);
        using var host = CreateHost(
            options =>
            {
                options.RequestPath = "/static";
                options.FileProvider = provider;
                options.Extensions = [".html"];
            },
            staticFileOptions: new StaticFileOptions
            {
                RequestPath = "/static",
                FileProvider = provider
            });
        using var client = host.GetTestClient();

        using var scopedResponse = await client.GetAsync("/static/about", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, scopedResponse.StatusCode);
        Assert.Equal("about", await scopedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        using var unscopedResponse = await client.GetAsync("/about", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unscopedResponse.StatusCode);
    }

    [Fact]
    public async Task It_Should_Normalize_A_Trailing_Slash_In_RequestPath()
    {
        var staticDirectory = Path.Combine(_webRoot, "static");
        Directory.CreateDirectory(staticDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(staticDirectory, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var provider = new PhysicalFileProvider(staticDirectory);
        using var host = CreateHost(
            options =>
            {
                options.RequestPath = "/static/";
                options.FileProvider = provider;
                options.Extensions = [".html"];
            },
            staticFileOptions: new StaticFileOptions
            {
                RequestPath = "/static",
                FileProvider = provider
            });
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/static/about", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Match_A_Prefix_That_Is_Not_A_Path_Segment()
    {
        var static2Directory = Path.Combine(_webRoot, "static2");
        Directory.CreateDirectory(static2Directory);
        await File.WriteAllTextAsync(
            Path.Combine(static2Directory, "about.html"),
            "wrong",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options =>
        {
            options.RequestPath = "/static";
            options.Extensions = [".html"];
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/static2/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Redirect_When_ShouldRedirect_Returns_True()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options =>
        {
            options.Extensions = [".html"];
            options.ShouldRedirect = _ => true;
            options.RedirectStatusCode = StatusCodes.Status302Found;
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about?x=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/about.html", response.Headers.Location.AbsolutePath);
        Assert.Equal("?x=1", response.Headers.Location.Query);
    }

    [Fact]
    public async Task It_Should_Pass_The_Original_And_Candidate_Paths_To_ShouldRedirect()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        var capturedOriginalPath = String.Empty;
        var capturedCandidatePath = String.Empty;
        var capturedQueryString = String.Empty;

        using var host = CreateHost(options =>
        {
            options.Extensions = [".html"];
            options.ShouldRedirect = context =>
            {
                capturedOriginalPath = context.OriginalPath.Value ?? String.Empty;
                capturedCandidatePath = context.CandidatePath.Value ?? String.Empty;
                capturedQueryString = context.HttpContext.Request.QueryString.Value ?? String.Empty;
                return false;
            };
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about?x=1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/about", capturedOriginalPath);
        Assert.Equal("/about.html", capturedCandidatePath);
        Assert.Equal("?x=1", capturedQueryString);
    }

    [Fact]
    public async Task It_Should_Use_The_Web_Root_When_File_Provider_Is_Null()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options =>
        {
            options.FileProvider = null;
            options.Extensions = [".html"];
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Work_After_The_Versioned_Static_Files_Middleware()
    {
        await CreateVersionedAppAsync();

        using var host = CreateVersionedHost(app =>
        {
            _ = app.UseVersionedStaticFiles();
            _ = app.UseExtensionlessStaticFiles();
            _ = app.UseStaticFiles();
        });

        using var client = host.GetTestClient();
        using var response = await client.GetAsync(
            "/static/app/index",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "versioned index",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task It_Should_Serve_The_Versioned_Directory_Index_With_Default_Files()
    {
        await CreateVersionedAppAsync();

        using var host = CreateVersionedHost(app =>
        {
            _ = app.UseVersionedStaticFiles();
            _ = app.UseExtensionlessStaticFiles();
            _ = app.UseDefaultFiles();
            _ = app.UseStaticFiles();
        });

        using var client = host.GetTestClient();
        using var response = await client.GetAsync(
            "/static/app/",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "versioned index",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task It_Should_Not_Resolve_A_Versioned_Directory_Without_A_Trailing_Slash_With_Default_Files()
    {
        await CreateVersionedAppAsync();

        using var host = CreateVersionedHost(app =>
        {
            _ = app.UseVersionedStaticFiles();
            _ = app.UseExtensionlessStaticFiles();
            _ = app.UseDefaultFiles();
            _ = app.UseStaticFiles();
        });

        using var client = host.GetTestClient();
        using var response = await client.GetAsync(
            "/static/app",
            TestContext.Current.CancellationToken);

        // The versioned middleware only rewrites paths that have a segment after the target
        // directory, so "/static/app" is never versioned. UseDefaultFiles also does not append
        // the slash because the unversioned directory has no index file; the index lives under
        // v1.0.0. Use "/static/app/" instead.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Work_After_Versioning_With_A_Dedicated_File_Provider()
    {
        var staticRoot = Path.Combine(_webRoot, "static");
        var appDirectory = Path.Combine(staticRoot, "app");
        var versionDirectory = Path.Combine(appDirectory, "v1.0.0");
        Directory.CreateDirectory(versionDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "index.html"),
            "versioned index",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2024-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken);

        using var provider = new PhysicalFileProvider(staticRoot);

        using var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .UseWebRoot(_webRoot)
                    .ConfigureServices(services =>
                    {
                        _ = services.AddLogging();
                        _ = services.AddVersionedStaticFiles(options =>
                        {
                            options.StaticFilesPathPrefix = "/static/";
                            options.StaticFilesDirectory = staticRoot;
                        });
                        _ = services.AddExtensionlessStaticFiles(options =>
                        {
                            options.RequestPath = "/static";
                            options.FileProvider = provider;
                            options.Extensions = [".html"];
                            options.ShouldApply = context =>
                                context.Request.Path.StartsWithSegments("/static");
                        });
                    })
                    .Configure(app =>
                    {
                        _ = app.UseVersionedStaticFiles();
                        _ = app.UseExtensionlessStaticFiles();
                        _ = app.UseStaticFiles(new StaticFileOptions
                        {
                            RequestPath = "/static",
                            FileProvider = provider
                        });
                    });
            })
            .Start();

        using var client = host.GetTestClient();
        using var response = await client.GetAsync(
            "/static/app/index",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "versioned index",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task It_Should_Probe_Relative_Paths_Without_A_Leading_Slash()
    {
        var provider = new RecordingFileProvider();

        using var host = CreateHost(options =>
        {
            options.FileProvider = provider;
            options.Extensions = [".html"];
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync(
            "/docs/about",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEmpty(provider.RequestedPaths);
        Assert.All(provider.RequestedPaths, path => Assert.False(path.StartsWith('/')));
    }

    [Fact]
    public async Task It_Should_Be_A_No_Op_When_The_File_Provider_Is_Empty()
    {
        using var host = CreateHost(options =>
        {
            options.FileProvider = new NullFileProvider();
            options.Extensions = [".html"];
        });
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Serve_Head_Requests()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_webRoot, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/about");
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, response.Content.Headers.ContentLength);
    }

    [Fact]
    public async Task It_Should_Probe_Paths_Whose_Last_Segment_Has_No_Extension_Even_If_A_Parent_Directory_Has_A_Dot()
    {
        var versionedDirectory = Path.Combine(_webRoot, "v1.2");
        Directory.CreateDirectory(versionedDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(versionedDirectory, "about.html"),
            "about",
            TestContext.Current.CancellationToken);

        using var host = CreateHost(options => options.Extensions = [".html"]);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/v1.2/about", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void It_Should_Reject_Extensions_Without_A_Leading_Dot()
    {
        var options = new ExtensionlessStaticFilesOptions
        {
            Extensions = ["html"]
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            new ExtensionlessStaticFilesMiddleware(
                _ => Task.CompletedTask,
                options,
                Substitute.For<IWebHostEnvironment>(),
                Substitute.For<ILogger<ExtensionlessStaticFilesMiddleware>>()));

        Assert.Equal("Extensions", exception.ParamName);
    }

    [Fact]
    public void It_Should_Reject_A_Redirect_Status_Code_Outside_The_3xx_Range()
    {
        var options = new ExtensionlessStaticFilesOptions
        {
            RedirectStatusCode = StatusCodes.Status200OK
        };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExtensionlessStaticFilesMiddleware(
                _ => Task.CompletedTask,
                options,
                Substitute.For<IWebHostEnvironment>(),
                Substitute.For<ILogger<ExtensionlessStaticFilesMiddleware>>()));

        Assert.Equal("RedirectStatusCode", exception.ParamName);
    }

    private async Task CreateVersionedAppAsync()
    {
        var appDirectory = Path.Combine(_webRoot, "static", "app");
        var versionDirectory = Path.Combine(appDirectory, "v1.0.0");
        Directory.CreateDirectory(versionDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(versionDirectory, "index.html"),
            "versioned index",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2024-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken);
    }

    private IHost CreateVersionedHost(Action<IApplicationBuilder> configureApp)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .UseWebRoot(_webRoot)
                    .ConfigureServices(services =>
                    {
                        _ = services.AddLogging();
                        _ = services.AddVersionedStaticFiles(options =>
                        {
                            options.StaticFilesPathPrefix = "/static/";
                            options.StaticFilesDirectory = Path.Combine(_webRoot, "static");
                        });
                        _ = services.AddExtensionlessStaticFiles(options =>
                        {
                            options.Extensions = [".html"];
                            options.ShouldApply = context =>
                                context.Request.Path.StartsWithSegments("/static");
                        });
                    })
                    .Configure(app => configureApp(app));
            })
            .Start();
    }

    private async Task<string> CapturePathAsync(
        Action<ExtensionlessStaticFilesOptions> configure,
        string method,
        string requestPath)
    {
        var capturedPath = String.Empty;

        using var host = CreateHost(configure, app =>
        {
            _ = app.Use(async (HttpContext context, RequestDelegate _) =>
            {
                capturedPath = context.Request.Path.Value ?? String.Empty;
                await context.Response.WriteAsync(capturedPath);
            });
        });

        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), requestPath);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        return capturedPath;
    }

    private IHost CreateHost(
        Action<ExtensionlessStaticFilesOptions> configure,
        Action<IApplicationBuilder>? configureApp = null,
        StaticFileOptions? staticFileOptions = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .UseWebRoot(_webRoot)
                    .ConfigureServices(services =>
                    {
                        _ = services.AddLogging();
                        _ = services.AddExtensionlessStaticFiles(configure);
                    })
                    .Configure(app =>
                    {
                        _ = app.UseExtensionlessStaticFiles();
                        configureApp?.Invoke(app);

                        if (staticFileOptions is null)
                        {
                            _ = app.UseStaticFiles();
                        }
                        else
                        {
                            _ = app.UseStaticFiles(staticFileOptions);
                        }
                    });
            })
            .Start();
    }

    private sealed class RecordingFileProvider : IFileProvider
    {
        public List<string> RequestedPaths { get; } = [];

        public IFileInfo GetFileInfo(string subpath)
        {
            RequestedPaths.Add(subpath);
            return new NotFoundFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) =>
            NotFoundDirectoryContents.Singleton;

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
