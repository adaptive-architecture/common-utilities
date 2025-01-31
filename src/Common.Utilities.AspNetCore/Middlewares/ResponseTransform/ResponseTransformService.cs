using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseTransform;

/// <summary>
/// Service for transforming the response before sending it to the client.
/// </summary>
public interface IResponseTransformService : IAsyncDisposable
{
    /// <summary>
    /// Transform the response.
    /// </summary>
    /// <param name="originalResponse">The original response stream.</param>
    /// <param name="context">The HttpContext.</param>
    public abstract Task<ReadOnlyMemory<byte>> Transform(MemoryStream originalResponse, HttpContext context);
}
