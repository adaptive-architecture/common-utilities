# Extension Methods

The Common.Utilities package provides several useful extension methods to enhance the .NET framework with commonly needed functionality.

## DateTime Extensions

Convert between DateTime objects and Unix timestamps with precision support.

### Unix Timestamp Conversion

Convert Unix timestamps (milliseconds or seconds) to DateTime objects:

```csharp
using AdaptArch.Common.Utilities.Extensions;

// From milliseconds
double timestampMs = 1640995200000; // 2022-01-01 00:00:00 UTC
DateTime dateFromMs = timestampMs.ToDateTimeFromUnixTimestampMilliseconds();

long timestampMsLong = 1640995200000L;
DateTime dateFromMsLong = timestampMsLong.ToDateTimeFromUnixTimestampMilliseconds();

// From seconds  
double timestampSec = 1640995200; // 2022-01-01 00:00:00 UTC
DateTime dateFromSec = timestampSec.ToDateTimeFromUnixTimestampSeconds();

ulong timestampSecUlong = 1640995200UL;
DateTime dateFromSecUlong = timestampSecUlong.ToDateTimeFromUnixTimestampSeconds();
```

## Dictionary Extensions

Enhanced dictionary operations with safer value retrieval and default value handling.

### Safe Value Retrieval

Get values from dictionaries with fallback defaults:

```csharp
using AdaptArch.Common.Utilities.Extensions;

var settings = new Dictionary<string, string>
{
    ["ApiUrl"] = "https://api.example.com",
    ["Timeout"] = "30"
};

// Get value or return default
string apiUrl = settings.GetValueOrDefault("ApiUrl", "https://localhost");
string missing = settings.GetValueOrDefault("MissingKey", "DefaultValue");

// Try get with default
bool found = settings.TryGetValueOrDefault("Timeout", "60", out string timeout);
```

### Factory-Based Defaults

Use factory methods to create default values only when needed:

```csharp
// Factory method called only if key is missing
var expensiveDefault = settings.GetValueOrDefault("CacheKey", () => 
{
    // This expensive operation only runs if key is missing
    return GenerateExpensiveDefault();
});

// Try get with factory
bool found = settings.TryGetValueOrDefault("ConfigPath", () => 
{
    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "myapp");
}, out string configPath);
```

## Task Extensions

Utilities for better async/await handling and fire-and-forget execution.

### Fire-and-Forget Execution

Execute tasks without awaiting and prevent UnobservedTaskException:

```csharp
using AdaptArch.Common.Utilities.Extensions;

// Fire-and-forget - no UnobservedTaskException
ProcessDataAsync().Forget();

// With explicit error handling
ProcessDataAsync().Forget(ex => 
{
    _logger.LogError(ex, "Background processing failed");
});

async Task ProcessDataAsync()
{
    await Task.Delay(1000);
    // Do work...
}
```

### Synchronous Execution of Async Code

Safely run async code synchronously when needed:

```csharp
// Run async method synchronously
var result = GetDataAsync().RunSync();

// With custom synchronization context
var resultWithContext = GetDataAsync().RunSync(customSyncContext);

async Task<string> GetDataAsync()
{
    await Task.Delay(100);
    return "Data";
}
```

> **Warning**: Use `RunSync()` sparingly and only when absolutely necessary, as it can lead to deadlocks in certain scenarios. Prefer async/await patterns when possible.

## JSON Extensions

Validate JSON strings before processing.

### JSON Validation

Check if a string contains valid JSON:

```csharp
using AdaptArch.Common.Utilities.Extensions;

string jsonString = """{"name": "John", "age": 30}""";
string invalidJson = "{name: 'John'}"; // Missing quotes around property name

bool isValid = jsonString.IsJson(); // Returns true
bool isInvalid = invalidJson.IsJson(); // Returns false

// Safe JSON processing
if (userInput.IsJson())
{
    var data = JsonSerializer.Deserialize<MyClass>(userInput);
    // Process data...
}
```

## Exception Extensions

Conditional exception throwing utilities for validation scenarios.

### Conditional Exception Throwing

Throw exceptions based on null checks:

```csharp
using AdaptArch.Common.Utilities.Extensions;

// Throw if value is null
string value = GetValue();
value.ThrowNotSupportedIfNull("Value cannot be null for this operation");

// Throw if value is NOT null
string? optionalValue = GetOptionalValue();
optionalValue.ThrowNotSupportedIfNotNull("Optional value must be null in this context");
```

These extensions are designed to reduce boilerplate code and provide safer, more expressive ways to handle common programming patterns in .NET applications.