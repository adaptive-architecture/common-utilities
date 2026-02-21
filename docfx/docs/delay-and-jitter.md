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

The core interface for generating delay sequences. It returns an enumerable of `TimeSpan` values:

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public interface IDelayGenerator
{
    IEnumerable<TimeSpan> GetDelays();
}
```

### Delay Types

The package supports multiple delay strategies:

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public enum DelayType
{
    Unknown = 0,        // Unknown type
    Constant = 1,       // Fixed delay for each iteration
    Linear = 2,         // Linearly increasing delay
    PowerOf2 = 3,       // Exponential backoff: iteration^2
    PowerOfE = 4        // Natural exponential backoff: iteration^e
}
```

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.Delay.Contracts;

// Create delay generator with exponential backoff
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOf2,
    DelayInterval = TimeSpan.FromSeconds(1),
    MaxIterations = 5
};

var delayGenerator = new DelayGenerator(options);

// Enumerate the generated delays
foreach (var delay in delayGenerator.GetDelays())
{
    Console.WriteLine($"Delay: {delay}");
    await Task.Delay(delay);
}
```

### Delay Strategies

#### Constant Delay

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.Constant,
    DelayInterval = TimeSpan.FromSeconds(5),
    MaxIterations = 3
};

// All iterations will have 5-second delays
```

#### Linear Delay

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.Linear,
    DelayInterval = TimeSpan.FromSeconds(2),
    MaxIterations = 5
};

// Delays scale linearly: 0*2s, 1*2s, 2*2s, 3*2s, 4*2s
```

#### Exponential Backoff (Power of 2)

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOf2,
    DelayInterval = TimeSpan.FromSeconds(1),
    MaxIterations = 5
};

// Delays: 0^2*1s, 1^2*1s, 2^2*1s, 3^2*1s, 4^2*1s = 0s, 1s, 4s, 9s, 16s
```

#### Natural Exponential Backoff

```csharp
var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOfE,
    DelayInterval = TimeSpan.FromSeconds(1),
    MaxIterations = 5
};

// Delays increase following iteration^e pattern
```

### DelayGeneratorOptions

Configure delay generation behavior:

```csharp
public class DelayGeneratorOptions
{
    // Maximum number of iterations to generate delays (default: 5)
    public int MaxIterations { get; set; } = 5;

    // Starting iteration index (default: 0)
    public int Current { get; set; } = 0;

    // Base interval for delay calculation
    public TimeSpan DelayInterval { get; set; } = TimeSpan.Zero;

    // Type of delay progression
    public DelayType DelayType { get; set; } = DelayType.Constant;

    // Jitter generator (default: ZeroJitterGenerator)
    public IJitterGenerator JitterGenerator { get; set; } = ZeroJitterGenerator.Instance;

    // Lower boundary for jitter percentage [0, 1] (default: 0.02)
    public float JitterLowerBoundary { get; set; } = 0.02f;

    // Upper boundary for jitter percentage [0, 1] (default: 0.27)
    public float JitterUpperBoundary { get; set; } = 0.27f;
}
```

## Jitter Generation

Jitter adds randomness to delays to prevent synchronized retry attempts from multiple clients.

### IJitterGenerator

```csharp
using AdaptArch.Common.Utilities.Delay.Contracts;

public interface IJitterGenerator
{
    TimeSpan New(TimeSpan baseValue, float lowerBoundary, float upperBoundary);
}
```

The jitter generator returns a `TimeSpan` that represents a positive or negative offset. The offset is a percentage of the `baseValue`, where the percentage is randomly chosen between `lowerBoundary` and `upperBoundary` (values between 0 and 1).

### Basic Jitter Usage

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

var jitterGenerator = new JitterGenerator(RandomGenerator.Instance);

TimeSpan baseDelay = TimeSpan.FromSeconds(10);
TimeSpan jitter = jitterGenerator.New(baseDelay, lowerBoundary: 0.05f, upperBoundary: 0.25f);

// jitter will be ±5-25% of baseDelay
TimeSpan jitteredDelay = baseDelay + jitter;
```

### Zero Jitter

For scenarios where you need deterministic delays:

```csharp
var zeroJitter = ZeroJitterGenerator.Instance;
TimeSpan jitter = zeroJitter.New(TimeSpan.FromSeconds(10), 0f, 1f);
// jitter will always be TimeSpan.Zero
```

## Combining Delay and Jitter

The `DelayGeneratorOptions` integrates jitter directly. When a `JitterGenerator` is set on the options, each delay from `GetDelays()` automatically includes jitter:

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

var options = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOf2,
    DelayInterval = TimeSpan.FromSeconds(1),
    MaxIterations = 5,
    JitterGenerator = new JitterGenerator(RandomGenerator.Instance),
    JitterLowerBoundary = 0.05f,
    JitterUpperBoundary = 0.25f
};

var delayGenerator = new DelayGenerator(options);

// Each delay includes jitter automatically
foreach (var delay in delayGenerator.GetDelays())
{
    await Task.Delay(delay);
}
```

### Complete Retry Logic Example

```csharp
using AdaptArch.Common.Utilities.Delay.Implementations;
using AdaptArch.Common.Utilities.Delay.Contracts;
using AdaptArch.Common.Utilities.GlobalAbstractions.Implementations;

public class RetryService
{
    private readonly DelayGeneratorOptions _delayOptions;

    public RetryService(DelayGeneratorOptions delayOptions)
    {
        _delayOptions = delayOptions;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        var delayGenerator = new DelayGenerator(_delayOptions);

        // First attempt without delay
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            lastException = ex;
        }

        // Retry with generated delays
        foreach (var delay in delayGenerator.GetDelays())
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw lastException ?? new InvalidOperationException("All retry attempts failed");
    }
}
```

### Usage Example

```csharp
// Configure delay with jitter
var delayOptions = new DelayGeneratorOptions
{
    DelayType = DelayType.PowerOf2,
    DelayInterval = TimeSpan.FromSeconds(1),
    MaxIterations = 5,
    JitterGenerator = new JitterGenerator(RandomGenerator.Instance)
};

var retryService = new RetryService(delayOptions);

// Use in your application
var result = await retryService.ExecuteWithRetryAsync(async () =>
{
    // This might fail and will be retried with exponential backoff + jitter
    return await httpClient.GetStringAsync("https://api.example.com/data");
});
```

## Advanced Scenarios

### Custom Delay Strategies

```csharp
public class CustomDelayGenerator : IDelayGenerator
{
    public IEnumerable<TimeSpan> GetDelays()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return TimeSpan.FromSeconds(Math.Min(i * i, 60));
        }
    }
}
```

## Best Practices

1. **Use exponential backoff**: `PowerOf2` strategy is ideal for most retry scenarios
2. **Add jitter**: Set a `JitterGenerator` on options to prevent thundering herd problems
3. **Limit iterations**: Set `MaxIterations` to prevent indefinitely long retry loops
4. **Consider the operation**: Different operations may need different delay strategies
5. **Monitor retry patterns**: Track retry attempts to identify systemic issues
6. **Respect cancellation tokens**: Always support cancellation in retry logic
7. **Log retry attempts**: Include attempt numbers and delays in logs for debugging

These utilities provide a robust foundation for implementing resilient retry logic and backoff strategies in your applications.

## Related Documentation

- [Background Jobs](background-jobs.md)
- [Leader Election](leader-election.md)
