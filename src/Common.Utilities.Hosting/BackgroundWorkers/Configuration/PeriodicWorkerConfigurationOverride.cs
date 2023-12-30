namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration override for a periodic background worker.
/// </summary>
public class PeriodicWorkerConfigurationOverride
{
    /// <summary>
    /// Gets or sets the name of the worker to override.
    /// </summary>
    public string Pattern { get; init; } = String.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the worker is enabled.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets the repeat period.
    /// </summary>
    public TimeSpan? Period { get; init; }

    /// <summary>
    /// Gets or sets the initial delay.
    /// </summary>
    public TimeSpan? InitialDelay { get; set; }
}
