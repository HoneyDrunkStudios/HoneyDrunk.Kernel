# 🏢 Hosting - Node Hosting and Discovery

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

Hosting abstractions define how Nodes describe themselves, advertise capabilities, discover dependencies, and integrate into the Grid. This enables dynamic service discovery, capability-based routing, and runtime validation.

**Location:** `HoneyDrunk.Kernel.Abstractions/Hosting/`

**Key Concepts:**
- **Node Manifest** - Declarative contract describing Node identity and dependencies
- **Node Descriptor** - Runtime metadata combining manifest and execution state
- **Node Capability** - Discoverable features/APIs a Node provides
- **Studio Configuration** - Environment-wide settings shared across Nodes
- **Grid Options** - Runtime configuration for Grid participation

---

## INodeManifest.cs

### What it is
Declarative contract describing a Node's identity, capabilities, dependencies, and configuration requirements.

### Real-world analogy
Like a `package.json` or `pom.xml` - declares what the Node is, what it needs, and what it provides.

### Properties

```csharp
public interface INodeManifest
{
    string NodeId { get; }                                    // Unique Node identifier
    string Version { get; }                                   // Semantic version
    string Description { get; }                               // Human-readable purpose
    IReadOnlyList<string> Capabilities { get; }               // Features this Node provides
    IReadOnlyList<string> Dependencies { get; }               // Required Nodes or capabilities
    IReadOnlyDictionary<string, string> ConfigurationSchema { get; } // Config requirements
    IReadOnlyDictionary<string, string> Tags { get; }         // Metadata for routing/filtering
    IReadOnlyList<string> HealthCheckEndpoints { get; }       // Health check URLs
    string? RequiredDependencyStage { get; }                  // Min lifecycle stage for deps
}
```

### Usage Example

```csharp
public class PaymentNodeManifest : INodeManifest
{
    public string NodeId => "payment-node";
    public string Version => "2.1.0";
    public string Description => "Processes payment transactions via Stripe and PayPal";
    
    public IReadOnlyList<string> Capabilities => new[]
    {
        "payment-processing",
        "refund-handling",
        "webhook-notifications"
    };
    
    public IReadOnlyList<string> Dependencies => new[]
    {
        "database-node",      // Node dependency
        "auth-gateway",       // Node dependency
        "email-notification"  // Capability dependency
    };
    
    public IReadOnlyDictionary<string, string> ConfigurationSchema => new Dictionary<string, string>
    {
        ["Stripe:ApiKey"] = "required,secret",
        ["PayPal:ClientId"] = "required,secret",
        ["PayPal:Secret"] = "required,secret",
        ["Database:ConnectionString"] = "required,secret",
        ["MaxRetryAttempts"] = "optional,int,default:3"
    };
    
    public IReadOnlyDictionary<string, string> Tags => new Dictionary<string, string>
    {
        ["protocol"] = "http",
        ["data-region"] = "us-east",
        ["payment-provider"] = "stripe,paypal"
    };
    
    public IReadOnlyList<string> HealthCheckEndpoints => new[] { "/health", "/ready" };
    
    public string? RequiredDependencyStage => "Running"; // Deps must be running before this Node starts
}
```

### Use Cases
- **Service Discovery** - Other Nodes can discover this Node by capability
- **Dependency Validation** - Validate required Nodes/capabilities exist at startup
- **Configuration Validation** - Ensure required config keys are present
- **Documentation** - Manifest serves as living documentation
- **Deployment** - CI/CD can validate manifest consistency

---

## INodeDescriptor.cs

### What it is
Runtime descriptor combining manifest information with execution state and enriched metadata.

### Real-world analogy
Like a service registry entry - includes both static (manifest) and dynamic (runtime) information.

### Properties

```csharp
public interface INodeDescriptor
{
    string NodeId { get; }
    string Version { get; }
    string Name { get; }
    string Description { get; }
    string? Sector { get; }                              // Logical grouping (e.g., "commerce")
    string? Cluster { get; }                             // Deployment group (e.g., "us-east")
    IReadOnlyList<INodeCapability> Capabilities { get; }
    IReadOnlyList<string> Dependencies { get; }
    IReadOnlyList<string> Slots { get; }                 // Deployment slots (blue/green, A/B)
    IReadOnlyDictionary<string, string> Tags { get; }
    INodeManifest? Manifest { get; }
    bool HasCapability(string capabilityName);
}
```

### Differences from INodeManifest

| Aspect | INodeManifest | INodeDescriptor |
|--------|--------------|-----------------|
| **Purpose** | Declarative contract | Runtime metadata |
| **Capabilities** | List of names | Rich `INodeCapability` objects |
| **State** | Static | Includes runtime state |
| **Sector/Cluster** | Not present | Deployment topology |
| **Slots** | Not present | Deployment slots for rollouts |

### Usage Example

```csharp
public class NodeRegistrationService(INodeDescriptor descriptor, IServiceDiscovery discovery)
{
    public async Task RegisterAsync()
    {
        // Register Node with service discovery
        await discovery.RegisterNodeAsync(new ServiceRegistration
        {
            NodeId = descriptor.NodeId,
            Version = descriptor.Version,
            Sector = descriptor.Sector,
            Cluster = descriptor.Cluster,
            Capabilities = descriptor.Capabilities.Select(c => c.Name).ToList(),
            Tags = descriptor.Tags,
            HealthCheckUrl = $"https://{Environment.MachineName}/health"
        });
    }
    
    public bool CanHandleRequest(string requiredCapability)
    {
        return descriptor.HasCapability(requiredCapability);
    }
}
```

---

## INodeCapability.cs

### What it is
Defines a discoverable capability/feature that a Node provides to the Grid.

### Real-world analogy
Like an API contract or microservice interface - describes what the Node can do and how to invoke it.

### Properties

```csharp
public interface INodeCapability
{
    string Name { get; }                                     // Capability name
    string Description { get; }                               // Purpose description
    string Version { get; }                                   // Capability version (independent of Node)
    string Category { get; }                                  // Domain (e.g., "data", "messaging")
    IReadOnlyList<string> SupportedProtocols { get; }        // "http", "grpc", "message-queue"
    IReadOnlyDictionary<string, string> Endpoints { get; }   // Protocol ? endpoint mapping
    string? InputSchema { get; }                              // JSON Schema, OpenAPI, etc.
    string? OutputSchema { get; }                             // Response schema
    IReadOnlyDictionary<string, string> Metadata { get; }    // Rate limits, SLAs, costs
}
```

### Usage Example

```csharp
public class PaymentProcessingCapability : INodeCapability
{
    public string Name => "payment-processing";
    public string Description => "Process credit card and ACH payments";
    public string Version => "2.0.0";
    public string Category => "commerce";
    
    public IReadOnlyList<string> SupportedProtocols => new[] { "http", "grpc" };
    
    public IReadOnlyDictionary<string, string> Endpoints => new Dictionary<string, string>
    {
        ["http"] = "https://payments.honeycomb.io/api/v2/process",
        ["grpc"] = "grpc://payments.honeycomb.io:50051"
    };
    
    public string? InputSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""amount"": { ""type"": ""number"" },
            ""currency"": { ""type"": ""string"", ""enum"": [""USD"", ""EUR""] },
            ""paymentMethod"": { ""type"": ""string"" }
        },
        ""required"": [""amount"", ""currency"", ""paymentMethod""]
    }";
    
    public string? OutputSchema => @"{
        ""type"": ""object"",
        ""properties"": {
            ""transactionId"": { ""type"": ""string"" },
            ""status"": { ""type"": ""string"", ""enum"": [""success"", ""failed""] }
        }
    }";
    
    public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>
    {
        ["rateLimit"] = "1000/minute",
        ["slaLatencyP95"] = "200ms",
        ["costPerCall"] = "$0.002"
    };
}
```

### Capability Discovery Example

```csharp
public class CapabilityRouter(IEnumerable<INodeDescriptor> nodes)
{
    public INodeDescriptor? FindNodeWithCapability(string capabilityName)
    {
        return nodes.FirstOrDefault(n => n.HasCapability(capabilityName));
    }
    
    public async Task<T> InvokeCapabilityAsync<T>(
        string capabilityName, 
        object request, 
        string preferredProtocol = "http")
    {
        var node = FindNodeWithCapability(capabilityName);
        if (node == null)
            throw new CapabilityNotFoundException(capabilityName);
        
        var capability = node.Capabilities.First(c => c.Name == capabilityName);
        if (!capability.Endpoints.TryGetValue(preferredProtocol, out var endpoint))
            throw new ProtocolNotSupportedException(preferredProtocol);
        
        // Invoke via preferred protocol
        return await InvokeEndpointAsync<T>(endpoint, request);
    }
}
```

---

## IStudioConfiguration.cs

### What it is
Environment-wide configuration shared across all Nodes in a Studio.

### Real-world analogy
Like Kubernetes ConfigMaps/Secrets at the namespace level - shared by all pods in that namespace.

### Properties

```csharp
public interface IStudioConfiguration
{
    string StudioId { get; }                              // Studio identifier
    string Environment { get; }                           // Environment name
    string? VaultEndpoint { get; }                        // Vault URL for secrets
    string? ObservabilityEndpoint { get; }                // OpenTelemetry collector
    string? ServiceDiscoveryEndpoint { get; }             // Service registry
    IReadOnlyDictionary<string, bool> FeatureFlags { get; }
    IReadOnlyDictionary<string, string> Tags { get; }
    bool TryGetValue(string key, out string? value);
}
```

### Usage Example

```csharp
public class StudioConfigurationProvider : IStudioConfiguration
{
    public string StudioId => "honeycomb";
    public string Environment => "production";
    public string? VaultEndpoint => "https://vault.honeycomb.io";
    public string? ObservabilityEndpoint => "https://otel-collector.honeycomb.io:4317";
    public string? ServiceDiscoveryEndpoint => "https://consul.honeycomb.io";
    
    public IReadOnlyDictionary<string, bool> FeatureFlags => new Dictionary<string, bool>
    {
        ["EnableNewPaymentFlow"] = true,
        ["EnableAdvancedAnalytics"] = false,
        ["EnableBetaFeatures"] = false
    };
    
    public IReadOnlyDictionary<string, string> Tags => new Dictionary<string, string>
    {
        ["cloud-provider"] = "aws",
        ["region"] = "us-east-1",
        ["cost-center"] = "engineering"
    };
    
    private readonly Dictionary<string, string> _config = new()
    {
        ["SharedDatabase:Host"] = "db.honeycomb.io",
        ["SharedCache:Host"] = "redis.honeycomb.io:6379",
        ["MessageQueue:Host"] = "rabbitmq.honeycomb.io"
    };
    
    public bool TryGetValue(string key, out string? value) => _config.TryGetValue(key, out value);
}
```

### When to use
- Shared infrastructure endpoints
- Studio-wide feature flags
- Authentication/authorization settings
- Observability[← Backend URLs
- Multi-tenant isolation boundaries

---

## GridOptions.cs

### What it is
Runtime configuration options for Grid participation.

### Real-world analogy
Like command-line arguments or environment variables that configure how a service runs.

### Properties

```csharp
public sealed class GridOptions
{
    public string NodeId { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string StudioId { get; set; } = string.Empty;
    public string Environment { get; set; } = "development";
    public Dictionary<string, string> Tags { get; } = [];
}
```

### Usage Example

```csharp
// appsettings.json
{
  "Grid": {
    "NodeId": "payment-node",
    "Version": "2.1.0",
    "StudioId": "honeycomb",
    "Environment": "production",
    "Tags": {
      "deployment-slot": "blue",
      "region": "us-east-1"
    }
  }
}

// Program.cs
builder.Services.Configure<GridOptions>(
    builder.Configuration.GetSection("Grid"));

// Usage
public class NodeInitializer(IOptions<GridOptions> gridOptions)
{
    public void Initialize()
    {
        var options = gridOptions.Value;
        Console.WriteLine($"Starting Node: {options.NodeId} v{options.Version}");
        Console.WriteLine($"Studio: {options.StudioId} ({options.Environment})");
    }
}
```

---

## Complete Hosting Example

```csharp
// 1. Define Node Manifest
public class PaymentNodeManifest : INodeManifest
{
    public string NodeId => "payment-node";
    public string Version => "2.1.0";
    public string Description => "Payment processing service";
    public IReadOnlyList<string> Capabilities => new[] { "payment-processing" };
    public IReadOnlyList<string> Dependencies => new[] { "database-node", "auth-gateway" };
    // ... other properties
}

// 2. Register Node with Grid
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GridOptions>(builder.Configuration.GetSection("Grid"));
builder.Services.AddSingleton<INodeManifest, PaymentNodeManifest>();
builder.Services.AddSingleton<INodeDescriptor>(sp =>
{
    var manifest = sp.GetRequiredService<INodeManifest>();
    var options = sp.GetRequiredService<IOptions<GridOptions>>().Value;
    
    return new NodeDescriptor
    {
        NodeId = manifest.NodeId,
        Version = manifest.Version,
        Name = "Payment Node",
        Description = manifest.Description,
        Sector = "commerce",
        Cluster = "payments-us-east",
        Capabilities = new[] { new PaymentProcessingCapability() },
        Dependencies = manifest.Dependencies,
        Tags = options.Tags,
        Manifest = manifest
    };
});

// 3. Validate manifest at startup
var app = builder.Build();

var manifest = app.Services.GetRequiredService<INodeManifest>();
var validator = new ManifestValidator();
var validationResult = validator.Validate(manifest);
if (!validationResult.IsValid)
{
    throw new InvalidOperationException(
        $"Manifest validation failed: {string.Join(", ", validationResult.Errors)}");
}

// 4. Register with service discovery
var descriptor = app.Services.GetRequiredService<INodeDescriptor>();
var discovery = app.Services.GetRequiredService<IServiceDiscovery>();
await discovery.RegisterNodeAsync(descriptor);

app.Run();
```

---

## Testing Patterns

```csharp
[Fact]
public void NodeManifest_HasRequiredProperties()
{
    // Arrange
    var manifest = new PaymentNodeManifest();
    
    // Assert
    Assert.NotEmpty(manifest.NodeId);
    Assert.NotEmpty(manifest.Version);
    Assert.NotEmpty(manifest.Capabilities);
}

[Fact]
public void NodeDescriptor_HasCapability_ReturnsTrue()
{
    // Arrange
    var descriptor = new NodeDescriptor
    {
        Capabilities = new[] { new PaymentProcessingCapability() }
    };
    
    // Act & Assert
    Assert.True(descriptor.HasCapability("payment-processing"));
    Assert.False(descriptor.HasCapability("email-sending"));
}

[Fact]
public void StudioConfiguration_TryGetValue_ReturnsValue()
{
    // Arrange
    var config = new TestStudioConfiguration();
    
    // Act
    var found = config.TryGetValue("SharedDatabase:Host", out var value);
    
    // Assert
    Assert.True(found);
    Assert.Equal("db.test.io", value);
}
```

---

## Summary

| Component | Purpose | Scope |
|-----------|---------|-------|
| **INodeManifest** | Declarative contract | Node definition |
| **INodeDescriptor** | Runtime metadata | Node + execution state |
| **INodeCapability** | Feature/API contract | Individual capability |
| **IStudioConfiguration** | Environment-wide config | All Nodes in Studio |
| **GridOptions** | Runtime settings | Node instance |

**Key Patterns:**
- Manifests declare dependencies and capabilities
- Descriptors enable runtime discovery and routing
- Capabilities provide rich API metadata
- Studio configuration shares environment settings
- GridOptions configure individual Node instances

---

[← Back to File Guide](FILE_GUIDE.md)

