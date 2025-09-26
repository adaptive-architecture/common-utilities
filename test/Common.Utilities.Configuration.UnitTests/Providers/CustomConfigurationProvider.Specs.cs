using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Implementation;
using AdaptArch.Common.Utilities.Configuration.Providers;
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
    private readonly IDataProvider _dataProviderMock = Substitute.For<IDataProvider>();
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
        Assert.Equal("0", customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    private async Task AssertConfigurationValuesPooling(IOptionsMonitor<CustomConfigurationSection> customConfiguration, string hash, bool wait)
    {
        if (wait)
        {
            await Task.Delay(_waitInterval);
        }

        Assert.Equal(hash, customConfiguration!.CurrentValue.Hash);
        Assert.Equal("foo", customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal("bar", customConfiguration!.CurrentValue.Data.Bar);
    }

    private ServiceProvider BuildServiceProvider(Action<CustomConfigurationSource> configureSource, string customConfigurationSectionName = "")
    {
        _ = _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMock;
            opt.Options.PoolingInterval = _poolingInterval;
            configureSource?.Invoke(opt);
        });

        var configuration = _configurationBuilder.Build();
        _ = _services.AddSingleton(configuration);
        _ = _services.AddSingleton<IConfiguration>(configuration);
        _ = _services.AddOptions<ConfigurationData>().BindConfiguration("staticSection");
        _ = _services.AddOptions<CustomConfigurationSection>().BindConfiguration(customConfigurationSectionName);

        return _services.BuildServiceProvider();
    }

    private List<Func<NSubstitute.Core.CallInfo, Task<TResult>>> GetSequence<TResult>(Func<TResult> func, int count, int exceptionIndex)
    {
        var results = new List<Func<NSubstitute.Core.CallInfo, Task<TResult>>>();
        for (var i = 0; i < count; i++)
        {
            if (i == exceptionIndex)
            {
                results.Add(_ => throw _getHashException);
            }
            else
            {
                results.Add(_ => Task.FromResult(func()));
            }
        }

        return results;
    }

    private void SetupGetHashSequence(Func<string> func, int count, int exceptionIndex = -1)
    {
        var results = GetSequence(func, count, exceptionIndex);
        _ = _dataProviderMock.GetHashAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(results[0], [.. results.Skip(1)]);
    }

    private void SetupReadDataSequence(Func<IReadOnlyDictionary<string, string>> func, int count, int exceptionIndex = -1)
    {
        var results = GetSequence(func, count, exceptionIndex);
        _ = _dataProviderMock.ReadDataAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(results[0], [.. results.Skip(1)]);
    }

    [Fact]
    public void Should_Throw_ArgumentNullException()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(null!, null!));
        _ = Assert.Throws<ArgumentNullException>(() => new CustomConfigurationProvider(Substitute.For<IDataProvider>(), null!));
    }

    [Fact]
    public void Should_Throw_If_DataProvider_Error_Is_Not_Ignored()
    {
        _ = _dataProviderMock.GetHashAsync(Arg.Any<CancellationToken>()).ThrowsAsync(_getHashException);

        _ = _configurationBuilder.AddCustomConfiguration(opt => opt.DataProvider = _dataProviderMock);

        _ = Assert.Throws<ApplicationException>(() => _ = _configurationBuilder.Build());
    }

    [Fact]
    public void Should_Not_Throw_If_DataProvider_Error_Is_Ignored()
    {
        Exception contextException = null;
        _ = _dataProviderMock.GetHashAsync(Arg.Any<CancellationToken>()).ThrowsAsync(_getHashException);

        _ = _configurationBuilder.AddCustomConfiguration(opt =>
        {
            opt.DataProvider = _dataProviderMock;
            opt.Options.HandleLoadException = ctx =>
            {
                contextException = ctx.Exception;
                return new LoadExceptionHandlerResult { IgnoreException = true, DisablePooling = true };
            };
        });

        _ = _configurationBuilder.Build();
        Assert.NotNull(contextException);
        Assert.Same(contextException, _getHashException);
    }

    [Fact]
    public void Should_Load_Configuration()
    {
        _ = _dataProviderMock.ReadDataAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new Dictionary<string, string>
        {
            { "hash", "0" }, { "data/foo", "foo" }, { "data/bar", "bar" }
        });

        SetupGetHashSequence(GetConstantHashValue, 1);
        var sp = BuildServiceProvider(opt =>
        {
            opt.Options.PoolingInterval = TimeSpan.Zero;
            opt.Options.Prefix = nameof(CustomConfigurationSection);
            opt.Options.OriginalKeyDelimiter = "/";
        }, nameof(CustomConfigurationSection));
        var staticConfiguration = sp.GetService<IOptionsMonitor<ConfigurationData>>();

        Assert.NotNull(staticConfiguration);
        Assert.Equal("!foo", staticConfiguration!.CurrentValue.Foo);
        Assert.Equal("!bar", staticConfiguration!.CurrentValue.Bar);

        AssertCustomConfiguration(sp.GetService<IOptionsMonitor<CustomConfigurationSection>>());
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON()
    {
        _ = _dataProviderMock.ReadDataAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new Dictionary<string, string>
        {
            {
                nameof(CustomConfigurationSection),
                "{\"hash\": \"0\", \"data\": {\"foo\": \"foo\", \"bar\": \"bar\"}}"
            }
        });

        SetupGetHashSequence(GetConstantHashValue, 1);

        var sp = BuildServiceProvider(opt => opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter),
            nameof(CustomConfigurationSection));
        AssertCustomConfiguration(sp.GetService<IOptionsMonitor<CustomConfigurationSection>>());
    }

    [Fact]
    public void Should_Load_Configuration_From_JSON_Null()
    {
        _ = _dataProviderMock.ReadDataAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(new Dictionary<string, string>
        {
            { nameof(CustomConfigurationSection), null }
        });

        SetupGetHashSequence(GetConstantHashValue, 1);
        var sp = BuildServiceProvider(opt => opt.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter));
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        Assert.NotNull(customConfiguration);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Hash);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Foo);
        Assert.Equal(String.Empty, customConfiguration!.CurrentValue.Data.Bar);
    }

    [RetryFact]
    public async Task Should_Pool_Configuration_Changes()
    {
        SetupGetHashSequence(GetHashValue, 3);
        SetupReadDataSequence(GetReadValue, 3);

        var sp = BuildServiceProvider(_ => { });
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "1", false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true);
        await AssertConfigurationValuesPooling(customConfiguration, "3", true);

        _ = await _dataProviderMock.Received(3).GetHashAsync(Arg.Any<CancellationToken>());
        _ = await _dataProviderMock.Received(3).ReadDataAsync(Arg.Any<CancellationToken>());
    }

    [RetryFact]
    public async Task Should_Pool_Configuration_Changes_But_Not_Read_If_Same_Hash()
    {
        SetupGetHashSequence(GetConstantHashValue, 2);
        SetupReadDataSequence(GetReadValue, 3);

        var sp = BuildServiceProvider(_ => { });
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "0", false);
        await AssertConfigurationValuesPooling(customConfiguration, "0", true);

        _ = await _dataProviderMock.Received(2).GetHashAsync(Arg.Any<CancellationToken>());
        _ = await _dataProviderMock.Received(1).ReadDataAsync(Arg.Any<CancellationToken>());
    }

    [RetryFact]
    public async Task Should_Pool_Configuration_Changes_But_DisablePooling_On_Error()
    {
        SetupGetHashSequence(GetHashValue, 4, 2);
        SetupReadDataSequence(GetReadValue, 4);

        var exceptions = new List<Exception>();
        var sp = BuildServiceProvider(opt =>
        {
            opt.Options.HandleLoadException = ctx =>
            {
                var result = new LoadExceptionHandlerResult();
                exceptions.Add(ctx.Exception);
                if (!ctx.Reload)
                    return result;

                result.DisablePooling = true;

                return result;
            };
        });
        var customConfiguration = sp.GetService<IOptionsMonitor<CustomConfigurationSection>>();

        await AssertConfigurationValuesPooling(customConfiguration, "1", false);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true);
        await AssertConfigurationValuesPooling(customConfiguration, "2", true);

        Assert.NotEmpty(exceptions);
        Assert.True(exceptions[0] is ApplicationException);

        _ = await _dataProviderMock.Received(3).GetHashAsync(Arg.Any<CancellationToken>());
        _ = await _dataProviderMock.Received(2).ReadDataAsync(Arg.Any<CancellationToken>());
    }
}
