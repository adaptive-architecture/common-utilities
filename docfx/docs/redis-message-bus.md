# Redis message bus

For some simple `pub/sub` workload it might be enough to use a Redis based implementation of `IMessageHub` or `IMessageHubAsync`.

## Service registration

### Common Scenarios (Default Setup)
For common scenarios, use the parameterless constructor which uses `ReflectionJsonDataSerializer`:

``` csharp
// Minimal API example for common scenarios

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Uses ReflectionJsonDataSerializer by default (requires runtime reflection)
builder.Services.AddSingleton(new RedisMessageHubOptions());
```

### AoT Scenarios
For AoT scenarios, use the constructor that accepts an `IDataSerializer` with `JsonDataSerializer`:

``` csharp
// AoT-compatible example

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Configure JSON serialization for AOT compatibility
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.TypeInfoResolverChain.Add(MyAppJsonSerializerContext.Default);
jsonSerializerOptions.TypeInfoResolverChain.Add(DefaultJsonSerializerContext.Default);

var jsonSerializer = new JsonDataSerializer(
    new MyAppJsonSerializerContext(jsonSerializerOptions));

// Use JsonDataSerializer for AoT scenarios
builder.Services.AddSingleton(new RedisMessageHubOptions(jsonSerializer));

```

### Service Registration Options
Regardless of which constructor you use, register the message hub service:

``` csharp
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

var app = builder.Build();

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

## Data Serializers

The Redis message bus supports two data serializers:

### ReflectionJsonDataSerializer (Default)
- **Usage**: Common scenarios with runtime reflection
- **Constructor**: `new RedisMessageHubOptions()` (parameterless)
- **AOT Compatible**: ❌ No (requires runtime reflection)
- **Performance**: Good for most use cases
- **Setup**: No additional configuration required

### JsonDataSerializer  
- **Usage**: AoT scenarios and high-performance applications
- **Constructor**: `new RedisMessageHubOptions(IDataSerializer dataSerializer)`
- **AOT Compatible**: ✅ Yes (with proper JsonSerializerContext)
- **Performance**: Optimized for AoT compilation
- **Setup**: Requires JsonSerializerContext configuration

## AOT Compatibility and Custom JsonSerializerContext

When using Native AOT compilation, the default JSON serialization behavior requires reflection which is not compatible with AOT. To support AOT scenarios, you need to provide a custom `JsonSerializerContext` that includes all the types you plan to serialize.

### Creating a Custom JsonSerializerContext

Create a partial class that inherits from `JsonSerializerContext` and declares all your message types:

``` csharp
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

// Include all your custom message types
[JsonSerializable(typeof(SayHello))]
[JsonSerializable(typeof(MyCustomMessage))]
// Include the generic message wrapper types
[JsonSerializable(typeof(Message<SayHello>))]
[JsonSerializable(typeof(IMessage<SayHello>))]
[JsonSerializable(typeof(Message<MyCustomMessage>))]
[JsonSerializable(typeof(IMessage<MyCustomMessage>))]
public partial class MyAppJsonSerializerContext : JsonSerializerContext;
```

### Configuring the JsonDataSerializer with Custom Context

Update your service registration to use the custom context:

``` csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Create JsonSerializerOptions with your custom context
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.TypeInfoResolverChain.Add(MyAppJsonSerializerContext.Default);
jsonSerializerOptions.TypeInfoResolverChain.Add(DefaultJsonSerializerContext.Default);

// Configure the JsonDataSerializer with the custom context
var jsonSerializer = new JsonDataSerializer(new MyAppJsonSerializerContext(jsonSerializerOptions));

builder.Services.AddSingleton(new RedisMessageHubOptions {
  DataSerializer = jsonSerializer
});

builder.Services.AddSingleton<IMessageHubAsync, RedisMessageHub>();
```

### Complete AOT-Compatible Example

Here's a complete example showing AOT-compatible Redis message bus setup:

``` csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;
using AdaptArch.Common.Utilities.Redis.PubSub;
using AdaptArch.Common.Utilities.Redis.Serialization.Implementations;
using StackExchange.Redis;

// Define your JsonSerializerContext with all message types
[JsonSerializable(typeof(SayHello))]
[JsonSerializable(typeof(Message<SayHello>))]
[JsonSerializable(typeof(IMessage<SayHello>))]
public partial class MyAppJsonSerializerContext : JsonSerializerContext;

var builder = WebApplication.CreateBuilder(args);

// Configure Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

// Configure JSON serialization for AOT compatibility
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.TypeInfoResolverChain.Add(MyAppJsonSerializerContext.Default);
jsonSerializerOptions.TypeInfoResolverChain.Add(DefaultJsonSerializerContext.Default);

var jsonSerializer = new JsonDataSerializer(
    new MyAppJsonSerializerContext(jsonSerializerOptions));

// Configure Redis message hub with custom serializer
builder.Services.AddSingleton(new RedisMessageHubOptions {
    DataSerializer = jsonSerializer
});

builder.Services.AddSingleton<IMessageHubAsync, RedisMessageHub>();
services.AddSingleton<MyMessageHandler>();

var app = builder.Build();
```

### Important Notes for AOT Compatibility

1. **Include All Message Types**: Every type you plan to serialize must be declared in your `JsonSerializerContext` with `[JsonSerializable]`.

2. **Include Wrapper Types**: Don't forget to include both the raw message type (e.g., `SayHello`) and the wrapped types (`Message<SayHello>`, `IMessage<SayHello>`).

3. **Chain TypeInfoResolvers**: Always chain your custom context with the `DefaultJsonSerializerContext` to ensure built-in types are supported.

4. **Project Configuration**: Add AOT properties to your project file:
   ``` xml
   <PropertyGroup>
     <PublishAot>true</PublishAot>
     <IsAotCompatible>true</IsAotCompatible>
   </PropertyGroup>
   ```

The `DefaultJsonSerializerContext` provided by the library includes support for common built-in types like `string`, `int`, `DateTime`, `Guid`, and the base message wrapper types with `object`.

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
