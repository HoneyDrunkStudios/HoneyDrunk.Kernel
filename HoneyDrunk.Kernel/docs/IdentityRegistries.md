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
- [Adding Custom Nodes](#adding-custom-nodes)
- [Best Practices](#best-practices)
- [IDE Support](#ide-support)
- [Migration from v0.2.x](#migration-from-v02x)
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
Static registry of well-known Node identifiers organized by sector.

### Location
`HoneyDrunk.Kernel.Abstractions/Nodes.cs`

### Structure

```csharp
public static class Nodes
{
    public static class Core { /* Core infrastructure Nodes */ }
    public static class AI { /* AI and ML Nodes */ }
    public static class Ops { /* Operations Nodes */ }
    public static class Data { /* Data processing Nodes */ }
    public static class Web { /* Web and API Nodes */ }
    public static class Messaging { /* Messaging Nodes */ }
    public static class Storage { /* Storage Nodes */ }
}
```

### Available Nodes

#### Core Infrastructure

```csharp
Nodes.Core.Kernel             // "HoneyDrunk.Kernel"
Nodes.Core.Transport           // "HoneyDrunk.Transport"
Nodes.Core.Vault               // "HoneyDrunk.Vault"
Nodes.Core.Data                // "HoneyDrunk.Data"
Nodes.Core.WebRest             // "HoneyCore.Web.Rest"
Nodes.Core.Auth                // "HoneyDrunk.Auth"
Nodes.Core.Testing             // "HoneyDrunk.Testing"
Nodes.Core.Build               // "HoneyDrunk.Build"
Nodes.Core.Standards           // "HoneyDrunk.Standards"
Nodes.Core.HiveXP              // "HiveXP"
Nodes.Core.Assets              // "HoneyDrunk.Assets"
Nodes.Core.MinimalNode         // "HoneyDrunk.Core.MinimalNode" (sample)
```

#### Operations and Monitoring

```csharp
Nodes.Ops.Pipelines            // "HoneyDrunk.Pipelines"
Nodes.Ops.Actions              // "HoneyDrunk.Actions"
Nodes.Ops.Deploy               // "HoneyDrunk.Deploy"
Nodes.Ops.Tools                // "HoneyDrunk.Tools"
Nodes.Ops.Pulse                // "Pulse"
Nodes.Ops.Collector            // "HoneyDrunk.Collector"
Nodes.Ops.Comms                // "HoneyDrunk.Comms"
Nodes.Ops.Console              // "HoneyDrunk.Console"
Nodes.Ops.AuditAgent           // "Audit.Agent"
Nodes.Ops.Ledger               // "Ledger"
Nodes.Ops.Invoice              // "Invoice"
Nodes.Ops.Pay                  // "Pay"
Nodes.Ops.Subs                 // "Subs"
Nodes.Ops.ClientPortalOS       // "HoneyDrunk.ClientPortalOS"
Nodes.Ops.HomeLab              // "HoneyDrunk.HomeLab"
```

#### AI and Machine Learning

```csharp
Nodes.AI.AgentKit              // "HoneyDrunk.AgentKit"
Nodes.AI.Clarity               // "HoneyDrunk.Clarity"
Nodes.AI.Governor              // "HoneyDrunk.Governor"
Nodes.AI.Operator              // "HoneyDrunk.Operator"
```

#### Creator Tools

```csharp
Nodes.Creator.Signal           // "HoneyDrunk.Signal"
Nodes.Creator.Forge            // "Forge"
```

#### Market Applications

```csharp
Nodes.Market.MarketCore        // "MarketCore"
Nodes.Market.HiveGigs          // "HiveGigs"
Nodes.Market.Tether            // "Tether"
Nodes.Market.ReView            // "Re:View"
Nodes.Market.MemoryBank        // "MemoryBank"
Nodes.Market.DreamMarket       // "DreamMarket"
Nodes.Market.Arcadia           // "Arcadia"
```

#### Gaming and Media

```csharp
Nodes.HoneyPlay.Draft          // "Draft"
Nodes.HoneyPlay.GamePrototype  // "Game #1 (TBD)"
```

#### Robotics and Hardware

```csharp
Nodes.Cyberware.Courier        // "HoneyMech.Courier"
Nodes.Cyberware.Sim            // "HoneyMech.Sim"
Nodes.Cyberware.Servo          // "HoneyMech.Servo"
```

#### Security and Defense

```csharp
Nodes.HoneyNet.BreachLab       // "BreachLab.exe"
Nodes.HoneyNet.Sentinel        // "HoneyDrunk.Sentinel"
```

#### Meta and Ecosystem

```csharp
Nodes.Meta.Grid                // "HoneyDrunk.Grid"
Nodes.Meta.HoneyHub            // "HoneyHub"
Nodes.Meta.HoneyConnect        // "HoneyConnect"
Nodes.Meta.ArchiveLegacy       // "Archive.Legacy"
Nodes.Meta.DevPortal           // "Meta.DevPortal"
Nodes.Meta.PackagePublisher    // "Meta.PackagePublisher"
Nodes.Meta.AtlasSync           // "Meta.AtlasSync"
```

### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions;

builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.Kernel; // Canonical pattern
    options.SectorId = Sectors.Core;
    options.EnvironmentId = GridEnvironments.Production;
});
```

### Naming Convention

Node IDs follow these patterns:
- **Core infrastructure**: `HoneyDrunk.{NodeName}` or `{NodeName}`
- **Sectored Nodes**: Often include sector prefix (e.g., `HoneyMech.Courier`)
- **Meta Nodes**: May use `Meta.{NodeName}` pattern

Examples:
- `HoneyDrunk.Kernel`
- `HoneyMech.Courier`
- `Meta.DevPortal`
- `Pulse` (single-word canonical name)

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
    public static readonly SectorId Creator   = new("Creator");
    public static readonly SectorId Market    = new("Market");
    public static readonly SectorId HoneyPlay = new("HoneyPlay");
    public static readonly SectorId Cyberware = new("Cyberware");
    public static readonly SectorId HoneyNet  = new("HoneyNet");
    public static readonly SectorId Meta      = new("Meta");
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

// Canonical v3 pattern - all identities from static registries
builder.Services.AddHoneyDrunkNode(options =>
{
    // Static registries (compile-time safe)
    options.NodeId = Nodes.Web.RestApi;
    options.SectorId = Sectors.Web;
    options.EnvironmentId = GridEnvironments.Production;
    
    // Configuration-driven values
    options.Version = builder.Configuration["Version"] ?? "1.0.0";
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "production";
    
    // Observability tags
    options.Tags["region"] = builder.Configuration["Azure:Region"] ?? "us-east-1";
    options.Tags["deployment-slot"] = builder.Configuration["DeploymentSlot"] ?? "primary";
});

var app = builder.Build();
app.Services.ValidateHoneyDrunkServices();
app.UseGridContext();

app.MapGet("/", (INodeContext node) => Results.Ok(new
{
    NodeId = node.NodeId,        // "HoneyDrunk.Web.RestApi"
    Sector = node.Tags["sector"], // "web"
    Environment = node.Environment // "production"
}));

app.Run();
```

---

## Adding Custom Nodes

### Option 1: Extend Existing Registry (Recommended)

Create a partial class to extend `Nodes`:

```csharp
// MyCompany.Nodes.cs
namespace HoneyDrunk.Kernel.Abstractions;

public static partial class Nodes
{
    public static class Financial
    {
        public static readonly NodeId PaymentService = new("HoneyDrunk.Financial.PaymentService");
        public static readonly NodeId BillingService = new("HoneyDrunk.Financial.BillingService");
        public static readonly NodeId InvoiceService = new("HoneyDrunk.Financial.InvoiceService");
    }
}
```

**Usage:**
```csharp
options.NodeId = Nodes.Financial.PaymentService;
```

### Option 2: Create Custom Registry

```csharp
// MyCompanyNodes.cs
namespace MyCompany.Grid;

public static class MyNodes
{
    public static readonly NodeId OrderService = new("MyCompany.OrderService");
    public static readonly NodeId InventoryService = new("MyCompany.InventoryService");
}
```

**Usage:**
```csharp
options.NodeId = MyNodes.OrderService;
```

---

## Best Practices

### ✅ DO

```csharp
// Use static registries
options.NodeId = Nodes.Core.ApiGateway;
options.SectorId = Sectors.Core;
options.EnvironmentId = GridEnvironments.Production;

// Extend registries for custom Nodes
public static partial class Nodes
{
    public static class MyDomain { /* custom nodes */ }
}

// Use using alias for Environments to avoid collision
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;
```

### ❌ DON'T

```csharp
// Don't use string literals (no compile-time safety)
options.NodeId = new NodeId("my-service"); // ❌

// Don't use inconsistent naming
options.NodeId = new NodeId("payment_service"); // ❌ snake_case
options.NodeId = new NodeId("PaymentService");  // ❌ PascalCase (no namespace)

// Don't skip the namespace
options.NodeId = new NodeId("ApiGateway"); // ❌ Missing "HoneyDrunk.Core."
```

### Naming Conventions

| Identity Type | Pattern | Example |
|---------------|---------|---------|
| **NodeId** | `{Namespace}.{Sector}.{Name}` | `HoneyDrunk.Core.ApiGateway` |
| **SectorId** | `{lowercase}` | `core`, `ai`, `web` |
| **EnvironmentId** | `{lowercase}` | `production`, `staging`, `development` |
| **ErrorCode** | `{category}.{detail}` | `validation.failed`, `auth.unauthorized` |

---

## IDE Support

### IntelliSense Discovery

Type `Nodes.` and IntelliSense shows all available sectors:
```
Nodes.
  ├─ Core
  ├─ AI
  ├─ Ops
  ├─ Data
  ├─ Web
  ├─ Messaging
  └─ Storage
```

Type `Nodes.Core.` and IntelliSense shows all Core nodes:
```
Nodes.Core.
  ├─ Minimal
  ├─ ApiGateway
  ├─ ConfigService
  ├─ SecretsService
  └─ IdentityService
```

### Go-to-Definition

F12 on `Nodes.Core.Minimal` jumps directly to the definition:
```csharp
public static readonly NodeId Minimal = new("HoneyDrunk.Core.Minimal");
```

### Refactoring Support

Rename `Nodes.Core.Minimal` → `Nodes.Core.SampleNode` propagates everywhere automatically.

---

## Migration from v0.2.x

### Before (v0.2.x)

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = new NodeId("payment-service");
    options.SectorId = SectorId.WellKnown.Core;
    options.EnvironmentId = EnvironmentId.WellKnown.Production;
});
```

### After (v0.3.0)

```csharp
using HoneyDrunk.Kernel.Abstractions;
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Financial.PaymentService; // Static registry
    options.SectorId = Sectors.Financial;             // Cleaner syntax
    options.EnvironmentId = GridEnvironments.Production; // Alias for collision
});
```

---

## Summary

| Registry | Purpose | Canonical Usage |
|----------|---------|-----------------|
| **Nodes** | Well-known Node identifiers | `Nodes.Core.ApiGateway` |
| **Sectors** | Sector grouping | `Sectors.Web` |
| **Environments** | Deployment stages | `GridEnvironments.Production` |
| **ErrorCode.WellKnown** | Standard error codes | `ErrorCode.WellKnown.NotFound` |

**Key Benefits:**
- ✅ Compile-time type safety
- ✅ IDE support (IntelliSense, go-to-def, refactoring)
- ✅ Consistent naming across the Grid
- ✅ Discoverable via IntelliSense
- ✅ Extensible via partial classes

**Best Practice:**
Always prefer static registries over ad-hoc string creation. This is the **v3 golden path**.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
