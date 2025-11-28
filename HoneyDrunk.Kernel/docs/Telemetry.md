# 📡 Telemetry - Observability Primitives

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- **Abstractions**
  - [ITelemetryContext.cs](#itelemetrycontextcs)
  - [ITelemetryActivityFactory.cs](#itelemetryactivityfactorycs)
  - [ITraceEnricher.cs](#itraceenrichercs)
  - [ILogScopeFactory.cs](#ilogscopefactorycs)
  - [TelemetryTags.cs](#telemetrytagscs)
- **Implementations**
  - [GridActivitySource.cs](#gridactivitysourcecs)
  - [HoneyDrunkTelemetry.cs](#honeydrunktelemetrycs)
  - [TelemetryActivityFactory.cs](#telemetryactivityfactorycs)
  - [TelemetryContext.cs](#telemetrycontextcs)
  - [GridContextTraceEnricher.cs](#gridcontexttraceenrichercs)
  - [TelemetryLogScopeFactory.cs](#telemetrylogscopefactorycs)
- [Complete Observability Example](#complete-observability-example)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Telemetry abstractions provide OpenTelemetry-ready tracing, enrichment, and log correlation for distributed observability. These abstractions enable unified querying, filtering, and correlation across the entire Grid without coupling to specific telemetry backends.

**What Telemetry Does:** Gives every Node a standard way to produce OpenTelemetry traces and structured logs that are pre-tagged with Grid context, so you can follow a **single correlation ID** across **HTTP, messaging, database, and logs** anywhere in the Grid.

**Location:** 
- Abstractions: `HoneyDrunk.Kernel.Abstractions/Telemetry/`
- Implementations: `HoneyDrunk.Kernel/Telemetry/`

**Key Concepts:**
- **Telemetry Context** - W3C Trace Context compatible view for observability
- **Activity Factory** - Creates enriched OpenTelemetry Activities with ambient context
- **Trace Enrichment** - Automatic tag injection with Grid/Node context
- **Log Scopes** - Structured logging with automatic context propagation
- **Standard Tags** - Semantic naming conventions for unified observability
- **Grid Activity Source** - Centralized `ActivitySource` for Grid operations

**Architecture:**
- **GridContext** - "Who am I" (Node, Studio, Environment, correlation, baggage)
- **Activity** - "What am I doing right now" (span with timing and outcome)
- **TelemetryContext** - "Observability snapshot" (GridContext + trace/span IDs)
- **HoneyDrunkTelemetry + ITelemetryActivityFactory** - Main entry points for starting spans
- **ITraceEnricher + ILogScopeFactory** - Standard way to attach context to traces and logs
- **TelemetryTags** - Shared vocabulary for Grid-wide observability

[↑ Back to top](#table-of-contents)

---

## Abstractions

### ITelemetryContext.cs

**What it is:** Readonly view of telemetry-relevant context for tracing and logging.

**Real-world analogy:** Like a flight data recorder - captures everything needed for post-flight analysis.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/ITelemetryContext.cs`

#### Properties

```csharp
public interface ITelemetryContext
{
    IGridContext GridContext { get; }           // Underlying Grid context
    string TraceId { get; }                     // W3C Trace Context trace-id
    string SpanId { get; }                      // Current span identifier
    string? ParentSpanId { get; }               // Parent span (if child)
    bool IsSampled { get; }                     // Whether trace is collected
    IReadOnlyDictionary<string, string> TelemetryBaggage { get; } // Vendor-specific metadata (reserved for future)
}
```

#### W3C Trace Context Mapping

| Property | W3C Trace Context | OpenTelemetry |
|----------|-------------------|---------------|
| `TraceId` | `traceparent` trace-id | `TraceId` |
| `SpanId` | `traceparent` span-id | `SpanId` |
| `ParentSpanId` | Previous span in chain | `ParentSpanId` |
| `IsSampled` | `traceparent` sampled flag | `Sampled` |

#### Baggage Semantics

**GridContext.Baggage vs TelemetryBaggage:**
- **GridContext.Baggage** - Grid-specific metadata propagated across Node boundaries (tenant hints, feature flags, custom routing)
- **TelemetryBaggage** - Reserved for backend-specific telemetry metadata (vendor hints, sampling decisions). Currently unused in v0.3.0, may be used for APM-specific tagging in future versions.

**Current Behavior:** `TelemetryBaggage` is typically empty. All application-level baggage flows through `GridContext.Baggage`.

[↑ Back to top](#table-of-contents)

---

### ITelemetryActivityFactory.cs

**What it is:** Factory abstraction for creating enriched `Activity` instances using ambient Grid/Operation context.

**Real-world analogy:** Like a form generator that auto-fills your personal info - starts activities with context already attached.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/ITelemetryActivityFactory.cs`

**Intended Audience:** **Recommended for application services** that want automatic context wiring, enricher integration, and consistent tagging without manual plumbing.

#### Methods

```csharp
public interface ITelemetryActivityFactory
{
    // Uses ambient IGridContextAccessor / IOperationContextAccessor
    Activity? Start(string name, IReadOnlyDictionary<string, object?>? additionalTags = null);
    
    // Uses explicit contexts (bypasses ambient)
    Activity? StartExplicit(
        string name,
        IGridContext gridContext,
        IOperationContext? operationContext = null,
        IReadOnlyDictionary<string, object?>? additionalTags = null);
}
```

#### When to Use

| Use Case | Recommended API |
|----------|-----------------|
| **Application service logic** | `ITelemetryActivityFactory.Start()` |
| **Infrastructure (HTTP/messaging/database)** | `GridActivitySource.StartActivity()` or domain helpers |
| **Kernel internal instrumentation** | `HoneyDrunkTelemetry.StartActivity()` |

#### Usage Example

```csharp
public class PaymentService(ITelemetryActivityFactory activityFactory)
{
    public async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        // Uses ambient GridContext from IGridContextAccessor
        using var activity = activityFactory.Start("ProcessPayment", new Dictionary<string, object?>
        {
            ["PaymentId"] = request.PaymentId,
            ["Amount"] = request.Amount
        });
        
        try
        {
            var result = await _gateway.ChargeAsync(request);
            activity?.SetTag(TelemetryTags.Outcome, "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag(TelemetryTags.Outcome, "failure");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex); // OpenTelemetry standard exception recording
            throw;
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

### ITraceEnricher.cs

**What it is:** Enriches distributed traces with Grid-wide context and metadata.

**Real-world analogy:** Like adding stamps to a passport - automatically annotates traces with relevant context.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/ITraceEnricher.cs`

#### Methods

```csharp
public interface ITraceEnricher
{
    void Enrich(ITelemetryContext context, IDictionary<string, object?> tags);
}
```

#### When to use
- Automatic context propagation across all traces
- Consistent tagging without manual instrumentation
- Adding custom business metadata to traces
- Environment-specific enrichment logic

[↑ Back to top](#table-of-contents)

---

### ILogScopeFactory.cs

**What it is:** Creates logging scopes enriched with Grid context for structured logging.

**Real-world analogy:** Like a meeting room that automatically records who's present - context is captured automatically.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/ILogScopeFactory.cs`

#### Methods

```csharp
public interface ILogScopeFactory
{
    IDisposable CreateScope(ITelemetryContext context);
    IDisposable CreateScope(ITelemetryContext context, IReadOnlyDictionary<string, object?> additionalProperties);
}
```

#### DI Registration and Lifetime

**ITelemetryContext Requirement:** `ILogScopeFactory` expects `ITelemetryContext` to be registered as **scoped** in DI. This is automatically handled by Node bootstrap when you call `AddHoneyDrunkGrid()` or `AddHoneyDrunkNode()`.

**Typical Registration:**
```csharp
// ITelemetryContext registered as scoped (automatically resolved from Activity + GridContext)
builder.Services.AddScoped<ITelemetryContext>(/* factory from previous section */);

// ILogScopeFactory registered as singleton
builder.Services.AddSingleton<ILogScopeFactory>(sp =>
    new TelemetryLogScopeFactory(sp.GetRequiredService<ILogger<TelemetryLogScopeFactory>>()));
```

**Note:** `TelemetryLogScopeFactory` takes a non-generic `ILogger` in its constructor but is typically registered with `ILogger<TelemetryLogScopeFactory>`. This means log scopes are keyed to that logger category. For truly Node-wide scoping independent of category, consider using a logger provider integration instead.

#### Usage Example

```csharp
public class OrderProcessor(
    ITelemetryContext telemetryContext,  // Scoped, resolved from Activity + GridContext
    ILogScopeFactory logScopeFactory,
    ILogger<OrderProcessor> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        // Create scope - all log statements within include telemetry context
        using (logScopeFactory.CreateScope(telemetryContext, new Dictionary<string, object?>
        {
            ["OrderId"] = order.Id,
            ["CustomerId"] = order.CustomerId,
            ["Amount"] = order.TotalAmount
        }))
        {
            logger.LogInformation("Processing order");
            await ValidateOrderAsync(order);
            logger.LogInformation("Order validated");
            await ChargePaymentAsync(order);
            logger.LogInformation("Payment charged");
        }
        
        // All logs above automatically include:
        // - TraceId, SpanId, CorrelationId
        // - NodeId, StudioId, Environment
        // - OrderId, CustomerId, Amount
    }
}
```

[↑ Back to top](#table-of-contents)

---

### TelemetryTags.cs

**What it is:** Standard telemetry tag names for Grid-wide observability.

**Real-world analogy:** Like a data dictionary - ensures everyone speaks the same language.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/TelemetryTags.cs`

#### Standard Tags

| Category | Tag | Description | Example |
|----------|-----|-------------|---------|
| **Correlation** | `hd.correlation_id` | Groups related operations | `01HQXZ...` |
| | `hd.causation_id` | Operation that triggered this | `01HQXY...` |
| **Identity** | `hd.node_id` | Node identifier | `payment-node` |
| | `hd.node_version` | Node version | `2.1.0` |
| | `hd.studio_id` | Studio identifier | `honeycomb` |
| | `hd.tenant_id` | Tenant identifier | `01HQXZ...` |
| | `hd.project_id` | Project identifier | `01HQXY...` |
| **Environment** | `hd.environment` | Environment name | `production` |
| | `hd.machine_name` | Host machine | `web-01` |
| | `hd.process_id` | OS process ID | `12345` |
| **Operation** | `hd.operation` | Operation name | `ProcessPayment` |
| | `hd.outcome` | Success/failure | `success` |
| | `hd.source` | Request source | `http` |
| | `hd.target` | Target system | `database` |
| | `hd.duration_ms` | Duration (ms) | `125.4` |
| **Lifecycle** | `hd.lifecycle_stage` | Node stage | `ready` |
| **Error** | `hd.error_type` | Error category | `validation` |
| | `hd.error_message` | Error description | `Invalid input` |
| **Caller** | `hd.caller_id` | Who made request | `user-123` |
| | `hd.caller_type` | Caller type | `user` |

[↑ Back to top](#table-of-contents)

---

## Implementations

### GridActivitySource.cs

**What it is:** Provides the central `ActivitySource` for HoneyDrunk Grid operations with helper methods for creating enriched activities.

**Real-world analogy:** Like a stamping station - automatically marks every activity with Grid identity.

**Location:** `HoneyDrunk.Kernel/Telemetry/GridActivitySource.cs`

**Intended Audience:** Primarily for **infrastructure-level operations** (HTTP pipelines, messaging adapters, database adapters). **Application services should prefer `ITelemetryActivityFactory`** for automatic context wiring and enricher integration.

#### Key Properties

```csharp
public static class GridActivitySource
{
    public const string SourceName = "HoneyDrunk.Grid";
    public const string Version = "0.3.0";
    public static ActivitySource Instance { get; }
}
```

#### Methods

##### StartActivity

Creates a new activity with Grid context enrichment.

```csharp
public static Activity? StartActivity(
    string operationName,
    IGridContext gridContext,
    ActivityKind kind = ActivityKind.Internal,
    IEnumerable<KeyValuePair<string, object?>>? tags = null)
```

**Automatic Tags:**
- `hd.correlation_id`, `hd.node_id`, `hd.studio_id`, `hd.environment`
- `hd.causation_id` (if present)
- `hd.baggage.*` (all baggage items)

##### Domain-Specific Helpers

```csharp
// HTTP operations
Activity? StartHttpActivity(string method, string path, IGridContext gridContext);

// Database operations
Activity? StartDatabaseActivity(string operationType, string tableName, IGridContext gridContext);

// Messaging operations
Activity? StartMessageActivity(string messageType, string destination, IGridContext gridContext, ActivityKind kind);
```

##### Source-Agnostic Helper Methods

These helpers work with **any** `Activity` regardless of source:

```csharp
// Record exception with standard tags
void RecordException(Activity? activity, Exception exception);

// Mark activity as successful
void SetSuccess(Activity? activity);
```

**Design Note:** `RecordException()` and `SetSuccess()` are safe to use with activities created from `ITelemetryActivityFactory`, `HoneyDrunkTelemetry`, or any other `ActivitySource`.

#### Usage Example

```csharp
// Infrastructure usage (HTTP middleware)
public class GridHttpMiddleware(IGridContext gridContext)
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using var activity = GridActivitySource.StartHttpActivity(
            context.Request.Method,
            context.Request.Path,
            gridContext);
        
        try
        {
            await next(context);
            GridActivitySource.SetSuccess(activity);
        }
        catch (Exception ex)
        {
            GridActivitySource.RecordException(activity, ex);
            throw;
        }
    }
}
```

#### OpenTelemetry Registration

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(GridActivitySource.SourceName);
        tracing.AddOtlpExporter();
    });
```

[↑ Back to top](#table-of-contents)

---

### HoneyDrunkTelemetry.cs

**What it is:** Central telemetry facade providing a single `ActivitySource` and helpers for starting activities enriched with Grid/Operation context and trace enrichers.

**Real-world analogy:** Like a dispatch center - coordinates all telemetry with consistent standards.

**Location:** `HoneyDrunk.Kernel/Telemetry/HoneyDrunkTelemetry.cs`

#### Key Members

```csharp
public static class HoneyDrunkTelemetry
{
    public static readonly ActivitySource ActivitySource = new("HoneyDrunk.Kernel");
    
    public static Activity? StartActivity(
        string name,
        IGridContext grid,
        IOperationContext? operation = null,
        IEnumerable<ITraceEnricher>? enrichers = null,
        IReadOnlyDictionary<string, object?>? additionalTags = null);
}
```

#### What It Does

1. **Creates baseline tags** from `IGridContext` (correlation, node, studio, environment)
2. **Adds operation tags** from `IOperationContext` (operation name, outcome)
3. **Runs trace enrichers** to add custom tags
4. **Applies additional tags** from caller
5. **Starts and returns** the enriched `Activity`

#### Usage Example

```csharp
public class PaymentProcessor(
    IGridContext gridContext,
    IOperationContext operationContext,
    IEnumerable<ITraceEnricher> enrichers)
{
    public async Task ProcessAsync(Payment payment)
    {
        using var activity = HoneyDrunkTelemetry.StartActivity(
            "ProcessPayment",
            gridContext,
            operationContext,
            enrichers,
            new Dictionary<string, object?>
            {
                ["payment.id"] = payment.Id,
                ["payment.amount"] = payment.Amount
            });
        
        // Activity automatically includes:
        // - Grid tags (correlation, node, studio)
        // - Operation tags (operation name, outcome)
        // - Enricher tags (custom business context)
        // - Payment-specific tags
        
        await ChargePaymentAsync(payment);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### TelemetryActivityFactory.cs

**What it is:** Implementation of `ITelemetryActivityFactory` that uses ambient context accessors and trace enrichers.

**Location:** `HoneyDrunk.Kernel/Telemetry/TelemetryActivityFactory.cs`

#### How It Works

```csharp
internal sealed class TelemetryActivityFactory(
    IGridContextAccessor gridAccessor,
    IOperationContextAccessor opAccessor,
    IEnumerable<ITraceEnricher> enrichers) : ITelemetryActivityFactory
{
    public Activity? Start(string name, IReadOnlyDictionary<string, object?>? additionalTags = null)
    {
        var grid = _gridAccessor.GridContext;
        if (grid is null) return null; // No ambient context
        
        var op = _opAccessor.Current;
        return HoneyDrunkTelemetry.StartActivity(name, grid, op, _enrichers, additionalTags);
    }
    
    public Activity? StartExplicit(string name, IGridContext gridContext, IOperationContext? operationContext = null, IReadOnlyDictionary<string, object?>? additionalTags = null)
    {
        return HoneyDrunkTelemetry.StartActivity(name, gridContext, operationContext, _enrichers, additionalTags);
    }
}
```

**Design:** Registered via DI as `ITelemetryActivityFactory`. Uses ambient accessors for `Start()` and explicit contexts for `StartExplicit()`.

[↑ Back to top](#table-of-contents)

---

### TelemetryContext.cs

**What it is:** Default implementation of `ITelemetryContext`.

**Location:** `HoneyDrunk.Kernel/Telemetry/TelemetryContext.cs`

#### Constructor

```csharp
public sealed class TelemetryContext(
    IGridContext gridContext,
    string traceId,
    string spanId,
    string? parentSpanId = null,
    bool isSampled = true,
    IReadOnlyDictionary<string, string>? telemetryBaggage = null) : ITelemetryContext
```

**Design:** Immutable wrapper around `IGridContext` with additional OpenTelemetry-specific metadata.

[↑ Back to top](#table-of-contents)

---

### TelemetryContext Creation and Lifetime

**What it is:** How `ITelemetryContext` instances are created and made available via DI.

#### How It Works

**In standard Nodes**, `ITelemetryContext` is registered as **scoped** when you call `AddHoneyDrunkGrid()` or `AddHoneyDrunkNode()`. The factory below illustrates how it is constructed from the current `Activity` and `IGridContext`.

`ITelemetryContext` is automatically created for each request/operation by Node middleware or message handlers:

1. **HTTP Requests** - `GridContextMiddleware` creates `ITelemetryContext` from:
   - Current `Activity.TraceId` / `Activity.SpanId` / `Activity.ParentSpanId`
   - Current `IGridContext` (from headers or ambient accessor)
   - `Activity.Recorded` for `IsSampled`

2. **Message Processing** - Message handler creates `ITelemetryContext` from:
   - Message properties (trace headers)
   - `IGridContext` extracted from message metadata
   - Current `Activity` if available

3. **Background Jobs** - Job infrastructure creates `ITelemetryContext` from:
   - Job metadata (correlation ID, trace context)
   - `IGridContext` from job parameters
   - New `Activity` started for job execution

#### Illustrative Factory (Custom Host Scenarios)

If you are hosting outside the standard Node bootstrap, here's how to manually register `ITelemetryContext`:

```csharp
// Example factory for custom hosting scenarios (not needed with AddHoneyDrunkGrid)
builder.Services.AddScoped<ITelemetryContext>(sp =>
{
    var gridContext = sp.GetRequiredService<IGridContextAccessor>().GridContext
        ?? throw new InvalidOperationException("No ambient GridContext");
    
    var activity = Activity.Current;
    if (activity is null)
    {
        // Fallback: create minimal context (not sampled)
        return new TelemetryContext(
            gridContext,
            traceId: gridContext.CorrelationId,
            spanId: gridContext.CorrelationId, // Reuse correlation ID for simplicity
            isSampled: false);
    }
    
    return new TelemetryContext(
        gridContext,
        traceId: activity.TraceId.ToString(),
        spanId: activity.SpanId.ToString(),
        parentSpanId: activity.ParentSpanId?.ToString(), // Nullable for root spans
        isSampled: activity.Recorded);
});
```

#### Usage in Services

```csharp
// ITelemetryContext automatically available in scoped services
public class OrderService(ITelemetryContext telemetryContext, ILogger<OrderService> logger)
{
    public async Task ProcessOrderAsync(Order order)
    {
        // Use telemetryContext for logging scopes, enrichers, etc.
        logger.LogInformation(
            "Processing order {OrderId} in trace {TraceId}",
            order.Id,
            telemetryContext.TraceId);
    }
}
```

**Lifetime:** Scoped to the current request/message/job. Created once per scope, shared across all services in that scope.

[↑ Back to top](#table-of-contents)

---

## Complete Observability Example

**Context:** This example shows Node-level bootstrap configuration and application service usage following recommended patterns.

```csharp
// ===== 1. Node Bootstrap (Program.cs) =====
var builder = WebApplication.CreateBuilder(args);

// Register Node with Grid (includes ITelemetryContext as scoped)
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "payment-node";
    options.StudioId = "demo-studio";
    options.Version = "1.0.0";
    options.Environment = "production";
});

// Register telemetry services
builder.Services.AddSingleton<ITraceEnricher, GridContextTraceEnricher>();
builder.Services.AddSingleton<ITelemetryActivityFactory, TelemetryActivityFactory>();
builder.Services.AddSingleton<ILogScopeFactory>(sp =>
    new TelemetryLogScopeFactory(sp.GetRequiredService<ILogger<TelemetryLogScopeFactory>>()));

// Configure OpenTelemetry (Node hosting layer, not app code)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        // Resource attributes should align with Grid concepts:
        // - service.name = NodeId
        // - service.version = NodeVersion
        // - TelemetryTags.StudioId, TelemetryTags.Environment as custom attributes
        
        resource.AddService(
            serviceName: "payment-node",     // In real Nodes, resolved from INodeContext.NodeId
            serviceVersion: "1.0.0");        // In real Nodes, resolved from INodeContext.Version
        
        resource.AddAttributes(new Dictionary<string, object>
        {
            [TelemetryTags.StudioId] = "demo-studio",      // From INodeContext.StudioId
            [TelemetryTags.Environment] = "production"     // From INodeContext.Environment
        });
    })
    .WithTracing(tracing =>
    {
        // Register both ActivitySources
        tracing.AddSource(GridActivitySource.SourceName);           // "HoneyDrunk.Grid"
        tracing.AddSource(HoneyDrunkTelemetry.ActivitySource.Name); // "HoneyDrunk.Kernel"
        
        // Add instrumentation
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        
        // Export to OTLP collector
        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Observability:OtlpEndpoint"]!);
        });
    });

var app = builder.Build();
app.UseGridContext(); // Middleware creates ITelemetryContext per request
app.MapControllers();
await app.RunAsync();

// ===== 2. Application Service (Recommended Pattern) =====
public class PaymentService(
    ITelemetryActivityFactory activityFactory,  // Ambient context-based
    ILogScopeFactory logScopeFactory,
    ITelemetryContext telemetryContext,         // Scoped, from Activity + GridContext
    ILogger<PaymentService> logger)
{
    public async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        // Create enriched logging scope
        using var logScope = logScopeFactory.CreateScope(telemetryContext, new Dictionary<string, object?>
        {
            ["PaymentId"] = request.PaymentId,
            ["Amount"] = request.Amount,
            ["Currency"] = request.Currency
        });
        
        // Start activity with ambient context (uses HoneyDrunkTelemetry.ActivitySource internally)
        using var activity = activityFactory.Start("ProcessPayment", new Dictionary<string, object?>
        {
            [TelemetryTags.Source] = "http",
            [TelemetryTags.Target] = "payment_gateway"
        });
        
        try
        {
            logger.LogInformation("Processing payment");
            var result = await _gateway.ChargeAsync(request);
            
            // Use OpenTelemetry standard methods
            activity?.SetTag(TelemetryTags.Outcome, "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Payment successful");
            
            return result;
        }
        catch (Exception ex)
        {
            // Use OpenTelemetry standard exception recording
            activity?.SetTag(TelemetryTags.Outcome, "failure");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            logger.LogError(ex, "Payment failed");
            throw;
        }
    }
}

// ===== 3. Infrastructure Middleware (GridActivitySource pattern) =====
public class DatabaseTracingMiddleware(IGridContext gridContext)
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationType, string tableName)
    {
        using var activity = GridActivitySource.StartDatabaseActivity(operationType, tableName, gridContext);
        
        try
        {
            var result = await operation();
            GridActivitySource.SetSuccess(activity);
            return result;
        }
        catch (Exception ex)
        {
            GridActivitySource.RecordException(activity, ex);
            throw;
        }
    }
}
```

**Key Patterns:**
- **Node Bootstrap** - Configure OpenTelemetry at hosting layer with Grid-aligned resource attributes
- **Application Services** - Use `ITelemetryActivityFactory` + `ILogScopeFactory` for automatic context wiring
- **Infrastructure** - Use `GridActivitySource` for HTTP/database/messaging adapters
- **Helper Methods** - `SetSuccess()` / `RecordException()` are source-agnostic and safe with any Activity

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

```csharp
[Fact]
public void GridActivitySource_StartActivity_EnrichesWithGridContext()
{
    // Arrange
    var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
    
    // Act
    using var activity = GridActivitySource.StartActivity("TestOperation", gridContext);
    
    // Assert
    activity.Should().NotBeNull();
    activity!.GetTagItem("hd.correlation_id").Should().Be("corr-123");
    activity.GetTagItem("hd.node_id").Should().Be("test-node");
    activity.GetTagItem("hd.studio_id").Should().Be("test-studio");
}

[Fact]
public void TraceEnricher_AddsGridContext()
{
    // Arrange
    var gridContext = new GridContext("test-123", "test-node", "test-studio", "test");
    var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
    var enricher = new GridContextTraceEnricher();
    var tags = new Dictionary<string, object?>();
    
    // Act
    enricher.Enrich(telemetryContext, tags);
    
    // Assert
    tags[TelemetryTags.CorrelationId].Should().Be("test-123");
    tags[TelemetryTags.NodeId].Should().Be("test-node");
    tags[TelemetryTags.StudioId].Should().Be("test-studio");
}

[Fact]
public void LogScopeFactory_CreatesScope()
{
    // Arrange
    var factory = new TelemetryLogScopeFactory(NullLogger.Instance);
    var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
    var telemetryContext = new TelemetryContext(gridContext, "trace-123", "span-456");
    
    // Act
    using var scope = factory.CreateScope(telemetryContext);
    
    // Assert
    scope.Should().NotBeNull();
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

| Component | Purpose | Location | Type | Audience |
|-----------|---------|----------|------|----------|
| **ITelemetryContext** | W3C Trace Context view | Abstractions | Interface | All |
| **ITelemetryActivityFactory** | Ambient activity creation | Abstractions | Interface | **Application Services** |
| **ITraceEnricher** | Automatic tag injection | Abstractions | Interface | All |
| **ILogScopeFactory** | Structured logging scopes | Abstractions | Interface | All |
| **TelemetryTags** | Standard tag names | Abstractions | Static class | All |
| **GridActivitySource** | Central ActivitySource + helpers | Implementation | Static class | **Infrastructure** |
| **HoneyDrunkTelemetry** | Telemetry facade | Implementation | Static class | **Kernel Internals** |
| **TelemetryActivityFactory** | Factory implementation | Implementation | Class (internal) | DI |
| **TelemetryContext** | Context implementation | Implementation | Class | DI |
| **GridContextTraceEnricher** | Built-in enricher | Implementation | Class | DI |
| **TelemetryLogScopeFactory** | Scope factory implementation | Implementation | Class | DI |

**Key Patterns:**

**For Application Services (Recommended):**
- Use `ITelemetryActivityFactory.Start()` for automatic context wiring and enricher integration
- Use `ILogScopeFactory.CreateScope()` for structured logging with automatic Grid context
- Use OpenTelemetry standard methods: `Activity.SetStatus()`, `Activity.RecordException()`
- Helper methods `GridActivitySource.SetSuccess()` / `RecordException()` are safe with any Activity

**For Infrastructure (HTTP/Messaging/Database Adapters):**
- Use `GridActivitySource.StartActivity()` or domain-specific helpers (`StartHttpActivity`, `StartDatabaseActivity`, `StartMessageActivity`)
- Directly tag activities with Grid context for protocol-level tracing

**For Kernel Internals:**
- Use `HoneyDrunkTelemetry.StartActivity()` for Kernel-internal instrumentation

**OpenTelemetry Integration:**
- Register both `GridActivitySource.SourceName` ("HoneyDrunk.Grid") and `HoneyDrunkTelemetry.ActivitySource.Name` ("HoneyDrunk.Kernel")
- Configure resource attributes to align with Grid concepts: `service.name = NodeId`, `service.version = NodeVersion`
- Activities automatically enriched with Grid context via `ITraceEnricher`
- Compatible with OTLP exporters, Jaeger, Zipkin, Application Insights

**Context Flow:**
1. `GridContext` provides Node/Studio/Environment identity
2. `Activity.Current` provides W3C trace/span IDs
3. `ITelemetryContext` bridges both for observability
4. Middleware creates `ITelemetryContext` per request/message (scoped DI)
5. Services inject `ITelemetryContext` for logging scopes and enrichers

**Two Activity Sources:**
- **GridActivitySource** ("HoneyDrunk.Grid") - Grid-level operations (HTTP, messaging, database)
- **HoneyDrunkTelemetry.ActivitySource** ("HoneyDrunk.Kernel") - Kernel-internal instrumentation

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

