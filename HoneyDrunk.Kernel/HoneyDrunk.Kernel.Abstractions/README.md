# HoneyDrunk.Kernel.Abstractions

[![NuGet](https://img.shields.io/nuget/v/HoneyDrunk.Kernel.Abstractions.svg)](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Pure Semantic Contracts for the HoneyDrunk Grid** - Zero-dependency abstractions that define the OS layer for distributed systems.

## 📋 What Is This?

**HoneyDrunk.Kernel.Abstractions** defines the **semantic OS contracts** for the entire HoneyDrunk.OS Grid. This package contains only interfaces, value types, and strongly-typed primitives—no concrete implementations, no middleware, no lifecycle hosts.

**Key Characteristics:**
- ✅ **Zero runtime dependencies** (only .NET BCL + Microsoft.Extensions.* abstractions)
- ✅ **Safe for libraries** - Reference from shared code without pulling in heavy runtimes
- ✅ **Stable contracts** - Semantic versioning with additive-only minor changes
- ✅ **Grid-first design** - Multi-tenant, observable, distributed by default

This is the package you reference when building **libraries, SDKs, or custom implementations** that need to understand Grid primitives without taking on implementation weight.

## 📦 What's Inside

### 🔑 Identity
**Strongly-typed, validated identifiers** for distributed systems:

**Core IDs:**
- `NodeId` - Kebab-case validated Node identifiers
- `SectorId` - Logical grouping of Nodes (Core, Ops, AI, Market, etc.)
- `EnvironmentId` - Environment discrimination (Production, Staging, Development, etc.)

**Correlation & Tracking:**
- `CorrelationId` - ULID-based request correlation (trace-id)
- `CausationId` - Parent-child operation tracking (parent-span-id)
- `TenantId` - Multi-tenant isolation boundaries
- `ProjectId` - Project/workspace organization
- `RunId` - Execution instance tracking

All IDs are **validated at construction time** and designed for safe serialization across transport boundaries.

### 🏷️ Identity Registries
**Static, discoverable registries** of well-known Grid values:

- `Nodes.*` - Real Nodes from the Grid catalog (Core.Kernel, Ops.Pulse, AI.AgentKit, Market.Arcadia, etc.)
- `Sectors.*` - Canonical sector taxonomy (Core, Ops, AI, Market, Data, Messaging, Storage, Web, Auth)
- `Environments.*` - Standard environment names (Production, Staging, Development, Testing, Performance, Integration, Local)
- `ErrorCode.*` - Well-known error codes for Grid exceptions

**Benefits:** IntelliSense discovery, compile-time validation, IDE refactoring support, consistent naming across Nodes.

### 🌐 Context
**Three-tier context model** that flows through async boundaries:

**Grid Context (Distributed):**
- `IGridContext` - Per-operation envelope that crosses Node boundaries
  - Carries `CorrelationId` (trace-id), `CausationId` (parent-span-id), `NodeId`, `StudioId`, `Environment`
  - Baggage for metadata propagation
  - `CreateChildContext()` for causality tracking

**Node Context (Static):**
- `INodeContext` - Per-process Node identity
  - Static metadata (NodeId, Version, StudioId, Environment, LifecycleStage)
  - Process info (MachineName, ProcessId, StartedAtUtc)

**Operation Context (Scoped):**
- `IOperationContext` - Per-unit-of-work tracking
  - Timing (StartedAtUtc, CompletedAtUtc)
  - Outcome (IsSuccess, ErrorMessage)
  - Metadata tags

**Accessors & Factories:**
- `IGridContextAccessor` - Ambient context access (AsyncLocal-based)
- `IOperationContextAccessor` - Ambient operation context
- `IOperationContextFactory` - Creates operation contexts from Grid context

### ⚙️ Configuration & Secrets
**Hierarchical configuration abstractions:**
- `IStudioConfiguration` - Studio-level configuration access
- `ISecretsSource` - Secret retrieval abstraction (Vault, Key Vault, env vars)

**Design Note:** Full hierarchical scoping (Studio → Node → Tenant) is a design target. Current abstractions focus on Studio and Node levels, with Vault providing secure backing.

### 🏢 Hosting
**Node hosting and discovery contracts:**
- `INodeDescriptor` - Node identity, version, capabilities, dependencies
- `INodeCapability` - Advertised capabilities (protocols, endpoints, schemas)
- `INodeManifest` - Deployment manifest metadata
- `IStudioConfiguration` - Studio environment settings

### 🤖 Agents
**AI agent execution framework abstractions:**
- `IAgentDescriptor` - Agent identity, capabilities, and constraints
- `IAgentExecutionContext` - Scoped context for agent operations
- `IAgentCapability` - Advertised agent skills and permissions

**Design Note:** Agent interop helpers (serialization, context projection) live in the runtime package (`HoneyDrunk.Kernel`), not here.

### 🔄 Lifecycle & Health
**Node lifecycle orchestration contracts:**

**Lifecycle Hooks:**
- `INodeLifecycle` - Node-level start/stop coordination
- `IStartupHook` - Priority-ordered initialization logic
- `IShutdownHook` - Priority-ordered cleanup logic

**Health & Readiness:**
- `IHealthCheck` - Service health monitoring contract
- `HealthStatus` - Standardized health status (Healthy, Degraded, Unhealthy)
- `IHealthContributor` - Composite health aggregation
- `IReadinessContributor` - Kubernetes-ready readiness probes

**Lifecycle Stages:**
- `NodeLifecycleStage` - Enumeration of Node states (Initializing, Starting, Ready, Degraded, Stopping, Stopped, Failed)

### 📡 Telemetry & Diagnostics
**Observability abstractions** for OpenTelemetry integration:

**Telemetry:**
- `ITelemetryContext` - Correlation and trace context
- `ITraceEnricher` - Automatic trace enrichment hooks
- `ILogScopeFactory` - Structured log scope creation
- `TelemetryTags` - Standard tag names for Grid telemetry

**Diagnostics:**
- `IMetricsCollector` - Metrics abstraction (counters, histograms, gauges)

**Design Note:** Concrete telemetry helpers (GridActivitySource, OpenTelemetry wiring) live in the runtime package.

### 🚚 Transport
**Protocol-agnostic context propagation:**
- `ITransportEnvelopeBinder` - Bind GridContext to outgoing transport envelopes
- `GridHeaderNames` - Standard header names (`X-Correlation-Id`, `X-Causation-Id`, `X-Node-Id`, etc.)

**Design Note:** Concrete binders (HTTP, messaging, jobs) and context mappers live in the runtime package.

### ⚠️ Errors
**Structured exception hierarchy:**
- `HoneyDrunkException` - Base exception with Grid identity propagation
- `ErrorCode` - Strongly-typed error codes with well-known taxonomy
- `IErrorClassifier` - Maps exceptions to HTTP status codes and retry policies

**Typed Exceptions:**
- `ValidationException`, `NotFoundException`, `SecurityException`, `ConcurrencyException`, `DependencyFailureException`

**Design Note:** All exceptions carry `CorrelationId` for distributed tracing.

### 💉 Dependency Injection
**Modular service registration:**
- `IModule` - Composable DI registration units

## 📥 Installation

```bash
dotnet add package HoneyDrunk.Kernel.Abstractions
```

```xml
<ItemGroup>
  <PackageReference Include="HoneyDrunk.Kernel.Abstractions" Version="0.3.0" />
</ItemGroup>
```

**Note:** Version shown is an example. Check [NuGet](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions/) for latest version.

## 🎯 When to Use This Package

### Use HoneyDrunk.Kernel.Abstractions when:
- ✅ **Building libraries or SDKs** that integrate with the Grid
- ✅ **Creating shared code** that multiple Nodes consume
- ✅ **Defining custom implementations** of Kernel interfaces
- ✅ **You want minimal transitive dependencies** (no runtime, no middleware, no lifecycle hosts)
- ✅ **Writing test doubles** or alternative implementations

### Use HoneyDrunk.Kernel (runtime) when:
- ✅ **Building executable Nodes/services**
- ✅ **You need concrete implementations** (GridContext, NodeContext, OperationContext)
- ✅ **You need middleware** (UseGridContext, lifecycle orchestration)
- ✅ **You need transport binders** (HTTP, messaging, jobs)
- ✅ **You need bootstrapping helpers** (AddHoneyDrunkGrid, ValidateHoneyDrunkServices)

## 🎨 Design Philosophy

### Minimal Dependencies
This package depends only on:
- ✅ **.NET 10 BCL** - Standard library only
- ✅ **Microsoft.Extensions.*** - DI, Configuration, Hosting abstractions (contracts only)
- ✅ **Ulid** - ULID generation for identity types
- ✅ **HoneyDrunk.Standards** - Build-time analyzers (no runtime dependency)

### Stable Contracts
Interfaces follow **strict semantic versioning**:
- 🔒 **Breaking changes** → Major versions only
- ➕ **Additive changes** → Minor versions (new interfaces, optional members)
- 🐛 **Bug fixes** → Patch versions

### Grid-First Design
All abstractions assume:
- 🌐 **Distributed systems** - Context propagates across Node boundaries
- 🏢 **Multi-tenancy** - TenantId and ProjectId are first-class
- 📊 **Observable by default** - Correlation, causation, and baggage built-in
- 🔒 **Strongly-typed identity** - Compile-time validation, runtime safety

### Explicit Over Implicit
- ✅ **No magic** - Context is explicitly passed or accessed via accessor
- ✅ **No ambient state** - AsyncLocal storage is opt-in via accessors
- ✅ **No reflection tricks** - Interfaces define behavior, implementations execute

## 💡 Example: Custom Implementations

### Custom Secrets Source

```csharp
using HoneyDrunk.Kernel.Abstractions.Configuration;

/// <summary>
/// Retrieves secrets from environment variables with a prefix.
/// </summary>
public class EnvironmentSecretsSource : ISecretsSource
{
    private const string Prefix = "SECRET_";
    
    public bool TryGetSecret(string key, out string? value)
    {
        var envKey = $"{Prefix}{key}";
        value = Environment.GetEnvironmentVariable(envKey);
        return value is not null;
    }
}

// Usage (registration in runtime):
builder.Services.AddSingleton<ISecretsSource, EnvironmentSecretsSource>();
```

### Custom Health Check

```csharp
using HoneyDrunk.Kernel.Abstractions.Health;

/// <summary>
/// Checks database connectivity.
/// </summary>
public class DatabaseHealthCheck(IDbConnection db) : IHealthCheck
{
    public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.ExecuteScalarAsync("SELECT 1", cancellationToken);
            return HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            return HealthStatus.Unhealthy;
        }
    }
}

// Usage (registration in runtime):
builder.Services.AddSingleton<IHealthCheck, DatabaseHealthCheck>();
```

### Custom Transport Binder

```csharp
using HoneyDrunk.Kernel.Abstractions.Transport;
using HoneyDrunk.Kernel.Abstractions.Context;

/// <summary>
/// Binds GridContext to gRPC metadata.
/// </summary>
public class GrpcMetadataBinder : ITransportEnvelopeBinder
{
    public void Bind(object envelope, IGridContext gridContext)
    {
        if (envelope is not Grpc.Core.Metadata metadata)
            return;
        
        metadata.Add(GridHeaderNames.CorrelationId, gridContext.CorrelationId);
        metadata.Add(GridHeaderNames.CausationId, gridContext.CausationId ?? string.Empty);
        metadata.Add(GridHeaderNames.NodeId, gridContext.NodeId);
        metadata.Add(GridHeaderNames.StudioId, gridContext.StudioId);
        metadata.Add(GridHeaderNames.Environment, gridContext.Environment);
        
        foreach (var (key, value) in gridContext.Baggage)
        {
            metadata.Add($"{GridHeaderNames.BaggagePrefix}{key}", value);
        }
    }
}

// Usage (registration in runtime):
builder.Services.AddSingleton<ITransportEnvelopeBinder, GrpcMetadataBinder>();
```

**Note:** These examples show **contract usage only**. Actual registration, middleware wiring, and lifecycle orchestration happen in the `HoneyDrunk.Kernel` runtime package.

## 🔗 Related Packages

- **[HoneyDrunk.Kernel](https://www.nuget.org/packages/HoneyDrunk.Kernel/)** - Runtime implementations (use this for executable Nodes)
- **[HoneyDrunk.Standards](https://www.nuget.org/packages/HoneyDrunk.Standards/)** - Analyzers and coding conventions

## 📚 Documentation

**See the main repository for comprehensive documentation:**

**Core Concepts:**
- **[Complete File Guide](../docs/FILE_GUIDE.md)** - Architecture reference (START HERE)
- **[Identity Guide](../docs/Identity.md)** - Strongly-typed identifiers
- **[Identity Registries](../docs/IdentityRegistries.md)** - Static well-known values *(planned)*
- **[Context Guide](../docs/Context.md)** - Three-tier context propagation

**Domain Guides:**
- **[Lifecycle Guide](../docs/Lifecycle.md)** - Lifecycle orchestration contracts
- **[Telemetry Guide](../docs/Telemetry.md)** - Observability primitives
- **[Transport Guide](../docs/Transport.md)** - Context propagation across boundaries
- **[Errors Guide](../docs/Errors.md)** - Exception hierarchy and error handling
- **[Agents Guide](../docs/Agents.md)** - Agent execution framework
- **[Health Guide](../docs/Health.md)** - Health monitoring contracts

## 📄 License

This project is licensed under the [MIT License](../LICENSE).

---

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](../docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
