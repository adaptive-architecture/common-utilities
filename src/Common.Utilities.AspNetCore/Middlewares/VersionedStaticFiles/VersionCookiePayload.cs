namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Payload structure for the version cookie.
/// </summary>
public class VersionCookiePayload
{
    private const char DataSeparator = '~';

    /// <summary>
    /// The version string.
    /// </summary>
    public string Version { get; set; } = String.Empty;
    /// <summary>
    /// The date and time when the version was modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UnixEpoch;

    /// <summary>
    /// The Unix timestamp representation of the DateModified property.
    /// </summary>
    public long UnixTimestamp => (long)(DateModified - DateTime.UnixEpoch).TotalSeconds;

    /// <summary>
    /// Returns a string representation of the VersionCookiePayload.
    /// </summary>
    public override string ToString() => $"{UnixTimestamp:D}{DataSeparator}{Version}";

    /// <summary>
    /// Tries to parse a string into a VersionCookiePayload.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">The resulting VersionCookiePayload if parsing is successful; otherwise, a default instance.</param>
    /// <returns>True if parsing is successful; otherwise, false.</returns>
    public static bool TryParse(string? value, out VersionCookiePayload result)
    {
        if (!String.IsNullOrEmpty(value))
        {
            var trimmedValue = value.Trim();
            var separatorIndex = trimmedValue.IndexOf(DataSeparator);
            if (separatorIndex > 0 && Int64.TryParse(trimmedValue.AsSpan(0, separatorIndex), out var unixTimestamp))
            {
                var dateModified = DateTime.UnixEpoch.AddSeconds(unixTimestamp);
                result = new VersionCookiePayload
                {
                    DateModified = dateModified,
                    Version = trimmedValue[(separatorIndex + 1)..]
                };
                return true;
            }
        }

        result = new VersionCookiePayload
        {
            DateModified = DateTime.UnixEpoch,
            Version = String.Empty
        };
        return false;
    }
}
