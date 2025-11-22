# Changelog

All notable changes to HoneyDrunk.Kernel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
