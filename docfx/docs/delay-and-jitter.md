# Delay and Jitter Generation

The Common.Utilities package provides sophisticated delay and jitter generation capabilities for implementing retry logic, backoff strategies, and preventing thundering herd problems in distributed systems.

## Overview

Delay and jitter generation is essential for:
- **Exponential backoff**: Gradually increasing delays between retry attempts
- **Avoiding thundering herd**: Preventing multiple clients from retrying simultaneously
- **Rate limiting**: Controlling the frequency of operations
- **Circuit breaker patterns**: Implementing recovery delays

## Delay Generation

### IDelayGenerator

The core interface for generating delay sequences:

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public interface IDelayGenerator
{
    TimeSpan GenerateDelay(int attempt);
}
```

### Delay Types

The package supports multiple delay strategies:

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public enum DelayType
{
    Constant,           // Fixed delay for each attempt
    Linear,             // Linearly increasing delay
    PowerOfTwo,         // Exponential backoff: 2^attempt
    PowerOfE            // Natural exponential backoff: e^attempt
}
```

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.Delay.Contracts;

// Create delay generator with exponential backoff
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOfTwo,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(5)
};

var delayGenerator = new DelayGenerator(options);

// Generate delays for retry attempts
for (int attempt = 0; attempt < 5; attempt++)
{
    TimeSpan delay = delayGenerator.GenerateDelay(attempt);
    Console.WriteLine($"Attempt {attempt}: delay {delay}");
}

// Output:
// Attempt 0: delay 00:00:01  (1 second)
// Attempt 1: delay 00:00:02  (2 seconds)
// Attempt 2: delay 00:00:04  (4 seconds)
// Attempt 3: delay 00:00:08  (8 seconds)
// Attempt 4: delay 00:00:16  (16 seconds)
```

### Delay Strategies

#### Constant Delay

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.Constant,
    BaseDelay = TimeSpan.FromSeconds(5)
};

// All attempts will have 5-second delays
```

#### Linear Delay

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.Linear,
    BaseDelay = TimeSpan.FromSeconds(2)
};

// Delays: 2s, 4s, 6s, 8s, 10s...
```

#### Exponential Backoff (Power of 2)

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOfTwo,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(10)
};

// Delays: 1s, 2s, 4s, 8s, 16s, 32s, 64s, 128s, 256s, 512s, 600s (capped)
```

#### Natural Exponential Backoff

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOfE,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(5)
};

// Delays: 1s, ~2.7s, ~7.4s, ~20.1s, ~54.6s, 300s (capped)
```

### DelayGeneratorOptions

Configure delay generation behavior:

```csharp
public class DelayGeneratorOptions
{
    public DelayType DelayType { get; set; } = DelayType.PowerOfTwo;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
}
```

## Jitter Generation

Jitter adds randomness to delays to prevent synchronized retry attempts from multiple clients.

### IJitterGenerator

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public interface IJitterGenerator
{
    TimeSpan ApplyJitter(TimeSpan delay);
}
```

### Basic Jitter Usage

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;

var jitterGenerator = new JitterGenerator();

TimeSpan baseDelay = TimeSpan.FromSeconds(10);
TimeSpan jitteredDelay = jitterGenerator.ApplyJitter(baseDelay);

// jitteredDelay will be between 5-15 seconds (±50% of base delay)
```

### Zero Jitter

For scenarios where you need deterministic delays:

```csharp
var zeroJitter = new ZeroJitterGenerator();
TimeSpan delay = zeroJitter.ApplyJitter(TimeSpan.FromSeconds(10));
// delay will always be exactly 10 seconds
```

## Combining Delay and Jitter

### Complete Retry Logic Example

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.Delay.Contracts;

public class RetryService
{
    private readonly IDelayGenerator _delayGenerator;
    private readonly IJitterGenerator _jitterGenerator;

    public RetryService(IDelayGenerator delayGenerator, IJitterGenerator jitterGenerator)
    {
        _delayGenerator = delayGenerator;
        _jitterGenerator = jitterGenerator;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation, 
        int maxAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        Exception lastException = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxAttempts - 1)
            {
                lastException = ex;
                
                // Calculate delay with jitter
                TimeSpan baseDelay = _delayGenerator.GenerateDelay(attempt);
                TimeSpan jitteredDelay = _jitterGenerator.ApplyJitter(baseDelay);
                
                await Task.Delay(jitteredDelay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("All retry attempts failed");
    }
}
```

### Usage Example

```csharp
// Configure delay and jitter
var delayOptions = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOfTwo,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(2)
};

var delayGenerator = new DelayGenerator(delayOptions);
var jitterGenerator = new JitterGenerator();
var retryService = new RetryService(delayGenerator, jitterGenerator);

// Use in your application
var result = await retryService.ExecuteWithRetryAsync(async () =>
{
    // This might fail and will be retried with exponential backoff + jitter
    return await httpClient.GetStringAsync("https://api.example.com/data");
}, maxAttempts: 5);
```

## Dependency Injection Setup

### Service Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.Delay.Contracts;

public void ConfigureServices(IServiceCollection services)
{
    // Configure delay options
    services.Configure<DelayGeneratorOptions>(options =>
    {
        options.DelayType = DelayType.PowerOfTwo;
        options.BaseDelay = TimeSpan.FromSeconds(1);
        options.MaxDelay = TimeSpan.FromMinutes(5);
    });

    // Register delay and jitter generators
    services.AddSingleton<IDelayGenerator, DelayGenerator>();
    services.AddSingleton<IJitterGenerator, JitterGenerator>();
    
    // Register retry service
    services.AddScoped<RetryService>();
}
```

### Configuration from Settings

```json
{
  "DelayGenerator": {
    "DelayType": "PowerOfTwo",
    "BaseDelay": "00:00:01",
    "MaxDelay": "00:05:00"
  }
}
```

```csharp
// In ConfigureServices
services.Configure<DelayGeneratorOptions>(
    configuration.GetSection("DelayGenerator"));
```

## Advanced Scenarios

### Custom Delay Strategies

```csharp
public class CustomDelayGenerator : IDelayGenerator
{
    public TimeSpan GenerateDelay(int attempt)
    {
        // Implement custom delay logic
        return TimeSpan.FromSeconds(Math.Min(attempt * attempt, 60));
    }
}
```

### Conditional Jitter

```csharp
public class ConditionalJitterGenerator : IJitterGenerator
{
    private readonly IJitterGenerator _jitterGenerator;
    private readonly IJitterGenerator _zeroJitterGenerator;

    public ConditionalJitterGenerator()
    {
        _jitterGenerator = new JitterGenerator();
        _zeroJitterGenerator = new ZeroJitterGenerator();
    }

    public TimeSpan ApplyJitter(TimeSpan delay)
    {
        // Only apply jitter for delays longer than 5 seconds
        if (delay > TimeSpan.FromSeconds(5))
        {
            return _jitterGenerator.ApplyJitter(delay);
        }
        
        return _zeroJitterGenerator.ApplyJitter(delay);
    }
}
```

### Circuit Breaker Integration

```csharp
public class CircuitBreakerService
{
    private readonly IDelayGenerator _delayGenerator;
    private DateTime _lastFailureTime;
    private int _failureCount;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        // Check if circuit is open
        if (_failureCount >= 5)
        {
            TimeSpan circuitOpenDelay = _delayGenerator.GenerateDelay(_failureCount - 5);
            
            if (DateTime.UtcNow - _lastFailureTime < circuitOpenDelay)
            {
                throw new InvalidOperationException("Circuit breaker is open");
            }
        }

        try
        {
            var result = await operation();
            _failureCount = 0; // Reset on success
            return result;
        }
        catch
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            throw;
        }
    }
}
```

## Best Practices

1. **Use exponential backoff**: PowerOfTwo strategy is ideal for most retry scenarios
2. **Always set max delay**: Prevent indefinitely long delays
3. **Add jitter**: Prevents thundering herd problems in distributed systems
4. **Consider the operation**: Different operations may need different delay strategies
5. **Monitor retry patterns**: Track retry attempts to identify systemic issues
6. **Respect cancellation tokens**: Always support cancellation in retry logic
7. **Log retry attempts**: Include attempt numbers and delays in logs for debugging

These utilities provide a robust foundation for implementing resilient retry logic and backoff strategies in your applications.