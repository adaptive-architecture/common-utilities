using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.VersionedStaticFiles;

public sealed class VersionedStaticFilesMiddlewareSpecs : IDisposable
{
    private readonly string _tempDirectory;

    public VersionedStaticFilesMiddlewareSpecs()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"vsf_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task It_Should_Use_Version_From_Cookie_When_Present()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        Assert.Equal("/static/app/v1.2.3/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Use_Version_From_File_When_Cookie_Not_Present()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v2.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js");

        Assert.Equal("/static/app/v2.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Modify_Path_When_Version_File_Not_Found()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js");

        Assert.Equal("/static/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Modify_Path_For_Non_Static_Files()
    {
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/api/data");

        Assert.Equal("/api/data", capturedPath);
    }

    [Fact]
    public async Task It_Should_Use_Configured_Version_When_UseConfiguredVersion_Returns_True()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v3.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = _ => true
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        // Should use v3.0.0 from file, not v1.2.3 from cookie
        Assert.Equal("/static/app/v3.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Pass_Correct_Context_To_UseConfiguredVersion_Callback()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        MiddlewareOptions.MiddlewareContext capturedContext = null;

        await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = context =>
                {
                    capturedContext = context;
                    return false;
                }
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        Assert.NotNull(capturedContext);
        Assert.Equal("app", capturedContext.Directory);
        Assert.Equal("/static/app/index.js", capturedContext.RequestPath);
        Assert.NotNull(capturedContext.HttpContext);
    }

    [Fact]
    public async Task It_Should_Handle_Invalid_Cookie_Format()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v2.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js",
            ".version_v_app=invalid-cookie-format");

        // Should fall back to version file
        Assert.Equal("/static/app/v2.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Handle_Malformed_Version_File()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            "invalid json content",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js");

        // Should not modify path when version file is malformed
        Assert.Equal("/static/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Handle_Different_Static_Path_Prefixes()
    {
        var assetsDirectory = Path.Combine(_tempDirectory, "assets");
        Directory.CreateDirectory(assetsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(assetsDirectory, "version.json"),
            """{"Version":"v1.5.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/assets/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/assets/assets/bundle.js");

        Assert.Equal("/assets/assets/v1.5.0/bundle.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Handle_Multiple_Path_Segments()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        Directory.CreateDirectory(Path.Combine(appDirectory, "js", "modules"));

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/js/modules/main.js",
            ".version_v_app=1609459200~v1.2.3");

        Assert.Equal("/static/app/v1.2.3/js/modules/main.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Handle_Case_Insensitive_Path_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/STATIC/app/index.js");

        Assert.Equal("/STATIC/app/v1.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Use_Correct_Cookie_Name_For_Target_Directory()
    {
        var app1Directory = Path.Combine(_tempDirectory, "app1");
        var app2Directory = Path.Combine(_tempDirectory, "app2");
        Directory.CreateDirectory(app1Directory);
        Directory.CreateDirectory(app2Directory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app1/index.js",
            ".version_v_app1=1609459200~v1.0.0; .version_v_app2=1609459200~v2.0.0");

        // Should use v1.0.0 from app1 cookie, not v2.0.0 from app2 cookie
        Assert.Equal("/static/app1/v1.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Call_Next_Middleware()
    {
        var nextCalled = false;
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var host = CreateTestHost(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            afterMiddleware: app =>
            {
                app.Run(async context =>
                {
                    nextCalled = true;
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("next called");
                });
            });

        var client = host.GetTestClient();
        await client.GetAsync("/static/app/index.js", TestContext.Current.CancellationToken);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task It_Should_Handle_Null_Path()
    {
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/");

        Assert.Equal("/", capturedPath);
    }

    #region Path Validation Tests

    [Fact]
    public async Task It_Should_Not_Process_Empty_String_Path()
    {
        var middlewareProcessed = false;
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var host = CreateTestHost(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = ctx =>
                {
                    middlewareProcessed = true;
                    return false;
                }
            },
            afterMiddleware: app =>
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("OK");
                });
            });

        var client = host.GetTestClient();
        await client.GetAsync("", TestContext.Current.CancellationToken);

        Assert.False(middlewareProcessed);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_With_Partial_Prefix_Match()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        // Path starts with "/stat" but not "/static/"
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/stat/file.js");

        Assert.Equal("/stat/file.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_Without_Trailing_Slash_In_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        // Path is "/static" but prefix is "/static/" - should not match
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static");

        Assert.Equal("/static", capturedPath);
    }

    [Fact]
    public async Task It_Should_Process_Path_With_Mixed_Case_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/StAtIc/app/index.js");

        Assert.Equal("/StAtIc/app/v1.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Process_Path_With_Lowercase_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js");

        Assert.Equal("/static/app/v1.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_Starting_With_Different_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/assets/app/index.js");

        Assert.Equal("/assets/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_With_Prefix_In_Middle()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/api/static/app/index.js");

        Assert.Equal("/api/static/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Process_Path_With_Custom_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/custom-assets/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/custom-assets/app/index.js");

        Assert.Equal("/custom-assets/app/v1.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_With_Similar_But_Different_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static-files/app/index.js");

        Assert.Equal("/static-files/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Path_With_Prefix_Without_Trailing_Slash()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);

        // Prefix configured without trailing slash won't match properly
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/app/index.js");

        // Should not modify because prefix "/static" doesn't match "/static/"
        Assert.Equal("/static/app/index.js", capturedPath);
    }

    [Fact]
    public async Task It_Should_Not_Process_Whitespace_Only_Path()
    {
        var middlewareProcessed = false;

        var host = CreateTestHost(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = ctx =>
                {
                    middlewareProcessed = true;
                    return false;
                }
            },
            afterMiddleware: app =>
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("OK");
                });
            });

        var client = host.GetTestClient();
        // Most HTTP clients won't allow whitespace-only paths, but we test with empty
        await client.GetAsync("", TestContext.Current.CancellationToken);

        Assert.False(middlewareProcessed);
    }

    [Fact]
    public async Task It_Should_Process_Path_With_Special_Characters_In_Directory()
    {
        var specialDirectory = Path.Combine(_tempDirectory, "my-app_v2");
        Directory.CreateDirectory(specialDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(specialDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/static/my-app_v2/index.js");

        Assert.Equal("/static/my-app_v2/v1.0.0/index.js", capturedPath);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/data")]
    public async Task It_Should_Call_Next_Middleware_For_Non_Matching_Paths(string path)
    {
        var nextCalled = false;

        var host = CreateTestHost(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            afterMiddleware: app =>
            {
                app.Run(async context =>
                {
                    nextCalled = true;
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("next called");
                });
            });

        var client = host.GetTestClient();
        await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task It_Should_Call_Next_Middleware_For_Empty_Paths()
    {
        var nextCalled = false;

        var host = CreateTestHost(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            afterMiddleware: app =>
            {
                app.Run(async context =>
                {
                    nextCalled = true;
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("next called");
                });
            });

        var client = host.GetTestClient();
        await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task It_Should_Preserve_Original_Case_In_Path()
    {
        var appDirectory = Path.Combine(_tempDirectory, "App");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        // Request with mixed case should preserve the case
        var capturedPath = await CaptureModifiedPath(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version"
            },
            "/Static/App/Index.js");

        // Should preserve the original case in the path
        Assert.Equal("/Static/App/v1.0.0/Index.js", capturedPath);
    }

    #endregion

    private static async Task<string> CaptureModifiedPath(MiddlewareOptions options, string requestPath, string cookieHeader = null)
    {
        string capturedPath = null;

        var host = CreateTestHost(options, afterMiddleware: app =>
        {
            app.Run(async context =>
            {
                capturedPath = context.Request.Path.Value;
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OK");
            });
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestPath);

        if (!String.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        return capturedPath;
    }

    private static IHost CreateTestHost(MiddlewareOptions options, Action<IApplicationBuilder> afterMiddleware = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(options);
                        services.AddLogging();
                        services.AddSingleton<IStaticAssetsProvider, DiskStaticAssetsProvider>();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<VersionedStaticFilesMiddleware>();
                        afterMiddleware?.Invoke(app);
                        app.Run(async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        });
                    });
            })
            .Start();
    }
}
