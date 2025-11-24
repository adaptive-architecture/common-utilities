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
public partial class VersionedStaticFilesMiddleware
{
    const string HttpPathDelimiter = "/";
    private readonly RequestDelegate _next;
    private readonly MiddlewareOptions _options;
    private readonly ILogger<VersionedStaticFilesMiddleware> _logger;
    private readonly IStaticAssetsProvider _provider;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing static file request: {RequestPath}")]
    private partial void LogProcessingRequest(string requestPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Extracted target directory: {TargetDirectory}")]
    private partial void LogExtractedDirectory(string targetDirectory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found version cookie for directory '{TargetDirectory}': Version={Version}, DateModified={DateModified}")]
    private partial void LogFoundVersionCookie(string targetDirectory, string version, DateTime dateModified);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No valid version cookie found for directory '{TargetDirectory}'")]
    private partial void LogNoVersionCookie(string targetDirectory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Version resolution strategy - UseConfiguredVersion: {UseConfiguredVersion}, CacheDuration: {CacheDuration}")]
    private partial void LogVersionResolutionStrategy(bool useConfiguredVersion, TimeSpan cacheDuration);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using configured version - clearing cookie version for directory '{TargetDirectory}'")]
    private partial void LogUsingConfiguredVersion(string targetDirectory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ensuring target directory exists: {TargetDirectory}")]
    private partial void LogEnsuringTargetDirectory(string targetDirectory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Read configured version from file - Directory: {TargetDirectory}, Version: {Version}, Timestamp: {Timestamp}")]
    private partial void LogConfiguredVersionFromFile(string targetDirectory, string version, DateTime timestamp);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No configured version file found for directory: {TargetDirectory}")]
    private partial void LogNoConfiguredVersionFile(string targetDirectory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Using configured version {Version} for directory '{TargetDirectory}'")]
    private partial void LogUsingVersion(string version, string targetDirectory);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Newer version available: {NewerVersionExists} - Current: {CurrentVersion}@{CurrentDate}, Configured: {ConfiguredVersion}@{ConfiguredDate}")]
    private partial void LogNewerVersionCheck(bool newerVersionExists, string currentVersion, DateTime currentDate, string? configuredVersion, DateTime? configuredDate);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ensuring version subdirectory exists - Directory: {TargetDirectory}, Version: {Version}")]
    private partial void LogEnsuringVersionDirectory(string targetDirectory, string version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rewriting request path - Original: {OriginalPath}, New: {NewPath}, Version: {Version}")]
    private partial void LogRewritingPath(string originalPath, string newPath, string version);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No version info available for directory '{TargetDirectory}' - skipping path rewrite")]
    private partial void LogSkippingPathRewrite(string targetDirectory);

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
        LogProcessingRequest(currentPath);

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
            LogSkippingPathRewrite(targetDirectory);
        }
    }

    private (string targetDirectory, int startIndex, int endIndex) ExtractTargetDirectory(string currentPath)
    {
        var staticPrefixLength = _options.StaticFilesPathPrefix.Length;
        var targetDirectoryStartIndex = staticPrefixLength;
        var targetDirectoryEndIndex = currentPath.IndexOf('/', staticPrefixLength);
        var targetDirectory = currentPath[targetDirectoryStartIndex..targetDirectoryEndIndex];

        LogExtractedDirectory(targetDirectory);

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
            LogFoundVersionCookie(targetDirectory, versionInfo.Version, versionInfo.DateModified);
        }
        else
        {
            LogNoVersionCookie(targetDirectory);
        }

        return versionInfo;
    }

    private async Task<(VersionCookiePayload? resolvedVersion, VersionFilePayload? configuredVersion)> ResolveVersionAsync(
        HttpContext context, string targetDirectory, VersionCookiePayload? versionInfo, MiddlewareOptions.MiddlewareContext middlewareContext)
    {
        var useConfiguredVersion = (versionInfo == null) || _options.UseConfiguredVersion(middlewareContext);
        var cacheDuration = _options.UseCacheDuration(middlewareContext);

        LogVersionResolutionStrategy(useConfiguredVersion, cacheDuration);

        if (useConfiguredVersion)
        {
            LogUsingConfiguredVersion(targetDirectory);
            versionInfo = null;
        }

        LogEnsuringTargetDirectory(targetDirectory);
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
            LogConfiguredVersionFromFile(targetDirectory, configuredVersion.Version, configuredVersion.Timestamp);
        }
        else
        {
            LogNoConfiguredVersionFile(targetDirectory);
        }
    }

    private VersionCookiePayload? CreateVersionInfo(string targetDirectory, VersionCookiePayload? versionInfo,
        VersionFilePayload? configuredVersion)
    {
        if (versionInfo == null && configuredVersion != null)
        {
            LogUsingVersion(configuredVersion.Version, targetDirectory);

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

        LogNewerVersionCheck(
            newerVersionExists,
            versionInfo.Version,
            versionInfo.DateModified,
            configuredVersion?.Version,
            configuredVersion?.Timestamp);

        LogEnsuringVersionDirectory(middlewareContext.Directory, versionInfo.Version);

        await _provider.EnsureVersionDirectoryExistsAsync(middlewareContext.Directory, versionInfo.Version, middlewareContext.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        middlewareContext.HttpContext.Response.OnStarting(() => SetResponseCookies(middlewareContext, versionInfo, newerVersionExists, cacheDuration));

        var newPath = middlewareContext.RequestPath[..targetDirectoryStartIndex]
            + middlewareContext.Directory
            + HttpPathDelimiter
            + versionInfo.Version
            + middlewareContext.RequestPath[targetDirectoryEndIndex..];

        LogRewritingPath(middlewareContext.RequestPath, newPath, versionInfo.Version);

        middlewareContext.HttpContext.Request.Path = newPath;
    }

    private Task SetResponseCookies(MiddlewareOptions.MiddlewareContext middlewareContext, VersionCookiePayload versionInfo, bool newerVersionExists, TimeSpan cacheDuration)
    {
#pragma warning disable S2092, S3330 // The following cookies are not security sensitive.
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
#pragma warning disable S2092, S3330

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

