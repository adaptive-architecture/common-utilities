namespace AdaptArch.Common.Utilities.Serialization.Contracts;

/// <summary>
/// Contract for serializing and deserializing data for string storage.
/// </summary>
public interface IStringDataSerializer
{
    /// <summary>
    /// Serializes an object to a string representation suitable for string storage.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="data">The object to serialize.</param>
    /// <returns>A string representation of the object, or null if the object is null.</returns>
    string? Serialize<T>(T data);

    /// <summary>
    /// Deserializes a string representation back to an object.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="data">The string representation of the object.</param>
    /// <returns>The deserialized object, or the default value if the string is null or empty.</returns>
    T? Deserialize<T>(string? data);
}
