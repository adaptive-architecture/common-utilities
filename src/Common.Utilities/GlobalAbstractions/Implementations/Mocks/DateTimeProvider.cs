using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

/// <summary>
/// An mock implementation of <see cref="IDateTimeProvider"/>.
/// </summary>
public class DateTimeMockProvider : MockProvider<DateTime>, IDateTimeProvider
{
    /// <inheritdoc />
    public DateTimeMockProvider(DateTime[] items) : base(items)
    {
    }

    /// <inheritdoc />
    public DateTime UtcNow => GetNextValue();
}
