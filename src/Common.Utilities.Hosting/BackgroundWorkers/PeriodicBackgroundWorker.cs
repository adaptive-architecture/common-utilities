using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers;

/// <summary>
/// A background workers that run periodically.
/// </summary>
public class PeriodicBackgroundWorker<T> : BackgroundService
    where T : IJob
{
    private readonly IScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<PeriodicWorkerConfiguration> _options;
    private readonly TimeProvider _timeProvider;
    private readonly PeriodicTimer _timer;

    private readonly ILogger<PeriodicBackgroundWorker<T>> _logger;

    /// <summary>
    /// Constructs a new instance of <see cref="PeriodicBackgroundWorker{T}"/>.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The options monitor</param>
    /// <param name="timeProvider">The time provider.</param>
    public PeriodicBackgroundWorker(IScopeFactory scopeFactory, ILogger<PeriodicBackgroundWorker<T>> logger,
        IOptionsMonitor<PeriodicWorkerConfiguration> options, TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
        _timeProvider = timeProvider;

        _timer = new PeriodicTimer(GetConfiguration().Period, _timeProvider);
        _options.OnChange(_ => UpdateTimerPeriod());
    }

    /// <inheritdoc/>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        UpdateTimerPeriod();
        return base.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        return base.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialDelay = GetConfiguration().InitialDelay;
        if (initialDelay > TimeSpan.Zero)
        {
            await Task.Delay(initialDelay, stoppingToken).ConfigureAwait(false);
        }

        var timerWasStopped = false;
        while (!stoppingToken.IsCancellationRequested && !timerWasStopped)
        {
            try
            {
                await ExecuteJobAsync(stoppingToken).ConfigureAwait(false);

                var nextTickWasCompleted = false;
                do
                {
                    // Filter out the immediate completion of the wait call on the timer.
                    // https://github.com/dotnet/runtime/issues/95238#issuecomment-1826758659
                    var nextTick = _timer.WaitForNextTickAsync(stoppingToken);
                    nextTickWasCompleted = nextTick.IsCompleted;

                    var @continue = await nextTick.ConfigureAwait(false);
                    if (!@continue)
                    {
                        timerWasStopped = true;
                    }

                } while (nextTickWasCompleted && !timerWasStopped);
            }
            catch (OperationCanceledException oEx)
            {
                _logger.LogInformation(oEx, "Background job {JobName} cancelled.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job {JobName} failed.", typeof(T).Name);
            }
        }
    }

    /// <summary>
    /// Gets the configuration for the current background worker.
    /// </summary>
    protected PeriodicWorkerConfiguration GetConfiguration()
    {
        return _options.CurrentValue.GetConfiguration(GetNamespacedName(typeof(T)));
    }

    private static string GetNamespacedName(Type type) => $"{type.Namespace}.{type.Name}";

    private void UpdateTimerPeriod()
    {
        var configuration = GetConfiguration();
        _timer.Period = configuration.Enabled ? GetConfiguration().Period : TimeSpan.MaxValue;
    }

    private async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(GetNamespacedName(typeof(T)));
        var job = scope.ServiceProvider.GetRequiredService<T>();
        await job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        _scopeFactory.DisposeScope(scope);
    }
}
