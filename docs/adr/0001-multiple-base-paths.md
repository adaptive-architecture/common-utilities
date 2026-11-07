# ADR-0001: Multiple application base paths

- **Status:** Accepted
- **Date:** 2026-11-07
- **Scope:** `AdaptArch.Common.Utilities.AspNetCore`
- **Decision owners:** Maintainers

## Context

An application sometimes needs to be reachable under more than one base path at the same time — for example while renaming `my-legacy-app-name` to `my-new-app-name`, or when one deployment serves several tenants under their own prefixes.

ASP.NET Core exposes exactly one `HttpRequest.PathBase` per request, and the built-in `UsePathBase(PathString)` middleware accepts a single prefix. We evaluated adding a `UsePathBases(params PathString[])` middleware to `AdaptArch.Common.Utilities.AspNetCore` that would match any of several prefixes, strip the matched prefix, and add it to `PathBase`.

The path splitting itself is trivial. The hard parts are everything that consumes `PathBase`: link generation, cookies, authentication, anti-forgery, Blazor/SPA base URLs, OpenAPI, SignalR, caching, and SEO. A library helper cannot make those correct for every consumer, and shipping one would create a false sense of “just works”.

## Decision

We will **not** add a multi-base-path middleware to the library.

Consumers should use one of the two supported approaches:

1. **Reverse proxy + `X-Forwarded-Prefix`** (preferred).
2. **Chaining the built-in `UsePathBase`** when the app itself must recognize multiple prefixes.

Both approaches are documented below together with their caveats.

## Why not add a helper

- **The framework team declined first-class multi-value support.** In [dotnet/aspnetcore#40959](https://github.com/dotnet/aspnetcore/issues/40959) (“UsePathBase regex”), a user chains several `UsePathBase` calls and asks for a dynamic/multiple-value form. The team’s answer was: *“we won't implement a feature for this (as you described, it creates many problems :) ) but you can implement this behavior yourself in middleware.”*
- **The framework’s direction is proxy-based.** [dotnet/aspnetcore#68811](https://github.com/dotnet/aspnetcore/issues/68811) — the .NET 11 validation scenario “BasePath when one deployment answers on several path prefixes” — explicitly recommends a proxy that sends `X-Forwarded-Prefix` plus `ForwardedHeaders.XForwardedPrefix`, and describes mapping several path bases inside the app as the *lighter alternative if setting up a proxy is impractical*.
- **Minimal hosting makes a hand-rolled middleware subtly wrong.** `WebApplication` wraps the user pipeline in `UseRouting`/`UseEndpoints`. The built-in `UsePathBase` compensates with an internal re-route (`RerouteHelper`); a plain `app.Use(...)` middleware that only rewrites `Path`/`PathBase` does not, so endpoints may already be matched against the unmodified path.
- **The sharp edges are not ours to solve.** Cookie paths, data protection, `<base href>`, OpenAPI `servers`, SignalR URLs, cache keys, and canonical URLs all need per-application decisions.
- **The library should stay a library.** Per the project constitution, modules must solve a specific, well-bounded cross-cutting concern. A migration-window helper with many environment-specific caveats is a deployment concern, not a general-purpose utility.

## Approach 1 (preferred): reverse proxy + `X-Forwarded-Prefix`

The proxy exposes both public prefixes and forwards to the app. When it strips a prefix, it tells the app which prefix was stripped via `X-Forwarded-Prefix`; the app enables `ForwardedHeaders.XForwardedPrefix` and ASP.NET Core sets `Request.PathBase` for that request.

```csharp
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost |
        ForwardedHeaders.XForwardedPrefix;

    // ASP.NET Core trusts only loopback by default. Trust your ingress explicitly.
    options.KnownProxies.Add(IPAddress.Parse("10.0.0.10"));
});

var app = builder.Build();

app.UseForwardedHeaders();
// ... rest of the pipeline
```

Proxy examples:

- **YARP** — the `X-Forwarded-Prefix` transform is part of the default transforms.
- **Traefik** — the `StripPrefix` middleware stores the stripped prefix in `X-Forwarded-Prefix`.
- **ingress-nginx** — use the `nginx.ingress.kubernetes.io/x-forwarded-prefix` annotation.
- **nginx** — set it manually, for example with one `location` block per public prefix and `proxy_set_header X-Forwarded-Prefix <prefix>;`.
- **Envoy/Istio, AWS ALB, GCP/Azure load balancers** — do not emit it by default; add header manipulation or set it at the edge.

### Caveats

- **It is not an RFC standard.** RFC 7239 defines `Forwarded` with `for`, `by`, `host`, and `proto` only; there is no standard path-prefix parameter. `X-Forwarded-Prefix` is a de facto convention. It is not documented on MDN, and ingress-nginx’s own documentation calls it “the non-standard `X-Forwarded-Prefix` header.”
- **Proxy support is not universal.** Traefik, YARP, Kong, Spring Cloud Gateway, and ingress-nginx support it; nginx, Envoy, AWS ALB, and the major cloud load balancers do not emit it without extra configuration.
- **Trust must be configured.** Only honor the header from known proxies/networks. ASP.NET Core trusts only loopback by default, and recent .NET servicing releases hardened forwarded-header handling to ignore headers from unknown proxies. See also [dotnet/aspnetcore#61449](https://github.com/dotnet/aspnetcore/issues/61449).
- **Multiple proxies are still rough.** Multiple `X-Forwarded-Prefix` values are not accumulated correctly today: [dotnet/aspnetcore#60587](https://github.com/dotnet/aspnetcore/issues/60587).
- **Auth cookies fragment per prefix by default.** An authentication cookie uses `HttpRequest.PathBase` as its default `Cookie.Path`. Set `Cookie.Path = "/"` if the same session must work under both prefixes, and share the data-protection key ring if the prefixes are served by separate apps:

  ```csharp
  builder.Services.ConfigureApplicationCookie(options =>
  {
      options.Cookie.Path = "/";
  });
  ```

  Verify the same for anti-forgery and any custom cookies. The session cookie defaults to `/`, but check it too.
- **Blazor/SPA base URLs.** A static `<base href="/">` breaks client-side routing under a prefix. In .NET 11+ Blazor Web Apps can use the `<BasePath />` component; earlier versions need a dynamic `<base>` value. Bundled assets and `loadBootResource` URLs may also need prefix-aware handling.
- **OpenAPI.** `UsePathBase` is not automatically reflected in the generated `servers` array ([dotnet/aspnetcore#61486](https://github.com/dotnet/aspnetcore/issues/61486)). Add an `OpenApiDocumentTransformer` or configure the servers explicitly.
- **SignalR / Blazor Server.** Hub and negotiate URLs are generated from `PathBase`, but clients must reconnect through the same prefix, and sticky sessions are required for scale-out.
- **Absolute URLs outside the request.** Links baked into configuration, database rows, emails, webhook callbacks, and SDKs still point at one prefix. Plan the migration.
- **Caching, rate limiting, and telemetry.** Keys and metric dimensions that include the path double in cardinality. Tag `PathBase` as a separate dimension where relevant.
- **SEO.** Serving the same app under two prefixes creates duplicate content. Prefer a `301` from the legacy prefix to the new one where clients can follow redirects, and emit a canonical URL.
- **The bare root path is still reachable** unless the proxy or app blocks it.

## Approach 2: chain the built-in `UsePathBase`

When a proxy cannot be used, the app can recognize several prefixes itself:

```csharp
// Register longer, nested prefixes first.
app.UsePathBase("/my-new-app-name");
app.UsePathBase("/my-legacy-app-name");
```

Each `UsePathBase` only acts when the current path starts with its own prefix, so non-overlapping prefixes behave as alternatives. The matched prefix is appended to `PathBase`, and the original `Path`/`PathBase` are restored when the middleware unwinds.

### Caveats

- **It is not an officially documented multi-base-path feature.** Chaining works de facto, but the team’s position is “write middleware yourself” ([dotnet/aspnetcore#40959](https://github.com/dotnet/aspnetcore/issues/40959)).
- **Order matters for nested prefixes.** `/app/legacy` must be registered before `/app`; otherwise `/app` consumes the request first.
- **Minimal hosting re-routes.** The built-in middleware uses an internal re-route to make endpoint matching observe the new path. A custom `app.Use(...)` that only assigns `PathBase` does not get that behavior.
- **Authentication/authorization ordering can break.** In `WebApplicationBuilder` apps, `UsePathBase` can insert its re-route after the framework’s authentication/authorization middleware, so `RequireAuthorization()` may not see the endpoint ([dotnet/aspnetcore#59638](https://github.com/dotnet/aspnetcore/issues/59638)). An `IStartupFilter` that inserts `UsePathBase` before routing is the known workaround.
- **The bare root path still works.** `UsePathBase` does not disable `/` ([dotnet/aspnetcore#2698](https://github.com/dotnet/aspnetcore/issues/2698)); redirect or return `404` explicitly if the app should only be reachable under the prefixes.
- **Link generation only knows the current request’s base.** You cannot generate a link under the *other* prefix from a request that arrived under one prefix. Cross-prefix links need to be constructed explicitly.
- **Same cookie caveat as Approach 1.** Auth cookies default to `PathBase`; set `Cookie.Path = "/"` if sessions must be shared.
- **Adding or changing a prefix requires a deploy** unless the list is read from configuration, and even then each prefix needs a `UsePathBase` call at startup.
- **Same SEO, caching, OpenAPI, Blazor, and SignalR caveats as Approach 1.**

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| New `UsePathBases(params PathString[])` middleware in `AdaptArch.Common.Utilities.AspNetCore` | Duplicates framework behavior, has to replicate the minimal-hosting re-route, and cannot solve cookie/SPA/OpenAPI/SEO caveats for consumers. |
| `app.Map("/prefix", branch => ...)` | Branches the pipeline and duplicates middleware/endpoint configuration per prefix; terminal branches are awkward to compose. |
| Proxy path rewrite without `X-Forwarded-Prefix` | The app cannot generate correct links or redirects for the public prefix. |
| `301` redirect from legacy to new | Preferred where feasible, but not always possible (non-browser clients, webhooks, hard-coded SDK base URLs). |
| IIS virtual application / HttpSys multiple registered prefixes | Server-level and platform-specific; not portable to Kestrel/Linux containers. See [dotnet/aspnetcore#17024](https://github.com/dotnet/aspnetcore/issues/17024). |

## Consequences

- **Positive:** no new public API surface, no false confidence, alignment with the framework’s proxy-based direction, and each application can choose the mechanism that fits its deployment.
- **Negative:** consumers must configure a proxy or chain `UsePathBase` themselves and must handle the cookie/SPA/SEO caveats. There is no single helper to point at.
- **Neutral:** this decision can be revisited if .NET adds first-class multi-base-path support, or if several internal applications prove that the same helper is needed repeatedly.

## References

- [dotnet/aspnetcore#40959 — UsePathBase regex (team declined multi-value/regex support)](https://github.com/dotnet/aspnetcore/issues/40959)
- [dotnet/aspnetcore#51669 — Read path base from a request header (resolved by X-Forwarded-Prefix)](https://github.com/dotnet/aspnetcore/issues/51669)
- [dotnet/aspnetcore#5898 — Add support for specifying the PathBase for all requests](https://github.com/dotnet/aspnetcore/issues/5898)
- [dotnet/aspnetcore#17024 — HttpSys multiple registered prefixes](https://github.com/dotnet/aspnetcore/issues/17024)
- [dotnet/aspnetcore#59638 — UsePathBase ordering in minimal hosting](https://github.com/dotnet/aspnetcore/issues/59638)
- [dotnet/aspnetcore#60587 — Multiple X-Forwarded-Prefix values](https://github.com/dotnet/aspnetcore/issues/60587)
- [dotnet/aspnetcore#68811 — Validation: BasePath when one deployment answers on several path prefixes](https://github.com/dotnet/aspnetcore/issues/68811)
- [dotnet/aspnetcore#23263 — Support for X-Forwarded-Prefix](https://github.com/dotnet/aspnetcore/issues/23263)
- [dotnet/aspnetcore#49249 — X-Forwarded-Prefix implementation (PR)](https://github.com/dotnet/aspnetcore/pull/49249)
- [dotnet/aspnetcore#2698 — UsePathBase does not disable the root path](https://github.com/dotnet/aspnetcore/issues/2698)
- [dotnet/aspnetcore#61486 — UsePathBase and OpenAPI servers](https://github.com/dotnet/aspnetcore/issues/61486)
- [dotnet/aspnetcore discussion #57139 — PathBase vs Prefix (HttpSys)](https://github.com/dotnet/aspnetcore/discussions/57139)
- [RFC 7239 — Forwarded HTTP Extension](https://www.rfc-editor.org/rfc/rfc7239)
- [ASP.NET Core — Configure ASP.NET Core to work with proxy servers and load balancers](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer)
- [ASP.NET Core — Share cookies across different base paths](https://learn.microsoft.com/aspnet/core/security/cookie-sharing)
- [ASP.NET Core Blazor — App base path](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/app-base-path)
- [ASP.NET Core 11 release notes — New BasePath component](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11)
- [Traefik — StripPrefix middleware](https://doc.traefik.io/traefik/reference/routing-configuration/http/middlewares/stripprefix/)
- [ingress-nginx — X-Forwarded-Prefix Header annotation](https://kubernetes.github.io/ingress-nginx/user-guide/nginx-configuration/annotations/#x-forwarded-prefix-header)

## Review triggers

Revisit this decision if:

- .NET adds a first-class multi-base-path API.
- `X-Forwarded-Prefix` support becomes universal across the proxies/load balancers the organization uses.
- Two or more applications independently implement the same workaround.
- The cookie/SPA/OpenAPI caveats are solved centrally by the framework or by a shared internal package.
