using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;

/// <summary>
/// When implemented, provides a way to rewrite the response.
/// </summary>
public interface IResponseRewriter : IDisposable
{
    /// <summary>
    /// Rewrite the response.
    /// </summary>
    /// <param name="buffer">The buffer to rewrite.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="originalStream">The original response stream.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public abstract Task RewriteAsync(ReadOnlyMemory<byte> buffer, HttpContext context, Stream originalStream, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether the response can be rewritten.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    public bool CanRewrite(HttpContext context);
}
