# ⚙️ Configuration - Hierarchical Config Management

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- **Hierarchical Configuration**
  - [ConfigScopeType.cs](#configscopetypecs)
  - [IConfigScope.cs](#iconfigscopecs)
  - [ConfigKey.cs](#configkeycs)
  - [ConfigPath.cs](#configpathcs)
  - [NodeRuntimeOptions.cs](#noderuntimeoptionscs)
  - [HoneyDrunkGridOptions.cs](#honeydrunkgridoptionscs)
- **Configuration Implementations**
  - [IStudioConfiguration](#istudioconfiguration)
  - [StudioConfiguration.cs](#studioconfigurationcs)
  - [HoneyDrunkConfigurationExtensions.cs](#honeydrunckconfigurationextensionscs)
- **Secrets Management**
  - [ISecretsSource.cs](#isecretssourcecs)
  - [CompositeSecretsSource.cs](#compositesecretssourcecs)

---

## Overview

Configuration in HoneyDrunk.Kernel follows a hierarchical model where settings can be scoped to different levels with automatic fallback:

```
Global → Studio → Node → Tenant → Project → Request
(Broadest)                                 (Narrowest)
```

**Configuration includes both regular settings and secure secrets management.**

**In v0.3, the hierarchical scope types (`ConfigScope`, `ConfigKey`, `ConfigPath`) do not back a custom `IConfigurationProvider` yet. All runtime configuration still comes from standard .NET `IConfiguration` plus `ISecretsSource`.**

**Location:** `HoneyDrunk.Kernel.Abstractions/Configuration/`

### Configuration System Architecture

The configuration system has three layers working together:

```
┌─────────────────────────────────────────────────────────┐
│ HoneyDrunkGridOptions (Grid Identity)                   │
│ ├─ StudioId: "honeydrunk-studios"                      │
│ ├─ Environment: "production"                            │
│ └─ Cluster, Slot, Tags                                  │
└──────────────┬──────────────────────────────────────────┘
               │ (bootstrap)
               ↓
┌─────────────────────────────────────────────────────────┐
│ IStudioConfiguration (Studio-wide config)               │
│ ├─ Unified access to IConfiguration + ISecretsSource   │
│ ├─ Feature flags, endpoints, tags                       │
│ └─ Uses StudioId/Environment from GridOptions           │
└──────────────┬──────────────────────────────────────────┘
               │ (runtime)
               ↓
┌─────────────────────────────────────────────────────────┐
│ NodeRuntimeOptions (Node behavior)                      │
│ ├─ Telemetry sampling, health intervals                │
│ ├─ Secret rotation settings                             │
│ └─ Should match Environment from GridOptions            │
└─────────────────────────────────────────────────────────┘
```

**Relationships:**
- **HoneyDrunkGridOptions** → "Who am I in the Grid" (StudioId, Environment, Cluster)
- **IStudioConfiguration** → "Where do my values come from" (IConfiguration + ISecretsSource)
- **NodeRuntimeOptions** → "How does this Node behave" (telemetry, health, rotation)

All three share `Environment` as a common identity dimension - ensure they stay aligned!

### Environment Alignment

The `Environment` value appears in four places and **must match** across all layers:

- **HoneyDrunkGridOptions.Environment** → Grid identity (who am I in the Grid)
- **INodeContext.Environment** → Runtime identity (string-based for performance)
- **IGridContext.Environment** → Per-request context (mirrors the same string value)
- **NodeRuntimeOptions.Environment** → Behavior knobs (telemetry, health, rotation)

**At runtime, `IGridContext.Environment` mirrors the same string value for each request. All four should align; `HoneyDrunkGridOptions` remains the source of truth.**

**Validation Pattern:**
```csharp
// At startup, validate alignment
var grid = app.Services.GetRequiredService<HoneyDrunkGridOptions>();
var runtime = app.Services.GetRequiredService<IOptions<NodeRuntimeOptions>>().Value;
if (grid.Environment != runtime.Environment)
{
    throw new InvalidOperationException(
        $"Environment mismatch: Grid={grid.Environment}, Runtime={runtime.Environment}");
}
```

See the [Complete Bootstrap Example](#complete-bootstrap-example) for the full pattern.

[↑ Back to top](#table-of-contents)

---

## ConfigScopeType.cs

Enum defining configuration scope levels:

| Value | Description | Example |
|-------|-------------|---------|
| `Global` | Shared across all Studios and Nodes | Default timeouts, global feature flags |
| `Studio` | Shared within a Studio environment | Studio-specific endpoints, regional settings |
| `Node` | Specific to a Node instance | Node-specific ports, local paths |
| `Tenant` | Tenant-specific configuration | Tenant quotas, custom branding |
| `Project` | Project-specific configuration | Project feature toggles |
| `Request` | Per-request/operation overrides | A/B test variants, canary flags |

[↑ Back to top](#table-of-contents)

---

## IConfigScope.cs

Defines the scope/context for configuration access.

```csharp
public interface IConfigScope
{
    ConfigScopeType ScopeType { get; }
    string? ScopeId { get; }
    IConfigScope? ParentScope { get; }
    string ScopePath { get; }
    IConfigScope CreateChildScope(ConfigScopeType scopeType, string scopeId);
}
```

### Usage Example

```csharp
// Create nested scope
var globalScope = new ConfigScope(ConfigScopeType.Global, null);
var studioScope = globalScope.CreateChildScope(ConfigScopeType.Studio, "honeycomb");
var nodeScope = studioScope.CreateChildScope(ConfigScopeType.Node, "payment-node");

Console.WriteLine(nodeScope.ScopePath); 
// Output: "global/studio:honeycomb/node:payment-node"

// Lookup order: node → studio → global
```

[↑ Back to top](#table-of-contents)

---

## ConfigKey.cs

Strongly-typed configuration key with hierarchical support.

```csharp
var dbKey = new ConfigKey("Database:ConnectionString");
var parentKey = dbKey.Parent; // ConfigKey("Database")
var segments = dbKey.GetSegments(); // ["Database", "ConnectionString"]
var childKey = dbKey.CreateChild("Timeout"); // "Database:ConnectionString:Timeout"
```

[↑ Back to top](#table-of-contents)

---

## ConfigPath.cs

Fully-qualified configuration path combining scope and key.

```csharp
var path = new ConfigPath(studioScope, new ConfigKey("Database:ConnectionString"));
Console.WriteLine(path.FullPath); 
// "studio:honeycomb/Database:ConnectionString"
```

### Hierarchy in Action

Here's how scopes, keys, and paths work together to create multi-tenant configuration:

```csharp
// Build the scope hierarchy
var globalScope = new ConfigScope(ConfigScopeType.Global, null);
var studioScope = globalScope.CreateChildScope(ConfigScopeType.Studio, "honeydrunk-studios");
var nodeScope = studioScope.CreateChildScope(ConfigScopeType.Node, "arcadia");
var tenantScope = nodeScope.CreateChildScope(ConfigScopeType.Tenant, "tenant-123");

// Create a hierarchical configuration key
var key = new ConfigKey("FeatureFlags:Arcadia:EnableNewOnboarding");

// Combine into a fully-qualified path
var path = new ConfigPath(tenantScope, key);
Console.WriteLine(path.FullPath);
// Output: "global/studio:honeydrunk-studios/node:arcadia/tenant:tenant-123/FeatureFlags:Arcadia:EnableNewOnboarding"

// Configuration lookup would search:
// 1. tenant:tenant-123/FeatureFlags:Arcadia:EnableNewOnboarding (most specific)
// 2. node:arcadia/FeatureFlags:Arcadia:EnableNewOnboarding
// 3. studio:honeydrunk-studios/FeatureFlags:Arcadia:EnableNewOnboarding  
// 4. global/FeatureFlags:Arcadia:EnableNewOnboarding (most general)
```

**Note:** This hierarchical scope model is foundational - v0.3 uses `StudioConfiguration` as the initial implementation, with full multi-tenant scoping planned for future versions.

### Implementation Status (v0.3)

**ConfigScope, ConfigKey, and ConfigPath are foundational building blocks only.** In v0.3:

- ✅ **Types defined** - Interfaces and implementations exist in Abstractions
- ❌ **Not yet wired** - StudioConfiguration still uses plain `IConfiguration` + `ISecretsSource`
- 🔮 **Future use** - These will power a multi-tenant configuration provider

Think of these as **identity primitives for configuration** (analogous to `NodeId`, `EnvironmentId` in Identity.md) - they define the grammar for hierarchical config, but aren't yet plugged into a concrete provider.

**Current State:** StudioConfiguration provides Studio-scoped config with secrets fallback  
**Future State:** Full hierarchy (Global → Studio → Node → Tenant → Project → Request) with automatic fallback

[↑ Back to top](#table-of-contents)

---

## NodeRuntimeOptions.cs

Standard runtime configuration options for all Nodes.

```csharp
public sealed record NodeRuntimeOptions
{
    public string Environment { get; set; } = "production";
    public string? Region { get; set; }
    public string? DeploymentRing { get; set; }
    public bool EnableDetailedTelemetry { get; set; } = true;
    public bool EnableDistributedTracing { get; set; } = true;
    public double TelemetrySamplingRate { get; set; } = 1.0;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int ShutdownGracePeriodSeconds { get; set; } = 30;
    public bool EnableSecretRotation { get; set; } = true;
    public int SecretRotationIntervalMinutes { get; set; } = 60;
    public Dictionary<string, string> Tags { get; init; } = [];
}
```

### Usage

```csharp
builder.Services.Configure<NodeRuntimeOptions>(
    builder.Configuration.GetSection("NodeRuntime"));

public class Startup(IOptions<NodeRuntimeOptions> options)
{
    var sampling = options.Value.TelemetrySamplingRate;
}
```

### Connection to INodeContext

`NodeRuntimeOptions.Environment` normally matches `INodeContext.Environment`, which in turn comes from `HoneyDrunkNodeOptions.EnvironmentId`. These three layers work together:

```csharp
public class TelemetryInitializer(
    IOptions<NodeRuntimeOptions> runtime,
    INodeContext node)
{
    public void Initialize()
    {
        // Configure telemetry from NodeRuntimeOptions
        _telemetry.EnableTracing = runtime.Value.EnableDistributedTracing;
        _telemetry.SamplingRate = runtime.Value.TelemetrySamplingRate;
        _telemetry.HealthCheckInterval = TimeSpan.FromSeconds(runtime.Value.HealthCheckIntervalSeconds);

        // Tag telemetry with NodeContext identity
        _telemetry.DefaultTags["node_id"] = node.NodeId;
        _telemetry.DefaultTags["environment"] = runtime.Value.Environment; // Should match node.Environment
        _telemetry.DefaultTags["region"] = runtime.Value.Region ?? "unknown";
        _telemetry.DefaultTags["version"] = node.Version;
        
        // Log startup
        _logger.LogInformation(
            "Telemetry initialized for {NodeId} in {Environment} with {SamplingRate}% sampling",
            node.NodeId,
            runtime.Value.Environment,
            runtime.Value.TelemetrySamplingRate * 100);
    }
}
```

**Relationship:**
- **HoneyDrunkNodeOptions** - Strongly-typed identity at bootstrap (uses `EnvironmentId` struct)
- **INodeContext** - Runtime Node identity (string-based for performance)
- **NodeRuntimeOptions** - Runtime behavior knobs (telemetry, health, secrets)

**Environment Flow:**
`HoneyDrunkNodeOptions.EnvironmentId` → `INodeContext.Environment` (string) → `NodeRuntimeOptions.Environment` (string, validated at startup)

**Recommended Values:**

NodeRuntimeOptions.Environment should match values from the `Environments` registry (`production`, `staging`, `development`, etc.) to stay aligned with `EnvironmentId` rules. See [Identity.md](Identity.md) for well-known environment values and validation.

See [Context.md](Context.md) for `INodeContext` details.

[↑ Back to top](#table-of-contents)

---

## HoneyDrunkGridOptions.cs

### What it is
Root configuration for Grid participation, defining Studio and environment-level settings.

### Real-world analogy
Like your network credentials - defines which organization (Studio) you belong to and which environment you're operating in.

**Location:** `HoneyDrunk.Kernel.Abstractions/Configuration/HoneyDrunkGridOptions.cs`

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `StudioId` | string | ✅ Yes | Studio identifier (multi-studio isolation boundary) |
| `Environment` | string | ✅ Yes | Environment identifier (production, staging, dev-alice) |
| `Cluster` | string? | No | Optional cluster/deployment group hint |
| `Slot` | string? | No | Optional deployment slot (blue, green, canary) for rollout strategies |
| `Tags` | Dictionary<string, string> | No | Arbitrary low-cardinality tags for categorization |

### Usage

```csharp
// appsettings.json
{
  "HoneyDrunk": {
    "Grid": {
      "StudioId": "honeydrunk-studios",
      "Environment": "production",
      "Cluster": "primary-cluster",
      "Slot": "blue",
      "Tags": {
        "team": "platform",
        "region": "us-west-2"
      }
    }
  }
}
```

```csharp
// Manual configuration
var gridOptions = new HoneyDrunkGridOptions
{
    StudioId = "honeydrunk-studios",
    Environment = "production",
    Cluster = "primary-cluster",
    Slot = "blue"
};
gridOptions.Tags["team"] = "platform";
gridOptions.Validate(); // Throws if StudioId or Environment are missing
```

### Validation

The `Validate()` method enforces required fields:

```csharp
public void Validate()
{
    if (string.IsNullOrWhiteSpace(StudioId))
        throw new InvalidOperationException("HoneyDrunkGridOptions.StudioId is required.");
    
    if (string.IsNullOrWhiteSpace(Environment))
        throw new InvalidOperationException("HoneyDrunkGridOptions.Environment is required.");
}
```

### When to use
- Defining Grid-level identity (Studio + Environment)
- Cluster and deployment slot routing
- Blue/green deployments
- Tag-based resource organization

### Why it matters
- **Multi-studio isolation** - Different studios operate independently
- **Environment separation** - Production vs. staging vs. development
- **Deployment strategies** - Slot-based rollouts (blue/green, canary)
- **Resource organization** - Tags for cost allocation, ownership tracking

### Terminology Alignment

**Environment and EnvironmentId:**
- `HoneyDrunkGridOptions.Environment` is a raw string that should align with `EnvironmentId` values from the Identity system
- Convert between them: `new EnvironmentId(gridOptions.Environment)` or use the `Environments` registry for well-known values
- See [Identity.md](Identity.md) for `EnvironmentId` validation rules and well-known values (production, staging, development, etc.)

**StudioId and Context Propagation:**
- `StudioId` from `HoneyDrunkGridOptions` is also propagated via the `X-Studio-Id` HTTP header
- This same value appears in `IGridContext.StudioId` and `INodeContext.StudioId`
- Multi-studio isolation: Each studio operates as an independent Grid workspace
- See [Context.md](Context.md) for context propagation patterns

[↑ Back to top](#table-of-contents)

---

## Configuration Implementations

This section covers the runtime implementations of configuration abstractions.

### IStudioConfiguration

**Interface definition** (contracts the implementation satisfies):

```csharp
public interface IStudioConfiguration
{
    /// <summary>
    /// Gets the Studio identifier.
    /// Example: "honeydrunk-studios", "partner-acme-studio", "internal-tools-studio".
    /// </summary>
    string StudioId { get; }

    /// <summary>
    /// Gets the environment name.
    /// Example: "production", "staging", "development".
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the Vault endpoint URL for secrets management.
    /// </summary>
    string? VaultEndpoint { get; }

    /// <summary>
    /// Gets the observability backend configuration (e.g., OpenTelemetry collector endpoint).
    /// </summary>
    string? ObservabilityEndpoint { get; }

    /// <summary>
    /// Gets the service discovery endpoint (if using external service registry).
    /// </summary>
    string? ServiceDiscoveryEndpoint { get; }

    /// <summary>
    /// Gets Studio-wide feature flags.
    /// </summary>
    IReadOnlyDictionary<string, bool> FeatureFlags { get; }

    /// <summary>
    /// Gets Studio-wide tags/labels.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value if found; otherwise null.</param>
    /// <returns>True if the value was found; otherwise false.</returns>
    bool TryGetValue(string key, out string? value);
}
```

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/IStudioConfiguration.cs` (next to hosting primitives, not under Configuration/)

[↑ Back to top](#table-of-contents)

---

### StudioConfiguration.cs

### What it is
Implementation of `IStudioConfiguration` that combines `IConfiguration` and `ISecretsSource` for unified configuration access.

### Real-world analogy
Like a configuration manager that checks both a config file and a password vault - regular settings come from config, secrets from the vault.

**Location:** `HoneyDrunk.Kernel/Configuration/StudioConfiguration.cs`

### Features
- **Unified access** - Single interface for both config and secrets
- **Fallback logic** - Try configuration first, then secrets
- **Feature flags** - Built-in support for feature toggles
- **Tags** - Studio-level metadata tags
- **Endpoints** - Service discovery, observability, vault endpoints

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `StudioId` | string | Studio identifier |
| `Environment` | string | Environment name |
| `VaultEndpoint` | string? | Vault service URL |
| `ObservabilityEndpoint` | string? | Telemetry/monitoring URL |
| `ServiceDiscoveryEndpoint` | string? | Service registry URL |
| `FeatureFlags` | IReadOnlyDictionary<string, bool> | Feature toggles |
| `Tags` | IReadOnlyDictionary<string, string> | Studio metadata |

### Usage

```csharp
// appsettings.json
{
  "Studio": {
    "VaultEndpoint": "https://vault.honeydrunk.com",
    "ObservabilityEndpoint": "https://metrics.honeydrunk.com",
    "ServiceDiscoveryEndpoint": "https://discovery.honeydrunk.com",
    "FeatureFlags": {
      "EnableNewUI": true,
      "EnableBetaFeatures": false
    },
    "Tags": {
      "owner": "platform-team",
      "cost-center": "engineering"
    }
  }
}
```

```csharp
// Registration
builder.Services.AddHoneyDrunkNodeConfiguration(builder.Configuration);

builder.Services.AddSingleton<IStudioConfiguration>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var secrets = sp.GetService<ISecretsSource>(); // Optional
    var grid = sp.GetRequiredService<HoneyDrunkGridOptions>();
    
    return new StudioConfiguration(
        studioId: grid.StudioId,          // From HoneyDrunkGridOptions (single source of truth)
        environment: grid.Environment,     // From HoneyDrunkGridOptions
        configuration: config,
        secretsSource: secrets);
});

// Usage
public class MyService(IStudioConfiguration studioConfig)
{
    public void Initialize()
    {
        // Check feature flag
        if (studioConfig.FeatureFlags.TryGetValue("EnableNewUI", out var enabled) && enabled)
        {
            EnableNewUI();
        }
        
        // Get configuration value (tries config first, then secrets)
        if (studioConfig.TryGetValue("ApiKey", out var apiKey))
        {
            ConfigureApi(apiKey);
        }
        
        // Access endpoints
        var vaultUrl = studioConfig.VaultEndpoint;
        var metricsUrl = studioConfig.ObservabilityEndpoint;
    }
}
```

**Note:** `StudioConfiguration` should always pull `StudioId` and `Environment` from `HoneyDrunkGridOptions` to maintain a single source of truth. These values are also propagated via `X-Studio-Id` and `X-Environment` headers in Grid context (see [Context.md](Context.md)).

### Configuration Lookup Order

```csharp
public bool TryGetValue(string key, out string? value)
{
    // 1. Try IConfiguration first (regular settings)
    value = _configuration[key];
    if (value != null) return true;
    
    // 2. Try ISecretsSource (sensitive data)
    if (_secretsSource?.TryGetSecret(key, out value) == true)
        return true;
    
    // 3. Not found
    value = null;
    return false;
}
```

### When to use
- Studio-wide configuration management
- Feature flag management
- Service endpoint discovery
- Unified config + secrets access

### Why it matters
- **Single access pattern** - One interface for all configuration
- **Security** - Automatic fallback to secrets for sensitive data
- **Feature management** - Built-in feature flags
- **Service discovery** - Standard endpoints for Grid services
- **Canonical source** - `HoneyDrunkGridOptions` is the only place you set `StudioId`/`Environment`; `StudioConfiguration` reflects those values

[↑ Back to top](#table-of-contents)

---

## HoneyDrunkConfigurationExtensions.cs

### What it is
Extension methods for registering and binding HoneyDrunk configuration in the DI container.

### Real-world analogy
Like a helper that reads your config file and automatically sets up all the configuration objects.

**Location:** `HoneyDrunk.Kernel/Configuration/HoneyDrunkConfigurationExtensions.cs`

### Methods

#### AddHoneyDrunkNodeConfiguration

Binds `HoneyDrunkGridOptions` from `IConfiguration` and registers it in DI.

```csharp
public static IServiceCollection AddHoneyDrunkNodeConfiguration(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = "HoneyDrunk:Grid")
```

**Parameters:**
- `services` - The service collection
- `configuration` - The configuration root
- `sectionName` - Config section name (default: `"HoneyDrunk:Grid"`)

**Returns:** The service collection for chaining

#### AddGridConfiguration

Fluent builder variant for binding `HoneyDrunkGridOptions`.

```csharp
public static IHoneyDrunkBuilder AddGridConfiguration(
    this IHoneyDrunkBuilder builder,
    IConfiguration configuration,
    string sectionName = "HoneyDrunk:Grid")
```

### Usage

```csharp
// Standard approach
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHoneyDrunkNodeConfiguration(
    builder.Configuration,
    sectionName: "HoneyDrunk:Grid"); // Optional, this is the default

var app = builder.Build();
```

```csharp
// Fluent builder approach
builder.Services.AddHoneyDrunk()
    .AddGridConfiguration(builder.Configuration)
    .AddNodeConfiguration(nodeOptions)
    .AddSecretsManagement();
```

### Configuration File Example

```json
{
  "HoneyDrunk": {
    "Grid": {
      "StudioId": "honeydrunk-studios",
      "Environment": "production",
      "Cluster": "us-west-2",
      "Slot": "blue",
      "Tags": {
        "team": "platform",
        "cost-center": "engineering",
        "region": "us-west-2"
      }
    }
  }
}
```

### What It Does

1. **Reads configuration** - Binds JSON to `HoneyDrunkGridOptions`
2. **Validates** - Calls `options.Validate()` to ensure required fields
3. **Registers singleton** - Makes `HoneyDrunkGridOptions` available via DI
4. **Registers IOptions** - Also registers `IOptions<HoneyDrunkGridOptions>` for options pattern

### Registration Details

```csharp
// Both registrations are made:
services.AddSingleton(options);                                    // Direct access
services.AddSingleton<IOptions<HoneyDrunkGridOptions>>(/* ... */); // Options pattern
```

**Important:** Once `AddHoneyDrunkNodeConfiguration` is used, `HoneyDrunkGridOptions` should always be resolved from DI. Avoid manually constructing separate instances - this ensures a single source of truth for `StudioId` and `Environment`, otherwise you risk diverging values between different parts of the app.

### Dependency Injection Usage

```csharp
// Direct injection
public class MyService(HoneyDrunkGridOptions gridOptions)
{
    var studioId = gridOptions.StudioId;
}

// Options pattern injection
public class MyService(IOptions<HoneyDrunkGridOptions> gridOptions)
{
    var studioId = gridOptions.Value.StudioId;
}
```

### Complete Bootstrap Example

Here's how to wire everything together using `HoneyDrunkGridOptions` as the single source of truth:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register Grid configuration (validates and registers HoneyDrunkGridOptions)
builder.Services.AddHoneyDrunkNodeConfiguration(builder.Configuration);

// 2. Register secrets source (optional, composite with fallback)
builder.Services.AddSingleton<ISecretsSource>(sp =>
{
    var sources = new List<ISecretsSource>
    {
        new EnvironmentSecretsSource()
    };
    
    // Add Vault if configured
    var grid = sp.GetRequiredService<HoneyDrunkGridOptions>();
    var vaultEndpoint = builder.Configuration["Vault:Endpoint"];
    if (!string.IsNullOrEmpty(vaultEndpoint))
    {
        sources.Add(new VaultSecretsSource(vaultEndpoint, grid.StudioId));
    }
    
    return new CompositeSecretsSource(sources);
});

// 3. Register StudioConfiguration (wired to GridOptions)
builder.Services.AddSingleton<IStudioConfiguration>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var secrets = sp.GetService<ISecretsSource>();
    var grid = sp.GetRequiredService<HoneyDrunkGridOptions>();
    
    return new StudioConfiguration(
        studioId: grid.StudioId,      // Single source of truth
        environment: grid.Environment, // Single source of truth
        configuration: config,
        secretsSource: secrets);
});

// 4. Register NodeRuntimeOptions (should match GridOptions.Environment)
builder.Services.Configure<NodeRuntimeOptions>(
    builder.Configuration.GetSection("NodeRuntime"));

var app = builder.Build();

// 5. Validate alignment at startup
var grid = app.Services.GetRequiredService<HoneyDrunkGridOptions>();
var runtime = app.Services.GetRequiredService<IOptions<NodeRuntimeOptions>>().Value;
if (grid.Environment != runtime.Environment)
{
    throw new InvalidOperationException(
        $"Environment mismatch: Grid={grid.Environment}, Runtime={runtime.Environment}");
}

app.Run();
```

This pattern ensures:
- `StudioId` and `Environment` have a single source of truth (`HoneyDrunkGridOptions`)
- All configuration layers are aligned
- Secrets and regular config are properly integrated
- Validation happens at startup, not in production

[↑ Back to top](#table-of-contents)

---

## Secrets Management

Configuration system supports secure storage and access of secrets.

**Location:** `HoneyDrunk.Kernel.Abstractions/Configuration/Secrets`

[↑ Back to top](#table-of-contents)

---

## ISecretsSource.cs

### What it is
Interface for secure access to passwords, API keys, and other sensitive configuration data.

### Real-world analogy
Like a password manager - different sources (environment variables, Vault, Key Vault) provide secrets in a standardized way.

### Interface

```csharp
public interface ISecretsSource
{
    bool TryGetSecret(string key, out string? value);
}
```

**Location:** `HoneyDrunk.Kernel.Abstractions/Configuration/ISecretsSource.cs`

### Usage Example

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

### When to use
- Database passwords
- API keys and tokens
- Encryption keys
- OAuth client secrets
- Any sensitive configuration that shouldn't be in plain text

### Why it matters
- **Separation of concerns** - Secrets are handled separately from regular configuration
- **Security** - Enables integration with secure stores (Vault, Azure Key Vault)
- **Flexibility** - Multiple sources with fallback logic
- **Rotation-ready** - Sources can refresh secrets without code changes

**Note on Secret Rotation:**

Secret rotation is **opt-in per Node** and **pattern-based, not Kernel behavior**. Kernel provides the primitives (`ISecretsSource` and `NodeRuntimeOptions.EnableSecretRotation`), but does not run rotation itself. Nodes decide if/when to implement a rotation service.

### Secret Rotation

When `NodeRuntimeOptions.EnableSecretRotation` is `true`, Nodes can periodically re-resolve configuration values backed by `ISecretsSource`, allowing rotation without redeployments:

```csharp
public class SecretRotationService(
    ISecretsSource secrets,
    IOptions<NodeRuntimeOptions> runtime,
    ILogger<SecretRotationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!runtime.Value.EnableSecretRotation)
        {
            logger.LogInformation("Secret rotation disabled");
            return;
        }

        var interval = TimeSpan.FromMinutes(runtime.Value.SecretRotationIntervalMinutes);
        logger.LogInformation("Secret rotation enabled with {Interval} interval", interval);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);
            
            // Re-resolve secrets from source
            if (secrets.TryGetSecret("DatabasePassword", out var newPassword))
            {
                await _connectionPool.UpdatePasswordAsync(newPassword);
                logger.LogInformation("Rotated database password");
            }
        }
    }
}
```

This pattern enables zero-downtime secret rotation when integrated with Vault, Azure Key Vault, or AWS Secrets Manager.

[↑ Back to top](#table-of-contents)

---

## CompositeSecretsSource.cs

### What it is
Implementation that chains multiple secret sources with automatic fallback logic.

### Real-world analogy
Like searching for your keys in multiple places - check coat pocket first, then kitchen counter, then car.

**Location:** `HoneyDrunk.Kernel/Configuration/Secrets/CompositeSecretsSource.cs`

### Features
- **Fallback logic** - Tries sources in order until one succeeds
- **Exception handling** - Skips sources that throw exceptions
- **First-match wins** - Returns value from first successful source

### Usage Example

```csharp
// Register multiple secret sources in priority order
var composite = new CompositeSecretsSource(new ISecretsSource[]
{
    new EnvironmentSecretsSource(),      // Try environment variables first
    new VaultSecretsSource(vaultClient),  // Then HashiCorp Vault
    new KeyVaultSource(keyVaultClient)    // Finally Azure Key Vault
});

// Use composite as single ISecretsSource
if (composite.TryGetSecret("DatabasePassword", out var password))
{
    // Use password from first source that has it
    var connectionString = $"Server=db;Password={password}";
}
```

### Typical Source Priority

| Priority | Source | Use Case |
|----------|--------|----------|
| 1 | Environment Variables | Local development, container overrides |
| 2 | HashiCorp Vault | Production secrets |
| 3 | Azure Key Vault | Cloud-native secrets |
| 4 | AWS Secrets Manager | AWS deployments |

### Behavior

```csharp
// Example: Fallback in action
var composite = new CompositeSecretsSource(new[]
{
    new Source1(), // Returns null for "ApiKey"
    new Source2(), // Throws exception
    new Source3()  // Returns "secret-value" ✓
});

composite.TryGetSecret("ApiKey", out var value);
// Result: value = "secret-value" from Source3
// Source1 returned null → try next
// Source2 threw exception → skip, try next
// Source3 succeeded → return value
```

### Registration in DI

```csharp
builder.Services.AddSingleton<ISecretsSource>(sp =>
{
    var sources = new List<ISecretsSource>
    {
        new EnvironmentSecretsSource()
    };
    
    // Add Vault if configured
    var vaultEndpoint = builder.Configuration["Vault:Endpoint"];
    if (!string.IsNullOrEmpty(vaultEndpoint))
    {
        sources.Add(new VaultSecretsSource(vaultEndpoint));
    }
    
    // Add Key Vault if configured
    var keyVaultUri = builder.Configuration["KeyVault:Uri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        sources.Add(new AzureKeyVaultSource(keyVaultUri));
    }
    
    return new CompositeSecretsSource(sources);
});
```

### Integration with StudioConfiguration

```csharp
public class StudioConfiguration : IStudioConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly ISecretsSource? _secretsSource;
    
    public bool TryGetValue(string key, out string? value)
    {
        // Try configuration first (non-sensitive settings)
        value = _configuration[key];
        if (value != null)
        {
            return true;
        }
        
        // Try secrets source for sensitive data
        if (_secretsSource != null && _secretsSource.TryGetSecret(key, out value))
        {
            return true;
        }
        
        value = null;
        return false;
    }
}
```

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

