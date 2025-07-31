using AdaptArch.Common.Utilities.AspNetCore.Middlewares.ResponseRewrite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Xunit;
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;

public class ResponseStreamWrapperSpecs
{
    private readonly IResponseRewriterFactory _rewriterFactory = new ResponseRewriterFactory();

    [Fact]
    public async Task It_Supports_Writing_AndReading()
    {
        await using var memorySteam = new MemoryStream();
        var responseBodyFeature = Substitute.For<IHttpResponseBodyFeature>();
        _ = responseBodyFeature.Stream.Returns(memorySteam);
        var context = new DefaultHttpContext();
        var responseStreamWrapper = new ResponseStreamWrapper(responseBodyFeature,
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

        await responseStreamWrapper.WriteAsync([1], 0, 1, TestContext.Current.CancellationToken);
        Assert.Equal(1, memorySteam.Length);

        var buffer = new byte[1];
        _ = responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        responseStreamWrapper.ReadExactly(buffer, 0, 1);
        Assert.Equal(1, buffer[0]);
#pragma warning disable CA2022  // Avoid inexact read with 'System.IO.Stream.ReadAsync(byte[], int, int)'
        _ = responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        _ = await responseStreamWrapper.ReadAsync(buffer, 0, 1, TestContext.Current.CancellationToken);
        Assert.Equal(1, buffer[0]);

        _ = responseStreamWrapper.Seek(0, SeekOrigin.Begin);
        _ = await responseStreamWrapper.ReadAsync((Memory<byte>)buffer, TestContext.Current.CancellationToken);
        Assert.Equal(1, buffer[0]);
#pragma warning restore CA2022

        responseStreamWrapper.Flush();
    }
}
