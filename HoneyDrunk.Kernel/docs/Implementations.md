# 🔧 Implementations - Kernel Runtime

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

This document covers the runtime implementations in `HoneyDrunk.Kernel` project.

**Location:** `HoneyDrunk.Kernel/`

## Key Implementations

### Context
- **GridContext.cs** - Default implementation of IGridContext
- **NodeContext.cs** - Node identity implementation
- **OperationContext.cs** - Operation tracking implementation
- **GridContextAccessor.cs** - Async-local context accessor

### Context Mappers
- **HttpContextMapper.cs** - Maps HTTP headers to GridContext
- **JobContextMapper.cs** - Maps[← Background job metadata
- **MessagingContextMapper.cs** - Maps message properties

### Lifecycle
- **NodeLifecycleManager.cs** - Coordinates startup/shutdown
- **NodeLifecycleHost.cs** - Hosts Node lifecycle

### Diagnostics
- **NoOpMetricsCollector.cs** - Zero-overhead metrics stub
- **NodeLifecycleHealthContributor.cs** - Lifecycle-based health
- **NodeContextReadinessContributor.cs** - Context-based readiness

### Configuration
- **StudioConfiguration.cs** - Studio config implementation

### DependencyInjection
- **HoneyDrunkCoreExtensions.cs** - Core service registration
- **ServiceProviderValidation.cs** - Startup validation

---

## Coming Soon

Full documentation for implementations is in progress.

---

[← Back to File Guide](FILE_GUIDE.md)

