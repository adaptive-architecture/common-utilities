using AdaptArch.Common.Utilities.Jobs.Contracts;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class TestJob : IJob
{
    private readonly JobState _jobState;

    public TestJob(JobState jobState) => _jobState = jobState;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return _jobState.IncrementAsync(cancellationToken);
    }
}
