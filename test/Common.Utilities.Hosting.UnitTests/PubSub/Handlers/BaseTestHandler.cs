using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public class BaseTestHandler
{
    protected readonly HandlerDependency Dependency;

    protected BaseTestHandler(HandlerDependency dependency)
    {
        Dependency = dependency;
    }
}
