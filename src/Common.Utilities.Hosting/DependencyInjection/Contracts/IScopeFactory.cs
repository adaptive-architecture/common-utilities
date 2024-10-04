using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;

/// <summary>
/// The scope factory.
/// </summary>
public interface IScopeFactory
{
    /// <summary>
    /// Creates a new scope.
    /// </summary>
    /// <param name="name">The name of the new scope.</param>
    /// <returns></returns>
    IServiceScope CreateScope(string name);

    /// <summary>
    /// Disposes the scope.
    /// </summary>
    void DisposeScope(IDisposable scope);
}
