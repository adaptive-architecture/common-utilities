using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

internal static class ServiceBuilder
{
    public static async Task<IServiceProvider> BeginTestAsync(JobState state, string enabled, Action<ServiceCollection> configure, CancellationToken cancellationToken)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"periodicWorkers:{nameof(RepeatingWorkerConfiguration.Enabled)}", enabled },
                { $"periodicWorkers:{nameof(RepeatingWorkerConfiguration.Interval)}", state.Interval.ToString("c") },
                { $"periodicWorkers:{nameof(RepeatingWorkerConfiguration.InitialDelay)}", state.InitialDelay.ToString("c") },
            });

        var configuration = configurationBuilder.Build();

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(configuration)
            .AddOptions<RepeatingWorkerConfiguration>().BindConfiguration("periodicWorkers");

        serviceCollection
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(state);

        configure(serviceCollection);

        var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cancellationToken);
        }

        state.Start();

        return serviceProvider;
    }

    public static async Task EndTestAsync(JobState state, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(cancellationToken);
        }
        state.Stop();
    }
}
