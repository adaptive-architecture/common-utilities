﻿using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Hosting.Internals;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Options;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;

/// <summary>
/// A job worker that runs the job repeatedly.
/// </summary>
internal abstract class RepeatingJobWorker<T> : JobWorker<T>
    where T : IJob
{
    private readonly IOptionsMonitor<RepeatingWorkerConfiguration> _options;
    private CancellationTokenSource? _configurationChangeTokenSource;
    protected RepeatingWorkerConfiguration Configuration { get; private set; }

    protected RepeatingJobWorker(IScopeFactory scopeFactory, IOptionsMonitor<RepeatingWorkerConfiguration> options)
        : base(scopeFactory)
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _configurationChangeTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            while (!Configuration.Enabled)
            {
                try
                {
                    await Task.Delay(BackgroundServiceGlobals.CheckEnabledPollingInterval, _configurationChangeTokenSource.Token)
                        .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
                }
                catch (TaskCanceledException)
                {
                    // This is expected when the configuration changes.
                }
            }
            await RepeatJobAsync(_configurationChangeTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            _configurationChangeTokenSource.Dispose();
        }
    }
    protected abstract Task RepeatJobAsync(CancellationToken stoppingToken);
    protected abstract void HandleConfigurationChange();

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
