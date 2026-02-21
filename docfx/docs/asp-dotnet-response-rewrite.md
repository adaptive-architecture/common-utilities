# ASP.NET Response Rewrite

Modify HTTP response content before it reaches the client using customizable rewriter implementations.

## Overview

Response rewriting enables you to:

- ✅ **Transform response content** dynamically based on request context
- ✅ **Apply conditional modifications** using custom logic
- ✅ **Maintain performance** through selective processing
- ✅ **Support multiple content types** with different rewriters

> **Warning**: Response processing is resource-intensive and can impact performance. Always benchmark your implementation in production environments.

## Basic Usage

Implement response rewriting by following these steps:

### Define the `IResponseRewriter`

This example demonstrates uppercasing all instances of `foo` in the response.

Create an `IResponseRewriter` implementation that reads the response, finds the word `foo`, and replaces it with `FOO`.
The `CanRewrite` method ensures we only process UTF-8 encoded text responses:

``` csharp
public class UppercaseFooRewriter : IResponseRewriter
{
    public async Task RewriteAsync(ReadOnlyMemory<byte> buffer, HttpContext context, Stream originalStream, CancellationToken cancellationToken)
    {
        var content = Encoding.UTF8.GetString(buffer.Span);
        content = content.Replace(" foo ", " FOO ");
        var data = Encoding.UTF8.GetBytes(content);
        await originalStream.WriteAsync(data, cancellationToken);
    }

    public bool CanRewrite(HttpContext context)
    {
        var contentType = context.Response.ContentType;
        return contentType.Contains("charset=utf-8", StringComparison.OrdinalIgnoreCase)
            && contentType.Contains("text/", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() { }
}
```

### Define the `IResponseRewriterFactory`

Create an `IResponseRewriterFactory` to manage multiple rewriter types and optimize performance. The factory handles:

- **Early filtering**: The `MightRewrite` method determines if a request might need rewriting before processing the response
- **Rewriter selection**: Creates the appropriate `IResponseRewriter` for each request, or returns `null` to skip rewriting

``` csharp
public class ResponseRewriterFactory : IResponseRewriterFactory
{
    /*
     For the purpose of the demo we will only ATTEMPT to rewrite requests that
     have the `rewrite-me` query parameter.
    */
    public bool MightRewrite(HttpContext context) => context.Request.Query.ContainsKey("rewrite-me");

    /*
     For the purpose of the demo we will create the rewriter by simply creating a new instance
     but in your own implementation you can take advantage of dependency injection.
    */
    public IResponseRewriter GetRewriter(HttpContext context)
    {
        if (context.Request.Path == "/foo-text")
        {
            return new UppercaseFooRewriter();
        }

        return null;
    }


}

```

### Register Dependencies

Configure the application to use response rewriting:

``` csharp


builder.Services
    .AddResponseRewriterFactory<ResponseRewriterFactory>() /*<--  Add the factory to the dependency container.*/
    .AddRouting();



var app = builder.Build();
app
    .UseResponseRewrite() /*<--  Add the middleware to the application pipeline.*/
    .UseRouting()
    .UseEndpoints(endpoints =>
    {
        // Add your endpoints
    });

app.Run();
```

## Related Documentation

- [Asp.Net Versioned Static Files](asp-dotnet-versioned-static-files.md)
