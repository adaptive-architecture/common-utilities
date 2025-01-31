using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseTransform;

/// <summary>
/// Factory for creating the response transform service.
/// </summary>
public interface IResponseTransformServiceFactory
{
    /// <summary>
    /// Get the response transform service.
    /// </summary>
    /// <param name="context">The HttpContext.</param>
    Task<IResponseTransformService?> GetTransformServiceAsync(HttpContext context);
}
