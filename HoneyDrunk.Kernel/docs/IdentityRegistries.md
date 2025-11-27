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
    options.SectorId = new SectorId("market");       // Inconsistent naming
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
    options.NodeId = WellKnownNodes.Core.Kernel;      // Compile-time safe
    options.SectorId = Sectors.Core;                   // Consistent naming
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
WellKnownNodes.Core.Kernel     // "kernel"
WellKnownNodes.Core.Transport  // "transport"
WellKnownNodes.Core.Vault      // "vault"
WellKnownNodes.Core.Data       // "data"
WellKnownNodes.Core.WebRest    // "web-rest"
WellKnownNodes.Core.Auth       // "auth"
```

#### Observability and Orchestration (2 nodes)

```csharp
WellKnownNodes.Ops.Pulse       // "pulse"
WellKnownNodes.Ops.Grid        // "grid"
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
    public static readonly NodeId Self = new("arcadia");
    public static readonly NodeId Auth = WellKnownNodes.Core.Auth; // Reference infra
}

// Option 3: Use Grid.Contracts (future package)
options.NodeId = GridNodes.Market.Arcadia; // From code-gen
```

### Naming Convention

Node IDs follow kebab-case pattern for all nodes:
- **Infrastructure**: Simple lowercase or kebab-case (e.g., `kernel`, `web-rest`, `pulse`)
- **Applications**: Same pattern (e.g., `arcadia`, `payment-service`)

**Rules:**
- ✅ **Lowercase** with optional hyphens as separators
- ✅ **Human-readable** and stable
- ❌ **No uppercase** (`Kernel`, `PaymentService`)
- ❌ **No dots** (`HoneyDrunk.Kernel`)
- ❌ **No underscores** (`payment_service`)
- ❌ **No single characters** (`p`)
- ❌ **No consecutive hyphens** (`payment--service`)

**Examples:**
- `kernel` (infrastructure, single-word)
- `web-rest` (infrastructure, kebab-case)
- `arcadia` (application, simple)
- `payment-service` (application, kebab-case)
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
    options.NodeId = WellKnownNodes.Ops.Pulse;
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
    options.NodeId = WellKnownNodes.Core.WebRest;
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

**Kernel provides foundational categories. Domain-specific Nodes extend these patterns.**

#### Validation
```csharp
ErrorCode.WellKnown.ValidationInput           // "validation.input"
ErrorCode.WellKnown.ValidationBusiness         // "validation.business"
```

#### Authentication & Authorization
```csharp
ErrorCode.WellKnown.AuthenticationFailure         // "authentication.failure"
ErrorCode.WellKnown.AuthenticationTokenExpired    // "authentication.token-expired"
ErrorCode.WellKnown.AuthorizationFailure          // "authorization.failure"
```

#### Context (Grid-Specific)
```csharp
ErrorCode.WellKnown.ContextMissing      // "context.missing" - TenantId, CorrelationId missing
ErrorCode.WellKnown.ContextInvalid      // "context.invalid" - malformed or cross-tenant
ErrorCode.WellKnown.TenantInactive      // "tenant.inactive" - tenant disabled/suspended
ErrorCode.WellKnown.ProjectInactive     // "project.inactive" - project disabled/suspended
```

#### Resource
```csharp
ErrorCode.WellKnown.ResourceNotFound    // "resource.notfound"
ErrorCode.WellKnown.ResourceConflict    // "resource.conflict"
```

#### Operation & State (Distributed System)
```csharp
ErrorCode.WellKnown.StateVersionConflict         // "state.version-conflict" - optimistic concurrency
ErrorCode.WellKnown.OperationIdempotentReplay    // "operation.idempotent-replay" - already applied
ErrorCode.WellKnown.OperationTimeout             // "operation.timeout"
```

#### Contract (Transport/Envelope)
```csharp
ErrorCode.WellKnown.ContractInvalid               // "contract.invalid"
ErrorCode.WellKnown.ContractUnsupportedVersion    // "contract.unsupported-version"
ErrorCode.WellKnown.ContractMissingField          // "contract.missing-field"
```

#### Feature & Quota (Runtime Gating)
```csharp
ErrorCode.WellKnown.FeatureDisabled      // "feature.disabled" - flag off
ErrorCode.WellKnown.FeatureNotAllowed    // "feature.not-allowed" - requires higher tier
ErrorCode.WellKnown.QuotaExceeded        // "quota.exceeded" - tenant limits hit
```

#### Dependency
```csharp
ErrorCode.WellKnown.DependencyUnavailable    // "dependency.unavailable"
ErrorCode.WellKnown.DependencyTimeout        // "dependency.timeout"
```

#### Configuration
```csharp
ErrorCode.WellKnown.ConfigurationInvalid    // "configuration.invalid"
```

#### System
```csharp
ErrorCode.WellKnown.InternalError          // "internal.error"
ErrorCode.WellKnown.ServiceUnavailable     // "service.unavailable"
ErrorCode.WellKnown.RateLimitExceeded      // "rate-limit.exceeded"
```

### Domain-Specific Extensions

**Other Nodes define their own error codes following the same pattern:**

```csharp
// Assets Node (HoneyDrunk.Assets)
public static class AssetErrorCodes
{
    public static readonly ErrorCode TooLarge = new("asset.too-large");
    public static readonly ErrorCode UnsupportedFormat = new("asset.unsupported-format");
    public static readonly ErrorCode ScanFailed = new("asset.scan-failed");
}

// AgentKit (HoneyDrunk.AgentKit)
public static class AgentErrorCodes
{
    public static readonly ErrorCode PlanFailed = new("agent.plan-failed");
    public static readonly ErrorCode ExecutionFailed = new("agent.execution-failed");
    public static readonly ErrorCode Rejected = new("agent.rejected");
    public static readonly ErrorCode UnsupportedIntent = new("agent.unsupported-intent");
}

// Cyberware (HoneyMech)
public static class DeviceErrorCodes
{
    public static readonly ErrorCode Offline = new("device.offline");
    public static readonly ErrorCode CommandRejected = new("device.command-rejected");
    public static readonly ErrorCode SimulationNotSupported = new("simulation.not-supported");
}
```

### Usage Example

```csharp
using HoneyDrunk.Kernel.Abstractions.Errors;
using HoneyDrunk.Kernel.Abstractions.Identity;

if (order is null)
{
    throw new NotFoundException(
        "Order not found",
        ErrorCode.WellKnown.ResourceNotFound);
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
    options.StudioId = builder.Configuration["Grid:StudioId"] ?? "honeydrunk-studios";
    
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
    public static readonly NodeId Self = new("arcadia");
    
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
    public static readonly NodeId Self = new("my-app");
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
| **NodeId** | kebab-case | `kernel`, `web-rest`, `payment-service` |
| **SectorId** | kebab-case | `core`, `ai`, `honeyplay` |
| **EnvironmentId** | kebab-case | `production`, `staging`, `development` |
| **ErrorCode** | category.detail | `validation.input`, `context.missing` |

**Note:** Registry classes (`Sectors`, `Environments`) use PascalCase property names (e.g., `Sectors.Core`, `Environments.Production`) that map to the kebab-case identity values shown above.
---

## Summary

| Registry | Purpose | Canonical Usage | Count |
|----------|---------|-----------------|-------|
| **WellKnownNodes** | Infrastructure node identifiers | `WellKnownNodes.Core.Kernel` | 8 nodes |
| **Sectors** | Sector grouping | `Sectors.Core` | 9 sectors |
| **Environments** | Deployment stages | `GridEnvironments.Production` | 7 environments |
| **ErrorCode.WellKnown** | Grid-native error codes | `ErrorCode.WellKnown.ContextMissing` | 27 codes |

**Key Benefits:**
- ✅ Compile-time type safety for infrastructure dependencies
- ✅ IDE support (IntelliSense, go-to-def, refactoring)
- ✅ Consistent naming for core primitives
- ✅ Grid-native error taxonomy (context, state, contracts, features, quotas)
- ✅ Applications define their own NodeIds (no Kernel coupling)
- ✅ Grid catalog (nodes.json) remains the single source of truth

**Architecture:**
```
Kernel (WellKnownNodes) → 8 infrastructure nodes
         ↓
Grid.Contracts → Full catalog from nodes.json (future)
         ↓
Applications → Define their own NodeIds

Kernel (ErrorCode.WellKnown) → 27 foundational error categories
         ↓
Domain Nodes → Extend with their own codes (asset.*, agent.*, device.*)
```

**Best Practice:**
- Use `WellKnownNodes` for infrastructure dependencies
- Define your own constants for application nodes
- Use `ErrorCode.WellKnown.*` for Grid-native errors
- Domain Nodes extend error codes with their own namespaces

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
