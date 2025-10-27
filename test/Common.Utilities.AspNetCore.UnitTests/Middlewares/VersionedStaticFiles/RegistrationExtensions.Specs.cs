#nullable enable
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.VersionedStaticFiles;

public class RegistrationExtensionsSpecs
{
    [Fact]
    public void AddVersionedStaticFiles_Should_Register_Options_In_Service_Collection()
    {
        var services = new ServiceCollection();
        var configuredOptions = new MiddlewareOptions
        {
            StaticFilesPathPrefix = "/custom/",
            StaticFilesDirectory = "custom/path",
            VersionCookieNamePrefix = ".custom_version"
        };

        services.AddVersionedStaticFiles(options =>
        {
            options.StaticFilesPathPrefix = configuredOptions.StaticFilesPathPrefix;
            options.StaticFilesDirectory = configuredOptions.StaticFilesDirectory;
            options.VersionCookieNamePrefix = configuredOptions.VersionCookieNamePrefix;
        });

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal(configuredOptions.StaticFilesPathPrefix, registeredOptions.StaticFilesPathPrefix);
        Assert.Equal(configuredOptions.StaticFilesDirectory, registeredOptions.StaticFilesDirectory);
        Assert.Equal(configuredOptions.VersionCookieNamePrefix, registeredOptions.VersionCookieNamePrefix);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Apply_Configuration_Action()
    {
        var services = new ServiceCollection();
        const string customPrefix = "/my-static/";

        services.AddVersionedStaticFiles(options => options.StaticFilesPathPrefix = customPrefix);

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal(customPrefix, registeredOptions.StaticFilesPathPrefix);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Register_Options_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddVersionedStaticFiles(options => options.StaticFilesPathPrefix = "/test/");

        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<MiddlewareOptions>();
        var instance2 = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Return_ServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddVersionedStaticFiles(_ => { });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Throw_When_Services_Is_Null()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddVersionedStaticFiles(_ => { }));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Throw_When_Configure_Is_Null()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddVersionedStaticFiles(null!));

        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Allow_Custom_UseConfiguredVersion_Function()
    {
        var services = new ServiceCollection();
        Func<MiddlewareOptions.MiddlewareContext, bool> customFunction = _ => true;

        services.AddVersionedStaticFiles(options => options.UseConfiguredVersion = customFunction);

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Same(customFunction, registeredOptions.UseConfiguredVersion);
    }

    [Fact]
    public void UseVersionedStaticFiles_Should_Return_ApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MiddlewareOptions());
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        var result = app.UseVersionedStaticFiles();

        Assert.NotNull(result);
    }

    [Fact]
    public void UseVersionedStaticFiles_Should_Throw_When_Builder_Is_Null()
    {
        IApplicationBuilder builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(builder.UseVersionedStaticFiles);

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Use_Default_Options_When_Not_Modified()
    {
        var services = new ServiceCollection();

        services.AddVersionedStaticFiles(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal(".aa_sfv_", registeredOptions.VersionCookieNamePrefix);
        Assert.Equal("/static/", registeredOptions.StaticFilesPathPrefix);
        Assert.Equal("wwwroot/static", registeredOptions.StaticFilesDirectory);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Allow_Partial_Configuration()
    {
        var services = new ServiceCollection();

        services.AddVersionedStaticFiles(options =>
        {
            options.StaticFilesPathPrefix = "/assets/";
            // Leave other properties at default
        });

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<MiddlewareOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal("/assets/", registeredOptions.StaticFilesPathPrefix);
        Assert.Equal(".aa_sfv_", registeredOptions.VersionCookieNamePrefix); // Still default
        Assert.Equal("wwwroot/static", registeredOptions.StaticFilesDirectory); // Still default
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Register_Default_Provider_When_No_Custom_Provider_Specified()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVersionedStaticFiles(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IStaticAssetsProvider>();

        Assert.NotNull(provider);
        Assert.IsType<DiskStaticAssetsProvider>(provider);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Register_Custom_Provider_When_Provided()
    {
        var services = new ServiceCollection();
        var customProvider = new TestStaticAssetsProvider();

        services.AddVersionedStaticFiles(_ => { }, customProvider);

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IStaticAssetsProvider>();

        Assert.NotNull(provider);
        Assert.Same(customProvider, provider);
    }

    [Fact]
    public void AddVersionedStaticFiles_Should_Register_Custom_Provider_As_Singleton()
    {
        var services = new ServiceCollection();
        var customProvider = new TestStaticAssetsProvider();

        services.AddVersionedStaticFiles(_ => { }, customProvider);

        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<IStaticAssetsProvider>();
        var instance2 = serviceProvider.GetService<IStaticAssetsProvider>();

        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
        Assert.Same(customProvider, instance1);
    }

    private class TestStaticAssetsProvider : IStaticAssetsProvider
    {
        public Task<VersionFilePayload?> ReadVersionFileAsync(string targetDirectory, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<VersionFilePayload?>(null);
        }

        public Task EnsureDirectoryExistsAsync(string targetDirectory, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureVersionDirectoryExistsAsync(string targetDirectory, string version, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
