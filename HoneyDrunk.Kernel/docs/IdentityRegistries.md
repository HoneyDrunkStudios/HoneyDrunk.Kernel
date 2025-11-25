# 🏷️ Identity Registries - Static Well-Known Values

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Why Static Registries?](#why-static-registries)
- [Nodes Registry](#nodes-registry)
- [Sectors Registry](#sectors-registry)
- [Environments Registry](#environments-registry)
- [ErrorCode Registry](#errorcode-registry-new-in-v030)
- [Complete Example](#complete-example)
- [Best Practices](#best-practices)
- [Summary](#summary)

---

## Overview

Identity registries provide static, well-known values for Node IDs, Sectors, and Environments. Using these registries ensures consistency across the Grid and prevents typos from string literals.

**Location:** `HoneyDrunk.Kernel.Abstractions/`

**Key Concepts:**
- **Static Registries** - Centralized, compile-time-safe identity values
- **Canonical Pattern** - Preferred over ad-hoc string creation
- **Refactoring Safety** - IDE renames propagate automatically
- **Discoverability** - IntelliSense shows all available values

---

## Why Static Registries?

### ❌ Before (v0.2.x - String Literals)

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-service"); // Typos not caught at compile time
    options.SectorId = new SectorId("financial");    // Inconsistent naming
    options.EnvironmentId = new EnvironmentId("prod"); // "prod" vs "production"?
});
```

**Problems:**
- ❌ Typos discovered at runtime
- ❌ Inconsistent naming across Nodes
- ❌ No IDE refactoring support
- ❌ No IntelliSense discovery

### ✅ After (v0.3.0 - Static Registries)

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Financial.PaymentService; // Compile-time safe
    options.SectorId = Sectors.Financial;             // Consistent naming
    options.EnvironmentId = GridEnvironments.Production; // Canonical value
});
```

**Benefits:**
- ✅ Compile-time type safety
- ✅ Consistent naming enforced
- ✅ IDE refactoring and go-to-definition
- ✅ IntelliSense discovery

---

## Nodes Registry

### What it is
Static registry of **infrastructure** Node identifiers required by Kernel and core Grid operations.

### ⚠️ Architecture Note (v0.3.0)

**Kernel only provides infrastructure node identities.**

- **Infrastructure Nodes** (8 nodes) → Defined in `WellKnownNodes` (Core, Ops sectors)
- **Application Nodes** (50+ nodes) → Define in your app or use Grid.Contracts (future)

**Why?**
- Kernel is the OS layer and shouldn't know about userland applications
- Adding a product node shouldn't require a Kernel package update
- Grid catalog (nodes.json) is the single source of truth for all nodes

### Location
`HoneyDrunk.Kernel.Abstractions/WellKnownNodes.cs`

### Structure

```csharp
public static class WellKnownNodes
{
    public static class Core { /* Core infrastructure (6 nodes) */ }
    public static class Ops  { /* Observability and orchestration (2 nodes) */ }
}
```

### Available Infrastructure Nodes

#### Core Infrastructure (6 nodes)

```csharp
WellKnownNodes.Core.Kernel     // "HoneyDrunk.Kernel"
WellKnownNodes.Core.Transport  // "HoneyDrunk.Transport"
WellKnownNodes.Core.Vault      // "HoneyDrunk.Vault"
WellKnownNodes.Core.Data       // "HoneyDrunk.Data"
WellKnownNodes.Core.WebRest    // "HoneyCore.Web.Rest"
WellKnownNodes.Core.Auth       // "HoneyDrunk.Auth"
```

#### Observability and Orchestration (2 nodes)

```csharp
WellKnownNodes.Ops.Pulse       // "Pulse"
WellKnownNodes.Ops.Grid        // "HoneyDrunk.Grid"
```

### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions;

builder.Services.AddHoneyDrunkNode(options =>
{
    // Infrastructure dependency - use WellKnownNodes
    options.NodeId = WellKnownNodes.Core.Kernel;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = GridEnvironments.Production;
});
```

### For Application Nodes

Application nodes should define their own NodeIds:

```csharp
// Option 1: Inline definition (simple apps)
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("arcadia");
    options.SectorId = Sectors.Market;
});

// Option 2: Define in your app (recommended)
public static class ArcadiaNodeIds
{
    public static readonly NodeId Self = new("Arcadia");
    public static readonly NodeId Auth = WellKnownNodes.Core.Auth; // Reference infra
}

// Option 3: Use Grid.Contracts (future package)
options.NodeId = GridNodes.Market.Arcadia; // From code-gen
```

### Naming Convention

Node IDs follow these patterns:
- **Core infrastructure**: `HoneyDrunk.{NodeName}` or `{NodeName}`
- **Applications**: Define your own convention

Examples:
- `HoneyDrunk.Kernel` (infrastructure)
- `Pulse` (infrastructure, single-word)
- `Arcadia` (application)
- `HoneyMech.Courier` (application with namespace)

---

## Sectors Registry

### What it is
Static registry of well-known Sector identifiers for grouping related Nodes.

### Location
`HoneyDrunk.Kernel.Abstractions/Sectors.cs`

### Available Sectors

```csharp
public static class Sectors
{
    public static readonly SectorId Core      = SectorId.WellKnown.Core;
    public static readonly SectorId Ops       = SectorId.WellKnown.Ops;
    public static readonly SectorId AI        = SectorId.WellKnown.AI;
    public static readonly SectorId Creator   = SectorId.WellKnown.Creator;
    public static readonly SectorId Market    = SectorId.WellKnown.Market;
    public static readonly SectorId HoneyPlay = SectorId.WellKnown.HoneyPlay;
    public static readonly SectorId Cyberware = SectorId.WellKnown.Cyberware;
    public static readonly SectorId HoneyNet  = SectorId.WellKnown.HoneyNet;
    public static readonly SectorId Meta      = SectorId.WellKnown.Meta;
}
```

### Sector Descriptions

| Sector | Purpose | Example Nodes |
|--------|---------|---------------|
| **Core** | Foundational primitives | Kernel, Transport, Vault, Data, Auth |
| **Ops** | CI/CD and observability | Pipelines, Actions, Deploy, Pulse, Collector |
| **AI** | Agents and orchestration | AgentKit, Clarity, Governor, Operator |
| **Creator** | Content and amplification | Signal, Forge |
| **Market** | Public SaaS products | MarketCore, HiveGigs, Arcadia, Re:View |
| **HoneyPlay** | Gaming and media | Draft, Game prototypes |
| **Cyberware** | Robotics and hardware | Courier, Sim, Servo |
| **HoneyNet** | Security and defense | BreachLab, Sentinel |
| **Meta** | Registries and knowledge | Grid, HoneyHub, DevPortal, AtlasSync |

### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions;

builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Ops.Pulse;
    options.SectorId = Sectors.Ops; // Canonical pattern
});
```

### Why Not `SectorId.WellKnown.*`?

Both work, but `Sectors.*` is cleaner:

```csharp
// Old style (verbose)
options.SectorId = SectorId.WellKnown.Core;

// New style (canonical)
options.SectorId = Sectors.Core;
```

---

## Environments Registry

### What it is
Static registry of well-known Environment identifiers for deployment stages.

### Location
`HoneyDrunk.Kernel.Abstractions/Environments.cs`

### ⚠️ Namespace Collision

`Microsoft.Extensions.Hosting` also exports an `Environments` class. Use a using alias:

```csharp
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

options.EnvironmentId = GridEnvironments.Production;
```

### Available Environments

```csharp
public static class Environments
{
    public static readonly EnvironmentId Production  = EnvironmentId.WellKnown.Production;
    public static readonly EnvironmentId Staging     = EnvironmentId.WellKnown.Staging;
    public static readonly EnvironmentId Development = EnvironmentId.WellKnown.Development;
    public static readonly EnvironmentId Testing     = EnvironmentId.WellKnown.Testing;
    public static readonly EnvironmentId Performance = EnvironmentId.WellKnown.Performance;
    public static readonly EnvironmentId Integration = EnvironmentId.WellKnown.Integration;
    public static readonly EnvironmentId Local       = EnvironmentId.WellKnown.Local;
}
```

### Environment Descriptions

| Environment | Purpose | Typical Usage |
|-------------|---------|---------------|
| **Production** | Live customer traffic | Real users, real data |
| **Staging** | Pre-production validation | Final testing before release |
| **Development** | Active development | Feature development |
| **Testing** | Automated testing | CI/CD test runs |
| **Performance** | Load testing | Performance benchmarks |
| **Integration** | Third-party integration | External API testing |
| **Local** | Developer workstation | Local development |

### Usage Example

```csharp
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.ApiGateway;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = GridEnvironments.Production; // Canonical pattern
});
```

### Dynamic Environment from Configuration

```csharp
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

var environmentName = builder.Environment.EnvironmentName; // ASP.NET Core env

options.EnvironmentId = environmentName switch
{
    "Production" => GridEnvironments.Production,
    "Staging" => GridEnvironments.Staging,
    "Development" => GridEnvironments.Development,
    _ => GridEnvironments.Local
};
```

---

## ErrorCode Registry (NEW in v0.3.0)

### What it is
Static registry of well-known error codes for consistent error handling.

### Location
`HoneyDrunk.Kernel.Abstractions/Identity/ErrorCode.cs` (WellKnown nested class)

### Available Error Codes

```csharp
ErrorCode.WellKnown.ValidationFailed      // "validation.failed"
ErrorCode.WellKnown.NotFound              // "resource.not-found"
ErrorCode.WellKnown.Unauthorized          // "auth.unauthorized"
ErrorCode.WellKnown.Forbidden             // "auth.forbidden"
ErrorCode.WellKnown.Conflict              // "resource.conflict"
ErrorCode.WellKnown.InternalError         // "internal.error"
ErrorCode.WellKnown.ServiceUnavailable    // "service.unavailable"
ErrorCode.WellKnown.Timeout               // "operation.timeout"
ErrorCode.WellKnown.RateLimitExceeded     // "rate-limit.exceeded"
```

### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Identity;

if (order is null)
{
    throw new NotFoundException(
        "Order not found",
        ErrorCode.WellKnown.NotFound);
}
```

---

## Complete Example

### Production Node Configuration

```csharp
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Hosting;
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure node using WellKnownNodes
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = WellKnownNodes.Core.WebRest;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = GridEnvironments.Production;
    
    options.Version = builder.Configuration["Version"] ?? "1.0.0";
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "production";
    
    options.Tags["region"] = builder.Configuration["Azure:Region"] ?? "us-east-1";
    options.Tags["deployment-slot"] = builder.Configuration["DeploymentSlot"] ?? "primary";
});

var app = builder.Build();
app.Services.ValidateHoneyDrunkServices();
app.UseGridContext();

app.MapGet("/", (INodeContext node) => Results.Ok(new
{
    NodeId = node.NodeId,
    Sector = node.Tags["sector"],
    Environment = node.Environment
}));

app.Run();
```

### Application Node Configuration

```csharp
// Application defines its own NodeId
public static class ArcadiaNodeIds
{
    public static readonly NodeId Self = new("Arcadia");
    
    // Reference infrastructure dependencies
    public static readonly NodeId Auth = WellKnownNodes.Core.Auth;
    public static readonly NodeId Data = WellKnownNodes.Core.Data;
}

// Usage
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = ArcadiaNodeIds.Self;
    options.SectorId = Sectors.Market;
    options.EnvironmentId = GridEnvironments.Production;
});
```

---

## Best Practices

### ✅ DO

```csharp
// Use WellKnownNodes for infrastructure dependencies
options.NodeId = WellKnownNodes.Core.Kernel;
options.SectorId = Sectors.Core;
options.EnvironmentId = GridEnvironments.Production;

// Define your own NodeIds for applications
public static class MyAppNodeIds
{
    public static readonly NodeId Self = new("MyApp");
    public static readonly NodeId Database = WellKnownNodes.Core.Data;
}

// Use using alias for Environments to avoid collision
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;
```

### ❌ DON'T

```csharp
// Don't use string literals without constants (no compile-time safety)
options.NodeId = new NodeId("my-service"); // ❌ Inline literal

// Don't use inconsistent naming
options.NodeId = new NodeId("payment_service"); // ❌ snake_case
options.NodeId = new NodeId("PaymentService");  // ❌ PascalCase (use kebab-case)

// Don't skip validation
options.NodeId = new NodeId("p"); // ❌ Too short (min 3 chars)
```

### Naming Conventions

| Identity Type | Pattern | Example |
|---------------|---------|---------|
| **NodeId** | `{Namespace}.{Name}` or `{Name}` | `HoneyDrunk.Kernel`, `Pulse` |
| **SectorId** | `{PascalCase}` | `Core`, `AI`, `HoneyPlay` |
| **EnvironmentId** | `{lowercase}` | `production`, `staging`, `development` |
| **ErrorCode** | `{category}.{detail}` | `validation.failed`, `auth.unauthorized` |

---

## Summary

| Registry | Purpose | Canonical Usage | Count |
|----------|---------|-----------------|-------|
| **WellKnownNodes** | Infrastructure node identifiers | `WellKnownNodes.Core.Kernel` | 8 nodes |
| **Sectors** | Sector grouping | `Sectors.Core` | 9 sectors |
| **Environments** | Deployment stages | `GridEnvironments.Production` | 7 environments |
| **ErrorCode.WellKnown** | Standard error codes | `ErrorCode.WellKnown.NotFound` | 11 codes |

**Key Benefits:**
- ✅ Compile-time type safety for infrastructure dependencies
- ✅ IDE support (IntelliSense, go-to-def, refactoring)
- ✅ Consistent naming for core primitives
- ✅ Applications define their own NodeIds (no Kernel coupling)
- ✅ Grid catalog (nodes.json) remains the single source of truth

**Architecture:**
```
Kernel (WellKnownNodes) → 8 infrastructure nodes
         ↓
Grid.Contracts → Full catalog from nodes.json (future)
         ↓
Applications → Define their own NodeIds
```

**Best Practice:**
Use `WellKnownNodes` for infrastructure dependencies. Define your own constants for application nodes.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
