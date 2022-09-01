using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using Moq;

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
        Assert.Throws<NullReferenceException>(() => _ = new ConfigurationBuilder().AddCustomConfiguration(_ => {}));
    }

    [Fact]
    public void Should_Add_The_DataProvider()
    {
        var builder = new ConfigurationBuilder();
        var moq = new Mock<IDataProvider>().Object;
        builder.AddCustomConfiguration(opt => opt.DataProvider = moq);

        Assert.NotEmpty(builder.Sources);
        Assert.Same(moq, ((CustomConfigurationSource)builder.Sources[0]).DataProvider);
    }
}
