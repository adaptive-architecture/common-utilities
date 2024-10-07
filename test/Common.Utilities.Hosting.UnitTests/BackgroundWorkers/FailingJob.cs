using AdaptArch.Common.Utilities.Jobs.Contracts;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class FailingJob : IJob
{
    private readonly JobState _jobState;

    public FailingJob(JobState jobState) => _jobState = jobState;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _jobState.IncrementAsync(cancellationToken);
        throw new Exception("Job failed.");
    }
}
