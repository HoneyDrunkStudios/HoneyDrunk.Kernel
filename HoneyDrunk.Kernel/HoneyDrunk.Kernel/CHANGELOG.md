# Changelog

All notable changes to HoneyDrunk.Kernel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2025-11-28

### Added
- **Transport Binders**: Protocol-agnostic context propagation implementations
  - `HttpResponseBinder` for HTTP response headers
  - `MessagePropertiesBinder` for messaging (RabbitMQ, Azure Service Bus)
  - `JobMetadataBinder` for background jobs (Hangfire, Quartz)
- **Context Mappers**: Enhanced mappers for extracting context from transport envelopes
  - `HttpContextMapper` with `GridHeaderNames` support
  - `MessagingContextMapper` for message properties
  - `JobContextMapper` for job metadata
- **Error Handling**: `DefaultErrorClassifier` for HTTP status mapping and retry policies
- **AgentsInterop**: Static helper classes for agent execution and serialization
  - `AgentContextProjection` for context composition
  - `GridContextSerializer` with automatic secret filtering
  - `AgentResultSerializer` for cross-process agent execution
- **Middleware**: `GridContextMiddleware` for automatic HTTP request context establishment
  - Extracts correlation, causation, and baggage from headers
  - Creates GridContext and OperationContext automatically
  - Echoes correlation headers in responses
- **Bootstrapping**: Unified Node initialization
  - `AddHoneyDrunkGrid(options)` for service registration
  - `UseGridContext()` middleware extension
  - `ValidateHoneyDrunkServices()` for fail-fast validation
- **Telemetry**: `GridActivitySource` for OpenTelemetry Activity API integration
  - `StartActivity()` with automatic Grid context enrichment
  - `StartHttpActivity()` for HTTP operations
  - `StartDatabaseActivity()` for database operations
  - `StartMessageActivity()` for messaging operations
  - `RecordException()` and `SetSuccess()` helpers

### Changed
- **Context Enhancement**: GridContext now supports `TenantId` and `ProjectId` for multi-tenant scenarios
- **Lifecycle**: Enhanced `NodeLifecycleManager` with health/readiness aggregation
- **Configuration**: Improved `StudioConfiguration` with environment-aware settings
- **Documentation**: Complete rewrite of package README to focus on runtime implementations

### Improved
- **Context Propagation**: Automatic context flow through middleware and transport layers
- **Observability**: Standardized telemetry tags and trace enrichment
- **Health Monitoring**: Enhanced health check aggregation and readiness probes
- **Validation**: Comprehensive service registration validation at startup

### Performance
- **Zero Allocations**: `NoOpMetricsCollector` has no runtime overhead when metrics disabled
- **AsyncLocal Optimization**: Context accessors use efficient AsyncLocal storage

## [0.2.1] - 2025-11-22

### Fixed
- Fixed README emoji encoding issues (replaced malformed `??` with proper Unicode emojis)
- Corrected package metadata to ensure README displays correctly on NuGet.org

### Changed
- Updated README with clearer emoji icons and improved documentation structure

## [0.2.0] - 2025-11-21

### Added
- **GridContext implementation**: Default implementation with causation chain support
- **NodeContext implementation**: Process-scoped Node identity tracking
- **OperationContext implementation**: Operation timing and outcome tracking
- **GridContextAccessor**: AsyncLocal-based context accessor
- **Context Mappers**: `HttpContextMapper`, `JobContextMapper`, `MessagingContextMapper`
- **NodeLifecycleManager**: Coordinates startup/shutdown with hook execution
- **NodeLifecycleHost**: Hosts Node lifecycle with health/readiness monitoring
- **StudioConfiguration**: Studio-wide configuration implementation
- **CompositeSecretsSource**: Chains multiple secret sources with fallback
- **CompositeHealthCheck**: Aggregates multiple health checks
- **NoOpMetricsCollector**: Zero-overhead metrics placeholder
- **DependencyInjection**: `AddHoneyDrunkCore`, `AddHoneyDrunkCoreNode` extensions
- **Validation**: Service provider validation at startup

### Removed
- **Breaking**: Removed `SystemClock` implementation (use BCL directly)
- **Breaking**: Removed `UlidGenerator` implementation (use `Ulid.NewUlid()` directly)
- **Breaking**: Removed `ConsoleLogSink` (use `ILogger<T>` directly)

### Changed
- **Breaking**: Major refactor as semantic OS layer runtime
- Renamed and restructured all implementations to match new three-tier context model
- Updated package description to reflect semantic OS layer purpose
- Improved logging and telemetry integration throughout
- Enhanced health check and readiness contributor patterns

## [0.1.2] - 2025-11-20

### Changed
- Updated Microsoft.Extensions.* packages to 10.0.0
- Updated Microsoft.CodeAnalysis.NetAnalyzers to 10.0.100
- Updated HoneyDrunk.Standards to 0.2.3
- Updated Ulid package to 1.4.1

## [0.1.1] - Previous Release

### Added
- Default implementations of kernel abstractions
- SystemClock, UlidGenerator, KernelContext
- CompositeHealthCheck and secrets management

## [0.1.0] - Initial Release

### Added
- Foundational runtime implementations for HoneyDrunk.OS
