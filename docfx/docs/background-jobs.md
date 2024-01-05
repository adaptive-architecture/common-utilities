# Background jobs

Sometimes in your application you might need to execute jobs in the background to achieve certain goals.

## Usage

For the purpose of this sample we will assume we need to run 2 jobs:
- One job to execute continuously and perform some heavy computations, like generating some hashes. We will simulate this by waiting for some time and then returning a random number.
- One job to periodically report the results of the heavy computations. We will simulate this by printing to the console the generated numbers.


To use the background jobs feature we will define two classes that implement he `IJob` interface.

``` csharp

    // We are using this class to share state between jobs.
    public class WorkersState
    {
        public ConcurrentBag<int> Numbers { get; set; } = [];
    }

    public class HeavyComputationsJob : IJob
    {
        private readonly WorkersState _state;

        public HeavyComputationsJob(WorkersState state) => _state = state;

        private static readonly Random s_random = new();
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Simulate some work
            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);

            _state.Numbers.Add(s_random.Next());
        }
    }

    public class ReporterJob : IJob
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

```

The configuration for the application would look like:

``` json

{
  "jobWorkers": {
    "enabled": true,
    "interval": "00:01:00",
    // Allow the application to start before doing anything.
    "initialDelay": "00:00:30",
    "overrides": [
      // We want the computation job to run more frequently but also give the application some time to "breath"
      // so we will override the interval it is running on.
      {
        "pattern": "HeavyComputationsJob",
        "interval": "00:00:15"
      }
    ]
  }
}

```

The sample application would look like the the following

``` csharp

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOptions<RepeatingWorkerConfiguration>()
    .BindConfiguration("jobWorkers");

builder.Services
    // Add the state object
    .AddSingleton<WorkersState>()

    // Add the necessary dependencies and expose the interface that will allow registering the jobs
    .AddBackgroundJobs()

    // Register the HeavyComputationsJob job.
    // This will cause the service to generate 2 random numbers per minute since it takes 15 seconds to generate a number
    // and there is a 15 second delay between each execution.
    .WithDelayedJob<HeavyComputationsJob>()

    // This will cause the service to generate 4 random numbers per minute since it takes 15 seconds to generate a number
    // and the wait period will have already elapsed by the time the finishes.
    //.WithPeriodicJob<HeavyComputationsJob>()

    // Register the reporter job
    .WithPeriodicJob<ReporterJob>();

var host = builder.Build();
host.Run();
Console.WriteLine("Application started!");

```

## Considerations

The `WithDelayedJob` call will register a worker that will execute the job and then await for the delay interval to pass before executing again.

The `WithPeriodicJob` call will register a worker that will execute the job every `Interval` period of time.
If the job takes longer than the actual interval, the worker perform the next execution of the job as soon as the previous execution completes.
