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
    private readonly SemaphoreSlim _lock = new(1);
    private readonly TimeSpan _lockTimeout = TimeSpan.FromMilliseconds(10);

    public PeriodicJobWorker(IScopeFactory scopeFactory, ILogger<PeriodicJobWorker<T>> logger,
        IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider)
        : base(scopeFactory, options, logger)
    {
        _timer = new PeriodicTimer(TimeSpan.FromHours(24), timeProvider);
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _lock.WaitAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.SuppressThrowing);

            _timer!.Dispose();
            _timer = null;
        }
        finally
        {
            _lock.Release();
        }

        await base.StopAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
    }

    protected override async Task RepeatJobAsync(CancellationToken stoppingToken)
    {
        var isInitialCall = true;
        SetTimerPeriod(Configuration.InitialDelay);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var continueRunning = await _timer!.WaitForNextTickAsync(stoppingToken)
                    .AsTask()
                    .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
                if (!continueRunning)
                {
                    return;
                }

                if (isInitialCall)
                {
                    // Reset the period to the configured value before the initial call.
                    // This is to avoid having another execution immediately after the initial one.
                    isInitialCall = false;
                    SetTimerPeriod(Configuration.Interval);
                }

                await ExecuteJobAsync(stoppingToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Job {JobName} failed.", GetNamespacedName(typeof(T)));
            }
        }
    }

    protected override void HandleConfigurationChange()
    {
        SetTimerPeriod(Configuration.Interval);
    }

    private void SetTimerPeriod(TimeSpan period)
    {
        try
        {
            _lock.Wait(_lockTimeout);
            if (_timer != null && _timer.Period != period)
            {
                _timer.Period = period;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
