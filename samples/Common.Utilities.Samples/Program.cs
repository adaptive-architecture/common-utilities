using System.Collections.Concurrent;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;
using AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Samples;
using AdaptArch.Common.Utilities.Jobs.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOptions<RepeatingWorkerConfiguration>()
    .BindConfiguration("jobWorkers");

builder.Services
    .AddSingleton<WorkersState>()
    .AddBackgroundJobs()
    // This will cause the service to generate 2 random numbers per minute since it takes 15 seconds to generate a number
    // and there is a 15 second delay between each execution.
    .WithDelayedJob<RandomNumberGeneratorJob>()

    // This will cause the service to generate 4 random numbers per minute since it takes 15 seconds to generate a number
    // and the wait period will have already elapsed by the time the finishes.
    //.WithPeriodicJob<RandomNumberGeneratorJob>()

    .WithPeriodicJob<ReporterJob>();

var host = builder.Build();
host.Run();
Console.WriteLine("Application started!");

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Samples
{
    internal class WorkersState
    {
        public ConcurrentBag<int> Numbers { get; set; } = [];
    }

    internal class ReporterJob : IJob
    {
        private readonly WorkersState _state;
        private readonly TimeProvider _timeProvider;

        public ReporterJob(WorkersState state, TimeProvider timeProvider)
        {
            _state = state;
            _timeProvider = timeProvider;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"[{_timeProvider.GetUtcNow():O}] Current lucky numbers are: {String.Join(", ", _state.Numbers)}");
            _state.Numbers.Clear();
        }
    }

    internal class RandomNumberGeneratorJob : IJob
    {
        private readonly WorkersState _state;

        public RandomNumberGeneratorJob(WorkersState state) => _state = state;

        private static readonly Random s_random = new();
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Simulate some work
            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);

            _state.Numbers.Add(s_random.Next());
        }
    }
}
