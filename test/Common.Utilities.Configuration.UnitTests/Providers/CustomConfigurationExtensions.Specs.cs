using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace AdaptArch.Common.Utilities.Configuration.UnitTests.Providers;

public class CustomConfigurationExtensionsSpecs
{
    [Fact]
    public void Should_Throw_If_Null_Configuration_Action()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new ConfigurationBuilder().AddCustomConfiguration(null!));
    }

    [Fact]
    public void Should_Throw_If_Null_Data_Provider()
    {
        Assert.Throws<NullReferenceException>(() => _ = new ConfigurationBuilder().AddCustomConfiguration(_ => { }));
    }

    [Fact]
    public void Should_Add_The_DataProvider()
    {
        var builder = new ConfigurationBuilder();
        var mock = Substitute.For<IDataProvider>();
        builder.AddCustomConfiguration(opt => opt.DataProvider = mock);

        Assert.NotEmpty(builder.Sources);
        Assert.Same(mock, ((CustomConfigurationSource)builder.Sources[0]).DataProvider);
    }
}
