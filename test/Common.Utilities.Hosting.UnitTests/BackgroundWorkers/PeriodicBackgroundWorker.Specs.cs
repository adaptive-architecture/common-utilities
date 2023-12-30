using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ServiceCollection;
using Microsoft.Extensions.Configuration;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration.Memory;


namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class PeriodicBackgroundWorkerSpecs
{
    private static readonly TimeSpan ShortJobDuration = TimeSpan.FromMicroseconds(50);
    private static readonly TimeSpan StandardPeriod = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StandardDelay = TimeSpan.FromSeconds(5);

    private readonly ServiceCollection _serviceCollection;
    private readonly IConfigurationBuilder _configurationBuilder = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Enabled)}", Boolean.TrueString },
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Period)}", StandardPeriod.ToString("c") },
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.InitialDelay)}", StandardDelay.ToString("c") },
        });

    private readonly MemoryConfigurationProvider _memoryConfigurationProvider;

    public class JobState
    {
        public int ExecutionCount;
    }

    public class ShortJob : IJob
    {
        private readonly JobState _jobState;

        public ShortJob(JobState jobState) => _jobState = jobState;

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _jobState.ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    public PeriodicBackgroundWorkerSpecs()
    {
        var configuration = _configurationBuilder.Build();
        _memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();

        _serviceCollection = new ServiceCollection();
        _serviceCollection
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(configuration)
            .AddOptions<PeriodicWorkerConfiguration>().BindConfiguration("periodicWorkers");

        _serviceCollection
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton<JobState>()
            .AddPeriodicBackgroundJobs()
            .WithPeriodicJob<ShortJob>();
    }

    [Fact]
    public async Task Should_Execute_The_Job()
    {
        var serviceProvider = await BeginTestAsync();
        var start = DateTime.UtcNow;
        var state = serviceProvider.GetRequiredService<JobState>();
        var iterationDuration = ShortJobDuration + StandardPeriod;

        while (DateTime.UtcNow - start < StandardDelay)
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(GetEstimatedExecutionCount(ShortJobDuration, start), state.ExecutionCount);
            await Task.Delay(iterationDuration);
        }

        for (var i = 0; i < 3; i++)
        {
            // Now it should be equal to `i` as it should have executed
            Assert.Equal(GetEstimatedExecutionCount(ShortJobDuration, start), state.ExecutionCount);
            await Task.Delay(iterationDuration);
        }

        await EndTestAsync(serviceProvider);
        var stopDateTime = DateTime.UtcNow;
        var finalEstimate = GetEstimatedExecutionCount(ShortJobDuration, start, stopDateTime);

        for (var i = 0; i < 3; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(iterationDuration);
            Assert.Equal(finalEstimate, state.ExecutionCount);
        }
    }

    private async Task<IServiceProvider> BeginTestAsync(CancellationToken cancellationToken = default)
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(cancellationToken);
        }

        return serviceProvider;
    }

    private async Task EndTestAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(cancellationToken);
        }
    }

    private static int GetEstimatedExecutionCount(TimeSpan jobDuration, DateTime start, DateTime? stopDateTime = null)
    {
        var end = stopDateTime ?? DateTime.UtcNow;
        var runningTime = end - start - StandardDelay;
        if (runningTime < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Ceiling(runningTime / (StandardPeriod + jobDuration));
    }
}
