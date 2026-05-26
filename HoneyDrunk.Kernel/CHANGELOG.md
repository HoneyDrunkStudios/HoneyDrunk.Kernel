# HoneyDrunk.Kernel - Repository Changelog

All notable changes to the HoneyDrunk.Kernel repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Kernel.Abstractions CHANGELOG](HoneyDrunk.Kernel.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Kernel CHANGELOG](HoneyDrunk.Kernel/CHANGELOG.md)

---

## [Unreleased]

### Changed (breaking — v0.8.0)

- **`AgentResultSerializer`, `GridContextSerializer`, `HttpContextMapper`, `JobContextMapper`** converted from `public sealed class` to `public static class`. All methods on these types are already static; the previous instance shape was unused at every call site in the Grid. **Migration:** drop any `new <Type>(...)` calls and use the static methods directly. No code in the Kernel codebase or its sibling Grid repos relied on the instance shape, but downstream consumers who held references to instances will need to remove them.
- **`JobContextMapper`** also drops its public instance constructor `(string nodeId, string studioId, string environment)` and the three private `_nodeId` / `_studioId` / `_environment` fields. The fields were never read and the constructor was unused. **Migration:** if you were calling `new JobContextMapper(nodeId, studioId, environment)` and then invoking the static `InitializeForJob` / `InitializeForScheduledJob` / `InitializeFromMetadata` methods, drop the construction step — the static methods are the only entry points.
- **`GridContextSnapshot` constructor parameter order** updated to match .NET conventions: `CancellationToken cancellation` is now the last parameter (after `DateTimeOffset? createdAtUtc`). **Migration:** all known callers (HoneyDrunk.Transport `GridContextFactory`, Kernel.Tests `GridContextSnapshotTests`, Transport.Tests) use named arguments and are unaffected. If you used positional args to specify both `cancellation` and `createdAtUtc`, swap the order.
- **Package version bumped** to `HoneyDrunk.Kernel 0.8.0` and `HoneyDrunk.Kernel.Abstractions 0.8.0` to reflect these breaking changes per semver pre-1.0 (`0.x.0 → 0.(x+1).0` for breaks).

### Internal

- Driven the remaining 17 maintainability findings on Kernel's SonarCloud dashboard to zero (ADR-0011 D11 gate-cleanup follow-up to PR #63). Real code fixes preferred over `[SuppressMessage]` — pre-1.0 packages with semantic versioning don't need "preserve binary compat" justifications. Specifically: the static-class conversions and `GridContextSnapshot` parameter reorder above (covered by the version bump), plus the minor fixes below.
- Converted multi-line `//` comment block on `GridContextSerializer.TryGetStringProperty` to a proper `///` XML doc (resolves S125 commented-out-code false positive).
- Re-applied the URI-constant extraction in `DefaultErrorClassifier` (8 const fields under `DocsBaseUri` interpolation) + added 11 direct tests on the internal `DefaultErrorClassifier.Classify` via `InternalsVisibleTo("HoneyDrunk.Kernel.Tests")` — one per Kernel exception type, both BCL fallback paths, and the unclassified-returns-null path.
- Re-applied the `AddHoneyDrunkNodeHint` constant extraction in `ServiceProviderValidation` (replaces 8 inline `"AddHoneyDrunkNode()"` literals) + added a `Validate_EmptyServices_ThrowsListingAllRequiredServicesWithHint` test that asserts every required service name appears in the thrown message alongside the registration hint.
- Re-applied the `nameof(...)` second-argument removals on `HoneyDrunkCoreExtensions.AddHoneyDrunkCoreNode` and `.ValidateHoneyDrunkServices` (Sonar S3236 — the BCL methods auto-capture argument names via `[CallerArgumentExpression]`) + added 9 tests covering both extension methods (null-arg paths, empty-NodeId / empty-Version validation, full registration verification for `INodeDescriptor` / `INodeContext` / `IGridContextAccessor` / `NodeLifecycleManager`, and `ValidateHoneyDrunkServices` no-op when no validator is registered).
- Refactored `ConfigurationValidator`'s capability-validation `foreach` loop into two LINQ passes (Sonar S3267): one counts empty-name capabilities and emits an error per occurrence, the other groups by name and emits a duplicate error per name that appears more than once. Existing `Duplicate capability name`/`Capability name cannot be empty` tests still pass — error messages preserved verbatim.
- Updated `WellKnownNodesTests.CanonicalNodeIds` `TheoryData<string, NodeId>` → `TheoryData<string, string>` (Test Explorer can't enumerate `NodeId` data rows because the struct isn't `[Serializable]`); tests now parse the string into `NodeId` inside the test body. Resolves the two Roslyn `xUnitData` info findings.
- Added `[SuppressMessage("Design", "CA1068")]` alongside the existing `[SuppressMessage("Roslynator", "RCS1163")]` on `GridContextSnapshot` ctor for completeness, then *removed all three* (RCS1163, CA1068, S107 about-the-CancellationToken) by actually moving `cancellation` to last position. The remaining S107 (10 params total) is suppressed with a *design* justification — the snapshot type mirrors `IGridContext`'s full surface 1:1 by intent, and the options-record alternative is a separate UX choice unrelated to the binary-compat framing of the previous justification.
- Added `InternalsVisibleTo("HoneyDrunk.Kernel.Tests")` to `HoneyDrunk.Kernel.csproj` so `internal sealed class` types (`DefaultErrorClassifier`, `ServiceProviderValidation`) can be directly tested without going through `IServiceProvider` activation.
- 941/941 tests pass (was 930 after #63 merge; net +11 from this PR).

- Triaged the initial SonarQube Cloud findings against Kernel (ADR-0011 D11 gate-cleanup). Fixed two Blocker missing-assertion tests (`OperationContextTests.Dispose_CalledMultipleTimes_IsSafe`, `NoopBillingEventEmitterTests.EmitAsync_CompletesForBillingEvent` — now `NotThrow`/`NotThrowAsync` via AwesomeAssertions). Removed three unused fields and a dead instance constructor from `JobContextMapper` and made the class `static` to match all callers. Reduced `GridContextSerializer.Deserialize` cognitive complexity from 17 to under 15 by extracting `TryGetStringProperty`, `TryGetTenantId`, and `ExtractBaggage` helpers. Made `AgentResultSerializer`, `GridContextSerializer`, and `HttpContextMapper` `static class` (all methods were already static). Deduped `MessagingContextMapper.InitializeFromMessage` and `ExtractFromMessage` against a shared `ResolveMetadata`/`ExtractFromMessage` core (resolves SonarCloud 30% duplication finding on this file). Suppressed the stale S3925 ISerializable findings on the five public Kernel exception types with justification — the legacy `SerializationInfo`/`StreamingContext` ctor pattern is obsolete in .NET 8+ (SYSLIB0051) and `BinaryFormatter` is removed (SYSLIB0011). Suppressed S2139 log-and-rethrow on `GridContextMiddleware.InvokeAsync` and `NodeLifecycleHost.{StartAsync, StopAsync}` with justification — middleware/lifecycle code intentionally logs with CorrelationId/NodeId attribution before rethrowing. Extracted eight hardcoded ProblemDetails type URIs in `DefaultErrorClassifier` to `DocsBaseUri`-derived constants. Extracted the repeated `"AddHoneyDrunkNode()"` registration hint in `ServiceProviderValidation` to a `AddHoneyDrunkNodeHint` constant. Migrated `samples/MinimalNode/Program.cs` from `app.Run()` to `await app.RunAsync()`. Trimmed trailing whitespace in `FixGridContextTests.ps1`. Suppressed S107 (>7 ctor params) on `GridContextSnapshot` and the private nested `NodeDescriptor` in `HoneyDrunkNodeServiceCollectionExtensions` with justification — refactoring to options records is a public-shape change deferred to a focused initiative. Dropped the redundant `nameof()` second argument from `ArgumentException.ThrowIfNullOrWhiteSpace`/`ArgumentNullException.ThrowIfNull` calls across 13 files (Sonar S3236 — the BCL methods already capture the argument name via `[CallerArgumentExpression]`). Converted `UlidIdentity.Parse(string?, string)` to `(string?, [CallerArgumentExpression(nameof(value))] string? paramName = null)` and dropped the explicit `nameof(value)` argument from the six identity wrappers (`CorrelationId`, `CausationId`, `OperationId`, `ProjectId`, `RunId`, `TenantId`). 911/911 tests pass; build clean (0 warnings).

- Extended Kernel's SonarQube Cloud wiring to also run on `push` to `main` (ADR-0011 D11 + SonarCloud's PR-vs-main-branch analysis model). The `sonarcloud` job's `if:` now accepts both events: PR-time analysis (after `pr-core` succeeds, same-repo only) and main-branch analysis (standalone, populates the SonarCloud Overview dashboard and the leak-period baseline). `always()` is required in the if so the job evaluates even when `pr-core` is skipped on push events. Follow-up to the initial onboarding PR #59; brings Kernel into alignment with the other 11 fan-out PRs.
- Onboarded Kernel to SonarQube Cloud (ADR-0011 D11). Wired a `sonarcloud` job in `pr.yml` that calls `HoneyDrunkStudios/HoneyDrunk.Actions/.github/workflows/job-sonarcloud.yml` after `pr-core`. Per-project source/test classification is discovered automatically from MSBuild `IsTestProject` properties; per-repo Sonar overrides can be added later via `Directory.Build.props` `<SonarQubeSetting>` items or as new inputs to `job-sonarcloud.yml`. SonarQube Cloud quality gate runs against PRs; branch-protection requirement added separately after the first successful run.
- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for Kernel PRs and added PR metadata placeholders for authorship, packet scope, and out-of-band review declarations.
- Adopted HoneyDrunk.Standards.Tests 0.2.9 for Kernel tests, refreshed HoneyDrunk.Standards to 0.2.9, and removed Thread.Sleep-based idempotency checks for ADR-0047 testing alignment.
- Backfilled Kernel identity unit-test coverage, wired the PR workflow for the coverage gate/baseline ratchet, and seeded `.github/coverage-baseline.json`; total line coverage now measures 72.0% under the Grid PR coverage gate filter.
- Migrated Kernel unit tests to AwesomeAssertions per ADR-0047 test-tooling standards.

---

## [0.7.0] - 2026-05-18

### Added

- Added `GridContextSnapshot` in Abstractions so downstream libraries can create initialized context snapshots without referencing the Kernel runtime package.

### Changed

- Consolidated duplicated HTTP header/correlation/baggage extraction logic used by `HttpContextMapper` and `GridContextMiddleware`.
- Consolidated repeated ULID and kebab-case identity parsing/validation helper logic across Kernel identity types.

### Package Versions

- `HoneyDrunk.Kernel.Abstractions` → `0.7.0`
- `HoneyDrunk.Kernel` → `0.7.0`

---

## [0.6.0] - 2026-05-17

### ⚠️ BREAKING CHANGES

- `WellKnownNodes` now exposes canonical `honeydrunk-*` Node IDs instead of short local aliases such as `kernel`, `transport`, `vault`, `auth`, `web-rest`, `data`, and `pulse`.
- `WellKnownNodes.Ops.Pulse` now resolves to `honeydrunk-pulse`, matching deploy-time `HONEYDRUNK_NODE_ID` and App Configuration label usage.

### Added

- Added missing well-known Node IDs for Vault.Rotation, Audit, Communications, Notify, Actions, Architecture, Studios, Lore, and the current AI-sector Nodes.
- Added tests that pin every well-known Node ID to its canonical Grid identity and verify uniqueness/validity.

### Changed

- Replaced deprecated `xunit` v2 package usage in the test project with `xunit.v3` and refreshed test infrastructure packages.

### Package Versions

- `HoneyDrunk.Kernel.Abstractions` → `0.6.0`
- `HoneyDrunk.Kernel` → `0.6.0`

---

## [0.5.0] - 2026-05-04

### ⚠️ BREAKING CHANGES

ADR-0026 promotes tenant identity from string propagation to a first-class Grid primitive.

- `IGridContext.TenantId` and `IOperationContext.TenantId` are now non-nullable `TenantId` values instead of `string?`.
- HTTP, messaging, job, and serializer entry points now parse tenant wire values as ULIDs and reject malformed tenant IDs.
- Missing tenant values default to `TenantId.Internal` (`00000000000000000000000000`) at Grid entry.

### Added

- `TenantId.Internal` and `TenantId.IsInternal` in `HoneyDrunk.Kernel.Abstractions.Identity`.
- Tenancy contracts in `HoneyDrunk.Kernel.Abstractions.Tenancy`:
  - `ITenantRateLimitPolicy`
  - `TenantRateLimitDecision`
  - `TenantRateLimitOutcome`
  - `IBillingEventEmitter`
  - `BillingEvent`
- Default runtime implementations in `HoneyDrunk.Kernel.Tenancy`:
  - `NoopTenantRateLimitPolicy`
  - `NoopBillingEventEmitter`

### Changed

- `GridContext`, `OperationContext`, mappers, middleware, transport binders, and `GridContextSerializer` now use strong `TenantId` propagation.
- Outbound transport binders omit `X-Tenant-Id` for `TenantId.Internal` and emit it for external tenants.
- `AddHoneyDrunkNode()` registers no-op tenancy services by default while allowing consumer Nodes to override them.

### Package Versions

- `HoneyDrunk.Kernel.Abstractions` → `0.5.0`
- `HoneyDrunk.Kernel` → `0.5.0`

---

## [0.4.0] - 2026-01-19

### ⚠️ BREAKING CHANGES

This release contains significant breaking changes to unify GridContext ownership and eliminate architectural gaps identified during scenario-driven verification.

**GridContext Ownership Model**
- GridContext is now created by DI scope and initialized by middleware/mappers
- Removed multi-argument constructors from `GridContext` - now takes only `(nodeId, studioId, environment)`
- Added `Initialize()` method for setting request-specific values (correlationId, causationId, etc.)
- Context throws `InvalidOperationException` if accessed before initialization
- Context throws `ObjectDisposedException` if accessed after scope ends

**IGridContext Interface**
- Removed `BeginScope()` method entirely (was a no-op placeholder)
- Removed `WithBaggage()` method - replaced with `AddBaggage()` (void, mutates in place)
- Added `IsInitialized` property to check initialization state

**IGridContextAccessor Interface**
- Changed from `IGridContext? GridContext { get; set; }` to `IGridContext GridContext { get; }` (read-only)
- Accessor now reads from `HttpContext.RequestServices`, not independent `AsyncLocal`
- Throws `InvalidOperationException` when accessed outside valid scope

**IGridContextFactory Interface**
- Removed `CreateRoot()` method - root contexts are now created by DI only
- `CreateChild()` remains for cross-node propagation scenarios

**Context Mappers (Now Static)**
- `HttpContextMapper` is now static with `ExtractFromHttpContext()` and `InitializeFromHttpContext()` methods
- `JobContextMapper` is now static with `InitializeForJob()`, `InitializeForScheduledJob()`, `InitializeFromMetadata()` methods
- `MessagingContextMapper` is now static with `InitializeFromMessage()` and `ExtractFromMessage()` methods

**Service Registration**
- Added duplicate registration guard - calling `AddHoneyDrunkNode()` twice now throws `InvalidOperationException`
- Added `FrameworkReference` to `Microsoft.AspNetCore.App` for HTTP context support

### 🎯 Architectural Improvements

**Unified Context Ownership**
- DI scope now owns the single GridContext instance
- Middleware initializes existing scoped context instead of creating new ones
- GridContextAccessor mirrors DI scope - never diverges

**Fail-Fast Behavior**
- Accessing uninitialized context properties throws immediately
- Accessing disposed context throws `ObjectDisposedException`
- Missing middleware configuration fails loudly at first access

**Fire-and-Forget Detection**
- `GridContext.MarkDisposed()` called when scope ends
- Background work that incorrectly holds context references will fail
- Forces explicit context creation for background scenarios

### 📝 Migration Guide

**Updating GridContext Creation**
```csharp
// Old (v0.3)
var context = new GridContext("corr-id", "node", "studio", "env", causationId: "cause");

// New (v0.4)
var context = new GridContext("node", "studio", "env");
context.Initialize(correlationId: "corr-id", causationId: "cause");
```

**Updating Baggage**
```csharp
// Old (v0.3) - immutable, returns new instance
context = context.WithBaggage("key", "value");

// New (v0.4) - mutable, modifies in place
context.AddBaggage("key", "value");
```

**Updating Mappers**
```csharp
// Old (v0.3) - instance-based
var mapper = new HttpContextMapper("node", "studio", "env");
var context = mapper.MapFromHttpContext(httpContext);

// New (v0.4) - static, initializes existing context
var values = HttpContextMapper.ExtractFromHttpContext(httpContext);
// Or directly initialize:
HttpContextMapper.InitializeFromHttpContext(gridContext, httpContext);
```

---

## [0.3.0] - 2025-11-28

### 🎯 Major Features

**Transport Abstraction**
- Added `ITransportEnvelopeBinder` for protocol-agnostic context propagation
- Implemented HTTP, messaging, and job transport binders
- Standardized `GridHeaderNames` for consistent header naming

**Error Handling**
- Introduced structured exception hierarchy with `HoneyDrunkException` base
- Added typed exceptions (Validation, NotFound, Security, Concurrency, DependencyFailure)
- Implemented `ErrorCode` with well-known taxonomy
- Added `IErrorClassifier` for automatic HTTP status mapping

**Agent Interoperability**
- Added `AgentsInterop` static helpers for agent execution
- Implemented `GridContextSerializer` with automatic secret filtering
- Added `AgentResultSerializer` for cross-process agent execution
- Implemented `AgentContextProjection` for context composition

**Telemetry & OpenTelemetry**
- Added `GridActivitySource` for OpenTelemetry Activity API integration
- Implemented `TelemetryTags` for standardized tag names
- Added activity helpers (StartActivity, StartHttpActivity, StartDatabaseActivity, StartMessageActivity)
- Enhanced trace enrichment and log correlation

**Middleware & Bootstrapping**
- Added `GridContextMiddleware` for automatic HTTP context establishment
- Implemented `UseGridContext()` middleware extension
- Added `AddHoneyDrunkNode()` unified bootstrapping
- Implemented `ValidateHoneyDrunkServices()` for fail-fast validation

**Multi-Tenant Support**
- Enhanced `IGridContext` with `TenantId` and `ProjectId` properties
- Added multi-tenant identity propagation across transport boundaries
- Updated all context mappers and binders for tenant/project awareness

### 📚 Documentation

**New Documentation Guides**
- **Bootstrapping.md** - Unified Node initialization patterns
- **OpenTelemetry.md** - Distributed tracing with Activity API
- **Transport.md** - Context propagation across boundaries
- **Errors.md** - Exception hierarchy and error handling

**Updated Documentation**
- Complete rewrite of root README.md
- Complete rewrite of package READMEs (Abstractions, Kernel)
- Enhanced **Implementations.md** with all v0.3 implementations
- Enhanced **Agents.md** with AgentsInterop section
- Enhanced **Context.md** with multi-tenant context model
- Enhanced **Testing.md** with v0.3 patterns
- Updated **FILE_GUIDE.md** as canonical architecture reference

### 🏗️ Architecture

**Context Model Evolution**
- Maintained three-tier model (Grid → Node → Operation)
- Enhanced GridContext with multi-tenant identity
- Clarified runtime uses strings for performance (not value objects)

**Transport Layer**
- Unified context propagation across HTTP, messaging, and job transports
- Standardized header naming with `GridHeaderNames`
- Automatic context extraction and injection

**Error Taxonomy**
- Hierarchical error codes (Grid, Node, Operation scopes)
- Automatic HTTP status code mapping
- Retry policy determination via `IErrorClassifier`

### 🔧 Breaking Changes

**None** - v0.3.0 is additive on top of v0.2.x. All existing code continues to work.

### 📦 Package Versions

- `HoneyDrunk.Kernel.Abstractions` → 0.3.0
- `HoneyDrunk.Kernel` → 0.3.0

---

## [0.2.1] - 2025-11-22

### Fixed
- Fixed README emoji encoding issues in both packages
- Corrected package metadata for proper NuGet.org display

---

## [0.2.0] - 2025-11-21

### 🎯 Major Refactor - Semantic OS Layer

**Three-Tier Context Model**
- Introduced `IGridContext`, `INodeContext`, `IOperationContext`
- Implemented context accessors and factories
- Added causation chain support

**Identity Primitives**
- Added strongly-typed IDs (NodeId, CorrelationId, TenantId, ProjectId, RunId)
- Implemented ULID-based correlation

**Lifecycle Orchestration**
- Added startup/shutdown hooks
- Implemented health and readiness contributors
- Added lifecycle stage management

**Configuration & Secrets**
- Introduced hierarchical configuration abstractions
- Added secrets source abstraction with fallback

**Agents & Hosting**
- Added agent execution framework abstractions
- Implemented Node descriptor and capability contracts

### Breaking Changes
- Removed `IClock` / `ISystemClock` (use BCL directly)
- Removed `IIdGenerator` (use BCL `Guid.NewGuid()` or `Ulid.NewUlid()`)
- Removed `ILogSink` (use `ILogger<T>`)

### Package Versions
- `HoneyDrunk.Kernel.Abstractions` → 0.2.0
- `HoneyDrunk.Kernel` → 0.2.0

---

## [0.1.2] - 2025-11-20

### Changed
- Updated to .NET 10 dependencies
- Updated Microsoft.Extensions.* packages to 10.0.0
- Updated analyzers and Standards package

---

## [0.1.1] - Initial Foundation

### Added
- Initial abstractions and runtime implementations
- Basic context propagation
- Diagnostics and health monitoring

---

## [0.1.0] - Genesis

### Added
- Repository initialization
- Core project structure
- Basic kernel abstractions

---

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
