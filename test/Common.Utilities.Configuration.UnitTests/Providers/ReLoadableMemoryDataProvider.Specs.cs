using AdaptArch.Common.Utilities.Configuration.Implementation;
using AdaptArch.Common.Utilities.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Configuration.UnitTests.Providers;

public class ReLoadableMemoryDataProviderSpecs
{
    [Fact]
    public void Should_Throw_If_Null_Configuration_Action()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = new ConfigurationBuilder().AddCustomConfiguration(null!));
    }

    [Fact]
    public void Should_SupportReloading()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddReLoadableMemoryDataProvider(new Dictionary<string, string>
        {
            { "Key1", "Value1" },
            { "Key2", "Value2" }
        });
        var configuration = configurationBuilder.Build();

        Assert.Equal("Value1", configuration["Key1"]);
        Assert.Equal("Value2", configuration["Key2"]);
        Assert.Null(configuration["Key3"]);

        var services = new ServiceCollection();
        _ = services
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(configuration);

        var sp = services.BuildServiceProvider();

        var spConfig = (IConfigurationRoot)sp.GetRequiredService<IConfiguration>();
        var reLoadableProviders = spConfig.Providers
            .OfType<CustomConfigurationProvider>()
            .Select(p => p.GetDataProvider())
            .OfType<ReLoadableMemoryDataProvider>()
            .First();

        reLoadableProviders.ReloadData(new Dictionary<string, string>
        {
            { "Key1", "NewValue1" },
            { "Key3", "Value3" }
        });

        // We still have the old values

        Assert.Equal("Value1", configuration["Key1"]);
        Assert.Equal("Value2", configuration["Key2"]);
        Assert.Null(configuration["Key3"]);

        spConfig.Reload();

        // Now we should have the new values
        Assert.Equal("NewValue1", configuration["Key1"]);
        Assert.Null(configuration["Key2"]);
        Assert.Equal("Value3", configuration["Key3"]);
    }
}
