using System.Runtime.CompilerServices;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;
using AdaptArch.Common.Utilities.Hosting.Internals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class RepeatingJobWorkerSpecs
{
    const int IterationsToCheck = 2;
    const double Tolerance = .75;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(2);
    private static readonly Action<ServiceCollection> AddPeriodicJob = svc => svc
        .AddBackgroundJobs().WithPeriodicJob<TestJob>();

    private static readonly Action<ServiceCollection> AddPeriodicFailingJob = svc => svc
        .AddBackgroundJobs().WithPeriodicJob<FailingJob>();

    private static readonly Action<ServiceCollection> AddDelayedJob = svc => svc
        .AddBackgroundJobs().WithDelayedJob<TestJob>();

    private static readonly Action<ServiceCollection> AddDelayedFailingJob = svc => svc
        .AddBackgroundJobs().WithDelayedJob<FailingJob>();

    private static Action<ServiceCollection> GetServiceCollectionAction(JobType jobType) => jobType switch
    {
        JobType.Periodic => AddPeriodicJob,
        JobType.Delayed => AddDelayedJob,
        _ => throw new ArgumentOutOfRangeException(nameof(jobType))
    };

    private static Action<ServiceCollection> GetServiceCollectionAction_Failing(JobType jobType) => jobType switch
    {
        JobType.Periodic => AddPeriodicFailingJob,
        JobType.Delayed => AddDelayedFailingJob,
        _ => throw new ArgumentOutOfRangeException(nameof(jobType))
    };

    [Theory]
    [InlineData(1, 1_000, 10_000, 2_000)]
    [InlineData(2, 1_000, 10_000, 2_000)]
    [InlineData(1, 3_000, 10_000, 2_000)]
    [InlineData(2, 3_000, 10_000, 2_000)]
    public async Task Should_Execute_The_Job(int jobTypeId, int jobDurationMs, int initialDelayMs, int intervalMs)
    {
        var jobType = jobTypeId.ToJobType();
        using var cts = new CancellationTokenSource(TestTimeout);

        var state = new JobState(TimeSpan.FromMilliseconds(jobDurationMs),
            TimeSpan.FromMilliseconds(initialDelayMs),
            TimeSpan.FromMilliseconds(intervalMs));
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, AddPeriodicJob, cts.Token);

        while (state.Elapsed <= state.InitialDelay)
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(state.GetEstimatedExecutionCount(jobType), state.ExecutionCount, Tolerance);
            await Task.Delay(state.ExecutionTime);
        }

        for (var i = 0; i < IterationsToCheck; i++)
        {
            // Now it should be equal to `i` as it should have executed
            Assert.Equal(state.GetEstimatedExecutionCount(jobType), state.ExecutionCount, Tolerance);
            await Task.Delay(state.ExecutionTime);
        }

        await ServiceBuilder.EndTestAsync(state, serviceProvider, cts.Token);

        var finalEstimate = state.GetEstimatedExecutionCount(jobType);

        for (var i = 0; i < IterationsToCheck; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(state.ExecutionTime);
            Assert.Equal(finalEstimate, state.ExecutionCount, Tolerance);
        }
    }

    [Theory]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Support_Disabling_After_Starting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        using var cts = new CancellationTokenSource(TestTimeout);

        var state = new JobState(TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), cts.Token);

        var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();
        memoryConfigurationProvider.Set($"periodicWorkers:{nameof(RepeatingWorkerConfiguration.Enabled)}", Boolean.FalseString);

        while (state.Elapsed < TimeSpan.FromMilliseconds(2_000))
        {
            if (state.ExecutionCount > 0)
            {
                break;
            }
            await Task.Delay(state.ExecutionTime);
        }

        Assert.True(state.ExecutionCount > 0);

        configuration.Reload();
        state.Start();

        while (state.Elapsed < TimeSpan.FromMilliseconds(3_000))
        {
            Assert.Equal(0, state.ExecutionCount, Tolerance);
            await Task.Delay(state.ExecutionTime);
        }
        Assert.Equal(0, state.ExecutionCount, Tolerance);
    }

    [Theory]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Support_Enabling_After_Starting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        using var cts = new CancellationTokenSource(TestTimeout);

        BackgroundServiceGlobals.CheckEnabledPollingInterval = TimeSpan.FromMilliseconds(10);

        var state = new JobState(TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.FalseString, GetServiceCollectionAction(jobType), cts.Token);

        while (state.Elapsed < TimeSpan.FromMilliseconds(2_000))
        {
            Assert.Equal(0, state.ExecutionCount, Tolerance);
            await Task.Delay(state.ExecutionTime);
        }

        var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();
        memoryConfigurationProvider.Set($"periodicWorkers:{nameof(RepeatingWorkerConfiguration.Enabled)}", Boolean.TrueString);
        configuration.Reload();
        state.Start();

        while (state.Elapsed < TimeSpan.FromMilliseconds(2_000))
        {
            if (state.ExecutionCount > 0)
            {
                break;
            }
            await Task.Delay(state.ExecutionTime);
        }

        Assert.True(state.ExecutionCount > 0);
    }

    [Theory]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Not_Fail_If_Job_Throws_Errors(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        using var cts = new CancellationTokenSource(TestTimeout);

        BackgroundServiceGlobals.CheckEnabledPollingInterval = TimeSpan.FromMilliseconds(10);

        var state = new JobState(TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction_Failing(jobType), cts.Token);

        while (state.Elapsed < TimeSpan.FromMilliseconds(2_000))
        {
            if (state.ExecutionCount > 0)
            {
                break;
            }
            await Task.Delay(state.ExecutionTime);
        }

        Assert.True(state.ExecutionCount > 0);
    }

    [Theory]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Not_Execute_If_Stopped_While_Waiting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        using var cts = new CancellationTokenSource(TestTimeout);

        var state = new JobState(TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(3_000),
            TimeSpan.FromMilliseconds(3_000));
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), cts.Token);

        while (state.Elapsed <= state.InitialDelay / 2)
        {
            // While the delay is not over, the job should not have executed.
            Assert.Equal(state.GetEstimatedExecutionCount(jobType), state.ExecutionCount, Tolerance);
            await Task.Delay(state.ExecutionTime);
        }

        await ServiceBuilder.EndTestAsync(state, serviceProvider, cts.Token);

        var finalEstimate = state.GetEstimatedExecutionCount(jobType);

        for (var i = 0; i < IterationsToCheck; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(state.ExecutionTime);
            Assert.Equal(finalEstimate, state.ExecutionCount, Tolerance);
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "RepeatJobAsync")]
    extern static Task PeriodicJobWorkerRepeatJobAsync(PeriodicJobWorker<TestJob> @this, CancellationToken stoppingToken);
}
