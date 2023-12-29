using System.Text.RegularExpressions;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The root configuration for a all periodic background workers.
/// </summary>
public class RootPeriodicWorkerConfiguration : PeriodicWorkerConfiguration
{
    /// <summary>
    /// Gets or sets the overrides for specific workers.
    /// </summary>
    public List<PeriodicWorkerConfigurationOverride> Overrides { get; init; } = new();

    /// <summary>
    /// Gets the configuration for a specific worker.
    /// </summary>
    public PeriodicWorkerConfiguration GetConfiguration(string workerName)
    {
        foreach (var @override in Overrides)
        {
            var matcher = new Regex(@override.Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (matcher.IsMatch(workerName))
            {
                return @override;
            }
        }
        return this;
    }
}
