namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <summary>
/// The result of the loading exception handlers.
/// </summary>
public class LoadExceptionHandlerResult
{
    /// <summary>
    /// Determines if the exception should be ignored.
    /// </summary>
    public bool IgnoreException { get; set; }

    /// <summary>
    /// Determines if the <see cref="CustomConfigurationProvider"/> should stop pooling for changes.
    /// </summary>
    public bool DisablePooling { get; set; }
}
