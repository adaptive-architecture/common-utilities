using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AdaptArch.Common.Utilities.Configuration.UnitTests.Providers;

public class CustomConfigurationSourceSpecs
{
    [Fact]
    public void Should_Throw_If_No_Data_Provider_Is_Set()
    {
        Assert.Throws<NullReferenceException>(() => _ = new CustomConfigurationSource().Build(new ConfigurationBuilder()));
    }

    [Fact]
    public void Should_Accept_Setting_Options()
    {
        var options = new CustomConfigurationProviderOptions();
        var source = new CustomConfigurationSource { Options = options };

        Assert.Same(options, source.Options);
    }

    [Fact]
    public void Should_Build_The_Provider()
    {
        var provider = new CustomConfigurationSource
        {
            DataProvider = new Mock<IDataProvider>().Object
        }.Build(new ConfigurationBuilder());

        Assert.NotNull(provider);
    }
}
