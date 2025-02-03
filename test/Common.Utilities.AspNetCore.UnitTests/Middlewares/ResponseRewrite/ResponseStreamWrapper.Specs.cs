using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;
using Microsoft.AspNetCore.Http;
using Xunit;
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;

public class ResponseStreamWrapperSpecs
{
    private readonly IResponseRewriterFactory _rewriterFactory = new ResponseRewriterFactory();

    [Fact]
    public async Task It_Supports_Writing_AndReading()
    {
        var memorySteam = new MemoryStream();
        var context = new DefaultHttpContext();
        var responseStreamWrapper = new ResponseStreamWrapper(memorySteam,
            context, _rewriterFactory);

        Assert.Equal(memorySteam.CanRead, responseStreamWrapper.CanRead);
        Assert.Equal(memorySteam.CanSeek, responseStreamWrapper.CanSeek);
        Assert.Equal(memorySteam.CanWrite, responseStreamWrapper.CanWrite);
        Assert.Equal(memorySteam.Length, responseStreamWrapper.Length);
        Assert.Equal(memorySteam.Position, responseStreamWrapper.Position);

        responseStreamWrapper.Position = 0;
        responseStreamWrapper.WriteByte(1);

        responseStreamWrapper.SetLength(0);
        responseStreamWrapper.WriteByte(1);
        Assert.Equal(1, memorySteam.Length);

        responseStreamWrapper.SetLength(0);

        await responseStreamWrapper.WriteAsync([1], 0, 1);
        Assert.Equal(1, memorySteam.Length);

        var buffer = new byte[1];
        responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        responseStreamWrapper.Read(buffer, 0, 1);
        Assert.Equal(1, buffer[0]);

        responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        await responseStreamWrapper.ReadAsync(buffer, 0, 1);
        Assert.Equal(1, buffer[0]);

        responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        await responseStreamWrapper.ReadAsync((Memory<byte>)buffer);
        Assert.Equal(1, buffer[0]);

        responseStreamWrapper.Flush();
    }
}
