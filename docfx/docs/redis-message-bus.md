# Redis message bus

Implement pub/sub messaging patterns using Redis as the message broker for distributed communication between application components.

## Overview

Redis message bus enables you to:

- ✅ **Distribute messages** across multiple application instances
- ✅ **Decouple components** through async pub/sub patterns  
- ✅ **Scale horizontally** using Redis as a shared message broker
- ✅ **Support AoT compilation** with configurable serialization

## Service Registration

### Common Scenarios (Default Setup)
Configure Redis message bus using the parameterless constructor with `ReflectionJsonDataSerializer`:

``` csharp
// Minimal API example for common scenarios

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Uses ReflectionJsonDataSerializer by default (requires runtime reflection)
builder.Services.AddSingleton(new RedisMessageHubOptions());
```

### AoT Scenarios
Configure Redis message bus for AoT compilation using the constructor that accepts an `IDataSerializer` with `JsonDataSerializer`:

``` csharp
// AoT-compatible example

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));

// Configure JSON serialization for AoT compatibility
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.TypeInfoResolverChain.Add(MyAppJsonSerializerContext.Default);
jsonSerializerOptions.TypeInfoResolverChain.Add(DefaultJsonSerializerContext.Default);

var jsonSerializer = new JsonDataSerializer(
    new MyAppJsonSerializerContext(jsonSerializerOptions));

// Use JsonDataSerializer for AoT scenarios
builder.Services.AddSingleton(new RedisMessageHubOptions(jsonSerializer));

```

### Service Registration Options
Register the message hub service using your preferred approach:

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

builder.Services.AddSingleton<MyMessageHandler>();

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
- **AoT Compatible**: ❌ No (requires runtime reflection)
- **Performance**: Good for most use cases
- **Setup**: No additional configuration required

### JsonDataSerializer  
- **Usage**: AoT scenarios and high-performance applications
- **Constructor**: `new RedisMessageHubOptions(IDataSerializer dataSerializer)`
- **AoT Compatible**: ✅ Yes (with proper JsonSerializerContext)
- **Performance**: Optimized for AoT compilation
- **Setup**: Requires JsonSerializerContext configuration

## AoT Compatibility and Custom JsonSerializerContext

When using Native AoT compilation, the default JSON serialization behavior requires reflection which is not compatible with AoT. To support AoT scenarios, you need to provide a custom `JsonSerializerContext` that includes all the types you plan to serialize.

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

builder.Services.AddSingleton(new RedisMessageHubOptions(jsonSerializer));

builder.Services.AddSingleton<IMessageHubAsync, RedisMessageHub>();
```

### Complete AoT-Compatible Example

Here's a complete example showing AoT-compatible Redis message bus setup:

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

// Configure JSON serialization for AoT compatibility
var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.TypeInfoResolverChain.Add(MyAppJsonSerializerContext.Default);
jsonSerializerOptions.TypeInfoResolverChain.Add(DefaultJsonSerializerContext.Default);

var jsonSerializer = new JsonDataSerializer(
    new MyAppJsonSerializerContext(jsonSerializerOptions));

// Configure Redis message hub with custom serializer
builder.Services.AddSingleton(new RedisMessageHubOptions(jsonSerializer));

builder.Services.AddSingleton<IMessageHubAsync, RedisMessageHub>();
builder.Services.AddSingleton<MyMessageHandler>();

var app = builder.Build();
```

### Important Notes for AoT Compatibility

1. **Include All Message Types**: Every type you plan to serialize must be declared in your `JsonSerializerContext` with `[JsonSerializable]`.

2. **Include Wrapper Types**: Don't forget to include both the raw message type (e.g., `SayHello`) and the wrapped types (`Message<SayHello>`, `IMessage<SayHello>`).

3. **Chain TypeInfoResolvers**: Always chain your custom context with the `DefaultJsonSerializerContext` to ensure built-in types are supported.

4. **Project Configuration**: Add AoT properties to your project file:
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
    // Store the subscriptionId so we can later unsubscribe.
    subscriptionId = _messageHub.Subscribe<SayHello>(MessageName,
      (m, _) => HandleMessageAsync(m.Data, CancellationToken.None));
    return Task.CompletedTask;
  }

  public Task StopListeningAsync(CancellationToken cancellationToken)
  {
    // Use the subscription id to unsubscribe.
    _messageHub.Unsubscribe(subscriptionId);
    return Task.CompletedTask;
  }

  public Task PublishAsync(SayHello command, CancellationToken cancellationToken)
  {
    _messageHub.Publish(MessageName, command);
    return Task.CompletedTask;
  }

  private Task HandleMessageAsync(SayHello command, CancellationToken cancellationToken)
  {
    // You will have more advanced logic than this
    Console.WriteLine($"Welcome {command.Name}");
    return Task.CompletedTask;
  }
}
```

## Related Documentation

- [In-Process PubSub](in-process-pubsub.md)
- [Handler Discovery](handler-discovery.md)
