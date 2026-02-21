# Encoding Utilities

The Common.Utilities package provides RFC-compliant encoding and decoding utilities for common encoding schemes used in web applications and APIs.

## Base32 Encoding

RFC 4648 compliant Base32 encoding with full support for padding and case-insensitive decoding.

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.Encoding;

// Encode data to Base32
byte[] data = Encoding.UTF8.GetBytes("Hello World!");
string encoded = Base32.Encode(data);
// Output: "JBSWY3DPEBLW64TMMQQQ===="

// Decode Base32 back to bytes
byte[] decoded = Base32.Decode(encoded);
string original = Encoding.UTF8.GetString(decoded);
// Output: "Hello World!"
```

### Advanced Base32 Operations

```csharp
// Case-insensitive decoding
string lowerCase = "jbswy3dpeblw64tmmqqq====";
byte[] decoded = Base32.Decode(lowerCase); // Works fine

// Subset encoding/decoding with offset and count
byte[] data = new byte[] { 72, 101, 108, 108, 111 }; // "Hello"
string partial = Base32.Encode(data, offset: 0, count: 3);

// Working with ReadOnlySpan<byte> for performance
ReadOnlySpan<byte> dataSpan = stackalloc byte[] { 72, 101, 108, 108, 111 }; // "Hello"
string encodedSpan = Base32.Encode(dataSpan);
```

### Use Cases for Base32

- **Case-insensitive environments**: Base32 is ideal when case sensitivity is an issue
- **Human-readable identifiers**: Better than Base64 for user-facing codes
- **URL-safe encoding**: Contains only alphanumeric characters and padding
- **QR codes and barcodes**: More reliable due to limited character set

## Base64Url Encoding

URL and filename safe Base64 encoding (RFC 4648 Section 5) that replaces URL-unsafe characters.

### Basic Usage

```csharp
using AdaptArch.Common.Utilities.Encoding;

// Encode data to Base64Url
byte[] data = Encoding.UTF8.GetBytes("Hello World!");
string encoded = Base64Url.Encode(data);
// Output: "SGVsbG8gV29ybGQh" (no padding, URL-safe)

// Decode Base64Url back to bytes
byte[] decoded = Base64Url.Decode(encoded);
string original = Encoding.UTF8.GetString(decoded);
// Output: "Hello World!"
```

### Base64Url vs Standard Base64

```csharp
string data = "Testing?/+";
byte[] bytes = Encoding.UTF8.GetBytes(data);

// Standard Base64 (contains URL-unsafe characters)
string standardB64 = Convert.ToBase64String(bytes);
// Output: "VGVzdGluZz8vKw==" (contains '/' and '+', has padding)

// Base64Url (URL-safe)
string urlSafeB64 = Base64Url.Encode(bytes);
// Output: "VGVzdGluZz8vKw" (no '/', '+' replaced, no padding)
```

### Use Cases for Base64Url

- **JWT tokens**: Industry standard for JSON Web Tokens
- **URL parameters**: Safe to use in query strings without encoding
- **Filenames**: Can be used in filenames across different file systems
- **REST APIs**: Safe for use in URL paths and parameters

## IEncoder Interface

Common abstraction for encoding operations, allowing for polymorphic encoding strategies.

### Implementing Custom Encoders

```csharp
using AdaptArch.Common.Utilities.Encoding;

// Use the built-in encoders through the interface
IEncoder base32Encoder = new Base32Encoder();
IEncoder base64UrlEncoder = new Base64UrlEncoder();

string data = "Hello World!";
byte[] bytes = Encoding.UTF8.GetBytes(data);

// Polymorphic encoding
string base32Result = base32Encoder.Encode(bytes);
string base64UrlResult = base64UrlEncoder.Encode(bytes);

// Polymorphic decoding
byte[] decodedBase32 = base32Encoder.Decode(base32Result);
byte[] decodedBase64Url = base64UrlEncoder.Decode(base64UrlResult);
```

### Custom Encoder Implementation

```csharp
public class HexEncoder : IEncoder
{
    public string Encode(byte[] input)
    {
        return Convert.ToHexString(input);
    }

    public byte[] Decode(string input)
    {
        return Convert.FromHexString(input);
    }
}

// Usage
IEncoder hexEncoder = new HexEncoder();
byte[] data = { 0xFF, 0xAB, 0x12 };
string hex = hexEncoder.Encode(data); // "FFAB12"
```

## Performance Considerations

### Span-Based Operations

All encoding utilities support `ReadOnlySpan<byte>` and `ReadOnlySpan<char>` for improved performance:

```csharp
// Avoid allocations with spans
Span<byte> buffer = stackalloc byte[1024];
ReadOnlySpan<byte> dataToEncode = buffer.Slice(0, actualLength);
string encoded = Base32.Encode(dataToEncode);
```

### Memory Efficiency

```csharp
// For large data sets, consider streaming approaches
public static string EncodeFile(string filePath)
{
    byte[] fileBytes = File.ReadAllBytes(filePath);
    return Base64Url.Encode(fileBytes);
}

// Better for large files - process in chunks
public static string EncodeFileChunked(string filePath)
{
    const int chunkSize = 8192;
    var chunks = new List<string>();
    
    using var fileStream = File.OpenRead(filePath);
    var buffer = new byte[chunkSize];
    int bytesRead;
    
    while ((bytesRead = fileStream.Read(buffer, 0, chunkSize)) > 0)
    {
        chunks.Add(Base64Url.Encode(buffer.AsSpan(0, bytesRead)));
    }
    
    return string.Join("", chunks);
}
```

## Best Practices

1. **Choose the right encoding**:
   - Use **Base32** for case-insensitive, human-readable scenarios
   - Use **Base64Url** for web applications, URLs, and JWT tokens
   - Use **standard Base64** only when compatibility with legacy systems is required

2. **Validate input**: Always validate encoded strings before decoding in production applications

3. **Handle exceptions**: Both encoders can throw exceptions on invalid input

4. **Use Span&lt;T&gt;**: Leverage span-based overloads for better performance with large datasets

5. **Consider context**: Base64Url is generally preferred in modern web applications due to its URL-safety

## Related Documentation

- [Extension Methods](extension-methods.md)