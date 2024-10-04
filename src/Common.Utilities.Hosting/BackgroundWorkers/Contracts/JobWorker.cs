using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;

internal abstract class JobWorker<T> : BackgroundService
    where T : IJob
{
    private readonly IScopeFactory _scopeFactory;
    protected ILogger Logger;

    protected JobWorker(IScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        Logger = logger;
    }

    protected static string GetNamespacedName(Type type) => $"{type.Namespace}.{type.Name}";

    protected async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(GetNamespacedName(typeof(T)));
        var job = scope.ServiceProvider.GetRequiredService<T>();
        try
        {
            await job.ExecuteAsync(cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogDebug(ex, "Job {JobName} was cancelled.", GetNamespacedName(typeof(T)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Job {JobName} failed.", GetNamespacedName(typeof(T)));
        }
        _scopeFactory.DisposeScope(scope);
    }
}
