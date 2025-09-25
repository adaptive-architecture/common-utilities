namespace AdaptArch.Common.Utilities.ConsistentHashing;

/// <summary>
/// Exception thrown when attempting to create a configuration snapshot that would exceed the history limit.
/// This exception helps enforce the maximum history size constraint configured for version-aware hash rings.
/// </summary>
public sealed class HashRingHistoryLimitExceededException : InvalidOperationException
{
    /// <summary>
    /// Gets the maximum allowed history size.
    /// </summary>
    public int MaxHistorySize { get; }

    /// <summary>
    /// Gets the current history count at the time the exception was thrown.
    /// </summary>
    public int CurrentCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class.
    /// </summary>
    public HashRingHistoryLimitExceededException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public HashRingHistoryLimitExceededException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public HashRingHistoryLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class.
    /// </summary>
    /// <param name="maxHistorySize">The maximum allowed history size.</param>
    /// <param name="currentCount">The current history count.</param>
    public HashRingHistoryLimitExceededException(int maxHistorySize, int currentCount)
        : base(FormatMessage(maxHistorySize, currentCount))
    {
        MaxHistorySize = maxHistorySize;
        CurrentCount = currentCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class with a custom message.
    /// </summary>
    /// <param name="maxHistorySize">The maximum allowed history size.</param>
    /// <param name="currentCount">The current history count.</param>
    /// <param name="message">The custom error message.</param>
    public HashRingHistoryLimitExceededException(int maxHistorySize, int currentCount, string message)
        : base(message)
    {
        MaxHistorySize = maxHistorySize;
        CurrentCount = currentCount;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashRingHistoryLimitExceededException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="maxHistorySize">The maximum allowed history size.</param>
    /// <param name="currentCount">The current history count.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public HashRingHistoryLimitExceededException(int maxHistorySize, int currentCount, string message, Exception innerException)
        : base(message, innerException)
    {
        MaxHistorySize = maxHistorySize;
        CurrentCount = currentCount;
    }

    private static string FormatMessage(int maxHistorySize, int currentCount)
    {
        return $"Cannot create configuration snapshot. History limit of {maxHistorySize} would be exceeded. Current count: {currentCount}";
    }

    /// <summary>
    /// Returns a string representation of the exception.
    /// </summary>
    /// <returns>A string describing the exception details.</returns>
    public override string ToString()
    {
        return $"{GetType().Name}: {Message} (MaxHistorySize: {MaxHistorySize}, CurrentCount: {CurrentCount})";
    }
}
