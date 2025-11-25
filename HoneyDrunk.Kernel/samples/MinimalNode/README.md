# Minimal Node Sample

This is a minimal HoneyDrunk Node demonstrating the unified bootstrapper and basic Grid integration.

## What It Demonstrates

- **Unified Bootstrapper** - `AddHoneyDrunkNode()` registers all core services
- **Static Identity Registries** - Using `Nodes.Core.Minimal`, `Sectors.Core`, `Environments.Development`
- **Service Validation** - `ValidateHoneyDrunkServices()` ensures proper registration
- **HTTP Middleware** - `UseGridContext()` adds automatic context propagation
- **Context Injection** - `INodeContext` and `IGridContext` injected into endpoints
- **Configuration-Driven** - Version and StudioId from config (not hardcoded)

## Running

```bash
cd samples/MinimalNode
dotnet run
```

## Testing

```bash
# Get node info with automatic correlation ID
curl http://localhost:5000/

# Response includes Node identity and request context:
{
  "message": "HoneyDrunk Minimal Node",
  "node": {
    "nodeId": "HoneyDrunk.Core.MinimalNode",
    "version": "1.0.0",
    "studioId": "demo-studio",
    "environment": "development",
    "lifecycleStage": "Initializing",
    "startedAtUtc": "2025-01-11T10:30:00Z"
  },
  "request": {
    "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "causationId": null,
    "nodeId": "HoneyDrunk.Core.MinimalNode",
    "createdAtUtc": "2025-01-11T10:30:15Z"
  }
}

# Health check
curl http://localhost:5000/health
```

## What's Registered

The `AddHoneyDrunkNode()` call automatically registers:

- `INodeContext` - Process-scoped Node identity
- `INodeDescriptor` - Node descriptor with capabilities
- `IGridContextAccessor` - Ambient Grid context accessor
- `IOperationContextAccessor` - Ambient operation context accessor
- `IOperationContextFactory` - Factory for creating operation contexts
- `IErrorClassifier` - Error classification for transport mapping
- `ITransportEnvelopeBinder` (3x) - HTTP, Message, Job envelope binders
- Scoped `IGridContext` factory

## Middleware

The `UseGridContext()` middleware:

- Extracts `X-Correlation-ID`, `X-Causation-ID`, `X-Studio-ID`, `X-Baggage-*` headers
- Creates a `GridContext` for the request
- Creates an `OperationContext` to track timing and outcome
- Echoes correlation and node IDs to response headers
- Cleans up ambient context after request completes

## Code Walkthrough

```csharp
// 1. Register Node with static identity registries (canonical v3 pattern)
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.MinimalNode;      // From static registry
    options.SectorId = Sectors.Core;               // From static registry
    options.EnvironmentId = GridEnvironments.Development; // From static registry
    
    // Version from config/assembly (avoid hardcoding)
    options.Version = builder.Configuration["Version"] ?? "1.0.0";
    // ... other options
});

// 2. Validate services (throws if required services missing)
app.Services.ValidateHoneyDrunkServices();

// 3. Add middleware for HTTP context mapping
app.UseGridContext();

// 4. Inject contexts into endpoints
app.MapGet("/", (INodeContext nodeContext, IGridContext gridContext) =>
{
    // nodeContext: static Node identity
    // gridContext: per-request Grid context with correlation
    // ...
});
```

## Identity Registries

### Static Node Registry (Canonical Pattern)

Instead of:
```csharp
options.NodeId = new NodeId("minimal-node"); // ? Magic string
```

Use:
```csharp
options.NodeId = Nodes.Core.MinimalNode; // ? Static registry
```

**Available Registries:**
- `Nodes.Core.*` - Core infrastructure (Kernel, Transport, Vault, Data, Auth, etc.)
- `Nodes.Ops.*` - Operations and monitoring (Pipelines, Actions, Pulse, Deploy, etc.)
- `Nodes.AI.*` - AI and agents (AgentKit, Clarity, Governor, Operator)
- `Nodes.Creator.*` - Creator tools (Signal, Forge)
- `Nodes.Market.*` - Market applications (MarketCore, HiveGigs, Arcadia, Re:View, etc.)
- `Nodes.HoneyPlay.*` - Gaming and media (Draft, game prototypes)
- `Nodes.Cyberware.*` - Robotics (Courier, Sim, Servo)
- `Nodes.HoneyNet.*` - Security (BreachLab, Sentinel)
- `Nodes.Meta.*` - Meta services (Grid, HoneyHub, DevPortal, AtlasSync)

### Static Sector Registry

```csharp
Sectors.Core        // Core infrastructure
Sectors.Ops         // Operations and monitoring
Sectors.AI          // AI and machine learning
Sectors.Creator     // Creator tools and platforms
Sectors.Market      // Market-facing applications
Sectors.HoneyPlay   // Gaming and media
Sectors.Cyberware   // Robotics and hardware
Sectors.HoneyNet    // Security and defense
Sectors.Meta        // Meta services and registries
```

### Static Environment Registry

```csharp
Environments.Production    // Live customer traffic
Environments.Staging       // Pre-production validation
Environments.Development   // Active development
Environments.Testing       // Automated tests
Environments.Performance   // Load testing
Environments.Integration   // Third-party integration
Environments.Local         // Developer workstation
```

## Next Steps

- Add lifecycle hooks (`IStartupHook`, `IShutdownHook`)
- Add health contributors (`IHealthContributor`, `IReadinessContributor`)
- Configure telemetry (OpenTelemetry integration via `GridActivitySource`)
- Add custom capabilities to Node descriptor
- Create child contexts for downstream calls
- Use transport binders for message/job context propagation
