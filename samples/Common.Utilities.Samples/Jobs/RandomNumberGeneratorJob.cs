using AdaptArch.Common.Utilities.Jobs.Contracts;

namespace AdaptArch.Common.Utilities.Samples.Jobs;

internal class RandomNumberGeneratorJob : IJob
{
    private readonly WorkersState _state;
    private static readonly Random s_random = new();

    public RandomNumberGeneratorJob(WorkersState state) => _state = state;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Simulate some work
        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);

        _state.Numbers.Add(s_random.Next());
    }
}
