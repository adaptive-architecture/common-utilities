# ADR-0002: Extensionless static files middleware

- **Status:** Accepted
- **Date:** 2026-11-08
- **Scope:** `AdaptArch.Common.Utilities.AspNetCore`
- **Decision owners:** Maintainers

## Context

Static sites and documentation bundles are often deployed as `name.html` files, but the desired
public URLs are extensionless: `/about` rather than `/about.html`. ASP.NET Core's
`StaticFileMiddleware` has no built-in extension probing. `UseDefaultFiles()` only maps directory
requests (`/docs/` → `/docs/index.html`); it does not map `/about` → `/about.html`.

Applications also increasingly compose static serving with
[`VersionedStaticFilesMiddleware`](../../docfx/docs/asp-dotnet-versioned-static-files.md), which rewrites
`/static/app/index` to `/static/app/<version>/index`. An extensionless solution must work both on
its own and after that rewrite.

We needed to decide:

1. Whether to add an extensionless middleware at all, and if so, what it should own.
2. Whether to ship a separate **versioned** extensionless middleware (or an option on the versioned
   middleware) in addition to the general middleware.
3. How redirect vs. internal-rewrite behavior should be configured.

## Decision

Add a standalone `ExtensionlessStaticFilesMiddleware` to `AdaptArch.Common.Utilities.AspNetCore`
that **only rewrites the request path** when a candidate file exists. Serving, content-type
resolution, conditional requests, ranges, compression, and `HEAD` remain the responsibility of the
static files middleware that runs later.

The middleware is configured through `ExtensionlessStaticFilesOptions`:

| Option | Default | Purpose |
|---|---|---|
| `RequestPath` | `PathString.Empty` | Prefix stripped before `IFileProvider.GetFileInfo`, mirroring `StaticFileOptions.RequestPath`. |
| `FileProvider` | `null` | Probe provider; `null` resolves to `IWebHostEnvironment.WebRootFileProvider`. |
| `Extensions` | `[".html"]` | Ordered candidate extensions. Entries must start with `.`. |
| `ShouldApply` | `_ => true` | Predicate that scopes processing to a path/prefix. |
| `ShouldRedirect` | `_ => false` | Predicate over `ExtensionlessRedirectContext` (`HttpContext`, `OriginalPath`, `CandidatePath`) that chooses redirect vs. rewrite. |
| `RedirectStatusCode` | `301` | Status used when `ShouldRedirect` returns `true`; must be 3xx. |

We will **not** ship a combined or versioned extensionless middleware. Consumers compose the two
existing middlewares in this order:

```csharp
app.UseVersionedStaticFiles();      // adds the version segment
app.UseExtensionlessStaticFiles();  // appends the extension
app.UseStaticFiles();               // serves the file
```

`ShouldRedirect` is a predicate, not a boolean, because the correct choice depends on the request
and the resolved file, and because redirects interact with versioning (see below).

## Why a path-rewriting middleware

- **No duplicate of the static file pipeline.** A custom file server would have to reimplement
  content-type mapping, `ETag`, `Last-Modified`, ranges, conditional requests, compression, `HEAD`,
  and `OnPrepareResponse`. Rewriting the path reuses all of it.
- **Composable with existing middleware.** The versioned middleware and any future path rewriter
  can run before it without special cases.
- **Small, testable public surface.** The middleware has one job and one probe loop.
- **Framework-consistent configuration.** `RequestPath` and `FileProvider` intentionally mirror
  `StaticFileOptions` so consumers can reuse the same provider instance and prefix.

## Why not a versioned extensionless middleware

We evaluated three options:

| Option | Description | Outcome |
|---|---|---|
| **A. Two independent middlewares** | `UseVersionedStaticFiles()` then `UseExtensionlessStaticFiles()` | **Chosen.** One responsibility per middleware, works for unversioned assets too, no change to the versioned middleware's API. |
| **B. Combined middleware** | An option on `VersionedStaticFilesMiddleware` or a new middleware that performs both rewrites | Rejected. Couples URL-extension policy to cache busting, only works for versioned paths, duplicates options, and still needs a redirect policy for the original client-facing path. |
| **C. Convenience wrapper** | `UseVersionedExtensionlessStaticFiles()` that calls A in the right order | Rejected for now. Hides two independently configurable middlewares to save one line. Revisit only if ordering mistakes become common. |

Option A's main cost is that ordering must be documented and tested. That is cheaper than coupling
two concerns and duplicating configuration, and it keeps the versioned middleware small.

### Redirects with versioning

A redirect is configured through `ShouldRedirect`, which receives both the path this middleware saw
(`OriginalPath`, already versioned) and the resolved file (`CandidatePath`, also versioned).

Redirecting to `CandidatePath` in a versioned pipeline is incorrect: the next request for
`/static/app/v1.2.3/index.html` is versioned again to `/static/app/v1.2.3/v1.2.3/index.html`.
The versioned middleware does not currently expose the original client-facing path, so a correct
redirect (to `/static/app/index.html`) is not possible out of the box.

Therefore:

- With `VersionedStaticFilesMiddleware`, use the internal rewrite (`ShouldRedirect = _ => false`).
- Without versioning, `ShouldRedirect` may be used freely, including with `301` for permanent
  mapping.

This limitation is documented and tested rather than hidden behind a combined API that would still
have to solve it.

## Consequences

- **Positive:** consumers get extensionless URLs without a custom file server; the static file
  pipeline stays authoritative; the versioned middleware keeps its API and tests; the new
  middleware also works for unversioned static assets.
- **Negative:** pipeline ordering matters and must be documented; redirects combined with versioning
  are unsupported until the versioned middleware exposes the pre-versioning path; every extensionless
  candidate costs an additional provider probe.
- **Neutral:** a convenience wrapper (Option C) remains a low-risk future addition if ordering
  mistakes become common. A combined middleware (Option B) is not planned.

## Risks accepted

- **Duplicate URLs and SEO.** `/about` and `/about.html` can both return `200` under internal
  rewrites. Consumers must emit canonical URLs or redirect one form at the edge.
- **Route conflicts.** A broad `ShouldApply` can rewrite API routes before routing/endpoints run.
  Consumers must scope the predicate.
- **Content type mapping.** The candidate extension must be known to the static files middleware's
  content type provider; `ServeUnknownFileTypes` is not a substitute.
- **Performance.** Each candidate is a provider probe (usually a file system stat). Scope with
  `ShouldApply` and keep `Extensions` minimal.
- **`MapStaticAssets`.** The endpoint/manifest-based static asset model does not include
  extensionless URLs; this middleware targets `UseStaticFiles()`.
- **Custom versioning providers.** `IStaticAssetsProvider` is not an `IFileProvider`. If versioned
  assets come from blob storage or another custom provider, extensionless probing only works when a
  matching `IFileProvider` over the same store is supplied to both middlewares.
- **Directory indexes.** Extensionless probing resolves files, not directories. `/static/app/`
  requires `UseDefaultFiles()` after versioning. `/static/app` (without the slash) is not rewritten
  by the versioned middleware and returns `404` unless a middleware appends the slash first.
- **Case sensitivity.** Linux file systems are case-sensitive; `/About` does not find `about.html`.

These and additional limitations are documented in
[Asp.Net Extensionless Static Files](../../docfx/docs/asp-dotnet-extensionless-static-files.md#risks-and-limitations).

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| Custom file-serving middleware | Reimplements content types, caching validators, ranges, compression, and `HEAD`; high risk of divergence from `UseStaticFiles()`. |
| `UseDefaultFiles()` only | Handles directory indexes, not `/file` → `/file.html`. |
| `MapFallbackToFile()` | SPA fallback maps every unmatched route to one file; it is not per-path extension probing and changes routing semantics. |
| URL Rewriting middleware (`Microsoft.AspNetCore.Rewrite`) with a custom rule | Possible, but rules are string-based and cannot consult `IFileProvider`, so a candidate file could be rewritten to a `404`. |
| Combined versioned + extensionless middleware | See Option B above. |

## References

- [ASP.NET Extensionless Static Files documentation](../../docfx/docs/asp-dotnet-extensionless-static-files.md)
- [ASP.NET Versioned Static Files documentation](../../docfx/docs/asp-dotnet-versioned-static-files.md)
- [ADR-0001: Multiple application base paths](0001-multiple-base-paths.md)
- [ASP.NET Core static files middleware](https://learn.microsoft.com/aspnet/core/fundamentals/static-files)
- Source: `src/Common.Utilities.AspNetCore/Middlewares/ExtensionlessStaticFiles/`

## Review triggers

Revisit this decision if:

- Consumers repeatedly place the middlewares in the wrong order.
- `VersionedStaticFilesMiddleware` starts exposing the pre-versioning path, which would make
  redirects safe in the versioned composition.
- Minimal API / `MapStaticAssets` gains first-class extensionless support.
- Profiling shows the extra provider probe is significant for a common deployment.
