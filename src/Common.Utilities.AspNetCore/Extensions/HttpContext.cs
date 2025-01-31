using System.Net;
using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Determines if the request is local.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    public static bool IsLocal(this HttpContext context)
    {
        var connection = context.Connection;
        if (connection.RemoteIpAddress == null && connection.LocalIpAddress == null)
        {
            return true;
        }

        if (connection.RemoteIpAddress != null)
        {
            return connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                || IPAddress.IsLoopback(connection.RemoteIpAddress);
        }

        return false;
    }
}
