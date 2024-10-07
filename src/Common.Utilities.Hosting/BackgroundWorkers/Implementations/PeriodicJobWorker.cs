using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;

/// <summary>
/// A background workers that run periodically.
/// </summary>
internal class PeriodicJobWorker<T> : JobWorker<T>
    where T : IJob
{
    public PeriodicJobWorker(IScopeFactory scopeFactory, ILogger<PeriodicJobWorker<T>> logger,
        IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider)
        : base(scopeFactory, options, timeProvider, logger)
    {
    }
}
