namespace AdaptArch.Common.Utilities.Postgres.LeaderElection;

/// <summary>
/// Validation for PostgreSQL identifiers that are interpolated into SQL statements.
/// </summary>
internal static class PostgresIdentifier
{
    // PostgreSQL truncates identifiers longer than 63 bytes.
    private const int MaxLength = 63;

    /// <summary>
    /// Determines whether <paramref name="name"/> is a plain PostgreSQL identifier:
    /// starts with a letter or underscore and contains only letters, digits or underscores.
    /// Anything else must be rejected because identifiers cannot be parameterized.
    /// </summary>
    public static bool IsValid(string? name)
    {
        if (String.IsNullOrWhiteSpace(name) || name.Length > MaxLength)
        {
            return false;
        }

        if (!Char.IsLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        return name.All(c => Char.IsLetterOrDigit(c) || c == '_');
    }
}
