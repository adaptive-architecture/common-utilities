namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public enum JobType
{
    Periodic = 1,
    Delayed = 2,
}

public static class JobTypeExtensions
{
    public static JobType ToJobType(this int jobType) => jobType switch
    {
        (int)JobType.Periodic => JobType.Periodic,
        (int)JobType.Delayed => JobType.Delayed,
        _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Invalid job type.")
    };
}

public class JobState
{
    public int ExecutionCount { get; private set; }
    public TimeSpan JobDuration { get; init; }
    public TimeSpan InitialDelay { get; init; }
    public TimeSpan Interval { get; init; }
    public TimeSpan ExecutionTime { get; internal set; }

    private DateTime? _start;
    private DateTime? _stop;
    private readonly TimeSpan _contextSwitchingTime = TimeSpan.FromMilliseconds(37);

    public JobState(TimeSpan jobDuration, TimeSpan initialDelay, TimeSpan period)
    {
        JobDuration = jobDuration;
        InitialDelay = initialDelay;
        Interval = period;
        // Context switching between threads can cause the job to take longer than the "expected" time.
        ExecutionTime = Interval + _contextSwitchingTime;
    }

    public void Start()
    {
        _start = DateTime.UtcNow;
        ExecutionCount = 0;
    }

    public void Stop() => _stop = DateTime.UtcNow;

    public TimeSpan Elapsed => _start.HasValue ? DateTime.UtcNow - _start.Value : TimeSpan.Zero;

    public async Task IncrementAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(JobDuration, cancellationToken);
        ExecutionCount++;
    }

    public double GetEstimatedExecutionCount(JobType jobType)
    {
        var end = _stop ?? DateTime.UtcNow;
        var start = _start ?? DateTime.UtcNow;
        var elapsed = end - start;
        if (elapsed < InitialDelay)
        {
            return 0;
        }

        return jobType switch
        {
            JobType.Periodic => GetCountForPeriodic(elapsed),
            JobType.Delayed => GetCountForDelayed(elapsed),
            _ => -1,
        };
    }

    private double GetCountForPeriodic(TimeSpan elapsed)
    {
        var executionTimeAfterFirst = elapsed - InitialDelay - JobDuration;
        if (executionTimeAfterFirst < TimeSpan.Zero)
        {
            return 0;
        }

        var expectedIterationTime = JobDuration > Interval
            ? JobDuration
            : Interval;

        var executionsAfterFirst = executionTimeAfterFirst / expectedIterationTime;
        return 1 + executionsAfterFirst;
    }
    private double GetCountForDelayed(TimeSpan elapsed)
    {
        var executionTimeAfterFirst = elapsed - InitialDelay - JobDuration - _contextSwitchingTime;
        if (executionTimeAfterFirst < TimeSpan.Zero)
        {
            return 0;
        }

        var expectedIterationTime = JobDuration + Interval - _contextSwitchingTime;
        var executionsAfterFirst = executionTimeAfterFirst / expectedIterationTime;
        return 1 + executionsAfterFirst;
    }

    public async Task Assert_NoExecution_WhileInitialDelay(JobType jobType, double? delay = null, double tolerance = .75)
    {
        var delayToUse = delay.HasValue ? TimeSpan.FromMilliseconds(delay.Value) : InitialDelay;
        using var cts = new CancellationTokenSource(delayToUse);
        // While the delay is not over, the job should not have executed.
        while (Elapsed < InitialDelay)
        {
            Assert.Equal(GetEstimatedExecutionCount(jobType), ExecutionCount, tolerance);
            try
            {
                await Task.Delay(ExecutionTime, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task Assert_Iterations_While_Running(JobType jobType, int iterationsToCheck = 3, double tolerance = .75)
    {
        using var cts = new CancellationTokenSource(iterationsToCheck * ExecutionTime);
        for (var i = 0; i < iterationsToCheck; i++)
        {
            // Now it should be equal to `i` as it should have executed
            Assert.Equal(GetEstimatedExecutionCount(jobType), ExecutionCount, tolerance);
            try
            {
                await Task.Delay(ExecutionTime, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task Assert_No_FurtherIterations_After_Stopped(double finalEstimate, int iterationsToCheck = 3, double tolerance = .75)
    {
        for (var i = 0; i < iterationsToCheck; i++)
        {
            // Now it should not advance anymore as the job has been stopped.
            await Task.Delay(ExecutionTime);
            Assert.Equal(finalEstimate, ExecutionCount, tolerance);
        }
    }

    public async Task WaitForExecutionAsync(int minExecutionCount = 1, CancellationToken cancellationToken = default)
    {
        var maxWait = ExecutionTime * (3 + minExecutionCount);
        using var cts = new CancellationTokenSource(maxWait);
        while (Elapsed < maxWait)
        {
            if (ExecutionCount >= minExecutionCount)
            {
                break;
            }
            try
            {
                await Task.Delay(ExecutionTime, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public static JobState New(int jobDurationMs, int initialDelayMs, int intervalMs) => new(
        TimeSpan.FromMilliseconds(jobDurationMs), TimeSpan.FromMilliseconds(initialDelayMs),
        TimeSpan.FromMilliseconds(intervalMs));

    public static JobState WithShortDurations() => New(100, 500, 500);
    public static JobState WithLongDurations() => New(1, 3_000, 1_000);
}
