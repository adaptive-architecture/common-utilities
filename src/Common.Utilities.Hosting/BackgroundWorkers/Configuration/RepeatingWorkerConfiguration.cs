using System.Text.RegularExpressions;

namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers.Configuration;

/// <summary>
/// The configuration for a repeating job worker.
/// </summary>
public class RepeatingWorkerConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the worker is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the repeat/delay interval.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Gets or sets the initial delay.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the overrides for specific workers.
    /// </summary>
    public List<RepeatingWorkerConfigurationOverride> Overrides { get; init; } = [];

    /// <summary>
    /// Gets the configuration for a specific worker.
    /// </summary>
    public RepeatingWorkerConfiguration GetConfiguration(string workerName)
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

    private static RepeatingWorkerConfiguration FromOverride(RepeatingWorkerConfigurationOverride @override)
    {
        var result = new RepeatingWorkerConfiguration();
        if (@override.Enabled.HasValue)
        {
            result.Enabled = @override.Enabled.Value;
        }

        if (@override.Interval.HasValue)
        {
            result.Interval = @override.Interval.Value;
        }

        if (@override.InitialDelay.HasValue)
        {
            result.InitialDelay = @override.InitialDelay.Value;
        }

        return result;
    }
}
