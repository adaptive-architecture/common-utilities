using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;

/// <summary>
/// Context passed to <see cref="ExtensionlessStaticFilesOptions.ShouldRedirect"/>.
/// </summary>
public sealed class ExtensionlessRedirectContext
{
    /// <summary>
    /// Gets the current HTTP context.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets the request path as seen by the extensionless middleware, after any earlier middleware
    /// (for example the versioned static files middleware) rewrote it.
    /// </summary>
    public required PathString OriginalPath { get; init; }

    /// <summary>
    /// Gets the existing file path that was resolved, for example <c>/static/app/index.html</c>.
    /// </summary>
    public required PathString CandidatePath { get; init; }
}
