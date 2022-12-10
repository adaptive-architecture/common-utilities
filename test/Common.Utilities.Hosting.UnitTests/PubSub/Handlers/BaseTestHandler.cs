namespace AdaptArch.Common.Utilities.Hosting.UnitTests.PubSub.Handlers;

public abstract class BaseTestHandler
{
    protected readonly HandlerDependency Dependency;

    protected BaseTestHandler(HandlerDependency dependency)
    {
        Dependency = dependency;
    }
}
