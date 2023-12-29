namespace AdaptArch.Common.Utilities.Hosting.BackgroundWorkers;

/// <summary>
/// Represents a background job.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// Executes the background job.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
