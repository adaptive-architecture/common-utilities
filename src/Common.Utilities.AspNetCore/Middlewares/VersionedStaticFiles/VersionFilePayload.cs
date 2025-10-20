namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

/// <summary>
/// Payload structure for the version file (e.g., `version.json`).
/// </summary>
public class VersionFilePayload
{
    /// <summary>
    /// The version string.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// The UTC timestamp when the version was set.
    /// </summary>
    public required DateTime Timestamp { get; set; }
}
