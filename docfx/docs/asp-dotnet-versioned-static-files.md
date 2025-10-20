# ASP.NET Versioned Static Files Middleware

Serve versioned static files with automatic version detection and pluggable storage backends.

## Overview

The Versioned Static Files Middleware enables you to:

- ✅ **Serve versioned assets** automatically based on cookies or version files
- ✅ **Support multiple storage backends** through the provider pattern (disk, blob storage, etc.)
- ✅ **Handle version upgrades** seamlessly for client applications
- ✅ **Cache static files** efficiently with configurable cache durations
- ✅ **Notify clients** of new versions through refresh cookies

> **Note**: This middleware rewrites request paths to include version directories before passing them to the static files middleware. It should be placed before `UseStaticFiles()` in the pipeline.

## Basic Usage

### 1. Register the Middleware

Configure the middleware with default options (uses disk-based storage):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesPathPrefix = "/static/";
    options.StaticFilesDirectory = "wwwroot/static";
    options.VersionCookieNamePrefix = ".app_version_";
});

var app = builder.Build();

// Place before UseStaticFiles
app.UseVersionedStaticFiles();
app.UseStaticFiles();

app.Run();
```

### 2. Directory Structure

Organize your static files with version subdirectories:

```
wwwroot/
└── static/
    └── app/
        ├── version.json         # Current version metadata
        ├── v1.0.0/              # Version 1.0.0 assets
        │   ├── index.html
        │   ├── app.js
        │   └── styles.css
        └── v2.0.0/              # Version 2.0.0 assets
            ├── index.html
            ├── app.js
            └── styles.css
```

### 3. Version File Format

The `version.json` file specifies the current version:

```json
{
  "Version": "v2.0.0",
  "Timestamp": "2025-01-15T10:30:00Z"
}
```

### 4. How It Works

When a client requests `/static/app/index.html`:

1. Middleware checks for a version cookie (e.g., `.app_version_v_app`)
2. If no cookie exists, reads `version.json` to determine the current version
3. Rewrites the request path to `/static/app/v2.0.0/index.html`
4. Sets cookies to remember the version and notify of updates
5. Passes the request to the next middleware (typically `UseStaticFiles()`)

## Storage Providers

The middleware uses the **provider pattern** to abstract storage operations, allowing you to serve static assets from different backends.

### Built-in Providers

#### DiskStaticAssetsProvider

The default provider reads files from the local file system.

```csharp
services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesDirectory = "wwwroot/static";
});
// Uses DiskStaticAssetsProvider by default
```

### Custom Providers

Implement `IStaticAssetsProvider` to create custom storage backends (e.g., Azure Blob Storage, AWS S3).

#### 1. Implement the Interface

```csharp
public class BlobStaticAssetsProvider : IStaticAssetsProvider
{
    private readonly BlobServiceClient _blobClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStaticAssetsProvider> _logger;

    public BlobStaticAssetsProvider(
        BlobServiceClient blobClient,
        string containerName,
        ILogger<BlobStaticAssetsProvider> logger)
    {
        _blobClient = blobClient;
        _containerName = containerName;
        _logger = logger;
    }

    public async Task<VersionFilePayload?> ReadVersionFileAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient($"{targetDirectory}/version.json");

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content.ToString();

            return JsonSerializer.Deserialize<VersionFilePayload>(
                content,
                DefaultJsonSerializerContext.Default.VersionFilePayload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read version file from blob storage");
            return null;
        }
    }

    public async Task EnsureDirectoryExistsAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        // For blob storage, ensure container exists
        var containerClient = _blobClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task EnsureVersionDirectoryExistsAsync(
        string targetDirectory,
        string version,
        CancellationToken cancellationToken = default)
    {
        // For blob storage, check if version directory has any blobs
        var containerClient = _blobClient.GetBlobContainerClient(_containerName);
        var prefix = $"{targetDirectory}/{version}/";

        var blobs = containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken);
        await foreach (var blob in blobs)
        {
            // At least one blob exists with this prefix
            return;
        }

        _logger.LogWarning(
            "No blobs found for version directory: {TargetDirectory}/{Version}",
            targetDirectory,
            version);
    }
}
```

#### 2. Register the Custom Provider

```csharp
var blobClient = new BlobServiceClient(connectionString);
var customProvider = new BlobStaticAssetsProvider(
    blobClient,
    "static-assets",
    loggerFactory.CreateLogger<BlobStaticAssetsProvider>());

services.AddVersionedStaticFiles(
    options =>
    {
        options.StaticFilesPathPrefix = "/static/";
    },
    provider: customProvider  // Use custom provider instead of default
);
```

## Configuration Options

### MiddlewareOptions Properties

```csharp
services.AddVersionedStaticFiles(options =>
{
    // Path prefix to match (e.g., "/static/")
    options.StaticFilesPathPrefix = "/static/";

    // Base directory for static files (used by DiskStaticAssetsProvider)
    options.StaticFilesDirectory = "wwwroot/static";

    // Cookie name prefix for version tracking
    options.VersionCookieNamePrefix = ".app_version_";

    // Cookie expiration duration (default: 30 days)
    options.CookieExpiration = TimeSpan.FromDays(30);

    // Function to determine if configured version should be used
    options.UseConfiguredVersion = context => false;

    // Function to determine cache duration based on context
    options.UseCacheDuration = context => TimeSpan.FromHours(1);
});
```

### Advanced Configuration

#### Force Version Updates

Override the version cookie to always serve the latest version:

```csharp
options.UseConfiguredVersion = context =>
{
    // Force latest version for admin users
    return context.HttpContext.User.IsInRole("Admin");
};
```

#### Dynamic Cache Duration

Adjust caching based on the request context:

```csharp
options.UseCacheDuration = context =>
{
    // Long cache for production assets
    if (context.Directory == "app")
        return TimeSpan.FromHours(24);

    // Short cache for frequently updated assets
    if (context.Directory == "dashboard")
        return TimeSpan.FromMinutes(5);

    return TimeSpan.FromHours(1);
};
```

## Version Upgrade Flow

### Client-Side Integration

The middleware sets two cookies:

1. **Version Cookie** (`.app_version_v_{directory}`): Stores the current version being served
2. **Refresh Cookie** (`.app_version_r_{directory}`): Indicates if a newer version is available (`"1"` = yes, `"0"` = no)

Example client-side code to detect and handle version updates:

```javascript
function checkForUpdates() {
    const refreshCookie = getCookie('.app_version_r_app');

    if (refreshCookie === '1') {
        // Notify user of available update
        showUpdateNotification();
    }
}

function applyUpdate() {
    // Clear version cookie to get latest version
    document.cookie = '.app_version_v_app=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';

    // Reload the application
    window.location.reload();
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
}
```

## Complete Example

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Option 1: Use default disk-based provider
builder.Services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesPathPrefix = "/static/";
    options.StaticFilesDirectory = "wwwroot/static";
    options.VersionCookieNamePrefix = ".myapp_";
    options.CookieExpiration = TimeSpan.FromDays(30);
    options.UseCacheDuration = _ => TimeSpan.FromHours(12);
});

// Option 2: Use custom blob storage provider
// var blobProvider = new BlobStaticAssetsProvider(...);
// builder.Services.AddVersionedStaticFiles(options => { ... }, blobProvider);

var app = builder.Build();

// Must be before UseStaticFiles
app.UseVersionedStaticFiles();
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/static/app/index.html"));

app.Run();
```

### Directory Structure

```
wwwroot/
└── static/
    ├── app/
    │   ├── version.json
    │   ├── v1.0.0/
    │   │   └── index.html
    │   └── v2.0.0/
    │       └── index.html
    └── dashboard/
        ├── version.json
        └── v1.5.0/
            └── index.html
```

### version.json

```json
{
  "Version": "v2.0.0",
  "Timestamp": "2025-01-15T14:30:00Z"
}
```

## IStaticAssetsProvider Interface

The provider interface abstracts storage operations:

```csharp
public interface IStaticAssetsProvider
{
    /// <summary>
    /// Reads the version.json file from the specified target directory.
    /// </summary>
    Task<VersionFilePayload?> ReadVersionFileAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the target directory exists.
    /// For disk: checks/creates directory.
    /// For blob: may fetch version.json from remote storage.
    /// </summary>
    Task EnsureDirectoryExistsAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a specific version subdirectory exists.
    /// For disk: checks/creates directory.
    /// For blob: may fetch version assets from remote storage.
    /// </summary>
    Task EnsureVersionDirectoryExistsAsync(
        string targetDirectory,
        string version,
        CancellationToken cancellationToken = default);
}
```

## Best Practices

### 1. Version Naming

Use semantic versioning for clarity:

```
v1.0.0 - Major.Minor.Patch
v2.0.0-beta - Pre-release versions
v2.1.0 - Feature updates
```

### 2. Deployment Strategy

When deploying a new version:

1. Upload new version directory (e.g., `v2.0.0/`)
2. Test the new version thoroughly
3. Update `version.json` to point to the new version
4. Existing users continue on their cached version
5. New users or those who clear cookies get the new version

### 3. Cache Headers

The middleware automatically sets cache headers based on `UseCacheDuration`:

```
Cache-Control: public, max-age=43200
Expires: Wed, 15 Jan 2025 14:30:00 GMT
ETag: "app-v2.0.0"
```

### 4. Logging

The middleware logs important events at appropriate levels:

- **Debug**: Path rewriting, version resolution
- **Information**: Version changes, directory creation
- **Warning**: Missing version files
- **Error**: File read errors (in custom providers)

## Troubleshooting

### Issue: Static files not being served

**Solution**: Ensure `UseVersionedStaticFiles()` is placed **before** `UseStaticFiles()` in the pipeline.

```csharp
app.UseVersionedStaticFiles();  // Must come first
app.UseStaticFiles();
```

### Issue: Version not updating

**Solution**: Check that:
1. `version.json` has been updated with the new version
2. The new version directory exists and contains the files
3. The timestamp in `version.json` is newer than the client's cached version

### Issue: 404 errors for versioned paths

**Solution**: Verify that:
1. The version directory matches the value in `version.json`
2. File paths are correct (case-sensitive on Linux)
3. The `StaticFilesDirectory` option points to the correct base directory

### Issue: Custom provider not being used

**Solution**: Ensure you pass the provider to `AddVersionedStaticFiles()`:

```csharp
services.AddVersionedStaticFiles(options => { ... }, customProvider);
```

## See Also

- [ASP.NET Response Rewrite](asp-dotnet-response-rewrite.md)
- [Extension Methods](extension-methods.md)
- [API Documentation](/api/AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles.html)
