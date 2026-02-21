# Handler Discovery

Automatically discover and register message handlers using attribute-based registration to reduce boilerplate code and simplify dependency injection setup.

## Overview

Handler discovery enables you to:

- ✅ **Reduce boilerplate** by automatically registering message handlers
- ✅ **Use dependency injection** with scoped handler resolution
- ✅ **Support multiple topics** per handler method
- ✅ **Organize handlers** across multiple assemblies

## Basic Usage

Configure automatic handler discovery by following these steps:

1. **Define handler classes** with public methods that match the `MessageHandler` delegate signature
2. **Decorate handler methods** with `MessageHandlerAttribute` or custom attributes
3. **Register dependencies** and the `IMessageHubAsync` implementation
4. **Call `AddPubSubMessageHandlers`** to enable automatic discovery


### Service Registration

Configure the service container with handler discovery:

``` csharp
serviceCollection
  // Register any dependency your handler class has.
  .AddSingleton<HandlerDependency>()
  // Register you `IMessageHubAsync` implementation.
  // For the sample we are using the `InProcessMessageHub` implementation.
  .AddSingleton(new InProcessMessageHubOptions
  {
      MaxDegreeOfParallelism = Environment.ProcessorCount * 4
  })
  .AddSingleton<IMessageHubAsync, InProcessMessageHub>()
  // Wire-up the service host and discovery configuration.
  .AddPubSubMessageHandlers<MessageHandlerAttribute>(GetType().Assembly, att => att.Topic);
```

### Handler Implementation

Create handler classes with decorated methods:

``` csharp
public class MyHandler
{
  private readonly HandlerDependency _dependency;

  public MyHandler(HandlerDependency dependency)
  {
    _dependency = dependency;
  }

  [MessageHandler(topic: "my-topic")]
  public Task HandleAMessage(IMessage<object> message, CancellationToken cancellationToken)
  {
    // use the `_dependency` to implement your logic.
    return Task.CompletedTask;
  }
}
```

## Related Documentation

- [In-Process PubSub](in-process-pubsub.md)
- [Redis Message Bus](redis-message-bus.md)
- [Background Jobs](background-jobs.md)

