namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration override for a repeating job worker.
/// </summary>
public class RepeatingWorkerConfigurationOverride
{
    /// <summary>
    /// Gets or sets the name of the worker to override.
    /// This is a Regex pattern that will be matched against the worker implementation name (with namespace).
    /// </summary>
    public string Pattern { get; init; } = String.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the worker is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the repeat/delay interval.
    /// </summary>
    public TimeSpan? Interval { get; init; }

    /// <summary>
    /// Gets or sets the initial delay.
    /// </summary>
    public TimeSpan? InitialDelay { get; set; }
}
