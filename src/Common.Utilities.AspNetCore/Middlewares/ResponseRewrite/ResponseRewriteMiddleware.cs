using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;

/// <summary>
/// Middleware for transforming the response before sending it to the client.
/// </summary>
public class ResponseRewriteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IResponseRewriterFactory _responseRewriterFactory;

    /// <summary>
    /// Constructor for the ResponseRewriteMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="responseRewriterFactory">The factory for creating the response transform service.</param>
    public ResponseRewriteMiddleware(RequestDelegate next, IResponseRewriterFactory responseRewriterFactory)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(responseRewriterFactory);
        _next = next;
        _responseRewriterFactory = responseRewriterFactory;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    public async Task Invoke(HttpContext context)
    {
        ResponseStreamWrapper? filteredResponse = null;

        try
        {
            if (_responseRewriterFactory.MightRewrite(context))
            {
                filteredResponse = new ResponseStreamWrapper(context.Response.Body, context, _responseRewriterFactory);
                context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(filteredResponse));
            }

            await _next(context);
        }
        finally
        {
            if (filteredResponse != null)
            {
                await filteredResponse.DisposeAsync();
            }
        }
    }
}
