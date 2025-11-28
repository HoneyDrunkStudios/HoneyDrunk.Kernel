# HoneyDrunk.Kernel - Repository Changelog

All notable changes to the HoneyDrunk.Kernel repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Kernel.Abstractions CHANGELOG](HoneyDrunk.Kernel.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Kernel CHANGELOG](HoneyDrunk.Kernel/CHANGELOG.md)

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
- Added `AddHoneyDrunkGrid()` unified bootstrapping
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
