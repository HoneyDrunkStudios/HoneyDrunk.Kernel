# 🔧 Implementations - Kernel Runtime

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

This document covers the runtime implementations in `HoneyDrunk.Kernel` project. These are the concrete classes that bring the abstractions to life.

**Location:** `HoneyDrunk.Kernel/`

**Philosophy:** Implementations follow "thin wrapper" principles - delegate to BCL where possible, add Grid-specific semantics where necessary.

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
    studioId: "production",
    environment: "production"
);

// Create child for downstream call
var childContext = rootContext.CreateChildContext("payment-service");

Assert.Equal(rootContext.CorrelationId, childContext.CorrelationId);
Assert.Equal(rootContext.CorrelationId, childContext.CausationId); // Causality!
```

---

### NodeContext.cs

Static Node identity implementation carrying process-level metadata.

**Location:** `HoneyDrunk.Kernel/Context/NodeContext.cs`

```csharp
public sealed class NodeContext : INodeContext
{
    public NodeId NodeId { get; }
    public string Version { get; }
    public string StudioId { get; }
    public EnvironmentId Environment { get; }
    public NodeLifecycleStage LifecycleStage { get; private set; }
    public DateTimeOffset StartedAtUtc { get; }
    public string InstanceId { get; }
    public string HostName { get; }
    public IReadOnlyDictionary<string, string> Tags { get; }
    
    public void UpdateLifecycleStage(NodeLifecycleStage stage)
    {
        LifecycleStage = stage;
    }
}
```

**Key Features:**
- Singleton-scoped (one per process)
- Mutable `LifecycleStage` for startup/shutdown tracking
- Captures machine/process metadata (hostname, instance ID)
- Tags for observability (region, deployment slot, etc.)

**Lifecycle Stages:**
```csharp
NodeLifecycleStage.Initializing  // Just started
NodeLifecycleStage.Starting      // Running startup hooks
NodeLifecycleStage.Running       // Ready to serve traffic
NodeLifecycleStage.Stopping      // Running shutdown hooks
NodeLifecycleStage.Stopped       // Gracefully stopped
```

---

### OperationContext.cs

Per-operation tracking implementation with timing and outcome.

**Location:** `HoneyDrunk.Kernel/Context/OperationContext.cs`

```csharp
public sealed class OperationContext : IOperationContext
{
    private readonly IGridContext _gridContext;
    private readonly INodeContext _nodeContext;
    private readonly string _runId;
    private readonly DateTimeOffset _startedAtUtc;
    private readonly Dictionary<string, string> _tags;
    private bool _completed;
    
    public IGridContext Grid => _gridContext;
    public INodeContext Node => _nodeContext;
    public string RunId => _runId;
    public DateTimeOffset StartedAtUtc => _startedAtUtc;
    public IReadOnlyDictionary<string, string> Tags => _tags;
    
    public void Complete()
    {
        if (_completed) return;
        _completed = true;
        // Emit telemetry: success, duration, etc.
    }
    
    public void Fail(string reason, Exception? exception = null)
    {
        if (_completed) return;
        _completed = true;
        _tags["outcome"] = "failed";
        _tags["failureReason"] = reason;
        // Emit telemetry: failure, exception, etc.
    }
    
    public void AddTag(string key, string value)
    {
        _tags[key] = value;
    }
}
```

**Key Features:**
- Scoped lifetime (per-request, per-job, per-message)
- Links `IGridContext` + `INodeContext`
- Tracks timing (start → complete/fail)
- Tags for custom metadata
- Emits telemetry on completion

**Usage:**
```csharp
public class OrderService(IOperationContext operationContext)
{
    public async Task ProcessOrderAsync(Order order)
    {
        operationContext.AddTag("orderId", order.Id);
        operationContext.AddTag("customerId", order.CustomerId);
        
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

---

### OperationContextAccessor.cs

Async-local storage for operation context.

**Location:** `HoneyDrunk.Kernel/Context/OperationContextAccessor.cs`

```csharp
public sealed class OperationContextAccessor : IOperationContextAccessor
{
    private static readonly AsyncLocal<IOperationContext?> _current = new();
    
    public IOperationContext? OperationContext
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
```

Same pattern as `GridContextAccessor` but for `IOperationContext`.

---

### OperationContextFactory.cs

Factory for creating `IOperationContext` instances.

**Location:** `HoneyDrunk.Kernel/Context/OperationContextFactory.cs`

```csharp
public sealed class OperationContextFactory : IOperationContextFactory
{
    private readonly IGridContextAccessor _gridContextAccessor;
    private readonly INodeContext _nodeContext;
    
    public OperationContextFactory(
        IGridContextAccessor gridContextAccessor,
        INodeContext nodeContext)
    {
        _gridContextAccessor = gridContextAccessor;
        _nodeContext = nodeContext;
    }
    
    public IOperationContext Create()
    {
        var gridContext = _gridContextAccessor.GridContext 
            ?? throw new InvalidOperationException("GridContext not set");
        
        return new OperationContext(gridContext, _nodeContext);
    }
}
```

**Usage:**
```csharp
public class MessageConsumer(IOperationContextFactory factory)
{
    public async Task HandleAsync(Message message)
    {
        using var operation = factory.Create();
        
        operation.AddTag("messageId", message.Id);
        
        try
        {
            await ProcessMessageAsync(message);
            operation.Complete();
        }
        catch (Exception ex)
        {
            operation.Fail("Message processing failed", ex);
        }
    }
}
```

---

## Context Mappers

Context mappers extract GridContext from transport-specific envelopes. See [Transport.md](Transport.md) for detailed mapper documentation.

### HttpContextMapper.cs

Extracts GridContext from HTTP request headers.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/HttpContextMapper.cs`

**Headers Read:**
- `X-Correlation-ID` → `CorrelationId`
- `X-Causation-ID` → `CausationId`
- `X-Node-ID` → `NodeId`
- `X-Studio-ID` → `StudioId`
- `X-Environment` → `Environment`
- `X-Baggage-*` → `Baggage`

### MessagingContextMapper.cs

Extracts GridContext from message properties.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/MessagingContextMapper.cs`

### JobContextMapper.cs

Extracts GridContext from background job metadata.

**Location:** `HoneyDrunk.Kernel/Context/Mappers/JobContextMapper.cs`

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
        // Extract or create GridContext
        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Ulid.NewUlid().ToString();
        
        var gridContext = new GridContext(
            correlationId: correlationId,
            nodeId: nodeContext.NodeId.Value,
            studioId: nodeContext.StudioId,
            environment: nodeContext.Environment.Value,
            causationId: httpContext.Request.Headers["X-Causation-ID"].FirstOrDefault(),
            baggage: ExtractBaggage(httpContext.Request.Headers)
        );
        
        // Set ambient context
        contextAccessor.GridContext = gridContext;
        
        try
        {
            // Echo correlation to response
            httpContext.Response.Headers["X-Correlation-ID"] = gridContext.CorrelationId;
            httpContext.Response.Headers["X-Node-ID"] = gridContext.NodeId;
            
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
            if (key.StartsWith("X-Baggage-", StringComparison.OrdinalIgnoreCase))
            {
                var baggageKey = key.Substring("X-Baggage-".Length);
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

---

## Lifecycle

### NodeLifecycleManager.cs

Coordinates startup hooks, shutdown hooks, and lifecycle stage transitions.

**Location:** `HoneyDrunk.Kernel/Lifecycle/NodeLifecycleManager.cs`

```csharp
public sealed class NodeLifecycleManager : INodeLifecycle
{
    private readonly IEnumerable<IStartupHook> _startupHooks;
    private readonly IEnumerable<IShutdownHook> _shutdownHooks;
    private readonly INodeContext _nodeContext;
    private readonly ILogger<NodeLifecycleManager> _logger;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _nodeContext.UpdateLifecycleStage(NodeLifecycleStage.Starting);
        
        _logger.LogInformation("Starting Node {NodeId}", _nodeContext.NodeId);
        
        // Execute startup hooks in sequence
        foreach (var hook in _startupHooks.OrderBy(h => h.Order))
        {
            _logger.LogDebug("Executing startup hook {HookName}", hook.GetType().Name);
            await hook.ExecuteAsync(cancellationToken);
        }
        
        _nodeContext.UpdateLifecycleStage(NodeLifecycleStage.Running);
        _logger.LogInformation("Node {NodeId} running", _nodeContext.NodeId);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _nodeContext.UpdateLifecycleStage(NodeLifecycleStage.Stopping);
        
        _logger.LogInformation("Stopping Node {NodeId}", _nodeContext.NodeId);
        
        // Execute shutdown hooks in reverse order
        foreach (var hook in _shutdownHooks.OrderByDescending(h => h.Order))
        {
            _logger.LogDebug("Executing shutdown hook {HookName}", hook.GetType().Name);
            await hook.ExecuteAsync(cancellationToken);
        }
        
        _nodeContext.UpdateLifecycleStage(NodeLifecycleStage.Stopped);
        _logger.LogInformation("Node {NodeId} stopped", _nodeContext.NodeId);
    }
}
```

**Hook Execution Order:**
- **Startup:** Ordered ascending (lowest Order first)
- **Shutdown:** Ordered descending (highest Order first, LIFO)

---

### NodeLifecycleHost.cs

`IHostedService` implementation that integrates with ASP.NET Core hosting.

**Location:** `HoneyDrunk.Kernel/Hosting/NodeLifecycleHost.cs`

```csharp
public sealed class NodeLifecycleHost : IHostedService
{
    private readonly INodeLifecycle _lifecycle;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _lifecycle.StartAsync(cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _lifecycle.StopAsync(cancellationToken);
    }
}
```

Bridges `INodeLifecycle` to ASP.NET Core's `IHostedService`.

---

## Diagnostics

### NoOpMetricsCollector.cs

Zero-overhead metrics stub for when metrics are disabled.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NoOpMetricsCollector.cs`

```csharp
internal sealed class NoOpMetricsCollector : IMetricsCollector
{
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags) { }
    
    public void RecordValue(string name, double value, params KeyValuePair<string, object>[] tags) { }
    
    public void RecordDuration(string name, TimeSpan duration, params KeyValuePair<string, object>[] tags) { }
    
    public IDisposable MeasureDuration(string name, params KeyValuePair<string, object>[] tags)
    {
        return NullDisposable.Instance;
    }
    
    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
```

**Registered When:** Metrics are disabled in configuration.

---

### NodeLifecycleHealthContributor.cs

Health contributor based on Node lifecycle stage.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NodeLifecycleHealthContributor.cs`

```csharp
public sealed class NodeLifecycleHealthContributor : IHealthContributor
{
    private readonly INodeContext _nodeContext;
    
    public string Name => "lifecycle";
    
    public Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var status = _nodeContext.LifecycleStage switch
        {
            NodeLifecycleStage.Running => HealthStatus.Healthy,
            NodeLifecycleStage.Starting => HealthStatus.Degraded,
            NodeLifecycleStage.Stopping => HealthStatus.Unhealthy,
            _ => HealthStatus.Unhealthy
        };
        
        return Task.FromResult(status);
    }
}
```

**Health Mapping:**
- `Running` → `Healthy`
- `Starting` → `Degraded` (not yet ready)
- `Stopping` → `Unhealthy` (shutting down)
- Other → `Unhealthy`

---

### NodeContextReadinessContributor.cs

Readiness contributor ensuring NodeContext is initialized.

**Location:** `HoneyDrunk.Kernel/Diagnostics/NodeContextReadinessContributor.cs`

```csharp
public sealed class NodeContextReadinessContributor : IReadinessContributor
{
    private readonly INodeContext _nodeContext;
    
    public string Name => "node-context";
    
    public Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        var isReady = _nodeContext.LifecycleStage == NodeLifecycleStage.Running;
        return Task.FromResult(isReady);
    }
}
```

**Kubernetes Integration:**
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
readinessProbe:
  httpGet:
    path: /ready
    port: 8080
```

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
        
        if (string.IsNullOrWhiteSpace(options.NodeId?.Value))
            throw new InvalidOperationException("NodeId is required");
        
        if (string.IsNullOrWhiteSpace(options.StudioId))
            throw new InvalidOperationException("StudioId is required");
        
        if (options.EnvironmentId is null || string.IsNullOrWhiteSpace(options.EnvironmentId.Value))
            throw new InvalidOperationException("EnvironmentId is required");
        
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

---

## Dependency Injection

### HoneyDrunkCoreExtensions.cs

Core service registration extension methods.

**Location:** `HoneyDrunk.Kernel/DependencyInjection/HoneyDrunkCoreExtensions.cs`

Registers all Kernel services. See [Bootstrapping.md](Bootstrapping.md) for details.

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
        _ = services.GetRequiredService<INodeLifecycle>();
        
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

---

## Summary

| Category | Implementation | Purpose |
|----------|----------------|---------|
| **Context** | GridContext, NodeContext, OperationContext | Core context carriers |
| **Accessors** | GridContextAccessor, OperationContextAccessor | Ambient context access |
| **Factories** | OperationContextFactory | Create operation contexts |
| **Mappers** | Http/Messaging/JobContextMapper | Extract context from transports |
| **Middleware** | GridContextMiddleware | HTTP request context |
| **Lifecycle** | NodeLifecycleManager, NodeLifecycleHost | Startup/shutdown orchestration |
| **Diagnostics** | NoOpMetricsCollector, Health/Readiness Contributors | Observability |
| **Validation** | ConfigurationValidator, ServiceProviderValidation | Fail-fast validation |

**Key Design Principles:**
- ✅ Thin wrappers over BCL (`AsyncLocal`, `IConfiguration`)
- ✅ Immutable value types where possible
- ✅ Fail-fast validation at startup
- ✅ Zero-overhead no-op implementations for disabled features
- ✅ Integration with ASP.NET Core primitives

---

[← Back to File Guide](FILE_GUIDE.md)

