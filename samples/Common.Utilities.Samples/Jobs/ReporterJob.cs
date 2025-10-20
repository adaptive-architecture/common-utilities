using AdaptArch.Common.Utilities.Jobs.Contracts;

namespace AdaptArch.Common.Utilities.Samples.Jobs;

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
