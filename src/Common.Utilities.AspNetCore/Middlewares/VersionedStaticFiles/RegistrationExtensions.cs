// Keep this in the "Microsoft.Extensions.DependencyInjection" for easy access.
// ReSharper disable once CheckNamespace
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding the VersionedStaticFilesMiddleware to the application pipeline.
/// </summary>
public static partial class RegistrationExtensions
{
    /// <summary>
    /// Add the VersionedStaticFilesMiddleware to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The action to configure the middleware options.</param>
    /// <param name="provider">Optional custom static assets provider. If null, DiskStaticAssetsProvider will be used.</param>
    public static IServiceCollection AddVersionedStaticFiles(this IServiceCollection services, Action<MiddlewareOptions> configure, IStaticAssetsProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MiddlewareOptions();
        configure(options);

        services.AddSingleton(options);

        if (provider != null)
        {
            services.AddSingleton(provider);
        }
        else
        {
            services.AddSingleton<IStaticAssetsProvider, DiskStaticAssetsProvider>();
        }

        return services;
    }

    /// <summary>
    /// Use the VersionedStaticFilesMiddleware in the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    public static IApplicationBuilder UseVersionedStaticFiles(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<VersionedStaticFilesMiddleware>();
    }
}
