using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class RepeatingJobWorkerSpecs
{
    private const int TestTimeout = 2 * 60 * 1000;

    private static Action<ServiceCollection> GetServiceCollectionAction(JobType jobType) => jobType switch
    {
        JobType.Periodic => svc => svc.AddBackgroundJobs().WithPeriodicJob<TestJob>(),
        JobType.Delayed => svc => svc.AddBackgroundJobs().WithDelayedJob<TestJob>(),
        _ => throw new ArgumentOutOfRangeException(nameof(jobType))
    };

    private static Action<ServiceCollection> GetServiceCollectionAction_Failing(JobType jobType) => jobType switch
    {
        JobType.Periodic => svc => svc.AddBackgroundJobs().WithPeriodicJob<FailingJob>().WithPeriodicJob<CancelledJob>(),
        JobType.Delayed => svc => svc.AddBackgroundJobs().WithDelayedJob<FailingJob>().WithPeriodicJob<CancelledJob>(),
        _ => throw new ArgumentOutOfRangeException(nameof(jobType))
    };

    private static void SetEnabledState(IServiceProvider serviceProvider, bool enabled)
    {
        var configuration = serviceProvider.GetRequiredService<IConfigurationRoot>();
        var memoryConfigurationProvider = configuration.Providers.OfType<MemoryConfigurationProvider>().Single();
        memoryConfigurationProvider.Set($"periodicWorkers:{nameof(RepeatingWorkerConfiguration.Enabled)}", enabled ? Boolean.TrueString : Boolean.FalseString);
        configuration.Reload();
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic, 100, 600, 200)]
    [InlineData((int)JobType.Delayed, 100, 600, 200)]
    public async Task Should_NotExecute_The_Job_While_Initially_Delayed(int jobTypeId, int jobDurationMs, int initialDelayMs, int intervalMs)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.New(jobDurationMs, initialDelayMs, intervalMs);
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), TestContext.Current.CancellationToken);

        await state.Assert_NoExecution_WhileInitialDelay(jobType);
        await ServiceBuilder.EndTestAsync(state, serviceProvider, TestContext.Current.CancellationToken);
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic, 1_000, 15_000, 2_000)]
    [InlineData((int)JobType.Delayed, 1_000, 15_000, 2_000)]
    // Jobs where the duration is higher than the repeat period.
    [InlineData((int)JobType.Periodic, 3_000, 15_000, 2_000)]
    [InlineData((int)JobType.Delayed, 3_000, 15_000, 2_000)]
    public async Task Should_Execute_The_Job(int jobTypeId, int jobDurationMs, int initialDelayMs, int intervalMs)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.New(jobDurationMs, initialDelayMs, intervalMs);
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), TestContext.Current.CancellationToken);

        await state.Assert_Iterations_While_Running(jobType);
        await ServiceBuilder.EndTestAsync(state, serviceProvider, TestContext.Current.CancellationToken);
        await state.Assert_No_FurtherIterations_After_Stopped();
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Support_Disabling_After_Starting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.WithShortDurations();
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), TestContext.Current.CancellationToken);

        await state.WaitForExecutionAsync();
        Assert.True(state.ExecutionCount > 0);

        SetEnabledState(serviceProvider, false);
        state.Start();

        await state.WaitForExecutionAsync(3);
        Assert.Equal(0, state.ExecutionCount, 1f);
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Support_Enabling_After_Starting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.WithShortDurations();
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.FalseString, GetServiceCollectionAction(jobType), TestContext.Current.CancellationToken);

        await state.WaitForExecutionAsync();
        SetEnabledState(serviceProvider, true);
        state.Start();

        await state.WaitForExecutionAsync();
        Assert.True(state.ExecutionCount > 0);
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Not_Fail_If_Job_Throws_Errors(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.WithShortDurations();
        _ = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction_Failing(jobType), TestContext.Current.CancellationToken);
        await state.WaitForExecutionAsync();
        Assert.True(state.ExecutionCount > 0);
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Not_Fail_If_Configuration_Reloads_After_Stopping(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.WithShortDurations();
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction_Failing(jobType), TestContext.Current.CancellationToken);
        await ServiceBuilder.EndTestAsync(state, serviceProvider, TestContext.Current.CancellationToken);

        try
        {
            SetEnabledState(serviceProvider, false);
        }
        catch (Exception)
        {
            Assert.Fail("Reloading configuration should not throw an exception.");
        }
    }

    [Theory(Timeout = TestTimeout)]
    [InlineData((int)JobType.Periodic)]
    [InlineData((int)JobType.Delayed)]
    public async Task Should_Not_Execute_If_Stopped_While_Waiting(int jobTypeId)
    {
        var jobType = jobTypeId.ToJobType();
        var state = JobState.WithLongDurations();
        var serviceProvider = await ServiceBuilder.BeginTestAsync(state, Boolean.TrueString, GetServiceCollectionAction(jobType), TestContext.Current.CancellationToken);

        await state.Assert_NoExecution_WhileInitialDelay(jobType);
        await ServiceBuilder.EndTestAsync(state, serviceProvider, TestContext.Current.CancellationToken);
        await state.Assert_No_FurtherIterations_After_Stopped();
    }
}
