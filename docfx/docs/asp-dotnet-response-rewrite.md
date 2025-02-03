# Asp.Net Response Rewrite

Sometimes in your application you might need to rewrite the response of the API.

## Word of caution
Processing the response is going to be a resource intensive operation and should be done carefully.

When doing this in a production application benchmark your implementation as it can have a detrimental impact on performance.

## Usage

In order to rewrite the response of the API you need to perform the following steps.

### Define the `IResponseRewriter`

Let's assume you want to uppercase all the instances of `foo` in the response.

To achieve this we will implement a `IResponseRewriter`. Our naive implementation will read the entire original response, look for the word `foo` and replace it with `FOO`.
Since we are reading the string from what we assume is a UTF8 encoded byte stream we will place this constrain in the `CanRewrite` method.

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

Depending on the needs you might have multiple responses you are looking at rewriting, each with it's own particularities. To keep things as simple as possible need to implement a `IResponseRewriterFactory` to is responsible for the following:
* Determine if the request might be rewritten. The `MightRewrite` method will be called before the actual response gets processed and this way it can reduce the unnecessary resource usage caused by the middleware. At this point no response will be available on the `HttpContext` object.
* Creating the `IResponseRewriter` correct for the request. In case we do not want to rewrite a response we should just return `null`.

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

### Register the dependencies with the application

In you application configuration register the necessary dependencies.

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
Console.WriteLine("Application started!");
```
