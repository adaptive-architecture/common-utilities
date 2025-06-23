using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

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
        IHttpResponseBodyFeature? originalBodyFeature = null;

        try
        {
            if (_responseRewriterFactory.MightRewrite(context))
            {
                originalBodyFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
                filteredResponse = new ResponseStreamWrapper(originalBodyFeature, context, _responseRewriterFactory);
                context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(filteredResponse));
            }

            await _next(context);
        }
        finally
        {
            if (filteredResponse != null)
            {
                await filteredResponse.DisposeAsync();
                context.Features.Set(originalBodyFeature);
            }
        }
    }
}
