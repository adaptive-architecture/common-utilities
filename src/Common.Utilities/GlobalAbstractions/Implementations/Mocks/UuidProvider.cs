using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

/// <summary>
/// An mock implementation of <see cref="IUuidProvider"/> based on <see cref="Guid"/> that uses the "D" pattern when serializing to string.
/// </summary>
public class UuidMockProvider : MockProvider<Guid>, IUuidProvider
{
    /// <inheritdoc />
    public UuidMockProvider(Guid[] items) : base(items)
    {
    }

    /// <inheritdoc />
    public string New() => GetNextValue().ToString("D");
}
