namespace AdaptArch.Common.Utilities.Configuration.Contracts;

/// <summary>
/// Configuration parser.
/// </summary>
public interface IConfigurationParser
{
    /// <summary>
    /// Parse a <see cref="String"/> containing configuration data.
    /// </summary>
    /// <param name="input">The configuration data stream.</param>
    IReadOnlyDictionary<string, string?> Parse(string input);
}
