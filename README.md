# HoneyDrunk.Kernel

[![Validate PR](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **The Semantic OS Layer for HoneyDrunk.OS** - Context propagation, lifecycle orchestration, and Grid primitives that power every Node across the Hive.

## 📦 What Is This?

HoneyDrunk.Kernel is the **foundational runtime layer** of HoneyDrunk.OS ("the Hive"). It's not just contracts—it's the **semantic OS** that Nodes, agents, and services use to communicate, coordinate, and observe themselves across the Grid.

### Core Responsibilities

- ✅ **Context Propagation** - Three-tier context model (Grid → Node → Operation) flows through async boundaries
- ✅ **Lifecycle Orchestration** - Startup hooks, health/readiness monitoring, graceful shutdown
- ✅ **Configuration Management** - Hierarchical scoping (Studio → Node → Tenant), Vault integration
- ✅ **Agent Interop** - Serialization and scoped context access for LLMs and automation
- ✅ **Telemetry Integration** - OpenTelemetry-ready tracing, enrichment, and log correlation
- ✅ **Identity Primitives** - Validated, strongly-typed IDs with static registries (NodeId, SectorId, EnvironmentId, ErrorCode)
- ✅ **Error Handling** - Structured exception hierarchy with Grid identity propagation
- ✅ **Transport Abstraction** - Protocol-agnostic context propagation (HTTP, messaging, jobs)

**Signal Quote:** *"Where everything begins."*

---

## 🚀 Quick Start

### Installation

```bash
dotnet add package HoneyDrunk.Kernel
# Or just the abstractions (contracts only):
dotnet add package HoneyDrunk.Kernel.Abstractions
```

```xml
<ItemGroup>
  <!-- Runtime implementations (recommended) -->
  <PackageReference Include="HoneyDrunk.Kernel" Version="0.3.0" />
  
  <!-- Abstractions only (for libraries) -->
  <PackageReference Include="HoneyDrunk.Kernel.Abstractions" Version="0.3.0" />
</ItemGroup>
```

### Minimal Node Setup (v0.3.0)

```csharp
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Hosting;
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

var builder = WebApplication.CreateBuilder(args);

// Register Kernel with static identity registries (canonical v3 pattern)
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.MinimalNode;      // From static registry
    options.SectorId = Sectors.Core;               // From static registry
    options.EnvironmentId = GridEnvironments.Development; // From static registry
    
    options.Version = "1.0.0";
    options.StudioId = "demo-studio";
    options.Tags["region"] = "local";
});

var app = builder.Build();

// Validate all required services are registered
app.Services.ValidateHoneyDrunkServices();

// Add Grid context middleware for HTTP request tracing
app.UseGridContext();

// Sample endpoint demonstrating context injection
app.MapGet("/", (INodeContext nodeContext, IGridContext gridContext) =>
{
    return Results.Ok(new
    {
        Message = "HoneyDrunk Minimal Node",
        Node = new
        {
            nodeContext.NodeId,
            nodeContext.Version,
            nodeContext.LifecycleStage
        },
        Request = new
        {
            gridContext.CorrelationId,
            gridContext.NodeId
        }
    });
});

app.Run();
```

**See [samples/MinimalNode](samples/MinimalNode/) for a complete working example.**

---

## 🎯 Key Features (v0.3.0)

### 🌐 Three-Tier Context Model

**GridContext** (Distributed) → **NodeContext** (Static) → **OperationContext** (Scoped)

```csharp
// GridContext: Flows across Node boundaries
public interface IGridContext
{
    CorrelationId CorrelationId { get; }    // ULID-based request correlation
    CausationId? CausationId { get; }        // Tracks cause-effect chains
    NodeId NodeId { get; }                   // Current Node ID
    string StudioId { get; }                 // Studio/tenant ID
    EnvironmentId Environment { get; }       // dev, staging, prod
    DateTimeOffset CreatedAtUtc { get; }     // Context creation timestamp
    IReadOnlyDictionary<string, string> Baggage { get; } // Propagated metadata
    
    IGridContext CreateChildContext(NodeId targetNodeId); // Causality tracking
}
```

### 🏷️ Static Identity Registries (NEW in v0.3.0)

Compile-time safe, discoverable identities:

```csharp
// Node registry (54 real nodes from the Grid catalog)
options.NodeId = Nodes.Core.Kernel;
options.NodeId = Nodes.Ops.Pulse;
options.NodeId = Nodes.AI.AgentKit;
options.NodeId = Nodes.Market.Arcadia;

// Sector registry (9 real sectors)
options.SectorId = Sectors.Core;
options.SectorId = Sectors.Ops;
options.SectorId = Sectors.AI;

// Environment registry (7 environments)
options.EnvironmentId = GridEnvironments.Production;
options.EnvironmentId = GridEnvironments.Development;
```

**Benefits:** IntelliSense discovery, compile-time validation, IDE refactoring support, consistent naming.

### ⚠️ Structured Error Handling (NEW in v0.3.0)

Exception hierarchy with Grid identity propagation:

```csharp
// Typed exceptions with ErrorCode
throw new NotFoundException(
    "Order not found",
    ErrorCode.WellKnown.ResourceNotFound);

throw new ValidationException(
    "Invalid email format",
    ErrorCode.WellKnown.ValidationInput);

// All exceptions carry Grid context
catch (HoneyDrunkException ex)
{
    logger.LogError(ex,
        "Operation failed with correlation {CorrelationId}",
        ex.CorrelationId);
}
```

**Error Classification:** Automatic mapping to HTTP status codes via `IErrorClassifier`.

### 🚚 Transport Abstraction (NEW in v0.3.0)

Protocol-agnostic context propagation:

```csharp
// HTTP response binder
httpBinder.Bind(httpContext.Response, gridContext);
// → X-Correlation-ID, X-Node-ID headers

// Message properties binder
messageBinder.Bind(messageProperties, gridContext);
// → RabbitMQ/Azure Service Bus headers

// Job metadata binder
jobBinder.Bind(jobMetadata, gridContext);
// → Hangfire/Quartz job context
```

---

## 📖 Documentation

### Package Documentation
- **[HoneyDrunk.Kernel.Abstractions README](HoneyDrunk.Kernel.Abstractions/README.md)** - Contracts/abstractions package
- **[HoneyDrunk.Kernel README](HoneyDrunk.Kernel/README.md)** - Runtime implementations package
- **[HoneyDrunk.Kernel.Tests README](HoneyDrunk.Kernel.Tests/README.md)** - Test suite documentation [NEW v0.3.0]
- **[MinimalNode Sample](samples/MinimalNode/README.md)** - Complete working example

### Architecture & Guides

**Core Documentation:**
- **[FILE_GUIDE.md](docs/FILE_GUIDE.md)** - Complete file structure and architecture reference (START HERE)
- **[Identity.md](docs/Identity.md)** - Strongly-typed identifiers (NodeId, CorrelationId, TenantId, etc.)
- **[IdentityRegistries.md](docs/IdentityRegistries.md)** - Static well-known values (Nodes, Sectors, Environments) [NEW v0.3.0]
- **[Context.md](docs/Context.md)** - Three-tier context propagation (Grid/Node/Operation)
- **[Configuration.md](docs/Configuration.md)** - Hierarchical configuration management
- **[Hosting.md](docs/Hosting.md)** - Node hosting and discovery

**Advanced Topics:**
- **[Agents.md](docs/Agents.md)** - Agent execution framework + AgentsInterop serialization [UPDATED v0.3.0]
- **[Lifecycle.md](docs/Lifecycle.md)** - Lifecycle orchestration (startup/shutdown hooks)
- **[Telemetry.md](docs/Telemetry.md)** - Observability primitives and OpenTelemetry integration
- **[Transport.md](docs/Transport.md)** - Context propagation across boundaries [NEW v0.3.0]
- **[Errors.md](docs/Errors.md)** - Exception hierarchy and error handling [NEW v0.3.0]
- **[Implementations.md](docs/Implementations.md)** - Runtime implementation details [UPDATED v0.3.0]

**Integration:**
- **[Bootstrapping.md](docs/Bootstrapping.md)** - Unified Node initialization [NEW v0.3.0]
- **[OpenTelemetry.md](docs/OpenTelemetry.md)** - Distributed tracing with Activity API [NEW v0.3.0]
- **[Health.md](docs/Health.md)** - Service health monitoring
- **[Secrets.md](docs/Secrets.md)** - Secure secrets management
- **[Diagnostics.md](docs/Diagnostics.md)** - Metrics and diagnostics
- **[DependencyInjection.md](docs/DependencyInjection.md)** - Modular service registration
- **[Testing.md](docs/Testing.md)** - Test patterns and best practices

### Standards
- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - Coding standards and conventions
- **[.github/instructions/](.github/instructions/)** - Repository guidelines

---

## 🏗️ Project Structure

```
HoneyDrunk.Kernel/
├── HoneyDrunk.Kernel.Abstractions/    # Contracts (zero dependencies)
│   ├── Agents/                         # Agent execution abstractions
│   ├── Configuration/                  # Hierarchical config
│   ├── Context/                        # Grid/Node/Operation context
│   ├── Diagnostics/                    # Metrics abstractions
│   ├── DI/                            # Module registration
│   ├── Errors/                         # Exception hierarchy [NEW v0.3.0]
│   ├── Health/                         # Health check contracts
│   ├── Hosting/                        # Node hosting & discovery
│   ├── Identity/                       # Strongly-typed IDs
│   ├── Lifecycle/                      # Startup/shutdown hooks
│   ├── Config/                         # Secrets management
│   ├── Telemetry/                      # Observability primitives
│   ├── Transport/                      # Transport abstraction [NEW v0.3.0]
│   ├── Nodes.cs                        # Static Node registry [NEW v0.3.0]
│   ├── Sectors.cs                      # Static Sector registry [NEW v0.3.0]
│   └── Environments.cs                 # Static Environment registry [NEW v0.3.0]
│
├── HoneyDrunk.Kernel/                  # Runtime implementations
│   ├── AgentsInterop/                  # Agent serialization [NEW v0.3.0]
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Context implementations
│   │   ├── Mappers/                    # HTTP/Job/Messaging mappers
│   │   └── Middleware/                 # GridContextMiddleware [NEW v0.3.0]
│   ├── DependencyInjection/           # Service registration
│   ├── Diagnostics/                    # Health/readiness/metrics
│   ├── Errors/                         # DefaultErrorClassifier [NEW v0.3.0]
│   ├── Health/                         # Composite health checks
│   ├── Hosting/                        # Node lifecycle host
│   ├── Lifecycle/                      # Lifecycle manager
│   ├── Config/                         # Composite secrets source
│   ├── Telemetry/                      # Trace enrichment + GridActivitySource
│   └── Transport/                      # Transport binders [NEW v0.3.0]
│
├── HoneyDrunk.Kernel.Tests/           # Unit & integration tests (130+ tests)
├── samples/MinimalNode/                # Complete working example [NEW v0.3.0]
└── docs/                               # Complete documentation (18 guides)
```

---

## 🆕 What's New in v0.3.0

### Static Identity Registries
- ✅ `Nodes.*` - 54 real nodes from Grid catalog (Core, Ops, AI, Market, etc.)
- ✅ `Sectors.*` - 9 real sectors with canonical naming
- ✅ `Environments.*` - 7 environments (Production, Staging, Development, etc.)
- ✅ Compile-time validation, IntelliSense discovery, IDE refactoring

### Error Handling
- ✅ `HoneyDrunkException` base with Grid identity propagation
- ✅ 6 typed exceptions (Validation, NotFound, Security, Concurrency, DependencyFailure)
- ✅ `ErrorCode` with 11 well-known codes + custom taxonomy
- ✅ `IErrorClassifier` for automatic HTTP status mapping

### Transport Abstraction
- ✅ `ITransportEnvelopeBinder` for protocol-agnostic context propagation
- ✅ Built-in binders: HTTP, messaging, background jobs
- ✅ GridContext mappers for extracting context from transport envelopes
- ✅ `GridHeaderNames` constants for standard headers

### AgentsInterop
- ✅ `AgentExecutionResult` serialization
- ✅ `GridContextSerializer` with automatic secret filtering
- ✅ `AgentResultSerializer` for cross-process agent execution
- ✅ `AgentContextProjection` for context composition

### Enhanced Documentation
- ✅ 18 comprehensive guides (5 new + 3 major updates)
- ✅ `Errors.md` - Exception hierarchy and error handling
- ✅ `Transport.md` - Context propagation patterns
- ✅ `IdentityRegistries.md` - Static registry usage
- ✅ `Agents.md` - Expanded with AgentsInterop section
- ✅ `Implementations.md` - Complete runtime details
- ✅ `Bootstrapping.md` - Unified Node initialization
- ✅ `OpenTelemetry.md` - Activity API integration

### Middleware & Hosting
- ✅ `UseGridContext()` middleware for HTTP request context
- ✅ `AddHoneyDrunkNode()` unified bootstrapper
- ✅ `ValidateHoneyDrunkServices()` fail-fast validation
- ✅ Kubernetes-ready health/readiness probes

---

## 📊 Statistics (v0.3.0)

- **85 files** documented across Abstractions + Implementations
- **18 documentation guides** (50,000+ words)
- **54 real Nodes** in static registry
- **9 sectors** covering the entire Grid
- **11 well-known error codes**
- **3 transport binders** (HTTP, messaging, jobs)
- **130+ tests** with ~95% coverage

---

## 🔗 Related Projects

- **[HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards)** - Analyzers and coding conventions
- **[HoneyDrunk.Transport](https://github.com/HoneyDrunkStudios/HoneyDrunk.Transport)** - Messaging infrastructure
- **[HoneyDrunk.Data](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data)** - Data persistence conventions
- **[HoneyDrunk.Vault](https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault)** - Secrets management
- **[Pulse](https://github.com/HoneyDrunkStudios/Pulse)** - Observability suite

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)

</div>
