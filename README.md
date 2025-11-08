# HoneyDrunk.Kernel

[![Validate PR](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/actions/workflows/validate-pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Foundational primitives for HoneyDrunk.OS** - The bedrock that powers every Node, service, and agent across the Grid.

## 📦 What Is This?

HoneyDrunk.Kernel is the **primordial layer** of HoneyDrunk.OS ("the Hive"). It defines shared primitives that make the ecosystem coherent and interoperable:

- ✅ **Dependency Injection** - Service registration and lifetime scoping
- ✅ **Configuration** - Unified secrets management (environment, Vault, composite)
- ✅ **Context Propagation** - Correlation/causation tracking across async boundaries
- ✅ **Diagnostics** - Health checks and metrics abstractions
- ✅ **Time & ID Abstractions** - Deterministic, testable primitives
- ✅ **Framework Integration** - Extends Microsoft.Extensions, doesn't replace it

**Signal Quote:** *"Where everything begins."*

---

## 🚀 Quick Start

### Installation

```xml
<ItemGroup>
  <!-- Abstractions (contracts only) -->
  <PackageReference Include="HoneyDrunk.Kernel.Abstractions" Version="0.1.0" />
  
  <!-- Runtime implementations -->
  <PackageReference Include="HoneyDrunk.Kernel" Version="0.1.0" />
</ItemGroup>
```

### Register Kernel Services

```csharp
using HoneyDrunk.Kernel.DI;

var builder = WebApplication.CreateBuilder(args);

// Register Kernel defaults (Clock, IdGenerator, Context, Metrics)
builder.Services.AddKernelDefaults();

var app = builder.Build();
app.Run();
```

---

## 🎯 Features

### 🔍 Core Primitives

| Component | Purpose | Key Types |
|-----------|---------|-----------|
| **Context Propagation** | Correlation/causation tracking | `IKernelContext` |
| **Time Abstractions** | Deterministic time for tests | `IClock`, `SystemClock` |
| **ID Generation** | Unique identifiers (ULID) | `IIdGenerator`, `UlidGenerator` |
| **Secrets Management** | Vault + environment integration | `ISecretsSource`, `CompositeSecretsSource` |
| **Health Checks** | Composite health monitoring | `IHealthCheck`, `CompositeHealthCheck` |
| **Metrics** | Observability abstraction | `IMetricsCollector` (no-op by default) |

### 🔗 Framework Integration

HoneyDrunk.Kernel **extends** rather than replaces Microsoft.Extensions:

| Microsoft.Extensions | Kernel's Role |
|---------------------|---------------|
| `ILogger<T>` | Used directly; no wrapper needed |
| `IConfiguration` | Used directly; Kernel adds `ISecretsSource` |
| `IHostedService` | Used directly for background services |
| `IServiceCollection` | Extended via `AddKernelDefaults()` |

### 🚫 Intentionally Out of Scope

Kernel stays **minimal**. Heavy behavior belongs downstream:

| Feature | Recommended Location |
|---------|---------------------|
| Metrics backends (OpenTelemetry, App Insights) | Service-level registration |
| Resilience (retry, circuit breaker) | `HoneyDrunk.Transport` |
| Validation (FluentValidation) | Service-level or `HoneyDrunk.Data` |
| Result<T> monads | Service-level or `HoneyDrunk.Common` |
| HTTP clients | `HoneyDrunk.Web.Rest` |
| Database abstractions | `HoneyDrunk.Data` |
| Authentication/Authorization | `HoneyDrunk.Auth` |

**Why?** Predictability > Cleverness. Simplicity scales.

---

## 📖 Usage Examples

### Context Propagation

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

### Deterministic Time (Testable)

```csharp
using HoneyDrunk.Kernel.Abstractions.Time;

public class EventStore(IClock clock)
{
    public Event CreateEvent(string data)
    {
        return new Event
        {
            Data = data,
            Timestamp = clock.UtcNow  // Mockable in tests!
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

**Note:** Kernel provides a no-op `IMetricsCollector` by default. Register a real backend (OpenTelemetry, Application Insights) in your service's startup.

### Composite Health Checks

```csharp
using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Health;

var healthChecks = new IHealthCheck[]
{
    new DatabaseHealthCheck(),
    new CacheHealthCheck(),
    new ExternalApiHealthCheck()
};

var composite = new CompositeHealthCheck(healthChecks);
var status = await composite.CheckAsync();
// Returns worst status: Unhealthy > Degraded > Healthy
```

---

## 🧪 Testing & Validation

### Writing Deterministic Tests

Kernel abstractions enable **repeatable** tests:

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
- ✅ Always inject `IClock` instead of using `DateTime.UtcNow`
- ✅ Use `IIdGenerator` for correlation/causation IDs
- ✅ Mock `IMetricsCollector` to verify metrics are recorded
- ✅ Use `IKernelContext` with known correlation IDs for tracing validation

### Local Development

```bash
git clone https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel
cd HoneyDrunk.Kernel/HoneyDrunk.Kernel

dotnet restore
dotnet build
dotnet test HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj
```

---

## 🛠️ Configuration

### Customization via DI

```csharp
// Replace default implementations
builder.Services.AddSingleton<IClock, CustomClock>();
builder.Services.AddSingleton<IIdGenerator, GuidGenerator>();
builder.Services.AddSingleton<IMetricsCollector, OpenTelemetryMetricsCollector>();
```

### Secrets Management

```csharp
using HoneyDrunk.Kernel.Abstractions.Config;
using HoneyDrunk.Kernel.Config.Secrets;

// Composite source: try environment first, then Vault
var secrets = new CompositeSecretsSource(new ISecretsSource[]
{
    new EnvironmentSecretsSource(),
    new VaultSecretsSource(vaultClient)
});

if (secrets.TryGetSecret("DatabasePassword", out var password))
{
    // Use password
}
```

---

## 🧱 Architecture

### Repository Layout

```
HoneyDrunk.Kernel/
 ├── HoneyDrunk.Kernel.Abstractions/    # Contracts & interfaces
 ├── HoneyDrunk.Kernel/                 # Runtime implementations
 ├── HoneyDrunk.Kernel.Tests/           # Test project
 ├── HoneyDrunk.Kernel.sln
 ├── .editorconfig
 └── .github/workflows/
     ├── validate-pr.yml
     └── publish.yml
```

### Design Philosophy

- **Predictability > Cleverness** – Simplicity scales
- **Replaceable without regret** – Contracts, not frameworks
- **Observable by default** – Every operation emits measurable signals
- **Secure by design** – Vault integration from the start
- **Portable** – Works in APIs, background services, agent runtimes

### Relationships

**Upstream Dependencies:**
- HoneyDrunk.Standards (analyzers, conventions)
- HoneyDrunk.Build (CI/CD tooling)

**Downstream Consumers:**
- HoneyDrunk.Data (database abstractions)
- HoneyDrunk.Transport (messaging, resilience)
- HoneyDrunk.Web.Rest (HTTP APIs)
- HoneyDrunk.Auth (authentication/authorization)
- HoneyDrunk.Vault (secrets management)

---

## ⚙️ Build & Release

### CI/CD Integration

The package is validated and published automatically:

```yaml
# Validate on PR
- push → build + test
- pull_request → validate formatting and analyzers

# Publish on tag
- tag v* → build + test + pack + publish to NuGet
```

### Release Workflow

```bash
# Tag a release
git tag v0.1.0
git push origin v0.1.0

# GitHub Actions automatically:
# 1. Builds solution
# 2. Runs tests
# 3. Packs both packages
# 4. Publishes to NuGet.org
# 5. Creates GitHub Release
```

---

## 📋 Testing Policy

- All tests live in `HoneyDrunk.Kernel.Tests` — **none** in runtime projects
- Shared fixtures will later come from `HoneyDrunk.Testing`
- Tests **must** use `IClock` and `IIdGenerator` for deterministic runs
- CI gate: build fails if tests fail; coverage threshold optional

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Read [.github/copilot-instructions.md](.github/copilot-instructions.md) for coding standards
2. Open an issue for discussion before major changes
3. Ensure all tests pass locally
4. Update documentation for new features

### Development Workflow

```bash
# Restore dependencies
dotnet restore HoneyDrunk.Kernel/HoneyDrunk.Kernel.sln

# Build with warnings as errors
dotnet build HoneyDrunk.Kernel/HoneyDrunk.Kernel.sln -c Release /p:TreatWarningsAsErrors=true

# Run tests
dotnet test HoneyDrunk.Kernel/HoneyDrunk.Kernel.Tests/HoneyDrunk.Kernel.Tests.csproj

# Pack for local testing
dotnet pack HoneyDrunk.Kernel/HoneyDrunk.Kernel.Abstractions/HoneyDrunk.Kernel.Abstractions.csproj -c Release -o ./artifacts
dotnet pack HoneyDrunk.Kernel/HoneyDrunk.Kernel/HoneyDrunk.Kernel.csproj -c Release -o ./artifacts
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🐝 About HoneyDrunk Studios

HoneyDrunk.Kernel is part of the **Hive** ecosystem - a collection of tools, libraries, and standards for building high-quality .NET applications.

**Other Projects:**
- 🚀 [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Build-transitive analyzers and conventions
- 🚧 HoneyDrunk.Data *(coming soon)* - Database abstractions
- 🚧 HoneyDrunk.Transport *(coming soon)* - Messaging and resilience

---

## 📞 Support

- **Questions:** Open a [discussion](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/discussions)
- **Bugs:** File an [issue](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
- **Feature Requests:** Open an [issue](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues) with the `enhancement` label

---

## 🧃 Motto

**"If the Kernel is stable, everything above it can change fearlessly."**

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [NuGet: Abstractions](https://www.nuget.org/packages/HoneyDrunk.Kernel.Abstractions) • [NuGet: Kernel](https://www.nuget.org/packages/HoneyDrunk.Kernel) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)

</div>
