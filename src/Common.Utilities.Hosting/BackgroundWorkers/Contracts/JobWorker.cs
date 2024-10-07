using System.Runtime.CompilerServices;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Hosting.Internals;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;

internal abstract class JobWorker<T> : BackgroundService
    where T : IJob
{
    private static readonly TimeSpan s_oneDay = TimeSpan.FromDays(1);
    private readonly IScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<RepeatingWorkerConfiguration> _options;
    private readonly SemaphoreSlim _configurationChangeLock;
    private readonly TimeProvider _timeProvider;
    private readonly PeriodicTimer _timer;
    private DateTime _nextExecutionTime;
    private bool _stopRequested;
    private CancellationTokenSource? _configurationChangeTokenSource;
    protected readonly ILogger Logger;
    protected RepeatingWorkerConfiguration Configuration { get; private set; }

    protected JobWorker(IScopeFactory scopeFactory, IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;

        Configuration = GetConfiguration();
        _options.OnChange(_ => HandleConfigurationChange(false));
        Logger = logger;
        _timeProvider = timeProvider;
        _timer = new PeriodicTimer(s_oneDay, timeProvider);
        _nextExecutionTime = _timeProvider.GetUtcNow().DateTime;
        _configurationChangeLock = new SemaphoreSlim(1);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _configurationChangeLock.Wait(cancellationToken);
        _stopRequested = true;
        _configurationChangeLock.Release();

        await base.StopAsync(cancellationToken)
            .ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        HandleConfigurationChange(true);
        SetTimerPeriod(true);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                RefreshConfigurationCts(stoppingToken);
                var token = _configurationChangeTokenSource!.Token;
                await WaitForNextExecutionTime(token).ConfigureAwait(false);
                SetTimerPeriod(false);

                if (!_configurationChangeTokenSource.IsCancellationRequested)
                {
                    await BeforeJobExecution().ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
                    await ExecuteJobAsync(stoppingToken).ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
                    await AfterJobExecution().ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogDebug(ex, "{JobName} execution for was cancelled.", GetNamespacedName(typeof(T)));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{JobName} execution failed.", GetNamespacedName(typeof(T)));
            }
            finally
            {
                DisposeConfigurationCts();
            }
        }
    }

    protected async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(GetNamespacedName(typeof(T)));
        try
        {
            var job = scope.ServiceProvider.GetRequiredService<T>();
            await job.ExecuteAsync(cancellationToken)
                .ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
        }
        finally
        {
            _scopeFactory.DisposeScope(scope);
        }
    }

    protected void SetTimerPeriod(bool useInitialDelay)
    {
        _configurationChangeLock.Wait(TimeSpan.FromSeconds(5));
        try
        {
            SetTimerPeriodCore(useInitialDelay);
        }
        finally
        {
            _configurationChangeLock.Release();
        }
    }

    protected virtual Task BeforeJobExecution() => Task.CompletedTask;
    protected virtual Task AfterJobExecution() => Task.CompletedTask;

    private void SetTimerPeriodCore(bool useInitialDelay)
    {
        var period = Configuration.Enabled ?
                (useInitialDelay ? Configuration.InitialDelay : Configuration.Interval)
                : s_oneDay;

        if (_timer.Period != period)
        {
            _timer.Period = period;
            _nextExecutionTime = _timeProvider.GetUtcNow().DateTime.Add(period);
        }
    }

    private void HandleConfigurationChange(bool useInitialDelay)
    {
        _configurationChangeLock.Wait(TimeSpan.FromSeconds(5));
        try
        {
            if (_stopRequested)
            {
                return;
            }

            Configuration = GetConfiguration();
            SetTimerPeriodCore(useInitialDelay);
            _configurationChangeTokenSource?.Cancel();
        }
        finally
        {
            _configurationChangeLock.Release();
        }
    }

    private void RefreshConfigurationCts(CancellationToken stoppingToken)
    {
        _configurationChangeLock.Wait(stoppingToken);
        try
        {
            if (_configurationChangeTokenSource?.IsCancellationRequested ?? true)
            {
                _configurationChangeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            }
        }
        finally
        {
            _configurationChangeLock.Release();
        }
    }

    private void DisposeConfigurationCts()
    {
        _configurationChangeLock.Wait(TimeSpan.FromSeconds(1));
        try
        {
            if (_configurationChangeTokenSource!.IsCancellationRequested)
            {
                _configurationChangeTokenSource.Dispose();
            }
        }
        finally
        {
            _configurationChangeLock.Release();
        }
    }

    private async ValueTask WaitForNextExecutionTime(CancellationToken token)
    {
        DateTime now;
        do
        {
            try
            {
                _ = await _timer.WaitForNextTickAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogDebug(ex, "Waiting for next tick was cancelled.");
                return;
            }

            now = _timeProvider.GetUtcNow().DateTime;
        } while (_nextExecutionTime > now);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RepeatingWorkerConfiguration GetConfiguration() =>
        _options.CurrentValue.GetConfiguration(GetNamespacedName(typeof(T)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetNamespacedName(Type type) => $"{type.Namespace}.{type.Name}";
}
