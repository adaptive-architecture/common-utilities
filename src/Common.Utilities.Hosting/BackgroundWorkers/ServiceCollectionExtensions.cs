using System.Diagnostics.CodeAnalysis;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Implementations;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Keep this in the "Microsoft.Extensions.Configuration" for easy access.
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.ServiceCollection;

public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Add a periodic background service.
    /// </summary>
    public interface IPeriodicBackgroundServiceBuilder
    {
        /// <summary>
        /// The service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }

    class PeriodicBackgroundServiceBuilder : IPeriodicBackgroundServiceBuilder
    {
        public PeriodicBackgroundServiceBuilder(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        public IServiceCollection Services { get; }
    }

    /// <summary>
    /// Add a periodic background service.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static IPeriodicBackgroundServiceBuilder AddPeriodicBackgroundJobs(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.TryAddSingleton<IScopeFactory, ScopeFactory>();
        return new PeriodicBackgroundServiceBuilder(serviceCollection);
    }

    /// <summary>
    /// Add a periodic background job.
    /// The job will be ran periodically and will re-execute immediately in case the job duration is longer than the repeat period.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="builder">The instance of <see cref="IPeriodicBackgroundServiceBuilder"/>.</param>
    public static IPeriodicBackgroundServiceBuilder WithPeriodicJob<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJob>(this IPeriodicBackgroundServiceBuilder builder)
        where TJob : class, IJob
    {
        builder.Services.TryAddScoped<TJob>();
        builder.Services.AddHostedService<PeriodicJobWorker<TJob>>();
        return builder;
    }
}
