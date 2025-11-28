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
- ✅ **Configuration Hooks** - Studio and Node level configuration, designed to plug into Vault and other secret backends
- ✅ **Agent Interop** - Serialization and scoped context access for LLMs and automation
- ✅ **Telemetry Integration** - OpenTelemetry-ready tracing, enrichment, and log correlation
- ✅ **Identity Primitives** - Runtime uses plain strings for performance with optional static registries for configuration
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
using HoneyDrunk.Kernel.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register Grid services (string-based configuration for v0.3.0)
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "minimal-node";
    options.StudioId = "demo-studio";
    options.Environment = "development";
    options.Version = "1.0.0";
    options.Tags["region"] = "local";
});

// Register additional Kernel services
builder.Services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
builder.Services.AddScoped<IOperationContextFactory, OperationContextFactory>();

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
// Runtime uses plain strings for performance
public interface IGridContext
{
    string CorrelationId { get; }            // ULID-based request correlation
    string? CausationId { get; }             // Tracks cause-effect chains (parent's CorrelationId)
    string NodeId { get; }                   // Current Node ID
    string StudioId { get; }                 // Studio/tenant ID
    string Environment { get; }              // dev, staging, prod
    DateTimeOffset CreatedAtUtc { get; }     // Context creation timestamp
    IReadOnlyDictionary<string, string> Baggage { get; } // Propagated metadata
    
    IGridContext CreateChildContext(string targetNodeId); // Causality tracking
}
```

**Design Note:** Runtime context uses plain `string` properties for performance. Configuration-time validation uses strongly-typed structs (`NodeId`, `EnvironmentId`) but these are converted to strings at bootstrap.

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
// → X-Correlation-Id, X-Node-Id headers

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

### Architecture & Guides

**Core Documentation:**
- **[FILE_GUIDE.md](docs/FILE_GUIDE.md)** - Complete file structure and architecture reference (START HERE)
- **[Identity.md](docs/Identity.md)** - Strongly-typed identifiers (NodeId, CorrelationId, TenantId, etc.)
- **[Context.md](docs/Context.md)** - Three-tier context propagation (Grid/Node/Operation)
- **[Configuration.md](docs/Configuration.md)** - Studio-level configuration
- **[Hosting.md](docs/Hosting.md)** - Node hosting and discovery

**Advanced Topics:**
- **[Agents.md](docs/Agents.md)** - Agent execution framework + AgentsInterop serialization
- **[Lifecycle.md](docs/Lifecycle.md)** - Lifecycle orchestration (startup/shutdown hooks)
- **[Telemetry.md](docs/Telemetry.md)** - Observability primitives and OpenTelemetry integration
- **[Transport.md](docs/Transport.md)** - Context propagation across boundaries
- **[Errors.md](docs/Errors.md)** - Exception hierarchy and error handling
- **[Implementations.md](docs/Implementations.md)** - Runtime implementation details

**Integration:**
- **[Bootstrapping.md](docs/Bootstrapping.md)** - Unified Node initialization
- **[OpenTelemetry.md](docs/OpenTelemetry.md)** - Distributed tracing with Activity API
- **[Health.md](docs/Health.md)** - Service health monitoring
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
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Grid/Node/Operation context
│   ├── Diagnostics/                    # Metrics abstractions
│   ├── DI/                             # Module registration
│   ├── Errors/                         # Exception hierarchy
│   ├── Health/                         # Health check contracts
│   ├── Hosting/                        # Node hosting & discovery
│   ├── Identity/                       # Strongly-typed IDs
│   ├── Lifecycle/                      # Startup/shutdown hooks
│   ├── Telemetry/                      # Observability primitives
│   └── Transport/                      # Transport abstraction
│
├── HoneyDrunk.Kernel/                  # Runtime implementations
│   ├── AgentsInterop/                  # Agent serialization
│   ├── Configuration/                  # Studio configuration
│   ├── Context/                        # Context implementations
│   │   ├── Mappers/                    # HTTP/Job/Messaging mappers
│   │   └── Middleware/                 # GridContextMiddleware
│   ├── DependencyInjection/            # Service registration
│   ├── Diagnostics/                    # Health/readiness/metrics
│   ├── Errors/                         # DefaultErrorClassifier
│   ├── Health/                         # Composite health checks
│   ├── Hosting/                        # Node lifecycle host
│   ├── Lifecycle/                      # Lifecycle manager
│   ├── Telemetry/                      # Trace enrichment + GridActivitySource
│   └── Transport/                      # Transport binders
│
├── HoneyDrunk.Kernel.Tests/           # Unit & integration tests
└── docs/                               # Complete documentation
```

---

## 🆕 What's New in v0.3.0

### Error Handling
- ✅ `HoneyDrunkException` base with Grid identity propagation
- ✅ Typed exceptions (Validation, NotFound, Security, Concurrency, DependencyFailure)
- ✅ `ErrorCode` with well-known codes + custom taxonomy
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
- ✅ Comprehensive documentation suite
- ✅ New guides: Errors, Transport, Bootstrapping, OpenTelemetry, Testing
- ✅ Updated guides: Agents, Implementations, FILE_GUIDE
- ✅ Complete API reference with examples

### Middleware & Hosting
- ✅ `UseGridContext()` middleware for HTTP request context
- ✅ `AddHoneyDrunkGrid()` unified bootstrapper
- ✅ `ValidateHoneyDrunkServices()` fail-fast validation
- ✅ Kubernetes-ready health/readiness probes

---

## 🔗 Related Projects

**Ecosystem** (designed to build on Kernel):

- **[HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards)** - Analyzers and coding conventions
- **HoneyDrunk.Transport** - Messaging infrastructure *(in development)*
- **HoneyDrunk.Data** - Data persistence conventions *(planned)*
- **HoneyDrunk.Vault** - Secrets management *(planned)*
- **Pulse** - Observability suite *(planned)*

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)

</div>
