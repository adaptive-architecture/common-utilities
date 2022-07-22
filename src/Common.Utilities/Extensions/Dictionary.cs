namespace AdaptArch.Common.Utilities.Extensions;

/// <summary>
/// Extension methods for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Try to get the value specified by a key from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The factory method for the default value to return.</param>
    /// <param name="value">The returned value.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The value found in the dictionary if this is not null or the default value if it is not present in the dictionary or it's null.</returns>
    public static bool TryGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue, out TValue value)
    {
        if (dictionary.TryGetValue(key, out var v) && v != null)
        {
            value = v;
            return true;
        }

        value = defaultValue(key);
        return false;
    }
}
