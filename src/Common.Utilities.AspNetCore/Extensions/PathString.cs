using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Extensions;

/// <summary>
/// Extension methods for <see cref="PathString"/>.
/// </summary>
public static class PathStringExtensions
{
    /// <summary>
    /// Gets the value of the <see cref="PathString"/>, or an empty string if it's null.
    /// </summary>
    /// <param name="path">The <see cref="PathString"/>.</param>
    public static string ValueOrEmptyString(this PathString path)
    {
        return path.Value ?? String.Empty;
    }
}
