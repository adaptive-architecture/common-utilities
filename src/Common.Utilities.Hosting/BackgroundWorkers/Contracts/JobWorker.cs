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

internal abstract partial class JobWorker<T> : BackgroundService
    where T : IJob
{
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
    protected readonly string JobName;

    [LoggerMessage(Level = LogLevel.Debug, Message = "{JobName} execution for was cancelled.")]
    private partial void LogExecutionCancelled(Exception ex, string jobName);

    [LoggerMessage(Level = LogLevel.Error, Message = "{JobName} execution failed.")]
    private partial void LogExecutionFailed(Exception ex, string jobName);

    protected JobWorker(IScopeFactory scopeFactory, IOptionsMonitor<RepeatingWorkerConfiguration> options, TimeProvider timeProvider, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;

        var type = typeof(T);
        JobName = $"{type.Namespace}.{type.Name}";
        Configuration = _options.CurrentValue.GetConfiguration(JobName);
        _ = _options.OnChange(_ => HandleConfigurationChange(false));
        Logger = logger;
        _timeProvider = timeProvider;
        _timer = new PeriodicTimer(BackgroundServiceGlobals.OneDay, timeProvider);
        _nextExecutionTime = _timeProvider.GetUtcNow().DateTime;
        _configurationChangeLock = new SemaphoreSlim(1);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _configurationChangeLock.WaitAsync(cancellationToken)
            .ConfigureAwait(BackgroundServiceGlobals.ConfigureAwaitOptions);
        _stopRequested = true;
        _ = _configurationChangeLock.Release();

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
                LogExecutionCancelled(ex, JobName);
            }
            catch (Exception ex)
            {
                LogExecutionFailed(ex, JobName);
            }
            finally
            {
                DisposeConfigurationCts();
            }
        }
    }

    protected async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(JobName);
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
        _ = _configurationChangeLock.Wait(TimeSpan.FromSeconds(5));
        try
        {
            SetTimerPeriodCore(useInitialDelay);
        }
        finally
        {
            _ = _configurationChangeLock.Release();
        }
    }

    protected virtual Task BeforeJobExecution() => Task.CompletedTask;
    protected virtual Task AfterJobExecution() => Task.CompletedTask;

    private void SetTimerPeriodCore(bool useInitialDelay)
    {
        TimeSpan period;
        if (Configuration.Enabled)
        {
            period = useInitialDelay ? Configuration.InitialDelay : Configuration.Interval;
        }
        else
        {
            period = BackgroundServiceGlobals.OneDay;
        }

        if (_timer.Period != period)
        {
            _timer.Period = period;
            _nextExecutionTime = _timeProvider.GetUtcNow().DateTime.Add(period);
        }
    }

    private void HandleConfigurationChange(bool useInitialDelay)
    {
        _ = _configurationChangeLock.Wait(TimeSpan.FromSeconds(5));
        try
        {
            if (_stopRequested)
            {
                return;
            }

            Configuration = _options.CurrentValue.GetConfiguration(JobName);
            SetTimerPeriodCore(useInitialDelay);
            _configurationChangeTokenSource?.Cancel();
        }
        finally
        {
            _ = _configurationChangeLock.Release();
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
            _ = _configurationChangeLock.Release();
        }
    }

    private void DisposeConfigurationCts()
    {
        _ = _configurationChangeLock.Wait(TimeSpan.FromSeconds(1));
        try
        {
            if (_configurationChangeTokenSource!.IsCancellationRequested)
            {
                _configurationChangeTokenSource.Dispose();
            }
        }
        finally
        {
            _ = _configurationChangeLock.Release();
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
}
