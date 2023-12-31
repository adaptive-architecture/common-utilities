using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class PeriodicWorkerConfigurationSpecs
{
    private const string WorkerName = "TestWorker";
    private readonly HashSet<string> ConfigurationIgnoredProperties = new()
    {
        nameof(PeriodicWorkerConfiguration.Overrides)
    };

    [Fact]
    public void Should_Return_Default_Values_When_No_Overrides()
    {
        var configuration = new PeriodicWorkerConfiguration();

        var result = configuration.GetConfiguration(WorkerName);

        Assert.True(result.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(60), result.Period);
        Assert.Equal(TimeSpan.FromSeconds(30), result.InitialDelay);
    }

    [Fact]
    public void Should_Ignore_Overrides_With_Empty_String_Pattern()
    {
        var configuration = new PeriodicWorkerConfiguration
        {
            Overrides =
            [
                new()
                {
                    Pattern = String.Empty,
                    Enabled = false,
                    Period = TimeSpan.FromMinutes(1),
                    InitialDelay = TimeSpan.FromSeconds(10)
                },
                new()
                {
                    Pattern = "NotATest",
                    Enabled = false,
                    Period = TimeSpan.FromMinutes(1),
                    InitialDelay = TimeSpan.FromSeconds(10)
                }
            ]
        };

        var result = configuration.GetConfiguration(String.Empty);

        Assert.True(result.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(60), result.Period);
        Assert.Equal(TimeSpan.FromSeconds(30), result.InitialDelay);
    }

    [Fact]
    public void Should_Apply_All_Overrides()
    {
        var configuration = new PeriodicWorkerConfiguration
        {
            Overrides =
            [
                new()
                {
                    Pattern = "Test",
                    Enabled = false,
                    Period = TimeSpan.FromMinutes(1),
                    InitialDelay = TimeSpan.FromSeconds(10)
                }
            ]
        };

        var result = configuration.GetConfiguration(WorkerName);

        Assert.False(result.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(1), result.Period);
        Assert.Equal(TimeSpan.FromSeconds(10), result.InitialDelay);
    }

    [Fact]
    public void Should_Apply_Present_Overrides()
    {
        var configuration = new PeriodicWorkerConfiguration
        {
            Overrides =
            [
                new()
                {
                    Pattern = "Test",
                }
            ]
        };

        var result = configuration.GetConfiguration(WorkerName);

        Assert.Equal(configuration.Enabled, result.Enabled);
        Assert.Equal(configuration.Period, result.Period);
        Assert.Equal(configuration.InitialDelay, result.InitialDelay);
    }

    [Fact]
    public void Overrides_Should_Override_All_Configuration_Properties()
    {
        var configuration = new PeriodicWorkerConfiguration
        {
            Enabled = true,
            Period = TimeSpan.FromMinutes(60),
            InitialDelay = TimeSpan.FromSeconds(30)
        };

        var overrides = new PeriodicWorkerConfigurationOverride
        {
            Pattern = "Test"
        };

        configuration.Overrides.Add(overrides);

        var configurationProperties = typeof(PeriodicWorkerConfiguration).GetProperties();
        var overrideProperties = typeof(PeriodicWorkerConfigurationOverride).GetProperties();

        foreach (var property in configurationProperties)
        {
            if (ConfigurationIgnoredProperties.Contains(property.Name))
            {
                continue;
            }

            var overrideProperty = overrideProperties.First(x => x.Name == property.Name);

            switch (property.PropertyType)
            {
                case var type when type == typeof(string) /*|| type == typeof(string?)*/:
                    overrideProperty.SetValue(overrides, "Test");
                    break;
                case var type when type == typeof(bool) || type == typeof(bool?):
                    overrideProperty.SetValue(overrides, false);
                    break;
                case var type when type == typeof(TimeSpan) || type == typeof(TimeSpan?):
                    overrideProperty.SetValue(overrides, TimeSpan.FromMilliseconds(173));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown property type: {property.PropertyType}");
            }
        }

        var result = configuration.GetConfiguration(WorkerName);

        foreach (var property in configurationProperties)
        {
            if (ConfigurationIgnoredProperties.Contains(property.Name))
            {
                continue;
            }

            var overrideProperty = overrideProperties.First(x => x.Name == property.Name);

            Assert.Equal(overrideProperty.GetValue(overrides), property.GetValue(result));
        }
    }
}
