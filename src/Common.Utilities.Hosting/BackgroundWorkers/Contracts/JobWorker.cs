using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Contracts;

internal abstract class JobWorker<T> : BackgroundService
    where T : IJob
{
    private readonly IScopeFactory _scopeFactory;

    protected JobWorker(IScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected static string GetNamespacedName(Type type) => $"{type.Namespace}.{type.Name}";

    protected async Task ExecuteJobAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope(GetNamespacedName(typeof(T)));
        var job = scope.ServiceProvider.GetRequiredService<T>();
        await job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        _scopeFactory.DisposeScope(scope);
    }
}
