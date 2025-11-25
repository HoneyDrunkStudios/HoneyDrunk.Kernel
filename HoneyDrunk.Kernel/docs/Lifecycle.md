# 🔄 Lifecycle - Node Lifecycle Management

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [INodeLifecycle.cs](#inodelifecyclecs)
- [IStartupHook.cs](#istartuphookcs)
- [IShutdownHook.cs](#ishutdownhookcs)
- [IHealthContributor.cs](#ihealthcontributorcs)
- [IReadinessContributor.cs](#ireadinesscontributorcs)
- [NodeLifecycleStage.cs](#nodelifecyclestagecs)
- [Complete Startup Sequence Example](#complete-startup-sequence-example)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Lifecycle management provides coordinated Node startup, shutdown, health monitoring, and readiness checks. This enables graceful initialization, zero-downtime deployments, and controlled shutdown sequences.

**Location:** `HoneyDrunk.Kernel.Abstractions/Lifecycle/`

**Key Concepts:**
- **Lifecycle Hooks** - Extensible startup/shutdown behavior
- **Health Contributors** - Coordinated health monitoring
- **Readiness Contributors** - Traffic gating based on readiness state
- **Lifecycle Stages** - Standardized Node state machine

[↑ Back to top](#table-of-contents)

---

## INodeLifecycle.cs

### What it is
Core interface for Node startup and shutdown coordination.

### Real-world analogy
Like an application server's lifecycle - initialize on startup, gracefully drain connections on shutdown.

### Methods

```csharp
public interface INodeLifecycle
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

### Usage Example

```csharp
public class CacheWarmupLifecycle(ICacheService cache) : INodeLifecycle
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Warm up cache before accepting traffic
        await cache.PreloadFrequentDataAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Flush pending cache writes
        await cache.FlushAsync(cancellationToken);
    }
}
```

### When to use
- Database connection initialization
- Cache preloading
- Service registration/deregistration
-[← Background service coordination
- Resource cleanup on shutdown

[↑ Back to top](#table-of-contents)

---

## IStartupHook.cs

### What it is
Extensible hook that executes during Node startup with priority ordering.

### Real-world analogy
Like systemd service dependencies - some services must start before others.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Priority` | int | 0 | Execution order (lower runs first) |

### Methods

```csharp
Task ExecuteAsync(CancellationToken cancellationToken);
```

### Usage Example

```csharp
public class DatabaseMigrationHook(IDbMigrator migrator) : IStartupHook
{
    // Run early (before other services)
    public int Priority => -100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await migrator.MigrateToLatestAsync(cancellationToken);
    }
}

public class CacheWarmupHook(ICacheService cache) : IStartupHook
{
    // Run later (after database is ready)
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await cache.WarmupAsync(cancellationToken);
    }
}
```

### Priority Guidelines

| Range | Purpose | Examples |
|-------|---------|----------|
| **< -100** | Critical infrastructure | Configuration validation, secrets loading |
| **-100 to 0** | Core services | Database migrations, service discovery registration |
| **0 to 100** | Application services | Cache warmup, data seeding |
| **> 100** | Optional initialization | Analytics setup, non-critical integrations |

### When to use
- Database migrations
- Configuration validation
- Data seeding
- Service discovery registration
- Connection warmup

[↑ Back to top](#table-of-contents)

---

## IShutdownHook.cs

### What it is
Extensible hook that executes during Node shutdown with priority ordering.

### Real-world analogy
Like cleanup handlers - flush buffers, close connections, save state.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Priority` | int | 0 | Execution order (lower runs first during shutdown) |

### Methods

```csharp
Task ExecuteAsync(CancellationToken cancellationToken);
```

### Usage Example

```csharp
public class MessageQueueDrainHook(IMessageQueue queue) : IShutdownHook
{
    // Drain queue early (before other services stop)
    public int Priority => -100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await queue.DrainAsync(cancellationToken);
    }
}

public class ServiceDiscoveryDeregisterHook(IServiceDiscovery discovery) : IShutdownHook
{
    // Deregister late (after all work is drained)
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await discovery.DeregisterAsync(cancellationToken);
    }
}
```

### Priority Guidelines

| Range | Purpose | Examples |
|-------|---------|----------|
| **< -100** | Stop accepting work | Deregister from load balancer |
| **-100 to 0** | Drain in-flight work | Flush message queues, complete requests |
| **0 to 100** | Release resources | Close database connections, stop[← Background jobs |
| **> 100** | Final cleanup | Delete temp files, send shutdown notifications |

### When to use
- Connection pool draining
- Message queue flushing
- Service discovery deregistration
- Graceful request completion
- Resource cleanup

[↑ Back to top](#table-of-contents)

---

## IHealthContributor.cs

### What it is
Contributes to overall Node health assessment with coordination and aggregation.

### Real-world analogy
Like health checks in Kubernetes - multiple probes determine overall health.

### Difference from IHealthCheck

| Feature | IHealthCheck | IHealthContributor |
|---------|--------------|-------------------|
| **Scope** | Standalone | Coordinated by Node |
| **Aggregation** | Manual | Automatic |
| **Criticality** | Not specified | `IsCritical` flag |
| **Ordering** | Not specified | `Priority` field |

### Properties

```csharp
public interface IHealthContributor
{
    string Name { get; }
    int Priority { get; }
    bool IsCritical { get; }
    Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken ct);
}
```

### Usage Example

```csharp
public class DatabaseHealthContributor(IDbConnection db) : IHealthContributor
{
    public string Name => "Database";
    public int Priority => 0;
    public bool IsCritical => true; // Node is unhealthy if database is down

    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await db.ExecuteScalarAsync<int>("SELECT 1", cancellationToken);
            return (HealthStatus.Healthy, null);
        }
        catch (TimeoutException)
        {
            return (HealthStatus.Degraded, "Database responding slowly");
        }
        catch (Exception ex)
        {
            return (HealthStatus.Unhealthy, $"Database unavailable: {ex.Message}");
        }
    }
}

public class CacheHealthContributor(ICache cache) : IHealthContributor
{
    public string Name => "Cache";
    public int Priority => 10;
    public bool IsCritical => false; // Node can function without cache

    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.PingAsync(cancellationToken);
            return (HealthStatus.Healthy, null);
        }
        catch
        {
            return (HealthStatus.Degraded, "Cache unavailable, using fallback");
        }
    }
}
```

### Health Aggregation Rules

1. **Critical contributors** with `Unhealthy` status ? Node is `Unhealthy`
2. **Any contributor** with `Degraded` status ? Node is `Degraded` (if not unhealthy)
3. **All contributors** `Healthy` ? Node is `Healthy`

### When to use
- Database connectivity monitoring
- External API health checks
- Cache availability checks
- Message queue connectivity
- File system access validation

[↑ Back to top](#table-of-contents)

---

## IReadinessContributor.cs

### What it is
Determines if a Node is ready to accept traffic or work.

### Real-world analogy
Like Kubernetes readiness probes - traffic only routes to ready pods.

### Health vs Readiness

| Aspect | Health | Readiness |
|--------|--------|-----------|
| **Question** | Is the Node functioning? | Is the Node ready to handle requests? |
| **Example Failure** | Database unreachable | Cache still warming up |
| **Action on Failure** | Restart Node | Wait, don't send traffic |
| **Kubernetes** | Liveness probe | Readiness probe |

### Properties

```csharp
public interface IReadinessContributor
{
    string Name { get; }
    int Priority { get; }
    bool IsRequired { get; }
    Task<(bool isReady, string? reason)> CheckReadinessAsync(CancellationToken ct);
}
```

### Usage Example

```csharp
public class DatabaseReadinessContributor(IDbConnection db) : IReadinessContributor
{
    public string Name => "Database";
    public int Priority => 0;
    public bool IsRequired => true; // Must be ready for Node to be ready

    public async Task<(bool isReady, string? reason)> CheckReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.ExecuteScalarAsync<int>("SELECT 1", cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Database not ready: {ex.Message}");
        }
    }
}

public class CacheReadinessContributor(ICacheService cache) : IReadinessContributor
{
    public string Name => "Cache";
    public int Priority => 10;
    public bool IsRequired => false; // Optional - Node can start without cache

    public async Task<(bool isReady, string? reason)> CheckReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        if (!cache.IsWarmedUp)
        {
            return (false, "Cache warmup in progress");
        }
        
        return (true, null);
    }
}
```

### Readiness Aggregation Rules

1. **Any required contributor** not ready ? Node is not ready
2. **All required contributors** ready ? Node is ready (optional contributors don't block)

### When to use
- Database connection establishment
- Cache warmup completion
- Configuration loading verification
- Service dependency readiness
- Initial data loading

[↑ Back to top](#table-of-contents)

---

## NodeLifecycleStage.cs

### What it is
Enum representing the current lifecycle stage of a Node.

### 🔄 Lifecycle State Machine

```
Initializing ? Starting ? Running ? Degraded
                             ?
                          Stopping ? Stopped
                             ?
                          Failed
```

### Values

| Stage | Description | Accepts Work? |
|-------|-------------|---------------|
| `Initializing` | Node is initializing, not yet ready | ? No |
| `Starting` | Services are starting up | ? No |
| `Running` | Fully operational | ? Yes |
| `Degraded` | Operational but with issues | ?? Limited |
| `Stopping` | Gracefully shutting down, draining work | ? No (draining existing) |
| `Stopped` | Shut down, no longer accepting work | ? No |
| `Failed` | Fatal error, non-operational | ? No |

### Usage Example

```csharp
public class NodeMonitor(INodeContext nodeContext, IMetricsCollector metrics)
{
    public void ReportStage()
    {
        metrics.RecordGauge("node.lifecycle_stage",
            (int)nodeContext.LifecycleStage,
            new KeyValuePair<string, object?>("stage", nodeContext.LifecycleStage.ToString()));
    }

    public bool CanAcceptWork()
    {
        return nodeContext.LifecycleStage == NodeLifecycleStage.Running;
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Complete Startup Sequence Example

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Register lifecycle components
        builder.Services.AddSingleton<IStartupHook, DatabaseMigrationHook>();
        builder.Services.AddSingleton<IStartupHook, CacheWarmupHook>();
        builder.Services.AddSingleton<IShutdownHook, MessageQueueDrainHook>();
        builder.Services.AddSingleton<IShutdownHook, ServiceDiscoveryDeregisterHook>();
        
        // Register health/readiness
        builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, CacheHealthContributor>();
        builder.Services.AddSingleton<IReadinessContributor, DatabaseReadinessContributor>();
        builder.Services.AddSingleton<IReadinessContributor, CacheReadinessContributor>();
        
        var app = builder.Build();
        
        // Startup sequence (handled by NodeLifecycleHost):
        // 1. NodeLifecycleStage = Initializing
        // 2. Execute IStartupHook instances by priority
        // 3. Check IReadinessContributor instances
        // 4. NodeLifecycleStage = Running
        
        app.MapGet("/health", async (IHealthContributor[] contributors) =>
        {
            var results = await Task.WhenAll(
                contributors.Select(c => c.CheckHealthAsync()));
            
            var worstStatus = results
                .Select(r => r.status)
                .Max();
            
            return Results.Json(new
            {
                status = worstStatus.ToString(),
                checks = results.Select((r, i) => new
                {
                    name = contributors[i].Name,
                    status = r.status.ToString(),
                    message = r.message
                })
            });
        });
        
        app.MapGet("/ready", async (IReadinessContributor[] contributors) =>
        {
            var results = await Task.WhenAll(
                contributors.Select(c => c.CheckReadinessAsync()));
            
            var allRequiredReady = contributors
                .Zip(results, (c, r) => (contributor: c, result: r))
                .Where(x => x.contributor.IsRequired)
                .All(x => x.result.isReady);
            
            return allRequiredReady 
                ? Results.Ok() 
                : Results.ServiceUnavailable();
        });
        
        await app.RunAsync();
        
        // Shutdown sequence (handled by NodeLifecycleHost):
        // 1. NodeLifecycleStage = Stopping
        // 2. Stop accepting new requests
        // 3. Execute IShutdownHook instances by priority
        // 4. NodeLifecycleStage = Stopped
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

```csharp
[Fact]
public async Task StartupHook_ExecutesInPriorityOrder()
{
    // Arrange
    var executionOrder = new List<string>();
    var hook1 = new TestStartupHook("Hook1", -100, () => executionOrder.Add("Hook1"));
    var hook2 = new TestStartupHook("Hook2", 0, () => executionOrder.Add("Hook2"));
    var hook3 = new TestStartupHook("Hook3", 100, () => executionOrder.Add("Hook3"));
    
    var hooks = new[] { hook3, hook1, hook2 }; // Registered out of order
    
    // Act
    foreach (var hook in hooks.OrderBy(h => h.Priority))
    {
        await hook.ExecuteAsync(CancellationToken.None);
    }
    
    // Assert
    Assert.Equal(new[] { "Hook1", "Hook2", "Hook3" }, executionOrder);
}

[Fact]
public async Task HealthContributor_CriticalUnhealthy_FailsNode()
{
    // Arrange
    var criticalContributor = new TestHealthContributor("Critical", isCritical: true)
    {
        Status = HealthStatus.Unhealthy
    };
    var nonCriticalContributor = new TestHealthContributor("NonCritical", isCritical: false)
    {
        Status = HealthStatus.Healthy
    };
    
    // Act
    var (criticalStatus, _) = await criticalContributor.CheckHealthAsync();
    var (nonCriticalStatus, _) = await nonCriticalContributor.CheckHealthAsync();
    
    // Assert - Aggregate would be Unhealthy due to critical contributor
    Assert.Equal(HealthStatus.Unhealthy, criticalStatus);
}

[Fact]
public async Task ReadinessContributor_RequiredNotReady_BlocksReadiness()
{
    // Arrange
    var requiredContributor = new TestReadinessContributor("Required", isRequired: true)
    {
        IsReady = false
    };
    var optionalContributor = new TestReadinessContributor("Optional", isRequired: false)
    {
        IsReady = true
    };
    
    // Act
    var (requiredReady, _) = await requiredContributor.CheckReadinessAsync();
    
    // Assert - Node not ready because required contributor not ready
    Assert.False(requiredReady);
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

| Component | Purpose | Priority | Critical/Required |
|-----------|---------|----------|-------------------|
| **INodeLifecycle** | Core startup/shutdown | N/A | N/A |
| **IStartupHook** | Extensible startup | Yes (lower first) | N/A |
| **IShutdownHook** | Extensible shutdown | Yes (lower first) | N/A |
| **IHealthContributor** | Health monitoring | Yes | Yes (`IsCritical`) |
| **IReadinessContributor** | Traffic gating | Yes | Yes (`IsRequired`) |

**Key Patterns:**
- Startup hooks execute in priority order (lower first)
- Shutdown hooks execute in priority order (lower first)
- Critical health contributors fail Node if unhealthy
- Required readiness contributors block traffic if not ready
- NodeLifecycleStage tracks current state

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

