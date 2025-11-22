# 🌐 HoneyDrunk.Kernel - Complete File Guide

## Overview

**Think of this library as the operating system kernel for a distributed computing grid**

Just like how an OS kernel provides fundamental primitives (process management, memory, I/O) that all applications rely on, this library provides the core runtime primitives that every HoneyDrunk Node needs to participate in the Grid. It defines the grammar that Nodes use to communicate, coordinate, and observe their distributed operations.

**Key Concepts:**
- **Grid**: The distributed system of interconnected Nodes
- **Node**: An independent service/application running in the Grid
- **Studio**: An isolated environment/workspace containing Nodes
- **Agent**: An AI assistant or automation that operates within the Grid
- **Context**: Tracking information that flows through operations

---

## 📚 Documentation Structure

This guide is organized into focused documents by domain:

### 🔷 HoneyDrunk.Kernel.Abstractions (Contracts)

| Domain | Document | Description |
|--------|----------|-------------|
| 🆔 **Identity** | [Identity.md](Identity.md) | Strongly-typed identifiers (NodeId, CorrelationId, TenantId, ProjectId, RunId) |
| 🌐 **Context** | [Context.md](Context.md) | Distributed context propagation (IGridContext, INodeContext, IOperationContext) |
| ⚙️ **Configuration** | [Configuration.md](Configuration.md) | Hierarchical configuration (IConfigScope, ConfigKey, NodeRuntimeOptions) |
| 🏢 **Hosting** | [Hosting.md](Hosting.md) | Node hosting and discovery (INodeDescriptor, INodeManifest, IStudioConfiguration) |
| 🤖 **Agents** | [Agents.md](Agents.md) | Agent execution framework (IAgentDescriptor, IAgentExecutionContext) |
| 🔄 **Lifecycle** | [Lifecycle.md](Lifecycle.md) | Node lifecycle management (INodeLifecycle, IStartupHook, IShutdownHook, Health/Readiness) |
| 📡 **Telemetry** | [Telemetry.md](Telemetry.md) | Observability primitives (ITelemetryContext, ITraceEnricher, ILogScopeFactory) |
| 🔐 **Secrets** | [Secrets.md](Secrets.md) | Secure secrets management (ISecretsSource) |
| ❤️ **Health** | [Health.md](Health.md) | Service health monitoring (IHealthCheck, HealthStatus) |
| 📈 **Diagnostics** | [Diagnostics.md](Diagnostics.md) | Metrics and diagnostics (IMetricsCollector) |
| 🔌 **DI** | [DependencyInjection.md](DependencyInjection.md) | Modular service registration (IModule) |

### 🔸 HoneyDrunk.Kernel (Implementations)

| Document | Description |
|----------|-------------|
| [Implementations.md](Implementations.md) | Runtime implementations of all abstractions |

### 🧪 HoneyDrunk.Kernel.Tests

| Document | Description |
|----------|-------------|
| [Testing.md](Testing.md) | Test structure, patterns, and best practices |

---

## 🔷 Quick Start

### Basic Concepts

**Grid Architecture:**
```
┌─────────────────────────────────────────────────┐
│                    Grid                         │
│  ┌─────────────┐    ┌─────────────┐            │
│  │  Studio A   │    │  Studio B   │            │
│  │ ┌─────────┐ │    │ ┌─────────┐ │            │
│  │ │ Node 1  │ │    │ │ Node 3  │ │            │
│  │ └─────────┘ │    │ └─────────┘ │            │
│  │ ┌─────────┐ │    │ ┌─────────┐ │            │
│  │ │ Node 2  │ │    │ │ Node 4  │ │            │
│  │ └─────────┘ │    │ └─────────┘ │            │
│  └─────────────┘    └─────────────┘            │
└─────────────────────────────────────────────────┘
```

**Context Hierarchy:**
```
GridContext (per-operation, flows across Nodes)
    ↓
NodeContext (per-process, static Node identity)
    ↓
OperationContext (per-unit-of-work, timing & outcome)
```

**Configuration Hierarchy:**
```
Global → Studio → Node → Tenant → Project → Request
(Broadest)                               (Narrowest)
```

### Installation

```bash
# Install abstractions (contracts only)
dotnet add package HoneyDrunk.Kernel.Abstractions

# Install runtime (includes abstractions)
dotnet add package HoneyDrunk.Kernel
```

### Basic Usage

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register Kernel services
builder.Services.AddHoneyDrunkCore(options =>
{
    options.NodeId = "my-node";
    options.StudioId = "my-studio";
    options.Environment = "production";
});

var app = builder.Build();
app.Run();
```

```csharp
// Using Grid Context
public class OrderService(IGridContext gridContext, ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        logger.LogInformation(
            "Processing order {OrderId} with correlation {CorrelationId}",
            order.Id,
            gridContext.CorrelationId);
        
        // Create child context for downstream call
        var childContext = gridContext.CreateChildContext("payment-node");
        await _paymentService.ChargeAsync(order, childContext);
    }
}
```

---

## 🔷 Design Philosophy

### Core Principles

1. **Make decisions once** - Kernel is the grammar all Nodes speak
2. **Small surface, strong contracts** - Prefer stable interfaces over frameworks
3. **Observability-ready** - Expose hooks; avoid hard dependencies on specific telemetry stacks
4. **Security-first** - Prepare for Vault integration; never hardcode secrets
5. **Test-friendly** - All abstractions support deterministic testing

### Why These Abstractions?

**Strongly-typed Identity (NodeId, CorrelationId, etc.):**
- Compile-time validation
- Prevents typos and invalid formats
- Efficient value semantics with record structs

**Context Hierarchy (Grid/Node/Operation):**
- Clear separation of concerns
- Per-operation context flows across boundaries
- Per-process context provides static identity
- Per-operation wrapper tracks timing/outcome

**Hierarchical Configuration:**
- Environment-specific overrides (Global → Studio → Node)
- Tenant/Project isolation for multi-tenancy
- Request-level overrides for experimentation

**Explicit Secrets Management:**
- Separation from regular config
- Composite fallback (env vars → Vault → Key Vault)
- Rotation-ready design

---

## 📦 Project Structure

```
HoneyDrunk.Kernel/
├── HoneyDrunk.Kernel.Abstractions/    # Contracts (zero dependencies)
│   ├── Agents/                         # Agent execution abstractions
│   ├── Configuration/                  # Hierarchical config
│   ├── Context/                        # Grid/Node/Operation context
│   ├── Diagnostics/                    # Metrics abstractions
│   ├── DI/                            # Module registration
│   ├── Health/                         # Health check contracts
│   ├── Hosting/                        # Node hosting & discovery
│   ├── Identity/                       # Strongly-typed IDs
│   ├── Lifecycle/                      # Startup/shutdown hooks
│   ├── Config/                         # Secrets management
│   └── Telemetry/                      # Observability primitives
│
├── HoneyDrunk.Kernel/                  # Runtime implementations
│   ├── AgentsInterop/                  # Agent serialization
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Context implementations
│   │   └── Mappers/                    # HTTP/Job/Messaging mappers
│   ├── DependencyInjection/           # Service registration
│   ├── Diagnostics/                    # Health/readiness/metrics
│   ├── Health/                         # Composite health checks
│   ├── Hosting/                        # Node lifecycle host
│   ├── Lifecycle/                      # Lifecycle manager
│   ├── Config/                         # Composite secrets source
│   └── Telemetry/                      # Trace enrichment
│
└── HoneyDrunk.Kernel.Tests/           # Unit & integration tests
    ├── Context/                        # Context tests
    └── Identity/                       # Identity validation tests
```

---

## 🔗 Relationships

### Upstream Dependencies

- **HoneyDrunk.Standards** - Analyzers and coding conventions (buildTransitive)
- **Microsoft.Extensions.*** - DI, Logging, Configuration abstractions
- **System.Text.Json** - Serialization

### Downstream Consumers

All other HoneyDrunk libraries depend on Kernel:

- **HoneyDrunk.Data** - Database abstractions
- **HoneyDrunk.Transport** - Messaging infrastructure
- **HoneyDrunk.Web.Rest** - HTTP APIs
- **HoneyDrunk.Auth** - Authentication/authorization
- **HoneyDrunk.Vault** - Secrets management

---

## 📖 Additional Resources

### Official Documentation
- [README.md](../../README.md) - Quick start and overview
- [.github/copilot-instructions.md](../../.github/copilot-instructions.md) - Coding standards

### Related Projects
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions

### External References
- [ULID Spec](https://github.com/ulid/spec) - Universally Unique Lexicographically Sortable Identifier
- [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

---

## 💡 Motto

**"If the Kernel is stable, everything above it can change fearlessly."**

---

*Last Updated: 2025-11-20*  
*Version: 0.2.1*  
*Target Framework: .NET 10.0*

