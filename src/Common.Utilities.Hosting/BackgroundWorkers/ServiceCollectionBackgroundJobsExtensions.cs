using System.Diagnostics.CodeAnalysis;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Implementations;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Implementations;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Keep this in the "Microsoft.Extensions.DependencyInjection" for easy access.
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding periodic jobs to the <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionBackgroundJobsExtensions
{
    /// <summary>
    /// Add a periodic background service.
    /// </summary>
    public interface IBackgroundJobServiceBuilder
    {
        /// <summary>
        /// The service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }

    class BackgroundJobServiceBuilder : IBackgroundJobServiceBuilder
    {
        public BackgroundJobServiceBuilder(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        public IServiceCollection Services { get; }
    }

    /// <summary>
    /// Add the dependencies for running background jobs.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    public static IBackgroundJobServiceBuilder AddBackgroundJobs(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddSingleton(TimeProvider.System);
        serviceCollection.TryAddSingleton<IScopeFactory, ScopeFactory>();
        return new BackgroundJobServiceBuilder(serviceCollection);
    }

    /// <summary>
    /// Add a periodic background job.
    /// The job will be ran periodically.
    /// It will execute at the specified interval, as long as the job duration is shorter than the interval.
    /// In case the job duration is longer than the interval, the job will be executed immediately.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="builder">The instance of <see cref="IBackgroundJobServiceBuilder"/>.</param>
    public static IBackgroundJobServiceBuilder WithPeriodicJob<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJob>(this IBackgroundJobServiceBuilder builder)
        where TJob : class, IJob
    {
        builder.Services.TryAddScoped<TJob>();
        _ = builder.Services.AddHostedService<PeriodicJobWorker<TJob>>();
        return builder;
    }

    /// <summary>
    /// Add a delayed background job.
    /// The job will be ran delayed and always wait for the specified interval before executing again.
    /// </summary>
    /// <typeparam name="TJob">The job type.</typeparam>
    /// <param name="builder">The instance of <see cref="IBackgroundJobServiceBuilder"/>.</param>
    public static IBackgroundJobServiceBuilder WithDelayedJob<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TJob>(this IBackgroundJobServiceBuilder builder)
        where TJob : class, IJob
    {
        builder.Services.TryAddScoped<TJob>();
        _ = builder.Services.AddHostedService<DelayedJobWorker<TJob>>();
        return builder;
    }
}
