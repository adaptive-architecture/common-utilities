using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers;

/// <summary>
/// A background workers that run periodically.
/// </summary>
public class PeriodicBackgroundWorker<T> : BackgroundService
    where T : IBackgroundJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<RootPeriodicWorkerConfiguration> _options;
    private readonly TimeProvider _timeProvider;
    private readonly PeriodicTimer _timer;

    /// <summary>
    /// The logger instance.
    /// </summary>
    protected readonly ILogger<PeriodicBackgroundWorker<T>> Logger;

    /// <summary>
    /// Constructs a new instance of <see cref="PeriodicBackgroundWorker{T}"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The options monitor</param>
    /// <param name="timeProvider">The time provider.</param>
    protected PeriodicBackgroundWorker(IServiceProvider serviceProvider, ILogger<PeriodicBackgroundWorker<T>> logger,
        IOptionsMonitor<RootPeriodicWorkerConfiguration> options, TimeProvider timeProvider)
    {
        _serviceProvider = serviceProvider;
        Logger = logger;
        _options = options;
        _timeProvider = timeProvider;

        _timer = new PeriodicTimer(TimeSpan.MaxValue, _timeProvider);
    }

    /// <inheritdoc/>
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _options.OnChange(_ => UpdateTimerPeriod());
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteJobAsync(stoppingToken);
                await _timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException oEx)
            {
                Logger.LogInformation(oEx, "Background job {JobName} cancelled.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Background job {JobName} failed.", typeof(T).Name);
            }
        }
    }

    /// <summary>
    /// Creates a new scope.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected virtual IServiceScope CreateScope(IServiceProvider serviceProvider)
    {
        return serviceProvider.CreateScope();
    }

    /// <summary>
    /// Disposes the scope.
    /// </summary>
    protected virtual void DisposeScope(IServiceScope scope)
    {
        scope.Dispose();
    }

    /// <summary>
    /// Gets the configuration for the current background worker.
    /// </summary>
    protected PeriodicWorkerConfiguration GetConfiguration()
    {
        var type = typeof(T);
        var assemblyQualifiedName = $"{type.Assembly.GetName().Name}.{type.FullName}";
        return _options.CurrentValue.GetConfiguration(assemblyQualifiedName);
    }

    private void UpdateTimerPeriod()
    {
        var configuration = GetConfiguration();
        _timer.Period = configuration.Enabled ? GetConfiguration().Period : TimeSpan.MaxValue;
    }

    private async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = CreateScope(_serviceProvider);
        var job = scope.ServiceProvider.GetRequiredService<T>();
        await job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        DisposeScope(scope);
    }
}
