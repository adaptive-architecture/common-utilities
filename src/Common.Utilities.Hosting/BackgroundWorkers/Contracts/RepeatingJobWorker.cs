using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Hosting.Internals;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;

/// <summary>
/// A job worker that runs the job repeatedly.
/// </summary>
internal abstract class RepeatingJobWorker<T> : JobWorker<T>
    where T : IJob
{
    private readonly IOptionsMonitor<RepeatingWorkerConfiguration> _options;
    private readonly SemaphoreSlim _lock = new(1);
    private CancellationTokenSource? _configurationChangeTokenSource;

    protected RepeatingWorkerConfiguration Configuration { get; private set; }

    protected RepeatingJobWorker(IScopeFactory scopeFactory, IOptionsMonitor<RepeatingWorkerConfiguration> options, ILogger logger)
        : base(scopeFactory, logger)
    {
        _options = options;

        Configuration = GetConfiguration();
        _options.OnChange(_ => OnConfigurationChange());
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        OnConfigurationChange();
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeConfigurationChangeTokenSource(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
        await base.StopAsync(cancellationToken)
            .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var token = await GetNewChangeToken(stoppingToken).ConfigureAwait(false);
            while (!Configuration.Enabled)
            {
                try
                {
                    await Task.Delay(BackgroundServiceGlobals.CheckEnabledPollingInterval, token)
                        .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
                }
                catch (OperationCanceledException cancellationException)
                {
                    Logger.LogDebug(cancellationException, "Configuration change detected for job {JobName}.", GetNamespacedName(typeof(T)));
                }
            }
            await RepeatJobAsync(token).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            await DisposeConfigurationChangeTokenSource(stoppingToken)
                .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
        }
    }
    protected abstract Task RepeatJobAsync(CancellationToken stoppingToken);
    protected abstract void HandleConfigurationChange();

    private async ValueTask<CancellationToken> GetNewChangeToken(CancellationToken cancellationToken)
    {
        ExceptionExtensions.ThrowNotSupportedIfNotNull(_configurationChangeTokenSource, "A configuration change token source already exists.");
        try
        {
            await _lock.WaitAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.SuppressThrowing);

            _configurationChangeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return _configurationChangeTokenSource.Token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DisposeConfigurationChangeTokenSource(CancellationToken cancellationToken)
    {
        try
        {
            await _lock.WaitAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.SuppressThrowing);

            if (_configurationChangeTokenSource != null)
            {
                _configurationChangeTokenSource.Cancel();
                _configurationChangeTokenSource.Dispose();
                _configurationChangeTokenSource = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private RepeatingWorkerConfiguration GetConfiguration()
    {
        return _options.CurrentValue.GetConfiguration(GetNamespacedName(typeof(T)));
    }

    private void OnConfigurationChange()
    {
        Configuration = GetConfiguration();
        HandleConfigurationChange();
        _configurationChangeTokenSource?.Cancel();
    }
}
