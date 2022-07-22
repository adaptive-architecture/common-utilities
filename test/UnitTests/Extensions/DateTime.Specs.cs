using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.UnitTests.Extensions;

public class DateTimeSpecs
{
    [Fact]
    public void Unix_Conversions_Should_Not_Fail()
    {
        var date = DateTime.UnixEpoch.AddSeconds(1);

        Assert.Equal(date, ((long) 1000).AsUnixTimeMilliseconds());
        Assert.Equal(date, ((ulong) 1000).AsUnixTimeMilliseconds());
        Assert.Equal(date, ((double) 1000).AsUnixTimeMilliseconds());

        Assert.Equal(date, ((long) 1).AsUnixTimeSeconds());
        Assert.Equal(date, ((ulong) 1).AsUnixTimeSeconds());
        Assert.Equal(date, ((double) 1).AsUnixTimeSeconds());

        Assert.Equal(1, date.ToUnixTimeSeconds());
        Assert.Equal(1000, date.ToUnixTimeMilliseconds());
    }
}
