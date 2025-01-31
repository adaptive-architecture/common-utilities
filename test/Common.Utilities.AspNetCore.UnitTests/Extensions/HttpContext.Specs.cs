using System.Net;
using AdaptArch.Common.Utilities.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Extensions;

public class HttpContextSpecs
{
    [Fact]
    public void Should_Return_True_If_Null()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = context.Connection.LocalIpAddress = null;

        Assert.True(context.IsLocal());
    }

    [Fact]
    public void Should_Return_True_If_Local()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = context.Connection.LocalIpAddress = IPAddress.Loopback;

        Assert.True(context.IsLocal());
    }

    [Fact]
    public void Should_Return_True_If_Loopback()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        Assert.True(context.IsLocal());
    }

    [Fact]
    public void Should_Return_False_If_Not_Local__Remote_Is_Null()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;
        context.Connection.LocalIpAddress = IPAddress.Parse("192.168.0.1");

        Assert.False(context.IsLocal());
    }

    [Fact]
    public void Should_Return_False_If_Not_Local__Local_Is_Null()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.0.1");
        context.Connection.LocalIpAddress = null;

        Assert.False(context.IsLocal());
    }

    [Fact]
    public void Should_Return_False_If_Not_Local__Local_Is_Loopback()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.0.1");
        context.Connection.LocalIpAddress = IPAddress.Loopback;

        Assert.False(context.IsLocal());
    }


}
