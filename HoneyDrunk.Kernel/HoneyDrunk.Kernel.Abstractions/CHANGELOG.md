# Changelog

All notable changes to HoneyDrunk.Kernel.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-01-19

### ⚠️ Breaking Changes

- **IGridContext**: Removed `BeginScope()` method (was no-op placeholder API)
- **IGridContext**: Removed `WithBaggage(key, value)` method - replaced with `AddBaggage(key, value)` (void, mutates in place)
- **IGridContext**: Added `IsInitialized` property to check initialization state
- **IGridContextAccessor**: Changed from `IGridContext? GridContext { get; set; }` to `IGridContext GridContext { get; }` (non-nullable, read-only)
- **IGridContextFactory**: Removed `CreateRoot()` method - root contexts are now DI-scoped only

### Added

- **IGridContext.IsInitialized**: Property indicating whether context has been initialized with request-specific values
- **IGridContext.AddBaggage()**: Mutable method to add baggage items to existing context

### Changed

- **IGridContext**: Documentation updated to reflect single-instance-per-scope ownership model
- **IGridContextAccessor**: Documentation updated to reflect read-only accessor pattern
- **IGridContextFactory**: Documentation updated to clarify CreateChild() is for cross-node propagation only

### Removed

- **IGridContext.BeginScope()**: Removed placeholder API that returned no-op disposable
- **IGridContext.WithBaggage()**: Replaced with mutable `AddBaggage()` to match scoped ownership model
- **IGridContextFactory.CreateRoot()**: Root context creation moved to DI container responsibility

## [0.3.0] - 2025-11-28

### Added
- **Transport Abstraction**: `ITransportEnvelopeBinder` for protocol-agnostic context propagation
- **Error Handling**: Structured exception hierarchy with `HoneyDrunkException` base
  - `ValidationException`, `NotFoundException`, `SecurityException`, `ConcurrencyException`, `DependencyFailureException`
  - `ErrorCode` with well-known taxonomy and custom error code support
  - `IErrorClassifier` for automatic HTTP status mapping and retry policy determination
- **Standard Headers**: `GridHeaderNames` constants for consistent HTTP/messaging headers
  - `X-Correlation-Id`, `X-Causation-Id`, `X-Node-Id`, `X-Studio-Id`, `X-Environment`
  - `X-Tenant-Id`, `X-Project-Id` for multi-tenant scenarios
  - `X-Baggage-*` prefix for metadata propagation
  - W3C `traceparent` and `baggage` support
- **Telemetry Tags**: `TelemetryTags` constants for standard OpenTelemetry tag names
  - Correlation, causation, node, studio, environment tags
  - Tenant and project tags for multi-tenant observability
- **Operation Context Extensions**: Enhanced `IOperationContext` with metadata and timing
- **Grid Context Extensions**: Multi-tenant identity support (`TenantId`, `ProjectId`)

### Changed
- **Context Model Enhancement**: GridContext now includes `TenantId` and `ProjectId` for multi-tenant scenarios
- **Documentation**: Complete rewrite of package README to focus on contracts vs runtime
- **Telemetry Integration**: Standardized tag names across all telemetry abstractions

### Improved
- **Identity Primitives**: Enhanced validation and serialization support
- **Configuration Abstractions**: Clarified hierarchical scoping design (Studio → Node)
- **Agent Contracts**: Improved agent execution context with scoped permissions

## [0.2.1] - 2025-11-22

### Fixed
- Fixed README emoji encoding issues (replaced malformed `??` with proper Unicode emojis)
- Corrected package metadata to ensure README displays correctly on NuGet.org

### Changed
- Updated README with clearer emoji icons for better readability

## [0.2.0] - 2025-11-21

### Added
- **Three-tier context model**: `IGridContext`, `INodeContext`, `IOperationContext`
- **Strongly-typed Identity primitives**: `NodeId`, `CorrelationId`, `TenantId`, `ProjectId`, `RunId`
- **Lifecycle contracts**: `IStartupHook`, `IShutdownHook`, `IHealthContributor`, `IReadinessContributor`
- **Configuration abstractions**: Hierarchical configuration with scope fallback
- **Hosting abstractions**: `INodeDescriptor`, `INodeCapability`, `INodeManifest`
- **Agent contracts**: `IAgentExecutionContext`, `IAgentDescriptor`, `IAgentCapability`
- **Telemetry contracts**: `ITraceEnricher`, `ILogScopeFactory`, `ITelemetryContext`
- **Secrets management**: `ISecretsSource` with fallback support
- **Health monitoring**: `IHealthCheck`, `HealthStatus`
- **Diagnostics**: `IMetricsCollector` for counters, histograms, gauges

### Removed
- **Breaking**: Removed `IClock` / `ISystemClock` (use BCL `DateTime.UtcNow` / `DateTimeOffset.UtcNow`)
- **Breaking**: Removed `IIdGenerator` (use BCL `Guid.NewGuid()` or `Ulid.NewUlid()`)
- **Breaking**: Removed `ILogSink` (use `Microsoft.Extensions.Logging.ILogger<T>`)

### Changed
- **Breaking**: Major refactor as semantic OS layer for HoneyDrunk.OS
- Package now focuses on Grid-specific abstractions, not general BCL wrappers
- Updated package description to reflect semantic OS layer purpose
- Improved XML documentation across all interfaces

## [0.1.2] - 2025-11-20

### Changed
- Updated Microsoft.Extensions.* packages to 10.0.0
- Updated Microsoft.CodeAnalysis.NetAnalyzers to 10.0.100
- Updated HoneyDrunk.Standards to 0.2.3

## [0.1.1] - Previous Release

### Added
- Initial release of foundational abstractions and contracts
- Interfaces for dependency injection, configuration, context propagation
- Diagnostics, time, and ID generation primitives

## [0.1.0] - Initial Release

### Added
- Core kernel abstractions for HoneyDrunk.OS
