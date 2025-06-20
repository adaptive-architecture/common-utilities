# Global Abstractions

The Common.Utilities package provides abstractions for common global dependencies that are typically difficult to test, such as system time, random number generation, and UUID creation. These abstractions enable better testability and dependency injection practices.

## Overview

Global abstractions solve the problem of testing code that depends on:
- Current system time (`DateTime.Now`, `DateTime.UtcNow`)
- Random number generation (`Random`, `System.Security.Cryptography.RandomNumberGenerator`)
- Unique identifier generation (`Guid.NewGuid()`)

By abstracting these dependencies, you can easily mock them in unit tests and have better control over their behavior.

## DateTime Abstraction

### IDateTimeProvider

Provides abstraction over system time operations.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTimeOffset OffsetNow { get; }
    DateTimeOffset OffsetUtcNow { get; }
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

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

[Test]
public void CreateOrder_SetsCorrectTimestamp()
{
    // Arrange
    var fixedTime = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
    var mockProvider = new DateTimeMockProvider();
    mockProvider.SetUtcNow(fixedTime);
    
    var orderService = new OrderService(mockProvider);

    // Act
    var order = orderService.CreateOrder("CUST001", 100.50m);

    // Assert
    Assert.AreEqual(fixedTime, order.CreatedAt);
}
```

### TimeProvider Integration

For .NET 8+ applications, there's also a wrapper for the new `TimeProvider` class:

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

// Use with .NET 8+ TimeProvider
services.AddSingleton<TimeProvider>(TimeProvider.System);
services.AddSingleton<IDateTimeProvider, TimeProviderWrapper>();
```

## Random Number Generation

### IRandomGenerator

Provides abstraction over random number generation.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IRandomGenerator
{
    int Next();
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
    void NextBytes(byte[] buffer);
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

    public string GenerateSessionId()
    {
        var bytes = new byte[16];
        _randomGenerator.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// Service registration
services.AddSingleton<IRandomGenerator, RandomGenerator>();
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

    // Assert
    Assert.AreEqual(6, result); // Known result for seed 42
}
```

## UUID Generation

### IUuidProvider

Provides abstraction over GUID/UUID generation with formatting options.

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;

public interface IUuidProvider
{
    string GenerateUuid();
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
            Id = _uuidProvider.GenerateUuid(),
            Title = title,
            Content = content
        };
    }
}
```

### Testing with Mock UUIDs

```csharp
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations.Mocks;

[Test]
public void CreateDocument_GeneratesCorrectId()
{
    // Arrange
    var expectedId = "test-document-id-123";
    var mockProvider = new UuidMockProvider();
    mockProvider.SetNextUuid(expectedId);
    
    var documentService = new DocumentService(mockProvider);

    // Act
    var document = documentService.CreateDocument("Test", "Content");

    // Assert
    Assert.AreEqual(expectedId, document.Id);
}
```

## Mock Providers for Testing

All abstractions come with built-in mock implementations for testing scenarios.

### DateTimeMockProvider

```csharp
var mockProvider = new DateTimeMockProvider();

// Set specific times
mockProvider.SetNow(new DateTime(2023, 6, 15, 14, 30, 0));
mockProvider.SetUtcNow(new DateTime(2023, 6, 15, 12, 30, 0, DateTimeKind.Utc));

// Advance time during tests
mockProvider.AdvanceBy(TimeSpan.FromHours(1));
```

### UuidMockProvider

```csharp
var mockProvider = new UuidMockProvider();

// Set specific UUIDs
mockProvider.SetNextUuid("fixed-uuid-for-test");

// Use sequential UUIDs
mockProvider.SetSequentialUuids("uuid-1", "uuid-2", "uuid-3");
```

### Generic MockProvider&lt;T&gt;

For creating custom mock implementations:

```csharp
public class CustomMockProvider<T> : MockProvider<T>
{
    // Implement custom mocking logic
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
    services.AddSingleton<IRandomGenerator, RandomGenerator>();
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
    // Mock implementations for testing
    services.AddSingleton<IDateTimeProvider, DateTimeMockProvider>();
    services.AddSingleton<IRandomGenerator>(new RandomGenerator(new Random(42))); // Seeded
    services.AddSingleton<IUuidProvider, UuidMockProvider>();
}
```

## Best Practices

1. **Always use abstractions**: Avoid direct calls to `DateTime.Now`, `Random`, or `Guid.NewGuid()` in business logic

2. **Register as singletons**: These providers are stateless and safe to use as singletons

3. **Seed random generators**: In tests, use seeded random generators for deterministic results

4. **Use UTC for storage**: Always use `UtcNow` for database storage and API responses

5. **Mock time in tests**: Use fixed times in tests to ensure consistent, predictable results

6. **Consider time zones**: Use `DateTimeOffset` when working with user time zones

These abstractions provide a foundation for writing testable, maintainable code by removing dependencies on global system state.