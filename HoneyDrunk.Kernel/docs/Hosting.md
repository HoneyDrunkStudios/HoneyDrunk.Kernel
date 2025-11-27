# 🏢 Hosting - Node Bootstrap and Lifecycle

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- **Bootstrap Configuration**
  - [HoneyDrunkNodeOptions.cs](#honeydrunk-node-optionscs)
  - [HoneyDrunkTelemetryOptions.cs](#honeydrunktelemetryoptionscs)
  - [MultiTenancyMode.cs](#multitenancymodecs)
- **DI Registration**
  - [IHoneyDrunkBuilder.cs](#ihoneydrunkbuildercs)
  - [AddHoneyDrunkNode()](#addhoneydrunknode)
- **Middleware**
  - [UseGridContext()](#usegridcontext)
  - [ValidateHoneyDrunkServices()](#validatehoneydrunkservices)
- **Lifecycle Management**
  - [NodeLifecycleHost.cs](#nodelifecyclehostcs)
- **Service Discovery (Future)**
  - [INodeManifest.cs](#inodemanifestcs)
  - [INodeDescriptor.cs](#inodedescriptorcs)
  - [INodeCapability.cs](#inodecapabilitycs)
  - [INodeManifestSource.cs](#inodemanifestsourcecs)
- **Studio Configuration**
  - [IStudioConfiguration.cs](#istudioconfigurationcs)
- **Complete Bootstrap Example](#complete-bootstrap-example)
- **Testing Patterns](#testing-patterns)
- **Summary](#summary)

---

## Overview

Hosting in HoneyDrunk.Kernel is about **transforming a plain .NET application into a Grid Node**. This involves:

1. **Bootstrap** - Configure Node identity (NodeId, SectorId, Environment) with strong typing
2. **DI Registration** - Wire up context propagation, lifecycle, telemetry via `AddHoneyDrunkNode()`
3. **Middleware** - Establish Grid context from HTTP requests with `UseGridContext()`
4. **Lifecycle** - Coordinate startup hooks, health checks, and graceful shutdown
5. **Discovery (Future)** - Advertise capabilities and discover dependencies dynamically

**Current State (v0.3):**
- ✅ Bootstrap with HoneyDrunkNodeOptions (strongly-typed identity)
- ✅ Context propagation (GridContext, NodeContext, OperationContext)
- ✅ Lifecycle coordination (startup/shutdown hooks)
- ✅ Telemetry integration (OpenTelemetry ActivitySource)
- 🔮 Service discovery (INodeManifest/Descriptor exist but not yet wired)

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/` (contracts), `HoneyDrunk.Kernel/Hosting/` (implementations)

[↑ Back to top](#table-of-contents)

---

## Bootstrap Configuration

### HoneyDrunkNodeOptions.cs

**What it is:** Core bootstrap options for converting an application into a Grid Node.

**Real-world analogy:** Like a passport application - provides identity, credentials, and metadata for Grid participation.

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/HoneyDrunkNodeOptions.cs`

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `NodeId` | NodeId? | ✅ Yes | Strongly-typed Node identifier (validated kebab-case) |
| `SectorId` | SectorId? | ✅ Yes | Sector for logical grouping (Core, Ops, AI, Market, etc.) |
| `StudioId` | string | ✅ Yes | Studio identifier owning this Node |
| `EnvironmentId` | EnvironmentId? | ✅ Yes | Environment (production, staging, development, etc.) |
| `TenancyMode` | MultiTenancyMode | No | Multi-tenancy execution mode (default: SingleTenant) |
| `Version` | string? | No | Semantic version override (defaults to assembly version) |
| `Telemetry` | HoneyDrunkTelemetryOptions | No | Telemetry configuration sub-options |
| `Tags` | Dictionary<string, string> | No | Low-cardinality discovery/filtering tags |

#### Validation

The `Validate()` method enforces required fields at startup:

```csharp
public void Validate()
{
    if (NodeId is null) throw new InvalidOperationException("NodeId is required.");
    if (SectorId is null) throw new InvalidOperationException("SectorId is required.");
    if (EnvironmentId is null) throw new InvalidOperationException("EnvironmentId is required.");
    if (string.IsNullOrWhiteSpace(StudioId)) throw new InvalidOperationException("StudioId is required.");
}
```

#### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register Node with strongly-typed identity
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Market.Arcadia;           // From static registry
    options.SectorId = Sectors.Market;                // From static registry
    options.EnvironmentId = Environments.Development; // From static registry
    options.StudioId = "honeydrunk-studios";
    options.Version = "1.0.0";
    
    // Optional: Configure telemetry
    options.Telemetry.EnableTracing = true;
    options.Telemetry.TraceSamplingRatio = 1.0;
    
    // Optional: Add tags for discovery/routing
    options.Tags["region"] = "us-west-2";
    options.Tags["deployment-slot"] = "blue";
});

var app = builder.Build();

// Validate all services registered correctly
app.Services.ValidateHoneyDrunkServices();

app.UseGridContext(); // Middleware for HTTP context propagation

app.Run();
```

#### Why Strongly-Typed?

**Configuration time (bootstrap) uses structs** for validation:
- ✅ Invalid formats caught at startup, not in production
- ✅ IntelliSense support for well-known values
- ✅ Refactoring safety (IDE renames propagate)

**Runtime (context propagation) uses strings** for performance:
- ✅ No allocation overhead in hot paths
- ✅ Direct serialization to HTTP headers
- ✅ Interop with external systems

See [Context.md](Context.md) for details on the dual type system.

[↑ Back to top](#table-of-contents)

---

### HoneyDrunkTelemetryOptions.cs

**What it is:** Telemetry-related sub-configuration for observability features.

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/HoneyDrunkTelemetryOptions.cs`

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableTracing` | bool | `true` | Distributed tracing instrumentation (OpenTelemetry) |
| `EnableMetrics` | bool | `true` | Metrics collection helpers |
| `EnableLogCorrelation` | bool | `true` | Log correlation scopes (CorrelationId in logs) |
| `TraceSamplingRatio` | double | `1.0` | Trace sampling ratio (0.0 - 1.0) |

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Ops.Pulse;
    // ... other required fields
    
    // Telemetry configuration
    options.Telemetry.EnableTracing = true;
    options.Telemetry.EnableMetrics = true;
    options.Telemetry.EnableLogCorrelation = true;
    options.Telemetry.TraceSamplingRatio = 0.1; // Sample 10% of traces
});
```

#### When to use

- **Production**: Lower sampling ratio (0.1 = 10%) to reduce overhead
- **Staging**: Moderate sampling (0.5 = 50%) for validation
- **Development**: Full sampling (1.0 = 100%) for debugging

[↑ Back to top](#table-of-contents)

---

### MultiTenancyMode.cs

**What it is:** Enum defining multi-tenancy execution strategy for a Node.

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/MultiTenancyMode.cs`

#### Values

| Value | Description | Use Case |
|-------|-------------|----------|
| `SingleTenant` | All requests implicitly scoped to one Studio/Tenant | Most Nodes (default) |
| `PerRequest` | Explicit tenant resolution per request (header/token) | Multi-tenant SaaS Nodes |
| `ProjectSegmented` | Segmented by project/workspace inside a tenant | Workspace-isolated Nodes |

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Market.Arcadia;
    // ... other fields
    
    // Default: SingleTenant (most common)
    options.TenancyMode = MultiTenancyMode.SingleTenant;
    
    // Or for multi-tenant SaaS:
    // options.TenancyMode = MultiTenancyMode.PerRequest;
});
```

**Note:** In v0.3, this is primarily **informational**. Kernel provides the identity rails (TenantId/ProjectId propagation) but does not enforce isolation. Applications implement their own multi-tenancy logic.

[↑ Back to top](#table-of-contents)

---

## DI Registration

### IHoneyDrunkBuilder.cs

**What it is:** Fluent builder interface returned by `AddHoneyDrunkNode()` for chaining additional configuration.

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/IHoneyDrunkBuilder.cs`

#### Properties

```csharp
public interface IHoneyDrunkBuilder
{
    IServiceCollection Services { get; }
}
```

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    // ... configure Node identity
})
.Services // Access underlying IServiceCollection
.AddSingleton<IMyCustomService, MyCustomService>(); // Chain additional services
```

**Design:** Simple pass-through to `IServiceCollection` for advanced scenarios. Most users won't need to use this directly.

[↑ Back to top](#table-of-contents)

---

### AddHoneyDrunkNode()

**What it is:** Main bootstrap extension that registers all Kernel services and transforms an application into a Grid Node.

**Location:** `HoneyDrunk.Kernel/Hosting/HoneyDrunkNodeServiceCollectionExtensions.cs`

#### Signature

```csharp
public static IHoneyDrunkBuilder AddHoneyDrunkNode(
    this IServiceCollection services,
    Action<HoneyDrunkNodeOptions> configure)
```

#### What It Registers

| Service | Lifetime | Description |
|---------|----------|-------------|
| `HoneyDrunkNodeOptions` | Singleton | Bootstrap options (validated) |
| `INodeContext` | Singleton | Process-scoped Node identity |
| `INodeDescriptor` | Singleton | Runtime Node metadata |
| `IGridContextAccessor` | Singleton | Ambient GridContext accessor |
| `IOperationContextAccessor` | Singleton | Ambient OperationContext accessor |
| `IOperationContextFactory` | Scoped | Factory for creating operation contexts |
| `IGridContext` | Scoped | Per-request Grid context (default factory) |
| `IErrorClassifier` | Singleton | Maps exceptions to HTTP status codes |
| `ITransportEnvelopeBinder` | Singleton (3x) | HTTP, messaging, job context binders |
| `NodeLifecycleManager` | Singleton | Coordinates health/readiness/lifecycle |
| `NodeLifecycleHost` | Hosted Service | Executes startup/shutdown hooks |
| `GridActivitySource` | Singleton | OpenTelemetry ActivitySource for tracing |

#### Validation Flow

```csharp
var options = new HoneyDrunkNodeOptions();
configure(options);
options.Validate(); // Throws if NodeId, SectorId, EnvironmentId, or StudioId missing

// Version fallback
options.Version ??= Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
```

#### Complete Example

```csharp
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// STEP 1: Register Node (validates + wires all services)
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.Kernel;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = Environments.Development;
    options.StudioId = "demo-studio";
    options.Version = "0.3.0";
    options.Telemetry.TraceSamplingRatio = 1.0;
});

var app = builder.Build();

// STEP 2: Validate all services registered
app.Services.ValidateHoneyDrunkServices();

// STEP 3: Add middleware for HTTP context propagation
app.UseGridContext();

// STEP 4: Define endpoints (GridContext is available)
app.MapGet("/", (INodeContext node, IGridContext grid) => Results.Ok(new
{
    Node = new { node.NodeId, node.Version, node.Environment },
    Request = new { grid.CorrelationId, grid.NodeId }
}));

app.Run();
```

[↑ Back to top](#table-of-contents)

---

## Middleware

### UseGridContext()

**What it is:** ASP.NET Core middleware that establishes Grid context from HTTP request headers.

**Location:** `HoneyDrunk.Kernel/Hosting/HoneyDrunkApplicationBuilderExtensions.cs`

#### What It Does

1. **Extracts headers** - Reads `X-Correlation-Id`, `X-Causation-Id`, `X-Studio-Id`, `X-Tenant-Id`, `X-Project-Id`
2. **Creates GridContext** - Maps headers to `IGridContext` (generates new OperationId)
3. **Sets accessors** - Populates `IGridContextAccessor.GridContext` and `IOperationContextAccessor.Current`
4. **Creates OperationContext** - Tracks request timing and outcome
5. **Echoes headers** - Returns `X-Correlation-Id`, `X-Node-Id`, `X-Tenant-Id`, `X-Project-Id` in response

#### Registration

```csharp
var app = builder.Build();

// Register early in pipeline (before other middleware)
app.UseGridContext();

// Now GridContext is available in all downstream middleware/endpoints
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

#### Flow Diagram

```
HTTP Request with X-Correlation-Id header
    ↓
[GridContextMiddleware]
    ├─ Extract headers (correlation, causation, tenant, project)
    ├─ Generate new OperationId (span-id)
    ├─ Create GridContext (three-ID model: Correlation, Operation, Causation)
    ├─ Set IGridContextAccessor.GridContext
    ├─ Create OperationContext (timing/outcome tracking)
    ├─ Set IOperationContextAccessor.Current
    ├─ Echo headers to response (OnStarting callback)
    ↓
[Your Controllers/Endpoints] (IGridContext, IOperationContext available)
    ↓
[GridContextMiddleware Finally Block]
    ├─ operation.Complete() or operation.Fail()
    ├─ Clear accessors
    ├─ operation.Dispose() (logs duration, emits telemetry)
    ↓
Response with X-Correlation-Id, X-Operation-Id, X-Node-Id headers
```

See [Context.md](Context.md) for detailed GridContext propagation patterns.

[↑ Back to top](#table-of-contents)

---

### ValidateHoneyDrunkServices()

**What it is:** Startup validation that ensures all required Kernel services are registered correctly.

**Location:** `HoneyDrunk.Kernel/Hosting/ServiceProviderExtensions.cs`

#### What It Validates

- `INodeContext` is registered and resolvable
- `IGridContextAccessor` is registered
- `IOperationContextAccessor` is registered
- `IOperationContextFactory` is registered (scoped)
- Other core services are available

#### Usage

```csharp
var app = builder.Build();

// Validate before starting (fail fast if misconfigured)
try
{
    app.Services.ValidateHoneyDrunkServices();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Kernel validation failed: {ex.Message}");
    Environment.Exit(1);
}

app.Run();
```

#### When to Use

- ✅ **Always** - Call in `Program.cs` after `builder.Build()`
- ✅ **Integration tests** - Validate test service provider before running tests
- ✅ **Startup scripts** - Validate in health check probe before marking ready

**Design:** Fail-fast pattern - better to crash at startup than discover misconfiguration in production.

[↑ Back to top](#table-of-contents)

---

## Lifecycle Management

### NodeLifecycleHost.cs

**What it is:** IHostedService implementation that coordinates Node lifecycle and executes startup/shutdown hooks.

**Location:** `HoneyDrunk.Kernel/Hosting/NodeLifecycleHost.cs`

#### Responsibilities

1. **Startup Coordination**
   - Sets `NodeLifecycleStage.Starting`
   - Executes `IStartupHook` implementations (ordered by priority)
   - Starts `INodeLifecycle` implementations
   - Sets `NodeLifecycleStage.Running`

2. **Shutdown Coordination**
   - Sets `NodeLifecycleStage.Stopping`
   - Stops `INodeLifecycle` implementations
   - Executes `IShutdownHook` implementations (ordered by priority)
   - Sets `NodeLifecycleStage.Stopped`

3. **Failure Handling**
   - Sets `NodeLifecycleStage.Failed` on exceptions
   - Logs critical errors
   - Rethrows to fail the application

#### Startup Flow

```
Application Start
    ↓
[NodeLifecycleHost.StartAsync]
    ├─ Log: "Starting Node {NodeId} v{Version} in {Environment}"
    ├─ Set NodeLifecycleStage.Starting
    ├─ Execute IStartupHook (priority order):
    │   ├─ DatabaseMigrationHook (Priority: 100)
    │   ├─ CacheWarmupHook (Priority: 200)
    │   └─ HealthCheckRegistrationHook (Priority: 300)
    ├─ Start INodeLifecycle implementations
    ├─ Set NodeLifecycleStage.Running
    ├─ Log: "Node {NodeId} is now running"
    ↓
Application Accepting Requests
```

#### Shutdown Flow

```
Shutdown Signal (SIGTERM, Ctrl+C)
    ↓
[NodeLifecycleHost.StopAsync]
    ├─ Log: "Stopping Node {NodeId}"
    ├─ Set NodeLifecycleStage.Stopping
    ├─ Stop INodeLifecycle implementations
    ├─ Execute IShutdownHook (priority order):
    │   ├─ DrainRequestsHook (Priority: 100)
    │   ├─ CloseConnectionsHook (Priority: 200)
    │   └─ FlushTelemetryHook (Priority: 300)
    ├─ Set NodeLifecycleStage.Stopped
    ├─ Log: "Node {NodeId} stopped successfully"
    ↓
Process Exit
```

#### Registering Hooks

```csharp
// Startup hooks
builder.Services.AddSingleton<IStartupHook, DatabaseMigrationHook>();
builder.Services.AddSingleton<IStartupHook, CacheWarmupHook>();

// Shutdown hooks
builder.Services.AddSingleton<IShutdownHook, ConnectionDrainHook>();
builder.Services.AddSingleton<IShutdownHook, TelemetryFlushHook>();
```

See [Lifecycle.md](Lifecycle.md) for detailed lifecycle hook patterns.

[↑ Back to top](#table-of-contents)

---

## Service Discovery (Future)

The following abstractions exist in v0.3 but are **not yet wired** into the bootstrap process. They are building blocks for future dynamic service discovery.

### INodeManifest.cs

**What it is:** Declarative contract describing a Node's identity, capabilities, dependencies, and configuration requirements.

**Status:** ✅ Defined, ❌ Not used in v0.3, 🔮 Future

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/INodeManifest.cs`

**Future Use Case:** Nodes will declare manifests at build-time, and the Grid will use them for:
- Dependency validation at startup
- Capability-based routing
- Configuration schema validation
- Service mesh integration

See interface definition in the code - similar to `package.json` or `pom.xml` for Nodes.

---

### INodeDescriptor.cs

**What it is:** Runtime descriptor combining manifest information with execution state.

**Status:** ✅ Minimal implementation in v0.3 (no capabilities), 🔮 Extended in future

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/INodeDescriptor.cs`

**Current v0.3 Implementation:** Created by `AddHoneyDrunkNode()` with basic identity (NodeId, Version, Sector, Studio, Environment) but no capabilities or manifest binding.

**Future:** Will include rich capability metadata, dependency tracking, and runtime state.

---

### INodeCapability.cs

**What it is:** Discoverable capability/feature that a Node provides to the Grid.

**Status:** ✅ Defined, ❌ Not used in v0.3, 🔮 Future

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/INodeCapability.cs`

**Future Use Case:** Nodes will advertise capabilities (e.g., "payment-processing", "email-sending") with:
- Supported protocols (HTTP, gRPC, message queue)
- Endpoint URLs
- Input/output schemas (JSON Schema, OpenAPI)
- Rate limits, SLAs, costs

---

### INodeManifestSource.cs

**What it is:** Abstraction for loading Node manifests from various sources.

**Status:** ✅ Defined, ❌ Not implemented in v0.3, 🔮 Future

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/INodeManifestSource.cs`

**Future Implementations:**
- `EmbeddedResourceManifestSource` - Load from assembly resources
- `FileSystemManifestSource` - Load from `node.manifest.json`
- `HttpManifestSource` - Fetch from Grid registry

[↑ Back to top](#table-of-contents)

---

## Studio Configuration

### IStudioConfiguration.cs

**What it is:** Environment-wide configuration shared across all Nodes in a Studio.

**Status:** ✅ Fully implemented and documented in [Configuration.md](Configuration.md)

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/IStudioConfiguration.cs`

**Cross-Reference:** See [Configuration.md](Configuration.md#istudioconfiguration) for complete documentation including:
- Interface definition
- StudioConfiguration implementation
- Feature flags and endpoints
- Integration with secrets management

[↑ Back to top](#table-of-contents)

---

## Complete Bootstrap Example

Here's a production-ready Node bootstrap with all features:

```csharp
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Node with strongly-typed identity
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Market.Arcadia;           // From static registry
    options.SectorId = Sectors.Market;                // From static registry  
    options.EnvironmentId = Environments.Production; // From static registry
    options.StudioId = "honeydrunk-studios";
    options.Version = "2.1.0";
    
    // Telemetry configuration
    options.Telemetry.EnableTracing = true;
    options.Telemetry.TraceSamplingRatio = 0.1; // 10% sampling in prod
    
    // Tags for discovery/routing
    options.Tags["region"] = "us-west-2";
    options.Tags["deployment-slot"] = "blue";
    options.Tags["cost-center"] = "marketplace";
});

// 2. Register custom startup/shutdown hooks
builder.Services.AddSingleton<IStartupHook, DatabaseMigrationHook>();
builder.Services.AddSingleton<IStartupHook, CacheWarmupHook>();
builder.Services.AddSingleton<IShutdownHook, ConnectionDrainHook>();

// 3. Register health contributors
builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
builder.Services.AddSingleton<IReadinessContributor, CacheReadinessContributor>();

// 4. Register application services
builder.Services.AddControllers();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// 5. Validate all services registered correctly (fail fast)
app.Services.ValidateHoneyDrunkServices();

// 6. Add Grid context middleware (early in pipeline)
app.UseGridContext();

// 7. Add application middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 8. Map endpoints (GridContext available in all handlers)
app.MapGet("/", (INodeContext node, IGridContext grid) => Results.Ok(new
{
    Node = new
    {
        Id = node.NodeId,
        Version = node.Version,
        Environment = node.Environment,
        Studio = node.StudioId,
        Uptime = (DateTimeOffset.UtcNow - node.StartedAtUtc).TotalSeconds
    },
    Request = new
    {
        CorrelationId = grid.CorrelationId,
        OperationId = grid.OperationId,
        TenantId = grid.TenantId,
        ProjectId = grid.ProjectId
    }
}));

app.MapGet("/health", (NodeLifecycleManager lifecycle) => 
{
    var health = lifecycle.GetHealthStatus();
    return health.IsHealthy ? Results.Ok(health) : Results.ServiceUnavailable();
});

app.MapGet("/ready", (NodeLifecycleManager lifecycle) =>
{
    var readiness = lifecycle.GetReadinessStatus();
    return readiness.IsReady ? Results.Ok(readiness) : Results.ServiceUnavailable();
});

app.MapControllers();

app.Run();

// Example startup hook
public class DatabaseMigrationHook(ILogger<DatabaseMigrationHook> logger) : IStartupHook
{
    public int Priority => 100; // Run early

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running database migrations...");
        await Task.Delay(100, cancellationToken); // Simulate migration
        logger.LogInformation("Database migrations complete");
    }
}

// Example shutdown hook
public class ConnectionDrainHook(ILogger<ConnectionDrainHook> logger) : IShutdownHook
{
    public int Priority => 100; // Run first during shutdown

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Draining active connections...");
        await Task.Delay(1000, cancellationToken); // Wait for requests to finish
        logger.LogInformation("All connections drained");
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

### Testing Bootstrap Configuration

```csharp
[Fact]
public void AddHoneyDrunkNode_WithValidOptions_RegistersAllServices()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    
    // Act
    services.AddHoneyDrunkNode(options =>
    {
        options.NodeId = Nodes.Core.Kernel;
        options.SectorId = Sectors.Core;
        options.EnvironmentId = Environments.Development;
        options.StudioId = "test-studio";
    });
    
    var provider = services.BuildServiceProvider();
    
    // Assert
    var nodeContext = provider.GetService<INodeContext>();
    Assert.NotNull(nodeContext);
    Assert.Equal("kernel", nodeContext.NodeId);
    Assert.Equal("core", nodeContext.Environment);
    
    var gridContextAccessor = provider.GetService<IGridContextAccessor>();
    Assert.NotNull(gridContextAccessor);
}

[Fact]
public void AddHoneyDrunkNode_WithMissingNodeId_ThrowsValidationException()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    
    // Act & Assert
    var ex = Assert.Throws<InvalidOperationException>(() =>
    {
        services.AddHoneyDrunkNode(options =>
        {
            // NodeId missing - should throw
            options.SectorId = Sectors.Core;
            options.EnvironmentId = Environments.Development;
            options.StudioId = "test-studio";
        });
    });
    
    Assert.Contains("NodeId is required", ex.Message);
}
```

### Testing Middleware

```csharp
[Fact]
public async Task UseGridContext_ExtractsCorrelationIdFromHeaders()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddHoneyDrunkNode(options =>
    {
        options.NodeId = Nodes.Core.Kernel;
        options.SectorId = Sectors.Core;
        options.EnvironmentId = Environments.Development;
        options.StudioId = "test-studio";
    });
    
    var provider = services.BuildServiceProvider();
    var context = new DefaultHttpContext();
    context.RequestServices = provider;
    context.Request.Headers["X-Correlation-Id"] = "test-correlation-123";
    
    var middleware = new GridContextMiddleware(
        next: async _ => await Task.CompletedTask,
        logger: NullLogger<GridContextMiddleware>.Instance);
    
    // Act
    await middleware.InvokeAsync(
        context,
        provider.GetRequiredService<INodeContext>(),
        provider.GetRequiredService<IGridContextAccessor>(),
        provider.GetRequiredService<IOperationContextAccessor>(),
        provider.GetRequiredService<IOperationContextFactory>());
    
    // Assert
    Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
    Assert.Equal("test-correlation-123", context.Response.Headers["X-Correlation-Id"].ToString());
}
```

### Testing Lifecycle Hooks

```csharp
[Fact]
public async Task NodeLifecycleHost_ExecutesStartupHooksInPriorityOrder()
{
    // Arrange
    var executionOrder = new List<string>();
    
    var hook1 = new TestStartupHook(100, () => executionOrder.Add("hook1"));
    var hook2 = new TestStartupHook(50, () => executionOrder.Add("hook2"));
    var hook3 = new TestStartupHook(200, () => executionOrder.Add("hook3"));
    
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IStartupHook>(hook1);
    services.AddSingleton<IStartupHook>(hook2);
    services.AddSingleton<IStartupHook>(hook3);
    services.AddHoneyDrunkNode(options =>
    {
        options.NodeId = Nodes.Core.Kernel;
        options.SectorId = Sectors.Core;
        options.EnvironmentId = Environments.Development;
        options.StudioId = "test-studio";
    });
    
    var provider = services.BuildServiceProvider();
    var host = provider.GetRequiredService<IHostedService>() as NodeLifecycleHost;
    
    // Act
    await host!.StartAsync(CancellationToken.None);
    
    // Assert
    Assert.Equal(new[] { "hook2", "hook1", "hook3" }, executionOrder); // Sorted by priority: 50, 100, 200
}

private class TestStartupHook(int priority, Action callback) : IStartupHook
{
    public int Priority => priority;
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        callback();
        return Task.CompletedTask;
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

### Core Bootstrap Components (v0.3)

| Component | Purpose | Status |
|-----------|---------|--------|
| **HoneyDrunkNodeOptions** | Bootstrap configuration | ✅ Fully implemented |
| **HoneyDrunkTelemetryOptions** | Telemetry sub-configuration | ✅ Fully implemented |
| **IHoneyDrunkBuilder** | Fluent builder for chaining | ✅ Fully implemented |
| **AddHoneyDrunkNode()** | Main DI registration entry point | ✅ Fully implemented |
| **UseGridContext()** | HTTP middleware for context | ✅ Fully implemented |
| **ValidateHoneyDrunkServices()** | Startup validation | ✅ Fully implemented |
| **NodeLifecycleHost** | IHostedService coordinator | ✅ Fully implemented |
| **MultiTenancyMode** | Multi-tenancy enum | ✅ Defined (informational) |

### Service Discovery Components (Future)

| Component | Purpose | Status |
|-----------|---------|--------|
| **INodeManifest** | Declarative Node contract | 🔮 Defined, not wired |
| **INodeDescriptor** | Runtime metadata | ⚠️ Minimal (no capabilities) |
| **INodeCapability** | Feature/API contract | 🔮 Defined, not wired |
| **INodeManifestSource** | Manifest loading | 🔮 Defined, not implemented |

### Key Patterns

**Bootstrap Flow:**
1. Configure `HoneyDrunkNodeOptions` with strongly-typed identity
2. Call `AddHoneyDrunkNode()` to register all services
3. Build application and call `ValidateHoneyDrunkServices()`
4. Add `UseGridContext()` middleware early in pipeline
5. Application runs with full Grid context propagation

**Lifecycle Flow:**
1. `NodeLifecycleHost` starts with `NodeLifecycleStage.Starting`
2. Executes `IStartupHook` implementations in priority order
3. Transitions to `NodeLifecycleStage.Running`
4. On shutdown signal, transitions to `NodeLifecycleStage.Stopping`
5. Executes `IShutdownHook` implementations in priority order
6. Transitions to `NodeLifecycleStage.Stopped`

**Type System:**
- **Bootstrap (config-time)**: Strongly-typed structs (`NodeId`, `SectorId`, `EnvironmentId`)
- **Runtime (hot-path)**: Strings for performance (`INodeContext.NodeId`, `IGridContext.NodeId`)

---

