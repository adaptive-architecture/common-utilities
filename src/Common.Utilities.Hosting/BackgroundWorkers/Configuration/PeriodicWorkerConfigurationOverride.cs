namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration override for a periodic background worker.
/// </summary>
public class PeriodicWorkerConfigurationOverride : PeriodicWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the name of the worker to override.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;
}
