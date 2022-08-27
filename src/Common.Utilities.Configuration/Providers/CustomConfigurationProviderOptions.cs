using AdaptArch.Common.Utilities.Configuration.Contracts;
using Microsoft.Extensions.Configuration;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <summary>
/// Options for the <see cref="CustomConfigurationProvider"/>.
/// </summary>
public class CustomConfigurationProviderOptions
{
    /// <summary>
    /// The interval at which the data provider should reload the configuration.
    /// Default value <see cref="TimeSpan.Zero"/>.
    /// This should be a value grater than <see cref="TimeSpan.Zero"/>.
    /// </summary>
    public TimeSpan PoolingInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Exception handler.
    /// If the function return "true" the exception is considered handled and it will not be re-thrown.
    /// </summary>
    public Func<LoadExceptionContext, LoadExceptionHandlerResult>? HandleLoadException { get; set; }

    /// <summary>
    /// The prefix to used for all the configuration keys.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// A delimiter from the configuration keys that should be replaced by <see cref="ConfigurationPath.KeyDelimiter"/>.
    /// </summary>
    public string? OriginalKeyDelimiter { get; set; }

    /// <summary>
    /// A <see cref="ConfigurationParser"/> to parse the individual values.
    /// </summary>
    public IConfigurationParser? ConfigurationParser { get; set; }
}
