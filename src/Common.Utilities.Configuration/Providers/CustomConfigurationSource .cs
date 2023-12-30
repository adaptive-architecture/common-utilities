using AdaptArch.Common.Utilities.Configuration.Contracts;
using Microsoft.Extensions.Configuration;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <summary>
/// A custom implementation for <see cref="IConfigurationSource"/>.
/// </summary>
public class CustomConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The configuration provider options;
    /// </summary>
    public CustomConfigurationProviderOptions Options { get; set; } = new();

    /// <summary>
    /// The configuration data provider.
    /// </summary>
    public IDataProvider? DataProvider { get; set; }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new CustomConfigurationProvider(
#pragma warning disable S112 // General exceptions should never be thrown
            DataProvider ?? throw new NullReferenceException($"The {nameof(DataProvider)} property is null."),
#pragma warning restore S112 // General exceptions should never be thrown
            Options
        );
    }
}
