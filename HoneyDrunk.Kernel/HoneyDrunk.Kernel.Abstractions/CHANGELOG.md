# Changelog

All notable changes to HoneyDrunk.Kernel.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
