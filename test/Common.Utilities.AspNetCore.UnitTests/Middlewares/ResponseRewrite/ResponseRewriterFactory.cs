using System.Text;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;

public class ResponseRewriterFactory : IResponseRewriterFactory
{
    public IResponseRewriter GetRewriter(HttpContext context)
    {
        if (context.Request.Path == "/transformed" || context.Request.Path == "/no-content")
        {
            return new Utf8StringResponseRewriter();
        }
        else if (context.Request.Path == "/methods-tests")
        {
            return new MethodsTestRewriter();
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

    public bool CanRewrite(HttpContext context)
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

public sealed class MethodsTestRewriter : IResponseRewriter
{
    public Task RewriteAsync(ReadOnlyMemory<byte> buffer, HttpContext context, Stream originalStream, CancellationToken cancellationToken)
    {
        Assert.False(context.Response.Body.CanRead);
        Assert.False(context.Response.Body.CanSeek);
        Assert.True(context.Response.Body.CanWrite);

        _ = Assert.Throws<NotSupportedException>(() => context.Response.Body.Length);
        _ = Assert.Throws<NotSupportedException>(() => context.Response.Body.Position);
        _ = Assert.Throws<NotSupportedException>(() => context.Response.Body.Position = 0);

        return Task.CompletedTask;
    }

    public void Dispose() { }

    public bool CanRewrite(HttpContext context) => true;
}
