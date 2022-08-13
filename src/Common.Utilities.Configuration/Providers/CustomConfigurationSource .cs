using Microsoft.Extensions.Configuration;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <summary>
/// A custom implementation for <see cref="IConfigurationSource"/>.
/// </summary>
public class CustomConfigurationSource: IConfigurationSource
{
    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder) => throw new NotImplementedException();
}
