using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;

/// <summary>
/// Middleware that rewrites extensionless request paths to an existing file with a configured extension.
/// </summary>
/// <remarks>
/// The middleware probes the configured file provider for <c>path + extension</c> candidates.
/// When a candidate exists, the request path is rewritten (or the request is redirected) and the
/// next middleware, normally the static files middleware, serves the file. The middleware never
/// opens files itself.
/// </remarks>
public partial class ExtensionlessStaticFilesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExtensionlessStaticFilesOptions _options;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<ExtensionlessStaticFilesMiddleware> _logger;
    private readonly PathString _requestPath;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rewriting extensionless request path '{OriginalPath}' to '{NewPath}'.")]
    private partial void LogRewritingPath(string originalPath, string newPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Redirecting extensionless request path '{OriginalPath}' to '{NewPath}' with status code {StatusCode}.")]
    private partial void LogRedirectingPath(string originalPath, string newPath, int statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The exact file '{Path}' exists; skipping extension probing.")]
    private partial void LogExactFileFound(string path);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionlessStaticFilesMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The middleware options.</param>
    /// <param name="environment">The hosting environment used to resolve the default file provider.</param>
    /// <param name="logger">The logger instance.</param>
    public ExtensionlessStaticFilesMiddleware(
        RequestDelegate next,
        ExtensionlessStaticFilesOptions options,
        IWebHostEnvironment environment,
        ILogger<ExtensionlessStaticFilesMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(logger);

        ValidateOptions(options);

        _next = next;
        _options = options;
        _logger = logger;
        _fileProvider = options.FileProvider ?? environment.WebRootFileProvider;
        ArgumentNullException.ThrowIfNull(_fileProvider);
        _requestPath = NormalizeRequestPath(options.RequestPath);
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task Invoke(HttpContext context)
    {
        if (!ShouldProcess(context) || !_options.ShouldApply(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalPath = context.Request.Path;
        if (!originalPath.StartsWithSegments(_requestPath, out var subPath))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var subPathValue = subPath.Value ?? String.Empty;
        if (subPathValue.Length == 0
            || subPathValue.EndsWith('/')
            || Path.HasExtension(subPathValue))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // IFileProvider subpaths are relative, so drop the leading slash that PathString keeps.
        var relativePath = subPathValue.TrimStart('/');

        // Do not shadow a file that already exists at the exact request path, including a file
        // without an extension. Such a file is left to the static files middleware and its
        // content type provider.
        var exactFile = _fileProvider.GetFileInfo(relativePath);
        if (exactFile.Exists && !exactFile.IsDirectory)
        {
            LogExactFileFound(relativePath);
            await _next(context).ConfigureAwait(false);
            return;
        }

        foreach (var extension in _options.Extensions)
        {
            var candidateSubPath = String.Concat(relativePath, extension);
            var candidateFile = _fileProvider.GetFileInfo(candidateSubPath);
            if (!candidateFile.Exists || candidateFile.IsDirectory)
            {
                continue;
            }

            var candidatePath = new PathString(String.Concat(_requestPath.Value, "/", candidateSubPath));
            var redirectContext = new ExtensionlessRedirectContext
            {
                HttpContext = context,
                OriginalPath = originalPath,
                CandidatePath = candidatePath
            };

            if (_options.ShouldRedirect(redirectContext))
            {
                LogRedirectingPath(
                    originalPath.Value ?? String.Empty,
                    candidatePath.Value ?? String.Empty,
                    _options.RedirectStatusCode);

                context.Response.StatusCode = _options.RedirectStatusCode;
                context.Response.Headers.Location = UriHelper.BuildAbsolute(
                    context.Request.Scheme,
                    context.Request.Host,
                    context.Request.PathBase,
                    candidatePath,
                    context.Request.QueryString);
                return;
            }

            LogRewritingPath(originalPath.Value ?? String.Empty, candidatePath.Value ?? String.Empty);
            context.Request.Path = candidatePath;
            break;
        }

        await _next(context).ConfigureAwait(false);
    }

    private static bool ShouldProcess(HttpContext context) =>
        HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);

    private static void ValidateOptions(ExtensionlessStaticFilesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Extensions);
        ArgumentNullException.ThrowIfNull(options.ShouldApply);
        ArgumentNullException.ThrowIfNull(options.ShouldRedirect);

        if (options.RedirectStatusCode is < StatusCodes.Status300MultipleChoices or >= StatusCodes.Status400BadRequest)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.RedirectStatusCode),
                options.RedirectStatusCode,
                "The redirect status code must be in the 3xx range.");
        }

        foreach (var extension in options.Extensions)
        {
            if (String.IsNullOrWhiteSpace(extension)
                || extension[0] != '.'
                || extension.Contains('/')
                || extension.Contains('\\'))
            {
                throw new ArgumentException(
                    "Extensions must start with a '.' and must not contain path separators.",
                    nameof(options.Extensions));
            }
        }
    }

    private static PathString NormalizeRequestPath(PathString requestPath)
    {
        var value = requestPath.Value;
        if (String.IsNullOrEmpty(value) || String.Equals(value, "/", StringComparison.Ordinal))
        {
            return PathString.Empty;
        }

        return new PathString(value.TrimEnd('/'));
    }
}
