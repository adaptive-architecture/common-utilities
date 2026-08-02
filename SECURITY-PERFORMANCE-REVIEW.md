# Security & Performance Review

**Date**: 2026-08-02
**Branch**: `review/security-performance`
**Scope**: All `src/` projects, build pipeline, and CI workflows.

This document lists the findings of a security and performance review of the repository.
Findings are split into items **fixed on this branch** and **follow-ups** that need a
separate decision (breaking key layouts, design changes, CI policy).

Severity legend: 🔴 critical · 🟠 high · 🟡 medium · 🔵 low

---

## Part 1 — Fixed on this branch

### Security

#### 🔴 S1. SQL identifier injection via unvalidated table name (Postgres)

`PostgresLeaseStore` interpolates the constructor-supplied table name into every SQL
statement (including runtime DDL in `EnsureTableExistsAsync`) via `String.Format`,
with **no validation**. The only guard, `PostgresLeaderElectionOptions.IsValidTableName`,
was never referenced by any production code path — the documented usage constructs
`PostgresLeaseStore` directly with a raw string.

- `src/Common.Utilities.Postgres/LeaderElection/PostgresLeaseStore.cs` (`String.Format(sql, _tableName)`)

**Fix**: the constructor now rejects any table name that is not a valid PostgreSQL
unquoted identifier (`^[A-Za-z_][A-Za-z0-9_]{0,62}$`), and the identifier is quoted
(`"{0}"`) in all SQL templates and the index name. `PostgresLeaderElectionOptions`
validation was tightened to match (the previous rule incorrectly allowed `-`).

**Behavior change**: table names containing `-` or other non-identifier characters are
now rejected at construction time.

#### 🔴 S2. Path traversal via version cookie (AspNetCore, VersionedStaticFiles)

`VersionCookiePayload.TryParse` accepted any attacker-controlled string as `Version`.
That value flowed into `Path.Combine(_baseDirectory, targetDirectory, version)` followed
by `Directory.CreateDirectory`, and into a request-path rewrite. `Path.Combine` does not
neutralize `..` and *rebases* when a later argument is rooted — so a crafted cookie could
create directories anywhere the process can write and influence static-file resolution.

- `src/Common.Utilities.AspNetCore/Middlewares/VersionedStaticFiles/VersionCookiePayload.cs`
- `src/Common.Utilities.AspNetCore/Middlewares/VersionedStaticFiles/DiskStaticAssetsProvider.cs`

**Fix**: `TryParse` now only accepts versions matching `^[A-Za-z0-9._-]{1,64}$` and
rejects `.` / `..`. As defense in depth, `DiskStaticAssetsProvider` verifies the resolved
full path is contained within the base directory before touching the filesystem.

**Behavior change**: version strings outside the allowlist now fail to parse (the
middleware falls back to its no-cookie behavior).

#### 🟠 S3. Unhandled exception → 500 for paths directly under the static prefix

`VersionedStaticFilesMiddleware.ExtractTargetDirectory` threw
`ArgumentOutOfRangeException` for any request like `/static/app.js` or `/static/`
(no second slash ⇒ `IndexOf` returns −1 ⇒ negative range). Trivial availability issue:
any anonymous request to such a path produced a 500.

- `src/Common.Utilities.AspNetCore/Middlewares/VersionedStaticFiles/VersionedStaticFilesMiddleware.cs`

**Fix**: the no-second-slash case is handled; such requests pass through unmodified.

#### 🟠 S4. Base32 buffer-size contract error and malleable decoding

- `GetArraySizeRequiredToEncode` returned `ceil(count·8/5)` while the encoder always
  writes `ceil(count/5)·8` padded characters. A caller sizing its buffer per the public
  contract got an `IndexOutOfRangeException` (write past the validated bound) — e.g.
  1 input byte ⇒ contract says 2 chars, encoder writes 8.
- The decoder stopped once the output was full and never validated trailing input
  characters, and did not require leftover bits to be zero — multiple distinct strings
  decoded to the same bytes (**non-canonical / malleable encoding**). Dangerous when
  base32 values are used as cache keys, dedup identities, or tokens.
- Size math used `float`, losing precision above ~2²⁴.

- `src/Common.Utilities/Encoding/Base32.cs`

**Fix**: integer-math size formula `((count + 4) / 5) * 8`; the decoder validates every
input character and rejects non-zero leftover bits. `Base64Url.Decode(string,int,int)`
also gained the missing null check for consistency.

**Behavior change**: previously-accepted malformed/non-canonical base32 inputs now throw
`FormatException`.

#### 🟠 S5. `HttpContext.IsLocal()` failed open

When both `RemoteIpAddress` and `LocalIpAddress` were null (some proxy, in-memory, or
Unix-socket transports), the method returned **true**. Anything gating diagnostics or
admin endpoints on `IsLocal()` treated those transports as local.

- `src/Common.Utilities.AspNetCore/Extensions/HttpContext.cs`

**Fix**: returns `false` when no address information is available (fail closed).

**Behavior change**: in-memory test servers without connection IPs now report non-local.

#### 🟠 S6. Redis topics silently became pattern subscriptions

`topic.ToChannel()` used `RedisChannel.PatternMode.Auto`: any topic containing `*`
became a *pattern* subscription. If topic names ever derive from user input,
`Subscribe("*")` is a firehose subscription across the entire keyspace
(information disclosure / DoS).

- `src/Common.Utilities.Redis/Utilities/Extensions.Redis.cs`

**Fix**: `PatternMode.Literal`.

**Behavior change**: consumers relying on implicit glob subscriptions must now opt in
explicitly (none exist in this repo).

### Reliability / performance

#### 🔴 P1. Postgres connection leak on every lease read

`PostgresLeaseStore.ExecuteReaderAsync` opened a pooled `NpgsqlConnection` and created an
`NpgsqlCommand` without disposing either; callers disposed only the reader. Without
`CommandBehavior.CloseConnection`, disposing the reader does **not** return the
connection to the pool — every acquire/renew/read leaked a pooled connection, and the
leader-election loop performs these every few seconds. Result: pool exhaustion.

- `src/Common.Utilities.Postgres/LeaderElection/PostgresLeaseStore.cs`

**Fix**: reader execution refactored so the helper owns connection/command/reader
lifetime and returns the mapped result; all three are disposed on every path.

#### 🟠 P2. `JobWorker` resource leaks (partial fix)

The `PeriodicTimer`, `SemaphoreSlim`, and configuration CTS were never disposed, and the
`IOptionsMonitor.OnChange` subscription was discarded — the callback (which captures
`this`) kept firing after the worker stopped, keeping the worker alive.

- `src/Common.Utilities.Hosting/BackgroundWorkers/Contracts/JobWorker.cs`

**Fix (applied)**: the worker now retains the `OnChange` subscription and disposes it
along with the timer, semaphore, and CTS in a new `Dispose` override.

**Not fixed here (see follow-up #23)**: the related semaphore-accounting issue
(`_ = _configurationChangeLock.Wait(timeout)` discards the result and the `finally`
releases regardless). Honoring the result in isolation exposes a latent deadlock the
current unbounded `SemaphoreSlim(1)` masks, so it needs the election/timer loop
redesigned rather than a one-line change.

#### 🔴 P3. `ExclusiveAccess` mutual exclusion broken by double-dispose

`LockedResource.Dispose` released the `SemaphoreSlim(1,1)` unconditionally — a double
dispose over-released, letting two callers hold the "exclusive" resource simultaneously
(or throwing `SemaphoreFullException`).

- `src/Common.Utilities/Synchronization/ExclusiveAccess.cs`

**Fix**: dispose is idempotent (interlocked flag; only the first dispose releases).

#### 🔴 P4. `RunSync` deadlocked forever on canceled tasks

The completion continuation used `TaskContinuationOptions.NotOnCanceled`, so a canceled
task never signaled the single-threaded pump: `CompleteAdding` never ran and
`RunSync` blocked indefinitely.

- `src/Common.Utilities/Extensions/Task.cs`

**Fix**: the continuation always runs; cancellation surfaces as
`TaskCanceledException` via `GetAwaiter().GetResult()`.

#### 🟠 P5. `HashRing` data race and O(n) lookups

- The snapshot history list was mutated under `lock` but **read without it** from
  `GetServer`/`GetServers`/`GetServersCore` — unsynchronized concurrent read/write of a
  `List<T>` (torn reads, `IndexOutOfRangeException` under load) in a class documented
  as thread-safe.
- `ConfigurationSnapshot.GetServer` used `List.FindIndex` (linear scan + delegate
  allocation per lookup) although the list is sorted and a binary search already existed
  in `HashRing`.
- Snapshots re-sorted an already-sorted list; `GetServers` copied the whole virtual-node
  list per call.

- `src/Common.Utilities/ConsistentHashing/HashRing.cs`
- `src/Common.Utilities/ConsistentHashing/ConfigurationSnapshot.cs`

**Fix**: reads take the same lock as writers; snapshot lookup uses binary search; the
redundant re-sort and per-call list copy were removed.

#### 🟠 P6. Shared non-thread-safe `Random` singleton

`RandomGenerator.Instance` wrapped one `Random` instance. `Random` instance methods are
not thread-safe: concurrent use (retry jitter) can corrupt internal state and degenerate
to returning zeros. Replaced with `Random.Shared`.

- `src/Common.Utilities/GlobalAbstractions/Implementations/RandomGenerator.cs`

#### 🟠 P7. Redis message hub leaked handler registrations

`Unsubscribe`/`UnsubscribeAsync` unsubscribed from Redis but never removed the handler
from `HandlerRegistry` — the registry grew without bound and kept handler delegates (and
captured state) alive for the process lifetime.

- `src/Common.Utilities.Redis/PubSub/RedisMessageHub.cs`

**Fix**: registrations are removed from the registry on unsubscribe. Malformed payload
deserialization was also moved inside the error-handled path so it routes to
`OnMessageHandlerError` instead of throwing inside the StackExchange.Redis callback.

#### 🟠 P8. Configuration provider never stopped polling

`CustomConfigurationProvider` started an infinite polling loop but did not implement
`IDisposable` — `ConfigurationRoot` disposes providers that do, so the CTS and loop
outlived the provider forever. `DisablePooling` also allowed a second concurrent poller
to start while the first was still in flight.

- `src/Common.Utilities.Configuration/Providers/CustomConfigurationProvider.cs`

**Fix**: the provider implements `IDisposable` (cancels and disposes the CTS);
enable/disable transitions are guarded against overlapping pollers.

#### 🟡 P9. Per-publish allocation and serializer-context churn

- `MessageHubOptions.GetMessageBuilder<T>()` allocated a new builder + date-time provider
  + UUID provider on **every publish**; these are stateless and are now cached.
- Redis and Postgres `JsonDataSerializer` created a `new DefaultJsonSerializerContext()`
  per serializer instance (each rebuilds its `JsonTypeInfo` cache) instead of the
  source-generated `.Default` singleton.

- `src/Common.Utilities/PubSub/Implementations/MessageHubOptions.cs`
- `src/Common.Utilities.Redis/Serialization/Implementations/JsonDataSerializer.cs`
- `src/Common.Utilities.Postgres/Serialization/Implementations/JsonDataSerializer.cs`

---

## Part 2 — Follow-ups (not fixed here)

These need either a design decision, a breaking key-layout migration, or CI policy
changes. Ordered by severity.

### Security / correctness

1. 🔴 **Leader election has no fencing tokens and weak "authorization".** The
   caller-supplied `participantId` (typically a guessable hostname/pod name) is the only
   token authorizing renew/release — any client with store access can steal or release
   another node's lease. There is no monotonic epoch, and `LeaderInfo.IsValid` compares
   `ExpiresAt` against the local clock, so clock skew can produce two simultaneous
   leaders. Recommend: random fencing token per acquisition, separate from the
   human-readable participant ID.
   (`src/Common.Utilities/Synchronization/LeaderElection/`, Redis + Postgres stores)
2. 🔴 **`RedisLeaseStore.GetCurrentLeaseAsync` deletes the lease key** when the lease
   *looks* expired by the local clock — a read operation performing an unauthenticated
   `DEL`; a clock-skewed reader can delete a live leader's lease.
   (`src/Common.Utilities.Redis/LeaderElection/RedisLeaseStore.cs`)
3. 🟠 **`RedisLeaderElectionOptions.KeyPrefix` and `.Database` are silently ignored** —
   keys are hardcoded to `leader_election:lease:{name}` and `GetDatabase()` is called
   without arguments. Fixing changes the key layout for existing deployments (needs a
   migration note), which is why it is not done on this branch.
4. 🟠 **Lua scripts parse the lease with `cjson` assuming PascalCase JSON** — plugging in
   any custom `IDataSerializer` (camelCase, MessagePack) silently breaks renew/release.
   (`RedisLeaseStore.cs` Lua scripts)
5. 🟠 **Config-provider DoS via key collisions**: after `OriginalKeyDelimiter → :`
   replacement, two distinct upstream keys can collide and `Dictionary.Add` throws,
   failing the entire configuration load. Also uses `InvariantCultureIgnoreCase` where
   .NET configuration uses `OrdinalIgnoreCase`.
   (`src/Common.Utilities.Configuration/Providers/CustomConfigurationProvider.cs`)
6. 🟡 **JSON parser key injection**: a key containing the delimiter can shadow a nested
   path; no explicit `MaxDepth` set (falls back to STJ's 64).
   (`src/Common.Utilities/Configuration/Implementation/JsonConfigurationParser.cs`)
7. 🟡 **Reflection deserializers accept unbounded payloads** — no `MaxDepth`/size limits
   on `ReflectionJsonDataSerializer`, which is the *default* for Redis pub/sub messages
   any publisher can write. (`src/Common.Utilities.Redis/Serialization/`)
8. 🟡 **`VersionedStaticFilesMiddleware` cookie `Secure` flag** follows the request
   scheme; behind TLS-terminating proxies the cookie is set without `Secure`.

### Performance / reliability

9. 🟠 **Hosting PubSub dispatches every message via `MethodInfo.Invoke`** (boxing array
   + reflection per message) and resolves handlers with `GetService` (null ⇒ NRE per
   message). A cached compiled delegate per handler would be the single biggest perf win
   in the Hosting package. (`src/Common.Utilities.Hosting/PubSub/ScopedMessageHandler.cs`)
10. 🟠 **`VersionedStaticFilesMiddleware` does 2 directory stats + a file read + a JSON
    parse per asset request** — needs a cached version lookup (e.g. `IMemoryCache` or
    file-watcher invalidation).
11. 🟠 **Election loop has no jitter or backoff** — N followers restarted together poll
    the store in lockstep every `RetryInterval`; error paths retry at the same fixed
    interval. The repo already ships `IJitterGenerator` — wire it in.
12. 🟡 **`PostgresLeaderElectionOptions.CleanupInterval` is never scheduled** — expired
    lease rows accumulate. `CommandTimeout`/`ConnectionTimeout` options are also unused.
23. 🟠 **`JobWorker` semaphore accounting.** `SetTimerPeriod`, `HandleConfigurationChange`
    and `DisposeConfigurationCts` discard the `SemaphoreSlim.Wait(timeout)` result and
    release in `finally` regardless. Because the semaphore is created as
    `SemaphoreSlim(1)` (no upper bound) a stray `Release()` cannot throw
    `SemaphoreFullException`, but it drifts the permit count upward so the "mutex" stops
    excluding. Honoring the result requires reworking the loop (a naive fix deadlocks the
    enable-after-start path, as the loop relies on the current over-release behavior).
    (`src/Common.Utilities.Hosting/BackgroundWorkers/Contracts/JobWorker.cs`)
13. 🟡 **`RepeatingWorkerConfiguration` compiles a new unanchored `Regex` per override
    per call** and can return the shared options instance (aliasing — one worker can
    mutate global config). (`src/Common.Utilities.Hosting/BackgroundWorkers/Configuration/`)
14. 🟡 **`ResponseStreamWrapper` sync-over-async** (`GetAwaiter().GetResult()` inside
    `Write`) and a full `ToArray()` copy of every span write.
    (`src/Common.Utilities.AspNetCore/Middlewares/ResponseRewrite/`)
15. 🔵 **`HandlerRegistry.Remove`/`GetRegistration` scan all topics per call**; jitter
    RNG math is slightly biased (`Next(0, 99) % 2`).

### Build / CI hardening

16. 🟠 **No NuGet lock files, but both workflow caches key on
    `hashFiles('**/packages.lock.json')`** — the key hashes to a constant, so caches are
    unversioned and restores non-deterministic. Enable `RestorePackagesWithLockFile`.
17. 🟠 **Only SDK-default analyzers run** — no `AnalysisMode`/`AnalysisLevel`, so the CA
    security/reliability rule sets are off. Adding `<AnalysisMode>All</AnalysisMode>`
    (or at least `AnalysisModeSecurity=All`) in `Directory.Build.props` is cheap and
    high-value. No CodeQL workflow exists.
18. 🟡 **GitHub Actions are tag-pinned, not SHA-pinned**; the NuGet API key is passed as
    a **command-line argument** in `pack.yml` (visible in process listings — prefer env
    vars); `publish-packages.sh` has `getopts "c:v:n:g"` — `g` is missing its colon, so
    the GitHub key is always empty if that path is re-enabled.
19. 🟡 **`nuget.config` maps `*` to both nuget.org and the local `./.nuget/` folder** —
    a dropped `.nupkg` can shadow an upstream package.
20. 🔵 **Version skew**: `Microsoft.Extensions.Hosting` 10.0.0 vs `.Abstractions` 10.0.9;
    `Microsoft.Bcl.Memory` still 9.0.7.
21. 🔵 **`pipeline/unit-test.sh` filter quoting**: the backslash-escaped quotes in
    `--filter \"FullyQualifiedName!~…\"` pass literal `"` characters into the filter
    expression; the samples exclusion likely never matched.
22. 🔵 **Release-config tests run against published packages**, not local source
    (`Directory.Packages.props` swaps ProjectReferences for PackageReferences in
    Release) — verification of source changes must use Debug.

---

## Verification performed on this branch

- New failing-first unit tests for every fixed item (TDD per the project constitution).
- `dotnet build` (warnings-as-errors, Roslynator) and `sh ./pipeline/unit-test.sh`
  pass in Debug configuration.
