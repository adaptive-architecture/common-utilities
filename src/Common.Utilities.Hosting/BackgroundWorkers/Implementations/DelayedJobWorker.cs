using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;

/// <summary>
/// A background workers that run with a consistent delay.
/// </summary>
internal class DelayedJobWorker<T> : JobWorker<T>
    where T : IJob
{
    public DelayedJobWorker(IScopeFactory scopeFactory, ILogger<DelayedJobWorker<T>> logger,
        IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider)
        : base(scopeFactory, options, timeProvider, logger)
    {
    }

    protected override Task AfterJobExecution()
    {
        // Set the period again to make sure we wait for the delay before executing the job again
        SetTimerPeriod(false);
        return base.AfterJobExecution();
    }
}
