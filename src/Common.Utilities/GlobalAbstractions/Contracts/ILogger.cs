namespace AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

/// <summary>
/// Defines logging operations for the common utilities library.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a trace message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogTrace(string message, params object[] args);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="exception">The exception related to the error.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogWarning(Exception? exception, string message, params object[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="exception">The exception related to the error.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogError(Exception? exception, string message, params object[] args);

    /// <summary>
    /// Logs an critical message.
    /// </summary>
    /// <param name="exception">The exception related to the error.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for the message.</param>
    void LogCritical(Exception? exception, string message, params object[] args);
}
