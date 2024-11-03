using System.Collections.Frozen;
using System.Text.Json;

namespace AdaptArch.Common.Utilities.Extensions;

/// <summary>
/// Extension methods for working with JSON.
/// </summary>
public static class JsonExtensions
{
    private static readonly FrozenSet<string> s_jsonValues = new HashSet<string>(["true", "false", "null"])
        .ToFrozenSet();

    /// <summary>
    /// Determines whether the specified value is a valid JSON.
    /// </summary>
    /// <param name="value">The value to check.</param>
    public static bool IsJson(this string value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return false;

        if (s_jsonValues.Contains(value))
        {
            return true;
        }

        try
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(value));
            _ = reader.Read();
            reader.Skip();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
