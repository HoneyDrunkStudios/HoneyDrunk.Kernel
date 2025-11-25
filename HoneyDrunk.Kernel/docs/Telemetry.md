# 📡 Telemetry - Observability Primitives

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ITelemetryContext.cs](#itelemetrycontextcs)
- [ITraceEnricher.cs](#itraceenrichercs)
- [ILogScopeFactory.cs](#ilogscopefactorycs)
- [TelemetryTags.cs](#telemetrytagscs)
- [Complete Observability Example](#complete-observability-example)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Telemetry abstractions provide OpenTelemetry-ready tracing, enrichment, and log correlation for distributed observability. These abstractions enable unified querying, filtering, and correlation across the entire Grid without coupling to specific telemetry backends.

**Location:** `HoneyDrunk.Kernel.Abstractions/Telemetry/`

**Key Concepts:**
- **Telemetry Context** - W3C Trace Context compatible view for observability
- **Trace Enrichment** - Automatic tag injection with Grid/Node context
- **Log Scopes** - Structured logging with automatic context propagation
- **Standard Tags** - Semantic naming conventions for unified observability

---

## ITelemetryContext.cs

### What it is
Readonly view of telemetry-relevant context for tracing and logging.

### Real-world analogy
Like a flight data recorder - captures everything needed for post-flight analysis.

### Properties

```csharp
public interface ITelemetryContext
{
    IGridContext GridContext { get; }           // Underlying Grid context
    string TraceId { get; }                     // W3C Trace Context trace-id
    string SpanId { get; }                      // Current span identifier
    string? ParentSpanId { get; }               // Parent span (if child)
    bool IsSampled { get; }                     // Whether trace is collected
    IReadOnlyDictionary<string, string> TelemetryBaggage { get; } // Vendor-specific metadata
}
```

### Usage Example

```csharp
public class TelemetryMiddleware(ITelemetryContext telemetryContext, ILogger logger)
{
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        // Automatic trace propagation
        logger.LogInformation(
            "Request started - TraceId: {TraceId}, SpanId: {SpanId}, CorrelationId: {CorrelationId}",
            telemetryContext.TraceId,
            telemetryContext.SpanId,
            telemetryContext.GridContext.CorrelationId);
        
        // Add trace headers to response
        httpContext.Response.Headers["X-Trace-Id"] = telemetryContext.TraceId;
        httpContext.Response.Headers["X-Span-Id"] = telemetryContext.SpanId;
        
        await next(httpContext);
    }
}
```

### W3C Trace Context Mapping

| Property | W3C Trace Context | OpenTelemetry |
|----------|-------------------|---------------|
| `TraceId` | `traceparent` trace-id | `TraceId` |
| `SpanId` | `traceparent` span-id | `SpanId` |
| `ParentSpanId` | Previous span in chain | `ParentSpanId` |
| `IsSampled` | `traceparent` sampled flag | `Sampled` |

---

## ITraceEnricher.cs

### What it is
Enriches distributed traces with Grid-wide context and metadata.

### Real-world analogy
Like adding stamps to a passport - automatically annotates traces with relevant context.

### Methods

```csharp
public interface ITraceEnricher
{
    void Enrich(ITelemetryContext context, IDictionary<string, object?> tags);
}
```

### Usage Example

```csharp
public class GridContextTraceEnricher : ITraceEnricher
{
    public void Enrich(ITelemetryContext context, IDictionary<string, object?> tags)
    {
        // Automatically add Grid context to all traces
        tags[TelemetryTags.CorrelationId] = context.GridContext.CorrelationId;
        tags[TelemetryTags.CausationId] = context.GridContext.CausationId;
        tags[TelemetryTags.NodeId] = context.GridContext.NodeId;
        tags[TelemetryTags.StudioId] = context.GridContext.StudioId;
        tags[TelemetryTags.Environment] = context.GridContext.Environment;
        
        // Add baggage as tags
        foreach (var (key, value) in context.GridContext.Baggage)
        {
            tags[$"hd.baggage.{key}"] = value;
        }
    }
}

public class NodeContextTraceEnricher(INodeContext nodeContext) : ITraceEnricher
{
    public void Enrich(ITelemetryContext context, IDictionary<string, object?> tags)
    {
        // Add Node-specific metadata
        tags[TelemetryTags.NodeVersion] = nodeContext.Version;
        tags[TelemetryTags.LifecycleStage] = nodeContext.LifecycleStage.ToString();
        tags[TelemetryTags.MachineName] = nodeContext.MachineName;
        tags[TelemetryTags.ProcessId] = nodeContext.ProcessId;
    }
}
```

### Enricher Registration

```csharp
// Register multiple enrichers (executed in order)
builder.Services.AddSingleton<ITraceEnricher, GridContextTraceEnricher>();
builder.Services.AddSingleton<ITraceEnricher, NodeContextTraceEnricher>();
builder.Services.AddSingleton<ITraceEnricher, CustomBusinessEnricher>();
```

### When to use
- Automatic context propagation across all traces
- Consistent tagging without manual instrumentation
- Adding custom business metadata to traces
- Environment-specific enrichment logic

---

## ILogScopeFactory.cs

### What it is
Creates logging scopes enriched with Grid context for structured logging.

### Real-world analogy
Like a meeting room that automatically records who's present - context is captured automatically.

### Methods

```csharp
public interface ILogScopeFactory
{
    IDisposable CreateScope(ITelemetryContext context);
    IDisposable CreateScope(ITelemetryContext context, IReadOnlyDictionary<string, object?> additionalProperties);
}
```

### Usage Example

```csharp
public class OrderProcessor(
    ITelemetryContext telemetryContext,
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
            
            await FulfillOrderAsync(order);
            logger.LogInformation("Order fulfilled");
        }
        
        // All logs above automatically include:
        // - TraceId, SpanId, CorrelationId
        // - NodeId, StudioId, Environment
        // - OrderId, CustomerId, Amount
    }
}
```

### Structured Logging Output

```json
{
  "timestamp": "2025-01-11T10:30:00Z",
  "level": "Information",
  "message": "Processing order",
  "hd.trace_id": "4bf92f3577b34da6a3ce929d0e0e4736",
  "hd.span_id": "00f067aa0ba902b7",
  "hd.correlation_id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
  "hd.node_id": "order-processor",
  "hd.studio_id": "honeycomb",
  "hd.environment": "production",
  "OrderId": "ORD-12345",
  "CustomerId": "CUST-67890",
  "Amount": 99.99
}
```

---

## TelemetryTags.cs

### What it is
Standard telemetry tag names for Grid-wide observability.

### Real-world analogy
Like a data dictionary - ensures everyone speaks the same language.

### Standard Tags

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
| **Lifecycle** | `hd.lifecycle_stage` | Node stage | `running` |
| **Error** | `hd.error_type` | Error category | `validation` |
| | `hd.error_message` | Error description | `Invalid input` |
| **Caller** | `hd.caller_id` | Who made request | `user-123` |
| | `hd.caller_type` | Caller type | `user` |

### Usage Example

```csharp
public class MetricsReporter(IMetricsCollector metrics, ITelemetryContext context)
{
    public void ReportPaymentProcessed(decimal amount, bool success)
    {
        metrics.RecordCounter("payments.processed", 1,
            new KeyValuePair<string, object?>(TelemetryTags.NodeId, context.GridContext.NodeId),
            new KeyValuePair<string, object?>(TelemetryTags.Environment, context.GridContext.Environment),
            new KeyValuePair<string, object?>(TelemetryTags.Outcome, success ? "success" : "failure"));
        
        metrics.RecordHistogram("payments.amount", (double)amount,
            new KeyValuePair<string, object?>(TelemetryTags.StudioId, context.GridContext.StudioId));
    }
}
```

### Query Examples (Prometheus/Grafana)

```promql
# Payments by Node
sum by (hd_node_id) (payments_processed_total{hd_environment="production"})

# Error rate by operation
rate(operations_total{hd_outcome="failure"}[5m]) / rate(operations_total[5m])

# P95 latency by Studio
histogram_quantile(0.95, sum by (hd_studio_id, le) (operation_duration_ms_bucket))
```

---

## Complete Observability Example

```csharp
// 1. Register telemetry services
builder.Services.AddSingleton<ITraceEnricher, GridContextTraceEnricher>();
builder.Services.AddSingleton<ITraceEnricher, NodeContextTraceEnricher>();
builder.Services.AddSingleton<ILogScopeFactory, TelemetryLogScopeFactory>();

// 2. Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("HoneyDrunk.*");
        tracing.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(nodeContext.NodeId, nodeContext.Version)
            .AddAttributes(new Dictionary<string, object>
            {
                [TelemetryTags.StudioId] = studioConfig.StudioId,
                [TelemetryTags.Environment] = studioConfig.Environment
            }));
        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(studioConfig.ObservabilityEndpoint);
        });
    });

// 3. Use in application
public class PaymentService(
    ITelemetryContext telemetryContext,
    ILogScopeFactory logScopeFactory,
    ITraceEnricher[] enrichers,
    ILogger<PaymentService> logger)
{
    public async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        // Create enriched scope
        using var scope = logScopeFactory.CreateScope(telemetryContext, new Dictionary<string, object?>
        {
            ["PaymentId"] = request.PaymentId,
            ["Amount"] = request.Amount,
            ["Currency"] = request.Currency
        });
        
        // Enrich trace
        var tags = new Dictionary<string, object?>
        {
            [TelemetryTags.Operation] = "ProcessPayment",
            [TelemetryTags.Source] = "http",
            [TelemetryTags.Target] = "payment_gateway"
        };
        
        foreach (var enricher in enrichers)
        {
            enricher.Enrich(telemetryContext, tags);
        }
        
        using var activity = ActivitySource.StartActivity("ProcessPayment", ActivityKind.Internal, tags);
        
        try
        {
            logger.LogInformation("Processing payment");
            var result = await _gateway.ChargeAsync(request);
            
            activity?.SetTag(TelemetryTags.Outcome, "success");
            logger.LogInformation("Payment successful");
            
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag(TelemetryTags.Outcome, "failure");
            activity?.SetTag(TelemetryTags.ErrorType, ex.GetType().Name);
            activity?.SetTag(TelemetryTags.ErrorMessage, ex.Message);
            
            logger.LogError(ex, "Payment failed");
            throw;
        }
    }
}
```

---

## Testing Patterns

```csharp
[Fact]
public void TraceEnricher_AddsGridContext()
{
    // Arrange
    var context = new TestTelemetryContext
    {
        GridContext = new GridContext
        {
            CorrelationId = "test-123",
            NodeId = "test-node",
            StudioId = "test-studio"
        }
    };
    var enricher = new GridContextTraceEnricher();
    var tags = new Dictionary<string, object?>();
    
    // Act
    enricher.Enrich(context, tags);
    
    // Assert
    Assert.Equal("test-123", tags[TelemetryTags.CorrelationId]);
    Assert.Equal("test-node", tags[TelemetryTags.NodeId]);
    Assert.Equal("test-studio", tags[TelemetryTags.StudioId]);
}

[Fact]
public void LogScopeFactory_CreatesScope()
{
    // Arrange
    var factory = new TelemetryLogScopeFactory();
    var context = new TestTelemetryContext { TraceId = "trace-123" };
    
    // Act
    using var scope = factory.CreateScope(context);
    
    // Assert - scope should be active
    Assert.NotNull(scope);
}
```

---

## Summary

| Component | Purpose | Integration Point |
|-----------|---------|-------------------|
| **ITelemetryContext** | W3C Trace Context view | OpenTelemetry, App Insights |
| **ITraceEnricher** | Automatic tag injection | Trace creation pipeline |
| **ILogScopeFactory** | Structured logging scopes | Logger configuration |
| **TelemetryTags** | Standard tag names | All metrics/traces/logs |

**Key Patterns:**
- Use enrichers for automatic context propagation
- Use log scopes for structured logging
- Use standard tags for unified querying
- Map to W3C Trace Context for interoperability

**OpenTelemetry Integration:**
- ITelemetryContext maps to Activity/Span
- TelemetryTags align with semantic conventions
- Enrichers inject tags at trace creation
- Compatible with OTLP exporters

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

