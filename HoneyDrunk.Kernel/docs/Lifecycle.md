# 🔄 Lifecycle - Node Lifecycle Management

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [NodeLifecycleHost.cs](#nodelifecyclehostcs)
- [NodeLifecycleManager.cs](#nodelifecyclemanagercs)
- [INodeLifecycle.cs](#inodelifecyclecs)
- [IStartupHook.cs](#istartuphookcs)
- [IShutdownHook.cs](#ishutdownhookcs)
- [IHealthContributor.cs](#ihealthcontributorcs)
- [IReadinessContributor.cs](#ireadinesscontributorcs)
- [NodeLifecycleStage.cs](#nodelifecyclestagecs)
- [Health and Readiness Impact on Lifecycle](#health-and-readiness-impact-on-lifecycle)
- [Relationship to IHostedService](#relationship-to-ihostedservice)
- [Complete Startup Sequence Example](#complete-startup-sequence-example)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Lifecycle management provides coordinated Node startup, shutdown, health monitoring, and readiness checks. This enables graceful initialization, zero-downtime deployments, and controlled shutdown sequences.

**Location:** `HoneyDrunk.Kernel.Abstractions/Lifecycle/` (abstractions), `HoneyDrunk.Kernel/Lifecycle/` (implementations)

**Key Concepts:**
- **Lifecycle Host** - IHostedService that orchestrates the entire lifecycle
- **Lifecycle Manager** - Central orchestrator for health, readiness, and state transitions
- **Lifecycle Hooks** - Extensible startup/shutdown behavior
- **Health Contributors** - Coordinated health monitoring
- **Readiness Contributors** - Traffic gating based on readiness state
- **Lifecycle Stages** - Standardized Node state machine

[↑ Back to top](#table-of-contents)

---

## NodeLifecycleHost.cs

### What it is
An `IHostedService` implementation that orchestrates the entire Node lifecycle, executing startup hooks, invoking `INodeLifecycle` implementations, and managing graceful shutdown.

### Real-world analogy
Like systemd or Docker's entrypoint - coordinates initialization, runs the main process, and handles shutdown signals.

### Key Responsibilities

1. **Startup Orchestration** - Transitions from Initializing (set at NodeContext construction) through Starting → Ready
2. **Hook Execution** - Runs `IStartupHook` and `IShutdownHook` in priority order
3. **Lifecycle Coordination** - Invokes all registered `INodeLifecycle` implementations
4. **Error Handling** - Transitions to Failed on startup/shutdown exceptions

### Startup Sequence

```csharp
public async Task StartAsync(CancellationToken cancellationToken)
{
    // 1. Initial state is Initializing (set by NodeContext constructor)
    // _nodeContext.LifecycleStage == NodeLifecycleStage.Initializing
    
    // 2. Transition to Starting
    _nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);
    
    try
    {
        // 3. Execute startup hooks by priority (lower first)
        var orderedHooks = _startupHooks.OrderBy(h => h.Priority).ToList();
        foreach (var hook in orderedHooks)
        {
            await hook.ExecuteAsync(cancellationToken);
        }
        
        // 4. Invoke INodeLifecycle.StartAsync for all registered lifecycles
        foreach (var lifecycle in _lifecycles)
        {
            await lifecycle.StartAsync(cancellationToken);
        }
        
        // 5. Transition to Ready
        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Ready);
    }
    catch (Exception ex)
    {
        // 6. Transition to Failed on any exception
        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
        throw; // Propagate to host
    }
}
```

**Note:** The actual implementation directly calls `_nodeContext.SetLifecycleStage()`. `NodeLifecycleManager` is used for health/readiness aggregation and can also be used to perform transitions with telemetry, but `NodeLifecycleHost` uses the simpler direct approach for its well-defined startup/shutdown flow.

### Shutdown Sequence

```csharp
public async Task StopAsync(CancellationToken cancellationToken)
{
    // 1. Transition to Stopping
    _nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);
    
    try
    {
        // 2. Invoke INodeLifecycle.StopAsync for all registered lifecycles
        foreach (var lifecycle in _lifecycles)
        {
            await lifecycle.StopAsync(cancellationToken);
        }
        
        // 3. Execute shutdown hooks by priority (lower first)
        var orderedHooks = _shutdownHooks.OrderBy(h => h.Priority).ToList();
        foreach (var hook in orderedHooks)
        {
            await hook.ExecuteAsync(cancellationToken);
        }
        
        // 4. Transition to Stopped
        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopped);
    }
    catch (Exception ex)
    {
        // 5. Transition to Failed on any exception
        _nodeContext.SetLifecycleStage(NodeLifecycleStage.Failed);
        throw; // Propagate to host
    }
}
```

### Registration

The `NodeLifecycleHost` is automatically registered when you call `AddHoneyDrunkNode`:

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = "payment-node";
    options.StudioId = "demo-studio";
    options.Version = "1.0.0";
    options.Environment = "production";
});
// NodeLifecycleHost is registered as IHostedService internally
```

### Error Handling

| Scenario | Behavior | Stage Transition |
|----------|----------|------------------|
| **Startup hook throws** | Abort startup, transition to Failed, propagate exception to host | Starting → Failed |
| **INodeLifecycle.StartAsync throws** | Abort startup, transition to Failed, propagate exception to host | Starting → Failed |
| **Shutdown hook throws** | Log error, transition to Failed, propagate exception to host | Stopping → Failed |
| **INodeLifecycle.StopAsync throws** | Log error, transition to Failed, propagate exception to host | Stopping → Failed |

**Policy:** Both startup and shutdown failures transition to Failed and propagate to the host. The host logs exceptions and attempts graceful cleanup where possible.

### Who Can Call SetLifecycleStage

**Kernel Internals:**
- `NodeLifecycleHost` (and maybe a couple of other core services) can use `INodeContext.SetLifecycleStage()` directly for well-defined lifecycle transitions.

**Application Code:**
- Should prefer `NodeLifecycleManager.TransitionToStage()` to get telemetry tags, structured logging, and a single mutation path.

### When to use
- Automatically used when `AddHoneyDrunkNode` is called
- No manual interaction required - host orchestrates everything
- For custom orchestration, you can implement your own `IHostedService` and use `NodeLifecycleManager.TransitionToStage()` (or, in advanced cases, manipulate `INodeContext.SetLifecycleStage()` directly)

[↑ Back to top](#table-of-contents)

---

## NodeLifecycleManager.cs

### What it is
Central orchestrator that coordinates health checks, readiness checks, and lifecycle stage transitions. This is the runtime implementation that aggregates all contributors and manages Node state.

### Real-world analogy
Like a Kubernetes controller - it polls health probes, aggregates readiness checks, and can manage pod lifecycle transitions.

### Key Responsibilities

1. **Health Aggregation** - Coordinates all `IHealthContributor` instances
2. **Readiness Aggregation** - Coordinates all `IReadinessContributor` instances
3. **Lifecycle Transitions** - Provides `TransitionToStage()` method with telemetry
4. **Telemetry Integration** - Emits OpenTelemetry activities for monitoring

**Note:** `NodeLifecycleManager` is **not** the primary lifecycle orchestrator. `NodeLifecycleHost` owns the startup/shutdown flow and transitions stages directly via `INodeContext.SetLifecycleStage()`. `NodeLifecycleManager` is used for health/readiness aggregation and can optionally be used by orchestration components (like background health monitors) to perform transitions with telemetry enrichment.

### Constructor

```csharp
public NodeLifecycleManager(
    INodeContext nodeContext,
    IEnumerable<IHealthContributor> healthContributors,
    IEnumerable<IReadinessContributor> readinessContributors,
    ILogger<NodeLifecycleManager> logger)
```

**Dependency Injection:** This is resolved by DI and you usually never `new` up this type yourself. It's automatically registered when you call `AddHoneyDrunkNode()`.

### Methods

#### CheckHealthAsync

Performs comprehensive health check across all registered contributors.

```csharp
public async Task<(HealthStatus status, IReadOnlyDictionary<string, (HealthStatus status, string? message)> details)}
    CheckHealthAsync(CancellationToken cancellationToken = default)
```

**Behavior:**
- Executes contributors in **priority order** (lowest first)
- Tracks worst status across all contributors
- **Fails fast** if critical contributor is unhealthy
- Returns aggregated status + per-contributor details
- Emits OpenTelemetry activity with tags

**Usage Example:**

```csharp
public class HealthEndpoint(NodeLifecycleManager lifecycleManager)
{
    [HttpGet("/health")]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var (status, details) = await lifecycleManager.CheckHealthAsync(ct);
        
        var response = new
        {
            status = status.ToString(),
            checks = details.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.status.ToString(),
                message = kvp.Value.message
            })
        };
        
        return status switch
        {
            HealthStatus.Healthy => Ok(response),
            HealthStatus.Degraded => StatusCode(200, response), // Still accepting traffic
            HealthStatus.Unhealthy => StatusCode(503, response),
            _ => StatusCode(500, response)
        };
    }
}
```

**Aggregation Rules:**
| Scenario | Result |
|----------|--------|
| No contributors | `Healthy` |
| All contributors `Healthy` | `Healthy` |
| Any contributor `Degraded` | `Degraded` |
| Any contributor `Unhealthy` | `Unhealthy` |
| **Critical** contributor `Unhealthy` | `Unhealthy` (stops checking remaining) |

#### CheckReadinessAsync

Performs comprehensive readiness check across all registered contributors.

```csharp
public async Task<(bool isReady, IReadOnlyDictionary<string, (bool isReady, string? reason)> details)}
    CheckReadinessAsync(CancellationToken cancellationToken = default)
```

**Behavior:**
- Executes contributors in **priority order** (lowest first)
- Checks all contributors (does not stop early)
- Returns `false` if **any required** contributor is not ready
- Optional contributors do not block readiness
- Emits OpenTelemetry activity with tags

**Usage Example:**

```csharp
public class ReadinessEndpoint(NodeLifecycleManager lifecycleManager)
{
    [HttpGet("/ready")]
    public async Task<IActionResult> GetReadiness(CancellationToken ct)
    {
        var (isReady, details) = await lifecycleManager.CheckReadinessAsync(ct);
        
        if (!isReady)
        {
            var notReadyChecks = details
                .Where(kvp => !kvp.Value.isReady)
                .Select(kvp => new { name = kvp.Key, reason = kvp.Value.reason });
            
            return StatusCode(503, new
            {
                ready = false,
                blockers = notReadyChecks
            });
        }
        
        return Ok(new { ready = true });
    }
}
```

**Aggregation Rules:**
| Scenario | Result |
|----------|--------|
| No contributors | `Ready` |
| All required contributors ready | `Ready` |
| Any **required** contributor not ready | `Not Ready` |
| Only optional contributors not ready | `Ready` |

#### TransitionToStage

Updates the Node lifecycle stage and logs the transition with telemetry.

```csharp
public void TransitionToStage(NodeLifecycleStage newStage)
```

**Behavior:**
- Updates `INodeContext.LifecycleStage`
- Logs transition with structured logging
- Emits OpenTelemetry activity
- No-op if stage is unchanged

**Usage Example:**

```csharp
public class HealthMonitorService(
    NodeLifecycleManager lifecycleManager,
    INodeContext nodeContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (status, _) = await lifecycleManager.CheckHealthAsync(stoppingToken);
            
            if (nodeContext.LifecycleStage == NodeLifecycleStage.Ready && status == HealthStatus.Unhealthy)
            {
                lifecycleManager.TransitionToStage(NodeLifecycleStage.Degraded);
            }
            else if (nodeContext.LifecycleStage == NodeLifecycleStage.Degraded && status == HealthStatus.Healthy)
            {
                lifecycleManager.TransitionToStage(NodeLifecycleStage.Ready);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

### Error Handling

The manager handles contributor exceptions gracefully:

| Scenario | Behavior |
|----------|----------|
| Health contributor throws | Returns `Unhealthy` for that check |
| Readiness contributor throws | Returns `Not Ready` for that check |
| Critical health contributor throws | Node marked `Unhealthy`, stops checking |
| Required readiness contributor throws | Node marked `Not Ready`, continues checking |
| `OperationCanceledException` with cancellation | Propagates cancellation |

### Telemetry Tags

**Health Check Activity:**
- `node.id` - Node identifier
- `node.lifecycle_stage` - Current lifecycle stage
- `health.contributors.count` - Number of contributors
- `health.status` - Aggregated result
- `health.failed_contributor` - Name of critical failure (if applicable)

**Readiness Check Activity:**
- `node.id` - Node identifier
- `node.lifecycle_stage` - Current lifecycle stage
- `readiness.contributors.count` - Number of contributors
- `readiness.status` - "Ready" or "NotReady"

**Lifecycle Transition Activity:**
- `node.id` - Node identifier
- `lifecycle.from` - Previous stage
- `lifecycle.to` - New stage

### When to use
- Implementing `/health` and `/ready` endpoints
- Used by orchestration components (such as `NodeLifecycleHost` or background health monitors) to coordinate state
- Monitoring Node operational state
- Coordinating multiple health/readiness checks
- Integrating with Kubernetes liveness/readiness probes

[↑ Back to top](#table-of-contents)

---

## INodeLifecycle.cs

### What it is
Core interface for Node-level startup and shutdown logic that requires coordination with other services.

### Real-world analogy
Like an application server's lifecycle - initialize on startup, gracefully drain connections on shutdown.

### How it's invoked
`INodeLifecycle` implementations are discovered and invoked by `NodeLifecycleHost`:

1. **During Startup:** After all `IStartupHook` instances complete, `NodeLifecycleHost` calls `StartAsync` on each registered `INodeLifecycle` implementation
2. **During Shutdown:** Before `IShutdownHook` instances run, `NodeLifecycleHost` calls `StopAsync` on each registered `INodeLifecycle` implementation

**Execution Order:**
```
Startup:  IStartupHook (priority order) → INodeLifecycle.StartAsync → Ready
Shutdown: Stopping → INodeLifecycle.StopAsync → IShutdownHook (priority order) → Stopped
```

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

// Register with DI
builder.Services.AddNodeLifecycle<CacheWarmupLifecycle>();
```

### When to use
- Database connection initialization
- Cache preloading
- Service registration/deregistration
- Background service coordination (e.g., starting message consumers)
- Resource cleanup on shutdown

### When NOT to use
- For **priority-ordered initialization** → use `IStartupHook` instead
- For **long-running background workers** → use `IHostedService` instead
- For **fire-and-forget cleanup tasks** → use `IShutdownHook` instead

**Rule of Thumb:** Use `INodeLifecycle` for Node-level initialization that requires access to DI services and doesn't need strict ordering beyond "after hooks, before Ready".

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
| **0 to 100** | Release resources | Close database connections, stop background workers |
| **> 100** | Final cleanup | Delete temp files, send shutdown notifications |

### When to use
- Connection pool draining
- Message queue flushing
- Service discovery deregistration
- Graceful request completion
- Resource cleanup

**Priority Semantics:** If multiple hooks share the same priority, execution order follows DI registration order.

[↑ back to table-of-contents](#table-of-contents)

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

[↑ back to table-of-contents](#table-of-contents)

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

[↑ back to table-of-contents](#table-of-contents)

---

## NodeLifecycleStage.cs

### What it is
Enum representing the current lifecycle stage of a Node.

### 🔄 Lifecycle State Machine

```
Initializing → Starting → Ready ⇄ Degraded
                             ↓
                          Stopping → Stopped
                             ↓
                          Failed
```

### Values

| Stage | Description | Accepts Work? |
|-------|-------------|---------------|
| `Initializing` | Node is initializing, not yet ready | ❌ No |
| `Starting` | Services are starting up | ❌ No |
| `Ready` | Fully operational | ✅ Yes |
| `Degraded` | Operational but with issues | ⚠️ Limited |
| `Stopping` | Gracefully shutting down, draining work | ❌ No (draining existing) |
| `Stopped` | Shut down, no longer accepting work | ❌ No |
| `Failed` | Fatal error, non-operational | ❌ No |

### Degraded State Policy

The `Degraded` state indicates operational but impaired capacity. **Policy is Node-specific:**

- Some Nodes may **continue accepting new work** in Degraded (e.g., cache unavailable, use fallback)
- Other Nodes may **reject new work** in Degraded (e.g., database degraded, read-only mode)

**Recommendation:** Applications should define explicit `CanAcceptWork()` logic:

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
        // Option 1: Strict - only Ready accepts work
        return nodeContext.LifecycleStage == NodeLifecycleStage.Ready;
        
        // Option 2: Lenient - Ready or Degraded both accept work
        // return nodeContext.LifecycleStage is NodeLifecycleStage.Ready
        //     or NodeLifecycleStage.Degraded;
    }
}
```

[↑ back to table-of-contents](#table-of-contents)

---

## Health and Readiness Impact on Lifecycle

### Overview

Health and readiness checks influence Node operational state. Here's how `NodeLifecycleManager` results **should** map to lifecycle stage transitions.

**⚠️ Important:** As of v0.3.0, these policies describe the **intended behavior**. Startup and steady-state transitions are currently **manual**, driven by application code using `NodeLifecycleManager` or direct calls to `INodeContext.SetLifecycleStage()`.

### Intended Policy (Future)

| From Stage | Trigger | To Stage | Notes |
|-----------|---------|----------|-------|
| **Starting** | All required readiness checks pass | **Ready** | Normal startup path (planned, not yet automated) |
| **Starting** | Required readiness times out (>30s) | **Degraded** | Node starts but flags degradation (planned) |
| **Ready** | Critical health contributor `Unhealthy` | **Failed** | Immediate failure for critical issues (planned) |
| **Ready** | Non-critical health contributor `Unhealthy` | **Degraded** | Node continues with reduced capacity (planned) |
| **Degraded** | All health checks return `Healthy` | **Ready** | Automatic recovery (planned) |
| **Degraded** | Critical health contributor `Unhealthy` | **Failed** | Degraded → Failed escalation (planned) |
| **Any** | Host shutdown requested | **Stopping** | External signal triggers shutdown (✅ implemented) |

### Current Implementation Status

**⚠️ Note:** As of v0.3.0, health/readiness→lifecycle transitions are **not fully automated**. Current behavior:

- `NodeLifecycleHost` transitions to `Ready` immediately after startup hooks complete
- `NodeLifecycleManager.CheckHealthAsync()` and `CheckReadinessAsync()` return status but **do not** automatically update `NodeLifecycleStage`
- Periodic health checks must be implemented by the application (e.g., via background service)

**Planned Enhancement:** Future versions will include optional background monitoring that automatically transitions between `Ready` / `Degraded` / `Failed` based on health/readiness results.

### Manual Transition Example

```csharp
// Current pattern: manual periodic checks
public class HealthMonitorService(
    NodeLifecycleManager lifecycleManager,
    INodeContext nodeContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (status, _) = await lifecycleManager.CheckHealthAsync(stoppingToken);
            
            if (nodeContext.LifecycleStage == NodeLifecycleStage.Ready && status == HealthStatus.Unhealthy)
            {
                // Use TransitionToStage for telemetry and structured logging
                lifecycleManager.TransitionToStage(NodeLifecycleStage.Degraded);
            }
            else if (nodeContext.LifecycleStage == NodeLifecycleStage.Degraded && status == HealthStatus.Healthy)
            {
                lifecycleManager.TransitionToStage(NodeLifecycleStage.Ready);
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

[↑ back to table-of-contents](#table-of-contents)

---

## Relationship to IHostedService

### Overview

.NET hosts (ASP.NET Core, Generic Host) provide `IHostedService` for long-running background operations. Kernel lifecycle primitives complement this, not replace it.

### When to Use Each

| Use Case | Recommended Abstraction | Reason |
|----------|-------------------------|--------|
| **Long-running background worker** | `IHostedService` / `BackgroundService` | Host lifecycle management, CancellationToken support |
| **Message queue consumer** | `IHostedService` | Continuous processing, auto-restart on failure |
| **Periodic scheduled jobs** | `IHostedService` | Timer-based execution, host-managed |
| **Node-level startup logic** | `IStartupHook` | Priority ordering, runs before Ready state |
| **Node-level initialization** | `INodeLifecycle` | Coordinated startup/shutdown, DI access |
| **Node-level shutdown logic** | `IShutdownHook` | Priority ordering, graceful cleanup |

### Execution Order

**Important:** .NET's generic host calls `StartAsync` on all `IHostedService` implementations **without a defined order** relative to each other. `NodeLifecycleHost` does **not** have any magical "run before everyone else" ability.

**Kernel Guarantees:**
**Within Node lifecycle:**
- `IStartupHook` instances execute in priority order (lower first)
- `INodeLifecycle.StartAsync` executes after all startup hooks
- `INodeLifecycle.StopAsync` executes before all shutdown hooks
- `IShutdownHook` instances execute in priority order (lower first)

❌ **Between IHostedServices:**
- No guaranteed order between `NodeLifecycleHost` and other `IHostedService` implementations
- All hosted services may start concurrently

### Coordinating with Node Readiness

If a background worker must respect Node readiness, its `ExecuteAsync` loop should check `INodeContext.LifecycleStage`:

```csharp
public class MetricsCollectorService(INodeContext nodeContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Wait until Node is Ready before doing work
            if (nodeContext.LifecycleStage != NodeLifecycleStage.Ready)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }
            
            // Node is ready - do work
            await CollectMetricsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### Example: Combining Both

```csharp
// Background worker for periodic tasks
public class MetricsCollectorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Collect metrics...
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

// Node-level initialization (runs once before Ready)
public class DatabaseWarmupHook : IStartupHook
{
    public int Priority => -100;
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Warm up connection pool...
    }
}

// Register both
builder.Services.AddHostedService<MetricsCollectorService>();
builder.Services.AddStartupHook<DatabaseWarmupHook>();
```

**Key Takeaway:** Use `IHostedService` for application features that run continuously; use Kernel lifecycle abstractions for Node orchestration.

[↑ back to table-of-contents](#table-of-contents)

---

## Complete Startup Sequence Example

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Register Grid services (includes NodeLifecycleHost as IHostedService)
        builder.Services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = "payment-node";
            options.StudioId = "demo-studio";
            options.Version = "1.0.0";
            options.Environment = "production";
        });
        
        // Register lifecycle components
        builder.Services.AddStartupHook<DatabaseMigrationHook>();
        builder.Services.AddStartupHook<CacheWarmupHook>();
        builder.Services.AddNodeLifecycle<ConnectionPoolLifecycle>();
        builder.Services.AddShutdownHook<MessageQueueDrainHook>();
        builder.Services.AddShutdownHook<ServiceDiscoveryDeregisterHook>();
        
        // Register health/readiness contributors
        builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, CacheHealthContributor>();
        builder.Services.AddSingleton<IReadinessContributor, DatabaseReadinessContributor>();
        builder.Services.AddSingleton<IReadinessContributor, CacheReadinessContributor>();
        
        // Register NodeLifecycleManager for health/readiness endpoints
        builder.Services.AddSingleton<NodeLifecycleManager>();
        
        var app = builder.Build();
        
        // NodeLifecycleHost startup sequence (automatic):
        // 1. NodeLifecycleStage = Initializing (set by NodeContext constructor)
        // 2. NodeLifecycleStage = Starting (set by NodeLifecycleHost)
        // 3. Execute IStartupHook instances by priority
        // 4. Execute INodeLifecycle.StartAsync for all registered lifecycles
        // 5. NodeLifecycleStage = Ready (currently unconditional, future versions may gate on IReadinessContributor)
        
        // Health endpoint using NodeLifecycleManager
        app.MapGet("/health", async (NodeLifecycleManager lifecycleManager, CancellationToken ct) =>
        {
            var (status, details) = await lifecycleManager.CheckHealthAsync(ct);
            
            return Results.Json(new
            {
                status = status.ToString(),
                checks = details.Select(kvp => new
                {
                    name = kvp.Key,
                    status = kvp.Value.status.ToString(),
                    message = kvp.Value.message
                })
            }, statusCode: status == HealthStatus.Unhealthy ? 503 : 200);
        });
        
        // Readiness endpoint using NodeLifecycleManager
        app.MapGet("/ready", async (NodeLifecycleManager lifecycleManager, CancellationToken ct) =>
        {
            var (isReady, details) = await lifecycleManager.CheckReadinessAsync(ct);
            
            return isReady 
                ? Results.Ok(new { ready = true }) 
                : Results.StatusCode(503, new
                {
                    ready = false,
                    blockers = details
                        .Where(kvp => !kvp.Value.isReady)
                        .Select(kvp => new { name = kvp.Key, reason = kvp.Value.reason })
                });
        });
        
        await app.RunAsync();
        
        // NodeLifecycleHost shutdown sequence (automatic):
        // 1. NodeLifecycleStage = Stopping
        // 2. Execute INodeLifecycle.StopAsync for all registered lifecycles
        // 3. Execute IShutdownHook instances by priority
        // 4. NodeLifecycleStage = Stopped
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

**Note:** Kernel includes additional tests for `NodeLifecycleHost` and `NodeLifecycleManager` integration; these examples focus on exercising the lifecycle primitives in isolation. See `HoneyDrunk.Kernel.Tests/Lifecycle/` for complete test coverage.

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
| **NodeLifecycleHost** | Orchestrates entire lifecycle (IHostedService) | N/A | N/A |
| **NodeLifecycleManager** | Aggregates health, readiness, transitions | N/A | N/A |
| **INodeLifecycle** | Node-level startup/shutdown | N/A | N/A |
| **IStartupHook** | Extensible startup | Yes (lower first) | N/A |
| **IShutdownHook** | Extensible shutdown | Yes (lower first) | N/A |
| **IHealthContributor** | Health monitoring | Yes | Yes (`IsCritical`) |
| **IReadinessContributor** | Traffic gating | Yes | Yes (`IsRequired`) |

**Key Patterns:**
- **NodeLifecycleHost** is the runtime orchestrator (registered via `AddHoneyDrunkNode`)
- **NodeLifecycleManager** provides health/readiness aggregation and stage transitions
- **INodeLifecycle** invoked after hooks complete, before Ready state
- Startup hooks execute in priority order (lower first)
- Shutdown hooks execute in priority order (lower first)
- Critical health contributors fail Node if unhealthy (fail-fast)
- Required readiness contributors block traffic if not ready
- NodeLifecycleStage tracks current state

**Typical Flow:**
1. Host starts → `NodeLifecycleHost.StartAsync` triggered
2. `NodeLifecycleStage = Starting`
3. Execute `IStartupHook` instances by priority
4. Execute `INodeLifecycle.StartAsync` for all registered lifecycles
5. `NodeLifecycleStage = Ready`
6. Periodic health checks via `NodeLifecycleManager.CheckHealthAsync()` (manual)
7. Host stops → `NodeLifecycleHost.StopAsync` triggered
8. `NodeLifecycleStage = Stopping`
9. Execute `INodeLifecycle.StopAsync` for all registered lifecycles
10. Execute `IShutdownHook` instances by priority
11. `NodeLifecycleStage = Stopped`

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

