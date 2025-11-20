# HoneyDrunk.Kernel

[![NuGet](https://img.shields.io/nuget/v/HoneyDrunk.Kernel.svg)](https://www.nuget.org/packages/HoneyDrunk.Kernel/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Runtime Implementations for the HoneyDrunk Grid** - Production-ready implementations of all Kernel abstractions.

## ?? What Is This?

**HoneyDrunk.Kernel** provides the runtime implementations of all contracts defined in `HoneyDrunk.Kernel.Abstractions`. This is the package you use when building executable Nodes, services, or applications that participate in the Grid.

## ?? What's Inside

### ?? Context Implementations
- **GridContext** - Default implementation with causation chain support
- **NodeContext** - Process-scoped Node identity
- **OperationContext** - Operation tracking with timing and outcome
- **GridContextAccessor** - Async-local context accessor

### ??? Context Mappers
Automatic context propagation from various sources:
- **HttpContextMapper** - Maps HTTP headers to GridContext
- **JobContextMapper** - Maps background job metadata
- **MessagingContextMapper** - Maps message properties for event-driven architectures

### ?? Lifecycle Management
- **NodeLifecycleManager** - Coordinates startup/shutdown
- **NodeLifecycleHost** - Hosts Node lifecycle with health/readiness

### ?? Diagnostics
- **NoOpMetricsCollector** - Zero-overhead placeholder (replace with OpenTelemetry in production)
- **NodeLifecycleHealthContributor** - Lifecycle-based health
- **NodeContextReadinessContributor** - Context-based readiness

### ?? Configuration
- **StudioConfiguration** - Studio-wide configuration implementation

### ?? Secrets
- **CompositeSecretsSource** - Chains multiple secret sources with fallback logic

### ?? Health
- **CompositeHealthCheck** - Aggregates multiple health checks

### ?? Dependency Injection
- **HoneyDrunkCoreExtensions** - Core service registration (`AddHoneyDrunkCore`, `AddHoneyDrunkCoreNode`)
- **ServiceProviderValidation** - Startup validation

## ?? Installation

```bash
dotnet add package HoneyDrunk.Kernel
```

```xml
<PackageReference Include="HoneyDrunk.Kernel" Version="0.2.0" />
```

**Note:** This package automatically includes `HoneyDrunk.Kernel.Abstractions` as a dependency.

## ?? Quick Start

### Basic Node Setup

```csharp
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register Kernel services with Node descriptor
var nodeDescriptor = new NodeDescriptor
{
    NodeId = "payment-node",
    Version = "1.0.0",
    Name = "Payment Processing Node",
    Sector = "commerce",
    Cluster = "payments-cluster"
};

builder.Services.AddHoneyDrunkCoreNode(nodeDescriptor);

var app = builder.Build();

// Validate services before starting
app.Services.ValidateHoneyDrunkServices();

app.Run();
```

### Using Context in Services

```csharp
public class OrderService(
    IGridContext gridContext,
    INodeContext nodeContext,
    ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        logger.LogInformation(
            "Processing order {OrderId} on Node {NodeId} with correlation {CorrelationId}",
            order.Id,
            nodeContext.NodeId,
            gridContext.CorrelationId);
        
        // Create child context for downstream call
        var childContext = gridContext.CreateChildContext("payment-node");
        await _paymentService.ChargeAsync(order, childContext);
    }
}
```

### HTTP Context Mapping

```csharp
// Automatically maps X-Correlation-ID, X-Causation-ID, X-Baggage-* headers
app.UseMiddleware<GridContextMiddleware>();

app.MapPost("/orders", async (Order order, IGridContext gridContext) =>
{
    // gridContext is automatically populated from HTTP headers
    await _orderService.ProcessOrderAsync(order);
    return Results.Created($"/orders/{order.Id}", order);
});
```

### Lifecycle Hooks

```csharp
// Register startup hooks
builder.Services.AddSingleton<IStartupHook, DatabaseMigrationHook>();
builder.Services.AddSingleton<IStartupHook, CacheWarmupHook>();

// Register shutdown hooks
builder.Services.AddSingleton<IShutdownHook, ConnectionDrainHook>();

// Register health contributors
builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
builder.Services.AddSingleton<IReadinessContributor, CacheReadinessContributor>();
```

## ?? When to Use This Package

**Use HoneyDrunk.Kernel when:**
- ? Building an executable Node/service
- ? You need context mappers (HTTP, messaging, jobs)
- ? You need lifecycle orchestration
- ? You want production-ready implementations

**Use HoneyDrunk.Kernel.Abstractions only when:**
- ? Building a library (use abstractions to avoid implementation dependencies)
- ? Creating custom implementations

## ?? Architecture

### Context Flow

```
HTTP Request with X-Correlation-ID header
    ?
HttpContextMapper extracts header ? GridContext
    ?
GridContext injected into OrderService
    ?
OrderService creates child context for PaymentService
    ?
ChildContext propagates to downstream Node
```

### Lifecycle Flow

```
Application Start
    ?
NodeLifecycleStage = Initializing
    ?
Execute IStartupHook instances (by priority)
    ?
Check IReadinessContributor instances
    ?
NodeLifecycleStage = Running
    ?
(Application runs...)
    ?
Shutdown signal received
    ?
NodeLifecycleStage = Stopping
    ?
Stop accepting new requests
    ?
Execute IShutdownHook instances (by priority)
    ?
NodeLifecycleStage = Stopped
```

## ?? Configuration

### appsettings.json

```json
{
  "Grid": {
    "NodeId": "payment-node",
    "Version": "1.0.0",
    "StudioId": "honeycomb",
    "Environment": "production",
    "Tags": {
      "deployment-slot": "blue",
      "region": "us-east-1"
    }
  },
  "NodeRuntime": {
    "Environment": "production",
    "Region": "us-east-1",
    "EnableDetailedTelemetry": true,
    "EnableDistributedTracing": true,
    "TelemetrySamplingRate": 1.0,
    "HealthCheckIntervalSeconds": 30,
    "ShutdownGracePeriodSeconds": 30
  }
}
```

## ?? Related Packages

- **[HoneyDrunk.Kernel.Abstractions](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions/)** - Contracts only
- **[HoneyDrunk.Standards](https://www.nuget.org/packages/HoneyDrunk.Standards/)** - Analyzers and coding conventions

## ?? Documentation

- **[Complete File Guide](../docs/FILE_GUIDE.md)** - Comprehensive architecture documentation
- **[Context Guide](../docs/Context.md)** - Context propagation patterns
- **[Lifecycle Guide](../docs/Lifecycle.md)** - Lifecycle orchestration
- **[Implementations Guide](../docs/Implementations.md)** - Runtime implementation details

## ?? Testing

See **[Testing Guide](../docs/Testing.md)** for patterns on:
- Mocking GridContext, NodeContext, OperationContext
- Testing with deterministic time
- Integration testing with DI
- Testing lifecycle hooks and health contributors

## ?? License

This project is licensed under the [MIT License](../LICENSE).

---

**Built with ?? by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](../docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
