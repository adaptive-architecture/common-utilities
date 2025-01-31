using System.Text;
using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseTransform;
using Microsoft.AspNetCore.Http;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseTransform;

public class ResponseTransformServiceFactory : IResponseTransformServiceFactory
{
    public async Task<IResponseTransformService> GetTransformServiceAsync(HttpContext context)
    {
        await Task.Yield();

        if (context.Request.Path == "/transformed")
        {
            return new StringTransformService();
        }

        return null;
    }
}

public sealed class StringTransformService : IResponseTransformService
{
    public async Task<ReadOnlyMemory<byte>> Transform(MemoryStream originalResponse, HttpContext context)
    {
        var responseEncoding = context.Response.ContentType?.Contains("utf-8", StringComparison.OrdinalIgnoreCase) == true
            ? Encoding.UTF8
            : Encoding.Default;

        using var reader = new StreamReader(originalResponse, responseEncoding);
        var originalContent = await reader.ReadToEndAsync();

        var transformed = originalContent.Replace("NOT", String.Empty);
        return responseEncoding.GetBytes(transformed);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
