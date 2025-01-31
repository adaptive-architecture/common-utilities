using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseTransform;

/// <summary>
/// Middleware for transforming the response before sending it to the client.
/// </summary>
public class ResponseTransformMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseTransformServiceFactory _responseTransformServiceFactory;

    /// <summary>
    /// Constructor for the ResponseTransformMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="responseTransformServiceFactory">The factory for creating the response transform service.</param>
    public ResponseTransformMiddleware(RequestDelegate next, IResponseTransformServiceFactory responseTransformServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(responseTransformServiceFactory);
        _next = next;
        _responseTransformServiceFactory = responseTransformServiceFactory;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    public async Task Invoke(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        await _next(context);

        var responseTransformService = await _responseTransformServiceFactory.GetTransformServiceAsync(context);
        if (responseTransformService == null)
        {
            responseBuffer.Seek(0, SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalBodyStream);
        }
        else
        {
            context.Response.Body = originalBodyStream;
            responseBuffer.Seek(0, SeekOrigin.Begin);
            var modifiedContent = await responseTransformService.Transform(responseBuffer, context);
            await context.Response.BodyWriter.WriteAsync(modifiedContent);
        }
    }
}
