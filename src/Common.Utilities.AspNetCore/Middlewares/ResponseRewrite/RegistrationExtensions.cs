using System.Diagnostics.CodeAnalysis;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;
using Microsoft.AspNetCore.Builder;

// Keep this in the "Microsoft.Extensions.DependencyInjection" for easy access.
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding the response rewrite middleware to the application pipeline.
/// </summary>
public static partial class RegistrationExtensions
{
    /// <summary>
    /// Add the response rewriter factory to the service collection.
    /// </summary>
    /// <typeparam name="T">The type of the response rewrite factory.</typeparam>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddResponseRewriterFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services)
        where T : class, IResponseRewriterFactory
    {
        return services.AddSingleton<IResponseRewriterFactory, T>();
    }

    /// <summary>
    /// Add the response rewrite middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    public static IApplicationBuilder UseResponseRewrite(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseRewriteMiddleware>();
    }
}
