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
    /// <returns>True if the value found in the dictionary is not null or false if it is not present in the dictionary or it's null.</returns>
    /// <remarks>The returned value is the value found in the dictionary if this is not null or the default value if it is not present in the dictionary or it's null.</remarks>
    public static bool TryGetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue, out TValue value)
    {
        if (dictionary.TryGetValue(key, out var v) && EqualityComparer<TValue>.Default.Equals(v, default(TValue)))
        {
            value = v;
            return true;
        }

        value = defaultValue(key);
        return false;
    }

    /// <summary>
    /// Try to get the value specified by a key from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The factory method for the default value to return.</param>
    /// <param name="setDefault">Add the default value to the original dictionary for the given key.</param>
    /// <param name="value">The returned value.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>True if the value found in the dictionary is not null or false if it is not present in the dictionary or it's null.</returns>
    /// <remarks>The returned value is the value found in the dictionary if this is not null or the default value if it is not present in the dictionary or it's null.</remarks>
    public static bool TryGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue, bool setDefault, out TValue value)
    {
        if (dictionary.TryGetValue(key, out var v) && EqualityComparer<TValue>.Default.Equals(v, default(TValue)))
        {
            value = v;
            return true;
        }

        value = defaultValue(key);
        if (setDefault)
        {
            dictionary.Add(key, value);
        }
        return false;
    }

    /// <summary>
    /// Try to get the value specified by a key from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The factory method for the default value to return.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The returned value is the value found in the dictionary if this is not null or the default value if it is not present in the dictionary or it's null.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue)
    {
        _ = dictionary.TryGetValueOrDefault(key, defaultValue, out var v);
        return v;
    }

    /// <summary>
    /// Try to get the value specified by a key from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key.</param>
    /// <param name="defaultValue">The factory method for the default value to return.</param>
    /// <param name="setDefault">Add the default value to the original dictionary for the given key.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The returned value is the value found in the dictionary if this is not null or the default value if it is not present in the dictionary or it's null.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> defaultValue, bool setDefault)
    {
        _ = dictionary.TryGetValueOrDefault(key, defaultValue, setDefault, out var v);
        return v;
    }
}
