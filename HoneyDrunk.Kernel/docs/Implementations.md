# 🔧 Implementations - Kernel Runtime

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Context Implementations](#context-implementations)
- [Context Mappers](#context-mappers)
- [Middleware](#middleware)
- [Lifecycle](#lifecycle)
- [Diagnostics](#diagnostics)
- [Configuration](#configuration)
- [Dependency Injection](#dependency-injection)
- [Summary](#summary)

---

## Overview

This document covers the runtime implementations in `HoneyDrunk.Kernel` project. These are the concrete classes that bring the abstractions to life.

**Location:** `HoneyDrunk.Kernel/`

**Philosophy:** Implementations follow "thin wrapper" principles - delegate to BCL where possible, add Grid-specific semantics where necessary.

[↑ Back to top](#table-of-contents)

---

## Context Implementations

### GridContext.cs

Default implementation of `IGridContext` carrying distributed tracing and baggage.

**Location:** `HoneyDrunk.Kernel/Context/GridContext.cs`

```csharp
public sealed class GridContext : IGridContext
{
    public string CorrelationId { get; }
    public string? CausationId { get; }
    public string NodeId { get; }
    public string StudioId { get; }
    public string Environment { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public IReadOnlyDictionary<string, string> Baggage { get; }
    
    public IGridContext CreateChildContext(string targetNodeId)
    {
        return new GridContext(
            correlationId: CorrelationId,          // Same correlation
            nodeId: targetNodeId,                   // New node
            studioId: StudioId,
            environment: Environment,
            causationId: CorrelationId,             // Parent correlation becomes causation
            baggage: Baggage,
            createdAtUtc: DateTimeOffset.UtcNow
        );
    }
}
```

**Key Features:**
- Immutable by design
- `CreateChildContext()` preserves correlation across Node boundaries
- CausationId tracks parent-child relationships
- Baggage flows automatically to children

**Usage:**
```csharp
// Create root context
var rootContext = new GridContext(
    correlationId: Ulid.NewUlid().ToString(),
    nodeId: "api-gateway",
    studioId: "demo-studio",        // Studio identifier, not environment
    environment: "production"
);

// Create child for downstream call
var childContext = rootContext.CreateChildContext("payment-service");

Assert.Equal(rootContext.CorrelationId, childContext.CorrelationId);
Assert.Equal(rootContext.CorrelationId, childContext.CausationId); // Causality!
```

[↑ Back to top](#table-of-contents)

---

### NodeContext.cs

Static Node identity implementation carrying process-level metadata.

**Location:** `HoneyDrunk.Kernel/Context/NodeContext.cs`

```csharp
public sealed class NodeContext : INodeContext
{
    public string NodeId { get; }
    public string Version { get; }
    public string StudioId { get; }
    public string Environment { get; }
    public NodeLifecycleStage LifecycleStage { get; private set; }
    public DateTimeOffset StartedAtUtc { get; }
    public string MachineName { get; }
    public int ProcessId { get; }
    public IReadOnlyDictionary<string, string> Tags { get; }
    
    public void SetLifecycleStage(NodeLifecycleStage stage)
    {
        LifecycleStage = stage;
    }
}
```

**Key Features:**
- Singleton-scoped (one per process)
- Mutable `LifecycleStage` for startup/shutdown tracking
- Captures machine/process metadata (hostname, process ID)
- Tags for observability (region, deployment slot, etc.)

**Lifecycle Stages:**
```csharp
NodeLifecycleStage.Initializing  // Just started (default)
NodeLifecycleStage.Starting      // Running startup hooks
NodeLifecycleStage.Ready         // Ready to serve traffic
NodeLifecycleStage.Degraded      // Operational but impaired
NodeLifecycleStage.Stopping      // Running shutdown hooks
NodeLifecycleStage.Stopped       // Gracefully stopped
NodeLifecycleStage.Failed        // Fatal error
```

[↑ Back to top](#table-of-contents)

---

### OperationContext.cs

Per-operation tracking implementation with timing and outcome.

**Location:** `HoneyDrunk.Kernel/Context/OperationContext.cs`

```csharp
public sealed class OperationContext : IOperationContext
{
    private readonly IGridContext _gridContext;
    private readonly string _operationId;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly Dictionary<string, object?> _metadata;
    
    public IGridContext GridContext => _gridContext;
    public string OperationName { get; }
    public string OperationId => _operationId;
    public string CorrelationId => _gridContext.CorrelationId;
    public string? CausationId => _gridContext.CausationId;
    public DateTimeOffset StartedAtUtc => _startedAtUtc;
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public bool? IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyDictionary<string, object?> Metadata => _metadata;
    
    public void Complete()
    {
        if (CompletedAtUtc.HasValue) return;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        IsSuccess = true;
        // Runtime integration point: telemetry providers can observe completion via OperationContextAccessor
    }
    
    public void Fail(string errorMessage, Exception? exception = null)
    {
        if (CompletedAtUtc.HasValue) return;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        IsSuccess = false;
        ErrorMessage = errorMessage;
        // Runtime integration point: telemetry providers can observe failure via OperationContextAccessor
    }
    
    public void AddMetadata(string key, object? value)
    {
        _metadata[key] = value;
    }
}
```

**Key Features:**
- Scoped lifetime (per-request, per-job, per-message)
- Links `IGridContext` properties for convenience
- Tracks timing (start → complete/fail)
- Metadata for custom tags
- **Telemetry Integration:** Intended to be observed by telemetry providers (e.g., `ITelemetryActivityFactory`, `IMetricsCollector`) to record duration and outcome

**Usage:**
```csharp
public class OrderService(IOperationContext operationContext)
{
    public async Task ProcessOrderAsync(Order order)
    {
        operationContext.AddMetadata("orderId", order.Id);
        operationContext.AddMetadata("customerId", order.CustomerId);
        
        try
        {
            await ValidateAndSaveAsync(order);
            operationContext.Complete(); // Success
        }
        catch (Exception ex)
        {
            operationContext.Fail("Order processing failed", ex);
            throw;
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

### GridContextAccessor.cs

Async-local storage for ambient context access.

**Location:** `HoneyDrunk.Kernel/Context/GridContextAccessor.cs`

```csharp
public sealed class GridContextAccessor : IGridContextAccessor
{
    private static readonly AsyncLocal<IGridContext?> _current = new();
    
    public IGridContext? GridContext
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

**Key Features:**
- Uses `AsyncLocal<T>` for async flow
- Automatically flows across `await` boundaries
- Isolated per async execution context
- Singleton-scoped service

**How It Works:**
```csharp
// Middleware sets context
_accessor.GridContext = new GridContext(...);

// Service reads context
var correlationId = _accessor.GridContext.CorrelationId;

// Context flows across async calls
await CallDownstreamServiceAsync(); // GridContext still available
```

[↑ Back to top](#table-of-contents)

---

### OperationContextAccessor.cs

Async-local storage for operation context.

**Location:** `HoneyDrunk.Kernel/Context/OperationContextAccessor.cs`

```csharp
public sealed class OperationContextAccessor : IOperationContextAccessor
{
    private static readonly AsyncLocal<IOperationContext?> _current = new();
    
    public IOperationContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

Same pattern as `GridContextAccessor` but for `IOperationContext`.

[↑ Back to top](#table-of-contents)

---

### OperationContextFactory.cs

Factory for creating `IOperationContext` instances.

**Location:** `HoneyDrunk.Kernel/Context/OperationContextFactory.cs`

```csharp
public sealed class OperationContextFactory : IOperationContextFactory
{
    private readonly IGridContextAccessor _gridContextAccessor;
    
    public OperationContextFactory(IGridContextAccessor gridContextAccessor)
    {
        _gridContextAccessor = gridContextAccessor;
    }
    
    public IOperationContext Create(string operationName)
    {
        var gridContext = _gridContextAccessor.GridContext 
            ?? throw new InvalidOperationException("GridContext not set");
        
        return new OperationContext(gridContext, operationName);
    }
}
```

**Usage:**
```csharp
public class MessageConsumer(IOperationContextFactory factory)
{
    public async Task HandleAsync(Message message)
    {
        var operation = factory.Create("ProcessMessage");
        
        operation.AddMetadata("messageId", message.Id);
        
        try
        {
            await ProcessMessageAsync(message);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Message processing failed", ex);
            throw;
        }
    }
}
```

**Note:** `IOperationContext` does not implement `IDisposable`. Call `Complete()` or `Fail()` explicitly to mark operation outcome. Telemetry providers can observe operation state via `IOperationContextAccessor`.

[↑ Back to top](#table-of-contents)

---

## Context Mappers

Context mappers extract GridContext from transport-specific envelopes. See [Transport.md](Transport.md) for detailed mapper documentation.

### HttpContextMapper.cs

Extracts GridContext from HTTP request headers using canonical `GridHeaderNames`.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/HttpContextMapper.cs`

**Headers Read:**
- `GridHeaderNames.CorrelationId` (`X-Correlation-Id`) → `CorrelationId`
- `GridHeaderNames.CausationId` (`X-Causation-Id`) → `CausationId`
- `GridHeaderNames.NodeId` (`X-Node-Id`) → `NodeId`
- `GridHeaderNames.StudioId` (`X-Studio-Id`) → `StudioId`
- `GridHeaderNames.Environment` (`X-Environment`) → `Environment`
- `GridHeaderNames.BaggagePrefix` (`X-Baggage-*`) → `Baggage`

### MessagingContextMapper.cs

Extracts GridContext from message properties.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/MessagingContextMapper.cs`

### JobContextMapper.cs

Extracts GridContext from background job metadata.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/JobContextMapper.cs`

[↑ Back to top](#table-of-contents)

---

## Middleware

### GridContextMiddleware.cs

ASP.NET Core middleware that creates GridContext for each HTTP request.

**Location:** `HoneyDrunk.Kernel/Context/Middleware/GridContextMiddleware.cs`

```csharp
public class GridContextMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(
        HttpContext httpContext,
        IGridContextAccessor contextAccessor,
        INodeContext nodeContext)
    {
        // Extract or create GridContext using canonical header names
        var correlationId = httpContext.Request.Headers[GridHeaderNames.CorrelationId].FirstOrDefault()
            ?? Ulid.NewUlid().ToString();
        
        var gridContext = new GridContext(
            correlationId: correlationId,
            nodeId: nodeContext.NodeId,
            studioId: nodeContext.StudioId,
            environment: nodeContext.Environment,
            causationId: httpContext.Request.Headers[GridHeaderNames.CausationId].FirstOrDefault(),
            baggage: ExtractBaggage(httpContext.Request.Headers)
        );
        
        // Set ambient context
        contextAccessor.GridContext = gridContext;
        
        try
        {
            // Echo correlation to response using canonical names
            httpContext.Response.Headers[GridHeaderNames.CorrelationId] = gridContext.CorrelationId;
            httpContext.Response.Headers[GridHeaderNames.NodeId] = gridContext.NodeId;
            
            await _next(httpContext);
        }
        finally
        {
            // Clean up
            contextAccessor.GridContext = null;
        }
    }
    
    private Dictionary<string, string> ExtractBaggage(IHeaderDictionary headers)
    {
        var baggage = new Dictionary<string, string>();
        
        foreach (var (key, value) in headers)
        {
            if (key.StartsWith(GridHeaderNames.BaggagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var baggageKey = key.Substring(GridHeaderNames.BaggagePrefix.Length);
                baggage[baggageKey] = value.ToString();
            }
        }
        
        return baggage;
    }
}
```

**Registered via:**
```csharp
app.UseGridContext(); // Extension method
```

**Note:** In more advanced setups, `GridContextMiddleware` can delegate to `HttpContextMapper` instead of inline extraction, enabling custom header mappings and protocol adapters.

[↑ Back to top](#table-of-contents)

---

## Lifecycle

Runtime lifecycle orchestration is implemented in the following components:

### NodeLifecycleHost

**Location:** `HoneyDrunk.Kernel/Lifecycle/NodeLifecycleHost.cs`

Implements `IHostedService` and orchestrates Node startup and shutdown using:
- `IStartupHook` (priority ordered, lower first)
- `INodeLifecycle` implementations
- `IShutdownHook` (priority ordered, lower first)

**Transitions `INodeContext.LifecycleStage` through:**
```
Initializing → Starting → Ready → Stopping → Stopped (or Failed on error)
```

**Startup Flow:**
1. Initial state: `Initializing` (set by `NodeContext` constructor)
2. Transition to `Starting`
3. Execute `IStartupHook` instances by priority (lowest first)
4. Execute `INodeLifecycle.StartAsync` for all registered lifecycles
5. Transition to `Ready`
6. On error: Transition to `Failed`

**Shutdown Flow:**
1. Transition to `Stopping`
2. Execute `INodeLifecycle.StopAsync` for all registered lifecycles
3. Execute `IShutdownHook` instances by priority (lowest first)
4. Transition to `Stopped`
5. On error: Transition to `Failed`

### NodeLifecycleManager

**Location:** `HoneyDrunk.Kernel/Lifecycle/NodeLifecycleManager.cs`

Aggregates `IHealthContributor` and `IReadinessContributor` and provides:
- `CheckHealthAsync(...)` - Returns aggregated `HealthStatus` + per-contributor details
- `CheckReadinessAsync(...)` - Returns aggregated readiness + per-contributor details
- `TransitionToStage(NodeLifecycleStage newStage)` - Updates lifecycle stage with telemetry

Used by health/readiness endpoints and background health monitors.

**Health Aggregation:**
- Critical contributors with `Unhealthy` status → Node is `Unhealthy` (fail-fast)
- Any contributor with `Degraded` status → Node is `Degraded`
- All contributors `Healthy` → Node is `Healthy`

**Readiness Aggregation:**
- Any required contributor not ready → Node is `Not Ready`
- All required contributors ready → Node is `Ready`

**See [Lifecycle.md](Lifecycle.md) for complete behavior and state machine.**

[↑ Back to top](#table-of-contents)

---

## Diagnostics

### NoOpMetricsCollector.cs

Zero-overhead metrics stub for when metrics are disabled.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NoOpMetricsCollector.cs`

```csharp
internal sealed class NoOpMetricsCollector : IMetricsCollector
{
    public void RecordCounter(string name, long value, params KeyValuePair<string, object?>[] tags) { }
    
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags) { }
    
    public void RecordGauge(string name, double value, params KeyValuePair<string, object?>[] tags) { }
}
```

**Registered When:** Metrics are disabled in configuration or no concrete collector is registered.

[↑ Back to top](#table-of-contents)

---

### NodeContextReadinessContributor.cs

Readiness contributor ensuring NodeContext is initialized and in appropriate stage.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NodeContextReadinessContributor.cs`

```csharp
public sealed class NodeContextReadinessContributor : IReadinessContributor
{
    private readonly INodeContext _nodeContext;
    
    public NodeContextReadinessContributor(INodeContext nodeContext)
    {
        _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
    }
    
    public string Name => "node-context";
    public int Priority => 0; // Run first
    public bool IsRequired => true;
    
    public Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        // Check that Node context has valid data
        if (string.IsNullOrWhiteSpace(_nodeContext.NodeId))
        {
            return Task.FromResult((false, (string?)"NodeId is not set"));
        }
        
        // Check that Node is in appropriate stage for readiness
        var stage = _nodeContext.LifecycleStage;
        if (stage is NodeLifecycleStage.Initializing or NodeLifecycleStage.Starting)
        {
            return Task.FromResult((false, (string?)$"Node is still {stage}"));
        }
        
        if (stage is NodeLifecycleStage.Failed or NodeLifecycleStage.Stopped or NodeLifecycleStage.Stopping)
        {
            return Task.FromResult((false, (string?)$"Node is {stage}"));
        }
        
        // Ready or Degraded are both considered ready
        return Task.FromResult((true, (string?)null));
    }
}
```

**Readiness Mapping:**
- `Ready` → Ready ✅
- `Degraded` → Ready ⚠️ (operational but impaired)
- `Initializing` / `Starting` → Not Ready (still starting up)
- `Stopping` / `Stopped` / `Failed` → Not Ready (not operational)

[↑ Back to top](#table-of-contents)

---

### ConfigurationValidator.cs

Validates Grid configuration at startup.

**Location:** `HoneyDrunk.Kernel/Diagnostics/ConfigurationValidator.cs`

```csharp
public sealed class ConfigurationValidator
{
    public void Validate(HoneyDrunkNodeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        if (string.IsNullOrWhiteSpace(options.NodeId))
            throw new InvalidOperationException("NodeId is required");
        
        if (string.IsNullOrWhiteSpace(options.StudioId))
            throw new InvalidOperationException("StudioId is required");
        
        if (string.IsNullOrWhiteSpace(options.Environment))
            throw new InvalidOperationException("Environment is required");
        
        // Validate version format
        if (!string.IsNullOrWhiteSpace(options.Version))
        {
            if (!Version.TryParse(options.Version, out _))
                throw new InvalidOperationException($"Invalid version format: {options.Version}");
        }
    }
}
```

Fails fast if configuration is invalid.

[↑ Back to top](#table-of-contents)

---

## Configuration

### StudioConfiguration.cs

Studio configuration implementation.

**Location:** `HoneyDrunk.Kernel/Configuration/StudioConfiguration.cs`

```csharp
public sealed class StudioConfiguration : IStudioConfiguration
{
    private readonly IConfiguration _configuration;
    
    public string StudioId { get; }
    public string EnvironmentName { get; }
    
    public string? GetValue(string key)
    {
        return _configuration[key];
    }
    
    public T? GetValue<T>(string key)
    {
        return _configuration.GetValue<T>(key);
    }
}
```

Thin wrapper over `IConfiguration` with Studio identity.

[↑ Back to top](#table-of-contents)

---

## Dependency Injection

### HoneyDrunkCoreExtensions.cs

Core service registration extension methods.

**Location:** `HoneyDrunk.Kernel/DependencyInjection/HoneyDrunkCoreExtensions.cs`

Registers all Kernel services. See [Bootstrapping.md](Bootstrapping.md) for details.

[↑ Back to top](#table-of-contents)

---

### ServiceProviderValidation.cs

Validates required services are registered at startup.

**Location:** `HoneyDrunk.Kernel/DependencyInjection/ServiceProviderValidation.cs`

```csharp
public static class ServiceProviderValidation
{
    public static void ValidateHoneyDrunkServices(this IServiceProvider services)
    {
        // Validate required services
        _ = services.GetRequiredService<INodeContext>();
        _ = services.GetRequiredService<IGridContextAccessor>();
        _ = services.GetRequiredService<IOperationContextAccessor>();
        _ = services.GetRequiredService<IOperationContextFactory>();
        
        // Validate at least one transport binder
        var binders = services.GetServices<ITransportEnvelopeBinder>();
        if (!binders.Any())
            throw new InvalidOperationException("No transport binders registered");
    }
}
```

**Usage:**
```csharp
app.Services.ValidateHoneyDrunkServices(); // Throws if services missing
```

[↑ Back to top](#table-of-contents)

---

## Summary

| Category | Implementation | Purpose |
|----------|----------------|---------|
| **Context** | GridContext, NodeContext, OperationContext | Core context carriers |
| **Accessors** | GridContextAccessor, OperationContextAccessor | Ambient context access via `AsyncLocal<T>` |
| **Factories** | OperationContextFactory | Create operation contexts |
| **Mappers** | Http/Messaging/JobContextMapper | Extract context from transports |
| **Middleware** | GridContextMiddleware | HTTP request context using `GridHeaderNames` |
| **Lifecycle** | NodeLifecycleHost, NodeLifecycleManager | Startup/shutdown orchestration + health/readiness |
| **Diagnostics** | NoOpMetricsCollector, NodeContextReadinessContributor | Observability |
| **Validation** | ConfigurationValidator, ServiceProviderValidation | Fail-fast validation |

**Key Design Principles:**
- ✅ Thin wrappers over BCL (`AsyncLocal`, `IConfiguration`)
- ✅ Immutable core identity (`GridContext`) with controlled mutation for runtime state (`OperationContext`)
- ✅ Fail-fast validation at startup
- ✅ Zero-overhead no-op implementations for disabled features
- ✅ Integration with ASP.NET Core primitives (`IHostedService`, `IMiddleware`)
- ✅ Canonical header names via `GridHeaderNames`

**Lifecycle Stage Transitions:**
```
Initializing → Starting → Ready ⇄ Degraded
                             ↓
                          Stopping → Stopped
                             ↓
                          Failed
```

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

