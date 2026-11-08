// Keep this in the "Microsoft.Extensions.DependencyInjection" namespace for easy access.
// ReSharper disable once CheckNamespace
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding the extensionless static files middleware to the application.
/// </summary>
public static class ExtensionlessStaticFilesRegistrationExtensions
{
    /// <summary>
    /// Adds the extensionless static files middleware options to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The action used to configure the middleware options.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddExtensionlessStaticFiles(
        this IServiceCollection services,
        Action<ExtensionlessStaticFilesOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ExtensionlessStaticFilesOptions();
        configure(options);

        services.AddSingleton(options);

        return services;
    }

    /// <summary>
    /// Adds the extensionless static files middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder, for chaining.</returns>
    /// <remarks>
    /// This overload resolves <see cref="ExtensionlessStaticFilesOptions"/> from the service
    /// collection. Register the options with <see cref="AddExtensionlessStaticFiles"/> first.
    /// </remarks>
    public static IApplicationBuilder UseExtensionlessStaticFiles(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<ExtensionlessStaticFilesMiddleware>();
    }

    /// <summary>
    /// Adds the extensionless static files middleware to the application pipeline using the
    /// supplied configuration.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configure">The action used to configure the middleware options.</param>
    /// <returns>The application builder, for chaining.</returns>
    /// <remarks>
    /// This overload does not require <see cref="AddExtensionlessStaticFiles"/>.
    /// </remarks>
    public static IApplicationBuilder UseExtensionlessStaticFiles(
        this IApplicationBuilder builder,
        Action<ExtensionlessStaticFilesOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ExtensionlessStaticFilesOptions();
        configure(options);

        return builder.UseMiddleware<ExtensionlessStaticFilesMiddleware>(options);
    }
}
