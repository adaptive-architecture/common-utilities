using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles;

/// <summary>
/// Options for the <see cref="ExtensionlessStaticFilesMiddleware"/>.
/// </summary>
/// <remarks>
/// The middleware only rewrites the request path. Serving the file, resolving the content
/// type, caching, conditional requests, range requests, and compression remain the
/// responsibility of the static files middleware that runs later in the pipeline.
/// </remarks>
public class ExtensionlessStaticFilesOptions
{
    /// <summary>
    /// Gets or sets the request path prefix that maps to the root of <see cref="FileProvider"/>.
    /// </summary>
    /// <remarks>
    /// This has the same meaning as <c>StaticFileOptions.RequestPath</c>: the prefix is removed
    /// from the request path before the file provider is queried. The default is
    /// <see cref="PathString.Empty"/>, which maps the whole request path to the provider root.
    /// A trailing slash is ignored.
    /// </remarks>
    public PathString RequestPath { get; set; } = PathString.Empty;

    /// <summary>
    /// Gets or sets the file provider used to check whether a candidate file exists.
    /// </summary>
    /// <remarks>
    /// When <see langword="null"/>, <see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.WebRootFileProvider"/>
    /// is used, which normally points at the <c>wwwroot</c> directory. If no web root is configured,
    /// the host supplies a <see cref="NullFileProvider"/> and the middleware becomes a no-op.
    /// </remarks>
    public IFileProvider? FileProvider { get; set; }

    /// <summary>
    /// Gets or sets the ordered list of extensions appended to an extensionless path.
    /// </summary>
    /// <remarks>
    /// Each entry must start with a dot, for example <c>.html</c>. The first candidate that exists
    /// in <see cref="FileProvider"/> wins. The default is <c>[".html"]</c>.
    /// </remarks>
    public IReadOnlyList<string> Extensions { get; set; } = [".html"];

    /// <summary>
    /// Gets or sets a predicate that determines whether the middleware should process a request.
    /// </summary>
    /// <remarks>
    /// Use this to scope extension probing to a path prefix, for example
    /// <c>context =&gt; context.Request.Path.StartsWithSegments("/static")</c>.
    /// Only GET and HEAD requests are considered. The default processes every GET and HEAD request.
    /// </remarks>
    public Func<HttpContext, bool> ShouldApply { get; set; } = _ => true;

    /// <summary>
    /// Gets or sets a predicate that determines whether a resolved candidate should be served
    /// through a redirect instead of an internal rewrite.
    /// </summary>
    /// <remarks>
    /// The predicate is called only after a candidate file has been found. When it returns
    /// <see langword="true"/>, the middleware responds with <see cref="RedirectStatusCode"/> and a
    /// <c>Location</c> header pointing at the candidate path, including the original query string.
    /// When it returns <see langword="false"/>, the request path is rewritten and the next
    /// middleware serves the file. The default never redirects.
    /// </remarks>
    public Func<ExtensionlessRedirectContext, bool> ShouldRedirect { get; set; } = _ => false;

    /// <summary>
    /// Gets or sets the status code used when <see cref="ShouldRedirect"/> returns <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The default is <c>301 Moved Permanently</c>. Use <c>302 Found</c> or <c>307 Temporary Redirect</c>
    /// when the mapping may change at runtime. The value must be in the 3xx range.
    /// </remarks>
    public int RedirectStatusCode { get; set; } = StatusCodes.Status301MovedPermanently;
}
