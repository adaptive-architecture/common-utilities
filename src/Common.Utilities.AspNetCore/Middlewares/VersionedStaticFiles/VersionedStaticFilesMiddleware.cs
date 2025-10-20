using AdaptArch.Common.Utilities.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;


/// <summary>
/// Middleware for serving versioned static files based on a cookie.
/// When a request comes in for a static file, the middleware checks for a version cookie.
/// If the cookie is present, it modifies the request path to include the version before passing it to the next middleware.
/// If the cookie is absent or malformed, it serves the version based on a the version defined in the `version.json` file in the static files directory.
/// </summary>
public class VersionedStaticFilesMiddleware
{
    const string HttpPathDelimiter = "/";
    private readonly RequestDelegate _next;
    private readonly MiddlewareOptions _options;
    private readonly ILogger<VersionedStaticFilesMiddleware> _logger;
    private readonly IStaticAssetsProvider _provider;

    /// <summary>
    /// Constructor for the VersionedStaticFilesMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The middleware options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The static assets provider.</param>
    public VersionedStaticFilesMiddleware(RequestDelegate next, MiddlewareOptions options,
        ILogger<VersionedStaticFilesMiddleware> logger, IStaticAssetsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(provider);

        _next = next;
        _options = options;
        _logger = logger;
        _provider = provider;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    public async Task Invoke(HttpContext context)
    {
        var currentPath = context.Request.Path.ValueOrEmptyString();

        if (currentPath.StartsWith(_options.StaticFilesPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await ProcessStaticFileRequestAsync(context, currentPath).ConfigureAwait(false);
        }

        await _next(context);
    }

    private async Task ProcessStaticFileRequestAsync(HttpContext context, string currentPath)
    {
        _logger.LogDebug("Processing static file request: {RequestPath}", currentPath);

        var (targetDirectory, targetDirectoryStartIndex, targetDirectoryEndIndex) = ExtractTargetDirectory(currentPath);

        var versionInfo = ExtractVersionFromCookie(context, targetDirectory);

        var middlewareContext = new MiddlewareOptions.MiddlewareContext
        {
            HttpContext = context,
            Directory = targetDirectory,
            RequestPath = currentPath
        };

        var (resolvedVersion, configuredVersion) = await ResolveVersionAsync(context, targetDirectory, versionInfo, middlewareContext)
            .ConfigureAwait(false);

        if (resolvedVersion != null)
        {
            await ApplyVersionedPathAsync(targetDirectoryStartIndex, targetDirectoryEndIndex, resolvedVersion, configuredVersion, middlewareContext)
                .ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("No version info available for directory '{TargetDirectory}' - skipping path rewrite", targetDirectory);
        }
    }

    private (string targetDirectory, int startIndex, int endIndex) ExtractTargetDirectory(string currentPath)
    {
        var staticPrefixLength = _options.StaticFilesPathPrefix.Length;
        var targetDirectoryStartIndex = staticPrefixLength;
        var targetDirectoryEndIndex = currentPath.IndexOf('/', staticPrefixLength);
        var targetDirectory = currentPath[targetDirectoryStartIndex..targetDirectoryEndIndex];

        _logger.LogDebug("Extracted target directory: {TargetDirectory}", targetDirectory);

        return (targetDirectory, targetDirectoryStartIndex, targetDirectoryEndIndex);
    }

    private VersionCookiePayload? ExtractVersionFromCookie(HttpContext context, string targetDirectory)
    {
        var cookieName = GetVersionCookieName(targetDirectory);
        var versionInfo = VersionCookiePayload.TryParse(context.Request.Cookies[cookieName], out var version)
            ? version
            : null;

        if (versionInfo != null)
        {
            _logger.LogDebug("Found version cookie for directory '{TargetDirectory}': Version={Version}, DateModified={DateModified}",
                targetDirectory, versionInfo.Version, versionInfo.DateModified);
        }
        else
        {
            _logger.LogDebug("No valid version cookie found for directory '{TargetDirectory}'", targetDirectory);
        }

        return versionInfo;
    }

    private async Task<(VersionCookiePayload? resolvedVersion, VersionFilePayload? configuredVersion)> ResolveVersionAsync(
        HttpContext context, string targetDirectory, VersionCookiePayload? versionInfo, MiddlewareOptions.MiddlewareContext middlewareContext)
    {
        var useConfiguredVersion = (versionInfo == null) || _options.UseConfiguredVersion(middlewareContext);
        var cacheDuration = _options.UseCacheDuration(middlewareContext);

        _logger.LogDebug("Version resolution strategy - UseConfiguredVersion: {UseConfiguredVersion}, CacheDuration: {CacheDuration}",
            useConfiguredVersion, cacheDuration);

        if (useConfiguredVersion)
        {
            _logger.LogDebug("Using configured version - clearing cookie version for directory '{TargetDirectory}'", targetDirectory);
            versionInfo = null;
        }

        _logger.LogDebug("Ensuring target directory exists: {TargetDirectory}", targetDirectory);
        await _provider.EnsureDirectoryExistsAsync(targetDirectory, context.RequestAborted)
            .ConfigureAwait(false);

        var configuredVersion = await _provider.ReadVersionFileAsync(targetDirectory, context.RequestAborted);

        LogConfiguredVersionInfo(targetDirectory, configuredVersion);

        var resolvedVersion = CreateVersionInfo(targetDirectory, versionInfo, configuredVersion);

        return (resolvedVersion, configuredVersion);
    }

    private void LogConfiguredVersionInfo(string targetDirectory, VersionFilePayload? configuredVersion)
    {
        if (configuredVersion != null)
        {
            _logger.LogDebug("Read configured version from file - Directory: {TargetDirectory}, Version: {Version}, Timestamp: {Timestamp}",
                targetDirectory, configuredVersion.Version, configuredVersion.Timestamp);
        }
        else
        {
            _logger.LogDebug("No configured version file found for directory: {TargetDirectory}", targetDirectory);
        }
    }

    private VersionCookiePayload? CreateVersionInfo(string targetDirectory, VersionCookiePayload? versionInfo,
        VersionFilePayload? configuredVersion)
    {
        if (versionInfo == null && configuredVersion != null)
        {
            _logger.LogInformation("Using configured version {Version} for directory '{TargetDirectory}'",
                configuredVersion.Version, targetDirectory);

            return new VersionCookiePayload
            {
                Version = configuredVersion.Version,
                DateModified = configuredVersion.Timestamp
            };
        }

        return versionInfo;
    }

    private async Task ApplyVersionedPathAsync(int targetDirectoryStartIndex, int targetDirectoryEndIndex,
        VersionCookiePayload versionInfo, VersionFilePayload? configuredVersion,
        MiddlewareOptions.MiddlewareContext middlewareContext)
    {
        var newerVersionExists = (configuredVersion?.Timestamp ?? DateTime.MinValue) > versionInfo.DateModified;
        var cacheDuration = _options.UseCacheDuration(middlewareContext);

        _logger.LogDebug("Newer version available: {NewerVersionExists} - Current: {CurrentVersion}@{CurrentDate}, Configured: {ConfiguredVersion}@{ConfiguredDate}",
            newerVersionExists,
            versionInfo.Version,
            versionInfo.DateModified,
            configuredVersion?.Version,
            configuredVersion?.Timestamp);

        _logger.LogDebug("Ensuring version subdirectory exists - Directory: {TargetDirectory}, Version: {Version}",
            middlewareContext.Directory, versionInfo.Version);

        await _provider.EnsureVersionDirectoryExistsAsync(middlewareContext.Directory, versionInfo.Version, middlewareContext.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        middlewareContext.HttpContext.Response.OnStarting(() => SetResponseCookies(middlewareContext, versionInfo, newerVersionExists, cacheDuration));

        var newPath = middlewareContext.RequestPath[..targetDirectoryStartIndex]
            + middlewareContext.Directory
            + HttpPathDelimiter
            + versionInfo.Version
            + middlewareContext.RequestPath[targetDirectoryEndIndex..];

        _logger.LogInformation("Rewriting request path - Original: {OriginalPath}, New: {NewPath}, Version: {Version}",
            middlewareContext.RequestPath, newPath, versionInfo.Version);

        middlewareContext.HttpContext.Request.Path = newPath;
    }

    private Task SetResponseCookies(MiddlewareOptions.MiddlewareContext middlewareContext, VersionCookiePayload versionInfo, bool newerVersionExists, TimeSpan cacheDuration)
    {
        middlewareContext.HttpContext.Response.Cookies.Append(GetVersionCookieName(middlewareContext.Directory),
            versionInfo.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = middlewareContext.HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.Add(_options.CookieExpiration)
            });

        middlewareContext.HttpContext.Response.Cookies.Append(GetRefreshCookieName(middlewareContext.Directory),
            newerVersionExists ? "1" : "0",
            new CookieOptions
            {
                HttpOnly = false,
                Secure = middlewareContext.HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.Add(_options.CookieExpiration)
            });

        if (cacheDuration > TimeSpan.Zero)
        {
            middlewareContext.HttpContext.Response.Headers.CacheControl = $"public, max-age={cacheDuration.TotalSeconds}";
            middlewareContext.HttpContext.Response.Headers.Expires = DateTime.UtcNow.Add(cacheDuration).ToString("R");
            middlewareContext.HttpContext.Response.Headers.ETag = $"\"{middlewareContext.Directory}-{versionInfo.Version}\"";
        }

        return Task.CompletedTask;
    }


    private string GetVersionCookieName(string targetDirectory) => $"{_options.VersionCookieNamePrefix}_v_{targetDirectory}";
    private string GetRefreshCookieName(string targetDirectory) => $"{_options.VersionCookieNamePrefix}_r_{targetDirectory}";
}

