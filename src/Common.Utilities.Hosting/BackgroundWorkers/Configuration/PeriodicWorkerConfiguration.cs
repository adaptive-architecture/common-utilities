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
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(60);

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
        foreach (var @override in Overrides)
        {
            if (String.IsNullOrWhiteSpace(@override.Pattern))
            {
                continue;
            }

            var matcher = new Regex(@override.Pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            if (matcher.IsMatch(workerName))
            {
                return FromOverride(@override);
            }
        }

        return this;
    }

    private static PeriodicWorkerConfiguration FromOverride(PeriodicWorkerConfigurationOverride @override)
    {
        var result = new PeriodicWorkerConfiguration();
        if (@override.Enabled.HasValue)
        {
            result.Enabled = @override.Enabled.Value;
        }

        if (@override.Period.HasValue)
        {
            result.Period = @override.Period.Value;
        }

        if (@override.InitialDelay.HasValue)
        {
            result.InitialDelay = @override.InitialDelay.Value;
        }

        return result;
    }
}
