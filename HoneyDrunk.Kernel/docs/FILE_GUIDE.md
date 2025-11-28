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
| 🏷️ **Identity Registries** | [IdentityRegistries.md](IdentityRegistries.md) | Static well-known values (Nodes, Sectors, Environments, ErrorCodes) |
| 🌐 **Context** | [Context.md](Context.md) | Distributed context propagation (IGridContext, INodeContext, IOperationContext) |
| ⚙️ **Configuration** | [Configuration.md](Configuration.md) | Studio-level configuration access (IStudioConfiguration) |
| 🏢 **Hosting** | [Hosting.md](Hosting.md) | Node hosting and discovery (INodeDescriptor, INodeManifest, IStudioConfiguration) |
| 🤖 **Agents** | [Agents.md](Agents.md) | Agent execution framework (IAgentDescriptor, IAgentExecutionContext, AgentsInterop) |
| 🔄 **Lifecycle** | [Lifecycle.md](Lifecycle.md) | Node lifecycle management (INodeLifecycle, IStartupHook, IShutdownHook, Health/Readiness) |
| 📡 **Telemetry** | [Telemetry.md](Telemetry.md) | Observability primitives (ITelemetryContext, ITraceEnricher, ILogScopeFactory) for application and infrastructure |
| ❤️ **Health** | [Health.md](Health.md) | Service health monitoring (IHealthCheck, HealthStatus) |
| 📈 **Diagnostics** | [Diagnostics.md](Diagnostics.md) | Metrics and diagnostics (IMetricsCollector) |
| 🔌 **DI** | [DependencyInjection.md](DependencyInjection.md) | Modular service registration (IModule) |
| 🚚 **Transport** | [Transport.md](Transport.md) | Context propagation across boundaries (ITransportEnvelopeBinder, HTTP/Message/Job binders) |
| ⚠️ **Errors** | [Errors.md](Errors.md) | Exception hierarchy and error handling (HoneyDrunkException, ErrorCode, IErrorClassifier) |

### 🔸 HoneyDrunk.Kernel (Implementations)

| Document | Description |
|----------|-------------|
| [Implementations.md](Implementations.md) | Runtime implementations of all abstractions |
| [Bootstrapping.md](Bootstrapping.md) | Unified Node initialization with AddHoneyDrunkGrid() |
| [OpenTelemetry.md](OpenTelemetry.md) | Distributed tracing with GridActivitySource and Activity API (infrastructure-facing) |

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
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register Grid services for this Node
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "my-node";
    options.StudioId = "my-studio";
    options.Environment = "production";
    options.Version = "1.0.0";
});

// Register additional Kernel services
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

var app = builder.Build();

// Grid context middleware (early in pipeline)
app.UseGridContext();

// Optional: Fail fast if services are misconfigured
app.Services.ValidateHoneyDrunkServices();

app.MapControllers();
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
        var childContext = gridContext.CreateChildContext("payment-service");
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

**Runtime Uses Plain Strings:**
- GridContext, NodeContext, OperationContext use `string` properties for performance
- No value object overhead in hot paths
- Direct serialization to HTTP headers, message properties, logs

**Context Hierarchy (Grid/Node/Operation):**
- Clear separation of concerns
- Per-operation context flows across boundaries
- Per-process context provides static identity
- Per-operation wrapper tracks timing/outcome

**Studio-Level Configuration:**
- Studio identity (`IStudioConfiguration`)
- Environment-aware settings
- Integration with external configuration providers

---

## 📦 Project Structure

```
HoneyDrunk.Kernel/
├── HoneyDrunk.Kernel.Abstractions/    # Contracts (zero dependencies)
│   ├── Agents/                         # Agent execution abstractions
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Grid/Node/Operation context
│   ├── Diagnostics/                    # Metrics abstractions
│   ├── DI/                             # Module registration
│   ├── Errors/                         # Exception hierarchy
│   ├── Health/                         # Health check contracts
│   ├── Hosting/                        # Node hosting & discovery
│   ├── Identity/                       # Strongly-typed IDs
│   ├── IdentityRegistries/             # Static well-known values
│   ├── Lifecycle/                      # Startup/shutdown hooks
│   ├── Telemetry/                      # Observability primitives
│   └── Transport/                      # Context propagation
│
├── HoneyDrunk.Kernel/                  # Runtime implementations
│   ├── AgentsInterop/                  # Agent serialization
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Context implementations
│   │   └── Mappers/                    # HTTP/Job/Messaging mappers
│   ├── DependencyInjection/            # Service registration
│   ├── Diagnostics/                    # Health/readiness/metrics
│   ├── Errors/                         # Error classification
│   ├── Health/                         # Composite health checks
│   ├── Hosting/                        # Node lifecycle host
│   ├── Lifecycle/                      # Lifecycle manager
│   └── Telemetry/                      # Trace enrichment, GridActivitySource
│
└── HoneyDrunk.Kernel.Tests/            # Unit & integration tests
    ├── Context/                        # Context tests
    ├── Diagnostics/                    # Diagnostics tests
    ├── Identity/                       # Identity validation tests
    └── Lifecycle/                      # Lifecycle tests
```

---

## 🔗 Relationships

### Upstream Dependencies

- **HoneyDrunk.Standards** - Analyzers and coding conventions (buildTransitive)
- **Microsoft.Extensions.*** - DI, Logging, Configuration abstractions
- **System.Text.Json** - Serialization
- **Ulid** - ULID generation for IDs

### Downstream Consumers

**Intended downstream consumers** include:

- **HoneyDrunk.Data** - Database abstractions
- **HoneyDrunk.Transport** - Messaging infrastructure
- **HoneyDrunk.Web.Rest** - HTTP APIs
- **HoneyDrunk.Auth** - Authentication/authorization
- **HoneyDrunk.Vault** - Secrets management

**Design Note:** Not all consumers exist yet. As the Grid evolves, new Nodes and libraries will depend on Kernel for Grid primitives.

---

## 📖 Additional Resources

### Official Documentation
- [README.md](../README.md) - Quick start and overview
- [.github/copilot-instructions.md](../.github/copilot-instructions.md) - Coding standards

### Related Projects
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions

### External References
- [ULID Spec](https://github.com/ulid/spec) - Universally Unique Lexicographically Sortable Identifier
- [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [System.Diagnostics.Activity](https://docs.microsoft.com/dotnet/api/system.diagnostics.activity) - .NET distributed tracing

---

## 💡 Motto

**"If the Kernel is stable, everything above it can change fearlessly."**

---

*Last Updated: 2025-11-28*  
*Version: 0.3.0*  
*Target Framework: .NET 10.0*

