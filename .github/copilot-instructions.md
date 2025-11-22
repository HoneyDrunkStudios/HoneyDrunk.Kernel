# HoneyDrunk.Kernel Repository Guidelines

## Project Overview

This repository contains the **semantic OS layer** for **HoneyDrunk.OS** ("the Hive").  
It defines:
- Context propagation primitives (Grid, Node, Operation contexts)
- Lifecycle orchestration (startup/shutdown hooks, health/readiness)
- Configuration management (hierarchical scoping, Vault integration)
- Agent interoperability (serialization, execution contexts)
- Telemetry integration (OpenTelemetry enrichment, log scoping)
- Identity primitives (strongly-typed, validated IDs)
- Repo-wide standards wiring (via **HoneyDrunk.Standards**)
- CI packaging for internal feeds

This is a **.NET 10.0** solution composed of:
- `HoneyDrunk.Kernel.Abstractions` (contracts-only)
- `HoneyDrunk.Kernel` (runtime implementations)
- `HoneyDrunk.Kernel.Tests` (separate test project)

**Version:** 0.2.0 (Major refactor as semantic OS layer)

---

## Technology Stack

- **Framework:** .NET 10.0  
- **Language:** C#  
- **Project Types:** Class Libraries (+ xUnit test project)  
- **Features Enabled:**  
  - Implicit Usings  
  - Nullable Reference Types  
  - Primary Constructors  
- **Key Dependencies:**
  - `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.0)
  - `Microsoft.Extensions.Configuration.Abstractions` (10.0.0)
  - `Microsoft.Extensions.Hosting.Abstractions` (10.0.0)
  - `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
  - `Ulid` (1.4.1)

---

## Coding Standards

### C# Conventions

- Follow Microsoft C# conventions plus **HoneyDrunk.Standards** analyzers (buildTransitive).  
- Nullable enabled everywhere; avoid `!` suppression unless justified.  
- Favor **primary constructors** for concise, immutable design.  
- **PascalCase** for public types/members; **camelCase** for locals/parameters.  
- Private fields only when state is required; prefer constructor-injected, readonly dependencies.  
- Keep interfaces minimal and composable; avoid "god" interfaces.

### Code Organization

- **No `/src` or `/tests` folders.** Projects live at repo root:  
  - `HoneyDrunk.Kernel.Abstractions/`  
  - `HoneyDrunk.Kernel/`  
  - `HoneyDrunk.Kernel.Tests/`
- Place repo-level configuration files at root (`.editorconfig`, `.gitattributes`, `Directory.Build.props`, `Directory.Build.targets`, `NuGet.config`).
- Keep implementations thin; heavy behavior belongs in downstream nodes (Transport, Data, Web.Rest).
- Organize by domain:
  - `Context/` - GridContext, NodeContext, OperationContext, mappers
  - `Lifecycle/` - Startup/shutdown hooks, lifecycle management
  - `Configuration/` - Configuration scoping, validation
  - `Telemetry/` - Trace enrichment, log scoping
  - `Identity/` - Strongly-typed ID primitives (CorrelationId, NodeId, etc.)
  - `Agents/` or `AgentsInterop/` - Agent execution contexts, serialization
  - `Health/` - Health check primitives
  - `Diagnostics/` - Metrics, validation, contributors
  - `DependencyInjection/` - Service registration extensions
  - `Hosting/` - Node lifecycle hosting

### Documentation

- XML docs required for all public APIs in `Abstractions`.  
- `README.md` must reflect current responsibilities, layout, and build instructions.  
- Update documentation when changing or extending public contracts.
- Major architecture changes documented in `docs/` folder.

---

## Architecture Principles (v0.2.0)

### What Kernel IS

- **Context Propagation Layer**: Three-tier context model (Grid → Node → Operation)
- **Lifecycle Orchestrator**: Manages Node startup, health, readiness, and shutdown
- **Configuration Foundation**: Hierarchical scoping (Studio → Node → Operation)
- **Agent Integration**: Serialization and context for LLM/automation interop
- **Telemetry Hooks**: OpenTelemetry enrichment without hard dependencies
- **Identity Grammar**: Strongly-typed, validated ID primitives
- **Service Discovery Primitives**: Node descriptors, capabilities, manifests

### What Kernel IS NOT

- **NOT a BCL wrapper**: Removed thin wrappers (IClock, IIdGenerator, ILogSink)
  - Use BCL directly: `DateTime.UtcNow`, `Guid.NewGuid()`, `ILogger<T>`
  - Kernel focuses on **Grid-specific abstractions**, not general utilities
- **NOT a framework**: Kernel provides contracts and orchestration, not business logic
- **NOT protocol-specific**: HTTP/gRPC/messaging mappers exist but aren't required

### Context Model

**Three-Tier Architecture:**

1. **GridContext** (`IGridContext`)
   - Flows across Node boundaries (distributed tracing)
   - Properties: `CorrelationId`, `CausationId`, `NodeId`, `StudioId`, `Environment`, `Baggage`, `Cancellation`
   - Propagated via HTTP headers, message properties, job metadata
   - Mappers: `HttpContextMapper`, `MessagingContextMapper`, `JobContextMapper`

2. **NodeContext** (`INodeContext`)
   - Static, Node-scoped metadata
   - Properties: `Descriptor`, `LifecycleStage`, `StartedAt`, `InstanceId`, `HostName`
   - Registered as singleton

3. **OperationContext** (`IOperationContext`)
   - Scoped per logical operation (request, job, message)
   - Links GridContext + NodeContext
   - Properties: `Grid`, `Node`, `RunId`, `StartedAt`, `Tags`
   - Registered as scoped

### Removed Primitives (v0.2.0)

These were **removed** in favor of BCL:
- ❌ `IClock` / `ISystemClock` → Use `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- ❌ `IIdGenerator` → Use `Guid.NewGuid()` or `Ulid.NewUlid()`
- ❌ `ILogSink` → Use `ILogger<T>` from `Microsoft.Extensions.Logging`

**Reasoning:** Kernel should own **Grid-specific semantics**, not general-purpose utilities.

---

## Build and Testing

### Building the Solution

```bash
dotnet restore
dotnet build -c Release
```

- Targets **.NET 10.0**.
- Warnings are treated as errors (enforced via props/Standards).

### Testing

```bash
dotnet test HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj -c Release --no-build
```

- Tests live only in `HoneyDrunk.Kernel.Tests` (no test code in runtime libraries).  
- Use BCL directly for deterministic time/IDs in tests (no more `IClock`/`IIdGenerator` mocks).
- Prefer **xUnit** + **FluentAssertions**.  
- Keep tests fast, isolated, and repeatable.
- Test classes should mirror implementation structure (e.g., `GridContextTests`, `NodeLifecycleManagerTests`).

---

## File Management

### Ignore Patterns

- Respect `.gitignore` (no `bin/`, `obj/`, user files, or secrets).  
- Never commit environment files or tokens.

### File Naming

- C# files: **PascalCase** matching the public type.  
- Config and YAML: clear, purpose-driven names.
- Test files: `{TypeName}Tests.cs` (e.g., `GridContextTests.cs`)

---

## Contribution Guidelines

### Making Changes

- Keep PRs small and focused.  
- Avoid breaking public contracts in `Abstractions` without discussion.  
- Update docs and samples with any code changes.  
- Run build and test locally before pushing.
- Consider migration impact for downstream Nodes.

### Code Reviews

- All changes require review.  
- Analyzer compliance via **HoneyDrunk.Standards** is mandatory.  
- Verify new primitives belong in **Kernel**, not downstream nodes.
- Context propagation changes require extra scrutiny.

### Commit Messages

- Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`.  
- Use present tense, concise first lines (<50 chars).  

---

## Special Considerations

### Kernel Philosophy

- **Make decisions once.** Kernel is the grammar all Nodes speak.  
- **Small surface, strong contracts.** Prefer stable interfaces over frameworks.  
- **Observability-ready.** Expose hooks; avoid hard dependencies on specific telemetry stacks.  
- **Security-first.** Prepare for Vault integration; never hardcode secrets.
- **Context is king.** Everything flows through GridContext → NodeContext → OperationContext.
- **Grid-native, not BCL wrapper.** Focus on distributed system primitives.

### Compatibility

- Support all modern IDEs: Visual Studio, VS Code, Rider.  
- Cross-platform (Windows, macOS, Linux) by default.
- Async-first: Context flows through `async`/`await` boundaries.

### Key Interfaces to Know

**Context:**
- `IGridContext`, `IGridContextAccessor` - Distributed context
- `INodeContext` - Static Node metadata
- `IOperationContext` - Scoped operation tracking

**Lifecycle:**
- `IStartupHook`, `IShutdownHook` - Lifecycle extension points
- `IHealthContributor`, `IReadinessContributor` - Health aggregation
- `INodeLifecycle` - Lifecycle state management

**Configuration:**
- `IConfigScope` - Hierarchical configuration access
- `IStudioConfiguration` - Studio-level settings
- `ISecretsSource` - Vault integration abstraction

**Identity:**
- `CorrelationId`, `NodeId`, `TenantId`, `ProjectId`, `RunId` - Strongly-typed IDs

**Telemetry:**
- `ITraceEnricher` - OpenTelemetry trace enrichment
- `ILogScopeFactory` - Structured logging scopes
- `ITelemetryContext` - Telemetry correlation

**Agents:**
- `IAgentExecutionContext` - Scoped context for LLM agents
- `IAgentDescriptor`, `IAgentCapability` - Agent metadata

**Hosting:**
- `INodeDescriptor`, `INodeCapability` - Node metadata for service discovery
- `INodeManifest`, `INodeManifestSource` - Deployment manifests

---

## CI/CD (GitHub Actions)

- **Triggers:** `push`, `pull_request`, and `tags` (`v*`)  
- **Steps:** build → test → pack → publish  
- **Secrets:**  
  - `HD_FEED_URL`  
  - `HD_FEED_USER`  
  - `HD_FEED_TOKEN`  
- Required checks on `main`: **Build**, **Test**  
- Output packages:  
  - `HoneyDrunk.Kernel.Abstractions` (v0.2.0)
  - `HoneyDrunk.Kernel` (v0.2.0)

---

## Migration Notes (v0.1.x → v0.2.0)

**Breaking Changes:**
- Removed `IClock`, `IIdGenerator`, `ILogSink` - use BCL equivalents
- Added three-tier context model (Grid/Node/Operation)
- Configuration now scoped (Studio → Node → Operation)
- Telemetry requires enrichers instead of direct logging

**See package release notes and migration guides in individual package READMEs.**
