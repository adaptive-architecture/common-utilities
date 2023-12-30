using AdaptArch.Common.Utilities.Configuration.Providers;

// Keep this in the "Microsoft.Extensions.Configuration" for easy access.
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder "/>.
/// </summary>
public static class CustomConfigurationExtensions
{
    /// <summary>
    /// Add a <see cref="CustomConfigurationProvider"/> provider to the configuration data sources.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configureSource">The configuration options.</param>
    /// <remarks>
    /// To get data from the existing configuration follow the instructions from here: https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider.
    /// </remarks>
    public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder builder, Action<CustomConfigurationSource> configureSource)
    {
        ArgumentNullException.ThrowIfNull(configureSource);

        var source = new CustomConfigurationSource();
        configureSource(source);

#pragma warning disable S112 // General exceptions should never be thrown
        if (source.DataProvider == null)
            throw new NullReferenceException($"The source's {nameof(source.DataProvider)} property is null.");
#pragma warning restore S112 // General exceptions should never be thrown

        return builder.Add(source);
    }
}
