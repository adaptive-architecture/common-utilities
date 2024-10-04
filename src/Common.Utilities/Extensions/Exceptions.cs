namespace AdaptArch.Common.Utilities.Extensions;

/// <summary>
/// Extension methods for <see cref="Exception"/> handling and throwing.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Throw a <see cref="NotSupportedException"/> if the object is null.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object instance.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="NotSupportedException"></exception>
    public static void ThrowNotSupportedIfNull<T>(T? obj, string message)
    {
        if (obj == null)
        {
            throw new NotSupportedException(message);
        }
    }

    /// <summary>
    /// Throw a <see cref="NotSupportedException"/> if the object is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="obj">The object instance.</param>
    /// <param name="message">The exception message.</param>
    /// <exception cref="NotSupportedException"></exception>
    public static void ThrowNotSupportedIfNotNull<T>(T? obj, string message)
    {
        if (obj != null)
        {
            throw new NotSupportedException(message);
        }
    }
}
