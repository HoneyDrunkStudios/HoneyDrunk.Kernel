# HoneyDrunk.Kernel

[![Validate PR](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **The Semantic OS Layer for HoneyDrunk.OS** - Context propagation, lifecycle orchestration, and Grid primitives that power every Node across the Hive.

## 📦 What Is This?

HoneyDrunk.Kernel is the **foundational runtime layer** of HoneyDrunk.OS ("the Hive"). It's not just contracts—it's the **semantic OS** that Nodes, agents, and services use to communicate, coordinate, and observe themselves across the Grid.

### Core Responsibilities

- ✅ **Context Propagation** - Grid, Node, and Operation context flows through async boundaries
- ✅ **Lifecycle Orchestration** - Startup hooks, health/readiness monitoring, graceful shutdown
- ✅ **Configuration Management** - Hierarchical scoping, Vault integration, strongly-typed keys
- ✅ **Agent Interop** - Serialization and scoped context access for LLMs and automation
- ✅ **Telemetry Integration** - OpenTelemetry-ready tracing, enrichment, and log correlation
- ✅ **Identity Primitives** - Validated, strongly-typed IDs (NodeId, TenantId, CorrelationId)
- ✅ **Health & Readiness** - Contributor-based aggregation for Kubernetes probes

**Signal Quote:** *"Where everything begins."*

---

## 🚀 Quick Start

### Installation

```xml
<ItemGroup>
  <!-- Abstractions (contracts only) -->
  <PackageReference Include="HoneyDrunk.Kernel.Abstractions" Version="0.2.0" />
  
  <!-- Runtime implementations -->
  <PackageReference Include="HoneyDrunk.Kernel" Version="0.2.0" />
</ItemGroup>
```

### Minimal Node Setup

```csharp
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Define your Node
var nodeDescriptor = new NodeDescriptor
{
    NodeId = "payment-node",
    Version = "1.0.0",
    Name = "Payment Processing Node",
    Sector = "commerce",
    Cluster = "payments-cluster"
};

// Register Kernel with validation
builder.Services.AddHoneyDrunkCoreNode(nodeDescriptor);

var app = builder.Build();

// Validate services before starting
app.Services.ValidateHoneyDrunkServices();

app.Run();
```

---

## 🎯 Key Features (v0.2.0)

### 🌐 Three-Tier Context Model

**GridContext** (Distributed) → **NodeContext** (Static) → **OperationContext** (Scoped)

```csharp
// GridContext: Flows across Node boundaries
public interface IGridContext
{
    string CorrelationId { get; }      // Tracks related operations
    string? CausationId { get; }        // Tracks cause-effect chains
    string NodeId { get; }              // Current Node ID
    string StudioId { get; }            // Studio/tenant ID
    string Environment { get; }         // dev, staging, prod
    IReadOnlyDictionary<string, string> Baggage { get; } // Propagated metadata
    CancellationToken Cancellation { get; } // Cancellation signal
}
```

**See [FILEGUIDE.md](HoneyDrunk.Kernel/docs/FILE_GUIDE.md) for comprehensive architecture documentation.**

---

## 📖 Documentation

### Package Documentation
- **[HoneyDrunk.Kernel.Abstractions README](HoneyDrunk.Kernel/HoneyDrunk.Kernel.Abstractions/README.md)** - Contracts/abstractions package
- **[HoneyDrunk.Kernel README](HoneyDrunk.Kernel/HoneyDrunk.Kernel/README.md)** - Runtime implementations package

### Architecture & Guides
- **[FILE_GUIDE.md](HoneyDrunk.Kernel/docs/FILE_GUIDE.md)** - Comprehensive file structure and architecture reference
- **[Identity Guide](HoneyDrunk.Kernel/docs/Identity.md)** - Strongly-typed identifiers
- **[Context Guide](HoneyDrunk.Kernel/docs/Context.md)** - Context propagation patterns
- **[Lifecycle Guide](HoneyDrunk.Kernel/docs/Lifecycle.md)** - Lifecycle orchestration
- **[Telemetry Guide](HoneyDrunk.Kernel/docs/Telemetry.md)** - Observability integration
- **[Testing Guide](HoneyDrunk.Kernel/docs/Testing.md)** - Test patterns and best practices

### Standards
- **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - Coding standards and conventions

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [FILEGUIDE](FILEGUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)

</div>
