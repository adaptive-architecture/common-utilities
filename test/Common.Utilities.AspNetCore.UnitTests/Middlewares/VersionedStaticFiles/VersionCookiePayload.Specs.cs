using AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.VersionedStaticFiles;

public class VersionCookiePayloadSpecs
{
    [Fact]
    public void It_Should_Parse_Valid_Cookie_Value()
    {
        const int timestamp = 1609459200; // 2021-01-01 00:00:00 UTC
        const string version = "v1.2.3";
        var cookieValue = $"{timestamp}~{version}";

        var result = VersionCookiePayload.TryParse(cookieValue, out var payload);

        Assert.True(result);
        Assert.Equal(version, payload.Version);
        Assert.Equal(DateTime.UnixEpoch.AddSeconds(timestamp), payload.DateModified);
    }

    [Fact]
    public void It_Should_Parse_Valid_Cookie_Value_With_Complex_Version()
    {
        const int timestamp = 1609459200;
        const string version = "v1.2.3-beta.1+build.456";
        var cookieValue = $"{timestamp}~{version}";

        var result = VersionCookiePayload.TryParse(cookieValue, out var payload);

        Assert.True(result);
        Assert.Equal(version, payload.Version);
    }

    [Fact]
    public void It_Should_Parse_Valid_Cookie_Value_With_Whitespace()
    {
        const int timestamp = 1609459200;
        const string version = "v1.2.3";
        var cookieValue = $"  {timestamp}~{version}  ";

        var result = VersionCookiePayload.TryParse(cookieValue, out var payload);

        Assert.True(result);
        Assert.Equal(version, payload.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("no-separator")]
    [InlineData("~version-only")]
    [InlineData("123")]
    [InlineData("not-a-number~v1.2.3")]
    public void It_Should_Return_False_For_Invalid_Formats(string value)
    {
        var result = VersionCookiePayload.TryParse(value, out var payload);

        Assert.False(result);
        Assert.Equal(String.Empty, payload.Version);
        Assert.Equal(DateTime.UnixEpoch, payload.DateModified);
    }

    [Fact]
    public void It_Should_Return_False_When_Separator_Is_At_Start()
    {
        const string cookieValue = "~v1.2.3";

        var result = VersionCookiePayload.TryParse(cookieValue, out var _);

        Assert.False(result);
    }

    [Fact]
    public void It_Should_Generate_Correct_String_Representation()
    {
        const int timestamp = 1609459200;
        const string version = "v1.2.3";
        var payload = new VersionCookiePayload
        {
            DateModified = DateTime.UnixEpoch.AddSeconds(timestamp),
            Version = version
        };

        var result = payload.ToString();

        Assert.Equal($"{timestamp}~{version}", result);
    }

    [Fact]
    public void It_Should_Calculate_Unix_Timestamp_Correctly()
    {
        const int timestamp = 1609459200;
        var payload = new VersionCookiePayload
        {
            DateModified = DateTime.UnixEpoch.AddSeconds(timestamp),
            Version = "v1.0.0"
        };

        Assert.Equal(timestamp, payload.UnixTimestamp);
    }

    [Fact]
    public void It_Should_Round_Trip_Parse_And_ToString()
    {
        const int originalTimestamp = 1609459200;
        const string originalVersion = "v1.2.3";
        var originalString = $"{originalTimestamp}~{originalVersion}";

        var parseResult = VersionCookiePayload.TryParse(originalString, out var payload);
        var recreatedString = payload.ToString();

        Assert.True(parseResult);
        Assert.Equal(originalString, recreatedString);
    }

    [Fact]
    public void It_Should_Handle_Empty_Version_String()
    {
        const int timestamp = 1609459200;
        var cookieValue = $"{timestamp}~";

        var result = VersionCookiePayload.TryParse(cookieValue, out var payload);

        Assert.True(result);
        Assert.Equal(String.Empty, payload.Version);
    }

    [Fact]
    public void It_Should_Handle_Version_With_Multiple_Separators()
    {
        const int timestamp = 1609459200;
        const string version = "v1~2~3";
        var cookieValue = $"{timestamp}~{version}";

        var result = VersionCookiePayload.TryParse(cookieValue, out var payload);

        Assert.True(result);
        Assert.Equal(version, payload.Version);
    }

    [Fact]
    public void It_Should_Use_Unix_Epoch_As_Default_DateModified()
    {
        var payload = new VersionCookiePayload();

        Assert.Equal(DateTime.UnixEpoch, payload.DateModified);
    }

    [Fact]
    public void It_Should_Use_Empty_String_As_Default_Version()
    {
        var payload = new VersionCookiePayload();

        Assert.Equal(String.Empty, payload.Version);
    }
}
