# Redis message bus

For some simple `pub/sub` workload it might be enough to use a Redis based implementation of `IMessageHub` or `IMessageHubAsync`.

## Service registration
To get to run using Reid all you need to do is:
* Register a `IConnectionMultiplexer` instance with the dependency container.
* Register the `RedisMessageHubOptions` with the dependency container.
* Register the `RedisMessageHub` or `IMessageHubAsync` with the dependency container.

``` csharp
// Minimal API example.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();



builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton(new RedisMessageHubOptions {
  // DataSerializer = new CustomDataSerializer() -- You can override the default serializer (JsonDataSerializer) if you wish to.
});

// Preferred way.
builder.Services.AddSingleton<IMessageHubAsync, RedisMessageHub>();
// OR
builder.Services.AddSingleton<IMessageHub, RedisMessageHub>();
// OR
builder.Services
  .AddSingleton<RedisMessageHub>()
  .AddSingleton<IMessageHub>(svc => svc.GetService<RedisMessageHub>())
  .AddSingleton<IMessageHubAsync>(svc => svc.GetService<RedisMessageHub>())



services.AddSingleton<MyMessageHandler>();

// Start listening.
app.MapGet("/start-listening", static async (MyMessageHandler handler, CancellationToken token) => {
  await handler.StartListeningAsync(token);
  return Results.Ok();
});

// Stop listening.
app.MapGet("/stop-listening", static async (MyMessageHandler handler, CancellationToken token) => {
  await handler.StopListeningAsync(token);
  return Results.Ok();
});

// Publish messages.
app.MapGet("/publish", static async (MyMessageHandler handler, CancellationToken token) => {
  await handler.PublishAsync(new SayHello { Name = "Marco" }, token);
  return Results.Ok();
});

app.Run();
```

## Usage

To use the message hub in your business manager all you need to do is:
* Inject a reference to the message hub.
* Subscribe to the messages you are interested in.
* Publish the messages you need to.
* Optionally unsubscribe from the messages you are no longer interested in.


``` csharp
public class SayHello
{
  public string Name { get; set; }
}

public class MyMessageHandler
{
  private readonly IMessageHubAsync _messageHub;
  private const string MessageName = "WELCOME_GUESTS";
  private string subscriptionId;

  public MyMessageHandler(IMessageHubAsync messageHub)
  {
    _messageHub = messageHub;
  }

  public Task StartListeningAsync(CancellationToken cancellationToken)
  {
    // Store the subscriptionId so we can latter unsubscribe.
    subscriptionId = _messageHub.Subscribe<SayHello>(MessageName,
      (m, _) => HandleMessageAsync(m.Data, CancellationToken.None));
  }

  public Task StopListeningAsync(CancellationToken cancellationToken)
  {
    // Use the subscription id to unsubscribe.
    _messageHub.Unsubscribe(subscriptionId);
  }

  public Task PublishAsync(SayHello command, CancellationToken cancellationToken)
  {
    _messageHub.Publish(MessageName, command);
  }

  private Task HandleMessageAsync(SayHello command, CancellationToken cancellationToken)
  {
    // You will have more advanced logic than this
    Console.WriteLine($"Welcome {command.Name}");
    return Task.CompletedTask;
  }
}
```
