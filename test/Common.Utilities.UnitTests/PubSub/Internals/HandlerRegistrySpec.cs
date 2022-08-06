using System.Linq;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub.Internals;

public class HandlerRegistrySpec
{
    private static Delegate s_delA = () => { Console.WriteLine("Delegate A"); };
    private static Delegate s_delB = () => { Console.WriteLine("Delegate B"); };

    [Fact]
    public void HandlerRegistry_Should_Add_Handlers()
    {
        var registry = new HandlerRegistry();

        _ = registry.Add<object>("topic_a", s_delA);
        _ = registry.Add<object>("topic_a", s_delB);
        _ = registry.Add<object>("topic_b", s_delA);

        var handlersTopicA = registry.GetTopicHandlers<object>("topic_a").ToArray();
        Assert.Equal(2, (int)handlersTopicA.Length);
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(s_delA)));
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(s_delB)));

        var handlersTopicB = registry.GetTopicHandlers<object>("topic_b").ToArray();
        Assert.Equal(1, (int)handlersTopicB.Length);
        Assert.Same(s_delA, handlersTopicB[0]);
    }

    [Fact]
    public void HandlerRegistry_Should_Only_Return_Appropriate_Handlers()
    {
        var registry = new HandlerRegistry();

        _ = registry.Add<HandlerRegistrySpec>("topic_a", s_delA);
        _ = registry.Add<object>("topic_a", s_delB);

        var handlersTopicA = registry.GetTopicHandlers<HandlerRegistrySpec>("topic_a").ToArray();
        Assert.Equal(2, (int)handlersTopicA.Length);
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(s_delA)));
        Assert.NotNull(handlersTopicA.FirstOrDefault(a => a.Equals(s_delB)));

        var handlersTopicB = registry.GetTopicHandlers<object>("topic_a").ToArray();
        Assert.Equal(1, (int)handlersTopicB.Length);
        Assert.Same(s_delB, handlersTopicB[0]);
    }

    [Fact]
    public void HandlerRegistry_Return_Registration_Data()
    {
        var registry = new HandlerRegistry();
        var registration = registry.GetRegistration("some_id");
        Assert.Null(registration);

        var id = registry.Add<object>("topic_a", s_delA);
        registration = registry.GetRegistration(id);
        Assert.NotNull(registration);
        Assert.Same(s_delA, registration.Handler);
        Assert.Same(id, registration.Id);
        Assert.Same("topic_a", registration.Topic);
        Assert.Same(typeof(object), registration.HandledType);

        registration = registry.GetRegistration("some_id");
        Assert.Null(registration);
    }
}
