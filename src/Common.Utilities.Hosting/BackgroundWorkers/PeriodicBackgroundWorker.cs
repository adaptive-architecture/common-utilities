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
    private PeriodicWorkerConfiguration _configuration;
    private CancellationTokenSource? _cancellationTokenSource;

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

        _configuration = GetConfiguration();
        _timer = new PeriodicTimer(TimeSpan.FromDays(1), _timeProvider);
        _options.OnChange(_ => HandleConfigurationChange());
    }

    /// <inheritdoc/>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        HandleConfigurationChange();
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
        while (!stoppingToken.IsCancellationRequested)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            await RepeatJobAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Gets the configuration for the current background worker.
    /// </summary>
    protected PeriodicWorkerConfiguration GetConfiguration()
    {
        return _options.CurrentValue.GetConfiguration(GetNamespacedName(typeof(T)));
    }

    private async Task RepeatJobAsync(CancellationToken stoppingToken)
    {
        var isInitialCall = true;
        _timer.Period = _configuration.InitialDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var continueRunning = await _timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
                if (!continueRunning)
                {
                    break;
                }

                if (_configuration.Enabled)
                {
                    await ExecuteJobAsync(stoppingToken).ConfigureAwait(false);
                }

                if (isInitialCall)
                {
                    // Reset the period to the configured value after the initial call.
                    isInitialCall = false;
                    _timer.Period = _configuration.Period;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job {JobName} failed.", GetNamespacedName(typeof(T)));
            }
        }
    }

    private static string GetNamespacedName(Type type) => $"{type.Namespace}.{type.Name}";

    private void HandleConfigurationChange()
    {
        _configuration = GetConfiguration();
        _timer.Period = _configuration.Period;
        _cancellationTokenSource?.Cancel();
    }

    private async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(GetNamespacedName(typeof(T)));
        var job = scope.ServiceProvider.GetRequiredService<T>();
        await job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        _scopeFactory.DisposeScope(scope);
    }
}
