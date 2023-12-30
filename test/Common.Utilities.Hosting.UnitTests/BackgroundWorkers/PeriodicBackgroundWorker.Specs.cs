using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ServiceCollection;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class PeriodicBackgroundWorkerSpecs
{
    [Theory]
    [InlineData(100, 5_000, 1_000)]
    [InlineData(2_000, 5_000, 1_000)]
    public async Task Should_Execute_The_Job(int executionDurationMs, int initialDelayMs, int periodMs)
    {
        using var cts = new CancellationTokenSource();

        var state = new JobState(TimeSpan.FromMilliseconds(executionDurationMs),
            TimeSpan.FromMilliseconds(initialDelayMs),
            TimeSpan.FromMilliseconds(periodMs));
        var serviceProvider = await BeginTestAsync(state, Boolean.TrueString, cts.Token);

        while (state.Elapsed < state.InitialDelay)
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(state.GetEstimatedExecutionCount(), state.ExecutionCount);
            await Task.Delay(state.ExecutionTime);
        }

        for (var i = 0; i < 3; i++)
        {
            // Now it should be equal to `i` as it should have executed
            Assert.Equal(state.GetEstimatedExecutionCount(), state.ExecutionCount);
            await Task.Delay(state.ExecutionTime);
        }

        await EndTestAsync(state, serviceProvider, cts.Token);
        cts.Cancel();

        var finalEstimate = state.GetEstimatedExecutionCount();

        for (var i = 0; i < 3; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(state.ExecutionTime);
            Assert.Equal(finalEstimate, state.ExecutionCount);
        }
    }

    [Fact]
    public async Task Should_Support_Disabling_After_Starting()
    {
        using var cts = new CancellationTokenSource();

        var state = new JobState(TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(2_000),
            TimeSpan.FromMilliseconds(500));
        var serviceProvider = await BeginTestAsync(state, Boolean.TrueString, cts.Token);

        var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();
        memoryConfigurationProvider.Set($"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Enabled)}", Boolean.FalseString);
        configuration.Reload();

        while (state.Elapsed < TimeSpan.FromMilliseconds(5_000))
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(0, state.ExecutionCount);
            await Task.Delay(state.ExecutionTime);
        }

        await EndTestAsync(state, serviceProvider, cts.Token);
        cts.Cancel();

        for (var i = 0; i < 3; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(state.ExecutionTime);
            Assert.Equal(0, state.ExecutionCount);
        }
    }

    [Fact]
    public async Task Should_Support_Enabling_After_Starting()
    {
        using var cts = new CancellationTokenSource();

        var state = new JobState(TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(5_000),
            TimeSpan.FromMilliseconds(1_000));
        var serviceProvider = await BeginTestAsync(state, Boolean.FalseString, cts.Token);

        var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();
        memoryConfigurationProvider.Set($"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Enabled)}", Boolean.TrueString);
        configuration.Reload();

        // Call `Start` again to refresh the state's internal start time.
        state.Start();

        while (state.Elapsed < state.InitialDelay)
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(state.GetEstimatedExecutionCount(), state.ExecutionCount);
            await Task.Delay(state.ExecutionTime);
        }

        for (var i = 0; i < 3; i++)
        {
            // Now it should be equal to `i` as it should have executed
            Assert.Equal(state.GetEstimatedExecutionCount(), state.ExecutionCount);
            await Task.Delay(state.ExecutionTime);
        }

        await EndTestAsync(state, serviceProvider, cts.Token);
        cts.Cancel();

        var finalEstimate = state.GetEstimatedExecutionCount();

        for (var i = 0; i < 3; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(state.ExecutionTime);
            Assert.Equal(finalEstimate, state.ExecutionCount);
        }
    }

    private static async Task<IServiceProvider> BeginTestAsync(JobState state, string enabled, CancellationToken cancellationToken = default)
    {
        var configurationBuilder = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Enabled)}", enabled },
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.Period)}", state.Period.ToString("c") },
            { $"periodicWorkers:{nameof(PeriodicWorkerConfiguration.InitialDelay)}", state.InitialDelay.ToString("c") },
        });

        var configuration = configurationBuilder.Build();

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddSingleton(configuration)
            .AddSingleton<IConfiguration>(configuration)
            .AddOptions<PeriodicWorkerConfiguration>().BindConfiguration("periodicWorkers");

        serviceCollection
            .AddSingleton<ILoggerFactory, NullLoggerFactory>()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(state)
            .AddPeriodicBackgroundJobs()
            .WithPeriodicJob<ShortJob>();

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

    private static async Task EndTestAsync(JobState state, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(cancellationToken);
        }
        state.Stop();
    }

    public class JobState
    {
        public int ExecutionCount { get; private set; }
        public TimeSpan Duration { get; init; }
        public TimeSpan InitialDelay { get; init; }
        public TimeSpan Period { get; init; }
        public TimeSpan ExecutionTime { get; internal set; }

        private DateTime? _start;
        private DateTime? _stop;
        private readonly TimeSpan _contextSwitchingTime = TimeSpan.FromMilliseconds(31);

        public JobState(TimeSpan duration, TimeSpan initialDelay, TimeSpan period)
        {
            Duration = duration;
            InitialDelay = initialDelay;
            Period = period;

            // The timer will always fire at it's specified period, regardless of the job duration.
            // https://github.com/dotnet/runtime/issues/95238#issuecomment-1826758659
            ExecutionTime = Period + _contextSwitchingTime;
        }

        public void Start() => _start = DateTime.UtcNow;
        public void Stop() => _stop = DateTime.UtcNow;

        public TimeSpan Elapsed => _start.HasValue ? DateTime.UtcNow - _start.Value : TimeSpan.Zero;

        public async Task IncrementAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Duration, cancellationToken);
            ExecutionCount++;
        }

        public int GetEstimatedExecutionCount()
        {
            var end = _stop ?? DateTime.UtcNow;
            var start = _start ?? DateTime.UtcNow;

            // If the job takes longer than the period, it will be executed again immediately.
            // We still need to wait for the job to finish the first execution before we can start the second.
            var expectedExecutionTime = _contextSwitchingTime + (Duration > Period
                ? Duration
                : Period);

            // Context switching between threads can cause the job to take longer than the "expected" time.
            var runningTime = end - start - InitialDelay - Duration - _contextSwitchingTime;
            if (runningTime <= TimeSpan.Zero)
            {
                return 0;
            }

            return (int)Math.Ceiling(runningTime / expectedExecutionTime);
        }
    }

    public class ShortJob : IJob
    {
        private readonly JobState _jobState;

        public ShortJob(JobState jobState) => _jobState = jobState;

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _jobState.IncrementAsync(cancellationToken);
        }
    }
}
