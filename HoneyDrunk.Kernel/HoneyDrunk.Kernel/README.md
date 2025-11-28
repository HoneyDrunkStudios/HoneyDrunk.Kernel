# HoneyDrunk.Kernel

[![NuGet](https://img.shields.io/nuget/v/HoneyDrunk.Kernel.svg)](https://www.nuget.org/packages/HoneyDrunk.Kernel/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Runtime Implementations for the HoneyDrunk Grid** - Production-ready implementations of all Kernel abstractions.

## 📋 What Is This?

**HoneyDrunk.Kernel** provides the runtime implementations of all contracts defined in `HoneyDrunk.Kernel.Abstractions`. This is the package you use when building executable Nodes, services, or applications that participate in the Grid.

**Package Relationships:**
- **HoneyDrunk.Kernel.Abstractions** = Contracts only (interfaces, value types)
- **HoneyDrunk.Kernel** = Real implementations, middleware, lifecycle orchestration, and bootstrapping helpers

Use this package when you want a Node to actually run inside the Grid.

## 📦 What's Inside

### 🌐 Context Implementations
- **GridContext** - Default implementation with correlation, causation, baggage, and `CreateChildContext()`
- **NodeContext** - Process-scoped Node identity (NodeId, StudioId, Environment, version, lifecycle stage)
- **OperationContext** - Per-operation tracking with timing, outcome, and metadata
- **GridContextAccessor** - Async-local accessor for ambient Grid context
- **OperationContextAccessor** - Async-local accessor for ambient operation context
- **OperationContextFactory** - Creates `IOperationContext` from current `IGridContext`

### 🔄 Context Mappers
Automatic context extraction from transport envelopes:
- **HttpContextMapper** - Maps HTTP headers to GridContext using `GridHeaderNames`
- **MessagingContextMapper** - Maps message properties to GridContext
- **JobContextMapper** - Maps background job metadata to GridContext

### 🚚 Transport Binders
Automatic context propagation to outgoing envelopes:
- **HttpResponseBinder** - Writes Grid headers to HTTP responses
- **MessagePropertiesBinder** - Writes Grid headers to message properties
- **JobMetadataBinder** - Writes Grid headers to job metadata

All implement `ITransportEnvelopeBinder`.

### ⚙️ Lifecycle Management
- **NodeLifecycleManager** - Coordinates startup/shutdown hooks, health/readiness contributors, and lifecycle stage transitions
- **NodeLifecycleHost** - `IHostedService` bridge that runs the lifecycle inside ASP.NET Core hosting

### 📈 Diagnostics & Health
- **NoOpMetricsCollector** - Zero-overhead `IMetricsCollector` implementation when metrics are disabled
- **NodeContextReadinessContributor** - Readiness signal based on `INodeContext.LifecycleStage`
- **ConfigurationValidator** - Validates Node configuration at startup

### 🔧 Configuration
- **StudioConfiguration** - Implementation of `IStudioConfiguration` over `IConfiguration`

### ❤️ Health
- **CompositeHealthCheck** - Aggregates multiple `IHealthCheck` instances into a single status

### 💉 Dependency Injection & Bootstrapping
- **AddHoneyDrunkGrid** - Unified registration for all Kernel services required for a Node
- **UseGridContext** - Middleware that creates and propagates GridContext for HTTP requests
- **ValidateHoneyDrunkServices** - Extension that verifies Kernel services are registered correctly

## 📥 Installation

```bash
dotnet add package HoneyDrunk.Kernel
```

```xml
<ItemGroup>
  <PackageReference Include="HoneyDrunk.Kernel" Version="0.3.0" />
</ItemGroup>
```

**Note:** This package automatically includes `HoneyDrunk.Kernel.Abstractions` as a dependency.

## 🚀 Quick Start

### Basic Node Setup

```csharp
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register Grid services for this Node
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "payment-service";
    options.StudioId = "demo-studio";
    options.Environment = "development";
    options.Version = "1.0.0";
    options.Tags["region"] = "local";
});

// Register additional Kernel services
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

builder.Services.AddControllers();

var app = builder.Build();

// Fail fast if required services are missing
app.Services.ValidateHoneyDrunkServices();

// Add Grid context middleware early in the pipeline
app.UseGridContext();

app.MapControllers();

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
        
        // Create child context for downstream Node
        var childContext = gridContext.CreateChildContext("payment-service");
        await _paymentService.ChargeAsync(order, childContext);
    }
}
```

### HTTP Context Mapping

```csharp
// Startup - automatically maps X-Correlation-Id, X-Causation-Id, X-Baggage-* headers
app.UseGridContext();

app.MapPost("/orders", async (
    Order order,
    IGridContext gridContext,
    IOrderService orderService) =>
{
    // gridContext is populated from incoming HTTP headers or created if missing
    await orderService.ProcessOrderAsync(order);
    return Results.Created($"/orders/{order.Id}", order);
});
```

### Lifecycle Hooks and Health

```csharp
// Startup hooks
builder.Services.AddSingleton<IStartupHook, DatabaseMigrationHook>();
builder.Services.AddSingleton<IStartupHook, CacheWarmupHook>();

// Shutdown hooks
builder.Services.AddSingleton<IShutdownHook, ConnectionDrainHook>();

// Health & readiness contributors
builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
builder.Services.AddSingleton<IReadinessContributor, NodeContextReadinessContributor>();
```

Kubernetes can then wire probes to `/health` and `/ready` using your chosen web Node.

## 🎯 When to Use Which Package

**Use HoneyDrunk.Kernel when:**
- ✅ Building an executable Node or HTTP API
- ✅ You want `AddHoneyDrunkGrid()` and `UseGridContext()`
- ✅ You need lifecycle orchestration, health, readiness, and context mappers

**Use HoneyDrunk.Kernel.Abstractions when:**
- ✅ Building a shared library used by multiple Nodes
- ✅ You want to depend only on contracts and provide your own implementations
- ✅ You are writing test doubles or alternative runtimes

## 🏗️ Architecture

### Context Flow

```
HTTP Request with X-Correlation-Id header
    ↓
HttpContextMapper extracts header → GridContext
    ↓
GridContext injected into OrderService
    ↓
OrderService creates child context for PaymentService
    ↓
ChildContext propagates to downstream Node
```

### Lifecycle Flow

```
Application Start
    ↓
NodeLifecycleStage = Initializing
    ↓
Execute IStartupHook instances (by priority)
    ↓
Check IReadinessContributor instances
    ↓
NodeLifecycleStage = Ready
    ↓
(Application runs...)
    ↓
Shutdown signal received
    ↓
NodeLifecycleStage = Stopping
    ↓
Stop accepting new requests
    ↓
Execute IShutdownHook instances (by priority)
    ↓
NodeLifecycleStage = Stopped
```

## ⚙️ Configuration

### appsettings.json

```json
{
  "Grid": {
    "StudioId": "honeycomb-prod",
    "Environment": "production"
  },
  "Node": {
    "Id": "payment-service",
    "Sector": "financial-services",
    "Version": "2.1.0",
    "Tags": {
      "deployment-slot": "blue",
      "region": "us-east-1"
    }
  }
}
```

### Loading Configuration

```csharp
builder.Services.AddHoneyDrunkGrid(options =>
{
    var node = builder.Configuration.GetSection("Node");
    
    options.NodeId = node["Id"] ?? "unknown-node";
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "default";
    options.Environment = builder.Configuration["Grid:Environment"] ?? "development";
    options.Version = node["Version"] ?? "1.0.0";
    
    // Load tags
    foreach (var tag in node.GetSection("Tags").GetChildren())
    {
        options.Tags[tag.Key] = tag.Value ?? string.Empty;
    }
});
```

## 🔗 Related Packages

- **[HoneyDrunk.Kernel.Abstractions](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions/)** - Contracts only
- **[HoneyDrunk.Standards](https://www.nuget.org/packages/HoneyDrunk.Standards/)** - Analyzers and coding conventions

## 📚 Documentation

**See the main repository for comprehensive documentation:**
- **[Complete File Guide](../docs/FILE_GUIDE.md)** - Architecture documentation
- **[Bootstrapping Guide](../docs/Bootstrapping.md)** - Unified Node initialization
- **[Context Guide](../docs/Context.md)** - Context propagation patterns
- **[Lifecycle Guide](../docs/Lifecycle.md)** - Lifecycle orchestration
- **[Implementations Guide](../docs/Implementations.md)** - Runtime implementation details
- **[Transport Guide](../docs/Transport.md)** - Context propagation across boundaries
- **[Testing Guide](../docs/Testing.md)** - Test patterns and best practices

## 🧪 Testing

See **[Testing Guide](../docs/Testing.md)** for patterns on:
- Creating test contexts (GridContext, NodeContext, OperationContext)
- Testing with real implementations vs mocks
- Integration testing with DI
- Testing lifecycle hooks and health contributors

## 📄 License

This project is licensed under the [MIT License](../LICENSE).

---

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](../docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
