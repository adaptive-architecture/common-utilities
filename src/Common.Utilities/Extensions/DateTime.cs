namespace AdaptArch.Common.Utilities.Extensions;

/// <summary>
/// Extension methods for <see cref="DateTime"/>.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Convert a unix time in milliseconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeMilliseconds(this double unixTime)
    {
        return DateTime.UnixEpoch.AddMilliseconds(unixTime);
    }

    /// <summary>
    /// Convert a unix time in milliseconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeMilliseconds(this long unixTime)
    {
        return DateTime.UnixEpoch.AddMilliseconds(unixTime);
    }

    /// <summary>
    /// Convert a unix time in milliseconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeMilliseconds(this ulong unixTime)
    {
        return DateTime.UnixEpoch.AddMilliseconds(unixTime);
    }

    /// <summary>
    /// Convert a unix time in seconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeSeconds(this double unixTime)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTime);
    }

    /// <summary>
    /// Convert a unix time in seconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeSeconds(this long unixTime)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTime);
    }

    /// <summary>
    /// Convert a unix time in seconds to a <see cref="DateTime"/> structure.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static DateTime AsUnixTimeSeconds(this ulong unixTime)
    {
        return DateTime.UnixEpoch.AddSeconds(unixTime);
    }

    /// <summary>
    /// Convert a <see cref="DateTime"/> structure in milliseconds.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static double ToUnixTimeMilliseconds(this DateTime unixTime)
    {
        return (unixTime - DateTime.UnixEpoch).TotalMilliseconds;
    }

    /// <summary>
    /// Convert a <see cref="DateTime"/> structure in seconds.
    /// </summary>
    /// <param name="unixTime">The value to convert</param>
    public static double ToUnixTimeSeconds(this DateTime unixTime)
    {
        return (unixTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
