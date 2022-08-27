namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <summary>
/// Load configuration exception context data.
/// </summary>
public class LoadExceptionContext
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    public LoadExceptionContext(Exception exception)
    {
        Exception = exception;
    }

    /// <summary>
    /// The exception that occurred.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Determines if the context of the exception was a call to reload.
    /// </summary>
    public bool Reload { get; set; } = false;
}
