# 🌐 Context - Distributed Context Propagation

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Type Model: Configuration vs Runtime](#type-model-configuration-vs-runtime)
  - [Strongly-Typed Configuration](#strongly-typed-configuration)
  - [String-Based Runtime](#string-based-runtime)
  - [Why Both?](#why-both)
  - [Conversion Flow](#conversion-flow)
  - [When to Use Each](#when-to-use-each)
- [IGridContext.cs](#igridcontextcs)
- [INodeContext.cs](#inodecontextcs)
- [IOperationContext.cs](#ioperationcontextcs)
- [IOperationContextFactory.cs](#ioperationcontextfactorycs)
- [IOperationContextAccessor.cs](#ioperationcontextaccessorcs)
- [IGridContextAccessor.cs](#igridcontextaccessorcs)
- [GridHeaderNames.cs](#gridheadernamescs)
- [NodeLifecycleStage.cs](#nodelifecyclestagecs)
- [GridContextMiddleware.cs](#gridcontextmiddlewarecs)
- [Context Mappers](#context-mappers-implementations)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

The context hierarchy provides three levels of context, each serving a different purpose:

1. **GridContext** - Per-operation context that flows across Node boundaries
2. **NodeContext** - Per-process context describing the Node's identity  
3. **OperationContext** - Per-unit-of-work context for timing and telemetry

**Location:** `HoneyDrunk.Kernel.Abstractions/Context/`

---

## Type Model: Configuration vs Runtime

### Strongly-Typed Configuration

At **configuration time** (bootstrap/DI), identity values use **strongly-typed structs** for validation:

```csharp
// HoneyDrunkNodeOptions uses strongly-typed identities
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("arcadia");               // NodeId struct
    options.SectorId = Sectors.Market;                    // SectorId struct (well-known)
    options.EnvironmentId = new EnvironmentId("production"); // EnvironmentId struct
});
```

**Benefits:**
- ✅ **Configuration-time validation** - Invalid formats caught at startup, not in production
- ✅ **IntelliSense support** - IDE autocomplete for well-known values
- ✅ **Refactoring safety** - Renames propagate correctly
- ✅ **Format enforcement** - Structs validate kebab-case, length, etc.

### String-Based Runtime

At **runtime** (context propagation/wire protocol), contexts use **strings** for performance:

```csharp
// IGridContext and INodeContext use strings
public interface IGridContext
{
    string CorrelationId { get; }  // string, not CorrelationId struct
    string NodeId { get; }         // string, not NodeId struct
    string StudioId { get; }       // string, not StudioId struct
    string Environment { get; }    // string, not EnvironmentId struct
}

public interface INodeContext
{
    string NodeId { get; }         // string, not NodeId struct
    string Version { get; }        // string
    string StudioId { get; }       // string
    string Environment { get; }    // string
}
```

**Benefits:**
- ✅ **Wire compatibility** - Direct serialization to HTTP headers, message properties, logs
- ✅ **Performance** - No struct allocation/boxing in hot paths
- ✅ **Interop** - External systems don't need to understand Kernel types
- ✅ **Simplicity** - Logging, tracing, debugging use plain strings

### Why Both?

This two-layer design provides **safety where it matters** (configuration) and **speed where it matters** (runtime):

| Layer | Type System | Purpose | Example |
|-------|-------------|---------|---------|
| **Configuration** | Strongly-typed structs | Validation at startup | `options.NodeId = new NodeId("payment-node")` |
| **Runtime** | Strings | Fast propagation across boundaries | `gridContext.NodeId` → `"payment-node"` (implicit conversion) |

### Conversion Flow

The framework handles conversion automatically during bootstrap:

```csharp
// 1. Configuration: Strongly-typed validation
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("arcadia"); // Validates format
});

// 2. Bootstrap converts to runtime strings
services.AddSingleton<INodeContext>(sp =>
{
    var opts = sp.GetRequiredService<HoneyDrunkNodeOptions>();
    return new NodeContext(
        nodeId: opts.NodeId!,  // Implicit string conversion
        version: opts.Version,
        studioId: opts.Severity!,
        environment: opts.EnvironmentId!,  // Implicit string conversion
        // ...
    );
});

// 3. Runtime: String-based for performance
public class OrderService(INodeContext nodeContext)
{
    public void LogNodeInfo()
    {
        // nodeContext.NodeId is a string here
        logger.LogInformation("Running on node: {NodeId}", nodeContext.NodeId);
    }
}
```

### Real-World Analogy

**Configuration (structs)** = Passport application  
- Strict validation: correct format, valid country codes, required fields
- Catches errors before you travel

**Runtime (strings)** = Passport at border crossings  
- Fast scanning: bar code reader, plain text fields
- No validation needed (already validated at issuance)

### When to Use Each

| Scenario | Use |
|----------|-----|
| Configuring a Node | Strongly-typed: `new NodeId("arcadia")` |
| Registering services | Strongly-typed: `options.SectorId = Sectors.Market` |
| Accessing context in code | String-based: `gridContext.NodeId` |
| Propagating context | String-based: HTTP headers, message properties |
| Logging/telemetry | String-based: `logger.LogInformation("{NodeId}", nodeContext.NodeId)` |
| Testing context behavior | String-based: `new GridContext(correlationId: "test-123", nodeId: "test-node", ...)` |

---

## IGridContext.cs

### What it is
The fundamental execution context that flows through every Grid operation.

### Real-world analogy
Like a passport that travels with a person across borders, carrying identity and stamps.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CorrelationId` | string | Groups all operations from a single user request (constant across tree - trace-id) |
| `OperationId` | string | Uniquely identifies this unit of work (span-id - new per operation) |
| `CausationId` | string? | Which operation triggered this one (parent-span-id - points to parent OperationId) |
| `NodeId` | string | Which Node is currently executing |
| `StudioId` | string | Which Studio owns this execution |
| `Environment` | string | Which environment (production, staging, development, etc.) |
| `Baggage` | IReadOnlyDictionary<string, string> | Key-value pairs propagated across boundaries |
| `Cancellation` | CancellationToken | Cooperative cancellation |
| `CreatedAtUtc` | DateTimeOffset | When this context was created |

### Key Methods

```csharp
// Create child context (causation chain)
IGridContext CreateChildContext(string? nodeId = null);

// Add baggage (propagates downstream)
IGridContext WithBaggage(string key, string value);

// Create nested scope
IDisposable BeginScope();
```

### Usage Example

```csharp
public class OrderService(IGridContext gridContext, ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        logger.LogInformation(
            "Processing order {OrderId} - Trace: {CorrelationId}, Span: {OperationId}",
            order.Id,
            gridContext.CorrelationId,
            gridContext.OperationId);
        
        // Add context baggage
        var enrichedContext = gridContext
            .WithBaggage("order_id", order.Id)
            .WithBaggage("customer_id", order.CustomerId);
        
        // Call downstream service with child context
        // Three-ID Model in action:
        // - CorrelationId stays the same (same trace)
        // - OperationId is new (new span)
        // - CausationId points to current OperationId (parent-child link)
        var childContext = enrichedContext.CreateChildContext("payment-node");
        await _paymentClient.ChargeAsync(order, childContext);
        
        // Verify three-ID relationships:
        // childContext.CorrelationId == gridContext.CorrelationId (same trace)
        // childContext.OperationId != gridContext.OperationId (new span)
        // childContext.CausationId == gridContext.OperationId (parent-child link)
    }
}
```

### Causation Chain Example

```
User Request
    ├─ CorrelationId: ABC123 (constant - trace-id)
    ├─ OperationId: OP-001 (this operation - span-id)
    └─ CausationId: null (no parent)
    ↓
API Gateway creates GridContext(CorrelationId: ABC123, OperationId: OP-002, CausationId: OP-001)
    ↓
Order Service receives context, creates child:
    GridContext(CorrelationId: ABC123, OperationId: OP-003, CausationId: OP-002)
    ↓
Payment Service receives context, creates child:
    GridContext(CorrelationId: ABC123, OperationId: OP-004, CausationId: OP-003)
```

**Key Points:**
- **CorrelationId stays constant** (`ABC123`) throughout the entire request tree (trace-id)
- **OperationId is unique** per operation (`OP-001`, `OP-002`, etc.) (span-id)
- **CausationId points to parent's OperationId** (parent-span-id) - forms the tree structure
- Trace reconstruction: All operations with `ABC123` are part of the same user request
- Span relationships: CausationId shows who-called-whom via OperationId references

---

## INodeContext.cs

### What it is
Process-scoped context describing the Node's identity and runtime state.

### Real-world analogy
Like a server's hostname and system info - static per process.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `NodeId` | string | This Node's identifier |
| `Version` | string | Semantic version of this Node |
| `StudioId` | string | Which Studio owns this Node |
| `Environment` | string | Which environment (production, staging, development, etc.) |
| `LifecycleStage` | NodeLifecycleStage | Current stage (Starting, Ready, Stopping, Stopped) |
| `StartedAtUtc` | DateTimeOffset | When this Node process started |
| `MachineName` | string | Host machine name |
| `ProcessId` | int | OS process ID |
| `Tags` | IReadOnlyDictionary<string, string> | Custom labels (sector, region, deployment-slot) for routing/filtering |

### Usage Example

```csharp
public class MetricsReporter(INodeContext nodeContext, IMetricsCollector metrics)
{
    public void ReportNodeInfo()
    {
        metrics.RecordGauge("node.uptime_seconds",
            (DateTimeOffset.UtcNow - nodeContext.StartedAtUtc).TotalSeconds,
            new KeyValuePair<string, object?>("node_id", nodeContext.NodeId),
            new KeyValuePair<string, object?>("version", nodeContext.Version));
    }
}
```

---

## IOperationContext.cs

### What it is
Bounded operation context tracking timing, outcome, and telemetry.

### Real-world analogy
Like a stopwatch and scorecard for a single task.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GridContext` | IGridContext | The Grid context for this operation |
| `OperationName` | string | Name/type of operation |
| `StartedAtUtc` | DateTimeOffset | When operation started |
| `CompletedAtUtc` | DateTimeOffset? | When operation completed (null if running) |
| `IsSuccess` | bool? | Whether operation succeeded (null if running) |
| `ErrorMessage` | string? | Error description if failed |
| `Metadata` | IReadOnlyDictionary<string, object?> | Operation-specific metadata |

### Key Methods

```csharp
void Complete();                                    // Mark as successful
void Fail(string errorMessage, Exception? ex);     // Mark as failed
void AddMetadata(string key, object? value);       // Attach metadata
```

### Usage Example

```csharp
public class PaymentProcessor(IOperationContextFactory factory)
{
    public async Task<Result> ProcessAsync(Payment payment)
    {
        using var opContext = factory.Create("ProcessPayment");
        try
        {
            opContext.AddMetadata("payment_id", payment.Id);
            opContext.AddMetadata("amount", payment.Amount);
            opContext.AddMetadata("currency", payment.Currency);
            
            var result = await ChargeAsync(payment);
            opContext.Complete();
            return result;
        }
        catch (Exception ex)
        {
            opContext.Fail($"Payment failed: {ex.Message}", ex);
            throw;
        }
        // Dispose automatically logs duration and outcome
    }
}
```

---

## IOperationContextFactory.cs

### What it is
Factory for creating `IOperationContext` instances with automatic ambient accessor management.

### Real-world analogy
Like a factory that starts stopwatches and automatically registers them in a shared scoreboard.

### Key Method

```csharp
IOperationContext Create(string operationName, IReadOnlyDictionary<string, object?>? metadata = null);
```

### Behavior
- Creates a new `IOperationContext` for the given operation name
- Uses current `IGridContext` from accessor
- Automatically sets `IOperationContextAccessor.Current` to the created context
- Returns disposable context that logs duration and outcome on disposal

### Usage Example

```csharp
public class OrderProcessor(IOperationContextFactory opFactory)
{
    public async Task ProcessAsync(Order order)
    {
        // Factory creates context AND sets ambient accessor
        using var operation = opFactory.Create("ProcessOrder", new Dictionary<string, object?>
        {
            ["order_id"] = order.Id,
            ["customer_id"] = order.CustomerId
        });
        
        try
        {
            await ValidateAsync(order);
            await SaveAsync(order);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Order processing failed", ex);
            throw;
        }
        // Dispose automatically:
        // 1. Logs operation duration
        // 2. Emits telemetry with outcome
        // 3. Clears ambient accessor
    }
}
```

### Why use Factory vs Direct Construction?

| Approach | When to Use |
|----------|-------------|
| **Factory** | Most scenarios - handles ambient accessor, telemetry hooks, standard lifecycle |
| **Direct** | Testing, custom scenarios where you control the full lifecycle |

---

## IOperationContextAccessor.cs

### What it is
Ambient accessor for current OperationContext (companion to `IGridContextAccessor`).

### Real-world analogy
Like a shared scoreboard showing the current active operation - visible from anywhere.

### Properties

```csharp
public interface IOperationContextAccessor
{
    /// <summary>
    /// Gets or sets the current operation context.
    /// </summary>
    IOperationContext? Current { get; set; }
}
```

### Usage

```csharp
public class LegacyMetricsCollector(IOperationContextAccessor opAccessor)
{
    public void RecordMetric(string name, double value)
    {
        var operation = opAccessor.Current;
        if (operation != null)
        {
            // Tag metric with current operation details
            _metrics.Record(name, value, new Dictionary<string, object?>
            {
                ["operation_name"] = operation.OperationName,
                ["correlation_id"] = operation.GridContext.CorrelationId,
                ["operation_id"] = operation.GridContext.OperationId
            });
        }
    }
}
```

### When to use
- Infrastructure code that needs operation context but can't accept injection
- Legacy code integration
- Metrics/logging systems that need current operation metadata
- Diagnostic tools that query "what's running right now?"

### How it works
- `IOperationContextFactory.Create()` automatically sets `Current` when creating operations
- Middleware (e.g., `GridContextMiddleware`) manages lifecycle - sets on request start, clears on finish
- AsyncLocal storage ensures context flows correctly across async/await boundaries

### ⚠️ Caution
Prefer explicit `IOperationContext` injection; use accessor only when necessary (async-local storage has performance overhead). The accessor is provided for scenarios where dependency injection is impractical.

---

## IGridContextAccessor.cs

### What it is
Ambient accessor for current GridContext (like `IHttpContextAccessor`).

### Real-world analogy
Like `Thread.CurrentPrincipal` - access current context anywhere.

### Usage

```csharp
public class LegacyLogger(IGridContextAccessor contextAccessor)
{
    public void Log(string message)
    {
        var context = contextAccessor.GridContext;
        if (context != null)
        {
            Console.WriteLine($"[{context.CorrelationId}] {message}");
        }
    }
}
```

### When to use
- Infrastructure code that needs context but can't accept injection
- Legacy code integration
- Deep call stacks where passing context is impractical

### ⚠️ Caution
Prefer explicit injection; use accessor only when necessary (async-local storage has performance overhead).

---

## GridHeaderNames.cs

### What it is
Standard HTTP header names for Grid context propagation.

### Real-world analogy
Like well-known HTTP headers (`Content-Type`, `Authorization`) but for Grid context.

### Header Constants

| Header | Purpose | Example |
|--------|---------|---------|
| `X-Correlation-Id` | Groups operations from single user request (trace-id) | `01HQXZ8K4TJ9X5B3N2YGF7WDCQ` |
| `X-Operation-Id` | Uniquely identifies this operation/span (span-id) | `01HQXZ8K4TJ9X5B3N2YGF7WDCR` |
| `X-Causation-Id` | Points to parent operation (parent-span-id) | `01HQXZ8K4TJ9X5B3N2YGF7WDCS` |
| `X-Studio-Id` | Which Studio owns execution | `honeydrunk-studios` |
| `X-Node-Id` | Which Node is executing | `kernel`, `payment-service` |
| `X-Environment` | Which environment | `production`, `staging` |
| `traceparent` | W3C trace context (interop) | `00-{trace-id}-{span-id}-01` |
| `baggage` | W3C baggage (comma-separated) | `tenant=abc,project=xyz` |
| `X-Baggage-*` | Custom baggage prefix | `X-Baggage-TenantId: abc123` |

### Usage in Middleware

```csharp
// Reading headers (HttpContextMapper)
var correlationId = httpContext.Request.Headers[GridHeaderNames.CorrelationId].FirstOrDefault();
var causationId = httpContext.Request.Headers[GridHeaderNames.CausationId].FirstOrDefault();

// Writing response headers
httpContext.Response.Headers[GridHeaderNames.CorrelationId] = gridContext.CorrelationId;
httpContext.Response.Headers[GridHeaderNames.NodeId] = nodeContext.NodeId;
```

### Design Notes

- **Stable contracts** - These names are used across all Grid Nodes
- **X- prefixes** - Custom headers for Grid-specific values
- **W3C interop** - `traceparent` and `baggage` for external system compatibility
- **Minimal set** - Prefer baggage for ad-hoc keys instead of defining new headers

---

## NodeLifecycleStage.cs

### What it is
Enum representing the lifecycle stages of a Node process.

### Real-world analogy
Like a server's state machine (starting → running → shutting down → stopped).

### Stages

| Stage | Description | Typical Actions |
|-------|-------------|-----------------|
| `Starting` | Node is initializing | Loading config, connecting to dependencies |
| `Ready` | Node is accepting work | Healthy, can serve requests |
| `Stopping` | Node is gracefully shutting down | Draining requests, closing connections |
| `Stopped` | Node has terminated | Process exiting |

### Usage

```csharp
public class NodeLifecycleManager(INodeContext nodeContext, ILogger logger)
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // NodeContext implementation provides SetLifecycleStage for internal use
        // (not on INodeContext interface - managed by NodeLifecycleManager)
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);
        
        // Initialize resources
        await InitializeDatabaseAsync(cancellationToken);
        await WarmupCachesAsync(cancellationToken);
        
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Ready);
        logger.LogInformation("Node {NodeId} is ready", nodeContext.NodeId);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);
        logger.LogInformation("Node {NodeId} is stopping", nodeContext.NodeId);
        
        // Drain requests, close connections
        await DrainRequestsAsync(cancellationToken);
        await CloseConnectionsAsync(cancellationToken);
        
        nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopped);
    }
}
```

**Note:** `SetLifecycleStage()` is available on the concrete `NodeContext` implementation, not on the `INodeContext` interface. Lifecycle stage mutation is managed internally by `NodeLifecycleManager` - application code reads the stage via `INodeContext.LifecycleStage` property.

---

## GridContextMiddleware.cs

### What it is
ASP.NET Core middleware that establishes Grid and Operation contexts for HTTP requests.

### Real-world analogy
Like a security checkpoint that validates passports (context) before letting travelers (requests) through.

### Responsibilities

1. **Extract context from headers** - Reads `X-Correlation-ID`, `X-Causation-ID`, etc.
2. **Create GridContext** - Maps headers to `IGridContext`
3. **Set ambient accessors** - Makes context available via `IGridContextAccessor` and `IOperationContextAccessor`
4. **Create OperationContext** - Tracks request timing and outcome
5. **Echo headers** - Returns `X-Correlation-ID` and `X-Node-ID` in response for traceability
6. **Sanitize inputs** - Defensive truncation of header values (max 256 chars)

### Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddHoneyDrunkNode(options => { /* ... */ });
builder.Services.AddGridContext();  // Registers middleware and accessors

var app = builder.Build();

// Use middleware (typically early in pipeline)
app.UseGridContext();  // Adds GridContextMiddleware

app.MapGet("/", (IGridContext gridContext) => Results.Ok(new
{
    CorrelationId = gridContext.CorrelationId,
    NodeId = gridContext.NodeId
}));

app.Run();
```

### Flow Diagram

```
Incoming Request
    ↓
[GridContextMiddleware]
    ├─ Read headers (X-Correlation-ID, X-Causation-ID, etc.)
    ├─ Create GridContext
    ├─ Set IGridContextAccessor.GridContext
    ├─ Create OperationContext (opFactory.Create("HttpRequest"))
    ├─ Set IOperationContextAccessor.Current
    ├─ Echo headers to response (OnStarting)
    ↓
[Your Controllers/Endpoints]
    ↓ (can inject IGridContext, IOperationContext)
    ↓
[GridContextMiddleware - Finally Block]
    ├─ operation.Complete() or operation.Fail()
    ├─ Clear IGridContextAccessor.GridContext
    ├─ Clear IOperationContextAccessor.Current
    ├─ operation.Dispose() (logs duration, emits telemetry)
    ↓
Response with X-Correlation-ID and X-Node-ID headers
```

### Defensive Features

**Header Length Limits:**
```csharp
private const int MaxHeaderLength = 256; // Prevent abuse
```

**Sanitization:**
- Truncates correlation/causation/studio IDs to 256 chars
- Preserves baggage as-is (upstream should filter high cardinality keys)

**Error Handling:**
- Catches unhandled exceptions
- Marks operation as failed with `operation.Fail()`
- Logs with correlation context
- Re-throws to let upstream middleware handle response

### Testing

```csharp
[Fact]
public async Task Middleware_ExtractsCorrelationId()
{
    // Arrange
    var context = new DefaultHttpContext();
    context.Request.Headers[GridHeaderNames.CorrelationId] = "test-123";
    
    var nodeContext = CreateMockNodeContext();
    var gridAccessor = new GridContextAccessor();
    var opAccessor = new OperationContextAccessor();
    var opFactory = CreateMockOperationContextFactory(gridAccessor);
    
    IGridContext? capturedContext = null;
    
    // Middleware constructor takes RequestDelegate and ILogger
    var middleware = new GridContextMiddleware(
        next: async (ctx) => 
        {
            capturedContext = gridAccessor.GridContext;
            await Task.CompletedTask;
        },
        logger: NullLogger<GridContextMiddleware>.Instance);
    
    // Act - InvokeAsync receives dependencies via DI in real scenarios
    // (Here we pass them explicitly for testing)
    await middleware.InvokeAsync(context, nodeContext, gridAccessor, opAccessor, opFactory);
    
    // Assert
    Assert.Equal("test-123", capturedContext?.CorrelationId);
}
```

**Note:** In production, ASP.NET Core DI injects `INodeContext`, `IGridContextAccessor`, `IOperationContextAccessor`, and `IOperationContextFactory` automatically into `InvokeAsync()`. The test shows the dependencies explicitly for clarity.

