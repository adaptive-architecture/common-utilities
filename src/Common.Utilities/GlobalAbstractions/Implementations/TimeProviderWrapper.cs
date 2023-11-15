using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

/// <summary>
/// An implementation of <see cref="IDateTimeProvider"/> based on <see cref="TimeProvider"/>.
/// </summary>
internal class TimeProviderWrapper : IDateTimeProvider
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="timeProvider">A reference to the desired <see cref="TimeProvider"/></param>
    public TimeProviderWrapper(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
}
