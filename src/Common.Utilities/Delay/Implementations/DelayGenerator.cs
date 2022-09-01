using AdaptArch.Common.Utilities.Delay.Contracts;

namespace AdaptArch.Common.Utilities.Delay.Implementations;

/// <inheritdoc />
public class DelayGenerator: IDelayGenerator
{
    private readonly DelayGeneratorOptions _options;
    private int _currentIteration;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    public DelayGenerator(DelayGeneratorOptions options)
    {
        _options = options;
        _currentIteration = _options.Current;
    }

    /// <inheritdoc />
    public IEnumerable<TimeSpan> GetDelays()
    {
        while (_currentIteration < _options.MaxIterations)
        {
            var delay = GetCurrentDelay(_currentIteration, _options.DelayType, _options.DelayInterval);
            var jitter = _options.JitterGenerator
                .New(delay, _options.JitterLowerBoundary, _options.JitterUpperBoundary);
            _currentIteration++;
            yield return delay + jitter;
        }
    }

    private static TimeSpan GetCurrentDelay(int iteration, DelayType delayType, TimeSpan delayInterval)
    {
        // ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (delayType)
        {
            case DelayType.Constant:
                return delayInterval;
            case DelayType.Linear:
                return iteration * delayInterval;
            case DelayType.PowerOf2:
                return Math.Pow(iteration, 2) * delayInterval;
            case DelayType.PowerOfE:
                return Math.Pow(iteration, Math.E) * delayInterval;
            default:
                throw new ArgumentOutOfRangeException(nameof(delayType), $"The \"{delayType}\" delay type is not known.");
        }
        // ReSharper restore SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    }
}
