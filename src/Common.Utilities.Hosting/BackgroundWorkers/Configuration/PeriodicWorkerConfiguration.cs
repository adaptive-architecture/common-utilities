using System.Text.RegularExpressions;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration for a periodic background worker.
/// </summary>
public class PeriodicWorkerConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the repeat period.
    /// </summary>
    public TimeSpan Period { get; init; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Gets or sets the initial delay.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the overrides for specific workers.
    /// </summary>
    public List<PeriodicWorkerConfigurationOverride> Overrides { get; init; } = new();

    /// <summary>
    /// Gets the configuration for a specific worker.
    /// </summary>
    public PeriodicWorkerConfiguration GetConfiguration(string workerName)
    {
        PeriodicWorkerConfigurationOverride? matchingOverride = null;
        foreach (var @override in Overrides)
        {
            if (String.IsNullOrWhiteSpace(@override.Pattern))
            {
                continue;
            }

            var matcher = new Regex(@override.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (matcher.IsMatch(workerName))
            {
                matchingOverride = @override;
            }
        }

        return matchingOverride == null ? this : new PeriodicWorkerConfiguration
        {
            Enabled = matchingOverride.Enabled ?? Enabled,
            Period = matchingOverride.Period ?? Period,
            InitialDelay = matchingOverride.InitialDelay ?? InitialDelay
        };
    }
}
