using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;

/// <summary>
/// A background workers that run perpetually.
/// </summary>
internal class PerpetualJobWorker<T> : RepeatingJobWorker<T>
    where T : IJob
{
    private readonly ILogger<PerpetualJobWorker<T>> _logger;

    public PerpetualJobWorker(IScopeFactory scopeFactory, ILogger<PerpetualJobWorker<T>> logger,
        IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider)
        : base(scopeFactory, options)
    {
        _logger = logger;
    }

    protected override void HandleConfigurationChange()
    {
        // Nothing to do in case the configuration changes.
    }

    protected override async Task RepeatJobAsync(CancellationToken stoppingToken)
    {
        var isInitialCall = true;
        var delayPeriod = Configuration.InitialDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delayPeriod, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);

                await ExecuteJobAsync(stoppingToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);

                if (isInitialCall)
                {
                    // Reset the period to the configured value after the initial call.
                    isInitialCall = false;
                    delayPeriod = Configuration.Interval;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobName} failed.", GetNamespacedName(typeof(T)));
            }
        }
    }
}
