using AdaptArch.Common.Utilities.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Extensions;

public class PathStringSpecs
{
    [Fact]
    public void ValueOrEmptyString_Should_Return_Empty_String_When_Value_Is_Null()
    {
        // Arrange
        var pathString = new PathString();

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Return_Empty_String_When_Value_Is_Empty()
    {
        // Arrange
        var pathString = new PathString(String.Empty);

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Return_Value_When_Path_Is_Root()
    {
        // Arrange
        var pathString = new PathString("/");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/", result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Return_Value_When_Path_Has_Single_Segment()
    {
        // Arrange
        var pathString = new PathString("/api");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/api", result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Return_Value_When_Path_Has_Multiple_Segments()
    {
        // Arrange
        var pathString = new PathString("/static/app/index.js");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/static/app/index.js", result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Preserve_Special_Characters()
    {
        // Arrange
        var pathString = new PathString("/api/v1/users?id=123");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/api/v1/users?id=123", result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Preserve_Case()
    {
        // Arrange
        var pathString = new PathString("/Static/App/Index.JS");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/Static/App/Index.JS", result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Preserve_Encoded_Characters()
    {
        // Arrange
        var pathString = new PathString("/path%20with%20spaces");

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal("/path%20with%20spaces", result);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/api/users")]
    [InlineData("/static/css/style.css")]
    [InlineData("/a/b/c/d/e/f/g")]
    public void ValueOrEmptyString_Should_Return_Value_For_Various_Paths(string path)
    {
        // Arrange
        var pathString = new PathString(path);

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal(path, result);
    }

    [Fact]
    public void ValueOrEmptyString_Should_Return_Empty_String_For_Default_PathString()
    {
        // Arrange
        var pathString = default(PathString);

        // Act
        var result = pathString.ValueOrEmptyString();

        // Assert
        Assert.Equal(String.Empty, result);
    }
}
