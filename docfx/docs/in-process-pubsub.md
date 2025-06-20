# In-Process Pub/Sub

The Common.Utilities package provides a comprehensive in-process publish-subscribe messaging system for decoupling components within your application and enabling event-driven architectures.

## Overview

The in-process pub/sub system enables:
- **Decoupled communication**: Components can communicate without direct references
- **Event-driven architectures**: React to events asynchronously
- **Testing support**: Mock message handling for unit tests
- **Scalable messaging**: Handle high-throughput scenarios with configurable parallelism
- **Null object pattern**: Disable messaging when needed

## Core Interfaces

### IMessageHub and IMessageHubAsync

The core interfaces for synchronous and asynchronous messaging:

```csharp
using AdaptArch.Common.Utilities.PubSub.Contracts;

// Synchronous interface
public interface IMessageHub
{
    string Subscribe<T>(string topic, MessageHandler<T> handler);
    void Publish<T>(string topic, T message);
    void Unsubscribe(string subscriptionId);
}

// Asynchronous interface
public interface IMessageHubAsync
{
    string Subscribe<T>(string topic, MessageHandler<T> handler);
    Task PublishAsync<T>(string topic, T message);
    void Unsubscribe(string subscriptionId);
}
```

### Message Handler Delegate

```csharp
public delegate Task MessageHandler<in T>(IMessage<T> message, CancellationToken cancellationToken);
```

## Basic Usage

### Setting Up the Message Hub

```csharp
using AdaptArch.Common.Utilities.PubSub.Implementations;
using Microsoft.Extensions.DependencyInjection;

// Configure services
services.AddSingleton(new InProcessMessageHubOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
});
services.AddSingleton<IMessageHubAsync, InProcessMessageHub>();
```

### Publishing Messages

```csharp
public class OrderService
{
    private readonly IMessageHubAsync _messageHub;

    public OrderService(IMessageHubAsync messageHub)
    {
        _messageHub = messageHub;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Process the order
        await ProcessOrderLogic(order);

        // Publish order processed event
        await _messageHub.PublishAsync("order.processed", new OrderProcessedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = order.Amount,
            ProcessedAt = DateTime.UtcNow
        });
    }
}

public class OrderProcessedEvent
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

### Subscribing to Messages

```csharp
public class EmailNotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly IMessageHubAsync _messageHub;
    private string _subscriptionId;

    public EmailNotificationService(IEmailSender emailSender, IMessageHubAsync messageHub)
    {
        _emailSender = emailSender;
        _messageHub = messageHub;
    }

    public void StartListening()
    {
        _subscriptionId = _messageHub.Subscribe<OrderProcessedEvent>("order.processed", HandleOrderProcessed);
    }

    public void StopListening()
    {
        if (_subscriptionId != null)
        {
            _messageHub.Unsubscribe(_subscriptionId);
            _subscriptionId = null;
        }
    }

    private async Task HandleOrderProcessed(IMessage<OrderProcessedEvent> message, CancellationToken cancellationToken)
    {
        var orderEvent = message.Data;
        
        await _emailSender.SendAsync(
            to: GetCustomerEmail(orderEvent.CustomerId),
            subject: "Order Processed",
            body: $"Your order {orderEvent.OrderId} has been processed successfully.",
            cancellationToken: cancellationToken);
    }
}
```

## Message Builder Pattern

Create messages with metadata and timestamps:

```csharp
using AdaptArch.Common.Utilities.PubSub.Implementations;

// Create a message with metadata
var message = MessageBuilder<OrderProcessedEvent>
    .Create()
    .WithData(orderEvent)
    .WithMetadata("source", "order-service")
    .WithMetadata("version", "1.0")
    .Build();

// Publish the built message
await _messageHub.PublishAsync("order.processed", message);
```

## Configuration Options

### InProcessMessageHubOptions

```csharp
public class InProcessMessageHubOptions
{
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
```

Configure parallelism for message processing:

```csharp
services.AddSingleton(new InProcessMessageHubOptions
{
    // Process up to 8 messages concurrently
    MaxDegreeOfParallelism = 8
});
```

## Multiple Subscribers

Multiple components can subscribe to the same topic:

```csharp
public class AuditService
{
    public void StartListening(IMessageHubAsync messageHub)
    {
        // Both EmailNotificationService and AuditService will receive the same messages
        messageHub.Subscribe<OrderProcessedEvent>("order.processed", HandleOrderProcessed);
    }

    private async Task HandleOrderProcessed(IMessage<OrderProcessedEvent> message, CancellationToken cancellationToken)
    {
        // Log the order processing event
        await LogOrderProcessingEvent(message.Data);
    }
}
```

## Error Handling

Handle exceptions in message handlers:

```csharp
private async Task HandleOrderProcessed(IMessage<OrderProcessedEvent> message, CancellationToken cancellationToken)
{
    try
    {
        await ProcessOrderEvent(message.Data);
    }
    catch (Exception ex)
    {
        // Log the exception
        _logger.LogError(ex, "Failed to process order event for order {OrderId}", message.Data.OrderId);
        
        // Optionally publish an error event
        await _messageHub.PublishAsync("order.processing.failed", new OrderProcessingFailedEvent
        {
            OrderId = message.Data.OrderId,
            Error = ex.Message,
            FailedAt = DateTime.UtcNow
        });
    }
}
```

## Null Object Pattern

Use `NullMessageHub` to disable messaging:

```csharp
using AdaptArch.Common.Utilities.PubSub.Implementations;

// For testing or when messaging should be disabled
services.AddSingleton(new NullMessageHubOptions());
services.AddSingleton<IMessageHubAsync, NullMessageHub>();
```

The `NullMessageHub` provides a no-op implementation that:
- Accepts subscriptions but doesn't store them
- Accepts published messages but doesn't process them
- Returns immediately from all operations

## Advanced Patterns

### Topic-Based Routing

```csharp
public class EventRouter
{
    private readonly IMessageHubAsync _messageHub;

    public EventRouter(IMessageHubAsync messageHub)
    {
        _messageHub = messageHub;
    }

    public void SetupRouting()
    {
        // Route different order events to different topics
        _messageHub.Subscribe<OrderCreatedEvent>("order.created", RouteOrderCreated);
        _messageHub.Subscribe<OrderUpdatedEvent>("order.updated", RouteOrderUpdated);
        _messageHub.Subscribe<OrderCancelledEvent>("order.cancelled", RouteOrderCancelled);
    }

    private async Task RouteOrderCreated(IMessage<OrderCreatedEvent> message, CancellationToken cancellationToken)
    {
        // Route to payment processing
        await _messageHub.PublishAsync("payment.process", new PaymentProcessRequest
        {
            OrderId = message.Data.OrderId,
            Amount = message.Data.Amount
        });

        // Route to inventory reservation
        await _messageHub.PublishAsync("inventory.reserve", new InventoryReservationRequest
        {
            OrderId = message.Data.OrderId,
            Items = message.Data.Items
        });
    }
}
```

### Message Filtering

```csharp
public class PriorityOrderHandler
{
    public void StartListening(IMessageHubAsync messageHub)
    {
        messageHub.Subscribe<OrderCreatedEvent>("order.created", HandleHighPriorityOrders);
    }

    private async Task HandleHighPriorityOrders(IMessage<OrderCreatedEvent> message, CancellationToken cancellationToken)
    {
        // Only process high-priority orders
        if (message.Data.Priority == OrderPriority.High)
        {
            await ProcessHighPriorityOrder(message.Data);
        }
    }
}
```

### Batch Processing

```csharp
public class BatchProcessor
{
    private readonly List<OrderProcessedEvent> _batch = new();
    private readonly Timer _flushTimer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public BatchProcessor(IMessageHubAsync messageHub)
    {
        messageHub.Subscribe<OrderProcessedEvent>("order.processed", HandleOrderProcessed);
        
        // Flush batch every 30 seconds
        _flushTimer = new Timer(FlushBatch, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task HandleOrderProcessed(IMessage<OrderProcessedEvent> message, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _batch.Add(message.Data);
            
            // Flush when batch is full
            if (_batch.Count >= 100)
            {
                await FlushBatchInternal();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void FlushBatch(object state)
    {
        await _semaphore.WaitAsync();
        try
        {
            await FlushBatchInternal();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task FlushBatchInternal()
    {
        if (_batch.Count > 0)
        {
            await ProcessBatch(_batch.ToList());
            _batch.Clear();
        }
    }
}
```

## Testing Support

### Unit Testing with Mocks

```csharp
[Test]
public async Task ProcessOrder_PublishesOrderProcessedEvent()
{
    // Arrange
    var mockMessageHub = new Mock<IMessageHubAsync>();
    var orderService = new OrderService(mockMessageHub.Object);
    var order = new Order { Id = "ORDER-123", CustomerId = "CUST-456", Amount = 100.50m };

    // Act
    await orderService.ProcessOrderAsync(order);

    // Assert
    mockMessageHub.Verify(hub => hub.PublishAsync("order.processed", It.IsAny<OrderProcessedEvent>()), Times.Once);
}
```

### Integration Testing

```csharp
[Test]
public async Task OrderProcessing_IntegrationTest()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddSingleton(new InProcessMessageHubOptions { MaxDegreeOfParallelism = 1 });
    services.AddSingleton<IMessageHubAsync, InProcessMessageHub>();
    services.AddScoped<OrderService>();
    services.AddScoped<EmailNotificationService>();

    var serviceProvider = services.BuildServiceProvider();
    var messageHub = serviceProvider.GetService<IMessageHubAsync>();
    var emailService = serviceProvider.GetService<EmailNotificationService>();

    // Set up message handling
    var messagesReceived = new List<OrderProcessedEvent>();
    messageHub.Subscribe<OrderProcessedEvent>("order.processed", async (message, ct) =>
    {
        messagesReceived.Add(message.Data);
    });

    // Act
    var orderService = serviceProvider.GetService<OrderService>();
    await orderService.ProcessOrderAsync(new Order { Id = "TEST-ORDER" });

    // Wait for async processing
    await Task.Delay(100);

    // Assert
    Assert.AreEqual(1, messagesReceived.Count);
    Assert.AreEqual("TEST-ORDER", messagesReceived[0].OrderId);
}
```

## Best Practices

1. **Use meaningful topic names**: Use hierarchical naming like "order.created", "user.registered"
2. **Handle exceptions**: Always wrap message handlers in try-catch blocks
3. **Avoid blocking operations**: Keep message handlers fast and non-blocking
4. **Unsubscribe properly**: Always unsubscribe when components are disposed
5. **Use dependency injection**: Register message hub as a singleton
6. **Consider message size**: Keep messages lightweight for better performance
7. **Monitor performance**: Track message processing times and queue sizes
8. **Use cancellation tokens**: Respect cancellation tokens in message handlers

The in-process pub/sub system provides a robust foundation for building event-driven applications while maintaining simplicity and testability.