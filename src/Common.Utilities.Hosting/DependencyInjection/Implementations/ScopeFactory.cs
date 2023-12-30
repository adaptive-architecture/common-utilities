using AdaptArch.Common.Utilities.Hosting.DependencyInjection.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace AdaptArch.Common.Utilities.Hosting.DependencyInjection.Implementations
{
    /// <summary>
    /// The scope factory.
    /// </summary>
    public class ScopeFactory : IScopeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs a new instance of <see cref="ScopeFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">The service scope factory.</param>
        public ScopeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public IServiceScope CreateScope(string name)
        {
            return _serviceProvider.CreateScope();
        }

        /// <inheritdoc/>
        public void DisposeScope(IDisposable scope)
        {
            scope.Dispose();
        }
    }
}
