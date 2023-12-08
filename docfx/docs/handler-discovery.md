# Handler Discovery

In order to facilitate the registration and reduce the boilerplate needed the `AdaptArch.Common.Utilities.Hosting` package provides a set of helper methods.

## Usage

To use the new method all you need to do is:
- Define your handler class containing a ***public*** method that ***satisfies*** the contract of the `MessageHandler` delegate. In your class declaration you add whatever dependencies you need, these will be resolved in a ***scoped*** context.
- Decorate the handler method with the `MessageHandlerAttribute` marker attribute (or any other attribute). You can add the `MessageHandlerAttribute` multiple times to the same method to register the handler multiple time for multiple topic.
- In your service configuration register the necessary dependencies and the `IMessageHubAsync` implementation.
- Call the `AddPubSubMessageHandlers` method specifying the assembly containing yor message handler and a function to determine the messages to handle based on the marker attribute. You can call this method multiple times to add handlers from multiple assemblies or using different marker attributes.


``` csharp
// Example

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


// The class implementation

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

