# xUnit Testing Utilities

The `AdaptArch.Common.Utilities.xUnit` package provides specialized testing utilities for xUnit test frameworks, designed to enhance test reliability and robustness.

## Features

### Retry Attributes

The package includes retry functionality for tests that may be flaky or depend on external resources. This helps improve test reliability by automatically retrying failed tests.

#### RetryFact Attribute

The `[RetryFact]` attribute works just like the standard `[Fact]` attribute, but automatically retries failed tests up to a specified number of times.

```csharp
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

public class MyTests
{
    [RetryFact(MaxRetries = 5)]
    public void MyFlakyTest()
    {
        // Test code that might occasionally fail
        // Will be retried up to 5 times if it fails
    }
    
    [RetryFact] // Uses default of 3 retries
    public void AnotherFlakyTest()
    {
        // Test code
    }
}
```

#### RetryTheory Attribute

The `[RetryTheory]` attribute works just like the standard `[Theory]` attribute, but automatically retries failed test cases.

```csharp
using AdaptArch.Common.Utilities.xUnit.Extensions.Retry;

public class MyTests
{
    [RetryTheory(MaxRetries = 3)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MyParameterizedTest(int value)
    {
        // Test code that might occasionally fail
        // Each test case will be retried up to 3 times if it fails
    }
}
```

## Configuration

### MaxRetries Property

Both `RetryFact` and `RetryTheory` attributes support the `MaxRetries` property:

- **Default Value**: 3 retries
- **Usage**: Set to any positive integer to control the number of retry attempts
- **Behavior**: The test will run initially, and if it fails, it will be retried up to the specified number of times

## Installation

Add the package reference to your test project:

```xml
<PackageReference Include="AdaptArch.Common.Utilities.xUnit" Version="[latest-version]" />
```

## Use Cases

The retry functionality is particularly useful for:

- **Integration tests** that depend on external services
- **Tests with timing dependencies** that may occasionally fail due to timing issues
- **Database tests** that might fail due to connection issues
- **Network-dependent tests** that could fail due to temporary network problems

## Implementation Details

The retry functionality is built on top of xUnit v3's extensibility framework and includes:

- Custom test case discoverers
- Specialized test case runners
- Message bus integration for proper test reporting
- Support for both synchronous and asynchronous test methods

## Best Practices

1. **Use sparingly**: Only apply retry attributes to tests that are genuinely flaky due to external dependencies
2. **Set appropriate retry counts**: Don't set excessively high retry counts as they can mask real issues
3. **Investigate failures**: If a test consistently requires retries, investigate the root cause
4. **Document usage**: Comment why a test needs retries to help future maintainers understand the reasoning

## Related Documentation

- [xUnit Documentation](https://xunit.net/)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)