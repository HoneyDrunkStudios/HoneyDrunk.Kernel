# ?? HoneyDrunk.Kernel - Complete File Guide

## Overview

**Think of this library as the foundation stone of a building**

Just like how a building's foundation provides stability and common infrastructure (plumbing, electricity) that every floor uses, this library provides core primitives that every HoneyDrunk service needs without having to reinvent them.

---

## ?? HoneyDrunk.Kernel.Abstractions (Contracts Project)

*The blueprint that defines "what" things should do, not "how" they do it*

### ?? Context (Tracking and Correlation)

*These help you trace requests across services like breadcrumbs in a forest*

#### IKernelContext.cs
- **What it is:** A container for tracking related operations across services
- **Real-world analogy:** Like a package tracking number that follows your shipment through multiple facilities
- **What it does:** Provides correlation/causation IDs and baggage for distributed tracing
- **What it contains:** 
  - `CorrelationId` - Groups all operations from a single user request
  - `CausationId` - Which specific operation triggered this one
  - `Cancellation` - Token to cancel long-running operations
  - `Baggage` - Key-value pairs that travel with the context (like sticky notes on a file folder)
- **How it's used:** Injected as a scoped service; created per-request with auto-generated IDs
- **Why it matters:** When a user clicks a button and 10 services process it, you can trace the entire journey with one ID
- **When to use:** Every service that participates in distributed operations
- **Example:**
  ```csharp
  public class OrderService(IKernelContext context)
  {
      public async Task ProcessAsync()
      {
          _logger.LogInformation("Processing with CorrelationId: {CorrelationId}", 
              context.CorrelationId);
      }
  }
  ```

---

### ? Time (Deterministic Timestamps)

*Abstractions that let you control time in tests*

#### IClock.cs
- **What it is:** A testable way to get the current time
- **Real-world analogy:** Instead of looking at a wall clock (which you can't control), you use a clock you can set to any time for testing
- **What it does:** Provides current UTC time and high-resolution timestamps
- **What it provides:**
  - `UtcNow` - Current UTC timestamp (replaceable in tests)
  - `GetTimestamp()` - High-resolution timer for measuring intervals
- **How it's used:** Injected as a singleton; used anywhere you need timestamps
- **Why it matters:** You can write tests that work reliably by controlling time
- **When to use:** Always use instead of `DateTime.UtcNow` or `DateTimeOffset.UtcNow`
- **Example:**
  ```csharp
  public class EventStore(IClock clock)
  {
      public Event CreateEvent(string data) => new Event
      {
          Data = data,
          Timestamp = clock.UtcNow  // Testable!
      };
  }
  ```

---

### ?? Ids (Unique Identifier Generation)

*Creates unique tracking numbers for everything*

#### IIdGenerator.cs
- **What it is:** Generates globally unique identifiers
- **Real-world analogy:** Like the barcode printer that creates unique codes for every product in a warehouse
- **What it does:** Generates ULIDs (sortable) and GUIDs
- **What it provides:**
  - `NewString()` - Generates a unique string ID (ULID format by default)
  - `NewGuid()` - Generates a standard GUID
- **How it's used:** Injected as a singleton; called whenever you need a unique ID
- **Why it matters:** Every message, request, and transaction needs a unique ID for tracking and debugging
- **When to use:** Creating correlation IDs, entity IDs, message IDs, or any unique identifier
- **Example:**
  ```csharp
  public class MessagePublisher(IIdGenerator idGenerator)
  {
      public Message CreateMessage(string payload) => new Message
      {
          Id = idGenerator.NewString(),  // ULID
          Payload = payload
      };
  }
  ```

---

### ?? Config (Secrets Management)

*Secure access to passwords, API keys, and other sensitive data*

#### ISecretsSource.cs
- **What it is:** Interface for retrieving secrets from secure stores
- **Real-world analogy:** Like a bank vault's access system - you ask for a specific key, and it gives you the secret (or says "not found")
- **What it does:** Abstracts secret retrieval from various sources (Vault, environment variables, Key Vault)
- **What it provides:**
  - `TryGetSecret(key, out value)` - Safely retrieves a secret by name
- **How it's used:** Injected as a singleton; typically wrapped in a composite for fallback logic
- **Why it matters:** Never hardcode passwords; fetch them securely at runtime from Vault or environment variables
- **When to use:** Retrieving connection strings, API keys, certificates, or any sensitive configuration
- **Example:**
  ```csharp
  public class DatabaseConnector(ISecretsSource secrets)
  {
      public string GetConnectionString()
      {
          if (secrets.TryGetSecret("DatabasePassword", out var password))
          {
              return $"Server=db;Password={password}";
          }
          throw new InvalidOperationException("Database password not found");
      }
  }
  ```

---

### ?? Health (Service Health Monitoring)

*Check if your services are alive and healthy*

#### IHealthCheck.cs
- **What it is:** Contract for health check implementations
- **Real-world analogy:** Like a doctor's checkup - "Are you healthy, degraded, or unhealthy?"
- **What it does:** Defines async method to check component health
- **What it provides:**
  - `CheckAsync()` - Returns `HealthStatus` (Healthy, Degraded, Unhealthy)
- **How it's used:** Implement for each component (database, cache, external API); aggregate with `CompositeHealthCheck`
- **Why it matters:** Kubernetes/monitoring systems can automatically restart unhealthy services
- **When to use:** Every critical dependency that could fail (databases, message queues, external APIs)
- **Example:**
  ```csharp
  public class DatabaseHealthCheck(IDbConnection db) : IHealthCheck
  {
      public async Task<HealthStatus> CheckAsync(CancellationToken ct = default)
      {
          try
          {
              await db.ExecuteScalarAsync("SELECT 1", ct);
              return HealthStatus.Healthy;
          }
          catch
          {
              return HealthStatus.Unhealthy;
          }
      }
  }
  ```

#### HealthStatus.cs
- **What it is:** Enum representing health states
- **Real-world analogy:** Green light, yellow light, red light
- **What it does:** Defines three levels of health
- **Values:**
  - `Healthy` - Everything is fine
  - `Degraded` - Working but with issues (e.g., slow database)
  - `Unhealthy` - Critical failure (e.g., database unreachable)
- **How it's used:** Returned by `IHealthCheck.CheckAsync()` implementations
- **Why it matters:** Allows nuanced health reporting (partial degradation vs. total failure)
- **When to use:** Implementing health checks that can detect performance issues, not just failures

---

### ?? Diagnostics (Observability)

*Tools for logging and metrics*

#### IMetricsCollector.cs
- **What it is:** Abstraction for recording application metrics
- **Real-world analogy:** Like a fitness tracker recording your steps, heart rate, and calories
- **What it does:** Provides methods to record counters, histograms, and gauges
- **What it provides:**
  - `RecordCounter()` - Count events (e.g., "orders processed")
  - `RecordHistogram()` - Measure distributions (e.g., response times)
  - `RecordGauge()` - Track current values (e.g., active connections)
- **How it's used:** Injected as a singleton; call at key points to record metrics
- **Why it matters:** Monitor application performance without coupling to specific telemetry systems
- **When to use:** Track business metrics, performance counters, resource usage
- **Example:**
  ```csharp
  public class PaymentProcessor(IMetricsCollector metrics)
  {
      public async Task ProcessAsync(decimal amount)
      {
          metrics.RecordCounter("payments.processed", 1, 
              new KeyValuePair<string, object?>("currency", "USD"));
          metrics.RecordHistogram("payments.amount", (double)amount);
      }
  }
  ```

#### ILogSink.cs
- **What it is:** Custom logging abstraction (optional - most code uses `ILogger<T>`)
- **Real-world analogy:** A specialized logging channel for structured data
- **What it does:** Provides alternative logging mechanism for special scenarios
- **What it provides:** Custom log sinks for special scenarios
- **How it's used:** Implement for specialized logging needs (audit logs, security events)
- **Why it matters:** Separates critical logs from general application logs
- **When to use:** Audit trails, security events, or logs requiring special handling
- **Note:** Most services should use Microsoft's `ILogger<T>` directly
- **Example:**
  ```csharp
  public class AuditLogSink : ILogSink
  {
      public void Log(LogLevel level, string message, params object[] args)
      {
          // Write to dedicated audit log store
      }
  }
  ```

#### LogLevel.cs
- **What it is:** Enum for log severity levels
- **Real-world analogy:** Volume knob for how noisy your logs should be
- **What it does:** Defines hierarchy of log severity
- **Values:** Trace, Debug, Information, Warning, Error, Critical
- **How it's used:** Passed to logging methods to indicate severity
- **Why it matters:** Filter logs by severity in production (only errors/warnings)
- **When to use:** Every log statement to indicate importance

---

### ?? DI (Dependency Injection)

*Modular service registration*

#### IModule.cs
- **What it is:** Interface for self-contained service registration modules
- **Real-world analogy:** Like a plug-in that knows how to install itself
- **What it does:** Encapsulates service registration logic for a feature/domain
- **What it provides:**
  - `ConfigureServices(IServiceCollection)` - Registers all services this module needs
- **How it's used:** Implement in feature assemblies; call during startup
- **Why it matters:** Keeps service registration organized and reusable
- **When to use:** Organizing related services (e.g., all Transport services, all Data services)
- **Example:**
  ```csharp
  public class TransportModule : IModule
  {
      public void ConfigureServices(IServiceCollection services)
      {
          services.AddSingleton<ITransportPublisher, ServiceBusPublisher>();
          services.AddSingleton<ITransportConsumer, ServiceBusConsumer>();
          services.AddHostedService<MessageConsumerHostedService>();
      }
  }
  ```

---

## ?? HoneyDrunk.Kernel (Runtime Implementations)

*The actual working implementations of the contracts*

### ?? Context (Context Implementation)

#### KernelContext.cs
- **What it is:** Default implementation of `IKernelContext`
- **Real-world analogy:** The actual tracking slip with IDs filled in
- **What it does:** Stores correlation ID, causation ID, baggage, and cancellation token
- **How it's used:** Created per-request with auto-generated IDs; injected as scoped service
- **Why it matters:** Provides the runtime container for distributed tracing data
- **When to use:** Automatically injected; rarely instantiated manually except in tests
- **Example:**
  ```csharp
  // In tests:
  var context = new KernelContext(
      correlationId: "test-corr-id",
      causationId: "test-cause-id",
      baggage: new Dictionary<string, string> { ["tenant"] = "test-tenant" },
      cancellation: CancellationToken.None);
  ```

---

### ? Time (Clock Implementation)

#### SystemClock.cs
- **What it is:** Production implementation using real system time
- **Real-world analogy:** The actual wall clock in your office
- **What it does:** Returns real system time via `DateTimeOffset.UtcNow` and `Stopwatch`
- **What it provides:**
  - `UtcNow` returns `DateTimeOffset.UtcNow`
  - `GetTimestamp()` uses `Stopwatch.GetTimestamp()` for high precision
- **How it's used:** Registered as singleton in `AddKernelDefaults()`
- **Why it matters:** Provides production-ready time source
- **When to use:** Production code (injected by default)
- **When to replace:** Tests use `FixedClock` or `AdjustableClock`
- **Example:**
  ```csharp
  // Production code automatically gets SystemClock:
  public class EventStore(IClock clock)
  {
      public Event CreateEvent(string data) => new Event
      {
          Timestamp = clock.UtcNow  // Uses system time in production
      };
  }
  ```

---

### ?? Ids (ID Generator Implementation)

#### UlidGenerator.cs
- **What it is:** Generates ULIDs (Universally Unique Lexicographically Sortable Identifiers)
- **Real-world analogy:** Like a serial number generator that creates IDs in chronological order
- **What it does:** Creates timestamp-prefixed, sortable unique identifiers
- **What it provides:**
  - `NewString()` generates a ULID string (e.g., `01FQXZ8K4TJ9X5B3N2YGF7WDCQ`)
  - `NewGuid()` generates a standard GUID
- **How it's used:** Registered as singleton; thread-safe for concurrent use
- **Why it matters:** ULIDs sort chronologically (better for databases than GUIDs)
- **When to use:** Default implementation; use unless you need custom ID generation
- **Example:**
  ```csharp
  var generator = new UlidGenerator();
  var id1 = generator.NewString(); // "01HQXZ8K4TJ9X5B3N2YGF7WDCQ"
  Thread.Sleep(10);
  var id2 = generator.NewString(); // "01HQXZ8K4TJ9X5B3N2YGF7WDCR" (sorts after id1)
  ```

---

### ?? Config (Secrets Implementation)

#### CompositeSecretsSource.cs
- **What it is:** Chains multiple secret sources with fallback logic
- **Real-world analogy:** "Try the first vault, if not found, try the second vault, then the third..."
- **What it does:** Iterates through multiple `ISecretsSource` implementations until one returns a value
- **How it works:** 
  - Attempts to fetch a secret from each source in order
  - Returns the first match found
  - Returns `false` if no source has the secret
- **How it's used:** Wrap multiple sources in priority order (e.g., environment → Vault → Key Vault)
- **Why it matters:** Enables fallback strategies and gradual migration between secret stores
- **When to use:** When you have multiple secret sources and need prioritization
- **Example:**
  ```csharp
  var composite = new CompositeSecretsSource(new ISecretsSource[]
  {
      new EnvironmentSecretsSource(),     // Try environment variables first
      new VaultSecretsSource(vaultClient),  // Then Vault
      new KeyVaultSource(keyVaultClient)    // Finally Azure Key Vault
  });
  
  if (composite.TryGetSecret("DatabasePassword", out var password))
  {
      // Use password from first source that has it
  }
  ```

---

### ?? Health (Health Check Implementation)

#### CompositeHealthCheck.cs
- **What it is:** Aggregates multiple health checks into one status
- **Real-world analogy:** Like a dashboard showing the worst status across all systems
- **What it does:** Runs all health checks in parallel and returns worst status
- **How it works:**
  - Runs all registered health checks in parallel
  - Returns the worst status: `Unhealthy` > `Degraded` > `Healthy`
- **How it's used:** Wrap all component health checks; expose via health endpoint
- **Why it matters:** Single endpoint to check overall system health
- **When to use:** When you have multiple components to monitor
- **Example:**
  ```csharp
  var composite = new CompositeHealthCheck(new IHealthCheck[]
  {
      new DatabaseHealthCheck(dbConnection),
      new CacheHealthCheck(redisClient),
      new ExternalApiHealthCheck(httpClient)
  });
  
  var status = await composite.CheckAsync();
  // Returns Unhealthy if ANY check is unhealthy
  // Returns Degraded if ANY check is degraded (and none unhealthy)
  // Returns Healthy only if ALL checks are healthy
  ```

---

### ?? Diagnostics (No-Op Implementations)

*Placeholder implementations that do nothing (for when you don't need telemetry)*

#### NoOpMetricsCollector.cs
- **What it is:** Metrics collector that discards all metrics
- **Real-world analogy:** A trash can labeled "metrics" that accepts everything but stores nothing
- **What it does:** Implements `IMetricsCollector` with empty methods
- **How it's used:** Default registration in `AddKernelDefaults()`
- **Why it matters:** Zero overhead when you don't need metrics
- **When to use:** Default registration; replace with OpenTelemetry or Application Insights in production
- **Example:**
  ```csharp
  // Registered by default:
  services.AddKernelDefaults();  // Uses NoOpMetricsCollector
  
  // Replace in production:
  services.AddSingleton<IMetricsCollector, OpenTelemetryCollector>();
  ```

#### NoOpLogSink.cs
- **What it is:** Log sink that discards all logs
- **Real-world analogy:** A black hole for logs
- **What it does:** Implements `ILogSink` with empty methods
- **How it's used:** Placeholder when `ILogSink` is required but not needed
- **Why it matters:** Satisfies interface requirements without overhead
- **When to use:** When you need a log sink but don't want any output
- **Example:**
  ```csharp
  services.AddSingleton<ILogSink, NoOpLogSink>();
  ```

---

### ?? DI (Service Registration)

*Tools to wire everything up in ASP.NET Core*

#### KernelServiceCollectionExtensions.cs
- **What it is:** The "setup wizard" for Kernel services
- **Real-world analogy:** The "easy install" button that configures everything
- **What it does:** Registers all default Kernel implementations in DI container
- **What it provides:**
  - `AddKernelDefaults()` - Registers all default implementations:
    - `IClock` → `SystemClock`
    - `IIdGenerator` → `UlidGenerator`
    - `IMetricsCollector` → `NoOpMetricsCollector`
    - `IKernelContext` → `KernelContext` (scoped, auto-generated IDs)
- **How it's used:** Call once in `Program.cs` or `Startup.cs`
- **Why it matters:** One-line setup for all Kernel primitives
- **When to use:** Every HoneyDrunk service startup
- **Example:**
  ```csharp
  var builder = WebApplication.CreateBuilder(args);
  
  // Register all Kernel defaults:
  builder.Services.AddKernelDefaults();
  
  // Optionally replace any default:
  builder.Services.AddSingleton<IMetricsCollector, OpenTelemetryCollector>();
  ```

---

## ?? HoneyDrunk.Kernel.Tests

*Validation and examples for all Kernel components*

### What to Expect

This project contains:
- **Unit tests** for all implementations (`SystemClock`, `UlidGenerator`, `KernelContext`, etc.)
- **Integration tests** for DI registration and wiring
- **Examples** of how to use Kernel in tests (mocking `IClock`, injecting fake contexts)

### Testing Best Practices

When writing tests that use Kernel:

1. **Time:** Inject `IClock`, never use `DateTime.UtcNow`
   ```csharp
   public class FixedClock(DateTimeOffset time) : IClock
   {
       public DateTimeOffset UtcNow => time;
       public long GetTimestamp() => time.Ticks;
   }
   ```

2. **IDs:** Inject `IIdGenerator` for predictable IDs in tests
   ```csharp
   public class SequentialIdGenerator : IIdGenerator
   {
       private int _counter;
       public string NewString() => $"test-id-{Interlocked.Increment(ref _counter)}";
       public Guid NewGuid() => Guid.NewGuid();
   }
   ```

3. **Context:** Create test contexts with known correlation IDs
   ```csharp
   var context = new KernelContext(
       correlationId: "test-correlation-id",
       causationId: "test-causation-id",
       baggage: new Dictionary<string, string>(),
       cancellation: CancellationToken.None);
   ```

---

## ?? Summary: The Big Picture

### What Problem Does This Solve?

Every application needs:
- Correlation IDs to trace requests across services
- Testable time for reliable tests
- Unique ID generation for tracking
- Health checks for monitoring
- Metrics for observability
- Secure secrets management

Instead of every team implementing these primitives differently, Kernel provides **one consistent set of abstractions** that all HoneyDrunk services share.

---

### How to Explain It to a Non-Technical Person:

> "Imagine you're building 100 houses. Instead of every builder making their own plumbing, electrical, and foundation designs, we create one standard set that everyone uses. This library is that standard foundation for software - it provides the basic utilities (time, IDs, tracking) that every service needs, so developers can focus on unique features instead of reinventing the wheel."

---

### How to Explain It to a Developer:

> "Kernel is the primordial layer of HoneyDrunk.OS. It provides stable, testable primitives for:
> - Context propagation (correlation/causation tracking)
> - Deterministic time (`IClock` instead of `DateTime.UtcNow`)
> - ID generation (ULIDs for sortable, unique identifiers)
> - Secrets management (Vault + environment variable fallback)
> - Health checks (composite status aggregation)
> - Metrics (abstractions for OpenTelemetry/App Insights)
> 
> It extends Microsoft.Extensions (doesn't replace) and stays minimal by design. Heavy features like resilience, validation, and HTTP live downstream in Transport, Data, and Web.Rest."

---

## ?? Quick Reference

### Core Concepts

| Concept | Abstraction | Implementation | Purpose |
|---------|-------------|----------------|---------|
| **Context** | `IKernelContext` | `KernelContext` | Correlation/causation tracking |
| **Time** | `IClock` | `SystemClock` | Testable timestamps |
| **IDs** | `IIdGenerator` | `UlidGenerator` | Unique identifier generation |
| **Secrets** | `ISecretsSource` | `CompositeSecretsSource` | Secure secrets retrieval |
| **Health** | `IHealthCheck` | `CompositeHealthCheck` | Service health aggregation |
| **Metrics** | `IMetricsCollector` | `NoOpMetricsCollector` | Observability hooks |

---

### Request Flow with Context


```
User Request → Web API
               →
           Generate CorrelationId (IIdGenerator)
               →
           Create KernelContext (IKernelContext)
               →
           Service A (reads CorrelationId)
               →
           Publish Message (propagates CorrelationId)
               →
           Service B (reads same CorrelationId, sets CausationId)
               →
           All logs tagged with CorrelationId
               →
           Trace entire request across services
```

---

### Typical Service Startup


```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register Kernel defaults (Clock, IDs, Context, Metrics)
builder.Services.AddKernelDefaults();

// 2. Replace no-op metrics with real implementation
builder.Services.AddSingleton<IMetricsCollector, OpenTelemetryCollector>();

// 3. Add secrets source
builder.Services.AddSingleton<ISecretsSource>(provider =>
{
    return new CompositeSecretsSource(new ISecretsSource[]
    {
        new EnvironmentSecretsSource(),
        new VaultSecretsSource(provider.GetRequiredService<IVaultClient>())
    });
});

// 4. Register health checks
builder.Services.AddSingleton<IHealthCheck>(provider =>
{
    return new CompositeHealthCheck(new IHealthCheck[]
    {
        new DatabaseHealthCheck(provider),
        new CacheHealthCheck(provider)
    });
});

var app = builder.Build();
app.Run();
```

---

### Dependency Injection Best Practices

**Lifetimes:**
- `IClock` ? **Singleton** (stateless, one instance)
- `IIdGenerator` ? **Singleton** (stateless, thread-safe)
- `IKernelContext` ? **Scoped** (per-request, auto-generated IDs)
- `IMetricsCollector` ? **Singleton** (stateless, thread-safe)
- `ISecretsSource` ? **Singleton** (cached, refresh on schedule)

**Injection Pattern:**
```csharp
public class OrderService(
    IKernelContext context,
    IClock clock,
    IIdGenerator idGenerator,
    IMetricsCollector metrics,
    ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        logger.LogInformation(
            "Processing order {OrderId} at {Timestamp} with {CorrelationId}",
            order.Id,
            clock.UtcNow,
            context.CorrelationId);
        
        metrics.RecordCounter("orders.processed", 1);
        
        // Business logic...
    }
}
```

---

### Testing Patterns

**Test with Fixed Time:**
```csharp
[Fact]
public void EventStore_CreatesEventWithFixedTimestamp()
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
```

**Test with Known IDs:**
```csharp
[Fact]
public void MessagePublisher_UsesCorrelationIdFromContext()
{
    // Arrange
    var context = new KernelContext("known-correlation-id", null, [], default);
    var publisher = new MessagePublisher(context);
    
    // Act
    var message = publisher.CreateMessage("payload");
    
    // Assert
    Assert.Equal("known-correlation-id", message.CorrelationId);
}
```

**Test Health Checks:**
```csharp
[Fact]
public async Task CompositeHealthCheck_ReturnsWorstStatus()
{
    // Arrange
    var checks = new IHealthCheck[]
    {
        new FakeHealthCheck(HealthStatus.Healthy),
        new FakeHealthCheck(HealthStatus.Degraded),
        new FakeHealthCheck(HealthStatus.Healthy)
    };
    var composite = new CompositeHealthCheck(checks);
    
    // Act
    var status = await composite.CheckAsync();
    
    // Assert: Worst status wins
    Assert.Equal(HealthStatus.Degraded, status);
}
```

---

## ?? Relationships

### Upstream (Dependencies)

- **HoneyDrunk.Standards** - Analyzers and coding conventions (buildTransitive)
- **Microsoft.Extensions.DependencyInjection** - DI abstractions
- **Microsoft.Extensions.Logging** - Logging abstractions

### Downstream (Consumers)

All other HoneyDrunk libraries depend on Kernel:

- **HoneyDrunk.Data** - Database abstractions (uses `IClock`, `IKernelContext`)
- **HoneyDrunk.Transport** - Messaging infrastructure (uses `IKernelContext` for correlation)
- **HoneyDrunk.Web.Rest** - HTTP APIs (propagates `IKernelContext` via middleware)
- **HoneyDrunk.Auth** - Authentication/authorization (tracks via `IKernelContext`)
- **HoneyDrunk.Vault** - Secrets management (implements `ISecretsSource`)

---

## ?? Design Philosophy

### Why These Abstractions?

**IClock instead of DateTime.UtcNow:**
- Tests become deterministic
- No more "flaky tests" that fail at midnight
- Measure precise intervals without thread sleep

**IIdGenerator instead of Guid.NewGuid():**
- ULIDs sort chronologically (better for databases)
- Consistent ID format across all services
- Testable with predictable sequences

**IKernelContext instead of ActivityContext:**
- Simplified API for correlation tracking
- Baggage for custom context propagation
- Works in non-HTTP scenarios (background jobs, messaging)

**ISecretsSource instead of IConfiguration:**
- Explicit separation: config vs. secrets
- Composite pattern for multi-source fallback
- Vault-first design for security

---

## ?? NuGet Packages

### HoneyDrunk.Kernel.Abstractions
- **Contents:** All interfaces and contracts
- **Dependencies:** None (zero dependencies!)
- **When to use:** If you only need contracts (e.g., building a library)

### HoneyDrunk.Kernel
- **Contents:** Runtime implementations
- **Dependencies:** `HoneyDrunk.Kernel.Abstractions`, `Microsoft.Extensions.*`
- **When to use:** Always (unless you're implementing custom runtimes)

---

## ? Performance Considerations

### Efficient Defaults

- **UlidGenerator** - Lock-free, thread-safe, faster than GUID generation
- **SystemClock** - Direct syscall, no allocations
- **KernelContext** - Immutable by default, safe to share across threads
- **NoOpMetricsCollector** - Zero overhead when telemetry is disabled

### Telemetry Overhead

When replacing `NoOpMetricsCollector` with real implementations:
- Use sampling for high-frequency operations
- Batch metric writes
- Consider async fire-and-forget for non-critical metrics

---

## ?? Future Enhancements

**Planned for v0.2.0:**
- `IKernelContextAccessor` - Async-local context storage
- `IClockProvider` - Time zone aware clock
- `IHealthCheckRegistry` - Dynamic health check registration
- `ISecretsCache` - In-memory caching for secrets

**Planned for v1.0.0:**
- Stable API surface (semver guarantees)
- Expanded test helpers in `HoneyDrunk.Testing`
- OpenTelemetry integration package

---

## ?? Additional Resources

### Official Documentation
- [README.md](README.md) - Quick start and overview
- [.github/copilot-instructions.md](.github/copilot-instructions.md) - Coding standards

### Related Projects
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions
- [HoneyDrunk.Transport](https://github.com/HoneyDrunkStudios/HoneyDrunk.Transport) - Messaging abstractions

### External References
- [ULID Spec](https://github.com/ulid/spec) - Universally Unique Lexicographically Sortable Identifier
- [Microsoft.Extensions.DependencyInjection Docs](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

---

## ?? Motto

**"If the Kernel is stable, everything above it can change fearlessly."**

---

*Last Updated: 2025-01-11*  
*Version: 0.1.0*  
*Target Framework: .NET 10.0*
