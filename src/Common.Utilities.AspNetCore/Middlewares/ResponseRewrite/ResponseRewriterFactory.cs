using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;

/// <summary>
/// Factory for creating the response rewriter.
/// </summary>
public interface IResponseRewriterFactory
{
    /// <summary>
    /// Check if the request might be rewritten. This is going to be called before the response starts.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    /// <returns>True if there is a chance we might want to rewrite the response</returns>
    bool MightRewrite(HttpContext context);

    /// <summary>
    /// Get the response rewriter.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    IResponseRewriter? GetRewriter(HttpContext context);
}
