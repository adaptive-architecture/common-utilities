﻿using AdaptArch.Common.Utilities.Configuration.Contracts;
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
    private readonly Mock<IDataProvider>  _dataProviderMoq = new();
    private readonly Exception _getHashException = new ApplicationException();
    private readonly TimeSpan _poolingInterval = TimeSpan.FromMilliseconds(100);
    private readonly TimeSpan _waitInterval = TimeSpan.FromMilliseconds(120);

    private int _hash;
    private string GetHashValue() => (++_hash).ToString("D");
    private string GetConstantHashValue() => _hash.ToString("D");
    private IReadOnlyDictionary<string, string> GetReadValue() => new Dictionary<string, string>
    {
        { "hash", _hash.ToString("D") }, { "data:foo", "foo" }, { "data:bar", "bar" }
    };

    private static void AssertCustomConfiguration(IOptionsMonitor<CustomConfigurationSection> customConfiguration)
    {
        Assert.NotNull(customConfiguration);
        Assert.Equal("A", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    private async Task AssertConfigurationValuesPooling(IOptionsMonitor<CustomConfigurationSection> customConfiguration, string hash, bool wait)
    {
        if (wait)
        {
            await Task.Delay(_waitInterval).ConfigureAwait(false);
        }

        Assert.Equal(hash, customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    private ServiceProvider BuildServiceProvider(string customConfigurationSectionName = "")
    {
        var configuration = _configurationBuilder.Build();
        _services.AddSingleton(configuration);
        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddOptions<ConfigurationData>().BindConfiguration("staticSection");
        _services.AddOptions<CustomConfigurationSection>().BindConfiguration(customConfigurationSectionName);

        return _services.BuildServiceProvider();
    }

    private void SetupGetHashSequence(Func<string> func, int count, int exceptionIndex = -1)
    {
        var seq = _dataProviderMoq.SetupSequence(s => s.GetHashAsync(It.IsAny<CancellationToken>()));
        for (var i = 0; i < count; i++)
        {
            seq = i == exceptionIndex
                ? seq.Throws(_getHashException)
                : seq.Returns(() => Task.FromResult(func()));
        }
    }

    private void SetupReadDataSequence(Func<IReadOnlyDictionary<string, string>> func, int count, int exceptionIndex = -1)
    {
        var seq = _dataProviderMoq.SetupSequence(s => s.ReadDataAsync(It.IsAny<CancellationToken>()));
        for (var i = 0; i < count; i++)
        {
            seq = i == exceptionIndex
                ? seq.Throws(_getHashException)
                : seq.Returns(() => Task.FromResult(func()));
        }
    }

    [Fact]
    public void Should_Throw_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(null!, null!));
        Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(new Mock<IDataProvider>().Object, null!));
    }

    [Fact]
    public void Should_Throw_If_DataProvider_Error_Is_Not_Ignored()
    {
        _dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Throws(_getHashException);

        _configurationBuilder.AddCustomConfiguration(opt => opt.DataProvider = _dataProviderMoq.Object);

        Assert.Throws<ApplicationException>(() => _ = _configurationBuilder.Build());
    }

    [Fact]
    public void Should_Not_Throw_If_DataProvider_Error_Is_Ignored()
    {
        Exception contextException = null;
        _dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .Throws(_getHashException);

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.HandleLoadException = ctx =>
            {
                contextException = ctx.Exception;
                return new LoadExceptionHandlerResult { IgnoreException = true, DisablePooling = true};
            };
        });

        _ = _configurationBuilder.Build();
        Assert.NotNull(contextException);
        Assert.Same(contextException, _getHashException);
    }

    [Fact]
    public void Should_Load_Configuration()
    {
        _dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                { "hash", "A" }, { "data/foo", "foo" }, { "data/bar", "bar" }
            });

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.Prefix = nameof(CustomConfigurationSection);
            opt.Options.OriginalKeyDelimiter = "/";
        });

        var sp = BuildServiceProvider(nameof(CustomConfigurationSection));
        var staticConfiguration = sp.GetService<IOptionsMonitor<ConfigurationData>>();

        Assert.NotNull(staticConfiguration);
        Assert.Equal("!foo", staticConfiguration!.CurrentValue.Foo);
        Assert.Equal("!bar", staticConfiguration!.CurrentValue.Bar);

        AssertCustomConfiguration(sp.GetService<IOptionsMonitor<CustomConfigurationSection>>());
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON()
    {
        _dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                {
                    nameof(CustomConfigurationSection),
                    "{\"hash\": \"A\", \"data\": {\"foo\": \"foo\", \"bar\": \"bar\"}}"
                }
            });

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter);
        });

        var sp = BuildServiceProvider(nameof(CustomConfigurationSection));
        AssertCustomConfiguration(sp.GetService<IOptionsMonitor<CustomConfigurationSection>>());
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON_Null()
    {
        _dataProviderMoq.Setup(s => s.GetHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("A");
        _dataProviderMoq.Setup(s => s.ReadDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { nameof(CustomConfigurationSection), null } });

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter);
        });

        var sp = BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.NotNull(customConfiguration);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Hash);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Bar);
    }

    [Fact]
    public async  Task Should_Pool_Configuration_Changes()
    {
        SetupGetHashSequence(GetHashValue, 3);
        SetupReadDataSequence(GetReadValue, 3);

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.PoolingInterval = _poolingInterval;
        });

        var sp = BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "1", false).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "3", true).ConfigureAwait(false);

        _dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Should_Pool_Configuration_Changes_But_Not_Read_If_Same_Hash()
    {
        SetupGetHashSequence(GetConstantHashValue, 2);
        SetupReadDataSequence(GetReadValue, 3);

        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.PoolingInterval = _poolingInterval;
        });

        var sp = BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "0", false).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "0", true).ConfigureAwait(false);

        _dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Should_Pool_Configuration_Changes_But_DisablePooling_On_Error()
    {
        SetupGetHashSequence(GetHashValue, 4, 2);
        SetupReadDataSequence(GetReadValue, 4);

        var exceptions = new List<Exception>();
        _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMoq.Object;
            opt.Options.PoolingInterval = _poolingInterval;
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

        var sp = BuildServiceProvider();
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "1", false).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true).ConfigureAwait(false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true).ConfigureAwait(false);

        Assert.NotEmpty(exceptions);
        Assert.True(exceptions[0] is ApplicationException);

        _dataProviderMoq.Verify(v => v.GetHashAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _dataProviderMoq.Verify(v => v.ReadDataAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
