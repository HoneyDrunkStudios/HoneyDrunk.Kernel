# HoneyDrunk.Kernel - Repository Changelog

All notable changes to the HoneyDrunk.Kernel repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Kernel.Abstractions CHANGELOG](HoneyDrunk.Kernel.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Kernel CHANGELOG](HoneyDrunk.Kernel/CHANGELOG.md)

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
