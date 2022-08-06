using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub.Internals;

public class HandlerRegistrySpecs
{
    private static readonly Delegate DelegateA = () => Console.WriteLine("Delegate A");
    private static readonly Delegate DelegateB = () => Console.WriteLine("Delegate B");

    [Fact]
    public void HandlerRegistry_Should_Add_Handlers()
    {
        var registry = new HandlerRegistry();

        _ = registry.Add<object>("topic_a", DelegateA);
        _ = registry.Add<object>("topic_a", DelegateB);
        _ = registry.Add<object>("topic_b", DelegateA);

        var handlersTopicA = registry.GetTopicHandlers<object>("topic_a").ToArray();
        var collectionSize = handlersTopicA.Length;
        Assert.Equal(2, collectionSize);
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(DelegateA)));
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(DelegateB)));

        var handlersTopicB = registry.GetTopicHandlers<object>("topic_b").ToArray();
        collectionSize = handlersTopicB.Length;
        Assert.Equal(1, collectionSize);
        Assert.Same(DelegateA, handlersTopicB[0]);
    }

    [Fact]
    public void HandlerRegistry_Should_Only_Return_Appropriate_Handlers()
    {
        var registry = new HandlerRegistry();

        _ = registry.Add<HandlerRegistrySpecs>("topic_a", DelegateA);
        _ = registry.Add<object>("topic_a", DelegateB);

        var handlersTopicA = registry.GetTopicHandlers<HandlerRegistrySpecs>("topic_a").ToArray();
        var collectionSize = handlersTopicA.Length;
        Assert.Equal(2, collectionSize);
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(DelegateA)));
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(DelegateB)));

        var handlersTopicB = registry.GetTopicHandlers<object>("topic_a").ToArray();
        collectionSize = handlersTopicB.Length;
        Assert.Equal(1, collectionSize);
        Assert.Same(DelegateB, handlersTopicB[0]);
    }

    [Fact]
    public void HandlerRegistry_Return_Registration_Data()
    {
        var registry = new HandlerRegistry();
        var registration = registry.GetRegistration("some_id");
        Assert.Null(registration);

        var id = registry.Add<object>("topic_a", DelegateA);
        registration = registry.GetRegistration(id);
        Assert.NotNull(registration);
        Assert.Same(DelegateA, registration.Handler);
        Assert.Same(id, registration.Id);
        Assert.Same("topic_a", registration.Topic);
        Assert.Same(typeof(object), registration.HandledType);

        registration = registry.GetRegistration("some_id");
        Assert.Null(registration);
    }
}
