# 🚀 Bootstrapping - Node Initialization and Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [AddHoneyDrunkNode](#addhoneydrunknode)
- [HoneyDrunkNodeOptions](#honeydrunk-nodeoptions)
- [Basic Usage](#basic-usage)
- [Service Validation](#service-validation)
- [Middleware Registration](#middleware-registration)
- [Fluent Builder Pattern](#fluent-builder-pattern)
- [Complete Example](#complete-example)
- [Configuration from appsettings.json](#configuration-from-appsettingsjson)
- [Testing](#testing)
- [Migration from v0.2.x](#migration-from-v02x)
- [Summary](#summary)

---

## Overview

Bootstrapping provides a unified, opinionated way to configure HoneyDrunk Nodes with all required services, validation, and middleware in a single registration call.

**Location:** `HoneyDrunk.Kernel/Hosting/`

**Key Concepts:**
- **Unified Registration** - Single `AddHoneyDrunkNode()` call registers all services
- **Service Validation** - Built-in validation ensures required services are present
- **Middleware Helpers** - Easy middleware registration with `UseGridContext()`
- **Fluent Builder** - Chainable configuration via `IHoneyDrunkBuilder`

---

## AddHoneyDrunkNode

### What it is
The primary bootstrapping method that registers all HoneyDrunk Kernel services in a single call.

### Real-world analogy
Like a "turnkey solution" for a house - walls, plumbing, electricity, and HVAC all installed with one contract, guaranteed to work together.

### Method Signature

```csharp
public static IHoneyDrunkBuilder AddHoneyDrunkNode(
    this IServiceCollection services,
    Action<HoneyDrunkNodeOptions> configure)
```

### What It Registers

| Service | Lifetime | Description |
|---------|----------|-------------|
| `HoneyDrunkNodeOptions` | Singleton | Node configuration options |
| `INodeContext` | Singleton | Static Node identity and metadata |
| `INodeDescriptor` | Singleton | Node descriptor for service discovery |
| `IGridContextAccessor` | Singleton | Ambient Grid context accessor (AsyncLocal) |
| `IOperationContextAccessor` | Singleton | Ambient operation context accessor (AsyncLocal) |
| `IOperationContextFactory` | Scoped | Factory for creating operation contexts |
| `IErrorClassifier` | Singleton | Error classification for transport mapping |
| `IServiceProviderValidation` | Singleton | Service registration validation |
| `ITransportEnvelopeBinder` (HTTP) | Singleton | HTTP response context binder |
| `ITransportEnvelopeBinder` (Message) | Singleton | Message properties context binder |
| `ITransportEnvelopeBinder` (Job) | Singleton | Job metadata context binder |
| `IGridContext` | Scoped | Default Grid context factory |
| `NodeLifecycleManager` | Singleton | Lifecycle coordination and health/readiness aggregation |
| `NodeLifecycleHost` | IHostedService | Startup/shutdown hook orchestration |
| `GridActivitySource` | Singleton | OpenTelemetry ActivitySource for distributed tracing |

**Returns:** `IHoneyDrunkBuilder` for fluent configuration chaining.

**New in v0.3.0:**
- ✅ Lifecycle coordination (`NodeLifecycleManager`, `NodeLifecycleHost`) - Orchestrates startup/shutdown hooks and health monitoring
- ✅ Telemetry primitives (`GridActivitySource`) - OpenTelemetry-ready distributed tracing

**Note:** Agent interop services (`AgentContextProjection`, `GridContextSerializer`, `AgentResultSerializer`) are static helper classes and don't require DI registration. Use them directly via static methods (see [Agents.md](Agents.md#agentsinterop---serialization-and-context-marshaling)).

---

## HoneyDrunkNodeOptions

### Configuration Properties

```csharp
public class HoneyDrunkNodeOptions
{
    /// <summary>
    /// Required: The Node identifier (validated kebab-case).
    /// </summary>
    public NodeId? NodeId { get; set; }
    
    /// <summary>
    /// Required: The sector this Node belongs to.
    /// </summary>
    public SectorId? SectorId { get; set; }
    
    /// <summary>
    /// Optional: The Studio identifier (defaults to grid-level config).
    /// </summary>
    public string? StudioId { get; set; }
    
    /// <summary>
    /// Optional: The environment identifier (defaults to grid-level config).
    /// </summary>
    public EnvironmentId? EnvironmentId { get; set; }
    
    /// <summary>
    /// Optional: The Node version (defaults to assembly version).
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// Optional: Additional metadata tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
    
    /// <summary>
    /// Validates all required options are set.
    /// </summary>
    public void Validate()
    {
        if (NodeId is null)
            throw new InvalidOperationException("NodeId is required.");
        if (SectorId is null)
            throw new InvalidOperationException("SectorId is required.");
    }
}
```

---

## Basic Usage

### Minimal Configuration

```csharp
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register all HoneyDrunk services
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-service");
    options.SectorId = SectorId.WellKnown.Core;
    options.EnvironmentId = EnvironmentId.WellKnown.Production;
});

var app = builder.Build();

// Validate services (throws if required services missing)
app.Services.ValidateHoneyDrunkServices();

// Add Grid context middleware
app.UseGridContext();

app.Run();
```

### Full Configuration

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    // Required: Node identity
    options.NodeId = new NodeId("payment-service");
    options.SectorId = new SectorId("financial-services");
    
    // Optional: Studio and environment
    options.StudioId = "honeycomb-prod";
    options.EnvironmentId = new EnvironmentId("production");
    
    // Optional: Version (defaults to assembly version)
    options.Version = "2.1.0";
    
    // Optional: Metadata tags
    options.Tags["region"] = "us-east-1";
    options.Tags["deployment-slot"] = "blue";
    options.Tags["cost-center"] = "engineering";
});
```

### Using Well-Known Identities

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("api-gateway");
    
    // Use well-known sectors
    options.SectorId = SectorId.WellKnown.Web;
    
    // Use well-known environments
    options.EnvironmentId = EnvironmentId.WellKnown.Development;
    
    // Available well-known sectors:
    // - SectorId.WellKnown.Core
    // - SectorId.WellKnown.AI
    // - SectorId.WellKnown.Ops
    // - SectorId.WellKnown.Data
    // - SectorId.WellKnown.Web
    // - SectorId.WellKnown.Messaging
    // - SectorId.WellKnown.Storage
    
    // Available well-known environments:
    // - EnvironmentId.WellKnown.Production
    // - EnvironmentId.WellKnown.Staging
    // - EnvironmentId.WellKnown.Development
    // - EnvironmentId.WellKnown.Testing
    // - EnvironmentId.WellKnown.Performance
    // - EnvironmentId.WellKnown.Integration
    // - EnvironmentId.WellKnown.Local
});
```

---

## Service Validation

### ValidateHoneyDrunkServices

Validates that all required Kernel services are properly registered. Should be called during application startup before processing requests.

```csharp
public static void ValidateHoneyDrunkServices(this IServiceProvider serviceProvider)
```

**Validates:**
- ✅ `INodeContext` - Static Node identity
- ✅ `IGridContextAccessor` - Ambient Grid context accessor
- ✅ `IOperationContextAccessor` - Ambient operation context accessor
- ✅ `IOperationContextFactory` - Operation context factory
- ✅ `INodeDescriptor` - Node descriptor
- ✅ `IErrorClassifier` - Error classification
- ✅ `NodeLifecycleManager` - Lifecycle coordination (NEW v0.3.0)
- ✅ `NodeLifecycleHost` - Startup/shutdown orchestration (NEW v0.3.0)

**Warns if missing (recommended):**
- ⚠️ `ITransportEnvelopeBinder` - Transport context binders
- ⚠️ `IStudioConfiguration` - Studio-level configuration

**Warns if missing (optional but recommended for v3):**
- ⚠️ `IStartupHook` - Custom Node initialization logic
- ⚠️ `IShutdownHook` - Custom graceful cleanup logic
- ⚠️ `IHealthContributor` - Health monitoring for /health endpoint
- ⚠️ `IReadinessContributor` - Readiness checks for /ready endpoint traffic gating

**Example:**

```csharp
var app = builder.Build();

try
{
    app.Services.ValidateHoneyDrunkServices();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Service validation failed: {ex.Message}");
    Environment.Exit(1);
}

app.Run();
```

**Error Message Example:**

```
Service validation failed:
Required HoneyDrunk services are not registered:
  - INodeContext is missing. Register via: AddHoneyDrunkNode()
  - IGridContextAccessor is missing. Register via: AddHoneyDrunkNode()
  - IOperationContextAccessor is missing. Register via: AddHoneyDrunkNode()
```

---

## Middleware Registration

### UseGridContext

Registers the GridContext middleware that extracts correlation/causation/studio headers from incoming requests and establishes Grid and Operation contexts.

```csharp
public static IApplicationBuilder UseGridContext(this IApplicationBuilder app)
```

**What It Does:**
1. Extracts Grid context from request headers (`X-Correlation-ID`, `X-Causation-ID`, etc.)
2. Creates a `GridContext` for the request
3. Sets `IGridContextAccessor.GridContext` (available to all downstream services)
4. Creates an `OperationContext` to track request timing and outcome
5. Sets `IOperationContextAccessor.Current` (available to all downstream services)
6. Echoes `X-Correlation-ID` and `X-Node-ID` to response headers
7. Cleans up ambient contexts when request completes

**Middleware Order:**

```csharp
var app = builder.Build();

// GridContext should be early in pipeline
app.UseGridContext();

// Other middleware comes after
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**Headers Extracted:**

| Request Header | Mapped To |
|----------------|-----------|
| `X-Correlation-ID` | `gridContext.CorrelationId` |
| `X-Causation-ID` | `gridContext.CausationId` |
| `X-Studio-ID` | `gridContext.StudioId` |
| `X-Baggage-*` | `gridContext.Baggage["*"]` |

**Headers Added to Response:**

| Response Header | Source |
|-----------------|--------|
| `X-Correlation-ID` | `gridContext.CorrelationId` |
| `X-Node-ID` | `nodeContext.NodeId` |

---

## Fluent Builder Pattern

### IHoneyDrunkBuilder

The `AddHoneyDrunkNode()` method returns an `IHoneyDrunkBuilder` for fluent configuration:

```csharp
public interface IHoneyDrunkBuilder
{
    IServiceCollection Services { get; }
}
```

**Chaining Configuration:**

```csharp
builder.Services
    .AddHoneyDrunkNode(options =>
    {
        options.NodeId = new NodeId("payment-service");
        options.SectorId = SectorId.WellKnown.Core;
        options.EnvironmentId = EnvironmentId.WellKnown.Production;
    })
    .Services // Access IServiceCollection for additional registrations
    .AddSingleton<IPaymentGateway, StripeGateway>()
    .AddScoped<IOrderService, OrderService>();
```

---

## Complete Example

### Production-Ready Node

```csharp
using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure HoneyDrunk Node
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-service");
    options.SectorId = new SectorId("financial-services");
    options.EnvironmentId = new EnvironmentId(builder.Environment.EnvironmentName);
    options.Version = "2.1.0";
    options.StudioId = builder.Configuration["Grid:StudioId"];
    
    options.Tags["region"] = builder.Configuration["Azure:Region"] ?? "unknown";
    options.Tags["deployment-slot"] = builder.Configuration["DeploymentSlot"] ?? "primary";
});

// Register application services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register business services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSingleton<IPaymentGateway, StripeGateway>();

var app = builder.Build();

// Validate Kernel services
app.Services.ValidateHoneyDrunkServices();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Grid context middleware (should be early)
app.UseGridContext();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTimeOffset.UtcNow
}));

app.Run();
```

### Endpoint Using Injected Contexts

```csharp
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(
    INodeContext nodeContext,
    IGridContext gridContext,
    IPaymentService paymentService,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        logger.LogInformation(
            "Processing payment on Node {NodeId} with correlation {CorrelationId}",
            nodeContext.NodeId,
            gridContext.CorrelationId);
        
        var result = await paymentService.ProcessAsync(request);
        
        return Ok(result);
    }
    
    [HttpGet("info")]
    public IActionResult GetNodeInfo()
    {
        return Ok(new
        {
            Node = new
            {
                nodeContext.NodeId,
                nodeContext.Version,
                nodeContext.StudioId,
                nodeContext.Environment,
                nodeContext.LifecycleStage
            },
            Request = new
            {
                gridContext.CorrelationId,
                gridContext.CausationId,
                gridContext.NodeId
            }
        });
    }
}
```

**Response:**

```json
{
  "node": {
    "nodeId": "payment-service",
    "version": "2.1.0",
    "studioId": "honeycomb-prod",
    "environment": "production",
    "lifecycleStage": "Running"
  },
  "request": {
    "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "causationId": "01HQXY7J3SI8W4A2M1XE6UFBZP",
    "nodeId": "payment-service"
  }
}
```

---

## Configuration from appsettings.json

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Grid": {
    "StudioId": "honeycomb-prod",
    "Environment": "production"
  },
  "Node": {
    "Id": "payment-service",
    "Sector": "financial-services",
    "Version": "2.1.0",
    "Tags": {
      "region": "us-east-1",
      "deployment-slot": "blue"
    }
  }
}
```

### Loading from Configuration

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    var nodeConfig = builder.Configuration.GetSection("Node");
    
    options.NodeId = new NodeId(nodeConfig["Id"] ?? "unknown-node");
    options.SectorId = new SectorId(nodeConfig["Sector"] ?? "core");
    options.Version = nodeConfig["Version"];
    options.StudioId = builder.Configuration["Grid:StudioId"];
    options.EnvironmentId = new EnvironmentId(
        builder.Configuration["Grid:Environment"] ?? "development");
    
    // Load tags
    var tagsSection = nodeConfig.GetSection("Tags");
    foreach (var tag in tagsSection.GetChildren())
    {
        options.Tags[tag.Key] = tag.Value ?? string.Empty;
    }
});
```

---

## Testing

### Unit Testing with AddHoneyDrunkNode

```csharp
[Fact]
public void AddHoneyDrunkNode_RegistersRequiredServices()
{
    // Arrange
    var services = new ServiceCollection();
    
    // Act
    services.AddHoneyDrunkNode(options =>
    {
        options.NodeId = new NodeId("test-node");
        options.SectorId = SectorId.WellKnown.Core;
        options.EnvironmentId = EnvironmentId.WellKnown.Development;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Assert
    Assert.NotNull(serviceProvider.GetService<INodeContext>());
    Assert.NotNull(serviceProvider.GetService<IGridContextAccessor>());
    Assert.NotNull(serviceProvider.GetService<IOperationContextAccessor>());
    Assert.NotNull(serviceProvider.GetService<IOperationContextFactory>());
    Assert.NotNull(serviceProvider.GetService<IErrorClassifier>());
}

[Fact]
public void ValidateHoneyDrunkServices_ThrowsWhenServicesNotRegistered()
{
    // Arrange
    var services = new ServiceCollection();
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(
        () => serviceProvider.ValidateHoneyDrunkServices());
    
    Assert.Contains("INodeContext", exception.Message);
}

[Fact]
public void ValidateHoneyDrunkServices_SucceedsWhenAllServicesRegistered()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddHoneyDrunkNode(options =>
    {
        options.NodeId = new NodeId("test-node");
        options.SectorId = SectorId.WellKnown.Core;
        options.EnvironmentId = EnvironmentId.WellKnown.Development;
    });
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Act & Assert - should not throw
    serviceProvider.ValidateHoneyDrunkServices();
}
```

### Integration Testing

```csharp
public class PaymentServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public PaymentServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task ProcessPayment_PropagatesCorrelationId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var correlationId = Ulid.NewUlid().ToString();
        
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        // Act
        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            Amount = 100.00m,
            Currency = "USD"
        });
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").First());
        Assert.NotNull(response.Headers.GetValues("X-Node-ID").First());
    }
}
```

---

## Migration from v0.2.x

### Before (v0.2.x)

```csharp
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "payment-service";
    options.Version = "2.1.0";
    options.StudioId = "honeycomb";
    options.Environment = "production";
    options.Tags["region"] = "us-east-1";
});

// Manually register context accessors
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();
```

### After (v0.3.0)

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-service");
    options.SectorId = SectorId.WellKnown.Core;
    options.EnvironmentId = EnvironmentId.WellKnown.Production;
    options.Version = "2.1.0";
    options.StudioId = "honeycomb";
    options.Tags["region"] = "us-east-1";
});

// Everything registered automatically!
```

**Breaking Changes:**
- ✅ `NodeId` is now strongly-typed (use `new NodeId("...")`)
- ✅ `SectorId` is now required
- ✅ `EnvironmentId` is now strongly-typed (use `new EnvironmentId("...")` or `EnvironmentId.WellKnown.*`)
- ✅ All context accessors registered automatically
- ✅ Transport binders registered automatically

---

## Summary

| Component | Purpose | Lifecycle |
|-----------|---------|-----------|
| **AddHoneyDrunkNode** | Unified service registration | Build time |
| **HoneyDrunkNodeOptions** | Node configuration | Singleton |
| **ValidateHoneyDrunkServices** | Service validation | Startup |
| **UseGridContext** | Middleware registration | Request pipeline |
| **IHoneyDrunkBuilder** | Fluent configuration | Build time |

**Key Benefits:**
- ✅ Single method registers all services
- ✅ Built-in validation prevents runtime errors
- ✅ Strongly-typed configuration with compile-time safety
- ✅ Automatic transport binder registration
- ✅ Fluent builder for chainable configuration
- ✅ Environment-aware configuration loading

**Best Practices:**
- Always call `ValidateHoneyDrunkServices()` during startup
- Place `UseGridContext()` early in middleware pipeline
- Use well-known identities when possible
- Load configuration from `appsettings.json`
- Add descriptive tags for observability

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
