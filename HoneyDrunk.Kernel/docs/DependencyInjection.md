# 🔌 Dependency Injection - Modular Service Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IModule.cs](#imodulecs)
- [Module Guidelines](#module-guidelines)
- [Node Composition Example](#node-composition-example)
- [Module Discovery and Ordering](#module-discovery-and-ordering)
- [Summary](#summary)

---

## Overview

Modular service registration for Node features. **Modules let Nodes opt into transport, data, telemetry, and domain features without each Node re-implementing DI wiring by hand.**

**What This Is:** Kernel defines `IModule` as the common shape for "feature packs" that a Node can install. Think of modules as composable building blocks that encapsulate DI registrations for a specific feature area (transport, database, payments, notifications, etc.).

**Location:** `HoneyDrunk.Kernel.Abstractions/DI/`

**Key Concepts:**
- **Feature Packs** - Encapsulate DI registrations for a specific domain or infrastructure concern
- **Composable** - Nodes install only the modules they need
- **Self-Contained** - Modules register their own services without reaching into other modules
- **Declarative** - Node composition stays clean and readable

[↑ Back to top](#table-of-contents)

---

## IModule.cs

**What it is:** Common interface for self-contained service registration modules.

**Location:** `HoneyDrunk.Kernel.Abstractions/DI/IModule.cs`

```csharp
public interface IModule
{
    void ConfigureServices(IServiceCollection services);
}
```

**Design:** Simple contract for "feature pack" style modules. A module knows how to register its own services, health contributors, lifecycle hooks, and telemetry integration without external coordination.

### Basic Usage Example

```csharp
// Infrastructure module (transport)
public class TransportModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITransportPublisher, ServiceBusPublisher>();
        services.AddSingleton<ITransportConsumer, ServiceBusConsumer>();
        services.AddHostedService<MessageConsumerHostedService>();
        
        // Optional: health/telemetry integration
        services.AddSingleton<IHealthContributor, ServiceBusHealthContributor>();
    }
}

// Domain module (business logic)
public class OrdersModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderRepository, SqlOrderRepository>();
        services.AddScoped<IOrderValidator, OrderValidator>();
    }
}

// Telemetry module
public class ObservabilityModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
        services.AddSingleton<ITraceEnricher, CustomBusinessEnricher>();
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Module Guidelines

**What Makes a Good Module:**

A module **should**:
- ✅ Register its own concrete services (implementations, repositories, validators, etc.)
- ✅ Optionally register health contributors, lifecycle hooks, metrics, and trace enrichers
- ✅ Be self-contained and independently testable
- ✅ Accept configuration via constructor parameters or options patterns

A module **should not**:
- ❌ Reach into environment-specific configuration directly if it can take options
- ❌ Mutate other modules' service registrations
- ❌ Assume specific ordering relative to other modules
- ❌ Register services with overlapping responsibilities from other modules

**Philosophy:** Think "feature pack" for transport, data, or domain features. A module should encapsulate everything needed for one feature area without creating coupling to other modules.

### Example: Options-Based Module

```csharp
public class DatabaseModule : IModule
{
    private readonly DatabaseOptions _options;
    
    public DatabaseModule(DatabaseOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Use options instead of reading configuration directly
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(_options.ConnectionString);
            options.EnableSensitiveDataLogging(_options.EnableSensitiveDataLogging);
        });
        
        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Node Composition Example

**In Practice:** Modules are usually registered via helper extensions on `IServiceCollection` so Node composition stays declarative.

### Registration Pattern

```csharp
// Extension method for cleaner registration (optional, but recommended)
public static class ModuleExtensions
{
    public static IServiceCollection AddModule(this IServiceCollection services, IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        module.ConfigureServices(services);
        return services;
    }
}
```

### Complete Node Example

```csharp
// Program.cs - Payment Node
var builder = WebApplication.CreateBuilder(args);

// 1. Register Node with Grid
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "payment-node";
    options.StudioId = "honeycomb";
    options.Version = "1.0.0";
    options.Environment = "production";
});

// 2. Register feature modules
builder.Services.AddModule(new TransportModule());
builder.Services.AddModule(new DatabaseModule(databaseOptions));
builder.Services.AddModule(new PaymentsModule());
builder.Services.AddModule(new NotificationsModule());
builder.Services.AddModule(new ObservabilityModule());

// 3. Build and run
var app = builder.Build();
app.UseGridContext();
app.MapControllers();
await app.RunAsync();
```

**Result:** Clean Node composition without DI soup. Each module encapsulates its feature area, and the Node bootstrap stays declarative and readable.

[↑ Back to top](#table-of-contents)

---

## Module Discovery and Ordering

### Ordering and Dependencies

**`IModule` does not define ordering or dependencies.** Modules are registered in the order you call them, and each module should be self-contained.

**If modules require specific order or shared configuration:**

1. **Shared Options Object** - Register configuration before module calls:
   ```csharp
   // Register shared options first
   builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
   
   // Modules can resolve options from DI
   builder.Services.AddModule(new DatabaseModule(databaseOptions));
   ```

2. **Higher-Level Composite Module** - Create a "Node module" that composes several feature modules:
   ```csharp
   public class PaymentNodeModule : IModule
   {
       public void ConfigureServices(IServiceCollection services)
       {
           // Compose multiple modules in correct order
           new DatabaseModule(dbOptions).ConfigureServices(services);
           new TransportModule().ConfigureServices(services);
           new PaymentsModule().ConfigureServices(services);
       }
   }
   ```

3. **Explicit Ordering** - Just call modules in the order you need:
   ```csharp
   // Database module must register first (provides IDbContext)
   builder.Services.AddModule(new DatabaseModule(options));
   
   // Repositories module depends on IDbContext
   builder.Services.AddModule(new RepositoriesModule());
   ```

**Recommendation:** Keep modules independent whenever possible. If you find yourself needing complex dependency graphs between modules, consider whether you're slicing features at the right level.

[↑ Back to top](#table-of-contents)

---

## Summary

**`IModule` provides a standard shape for composable Node features.**

| Aspect | Guidance |
|--------|----------|
| **Purpose** | Encapsulate DI registrations for a feature area (transport, data, domain) |
| **Scope** | Self-contained feature packs that a Node can opt into |
| **Registration** | Via `AddModule()` extension or direct `ConfigureServices()` calls |
| **Ordering** | No automatic ordering - register in the order you need |
| **Dependencies** | Prefer shared options objects over cross-module coupling |

**Key Patterns:**
- Use modules to organize Node features (transport, database, payments, telemetry)
- Keep modules self-contained and independently testable
- Register modules declaratively in Node bootstrap (`Program.cs`)
- Use options patterns for configuration instead of reading directly
- Compose complex Nodes from simple, focused modules

**Relationship to Kernel:**
- **Kernel** provides the `IModule` contract
- **Nodes** compose modules to build their feature set
- **Modules** encapsulate DI registrations for specific features

**Example Node Composition:**
```
Payment Node = Grid Core + Transport Module + Database Module + Payments Module + Observability Module
```

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

