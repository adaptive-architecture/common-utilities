using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

/// <summary>
/// An implementation of <see cref="IDateTimeProvider"/> based on <see cref="DateTime"/>.
/// </summary>
public class DateTimeProvider: IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
