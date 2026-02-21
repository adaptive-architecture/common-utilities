# Global Abstractions

The Common.Utilities package provides abstractions for common global dependencies that are typically difficult to test, such as system time, random number generation, and UUID creation. These abstractions enable better testability and dependency injection practices.

## Overview

Global abstractions solve the problem of testing code that depends on:
- Current system time (`DateTime.UtcNow`)
- Random number generation (`Random`)
- Unique identifier generation (`Guid.NewGuid()`)

By abstracting these dependencies, you can easily mock them in unit tests and have better control over their behavior.

## DateTime Abstraction

### IDateTimeProvider

Provides abstraction over system time operations.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
```

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

public class OrderService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public OrderService(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public Order CreateOrder(string customerId, decimal amount)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Amount = amount,
            CreatedAt = _dateTimeProvider.UtcNow, // Testable!
            Status = OrderStatus.Pending
        };
    }
}

// Service registration
services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
```

### Testing with Mocks

The `DateTimeMockProvider` accepts an array of `DateTime` values and cycles through them on each access to `UtcNow`:

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

[Test]
public void CreateOrder_SetsCorrectTimestamp()
{
    // Arrange
    var fixedTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
    var mockProvider = new DateTimeMockProvider([fixedTime]);

    var orderService = new OrderService(mockProvider);

    // Act
    var order = orderService.CreateOrder("CUST001", 100.50m);

    // Assert
    Assert.AreEqual(fixedTime, order.CreatedAt);
}
```

## Random Number Generation

### IRandomGenerator

Provides abstraction over random number generation.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IRandomGenerator
{
    int Next(int minValue, int maxValue);
}
```

### Basic Usage

```csharp
public class GameService
{
    private readonly IRandomGenerator _randomGenerator;

    public GameService(IRandomGenerator randomGenerator)
    {
        _randomGenerator = randomGenerator;
    }

    public int RollDice()
    {
        return _randomGenerator.Next(1, 7); // 1-6 inclusive
    }
}

// Service registration — RandomGenerator requires a Random instance
services.AddSingleton<IRandomGenerator>(RandomGenerator.Instance);
```

### Deterministic Testing

```csharp
[Test]
public void RollDice_WithSeededRandom_ReturnsExpectedValue()
{
    // Arrange
    var random = new Random(42); // Seeded for deterministic results
    var randomGenerator = new RandomGenerator(random);
    var gameService = new GameService(randomGenerator);

    // Act
    var result = gameService.RollDice();

    // Assert — known result for seed 42
    Assert.That(result, Is.InRange(1, 6));
}
```

## UUID Generation

### IUuidProvider

Provides abstraction over GUID/UUID generation with formatting options.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IUuidProvider
{
    string New();
}
```

### Available Implementations

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

// Dashed format: "550e8400-e29b-41d4-a716-446655440000"
services.AddSingleton<IUuidProvider, DashedUuidProvider>();

// Undashed format: "550e8400e29b41d4a716446655440000"
services.AddSingleton<IUuidProvider, UnDashedUuidProvider>();
```

### Basic Usage

```csharp
public class DocumentService
{
    private readonly IUuidProvider _uuidProvider;

    public DocumentService(IUuidProvider uuidProvider)
    {
        _uuidProvider = uuidProvider;
    }

    public Document CreateDocument(string title, string content)
    {
        return new Document
        {
            Id = _uuidProvider.New(),
            Title = title,
            Content = content
        };
    }
}
```

### Testing with Mock UUIDs

The `UuidMockProvider` accepts an array of `Guid` values and cycles through them on each call to `New()`, returning the dashed format:

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

[Test]
public void CreateDocument_GeneratesCorrectId()
{
    // Arrange
    var expectedGuid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    var mockProvider = new UuidMockProvider([expectedGuid]);

    var documentService = new DocumentService(mockProvider);

    // Act
    var document = documentService.CreateDocument("Test", "Content");

    // Assert
    Assert.AreEqual("550e8400-e29b-41d4-a716-446655440000", document.Id);
}
```

## Mock Providers for Testing

All abstractions come with built-in mock implementations that extend `MockProvider<T>`. Mock providers accept an array of values via their constructor and cycle through them on each access.

### DateTimeMockProvider

```csharp
// Provide a sequence of times — UtcNow cycles through them
var mockProvider = new DateTimeMockProvider([
    new DateTime(2023, 6, 15, 12, 0, 0, DateTimeKind.Utc),
    new DateTime(2023, 6, 15, 13, 0, 0, DateTimeKind.Utc),
    new DateTime(2023, 6, 15, 14, 0, 0, DateTimeKind.Utc)
]);

var first = mockProvider.UtcNow;  // 12:00
var second = mockProvider.UtcNow; // 13:00
var third = mockProvider.UtcNow;  // 14:00
var fourth = mockProvider.UtcNow; // wraps back to 12:00
```

### UuidMockProvider

```csharp
// Provide a sequence of GUIDs — New() cycles through them
var mockProvider = new UuidMockProvider([
    Guid.Parse("00000000-0000-0000-0000-000000000001"),
    Guid.Parse("00000000-0000-0000-0000-000000000002")
]);

var first = mockProvider.New();  // "00000000-0000-0000-0000-000000000001"
var second = mockProvider.New(); // "00000000-0000-0000-0000-000000000002"
```

### Custom MockProvider&lt;T&gt;

For creating custom mock implementations that cycle through a collection:

```csharp
public class CustomMockProvider : MockProvider<string>
{
    public CustomMockProvider(string[] items) : base(items) { }

    public string NextValue() => GetNextValue();
}
```

## Dependency Injection Setup

### Complete Setup Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

public void ConfigureServices(IServiceCollection services)
{
    // Production implementations
    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    services.AddSingleton<IRandomGenerator>(RandomGenerator.Instance);
    services.AddSingleton<IUuidProvider, DashedUuidProvider>();

    // Your business services
    services.AddScoped<OrderService>();
    services.AddScoped<GameService>();
    services.AddScoped<DocumentService>();
}
```

### Test Setup Example

```csharp
public void ConfigureTestServices(IServiceCollection services)
{
    var fixedTime = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    services.AddSingleton<IDateTimeProvider>(new DateTimeMockProvider([fixedTime]));
    services.AddSingleton<IRandomGenerator>(new RandomGenerator(new Random(42))); // Seeded
    services.AddSingleton<IUuidProvider>(new UuidMockProvider([Guid.NewGuid()]));
}
```

## Best Practices

1. **Always use abstractions**: Avoid direct calls to `DateTime.UtcNow`, `Random`, or `Guid.NewGuid()` in business logic

2. **Register as singletons**: These providers are stateless and safe to use as singletons

3. **Seed random generators**: In tests, use seeded random generators for deterministic results

4. **Use UTC for storage**: Always use `UtcNow` for database storage and API responses

5. **Mock time in tests**: Use fixed times in tests to ensure consistent, predictable results

These abstractions provide a foundation for writing testable, maintainable code by removing dependencies on global system state.
