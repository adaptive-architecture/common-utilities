# ASP.NET Extensionless Static Files Middleware

Serve clean URLs such as `/about` from `about.html`, while keeping the built-in static file
middleware responsible for serving the file.

## Overview

The `ExtensionlessStaticFilesMiddleware` maps a request path without an extension to an existing
file that has a configured extension:

```
/about          -> /about.html
/docs/intro     -> /docs/intro.html
/static/app/app -> /static/app/app.js   (if .js is configured)
```

The middleware only **rewrites the request path**. It does not open, read, or serve files. The
static files middleware that runs later in the pipeline still handles:

- Content type resolution (`Content-Type`)
- `Last-Modified`, `ETag`, `Accept-Ranges`, and `Content-Length`
- Conditional requests (`If-None-Match`, `If-Modified-Since`)
- Range requests (`206 Partial Content`)
- Response compression
- `HEAD` requests
- `OnPrepareResponse` caching configuration

This keeps behavior consistent with any other static file served by `UseStaticFiles()`.

> **Important:** this middleware does not replace `UseDefaultFiles()`. Directory indexes such as
> `/docs/` → `/docs/index.html` are still the job of the default files middleware.

## When to use it

| Case | Example | Recommended solution |
|---|---|---|
| Directory index | `/docs/` → `/docs/index.html` | `UseDefaultFiles()` |
| Clean URL for a file with an extension | `/about` → `/about.html` | **This middleware** |
| File on disk has no extension | `/CNAME`, `/apple-app-site-association` | Custom `IContentTypeProvider` |
| SPA route fallback | `/orders/42` → `/index.html` | `MapFallbackToFile()` |

Use the middleware when you want extensionless URLs for a static site or a set of static assets.
Do not use it to implement SPA routing or to serve files that genuinely have no extension.

## Basic usage

### 1. Register the options

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExtensionlessStaticFiles(options =>
{
    options.Extensions = [".html"];
    options.ShouldApply = context =>
        context.Request.Path.StartsWithSegments("/static");
});

var app = builder.Build();
```

### 2. Add the middleware before `UseStaticFiles`

```csharp
app.UseExtensionlessStaticFiles();
app.UseStaticFiles();
```

The middleware must run **before** the static files middleware. Otherwise the static files
middleware will already have passed the extensionless request to the next component.

### 3. Directory structure

With the configuration above:

```
wwwroot/
└── static/
    ├── about.html       # served at /static/about
    └── docs/
        └── intro.html   # served at /static/docs/intro
```

### 4. How it works

For a request to `/static/about`:

1. The middleware checks that the method is `GET` or `HEAD`.
2. It checks `ShouldApply`.
3. It strips `RequestPath` (empty by default) and verifies that the remaining path has no
   extension and does not end with `/`.
4. It checks whether an exact file exists at the request path. If it does, the request is left
   alone so the exact file wins.
5. It probes `path + extension` for each configured extension, in order.
6. When a candidate exists, the request path is rewritten to the candidate and the next
   middleware serves it. If `ShouldRedirect` returns `true`, the middleware responds with a
   redirect instead.

## Options

All options live on `ExtensionlessStaticFilesOptions`.

| Option | Default | Description |
|---|---|---|
| `RequestPath` | `PathString.Empty` | URL prefix that maps to the root of `FileProvider`. The prefix is removed before probing. A trailing slash is ignored. |
| `FileProvider` | `null` | File provider used to probe for candidates. `null` means `IWebHostEnvironment.WebRootFileProvider` (normally `wwwroot`). |
| `Extensions` | `[".html"]` | Ordered list of extensions to append. Each entry must start with `.` and must not contain path separators. |
| `ShouldApply` | `_ => true` | Predicate that decides whether a request should be processed. Use it to scope the middleware to a path prefix. |
| `ShouldRedirect` | `_ => false` | Predicate that decides whether a resolved candidate should be served by a redirect instead of an internal rewrite. |
| `RedirectStatusCode` | `301` | Status code used when `ShouldRedirect` returns `true`. Must be in the 3xx range. |

### `RequestPath`

`RequestPath` has the same meaning as `StaticFileOptions.RequestPath`: it maps the URL prefix to
the root of the file provider and is removed before `GetFileInfo` is called.

```csharp
options.RequestPath = "/static";
options.FileProvider = new PhysicalFileProvider(
    Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "static"));

// /static/about -> probes about.html under wwwroot/static
```

With the default web root provider, leave `RequestPath` empty and scope probing with `ShouldApply`:

```csharp
options.RequestPath = PathString.Empty;
options.FileProvider = null; // wwwroot
options.ShouldApply = context => context.Request.Path.StartsWithSegments("/static");

// /static/about -> probes static/about.html under wwwroot
```

Do **not** set `RequestPath = "/static"` while leaving the default web root provider unless your
files really live at `wwwroot/about.html`. The prefix is stripped first, so that configuration
probes `wwwroot/about.html`, not `wwwroot/static/about.html`.

### `FileProvider`

When `FileProvider` is `null`, the middleware uses
`IWebHostEnvironment.WebRootFileProvider`. This is the same default used by `UseStaticFiles()`.

If no web root is configured, the host supplies a `NullFileProvider`. In that case every probe
returns "not found" and the middleware becomes a no-op; it does not throw.

A `PhysicalFileProvider` is `IDisposable`. If you create one, create it once and dispose it with
the application:

```csharp
var staticProvider = new PhysicalFileProvider(staticRoot);

builder.Services.AddSingleton(staticProvider);
app.Lifetime.ApplicationStopped.Register(staticProvider.Dispose);
```

### `Extensions`

The list is ordered. The first candidate that exists wins.

```csharp
options.Extensions = [".html", ".htm"];
```

Each entry must start with `.` and must not contain `/` or `\`. Invalid entries fail at pipeline
construction with an `ArgumentException`.

The extension must also be known to the content type provider used by `UseStaticFiles()`. For
example, `.html` maps to `text/html`, `.json` to `application/json`, and so on. Unknown extensions
are not served unless the static files middleware enables `ServeUnknownFileTypes`, which Microsoft
recommends against.

### `ShouldApply`

`ShouldApply` runs before any file system probing. Use it to avoid probing API routes or to limit
the middleware to a specific prefix.

```csharp
options.ShouldApply = context =>
    context.Request.Path.StartsWithSegments("/static")
    && !context.Request.Path.StartsWithSegments("/static/api");
```

`StartsWithSegments` matches whole segments, so `/static` does not match `/static2`.

The predicate can be called concurrently and must be thread-safe.

### `ShouldRedirect`

`ShouldRedirect` is called only after a candidate file has been found. It receives an
`ExtensionlessRedirectContext`:

| Property | Description |
|---|---|
| `HttpContext` | The current request context. |
| `OriginalPath` | The path as seen by this middleware, after any earlier rewrite (for example the version segment added by the versioned static files middleware). |
| `CandidatePath` | The existing file path that was resolved, for example `/static/app/index.html`. |

Return `true` to redirect, `false` to rewrite internally.

```csharp
options.ShouldRedirect = context =>
    context.CandidatePath.Value!.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
    && context.HttpContext.Request.Headers["Sec-Fetch-Mode"] == "navigate";
```

When redirecting:

- The response status is `RedirectStatusCode` (default `301`).
- The `Location` header is absolute and includes `PathBase` and the original query string.
- The middleware short-circuits and does not call the next middleware.
- The predicate can be called concurrently and must be thread-safe.

Use a temporary redirect (`302` or `307`) when the file mapping can change at runtime. Use `301`
only when the URL mapping is permanent and cacheable.

## File provider configuration

The middleware must resolve the same physical files that the static files middleware serves.

### Option 1: default web root

```csharp
builder.Services.AddExtensionlessStaticFiles(options =>
{
    options.Extensions = [".html"];
    options.ShouldApply = context => context.Request.Path.StartsWithSegments("/static");
});

app.UseExtensionlessStaticFiles();
app.UseStaticFiles(); // default web root, no RequestPath
```

Probing `/static/about` checks `static/about.html` under `wwwroot`.

### Option 2: dedicated static root

```csharp
var staticRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "static");
var staticProvider = new PhysicalFileProvider(staticRoot);

builder.Services.AddSingleton(staticProvider);
app.Lifetime.ApplicationStopped.Register(staticProvider.Dispose);

builder.Services.AddExtensionlessStaticFiles(options =>
{
    options.RequestPath = "/static";
    options.FileProvider = staticProvider;
    options.Extensions = [".html"];
});

app.UseExtensionlessStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/static",
    FileProvider = staticProvider
});
```

Probing `/static/about` strips `/static` and checks `about.html` under `wwwroot/static`. Both
middlewares use the same provider instance, so they cannot drift.

### What about DI?

`IFileProvider` is not registered in DI by default, and `UseStaticFiles()` does not consume an
`IFileProvider` service. It uses `StaticFileOptions.FileProvider` or falls back to
`IWebHostEnvironment.WebRootFileProvider`.

`StaticFileOptions` is only available through DI when you call `UseStaticFiles()` **without**
arguments after configuring `services.Configure<StaticFileOptions>(...)`. If you call
`UseStaticFiles(new StaticFileOptions { ... })`, those options are passed directly to the
middleware and are not registered in DI. In that case, register the same options or provider
yourself if another component needs them.

## Using with VersionedStaticFilesMiddleware

The versioned middleware rewrites the request path to include a version directory. Run the
extensionless middleware **after** it so it can see the versioned path:

```csharp
builder.Services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesPathPrefix = "/static/";
    options.StaticFilesDirectory = "wwwroot/static";
});

builder.Services.AddExtensionlessStaticFiles(options =>
{
    options.Extensions = [".html"];
    options.ShouldApply = context => context.Request.Path.StartsWithSegments("/static");
});

app.UseVersionedStaticFiles();        // /static/app/index -> /static/app/v1.2.3/index
app.UseExtensionlessStaticFiles();    // .../index          -> .../index.html
app.UseStaticFiles();                 // serves the file
```

With this order:

- `/static/app/index` is versioned to `/static/app/v1.2.3/index`.
- The extensionless middleware resolves `/static/app/v1.2.3/index.html`.
- `UseStaticFiles()` serves the file with the correct content type and cache headers.

Putting the extensionless middleware first would probe the unversioned path, which normally does
not exist.

### Provider and directory layout

The versioned middleware only rewrites the URL. The extensionless middleware must be able to see the
versioned files through an `IFileProvider`, and both middlewares must resolve the same directory.

#### Default web root

The configuration above works when `StaticFilesDirectory` is inside `wwwroot`. The extensionless
middleware probes `WebRootFileProvider` for `static/app/v1.2.3/index.html`, and `UseStaticFiles()`
serves that same file.

#### Dedicated static directory

When the assets live outside `wwwroot`, share one `PhysicalFileProvider` between all three
middlewares:

```csharp
var staticRoot = Path.Combine(builder.Environment.ContentRootPath, "static");
var staticProvider = new PhysicalFileProvider(staticRoot);

builder.Services.AddSingleton(staticProvider);
builder.Services.AddVersionedStaticFiles(options =>
{
    options.StaticFilesPathPrefix = "/static/";
    options.StaticFilesDirectory = staticRoot; // version.json and v*/ live here
});

builder.Services.AddExtensionlessStaticFiles(options =>
{
    options.RequestPath = "/static";           // strip the public prefix
    options.FileProvider = staticProvider;     // probe the same root
    options.Extensions = [".html"];
});

var app = builder.Build();
app.Lifetime.ApplicationStopped.Register(staticProvider.Dispose);

app.UseVersionedStaticFiles();                 // /static/app/index
                                               //   -> /static/app/v1.2.3/index
app.UseExtensionlessStaticFiles();             //   -> /static/app/v1.2.3/index.html
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/static",
    FileProvider = staticProvider
});
```

The prefix and provider root must agree:

| `FileProvider` root | `RequestPath` | Request | Probes |
|---|---|---|---|
| `wwwroot` | empty | `/static/app/index` | `static/app/index.html` |
| `wwwroot/static` | `/static` | `/static/app/index` | `app/index.html` |

A mismatch silently turns the middleware into a no-op because the candidate is not found.

#### Custom `IStaticAssetsProvider`

`IStaticAssetsProvider` is used for version metadata and directory setup; it is **not** an
`IFileProvider`. If you use blob storage or another custom versioning provider, `UseStaticFiles()`
still needs an `IFileProvider` over the same store. Pass that provider to
`UseExtensionlessStaticFiles` as well. Without a matching `IFileProvider`, extension probing cannot
see the versioned files and the middleware falls through.

### Directory indexes

The extensionless middleware resolves **files**, not directories. A request for `/static/app/`
needs `UseDefaultFiles()` after versioning and before `UseStaticFiles()`:

```csharp
app.UseVersionedStaticFiles();
app.UseExtensionlessStaticFiles();
app.UseDefaultFiles();   // /static/app/ -> /static/app/vX.Y.Z/index.html
app.UseStaticFiles();
```

With that order, `/static/app/` becomes `/static/app/vX.Y.Z/` and default files resolves the index.

`/static/app` (without the trailing slash) does **not** work out of the box. Two things combine:

1. The versioned middleware only rewrites paths that have a segment after the target directory, so
   `/static/app` is left untouched.
2. `UseDefaultFiles()` only appends the trailing slash when the directory already contains a default
   file. The unversioned `static/app/` directory contains only `version.json`; `index.html` lives
   under `vX.Y.Z/`, so no redirect is issued and the request ends in `404`.

Use `/static/app/` (or `/static/app/index`), or add a small middleware before versioning that
appends the slash for directory requests:

```csharp
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (HttpMethods.IsGet(context.Request.Method)
        && path.StartsWithSegments("/static")
        && !path.Value!.EndsWith('/')
        && !Path.HasExtension(path.Value))
    {
        var webRoot = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .WebRootFileProvider;

        if (webRoot.GetDirectoryContents(path.Value.TrimStart('/')).Exists)
        {
            context.Request.Path = path + "/";
        }
    }

    await next();
});

app.UseVersionedStaticFiles();
```

With a dedicated provider, check that provider instead of `WebRootFileProvider`, and configure
`UseDefaultFiles` with the same root and prefix:

```csharp
app.UseDefaultFiles(new DefaultFilesOptions
{
    RequestPath = "/static",
    FileProvider = staticProvider
});
```

### Redirects and versioning

Do **not** redirect to `CandidatePath` when the versioned middleware is in the pipeline.
`CandidatePath` contains the version segment. A redirect to `/static/app/v1.2.3/index.html` is
processed by the versioned middleware again on the next request and becomes
`/static/app/v1.2.3/v1.2.3/index.html`.

For versioned assets, prefer the internal rewrite (`ShouldRedirect = _ => false`) and keep the
clean URL. If you need a redirect, redirect to the client-facing path plus the extension
(`/static/app/index.html`), which requires preserving the original pre-versioning path. The
current `VersionedStaticFilesMiddleware` does not expose that path, so a redirect in this
combination is not supported out of the box.

## Should there be a versioned extensionless middleware?

We evaluated three options.

### Option A: two independent middlewares (current)

`UseVersionedStaticFiles()` followed by `UseExtensionlessStaticFiles()`.

**Pros**
- Each middleware has one responsibility.
- The extensionless middleware works for unversioned assets too.
- The versioned middleware does not depend on URL-extension policy.
- The versioned middleware's public API and tests do not change.

**Cons**
- Ordering must be documented and tested.
- Two file system probes per request (version file and candidate file).
- Redirect semantics require care, as described above.

### Option B: a combined middleware

Add an option such as `ResolveExtensionlessPaths` to `VersionedStaticFilesMiddleware`, or create a
`VersionedExtensionlessStaticFilesMiddleware` that performs both rewrites.

**Pros**
- One pass, no ordering mistake.
- Could avoid probing the version file twice.

**Cons**
- Couples two independent concerns in one public API.
- Only works for versioned paths.
- Duplicates the extension/probe options or exposes them through the versioned options.
- Makes the versioned middleware harder to test and reason about.
- Still needs a policy for redirects and the original client-facing path.

### Option C: a convenience wrapper

A `UseVersionedExtensionlessStaticFiles()` method that only calls the two existing middleware
registrations in the correct order.

**Pros**
- Enforces ordering.
- Very small.

**Cons**
- Adds public API that hides two independently configurable middlewares.
- Still has to explain provider and prefix configuration.
- Saves one line of code.

### Decision

Keep the two middlewares separate. The extensionless middleware is a general URL policy; the
versioned middleware is a cache-busting strategy. Composing them is explicit, testable, and keeps
the versioned middleware small. A combined API would have to guess the provider and request path,
or duplicate configuration, and would not remove the redirect caveat.

Revisit this decision if consumers repeatedly get the order wrong or if profiling shows the extra
probe is significant. A convenience wrapper (Option C), not a combined middleware (Option B), is
the lowest-risk change at that point.

## Risks and limitations

### Duplicate URLs and SEO

`/about` and `/about.html` both return `200` when rewriting. Search engines can treat them as
duplicate content. Choose one of these strategies:

- Use internal rewrites and emit a canonical `<link>` in the HTML.
- Redirect `/about.html` to `/about` at the edge or in the application.
- Use redirect mode for `/about` and accept that the `.html` extension becomes visible.

The middleware does not manage canonical URLs.

### Additional URL surface

The middleware does not expose files that were not already reachable through `UseStaticFiles()` on
the same root, but it makes them reachable under additional URLs. Do not rely on exact-URL blocking
to protect private files. Keep private files outside every directory served by static files.

### Route conflicts

When `ShouldApply` returns `true` for API routes, an extensionless request such as `/api/status`
can be rewritten to `/api/status.html` before routing or endpoints run. Scope the middleware with
`ShouldApply`, and place it so it cannot shadow endpoints. In minimal hosting, routing may run
before user middleware; do not rely on the middleware to rewrite paths that endpoint routing has
already matched.

### Content type

The candidate extension must be mapped by the content type provider used by the static files
middleware. Otherwise the static files middleware passes the request through and the client gets a
404. Do not enable `ServeUnknownFileTypes` just to make this middleware work; configure the content
type mapping instead.

### Path traversal

The middleware passes relative paths to `IFileProvider.GetFileInfo`. `PhysicalFileProvider`
rejects paths that navigate above its root and returns a not-found file info. Do not replace the
provider with code that concatenates untrusted paths with `File.Exists` or `File.OpenRead`.

### Case sensitivity

Linux file systems are case-sensitive. `/About` probes `About.html`, not `about.html`. Match the
case of the files on disk or normalize at the edge, but be aware that normalization can change
URL semantics.

### Performance

Every extensionless candidate costs one `IFileProvider.GetFileInfo` call, which is usually a file
system stat for `PhysicalFileProvider`. Limit `Extensions` to the extensions you actually use,
scope with `ShouldApply`, and consider a startup manifest or a cache only if profiling shows a
problem. Do not cache negative results indefinitely; files can be deployed at runtime.

### `MapStaticAssets`

`MapStaticAssets` (.NET 9+) is endpoint- and manifest-based. Extensionless URLs are not part of the
build-time manifest. This middleware is designed to work with `UseStaticFiles()`. If you use
`MapStaticAssets`, keep `UseStaticFiles()` for the extensionless paths, or do not use this
middleware.

### `NullFileProvider` and missing web roots

If no web root is configured, `WebRootFileProvider` is a `NullFileProvider`. The middleware
becomes a no-op and requests fall through to the next component. It does not throw and does not
create directories.

### Exact file precedence

If a file exists at the exact request path (including a file without an extension), the middleware
does not probe candidates. This avoids shadowing real files. If that exact file has no known
content type, the static files middleware may still return a 404; use a custom
`IContentTypeProvider` for those files.

### Methods

Only `GET` and `HEAD` are processed. Other methods pass through unchanged. If you need `OPTIONS`
or `POST` handling, do it in an endpoint, not here.

### Query strings and `PathBase`

The query string is never modified. Redirects include the original query string and `PathBase`.
Internal rewrites only change `Request.Path`, so `Request.PathBase` and `Request.QueryString`
remain intact.

### AOT and trimming

The middleware uses no reflection or dynamic code. It is compatible with the repository's
`IsAotCompatible` setting and with trimming.

### Delegate thread safety

`ShouldApply` and `ShouldRedirect` can run concurrently for different requests. They must be
thread-safe and should not mutate shared state without synchronization.

### Configuration validation

Invalid extensions and redirect status codes fail when the middleware is constructed, which
normally happens during application startup. This is intentional: an invalid configuration should
not silently produce 404s.

## Troubleshooting

### The file exists but the request returns 404

Check, in order:

1. Is `UseExtensionlessStaticFiles()` before `UseStaticFiles()`?
2. Does `ShouldApply` return `true` for the path?
3. Does `RequestPath` match the URL prefix and the file provider root?
4. Does the extension start with `.`?
5. Is the last path segment free of dots? A path such as `/v1.2` is treated as having an extension.
6. Does the exact path exist as a file and therefore take precedence?
7. Is the candidate extension mapped to a content type by `UseStaticFiles()`?

### The wrong file is served

Check the order of `Extensions`. The first existing candidate wins. Also check whether a file exists
at the exact request path.

### Redirect loop or doubled version segment

You are redirecting to a `CandidatePath` that already contains a version segment. Use internal
rewrites with `VersionedStaticFilesMiddleware`, or preserve the original client-facing path and
redirect to that path plus the extension.

### API routes stopped working

`ShouldApply` is too broad. Scope it to the static file prefix, or move the middleware so it cannot
intercept endpoint requests.

### The middleware does nothing

`FileProvider` is `null` and the application has no web root, so `WebRootFileProvider` is a
`NullFileProvider`. Configure a web root or pass a `PhysicalFileProvider`.

### Configuration exception at startup

- `Extensions must start with a '.' and must not contain path separators.` Fix the extension list.
- `The redirect status code must be in the 3xx range.` Use `301`, `302`, `307`, or `308`.

## See Also

- [ASP.NET Versioned Static Files](asp-dotnet-versioned-static-files.md)
- [ASP.NET Response Rewrite](asp-dotnet-response-rewrite.md)
- [Extension Methods](extension-methods.md)
- [API Documentation](/api/AdaptArch.Common.Utilities.AspNetCore.Middlewares.ExtensionlessStaticFiles.html)
