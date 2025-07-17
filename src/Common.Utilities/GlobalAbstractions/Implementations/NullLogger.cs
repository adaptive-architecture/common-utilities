using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

namespace AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

/// <summary>
/// A null implementation of <see cref="ILogger"/> that discards all log messages.
/// </summary>
public class NullLogger : ILogger
{
    /// <summary>
    /// Singleton instance of the <see cref="NullLogger"/>.
    /// </summary>
    public static readonly NullLogger Instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NullLogger"/> class.
    /// </summary>
    protected NullLogger()
    {
        // This constructor is protected to prevent instantiation from outside.
    }
    /// <inheritdoc />
    public void LogTrace(string message, params object[] args)
    {
        // Discard the log message
    }

    /// <inheritdoc />
    public void LogDebug(string message, params object[] args)
    {
        // Discard the log message
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object[] args)
    {
        // Discard the log message
    }

    /// <inheritdoc />
    public void LogWarning(Exception? exception, string message, params object[] args)
    {
        // Discard the log message
    }

    /// <inheritdoc />
    public void LogError(Exception? exception, string message, params object[] args)
    {
        // Discard the log message
    }

    /// <inheritdoc />
    public void LogCritical(Exception? exception, string message, params object[] args)
    {
        // Discard the log message
    }
}
