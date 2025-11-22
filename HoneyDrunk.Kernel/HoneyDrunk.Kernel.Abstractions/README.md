# HoneyDrunk.Kernel.Abstractions

[![NuGet](https://img.shields.io/nuget/v/HoneyDrunk.Kernel.Abstractions.svg)](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Pure Contracts for the HoneyDrunk Grid** - Zero-dependency abstractions that define the semantic OS layer.

## ?? What Is This?

**HoneyDrunk.Kernel.Abstractions** contains the pure interface definitions and contracts for the entire HoneyDrunk.OS Grid. This package has **zero runtime dependencies** (only build-time analyzers) and can be referenced by any library that needs to understand Grid primitives without taking on implementation dependencies.

## ?? What's Inside

### ?? Identity
Strongly-typed, validated identifiers:
- **NodeId** - Kebab-case validated Node identifiers
- **CorrelationId** - ULID-based request correlation
- **TenantId** - Multi-tenant isolation boundaries
- **ProjectId** - Project/workspace organization
- **RunId** - Execution instance tracking

### ?? Context
Three-tier context model:
- **IGridContext** - Per-operation context that flows across Node boundaries
- **INodeContext** - Per-process static Node identity
- **IOperationContext** - Per-unit-of-work timing and outcome
- **IGridContextAccessor** - Ambient context accessor

### ?? Configuration
Hierarchical configuration with scope fallback (Global ? Studio ? Node ? Tenant ? Project ? Request)

### ?? Hosting
Node hosting, discovery, and capability advertisement

### ?? Agents
AI agent execution framework with scoped permissions

### ?? Lifecycle
Node lifecycle orchestration (startup hooks, health/readiness contributors, shutdown hooks)

### ?? Telemetry
OpenTelemetry-ready observability (W3C Trace Context, enrichers, log scopes, standard tags)

### ?? Secrets
Secure secrets management with fallback support

### ?? Health
Service health monitoring (IHealthCheck, HealthStatus)

### ?? Diagnostics
Metrics collection (counters, histograms, gauges)

### ?? Dependency Injection
Modular service registration (IModule)

## ?? Installation

```bash
dotnet add package HoneyDrunk.Kernel.Abstractions
```

```xml
<PackageReference Include="HoneyDrunk.Kernel.Abstractions" Version="0.2.1" />
```

## ?? When to Use This Package

**Use Abstractions when:**
- ? Building a library that works with Grid primitives
- ? You need contracts without implementation dependencies
- ? Creating custom implementations of Kernel interfaces
- ? Defining Node capabilities and manifests
- ? You want minimal transitive dependencies

**Use HoneyDrunk.Kernel (full runtime) when:**
- ? You need actual implementations
- ? Building an executable Node/service
- ? You need context mappers or lifecycle hosts

## ?? Design Philosophy

### Minimal Dependencies
This package only depends on:
- .NET 10 BCL
- Microsoft.Extensions.* abstractions (DI, Configuration, Hosting)
- Ulid (for ULID-based identity types)
- HoneyDrunk.Standards (build-time only - analyzers)

### Stable Contracts
Interfaces follow semantic versioning strictly:
- Breaking changes only in major versions
- Additive changes in minor versions
- Bug fixes in patch versions

### Grid-First Design
All abstractions assume distributed, multi-tenant, observable systems:
- Context propagates automatically
- Identity is strongly-typed
- Observability is built-in
- Multi-tenancy is first-class

## ?? Example: Custom Implementation

```csharp
// Custom secrets source
public class EnvironmentSecretsSource : ISecretsSource
{
    public bool TryGetSecret(string key, out string? value)
    {
        value = Environment.GetEnvironmentVariable($"SECRET_{key}");
        return value is not null;
    }
}

// Custom health check
public class DatabaseHealthCheck(IDbConnection db) : IHealthCheck
{
    public async Task<HealthStatus> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            await db.ExecuteScalarAsync("SELECT 1", ct);
            return HealthStatus.Healthy;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }
}
```

## ?? Related Packages

- **[HoneyDrunk.Kernel](https://www.nuget.org/packages/HoneyDrunk.Kernel/)** - Runtime implementations
- **[HoneyDrunk.Standards](https://www.nuget.org/packages/HoneyDrunk.Standards/)** - Analyzers and coding conventions

## ?? Documentation

- **[Complete File Guide](../docs/FILE_GUIDE.md)** - Comprehensive architecture documentation
- **[Identity Guide](../docs/Identity.md)** - Strongly-typed identifiers
- **[Context Guide](../docs/Context.md)** - Context propagation patterns
- **[Lifecycle Guide](../docs/Lifecycle.md)** - Lifecycle orchestration
- **[Telemetry Guide](../docs/Telemetry.md)** - Observability integration

## ?? License

This project is licensed under the [MIT License](../LICENSE).

---

**Built with ?? by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](../docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
