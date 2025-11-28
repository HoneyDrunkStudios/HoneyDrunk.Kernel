# ❤️ Health - Service Health Monitoring

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IHealthCheck.cs](#ihealthcheckcs)
- [HealthStatus.cs](#healthstatuscs)
- [CompositeHealthCheck](#compositehealthcheck-implementation)
- [Relationship to Lifecycle](#relationship-to-lifecycle)
- [Summary](#summary)

---

## Overview

Health checks enable monitoring systems (Kubernetes, load balancers) to automatically detect and respond to unhealthy services.

**What This Is:** Kernel's lightweight health primitives (`IHealthCheck`) are for **internal service-level checks** within a Node. These are intentionally minimal: they return a single status and require no tags, messages, or metadata.

**What This Is Not:** Node-level health aggregation for liveness/readiness endpoints is handled by **Lifecycle's `IHealthContributor`**, which provides priority ordering, criticality flags, and per-check messages.

**Location:** `HoneyDrunk.Kernel.Abstractions/Health/`

**Key Distinction:**
- **`IHealthCheck`** - Simple service-level health function (database connection, cache ping)
- **`IHealthContributor`** - Node-level, priority-aware, criticality-aware aggregation for orchestrators

[↑ Back to top](#table-of-contents)

---

## IHealthCheck.cs

**What it is:** Minimal interface for checking the health of an internal service component.

**Location:** `HoneyDrunk.Kernel.Abstractions/Health/IHealthCheck.cs`

```csharp
public interface IHealthCheck
{
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}
```

**Design:** This is intentionally minimal: it returns a single status and requires no tags, messages, or metadata. For richer health modeling with criticality and diagnostics, see Lifecycle's `IHealthContributor`.

### Usage Example

```csharp
public class DatabaseHealthCheck(IDbConnection db) : IHealthCheck
{
    public async Task<HealthStatus> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            await db.ExecuteScalarAsync("SELECT 1", ct);
            return HealthStatus.Healthy;
        }
        catch (TimeoutException)
        {
            return HealthStatus.Degraded; // Slow but functional
        }
        catch
        {
            return HealthStatus.Unhealthy; // Cannot connect
        }
    }
}
```

### When to Use

| Use Case | Recommended Abstraction |
|----------|-------------------------|
| Internal service health (cache, database, file system) | `IHealthCheck` |
| Node-level health for Kubernetes liveness probes | `IHealthContributor` (Lifecycle) |
| Readiness checks for traffic gating | `IReadinessContributor` (Lifecycle) |

[↑ Back to top](#table-of-contents)

---

## HealthStatus.cs

**What it is:** Three-state health status for service components.

**Location:** `HoneyDrunk.Kernel.Abstractions/Health/HealthStatus.cs`

```csharp
public enum HealthStatus
{
    Healthy,    // Fully operational
    Degraded,   // Operational but impaired (e.g., slow response, partial connectivity)
    Unhealthy   // Not operational (e.g., database unreachable, critical failure)
}
```

**Design:** Simple tri-state model. No "unknown" or "warning" states - components are either working, working poorly, or not working.

[↑ Back to top](#table-of-contents)

---

## CompositeHealthCheck (Implementation)

**What it is:** Aggregates multiple health checks, returning the worst status.

**Location:** `HoneyDrunk.Kernel/Health/CompositeHealthCheck.cs`

**Design Note:** `CompositeHealthCheck` only returns a single aggregated `HealthStatus`. It does **not** track per-check messages, criticality, or diagnostics. For advanced health modeling with priority ordering and fail-fast behavior, use Lifecycle's `IHealthContributor`.

### Aggregation Rules

| Scenario | Result |
|----------|--------|
| All checks `Healthy` | `Healthy` |
| Any check `Degraded`, none `Unhealthy` | `Degraded` |
| Any check `Unhealthy` | `Unhealthy` |

### Usage Example

```csharp
var composite = new CompositeHealthCheck(new IHealthCheck[]
{
    new DatabaseHealthCheck(dbConnection),
    new CacheHealthCheck(redisClient),
    new ExternalApiHealthCheck(httpClient)
});

var status = await composite.CheckAsync();
// Returns Unhealthy if ANY check is unhealthy
// Returns Degraded if ANY check is degraded (and none unhealthy)
// Returns Healthy only if ALL checks are healthy
```

[↑ Back to top](#table-of-contents)

---

## Relationship to Lifecycle

**Kernel's Health primitives vs Lifecycle's Health Contributors:**

| Feature | `IHealthCheck` (Health) | `IHealthContributor` (Lifecycle) |
|---------|-------------------------|----------------------------------|
| **Purpose** | Internal service-level checks | Node-level health for orchestrators |
| **Status** | Simple `HealthStatus` enum | `HealthStatus` + optional message |
| **Priority** | No ordering | Priority-ordered execution |
| **Criticality** | No concept | `IsCritical` flag for fail-fast |
| **Aggregation** | Manual via `CompositeHealthCheck` | Automatic via `NodeLifecycleManager` |
| **Diagnostics** | No per-check messages | Per-check status + message |
| **Use Case** | Internal component health | Kubernetes liveness/readiness |

**Design Philosophy:**
- **`IHealthCheck`** - Boring, simple, minimal. Use for internal checks where you just need pass/fail/degraded.
- **`IHealthContributor`** - Rich, orchestrated, observable. Use for Node-level health exposed to external systems.

**Example:**

```csharp
// Internal health check (simple)
public class RedisHealthCheck(IRedisClient redis) : IHealthCheck
{
    public async Task<HealthStatus> CheckAsync(CancellationToken ct = default)
    {
        return await redis.PingAsync(ct) ? HealthStatus.Healthy : HealthStatus.Unhealthy;
    }
}

// Node-level health contributor (rich)
public class RedisHealthContributor(IRedisClient redis) : IHealthContributor
{
    public string Name => "Redis";
    public int Priority => 10;
    public bool IsCritical => false; // Node can function without cache
    
    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var latency = await redis.PingAsync(ct);
            return latency < TimeSpan.FromMilliseconds(100)
                ? (HealthStatus.Healthy, null)
                : (HealthStatus.Degraded, $"High latency: {latency.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            return (HealthStatus.Unhealthy, $"Redis unavailable: {ex.Message}");
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

**Health primitives provide minimal, internal service-level health checks.**

| Component | Purpose | Type | Audience |
|-----------|---------|------|----------|
| **`IHealthCheck`** | Service-level health function | Interface | Internal components |
| **`HealthStatus`** | Three-state health enum | Enum | All |
| **`CompositeHealthCheck`** | Simple aggregation | Implementation | Internal orchestration |

**Key Patterns:**
- Use `IHealthCheck` for simple pass/fail checks inside a Node
- Use Lifecycle's `IHealthContributor` for Node-level health exposed to Kubernetes/load balancers
- `CompositeHealthCheck` provides basic aggregation without diagnostics or criticality

**Relationship to Lifecycle:**
- **Health** - Minimal internal primitives (this is the small wrench)
- **Lifecycle** - Rich Node-level orchestration (this is the heavy machinery)

The two don't collide; they complement.

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

