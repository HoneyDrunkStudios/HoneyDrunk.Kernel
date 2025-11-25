# ❤️ Health - Service Health Monitoring

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IHealthCheck.cs](#ihealthcheckcs)
- [HealthStatus.cs](#-healthstatuscs)
- [CompositeHealthCheck](#compositehealthcheck-implementation)

---

## Overview

Health checks enable monitoring systems (Kubernetes, load balancers) to automatically detect and respond to unhealthy services.

**Location:** `HoneyDrunk.Kernel.Abstractions/Health/`

[↑ Back to top](#table-of-contents)

---

## IHealthCheck.cs

```csharp
public interface IHealthCheck
{
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}
```

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

[↑ Back to top](#table-of-contents)

---

## ❤️ HealthStatus.cs

```csharp
public enum HealthStatus
{
    Healthy,    // Everything is fine
    Degraded,   // Working but with issues (e.g., slow response)
    Unhealthy   // Critical failure (e.g., database unreachable)
}
```

[↑ Back to top](#table-of-contents)

---

## CompositeHealthCheck (Implementation)

**Location:** `HoneyDrunk.Kernel/Health/CompositeHealthCheck.cs`

Aggregates multiple health checks, returning the worst status.

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

[← Back to File Guide](../FILE_GUIDE_NEW.md) | [↑ Back to top](#table-of-contents)

