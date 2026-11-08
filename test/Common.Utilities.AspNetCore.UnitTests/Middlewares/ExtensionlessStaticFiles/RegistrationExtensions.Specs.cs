#nullable enable
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ExtensionlessStaticFiles;

public class ExtensionlessStaticFilesRegistrationExtensionsSpecs
{
    [Fact]
    public void AddExtensionlessStaticFiles_Should_Register_Options_In_Service_Collection()
    {
        var services = new ServiceCollection();

        services.AddExtensionlessStaticFiles(options =>
        {
            options.RequestPath = "/static";
            options.Extensions = [".html", ".htm"];
        });

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<ExtensionlessStaticFilesOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal("/static", registeredOptions.RequestPath.Value);
        Assert.Equal(new[] { ".html", ".htm" }, registeredOptions.Extensions);
    }

    [Fact]
    public void AddExtensionlessStaticFiles_Should_Register_Options_As_Singleton()
    {
        var services = new ServiceCollection();

        services.AddExtensionlessStaticFiles(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        var instance1 = serviceProvider.GetService<ExtensionlessStaticFilesOptions>();
        var instance2 = serviceProvider.GetService<ExtensionlessStaticFilesOptions>();

        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void AddExtensionlessStaticFiles_Should_Return_ServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddExtensionlessStaticFiles(_ => { });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExtensionlessStaticFiles_Should_Throw_When_Services_Is_Null()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddExtensionlessStaticFiles(_ => { }));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddExtensionlessStaticFiles_Should_Throw_When_Configure_Is_Null()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services.AddExtensionlessStaticFiles(null!));

        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void AddExtensionlessStaticFiles_Should_Use_Default_Options_When_Not_Modified()
    {
        var services = new ServiceCollection();

        services.AddExtensionlessStaticFiles(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<ExtensionlessStaticFilesOptions>();

        Assert.NotNull(registeredOptions);
        Assert.Equal(PathString.Empty, registeredOptions.RequestPath);
        Assert.Null(registeredOptions.FileProvider);
        Assert.Equal(new[] { ".html" }, registeredOptions.Extensions);
        Assert.Equal(StatusCodes.Status301MovedPermanently, registeredOptions.RedirectStatusCode);
        Assert.NotNull(registeredOptions.ShouldApply);
        Assert.NotNull(registeredOptions.ShouldRedirect);
    }

    [Fact]
    public void UseExtensionlessStaticFiles_Should_Return_ApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ExtensionlessStaticFilesOptions());
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        var result = app.UseExtensionlessStaticFiles();

        Assert.NotNull(result);
    }

    [Fact]
    public void UseExtensionlessStaticFiles_Should_Throw_When_Builder_Is_Null()
    {
        IApplicationBuilder builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.UseExtensionlessStaticFiles());

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void UseExtensionlessStaticFiles_With_Configure_Should_Return_ApplicationBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        var result = app.UseExtensionlessStaticFiles(_ => { });

        Assert.NotNull(result);
    }

    [Fact]
    public void UseExtensionlessStaticFiles_With_Configure_Should_Throw_When_Builder_Is_Null()
    {
        IApplicationBuilder builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.UseExtensionlessStaticFiles(_ => { }));

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void UseExtensionlessStaticFiles_With_Configure_Should_Throw_When_Configure_Is_Null()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        var exception = Assert.Throws<ArgumentNullException>(() =>
            app.UseExtensionlessStaticFiles(null!));

        Assert.Equal("configure", exception.ParamName);
    }
}
