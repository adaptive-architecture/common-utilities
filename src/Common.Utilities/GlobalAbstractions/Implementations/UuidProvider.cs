using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

/// <summary>
/// An implementation of <see cref="IUuidProvider"/> based on <see cref="Guid"/> that uses the "D" pattern when serializing to string.
/// </summary>
public class DashedUuidProvider : IUuidProvider
{
    /// <inheritdoc />
    public string New() => Guid.NewGuid().ToString("D");
}

/// <summary>
/// An implementation of <see cref="IUuidProvider"/> based on <see cref="Guid"/> that uses the "N" pattern when serializing to string.
/// </summary>
public class UnDashedUuidProvider : IUuidProvider
{
    /// <inheritdoc />
    public string New() => Guid.NewGuid().ToString("N");
}
