using System.Text;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;
using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;

public class ResponseRewriterFactory : IResponseRewriterFactory
{
    public IResponseRewriter GetRewriter(HttpContext context)
    {
        if (context.Request.Path == "/transformed" && Utf8StringResponseRewriter.CanRewrite(context))
        {
            return new Utf8StringResponseRewriter();
        }
        else if (context.Request.Path == "/no-content")
        {
            return new Utf8StringResponseRewriter();
        }

        return null;
    }

    public bool MightRewrite(HttpContext context) => !context.Request.Query.ContainsKey("skip-rewrite");
}

public sealed class Utf8StringResponseRewriter : IResponseRewriter
{
    public async Task RewriteAsync(ReadOnlyMemory<byte> buffer, HttpContext context, Stream originalStream, CancellationToken cancellationToken)
    {
        var content = Encoding.UTF8.GetString(buffer.Span);
        content = content.Replace("NOT ", String.Empty);
        var data = Encoding.UTF8.GetBytes(content);
        await originalStream.WriteAsync(data, cancellationToken);
    }

    public void Dispose() { }

    public static bool CanRewrite(HttpContext context)
    {
        if (context.Response.StatusCode == StatusCodes.Status204NoContent)
        {
            return true;
        }

        var contentType = context.Response.ContentType;
        return contentType.Contains("charset=utf-8", StringComparison.OrdinalIgnoreCase)
            && (contentType.Contains("text/", StringComparison.OrdinalIgnoreCase)
                || contentType.Contains("/json", StringComparison.OrdinalIgnoreCase));
    }
}
