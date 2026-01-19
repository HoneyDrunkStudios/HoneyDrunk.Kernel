# 🚀 Bootstrapping - Node Initialization and Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [AddHoneyDrunkNode](#AddHoneyDrunkNode)
- [GridOptions](#gridoptions)
- [Basic Usage](#basic-usage)
- [Service Validation](#service-validation)
- [Middleware Registration](#middleware-registration)
- [Fluent Builder Pattern](#fluent-builder-pattern)
- [Complete Example](#complete-example)
- [Configuration from appsettings.json](#configuration-from-appsettingsjson)
- [Testing](#testing)
- [Summary](#summary)

---

## Overview

Bootstrapping provides a unified, opinionated way to configure HoneyDrunk Nodes with all required services, validation, and middleware in a single registration call.

**Location:** `HoneyDrunk.Kernel/Hosting/`

**Key Concepts:**
- **Unified Registration** - Single `AddHoneyDrunkNode()` call registers all services
- **Service Validation** - Built-in validation ensures required services are present
- **Middleware Helpers** - Easy middleware registration with `UseGridContext()`
- **String-Based Configuration** - Runtime uses plain strings for performance (no value objects)

---

## AddHoneyDrunkNode

### What it is
The primary bootstrapping method that registers all HoneyDrunk Kernel services in a single call.

### Real-world analogy
Like a "turnkey solution" for a house - walls, plumbing, electricity, and HVAC all installed with one contract, guaranteed to work together.

### v0.4.0 Registration Guard

**Breaking Change:** `AddHoneyDrunkNode()` now includes a **registration guard** that prevents duplicate registration:

```csharp
// First call - registers all services
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = "payment-service";
    options.StudioId = "honeycomb";
    options.Environment = "production";
});

// Second call - throws InvalidOperationException!
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = "other-node"; // ERROR: Already registered
    // ...
});
```

**Rationale:** Previously, calling `AddHoneyDrunkNode()` multiple times would silently replace services, leading to unpredictable behavior. Now you get a clear exception at startup.

### Method Signature

```csharp
public static IServiceCollection AddHoneyDrunkNode(
    this IServiceCollection services,
    Action<GridOptions> configure)
```

### What It Registers

| Service | Lifetime | Description |
|---------|----------|-------------|
| `INodeContext` | Singleton | Static Node identity and metadata |
| `IGridContext` | Scoped | Default Grid context for operations |
| `IMetricsCollector` | Singleton | Metrics collection (defaults to `NoOpMetricsCollector`) |
| `NodeLifecycleHost` | IHostedService | Startup/shutdown hook orchestration |

**Additional Services (register separately):**
- `IGridContextAccessor` - Register with `services.AddSingleton<IGridContextAccessor, GridContextAccessor>()`
- `IOperationContextAccessor` - Register with `services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>()`
- `IOperationContextFactory` - Register with `services.AddScoped<IOperationContextFactory, OperationContextFactory>()`
- `ITransportEnvelopeBinder` implementations - Register transport binders as needed
- `NodeLifecycleManager` - Register with `services.AddSingleton<NodeLifecycleManager>()`

**Returns:** `IServiceCollection` for fluent configuration chaining.

**Design Note:** The current implementation registers core services. Additional context accessors, transport binders, and lifecycle coordination services are registered separately as shown in the examples below. Future versions may consolidate all registrations into a single call.

---

## GridOptions

### Configuration Properties

```csharp
public sealed class GridOptions
{
    /// <summary>
    /// Required: The Node identifier.
    /// Example: "payment-node", "notification-node".
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Required: The Node version.
    /// Example: "1.0.0", "2.1.3-beta".
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Required: The Studio identifier.
    /// Example: "honeycomb", "staging".
    /// </summary>
    public string StudioId { get; set; } = string.Empty;
    
    /// <summary>
    /// Required: The environment name.
    /// Example: "production", "staging", "development".
    /// </summary>
    public string Environment { get; set; } = "development";
    
    /// <summary>
    /// Optional: Additional metadata tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; } = new();
}
```

**Validation:** The bootstrapping extension validates that `NodeId` and `StudioId` are not empty. Other validation happens at runtime.

---

## Basic Usage

### Minimal Configuration

```csharp
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register Grid services (core only)
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = "payment-service";
    options.StudioId = "honeycomb";
    options.Environment = "production";
    options.Version = "1.0.0";
});

// Register additional services
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

var app = builder.Build();

// Validate services (optional but recommended)
app.Services.ValidateHoneyDrunkServices();

app.Run();
```

### Full Configuration

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    // Required: Node identity
    options.NodeId = "payment-service";
    options.StudioId = "honeycomb-prod";
    options.Environment = builder.Environment.EnvironmentName; // "Production", "Development", etc.
    
    // Optional: Version (defaults to "1.0.0")
    options.Version = "2.1.0";
    
    // Optional: Metadata tags
    options.Tags["region"] = "us-east-1";
    options.Tags["deployment-slot"] = "blue";
    options.Tags["cost-center"] = "engineering";
});

// Register context accessors
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();

// Register context factory
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

// Register lifecycle coordination
builder.Services.AddSingleton<NodeLifecycleManager>();

// Register health/readiness contributors as needed
builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
builder.Services.AddSingleton<IReadinessContributor, NodeContextReadinessContributor>();
```

---

## Service Validation

### ValidateHoneyDrunkServices

Validates that required Kernel services are properly registered. Should be called during application startup before processing requests.

```csharp
public static void ValidateHoneyDrunkServices(this IServiceProvider serviceProvider)
```

**Validates:**
- ✅ `INodeContext` - Static Node identity
- ✅ `IGridContextAccessor` - Ambient Grid context accessor
- ✅ `IOperationContextAccessor` - Ambient operation context accessor
- ✅ `IOperationContextFactory` - Operation context factory
- ✅ `INodeDescriptor` - Node descriptor (if registered)
- ✅ `IErrorClassifier` - Error classification (if registered)
- ✅ `NodeLifecycleManager` - Lifecycle coordination (if registered)
- ✅ `NodeLifecycleHost` - Startup/shutdown orchestration (via `IHostedService`)

**Behavior:**
- **Throws `InvalidOperationException`** if core services (`INodeContext`, context accessors) are missing
- **Logs warnings** if recommended services are missing (transport binders, lifecycle services)

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
  - IGridContextAccessor is missing. Register via: services.AddSingleton<IGridContextAccessor, GridContextAccessor>()
```

---

## Middleware Registration

### UseGridContext

Registers the GridContext middleware that initializes Grid context for incoming requests.

```csharp
public static IApplicationBuilder UseGridContext(this IApplicationBuilder app)
```

### v0.4.0 Two-Phase Initialization

**Breaking Change:** In v0.4.0, `GridContext` uses a **two-phase initialization** pattern:

1. **DI creates GridContext** - Constructor sets `NodeId`, `StudioId`, `Environment` from `INodeContext`
2. **Middleware initializes it** - Calls `Initialize()` to set `CorrelationId`, `CausationId`, `Baggage` from HTTP headers

The middleware no longer creates a new context; it **initializes the existing scoped context**:

```csharp
// Middleware behavior (conceptual):
public async Task InvokeAsync(HttpContext httpContext, IGridContext gridContext, ...)
{
    // GridContext is already created by DI, but not initialized
    // Middleware extracts headers and initializes it:
    var correlationId = httpContext.Request.Headers[GridHeaderNames.CorrelationId].FirstOrDefault()
                        ?? Ulid.NewUlid().ToString();
    var causationId = httpContext.Request.Headers[GridHeaderNames.CausationId].FirstOrDefault();
    
    // Initialize the existing context with request-specific values
    gridContext.Initialize(correlationId, causationId, baggage);
    
    await _next(httpContext);
}
```

### What It Does:
1. **Extracts** correlation/causation/baggage from request headers
2. **Initializes** the existing `IGridContext` (already resolved from DI)
3. Sets `IGridContextAccessor.GridContext` (available to all downstream services)
4. Echoes `X-Correlation-Id` and `X-Node-Id` to response headers
5. Cleans up ambient context when request completes

**Note:** The middleware does **not** automatically create `IOperationContext`. If you need operation tracking, create `OperationContext` explicitly in your services using `IOperationContextFactory`.

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
| `X-Correlation-Id` | `gridContext.CorrelationId` |
| `X-Causation-Id` | `gridContext.CausationId` |
| `X-Studio-Id` | Ignored (uses `INodeContext.StudioId`) |
| `X-Baggage-*` | `gridContext.Baggage["*"]` |

**Headers Added to Response:**

| Response Header | Source |
|-----------------|--------|
| `X-Correlation-Id` | `gridContext.CorrelationId` |
| `X-Node-Id` | `nodeContext.NodeId` |

---

## Fluent Builder Pattern

### Chaining Configuration

The `AddHoneyDrunkNode()` method returns `IServiceCollection` for fluent configuration:

```csharp
builder.Services
    .AddHoneyDrunkNode(options =>
    {
        options.NodeId = "payment-service";
        options.StudioId = "honeycomb";
        options.Environment = "production";
        options.Version = "2.1.0";
    })
    .AddSingleton<IGridContextAccessor, GridContextAccessor>()
    .AddScoped<IOperationContextFactory, OperationContextFactory>()
    .AddSingleton<IPaymentGateway, StripeGateway>()
    .AddScoped<IOrderService, OrderService>();
```

---

## Complete Example

### Production-Ready Node

```csharp
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Hosting;
using HoneyDrunk.Kernel.Lifecycle;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure HoneyDrunk Grid
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = "payment-service";
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "honeycomb";
    options.Environment = builder.Environment.EnvironmentName;
    options.Version = "2.1.0";
    
    options.Tags["region"] = builder.Configuration["Azure:Region"] ?? "unknown";
    options.Tags["deployment-slot"] = builder.Configuration["DeploymentSlot"] ?? "primary";
});

// Register context services
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

// Register lifecycle coordination
builder.Services.AddSingleton<NodeLifecycleManager>();

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
app.MapGet("/health", (NodeLifecycleManager lifecycleManager) => 
{
    var (status, details) = await lifecycleManager.CheckHealthAsync();
    return Results.Json(new { status, details });
});

// Readiness endpoint
app.MapGet("/ready", (NodeLifecycleManager lifecycleManager) => 
{
    var (isReady, details) = await lifecycleManager.CheckReadinessAsync();
    return isReady ? Results.Ok(new { ready = true }) : Results.StatusCode(503);
});

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
    "environment": "Production",
    "lifecycleStage": "Ready"
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
    "NodeId": "payment-service",
    "StudioId": "honeycomb-prod",
    "Environment": "production",
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
    var gridConfig = builder.Configuration.GetSection("Grid");
    
    options.NodeId = gridConfig["NodeId"] ?? throw new InvalidOperationException("Grid:NodeId is required");
    options.StudioId = gridConfig["StudioId"] ?? throw new InvalidOperationException("Grid:StudioId is required");
    options.Environment = gridConfig["Environment"] ?? builder.Environment.EnvironmentName;
    options.Version = gridConfig["Version"] ?? "1.0.0";
    
    // Load tags
    var tagsSection = gridConfig.GetSection("Tags");
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
        options.NodeId = "test-node";
        options.StudioId = "test-studio";
        options.Environment = "test";
    });
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Assert
    Assert.NotNull(serviceProvider.GetService<INodeContext>());
    Assert.NotNull(serviceProvider.GetService<IMetricsCollector>());
}

[Fact]
public void ValidateHoneyDrunkServices_ThrowsWhenCoreServicesNotRegistered()
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
        options.NodeId = "test-node";
        options.StudioId = "test-studio";
        options.Environment = "test";
    });
    
    // Add additional required services
    services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
    services.AddScoped<IOperationContextFactory, OperationContextFactory>();
    
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
        
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
        
        // Act
        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            Amount = 100.00m,
            Currency = "USD"
        });
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-Id").First());
        Assert.NotNull(response.Headers.GetValues("X-Node-Id").First());
    }
}
```

---

## Summary

| Component | Purpose | Signature |
|-----------|---------|-----------|
| **AddHoneyDrunkNode** | Unified service registration | `IServiceCollection AddHoneyDrunkNode(this IServiceCollection, Action<GridOptions>)` |
| **GridOptions** | Node configuration | String-based (NodeId, StudioId, Environment, Version, Tags) |
| **ValidateHoneyDrunkServices** | Service validation | `void ValidateHoneyDrunkServices(this IServiceProvider)` |
| **UseGridContext** | Middleware registration | `IApplicationBuilder UseGridContext(this IApplicationBuilder)` |

**Key Benefits:**
- ✅ Core services registered with one call
- ✅ Built-in validation prevents runtime errors
- ✅ String-based configuration for performance
- ✅ Fluent builder for chainable configuration
- ✅ Environment-aware configuration loading

**Best Practices:**
- Always call `ValidateHoneyDrunkServices()` during startup
- Place `UseGridContext()` early in middleware pipeline
- Load configuration from `appsettings.json`
- Add descriptive tags for observability
- Register additional services (context accessors, lifecycle) explicitly

**Current State (v0.3.0):**
- `AddHoneyDrunkNode()` registers core services (`INodeContext`, scoped `IGridContext`, `IMetricsCollector`, `NodeLifecycleHost`)
- Additional services (context accessors, lifecycle manager, transport binders) are registered separately
- Future versions may consolidate all registrations into a single bootstrap call

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
