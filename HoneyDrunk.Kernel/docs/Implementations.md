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

**v0.4.0 Two-Phase Initialization:**

```csharp
public sealed class GridContext : IGridContext, IDisposable
{
    // Immutable identity (set in constructor)
    public string NodeId { get; }
    public string StudioId { get; }
    public string Environment { get; }
    
    // Request-specific values (set via Initialize())
    public string CorrelationId { get; private set; } = "";
    public string? CausationId { get; private set; }
    public string? TenantId { get; private set; }
    public string? ProjectId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public CancellationToken Cancellation { get; private set; }
    public IReadOnlyDictionary<string, string> Baggage => _baggage;
    
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
    
    private readonly Dictionary<string, string> _baggage = new();
    
    // Phase 1: Constructor - identity values only
    public GridContext(string nodeId, string studioId, string environment)
    {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        StudioId = studioId ?? throw new ArgumentNullException(nameof(studioId));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    
    // Phase 2: Initialize - request-specific values
    public void Initialize(
        string correlationId,
        string? causationId = null,
        string? tenantId = null,
        string? projectId = null,
        IReadOnlyDictionary<string, string>? baggage = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (IsInitialized) throw new InvalidOperationException("Context already initialized.");
        
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        CausationId = causationId;
        TenantId = tenantId;
        ProjectId = projectId;
        Cancellation = cancellationToken;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        
        if (baggage != null)
        {
            foreach (var kvp in baggage)
                _baggage[kvp.Key] = kvp.Value;
        }
        
        IsInitialized = true;
    }
    
    // Mutates in-place, returns void (v0.4.0 change)
    public void AddBaggage(string key, string value)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();
        _baggage[key] = value;
    }
    
    public IGridContext CreateChildContext(string targetNodeId) { ... }
    
    public void Dispose() { IsDisposed = true; }
}
```

**Key Features (v0.4.0):**
- **Two-phase initialization** - Constructor sets identity, `Initialize()` sets request values
- **IsInitialized property** - Check whether context has been initialized
- **AddBaggage() is mutable** - Returns `void`, mutates in-place
- **BeginScope() removed** - Was a no-op, now gone
- **Disposal tracking** - Throws on access after disposal
- `CreateChildContext()` preserves correlation across Node boundaries
- CausationId tracks parent-child relationships
- Baggage flows automatically to children

**Usage:**
```csharp
// DI creates the context (Phase 1)
var context = new GridContext(
    nodeId: "api-gateway",
    studioId: "demo-studio",
    environment: "production");

// Middleware initializes it (Phase 2)
context.Initialize(
    correlationId: Ulid.NewUlid().ToString(),
    causationId: incomingCausationId,
    tenantId: tenantFromHeader);

// Now context is fully usable
var childContext = context.CreateChildContext("payment-service");
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

Accesses GridContext from the current DI scope via HttpContext.

**Location:** `HoneyDrunk.Kernel/Context/GridContextAccessor.cs`

**v0.4.0 Implementation (Breaking Change):**

```csharp
public sealed class GridContextAccessor : IGridContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public GridContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public IGridContext GridContext
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new InvalidOperationException("No active HTTP context.");
            
            return httpContext.RequestServices.GetRequiredService<IGridContext>();
        }
    }
}
```

**Key Changes in v0.4.0:**
- **No AsyncLocal** - Reads from `HttpContext.RequestServices` instead
- **Read-only property** - No setter; DI scope owns the context
- **Non-nullable** - Throws if accessed outside HTTP request scope
- **Requires IHttpContextAccessor** - Must be registered in DI

**How It Works:**
```csharp
// DI automatically creates GridContext for each request scope
// Accessor reads from the current request's service provider
var correlationId = _accessor.GridContext.CorrelationId;

// Throws if called outside HTTP request (e.g., background job)
// For background jobs, resolve IGridContext directly from job's scope
```

**Migration Note:** If you were setting `GridContext` on the accessor directly, this is no longer supported. The DI container now owns context lifecycle.

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

Context mappers extract and initialize GridContext from transport-specific envelopes. **v0.4.0:** All mappers are now **static classes** with initialization methods.

### HttpContextMapper.cs (Static)

Extracts headers from HTTP requests and initializes GridContext.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/HttpContextMapper.cs`

```csharp
public static class HttpContextMapper
{
    // Extract header values without modifying context
    public static (string CorrelationId, string? CausationId, string? TenantId, ...)
        ExtractFromHttpContext(HttpContext httpContext);
    
    // Initialize an existing GridContext from HTTP headers
    public static void InitializeFromHttpContext(
        IGridContext gridContext, 
        HttpContext httpContext);
}
```

**Usage:**
```csharp
// In middleware:
var gridContext = httpContext.RequestServices.GetRequiredService<IGridContext>();
HttpContextMapper.InitializeFromHttpContext(gridContext, httpContext);
```

**Headers Read:**
- `X-Correlation-Id` → `CorrelationId`
- `X-Causation-Id` → `CausationId`
- `X-Tenant-Id` → `TenantId`
- `X-Project-Id` → `ProjectId`
- `X-Baggage-*` → `Baggage`

### MessagingContextMapper.cs (Static)

Extracts context from message properties and initializes GridContext.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/MessagingContextMapper.cs`

```csharp
public static class MessagingContextMapper
{
    // Initialize GridContext from message properties
    public static void InitializeFromMessage(
        IGridContext gridContext,
        IReadOnlyDictionary<string, object?> messageProperties);
    
    // Extract correlation ID from message (returns null if not found)
    public static string? ExtractFromMessage(
        IReadOnlyDictionary<string, object?> messageProperties);
}
```

### JobContextMapper.cs (Static)

Initializes GridContext for background jobs.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/JobContextMapper.cs`

```csharp
public static class JobContextMapper
{
    // Initialize for a generic background job
    public static void InitializeForJob(
        IGridContext gridContext,
        string jobId,
        string? parentCorrelationId = null);
    
    // Initialize for a scheduled/recurring job
    public static void InitializeForScheduledJob(
        IGridContext gridContext,
        string scheduleId,
        string? scheduleName = null);
    
    // Initialize from job metadata dictionary
    public static void InitializeFromMetadata(
        IGridContext gridContext,
        IReadOnlyDictionary<string, object?> metadata);
}
```

**Design Note (v0.4.0):** Mappers are static because they don't hold state. The context instance is passed in and mutated directly via `Initialize()` method.

[↑ Back to top](#table-of-contents)

---

## Middleware

### GridContextMiddleware.cs

ASP.NET Core middleware that initializes GridContext for each HTTP request.

**Location:** `HoneyDrunk.Kernel/Context/Middleware/GridContextMiddleware.cs`

**v0.4.0 Behavior (Breaking Change):**

The middleware no longer creates a new GridContext. Instead, it **initializes the existing scoped context** from the DI container:

```csharp
public class GridContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GridContextMiddleware> _logger;
    
    public async Task InvokeAsync(
        HttpContext httpContext,
        IGridContext gridContext)  // Resolved from DI - already created, not initialized
    {
        // Initialize the context with request-specific values
        HttpContextMapper.InitializeFromHttpContext(gridContext, httpContext);
        
        // Now context is fully usable
        
        try
        {
            // Echo correlation to response
            httpContext.Response.OnStarting(() =>
            {
                httpContext.Response.Headers[GridHeaderNames.CorrelationId] = gridContext.CorrelationId;
                httpContext.Response.Headers[GridHeaderNames.NodeId] = gridContext.NodeId;
                return Task.CompletedTask;
            });
            
            await _next(httpContext);
        }
        finally
        {
            // Disposal handled by DI scope, not middleware
        }
    }
}
```

**Key Differences from v0.3.0:**
- Context is **resolved from DI**, not created in middleware
- Middleware calls `Initialize()` on the existing context
- No setter on `IGridContextAccessor` - context comes from DI scope
- `IDisposable` cleanup handled by DI container

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

