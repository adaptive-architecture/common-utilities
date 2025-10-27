using System.Globalization;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.VersionedStaticFiles;

public sealed class MiddlewareOptionsSpecs : IDisposable
{
    private readonly string _tempDirectory;

    public MiddlewareOptionsSpecs()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"vsf_opts_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region UseConfiguredVersion Tests

    [Fact]
    public void UseConfiguredVersion_Should_Have_Default_Value()
    {
        var options = new MiddlewareOptions();

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        var result = options.UseConfiguredVersion(context);

        Assert.False(result);
    }

    [Fact]
    public void UseConfiguredVersion_Should_Accept_Custom_Function()
    {
        var customFunctionCalled = false;
        var options = new MiddlewareOptions
        {
            UseConfiguredVersion = _ =>
            {
                customFunctionCalled = true;
                return true;
            }
        };

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        var result = options.UseConfiguredVersion(context);

        Assert.True(customFunctionCalled);
        Assert.True(result);
    }

    [Fact]
    public void UseConfiguredVersion_Should_Receive_Correct_Context()
    {
        MiddlewareOptions.MiddlewareContext capturedContext = null;
        var options = new MiddlewareOptions
        {
            UseConfiguredVersion = context =>
            {
                capturedContext = context;
                return false;
            }
        };

        var httpContext = new DefaultHttpContext();
        const string expectedDirectory = "app";
        const string expectedPath = "/static/app/index.js";

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = httpContext,
            Directory = expectedDirectory,
            RequestPath = expectedPath
        };

        options.UseConfiguredVersion(context);

        Assert.NotNull(capturedContext);
        Assert.Same(httpContext, capturedContext.HttpContext);
        Assert.Equal(expectedDirectory, capturedContext.Directory);
        Assert.Equal(expectedPath, capturedContext.RequestPath);
    }

    [Fact]
    public void UseConfiguredVersion_Should_Support_Conditional_Logic()
    {
        var options = new MiddlewareOptions
        {
            UseConfiguredVersion = context => context.Directory == "admin"
        };

        var adminContext = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "admin",
            RequestPath = "/static/admin/index.js"
        };

        var appContext = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        Assert.True(options.UseConfiguredVersion(adminContext));
        Assert.False(options.UseConfiguredVersion(appContext));
    }

    [Fact]
    public async Task UseConfiguredVersion_Should_Override_Cookie_When_True()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v3.0.0","Timestamp":"2021-06-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (capturedPath, _) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = _ => true
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        Assert.Equal("/static/app/v3.0.0/index.js", capturedPath);
    }

    [Fact]
    public async Task UseConfiguredVersion_Should_Respect_Cookie_When_False()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v3.0.0","Timestamp":"2021-06-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (capturedPath, _) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = _ => false
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        Assert.Equal("/static/app/v1.2.3/index.js", capturedPath);
    }

    #endregion

    #region UseCacheDuration Tests

    [Fact]
    public void UseCacheDuration_Should_Have_Default_Value()
    {
        var options = new MiddlewareOptions();

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        var result = options.UseCacheDuration(context);

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void UseCacheDuration_Should_Accept_Custom_Function()
    {
        var customFunctionCalled = false;
        var expectedDuration = TimeSpan.FromHours(1);
        var options = new MiddlewareOptions
        {
            UseCacheDuration = _ =>
            {
                customFunctionCalled = true;
                return expectedDuration;
            }
        };

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        var result = options.UseCacheDuration(context);

        Assert.True(customFunctionCalled);
        Assert.Equal(expectedDuration, result);
    }

    [Fact]
    public void UseCacheDuration_Should_Receive_Correct_Context()
    {
        MiddlewareOptions.MiddlewareContext capturedContext = null;
        var options = new MiddlewareOptions
        {
            UseCacheDuration = context =>
            {
                capturedContext = context;
                return TimeSpan.Zero;
            }
        };

        var httpContext = new DefaultHttpContext();
        const string expectedDirectory = "app";
        const string expectedPath = "/static/app/index.js";

        var context = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = httpContext,
            Directory = expectedDirectory,
            RequestPath = expectedPath
        };

        options.UseCacheDuration(context);

        Assert.NotNull(capturedContext);
        Assert.Same(httpContext, capturedContext.HttpContext);
        Assert.Equal(expectedDirectory, capturedContext.Directory);
        Assert.Equal(expectedPath, capturedContext.RequestPath);
    }

    [Fact]
    public void UseCacheDuration_Should_Support_Conditional_Logic()
    {
        var options = new MiddlewareOptions
        {
            UseCacheDuration = context => context.Directory == "assets"
                ? TimeSpan.FromDays(30)
                : TimeSpan.FromHours(1)
        };

        var assetsContext = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "assets",
            RequestPath = "/static/assets/logo.png"
        };

        var appContext = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = new DefaultHttpContext(),
            Directory = "app",
            RequestPath = "/static/app/index.js"
        };

        Assert.Equal(TimeSpan.FromDays(30), options.UseCacheDuration(assetsContext));
        Assert.Equal(TimeSpan.FromHours(1), options.UseCacheDuration(appContext));
    }

    [Fact]
    public async Task UseCacheDuration_Should_Set_Cache_Headers_When_Positive_Duration()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cacheDuration = TimeSpan.FromHours(24);
        var (_, headers) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseCacheDuration = _ => cacheDuration
            },
            "/static/app/index.js");

        Assert.True(headers.ContainsKey("Cache-Control"));
        Assert.Equal($"public, max-age={cacheDuration.TotalSeconds}", headers["Cache-Control"]);
        Assert.True(headers.ContainsKey("Expires"));
        Assert.True(headers.ContainsKey("ETag"));
        Assert.Contains("app-v1.0.0", headers["ETag"]);
    }

    [Fact]
    public async Task UseCacheDuration_Should_Not_Set_Cache_Headers_When_Zero_Duration()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (_, headers) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseCacheDuration = _ => TimeSpan.Zero
            },
            "/static/app/index.js");

        Assert.False(headers.ContainsKey("Cache-Control"));
        Assert.False(headers.ContainsKey("Expires"));
        Assert.False(headers.ContainsKey("ETag"));
    }

    [Fact]
    public async Task UseCacheDuration_Should_Not_Set_Cache_Headers_When_Negative_Duration()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (_, headers) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseCacheDuration = _ => TimeSpan.FromSeconds(-1)
            },
            "/static/app/index.js");

        Assert.False(headers.ContainsKey("Cache-Control"));
        Assert.False(headers.ContainsKey("Expires"));
        Assert.False(headers.ContainsKey("ETag"));
    }

    [Fact]
    public async Task UseCacheDuration_Should_Support_Different_Durations_Per_Directory()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        var assetsDirectory = Path.Combine(_tempDirectory, "assets");
        Directory.CreateDirectory(appDirectory);
        Directory.CreateDirectory(assetsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );
        await File.WriteAllTextAsync(
            Path.Combine(assetsDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var options = new MiddlewareOptions
        {
            StaticFilesPathPrefix = "/static/",
            StaticFilesDirectory = _tempDirectory,
            VersionCookieNamePrefix = ".version",
            UseCacheDuration = context => context.Directory switch
            {
                "app" => TimeSpan.FromHours(1),
                "assets" => TimeSpan.FromDays(30),
                _ => TimeSpan.Zero
            }
        };

        var (_, appHeaders) = await CaptureModifiedPathAndHeaders(options, "/static/app/index.js");
        var (_, assetsHeaders) = await CaptureModifiedPathAndHeaders(options, "/static/assets/logo.png");

        Assert.True(appHeaders.ContainsKey("Cache-Control"));
        Assert.Equal("public, max-age=3600", appHeaders["Cache-Control"]);

        Assert.True(assetsHeaders.ContainsKey("Cache-Control"));
        Assert.Equal("public, max-age=2592000", assetsHeaders["Cache-Control"]);
    }

    [Fact]
    public async Task UseCacheDuration_Should_Support_Path_Based_Logic()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var options = new MiddlewareOptions
        {
            StaticFilesPathPrefix = "/static/",
            StaticFilesDirectory = _tempDirectory,
            VersionCookieNamePrefix = ".version",
            UseCacheDuration = context => context.RequestPath.Contains(".js")
                ? TimeSpan.FromHours(1)
                : TimeSpan.FromDays(7)
        };

        var (_, jsHeaders) = await CaptureModifiedPathAndHeaders(options, "/static/app/main.js");
        var (_, cssHeaders) = await CaptureModifiedPathAndHeaders(options, "/static/app/styles.css");

        Assert.Equal("public, max-age=3600", jsHeaders["Cache-Control"]);
        Assert.Equal("public, max-age=604800", cssHeaders["Cache-Control"]);
    }

    [Fact]
    public async Task UseCacheDuration_Should_Include_Version_In_ETag()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v2.5.3","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (_, headers) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseCacheDuration = _ => TimeSpan.FromHours(1)
            },
            "/static/app/bundle.js");

        Assert.True(headers.ContainsKey("ETag"));
        Assert.Equal("\"app-v2.5.3\"", headers["ETag"]);
    }

    #endregion

    #region CookieExpiration Tests

    [Fact]
    public void CookieExpiration_Should_Have_Default_Value()
    {
        var options = new MiddlewareOptions();

        Assert.Equal(TimeSpan.FromDays(1), options.CookieExpiration);
    }

    [Fact]
    public void CookieExpiration_Should_Accept_Custom_Value()
    {
        var customExpiration = TimeSpan.FromHours(12);
        var options = new MiddlewareOptions
        {
            CookieExpiration = customExpiration
        };

        Assert.Equal(customExpiration, options.CookieExpiration);
    }

    [Fact]
    public async Task CookieExpiration_Should_Be_Applied_To_Version_Cookie()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromHours(6);
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey(".version_v_app"));
        var versionCookie = cookies[".version_v_app"];
        Assert.NotNull(versionCookie.Expires);

        // The cookie expiration should be approximately now + cookieExpiration
        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = versionCookie.Expires.Value;

        // Allow a tolerance of a few seconds for test execution time
        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5, $"Cookie expiration difference was {difference} seconds");
    }

    [Fact]
    public async Task CookieExpiration_Should_Be_Applied_To_Refresh_Cookie()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromHours(6);
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey(".version_r_app"));
        var refreshCookie = cookies[".version_r_app"];
        Assert.NotNull(refreshCookie.Expires);

        // The cookie expiration should be approximately now + cookieExpiration
        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = refreshCookie.Expires.Value;

        // Allow a tolerance of a few seconds for test execution time
        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5, $"Cookie expiration difference was {difference} seconds");
    }

    [Fact]
    public async Task CookieExpiration_Should_Support_Short_Duration()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromMinutes(30);
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey(".version_v_app"));
        var versionCookie = cookies[".version_v_app"];
        Assert.NotNull(versionCookie.Expires);

        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = versionCookie.Expires.Value;

        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5);
    }

    [Fact]
    public async Task CookieExpiration_Should_Support_Long_Duration()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromDays(365);
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey(".version_v_app"));
        var versionCookie = cookies[".version_v_app"];
        Assert.NotNull(versionCookie.Expires);

        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = versionCookie.Expires.Value;

        // For long durations, allow a slightly larger tolerance
        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5);
    }

    [Fact]
    public async Task CookieExpiration_Should_Apply_Same_Expiration_To_Both_Cookies()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromHours(8);

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey(".version_v_app"));
        Assert.True(cookies.ContainsKey(".version_r_app"));

        var versionCookieExpires = cookies[".version_v_app"].Expires.Value;
        var refreshCookieExpires = cookies[".version_r_app"].Expires.Value;

        // Both cookies should have the same expiration time (within a small tolerance)
        var difference = Math.Abs((versionCookieExpires - refreshCookieExpires).TotalSeconds);
        Assert.True(difference < 1, "Version and refresh cookies should have the same expiration");
    }

    [Fact]
    public async Task CookieExpiration_Should_Apply_To_Cookies_With_Custom_Cookie_Prefix()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v1.0.0","Timestamp":"2021-01-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromHours(4);
        const string customPrefix = ".custom_prefix_";
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = customPrefix,
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js");

        Assert.True(cookies.ContainsKey($"{customPrefix}_v_app"));
        var versionCookie = cookies[$"{customPrefix}_v_app"];
        Assert.NotNull(versionCookie.Expires);

        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = versionCookie.Expires.Value;

        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5);
    }

    [Fact]
    public async Task CookieExpiration_Should_Not_Be_Set_When_No_Version_Available()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        // No version.json file created

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = TimeSpan.FromHours(2)
            },
            "/static/app/index.js");

        // No cookies should be set when no version is available
        Assert.Empty(cookies);
    }

    [Fact]
    public async Task CookieExpiration_Should_Work_With_Cookie_From_Request()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v2.0.0","Timestamp":"2021-06-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var cookieExpiration = TimeSpan.FromHours(10);
        var beforeRequest = DateTimeOffset.UtcNow;

        var cookies = await CaptureCookies(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                CookieExpiration = cookieExpiration
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        // Even when cookie is present in request, response should set cookie with new expiration
        Assert.True(cookies.ContainsKey(".version_v_app"));
        var versionCookie = cookies[".version_v_app"];
        Assert.NotNull(versionCookie.Expires);

        var expectedExpiration = beforeRequest.Add(cookieExpiration);
        var actualExpiration = versionCookie.Expires.Value;

        var difference = Math.Abs((actualExpiration - expectedExpiration).TotalSeconds);
        Assert.True(difference < 5);
    }

    #endregion

    #region Combined Behavior Tests

    [Fact]
    public async Task UseConfiguredVersion_And_UseCacheDuration_Should_Work_Together()
    {
        var appDirectory = Path.Combine(_tempDirectory, "app");
        Directory.CreateDirectory(appDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(appDirectory, "version.json"),
            """{"Version":"v5.0.0","Timestamp":"2021-06-01T00:00:00Z"}""",
            TestContext.Current.CancellationToken
        );

        var (capturedPath, headers) = await CaptureModifiedPathAndHeaders(
            new MiddlewareOptions
            {
                StaticFilesPathPrefix = "/static/",
                StaticFilesDirectory = _tempDirectory,
                VersionCookieNamePrefix = ".version",
                UseConfiguredVersion = ctx => ctx.Directory == "app",
                UseCacheDuration = ctx => ctx.Directory == "app" ? TimeSpan.FromDays(1) : TimeSpan.Zero
            },
            "/static/app/index.js",
            ".version_v_app=1609459200~v1.2.3");

        // Should use configured version (v5.0.0) instead of cookie version (v1.2.3)
        Assert.Equal("/static/app/v5.0.0/index.js", capturedPath);

        // Should set cache headers
        Assert.True(headers.ContainsKey("Cache-Control"));
        Assert.Equal("public, max-age=86400", headers["Cache-Control"]);
        Assert.Contains("app-v5.0.0", headers["ETag"]);
    }

    #endregion

    private static async Task<(string capturedPath, Dictionary<string, string> headers)> CaptureModifiedPathAndHeaders(
        MiddlewareOptions options,
        string requestPath,
        string cookieHeader = null)
    {
        string capturedPath = null;
        var capturedHeaders = new Dictionary<string, string>();

        var host = CreateTestHost(options, afterMiddleware: app =>
        {
            app.Run(async context =>
            {
                capturedPath = context.Request.Path.Value;

                context.Response.StatusCode = 200;
                // Write to the response to trigger OnStarting callbacks
                await context.Response.WriteAsync("OK");
            });
        });

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestPath);

        if (!String.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Capture response headers after response is complete
        foreach (var header in response.Headers)
        {
            capturedHeaders[header.Key] = String.Join(", ", header.Value);
        }
        foreach (var header in response.Content.Headers)
        {
            capturedHeaders[header.Key] = String.Join(", ", header.Value);
        }

        return (capturedPath, capturedHeaders);
    }

    private static async Task<Dictionary<string, CookieInfo>> CaptureCookies(
        MiddlewareOptions options,
        string requestPath,
        string cookieHeader = null)
    {
        var capturedCookies = new Dictionary<string, CookieInfo>();

        var host = CreateTestHost(options, afterMiddleware: app =>
        {
            app.Run(async context =>
            {
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

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Parse Set-Cookie headers
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            foreach (var setCookieValue in setCookieHeaders)
            {
                var cookieInfo = ParseSetCookieHeader(setCookieValue);
                if (cookieInfo != null)
                {
                    capturedCookies[cookieInfo.Name] = cookieInfo;
                }
            }
        }

        return capturedCookies;
    }

    private static CookieInfo ParseSetCookieHeader(string setCookieValue)
    {
        var parts = setCookieValue.Split(';');
        if (parts.Length == 0) return null;

        var nameValuePart = parts[0].Trim();
        var equalIndex = nameValuePart.IndexOf('=');
        if (equalIndex <= 0) return null;

        var cookieInfo = new CookieInfo
        {
            Name = nameValuePart[..equalIndex],
            Value = nameValuePart[(equalIndex + 1)..]
        };

        foreach (var part in parts.Skip(1))
        {
            var trimmedPart = part.Trim();
            var partEqualIndex = trimmedPart.IndexOf('=');

            if (partEqualIndex > 0)
            {
                var attributeName = trimmedPart[..partEqualIndex].Trim();
                var attributeValue = trimmedPart[(partEqualIndex + 1)..].Trim();

                if (attributeName.Equals("expires", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTimeOffset.TryParse(attributeValue, CultureInfo.InvariantCulture, out var expiresValue))
                    {
                        cookieInfo.Expires = expiresValue;
                    }
                }
                else if (attributeName.Equals("path", StringComparison.OrdinalIgnoreCase))
                {
                    cookieInfo.Path = attributeValue;
                }
            }
        }

        return cookieInfo;
    }

    private class CookieInfo
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public string Path { get; set; }
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
