---
_layout: landing
---

# Common Utilities

A comprehensive collection of .NET utilities and extensions designed to reduce boilerplate code and accelerate development. These packages provide solutions for common cross-cutting concerns in modern .NET applications.

## Package Overview

### AdaptArch.Common.Utilities
**Core utilities package** - Dependency-free foundation providing:
- **Extension Methods**: Enhanced DateTime, Dictionary, Task, JSON, and Exception utilities
- **Encoding**: RFC-compliant Base32 and Base64Url encoding
- **Consistent Hashing**: Distributed key-to-server mapping with minimal redistribution
- **Global Abstractions**: Testable wrappers for DateTime, Random, and UUID generation
- **Delay & Jitter**: Exponential backoff and retry strategies
- **PubSub System**: In-process publish-subscribe messaging
- **Synchronization**: Thread-safe exclusive access utilities
- **Job Contracts**: Background job execution interfaces

### AdaptArch.Common.Utilities.Configuration
**Configuration extensions** for `Microsoft.Extensions.Configuration`:
- **Custom Configuration Providers**: Integrate any data source as configuration
- **Dynamic Reloading**: Automatic configuration updates with polling
- **JSON Parsing**: Handle complex nested configuration formats
- **Error Handling**: Robust exception handling with retry policies

### AdaptArch.Common.Utilities.Hosting
**Hosting extensions** for `Microsoft.Extensions.Hosting`:
- **Background Jobs**: Periodic and delayed job execution
- **Message Handler Discovery**: Automatic registration of pub/sub handlers
- **Dependency Injection**: Enhanced service scope management
- **Configuration-Driven**: JSON-based job scheduling and configuration

### AdaptArch.Common.Utilities.AspNetCore
**ASP.NET Core extensions**:
- **Response Rewriting**: Middleware for transforming HTTP responses
- **HttpContext Extensions**: Utility methods for request processing
- **Performance Optimized**: Stream-based processing without buffering

### AdaptArch.Common.Utilities.Redis
**Redis integrations** built on `StackExchange.Redis`:
- **Distributed PubSub**: Redis-backed message hub implementation
- **Serialization**: Pluggable JSON and custom serialization
- **Connection Management**: Simplified Redis connection setup

## Key Benefits

✅ **Testability**: All time, random, and external dependencies are abstracted  
✅ **Performance**: Optimized with Span&lt;T&gt; and minimal allocations  
✅ **Reliability**: Built-in error handling, timeouts, and retry mechanisms  
✅ **Flexibility**: Multiple implementations for different scenarios  
✅ **Standards Compliance**: RFC-compliant encoding and industry best practices  
✅ **Thread Safety**: Safe concurrent operations with proper synchronization  
✅ **Dependency Injection Ready**: Full integration with Microsoft.Extensions.DependencyInjection  
✅ **Production Ready**: Includes logging, proper disposal, and resource management  

## Quick Start

```bash
dotnet add package AdaptArch.Common.Utilities
```

```csharp
// Basic usage examples
using AdaptArch.Common.Utilities.Extensions;
using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.ConsistentHashing;

// Extension methods
var timestamp = DateTime.UtcNow.ToUnixTimeMilliseconds();
var config = settings.GetValueOrDefault("ApiUrl", key => "https://localhost");
ProcessDataAsync().Forget(); // Fire-and-forget

// Consistent hashing for load balancing
var ring = new HashRing<string>();
ring.Add("server1.example.com");
ring.Add("server2.example.com");
ring.CreateConfigurationSnapshot();
string server = ring.GetServer("user-12345"); // Always routes to same server

// Testable abstractions
public class OrderService
{
    public OrderService(IDateTimeProvider dateTime, IMessageHubAsync messageHub) { }
}

// PubSub messaging
await messageHub.PublishAsync("order.created", orderEvent, CancellationToken.None);
await messageHub.SubscribeAsync<OrderEvent>("order.created", HandleOrder, CancellationToken.None);
```

## Getting Started

1. **Install packages** based on your needs
2. **Register services** in your DI container
3. **Explore the documentation** for detailed usage examples
4. **Check the API reference** for complete method signatures

These utilities form the foundation for building robust, maintainable, and testable .NET applications.
