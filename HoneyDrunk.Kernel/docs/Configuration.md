# ⚙️ Configuration - Hierarchical Config Management

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

Configuration in HoneyDrunk.Kernel follows a hierarchical model where settings can be scoped to different levels with automatic fallback:

```
Global → Studio → Node → Tenant → Project → Request
(Broadest)                                 (Narrowest)
```

**Location:** `HoneyDrunk.Kernel.Abstractions/Configuration/`

---

## ConfigScopeType.cs

Enum defining configuration scope levels:

| Value | Description | Example |
|-------|-------------|---------|
| `Global` | Shared across all Studios and Nodes | Default timeouts, global feature flags |
| `Studio` | Shared within a Studio environment | Studio-specific endpoints, regional settings |
| `Node` | Specific to a Node instance | Node-specific ports, local paths |
| `Tenant` | Tenant-specific configuration | Tenant quotas, custom branding |
| `Project` | Project-specific configuration | Project feature toggles |
| `Request` | Per-request/operation overrides | A/B test variants, canary flags |

---

## IConfigScope.cs

Defines the scope/context for configuration access.

```csharp
public interface IConfigScope
{
    ConfigScopeType ScopeType { get; }
    string? ScopeId { get; }
    IConfigScope? ParentScope { get; }
    string ScopePath { get; }
    IConfigScope CreateChildScope(ConfigScopeType scopeType, string scopeId);
}
```

### Usage Example

```csharp
// Create nested scope
var globalScope = new ConfigScope(ConfigScopeType.Global, null);
var studioScope = globalScope.CreateChildScope(ConfigScopeType.Studio, "honeycomb");
var nodeScope = studioScope.CreateChildScope(ConfigScopeType.Node, "payment-node");

Console.WriteLine(nodeScope.ScopePath); 
// Output: "global/studio:honeycomb/node:payment-node"

// Lookup order: node → studio → global
```

---

## ConfigKey.cs

Strongly-typed configuration key with hierarchical support.

```csharp
var dbKey = new ConfigKey("Database:ConnectionString");
var parentKey = dbKey.Parent; // ConfigKey("Database")
var segments = dbKey.GetSegments(); // ["Database", "ConnectionString"]
var childKey = dbKey.CreateChild("Timeout"); // "Database:ConnectionString:Timeout"
```

---

## ConfigPath.cs

Fully-qualified configuration path combining scope and key.

```csharp
var path = new ConfigPath(studioScope, new ConfigKey("Database:ConnectionString"));
Console.WriteLine(path.FullPath); 
// "studio:honeycomb/Database:ConnectionString"
```

---

## NodeRuntimeOptions.cs

Standard runtime configuration options for all Nodes.

```csharp
public sealed record NodeRuntimeOptions
{
    public string Environment { get; set; } = "production";
    public string? Region { get; set; }
    public string? DeploymentRing { get; set; }
    public bool EnableDetailedTelemetry { get; set; } = true;
    public bool EnableDistributedTracing { get; set; } = true;
    public double TelemetrySamplingRate { get; set; } = 1.0;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int ShutdownGracePeriodSeconds { get; set; } = 30;
    public bool EnableSecretRotation { get; set; } = true;
    public int SecretRotationIntervalMinutes { get; set; } = 60;
    public Dictionary<string, string> Tags { get; init; } = [];
}
```

### Usage

```csharp
builder.Services.Configure<NodeRuntimeOptions>(
    builder.Configuration.GetSection("NodeRuntime"));

public class Startup(IOptions<NodeRuntimeOptions> options)
{
    var sampling = options.Value.TelemetrySamplingRate;
}
```

---

[← Back to File Guide](FILE_GUIDE.md)

