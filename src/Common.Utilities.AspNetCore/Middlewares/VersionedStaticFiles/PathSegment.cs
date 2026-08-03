using System.Text.RegularExpressions;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Validation for the path segments (target directory, version) used to build
/// filesystem paths and rewritten request paths.
/// </summary>
internal static partial class PathSegment
{
    [GeneratedRegex("^[A-Za-z0-9._+-]{1,64}$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Determines whether <paramref name="value"/> is safe to use as a single path segment.
    /// Rejects anything that could traverse ("."/"..", separators) or re-root the path.
    /// </summary>
    public static bool IsValid(string? value)
        => value is not (null or "." or "..") && Pattern().IsMatch(value);
}
