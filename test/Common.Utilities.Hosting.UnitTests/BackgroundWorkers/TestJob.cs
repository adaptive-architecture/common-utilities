﻿using AdaptArch.Common.Utilities.Hosting.UnitTests.BackgroundWorkers;
using AdaptArch.Common.Utilities.Jobs.Contracts;

namespace Common.Utilities.Hosting.UnitTests.BackgroundWorkers;

public class TestJob : IJob
{
    private readonly JobState _jobState;

    public TestJob(JobState jobState) => _jobState = jobState;

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return _jobState.IncrementAsync(cancellationToken);
    }
}
