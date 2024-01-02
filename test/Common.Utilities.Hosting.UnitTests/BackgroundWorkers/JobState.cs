namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public enum JobType
{
    Periodic = 1,
    Perpetual = 2,
}

public static class JobTypeExtensions
{
    public static JobType ToJobType(this int jobType) => jobType switch
    {
        (int)JobType.Periodic => JobType.Periodic,
        (int)JobType.Perpetual => JobType.Perpetual,
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
            JobType.Perpetual => GetCountForPerpetual(elapsed),
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
    private double GetCountForPerpetual(TimeSpan elapsed)
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
}
