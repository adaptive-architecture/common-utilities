namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration for a periodic background worker.
/// </summary>
public class PeriodicWorkerConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the worker is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the repeat period.
    /// </summary>
    public TimeSpan Period { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the initial delay.
    /// </summary>
    public TimeSpan InitialDelay { get; set; }
}
