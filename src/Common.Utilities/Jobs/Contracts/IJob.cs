namespace AdaptArch.Common.Utilities.Jobs.Contracts;

/// <summary>
/// Represents a background job.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Executes the background job.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
