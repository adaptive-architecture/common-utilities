using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Implementation;
using AdaptArch.Common.Utilities.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace AdaptArch.Common.Utilities.Configuration.UnitTests.Providers;

public class CustomConfigurationProviderSpecs
{
    private class ConfigurationData
    {
        public string Foo { get; set; } = String.Empty;
        public string Bar { get; set; } = String.Empty;
    }

    private class CustomConfigurationSection
    {
        public ConfigurationData Data { get; set; } = new();
        public string Hash { get; set; } = String.Empty;
    }

    private readonly IConfigurationBuilder _configurationBuilder = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { "staticSection:foo", "!foo" },
            { "staticSection:bar", "!bar" }
        });

    private readonly ServiceCollection _services = new();

    [Fact]
    public void Should_Throw_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(null!, null!));
        Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(new Mock<IDataProvider>().Object, null!));
    }

    [Fact]
    public void Should_Throw_If_DataProvider_Error_Is_Not_Ignored()
    {
        var dataProviderMoq = new Mock<IDataProvider>();
        dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Throws(new ApplicationException());

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
        });

        Assert.Throws<ApplicationException>(() => _ = _configurationBuilder.Build());
    }

    [Fact]
    public void Should_Not_Throw_If_DataProvider_Error_Is_Ignored()
    {
        var ex = new ApplicationException();
        Exception contextException = null;
        var dataProviderMoq = new Mock<IDataProvider>();
        dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Throws(ex);

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.HandleLoadException = ctx =>
            {
                contextException = ctx.Exception;
                return new LoadExceptionHandlerResult { IgnoreException = true };
            };
        });

        _ = _configurationBuilder.Build();
        Assert.NotNull(contextException);
        Assert.Same(contextException, ex);
    }

    [Fact]
    public void Should_Load_Configuration()
    {
        var dataProviderMoq = new Mock<IDataProvider>();
        dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { "hash", "A" }, { "data/foo", "foo" }, { "data/bar", "bar" }
            });


        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.Prefix = nameof(CustomConfigurationSection);
            opt.Options.OriginalKeyDelimiter = "/";
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);

        _services.AddOptions<ConfigurationData>().BindConfiguration("staticSection");
        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(nameof(CustomConfigurationSection));

        var sp = _services.BuildServiceProvider();
        var staticConfiguration = sp.GetService<IOptionsMonitor<ConfigurationData>>();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();
        
        Assert.NotNull(staticConfiguration);
        Assert.Equal("!foo", staticConfiguration!.CurrentValue.Foo);
        Assert.Equal("!bar", staticConfiguration!.CurrentValue.Bar);
        Assert.NotNull(customConfiguration);
        Assert.Equal("A", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON()
    {
        var dataProviderMoq = new Mock<IDataProvider>();
        dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                {
                    nameof(CustomConfigurationSection),
                    "{\"hash\": \"A\", \"data\": {\"foo\": \"foo\", \"bar\": \"bar\"}}"
                }
            });


        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter);
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(nameof(CustomConfigurationSection));

        var sp = _services.BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.NotNull(customConfiguration);
        Assert.Equal("A", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON_Null()
    {
        var dataProviderMoq = new Mock<IDataProvider>();
        dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { nameof(CustomConfigurationSection), null } });


        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter);
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(nameof(CustomConfigurationSection));

        var sp = _services.BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.NotNull(customConfiguration);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Hash);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Bar);
    }

    [Fact]
    public async  Task Should_Pool_Configuration_Changes()
    {
        var calls = 0;
        Task<string> GetHashFunc() => Task.FromResult((++calls).ToString("D"));

        Task<IReadOnlyDictionary<string, string>> ReadDataFunc() => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
            {
                { "hash", calls.ToString("D") }, { "data:foo", "foo" }, { "data:bar", "bar" }
            });

        var dataProviderMoq = new Mock<IDataProvider>();

        dataProviderMoq.SetupSequence(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Returns(GetHashFunc)
            .Returns(GetHashFunc)
            .Returns(GetHashFunc);

        dataProviderMoq.SetupSequence(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc);


        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.PoolingInterval = TimeSpan.FromMilliseconds(100);
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);

        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(String.Empty);

        var sp = _services.BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.Equal("1", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("2", customConfiguration!.CurrentValue.Hash);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("3", customConfiguration!.CurrentValue.Hash);

        dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Should_Pool_Configuration_Changes_But_Not_Read_If_Same_Hash()
    {
        const int calls = 0;
        Task<string> GetHashFunc() => Task.FromResult(calls.ToString("D"));

        Task<IReadOnlyDictionary<string, string>> ReadDataFunc() => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
            {
                { "hash", calls.ToString("D") }, { "data:foo", "foo" }, { "data:bar", "bar" }
            });

        var dataProviderMoq = new Mock<IDataProvider>();

        dataProviderMoq.SetupSequence(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Returns(GetHashFunc)
            .Returns(GetHashFunc);

        dataProviderMoq.SetupSequence(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc);


        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.PoolingInterval = TimeSpan.FromMilliseconds(100);
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);

        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(String.Empty);

        var sp = _services.BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.Equal("0", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("0", customConfiguration!.CurrentValue.Hash);

        dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Should_Pool_Configuration_Changes_But_DisablePooling_On_Error()
    {
        var calls = 0;
        Task<string> GetHashFunc() => Task.FromResult((++calls).ToString("D"));

        Task<IReadOnlyDictionary<string, string>> ReadDataFunc() => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
            {
                { "hash", calls.ToString("D") }, { "data:foo", "foo" }, { "data:bar", "bar" }
            });

        var dataProviderMoq = new Mock<IDataProvider>();

        dataProviderMoq.SetupSequence(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Returns(GetHashFunc)
            .Returns(GetHashFunc)
            .Throws(new ApplicationException())
            .Returns(GetHashFunc);

        dataProviderMoq.SetupSequence(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc)
            .Returns(ReadDataFunc);


        var exceptions = new List<Exception>();
        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = dataProviderMoq.Object;
            opt.Options.PoolingInterval = TimeSpan.FromMilliseconds(100);
            opt.Options.HandleLoadException = ctx =>
            {
                var result = new LoadExceptionHandlerResult();
                exceptions.Add(ctx.Exception);
                if (!ctx.Reload)
                    return result;

                result.DisablePooling = true;
                result.IgnoreException = true;

                return result;
            };
        });

        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);

        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(String.Empty);

        var sp = _services.BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.Equal("1", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("2", customConfiguration!.CurrentValue.Hash);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("2", customConfiguration!.CurrentValue.Hash);

        await Task.Delay(TimeSpan.FromMilliseconds(120)).ConfigureAwait(false);

        Assert.Equal("2", customConfiguration!.CurrentValue.Hash);

        Assert.NotEmpty(exceptions);
        Assert.True(exceptions[0] is ApplicationException);

        dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
