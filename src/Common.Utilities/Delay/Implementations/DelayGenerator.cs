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
            var delay = GetCurrentDelay(_currentIteration);
            var jitter = _options.JitterGenerator
                .New(delay, _options.JitterLowerBoundary, _options.JitterUpperBoundary);
            _currentIteration++;
            yield return delay + jitter;
        }
    }

    private TimeSpan GetCurrentDelay(int iteration)
    {
        // ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        // ReSharper disable once ConvertSwitchStatementToSwitchExpression
        switch (_options.DelayType)
        {
            case DelayType.Constant:
                return _options.DelayInterval;
            case DelayType.Linear:
                return iteration * _options.DelayInterval;
            case DelayType.PowerOf2:
                return Math.Pow(iteration, 2) * _options.DelayInterval;
            case DelayType.PowerOfE:
                return Math.Pow(iteration, Math.E) * _options.DelayInterval;
            default:
                throw new ArgumentOutOfRangeException(nameof(_options.DelayType), $"The \"{_options.DelayType}\" delay type is not known.");
        }
        // ReSharper restore SwitchStatementHandlesSomeKnownEnumValuesWithDefault
    }
}
