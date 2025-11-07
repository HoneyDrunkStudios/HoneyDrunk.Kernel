# HoneyDrunk.Kernel

Foundational primitives for HoneyDrunk.OS

## 🧬 Overview

HoneyDrunk.Kernel is the primordial layer of the Grid — the bedrock that powers every Node, service, and agent across HoneyDrunk.OS.
It defines the shared primitives that make the ecosystem coherent and interoperable: dependency injection, configuration, diagnostics, context propagation, and application lifecycles.
Every architectural pattern within the Grid ultimately descends from the Kernel.

**Signal Quote:** "Where everything begins."

**Target Framework:** .NET 10.0

## 🚀 Purpose

The Kernel exists to make architectural decisions once, not repeatedly across services.
It's how small teams achieve large-scale stability — one unified runtime grammar guiding the entire Hive.

## 🧩 Core Responsibilities

| Area | Description |
|------|-------------|
| Dependency Injection (DI) | Central composition layer for service registration and lifetime scoping. |
| Configuration | Unified configuration provider that reads from environment variables, manifests, and Vault. |
| Context Propagation | Lightweight context object for tracing, correlation, and cancellation across async boundaries. |
| Diagnostics | Shared contracts for logging, metrics, and health checks. |
| Time & ID Abstractions | Deterministic abstractions for time and unique identifiers to improve testability. |
| Hosting Lifecycle | Common startup, shutdown, and background worker orchestration primitives. |

## 🧠 Design Philosophy

- **Predictability > Cleverness** – Simplicity scales.
- **Replaceable without regret** – Kernel defines contracts, not frameworks.
- **Observable by default** – Every operation should emit measurable signals.
- **Secure by design** – Vault integration from the start, not bolted on later.
- **Portable** – Works in APIs, background services, or agent runtimes.

## 🔗 Framework Integration

HoneyDrunk.Kernel **extends** rather than replaces Microsoft.Extensions primitives:

| Microsoft.Extensions Feature | Kernel's Role |
|------------------------------|---------------|
| `ILogger<T>` | Used directly; no wrapper needed |
| `IConfiguration` | Used directly; Kernel adds `ISecretsSource` for Vault integration |
| `IHostedService` | Used directly for background services |
| `IServiceCollection` | Extended via `AddKernelDefaults()` |

**What Kernel Adds:**
- `IKernelContext` for correlation/causation propagation across async boundaries
- `IClock` and `IIdGenerator` for deterministic, testable time and ID generation
- `ISecretsSource` for unified secrets management (environment, Vault, composite)
- `IHealthCheck` composition patterns for service health monitoring
- `IMetricsCollector` abstraction (no-op by default; real backends provided by downstream services)

## 🚫 Intentionally Out of Scope

The following belong in **downstream Nodes**, not Kernel:

| Feature | Recommended Location |
|---------|---------------------|
| Metrics backends (OpenTelemetry, Application Insights) | Service-level registration |
| Resilience (retry, circuit breaker, timeout) | `HoneyDrunk.Transport` |
| Validation (`IValidator<T>`, FluentValidation) | Service-level or `HoneyDrunk.Data` |
| Result<T> monads / Railway-oriented programming | Service-level or future `HoneyDrunk.Common` |
| HTTP clients (HttpClientFactory) | `HoneyCore.Web.Rest` |
| Database abstractions (repositories, EF Core) | `HoneyDrunk.Data` |
| Authentication/Authorization | `HoneyDrunk.Auth` |

**Why?** Kernel stays minimal and focused. Heavy behavior belongs at service boundaries where it can be composed and replaced independently.

## 🧱 Repository Layout

```
HoneyDrunk.Kernel/
 ├── HoneyDrunk.Kernel/                 # Runtime library
 ├── HoneyDrunk.Kernel.Abstractions/    # Interfaces & shared contracts
 ├── HoneyDrunk.Kernel.Tests/           # Separate test project
 ├── HoneyDrunk.Kernel.sln
 ├── Directory.Build.props
 ├── Directory.Build.targets
 ├── .editorconfig
 ├── .gitattributes
 ├── .gitignore
 ├── CODEOWNERS
 └── .github/
     └── workflows/
         └── build.yml
```

### Testing Policy

- All tests live in `HoneyDrunk.Kernel.Tests` — none in runtime projects.
- Shared fixtures will later come from `HoneyDrunk.Testing`.
- Tests must use `IClock` and `IIdGenerator` for deterministic runs.
- CI gate: build fails if tests fail; coverage threshold optional.

## 🔗 Relationships

**Upstream:**
- HoneyDrunk.Standards
- HoneyDrunk.Build

**Downstream:**
- HoneyDrunk.Data
- HoneyDrunk.Transport
- HoneyCore.Web.Rest
- HoneyDrunk.Auth
- HoneyDrunk.Vault

## 📖 Quick Start

### Register Kernel Services

```csharp
using HoneyDrunk.Kernel.DI;

var builder = WebApplication.CreateBuilder(args);

// Register Kernel defaults (Clock, IdGenerator, Context, Metrics)
builder.Services.AddKernelDefaults();

var app = builder.Build();
app.Run();
```

### Use Context Propagation

```csharp
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Logging;

public class OrderService(IKernelContext context, ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(string orderId)
    {
        logger.LogInformation(
            "Processing order {OrderId} with CorrelationId: {CorrelationId}", 
            orderId, 
            context.CorrelationId);
        
        // Context flows through async boundaries automatically
        await SaveOrderAsync(orderId, context.Cancellation);
    }
}
```

### Use Deterministic Time

```csharp
using HoneyDrunk.Kernel.Abstractions.Time;

public class EventStore(IClock clock)
{
    public Event CreateEvent(string data)
    {
        return new Event
        {
            Data = data,
            Timestamp = clock.UtcNow  // Mockable in tests
        };
    }
}
```

### Record Metrics

```csharp
using HoneyDrunk.Kernel.Abstractions.Diagnostics;

public class PaymentProcessor(IMetricsCollector metrics)
{
    public async Task ProcessPaymentAsync(decimal amount)
    {
        metrics.RecordCounter("payments.processed", 1, 
            new KeyValuePair<string, object?>("currency", "USD"));
        
        metrics.RecordHistogram("payments.amount", (double)amount);
    }
}
```

**Note:** Kernel provides a no-op `IMetricsCollector` by default. Register a real backend (OpenTelemetry, Application Insights, etc.) in your service's startup.

## 🧪 Local Development

```bash
git clone https://github.com/HoneyDrunkStudios/kernel
cd kernel

dotnet restore
dotnet build
dotnet test HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj
```

This Node consumes private packages from the HoneyDrunk Azure Artifacts feed.
Configure the following secrets or NuGet.config sources:

- `HD_FEED_URL`
- `HD_FEED_USER`
- `HD_FEED_TOKEN`

### Writing Tests

Kernel abstractions enable deterministic testing:

```csharp
using HoneyDrunk.Kernel.Abstractions.Time;
using Xunit;

public class EventStoreTests
{
    [Fact]
    public void CreateEvent_UsesFixedTimestamp()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(fixedTime);
        var store = new EventStore(clock);
        
        // Act
        var evt = store.CreateEvent("test-data");
        
        // Assert
        Assert.Equal(fixedTime, evt.Timestamp);
    }
}

// Simple test double for IClock
public class FixedClock(DateTimeOffset fixedTime) : IClock
{
    public DateTimeOffset UtcNow => fixedTime;
    public long GetTimestamp() => fixedTime.Ticks;
}
```

**Testing Best Practices:**
- Always inject `IClock` instead of using `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- Use `IIdGenerator` for correlation/causation IDs in tests
- Mock `IMetricsCollector` to verify metrics are recorded correctly
- Use `IKernelContext` with known correlation IDs for tracing validation

## ⚙️ Build & Release

- **Workflow:** `HoneyDrunk.Actions` → `publish-nuget.yml`
- **Tag Convention:** `vX.Y.Z` → triggers build, pack, and publish
- **Analyzers:** Enforced automatically via `HoneyDrunk.Standards` (buildTransitive)
- **Output:** Internal Azure Artifacts feed

CI runs on:
- `push` → build + test
- `pull_request` → validate formatting and analyzers
- `tag v*` → publish package

## 🧃 Motto

**"If the Kernel is stable, everything above it can change fearlessly."**
