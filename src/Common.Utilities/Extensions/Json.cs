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
    /// Determines whether the specified value is a JSON number.
    /// </summary>
    /// <param name="value">The value to check.</param>
    public static bool IsJsonNumber(this string value)
    {
        var decimalSeparatorFound = false;
        char currentChar;

        for (var i = 0; i < value.Length; i++)
        {
            currentChar = value[i];
            if (Char.IsAsciiDigit(currentChar))
            {
                continue;
            }

            if (!decimalSeparatorFound && currentChar == '.')
            {
                decimalSeparatorFound = true;
                continue;
            }

            if (currentChar == 'e' || currentChar == 'E')
            {
                if (i >= value.Length - 1)
                {
                    return false;
                }

                currentChar = value[++i];
                // Check the next character for a sign and then a digit.
                if (currentChar != '+' && currentChar != '-' && !Char.IsAsciiDigit(currentChar))
                {
                    return false;
                }

                // All the following characters must be digits.
                while (++i < value.Length)
                {
                    if (!Char.IsAsciiDigit(value[i]))
                    {
                        return false;
                    }
                }
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified value is a valid JSON.
    /// </summary>
    /// <param name="value">The value to check.</param>
    public static bool IsJson(this string value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return false;

        if (s_jsonValues.Contains(value) || value.IsJsonNumber())
        {
            return true;
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
