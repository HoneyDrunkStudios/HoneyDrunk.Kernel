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
- [IGridContextAccessor.cs](#igridcontextaccessorcs)
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
    options.NodeId = new NodeId("payment-node");          // NodeId struct
    options.SectorId = new SectorId("financial");         // SectorId struct
    options.EnvironmentId = new EnvironmentId("production"); // EnvironmentId struct
});
```

**Benefits:**
- ✅ **Compile-time validation** - Invalid formats caught at build time
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
| **Configuration** | Strongly-typed structs | Validation at build/startup | `options.NodeId = new NodeId("payment-node")` |
| **Runtime** | Strings | Fast propagation across boundaries | `gridContext.NodeId` → `"payment-node"` |

### Conversion Flow

The framework handles conversion automatically during bootstrap:

```csharp
// 1. Configuration: Strongly-typed validation
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-node"); // Validates format
});

// 2. Bootstrap converts to runtime strings
services.AddSingleton<INodeContext>(sp =>
{
    var opts = sp.GetRequiredService<HoneyDrunkNodeOptions>();
    return new NodeContext(
        nodeId: opts.NodeId!.Value,  // .Value extracts the string
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
| Configuring a Node | Strongly-typed: `new NodeId("my-node")` |
| Registering services | Strongly-typed: `options.SectorId = Sectors.Core` |
| Accessing context in code | String-based: `gridContext.NodeId` |
| Propagating context | String-based: HTTP headers, message properties |
| Logging/telemetry | String-based: `logger.LogInformation("{NodeId}", nodeContext.NodeId)` |
| Testing context behavior | String-based: `new GridContext(correlationId: "test-123", nodeId: "test-node", ...)` |

### Migration Note

If you're coming from v0.2.x where everything was strings:

```csharp
// v0.2.x - All strings
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "payment-node";        // string
    options.Environment = "production";     // string
});

// v0.3.0 - Strongly-typed configuration
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-node");             // struct
    options.EnvironmentId = new EnvironmentId("production"); // struct
});

// Runtime behavior unchanged - still strings
public class MyService(INodeContext nodeContext)
{
    // nodeContext.NodeId is still a string (unchanged)
}
```

---

## IGridContext.cs

### What it is
The fundamental execution context that flows through every Grid operation.

### Real-world analogy
Like a passport that travels with a person across borders, carrying identity and stamps.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CorrelationId` | string | Groups all operations from a single user request |
| `CausationId` | string? | Which operation triggered this one (parent-child chain) |
| `NodeId` | string | Which Node is currently executing |
| `StudioId` | string | Which Studio/environment owns this execution |
| `Environment` | string | Production, staging, development, etc. |
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
            "Processing order {OrderId} with {CorrelationId}",
            order.Id,
            gridContext.CorrelationId);
        
        // Add context baggage
        var enrichedContext = gridContext
            .WithBaggage("order_id", order.Id)
            .WithBaggage("customer_id", order.CustomerId);
        
        // Call downstream service with child context
        var childContext = enrichedContext.CreateChildContext("payment-node");
        await _paymentClient.ChargeAsync(order, childContext);
    }
}
```

### Causation Chain Example

```
User Request (CorrelationId: ABC123)
    ↓
API Gateway creates GridContext(CorrelationId: ABC123, CausationId: null)
    ↓
Order Service receives context, creates child:
    GridContext(CorrelationId: XYZ789, CausationId: ABC123)
    ↓
Payment Service receives context, creates child:
    GridContext(CorrelationId: DEF456, CausationId: XYZ789)
```

Trace reconstruction: DEF456 → XYZ789 → ABC123

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
| `Environment` | string | Which environment (production, staging, etc.) |
| `LifecycleStage` | NodeLifecycleStage | Current stage (Starting, Ready, Stopping, Stopped) |
| `StartedAtUtc` | DateTimeOffset | When this Node process started |
| `MachineName` | string | Host machine name |
| `ProcessId` | int | OS process ID |
| `Tags` | IReadOnlyDictionary<string, string> | Custom labels for routing/filtering |

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

## Context Mappers (Implementations)

**Location:** `HoneyDrunk.Kernel/Context/Mappers/`

### HttpContextMapper.cs
Maps HTTP request headers to GridContext:
- Reads `X-Correlation-ID`, `X-Causation-ID` from headers
- Extracts baggage from `X-Baggage-*` headers
- Creates GridContext for incoming HTTP requests

### JobContextMapper.cs
Maps background job metadata to GridContext:
- Reads correlation/causation from job payload
- Attaches job-specific baggage
- Enables tracing across async boundaries

### MessagingContextMapper.cs
Maps message properties to GridContext:
- Reads from message headers/properties
- Propagates context across message brokers
- Maintains causation chains in event-driven architectures

---

## Testing Patterns

```csharp
[Fact]
public async Task GridContext_CreateChildContext_SetsCausationId()
{
    // Arrange
    var parentContext = new GridContext(
        correlationId: "parent-123",
        causationId: null,
        nodeId: "test-node",
        studioId: "test-studio",
        environment: "test",
        baggage: new Dictionary<string, string>(),
        cancellation: CancellationToken.None);
    
    // Act
    var childContext = parentContext.CreateChildContext("child-node");
    
    // Assert
    Assert.NotEqual(parentContext.CorrelationId, childContext.CorrelationId);
    Assert.Equal(parentContext.CorrelationId, childContext.CausationId);
    Assert.Equal("child-node", childContext.NodeId);
}

[Fact]
public async Task OperationContext_Dispose_CompletesAutomatically()
{
    // Arrange
    var factory = new OperationContextFactory(gridContext, metrics);
    
    // Act
    using (var operation = factory.Create("TestOperation"))
    {
        operation.AddMetadata("key", "value");
        // Not calling Complete() or Fail()
    }
    
    // Assert - disposed operation auto-completes if not explicitly failed
}
```

---

## Summary

| Context Type | Scope | Lifetime | Purpose |
|-------------|-------|----------|---------|
| **GridContext** | Per-operation | Request duration | Correlation, causation, baggage |
| **NodeContext** | Per-process | Process lifetime | Node identity, version, lifecycle |
| **OperationContext** | Per-unit-of-work | Operation duration | Timing, outcome, metadata |

**Key Patterns:**
- GridContext flows across boundaries (HTTP, messaging, RPC)
- NodeContext provides static identity (health checks, metrics)
- OperationContext wraps units of work (requests, jobs, messages)

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

