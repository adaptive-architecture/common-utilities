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
internal class PeriodicJobWorker<T> : RepeatingJobWorker<T>
    where T : IJob
{
    private PeriodicTimer? _timer;
    private readonly ILogger<PeriodicJobWorker<T>> _logger;
    private readonly object _lock = new();

    public PeriodicJobWorker(IScopeFactory scopeFactory, ILogger<PeriodicJobWorker<T>> logger,
        IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider)
        : base(scopeFactory, options)
    {
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromDays(1), timeProvider);
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _timer!.Dispose();
            _timer = null;
        }
        return base.StopAsync(cancellationToken);
    }

    protected override async Task RepeatJobAsync(CancellationToken stoppingToken)
    {
        var isInitialCall = true;
        SetTimerPeriod(Configuration.InitialDelay);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var continueRunning = await _timer!.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                if (!continueRunning)
                {
                    break;
                }

                if (isInitialCall)
                {
                    // Reset the period to the configured value before the initial call.
                    // This is to avoid having another execution immediately after the initial one.
                    isInitialCall = false;
                    _timer.Period = Configuration.Interval;
                }

                await ExecuteJobAsync(stoppingToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job {JobName} failed.", GetNamespacedName(typeof(T)));
            }
        }
    }

    protected override void HandleConfigurationChange()
    {
        SetTimerPeriod(Configuration.Interval);
    }

    private void SetTimerPeriod(TimeSpan period)
    {
        lock (_lock)
        {
            if (_timer == null || _timer.Period == period)
            {
                return;
            }
            _timer.Period = period;
        }
    }
}
