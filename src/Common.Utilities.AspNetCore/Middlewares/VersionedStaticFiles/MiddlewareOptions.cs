using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Options for the VersionedStaticFilesMiddleware.
/// </summary>
public class MiddlewareOptions
{
    /// <summary>
    /// The name of the cookie that contains the version information.
    /// </summary>
    public string VersionCookieNamePrefix { get; set; } = ".aa_sfv_";

    /// <summary>
    /// The path prefix for versioned static files.
    /// </summary>
    public string StaticFilesPathPrefix { get; set; } = "/static/";

    /// <summary>
    /// The directory from which to serve static files.
    /// </summary>
    public string StaticFilesDirectory { get; set; } = "wwwroot/static";

    /// <summary>
    /// A function to determine whether to use a configured version instead of the version from the cookie.
    /// If it returns true, the middleware will use a configured version (e.g., the version defined in a `version.json` file in the static files directory).
    /// If it returns false, the middleware will use the version from the cookie if present.
    /// </summary>
    public Func<MiddlewareContext, bool> UseConfiguredVersion { get; set; } = (_) => false;

    /// <summary>
    /// A function to determine the cache duration for the static files.
    /// The function receives the current middleware context and returns a TimeSpan representing the cache duration.
    /// Default is no caching (TimeSpan.Zero).
    /// If a positive non-zero duration is returned, the middleware will set appropriate cache headers on the response.
    /// </summary>
    public Func<MiddlewareContext, TimeSpan> UseCacheDuration { get; set; } = (_) => TimeSpan.Zero;

    /// <summary>
    /// The cookie expiration duration.
    /// Default is 1 day.
    /// </summary>
    public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// The context for the middleware.
    /// </summary>
    public class MiddlewareContext
    {
        /// <summary>
        /// The current HttpContext.
        /// </summary>
        public HttpContext HttpContext { get; init; } = null!;
        /// <summary>
        /// The request path.
        /// </summary>
        public string RequestPath { get; init; } = String.Empty;
        /// <summary>
        /// The target directory within the static files directory.
        /// </summary>
        public string Directory { get; init; } = String.Empty;
    }
}
