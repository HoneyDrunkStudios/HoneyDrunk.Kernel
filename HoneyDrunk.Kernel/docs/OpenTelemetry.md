# 📊 OpenTelemetry Integration - Distributed Tracing with Activity API

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [GridActivitySource](#gridactivitysource)
- [Core Methods](#core-methods)
  - [StartActivity](#startactivity)
  - [StartHttpActivity](#starthttpactivity)
  - [StartDatabaseActivity](#startdatabaseactivity)
  - [StartMessageActivity](#startmessageactivity)
- [Helper Methods](#helper-methods)
- [OpenTelemetry Configuration](#opentelemetry-configuration)
- [Trace Visualization](#trace-visualization)
- [Complete Example](#complete-example)
- [Activity Naming Conventions](#activity-naming-conventions)
- [Best Practices](#best-practices)
- [Testing](#testing)
- [Summary](#summary)

---

## Overview

GridActivitySource provides OpenTelemetry-compatible distributed tracing using .NET's `System.Diagnostics.Activity` API with automatic Grid context enrichment.

**Location:** `HoneyDrunk.Kernel/Telemetry/GridActivitySource.cs`

**Key Concepts:**
- **ActivitySource** - OpenTelemetry-compatible source for creating traces
- **Automatic Enrichment** - Grid context automatically added to all activities
- **Standard Naming** - Consistent activity names across the Grid
- **Zero Dependencies** - Uses built-in .NET `Activity` API

---

## GridActivitySource

### What it is
A static helper class that provides a centralized `ActivitySource` and convenience methods for creating Grid-aware distributed traces.

### Real-world analogy
Like a stamp machine at the post office - every outgoing package (operation) gets automatically stamped with tracking information (Grid context).

### Static Properties

```csharp
public static class GridActivitySource
{
    /// <summary>
    /// The ActivitySource name for all HoneyDrunk Grid operations.
    /// </summary>
    public const string SourceName = "HoneyDrunk.Grid";
    
    /// <summary>
    /// The version of the ActivitySource (matches Kernel version).
    /// </summary>
    public const string Version = "0.3.0";
    
    /// <summary>
    /// Gets the ActivitySource instance for HoneyDrunk Grid operations.
    /// </summary>
    public static ActivitySource Instance { get; }
}
```

---

## Core Methods

### StartActivity

Creates a new activity with automatic Grid context enrichment.

```csharp
public static Activity? StartActivity(
    string operationName,
    IGridContext gridContext,
    ActivityKind kind = ActivityKind.Internal,
    IEnumerable<KeyValuePair<string, object?>>? tags = null)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `operationName` | `string` | Name of the operation (e.g., "ProcessPayment") |
| `gridContext` | `IGridContext` | Grid context to enrich the activity with |
| `kind` | `ActivityKind` | Activity kind (Internal, Server, Client, Producer, Consumer) |
| `tags` | `IEnumerable<KeyValuePair<string, object?>>?` | Additional tags to add |

**Returns:** `Activity?` - The started activity, or `null` if no listeners are active.

**Automatic Tags Added:**

| Tag | Source | Example |
|-----|--------|---------|
| `hd.correlation_id` | `gridContext.CorrelationId` | `01HQXZ8K4TJ9X5B3N2YGF7WDCQ` |
| `hd.node_id` | `gridContext.NodeId` | `payment-service` |
| `hd.studio_id` | `gridContext.StudioId` | `honeycomb-prod` |
| `hd.environment` | `gridContext.Environment` | `production` |
| `hd.causation_id` | `gridContext.CausationId` | `01HQXY7J3SI8W4A2M1XE6UFBZP` |
| `hd.baggage.*` | `gridContext.Baggage` | `hd.baggage.TenantId: 01HQXZ...` |

**Example:**

```csharp
public class PaymentService(IGridContext gridContext, ILogger<PaymentService> logger)
{
    public async Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        using var activity = GridActivitySource.StartActivity(
            "ProcessPayment",
            gridContext,
            ActivityKind.Internal,
            new[]
            {
                new KeyValuePair<string, object?>("payment.amount", request.Amount),
                new KeyValuePair<string, object?>("payment.currency", request.Currency)
            });
        
        try
        {
            logger.LogInformation("Processing payment for {Amount} {Currency}",
                request.Amount, request.Currency);
            
            var result = await ChargeAsync(request);
            
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

**Trace Output:**

```json
{
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7",
  "operationName": "ProcessPayment",
  "kind": "Internal",
  "startTime": "2025-01-11T10:30:00.000Z",
  "duration": "125ms",
  "status": "Ok",
  "tags": {
    "hd.correlation_id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "hd.node_id": "payment-service",
    "hd.studio_id": "honeycomb-prod",
    "hd.environment": "production",
    "payment.amount": 100.00,
    "payment.currency": "USD"
  }
}
```

---

### StartHttpActivity

Creates an activity for HTTP operations with semantic HTTP tags.

```csharp
public static Activity? StartHttpActivity(
    string method,
    string path,
    IGridContext gridContext)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `method` | `string` | HTTP method (GET, POST, PUT, DELETE, etc.) |
| `path` | `string` | Request path (/api/payments) |
| `gridContext` | `IGridContext` | Grid context |

**Additional Tags:**

| Tag | Example |
|-----|---------|
| `http.method` | `POST` |
| `http.target` | `/api/payments` |

**Example:**

```csharp
public class PaymentController(
    IGridContext gridContext,
    IPaymentService paymentService) : ControllerBase
{
    [HttpPost("/api/payments")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        using var activity = GridActivitySource.StartHttpActivity(
            HttpContext.Request.Method,
            HttpContext.Request.Path,
            gridContext);
        
        try
        {
            var result = await paymentService.ProcessAsync(request);
            GridActivitySource.SetSuccess(activity);
            return Ok(result);
        }
        catch (Exception ex)
        {
            GridActivitySource.RecordException(activity, ex);
            throw;
        }
    }
}
```

**Trace Output:**

```json
{
  "operationName": "HTTP POST /api/payments",
  "kind": "Server",
  "tags": {
    "hd.correlation_id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "hd.node_id": "payment-service",
    "http.method": "POST",
    "http.target": "/api/payments"
  }
}
```

---

### StartDatabaseActivity

Creates an activity for database operations with semantic database tags.

```csharp
public static Activity? StartDatabaseActivity(
    string operationType,
    string tableName,
    IGridContext gridContext)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `operationType` | `string` | Database operation (query, command, etc.) |
| `tableName` | `string` | Table/collection name |
| `gridContext` | `IGridContext` | Grid context |

**Additional Tags:**

| Tag | Example |
|-----|---------|
| `db.operation` | `query` |
| `db.table` | `Payments` |

**Example:**

```csharp
public class PaymentRepository(IGridContext gridContext, IDbConnection connection)
{
    public async Task<Payment?> GetByIdAsync(string paymentId)
    {
        using var activity = GridActivitySource.StartDatabaseActivity(
            "query",
            "Payments",
            gridContext);
        
        activity?.SetTag("db.payment_id", paymentId);
        
        try
        {
            var payment = await connection.QuerySingleOrDefaultAsync<Payment>(
                "SELECT * FROM Payments WHERE Id = @Id",
                new { Id = paymentId });
            
            GridActivitySource.SetSuccess(activity);
            return payment;
        }
        catch (Exception ex)
        {
            GridActivitySource.RecordException(activity, ex);
            throw;
        }
    }
}
```

**Trace Output:**

```json
{
  "operationName": "DB query Payments",
  "kind": "Client",
  "tags": {
    "hd.correlation_id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "hd.node_id": "payment-service",
    "db.operation": "query",
    "db.table": "Payments",
    "db.payment_id": "PAY-123"
  }
}
```

---

### StartMessageActivity

Creates an activity for messaging operations (publish/consume).

```csharp
public static Activity? StartMessageActivity(
    string messageType,
    string destination,
    IGridContext gridContext,
    ActivityKind kind = ActivityKind.Producer)
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `messageType` | `string` | Message type (OrderCreated, PaymentCompleted, etc.) |
| `destination` | `string` | Queue/topic name |
| `gridContext` | `IGridContext` | Grid context |
| `kind` | `ActivityKind` | Producer (publishing) or Consumer (receiving) |

**Additional Tags:**

| Tag | Example |
|-----|---------|
| `messaging.message_type` | `OrderCreated` |
| `messaging.destination` | `orders-queue` |

**Producer Example:**

```csharp
public class OrderEventPublisher(
    IGridContext gridContext,
    IMessageBus messageBus)
{
    public async Task PublishOrderCreatedAsync(Order order)
    {
        using var activity = GridActivitySource.StartMessageActivity(
            "OrderCreated",
            "orders-queue",
            gridContext,
            ActivityKind.Producer);
        
        activity?.SetTag("order.id", order.Id);
        
        try
        {
            await messageBus.PublishAsync("orders-queue", new OrderCreatedEvent
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAtUtc
            });
            
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

**Consumer Example:**

```csharp
public class OrderEventConsumer(IGridContext gridContext)
{
    public async Task HandleOrderCreatedAsync(OrderCreatedEvent evt)
    {
        using var activity = GridActivitySource.StartMessageActivity(
            "OrderCreated",
            "orders-queue",
            gridContext,
            ActivityKind.Consumer);
        
        activity?.SetTag("order.id", evt.OrderId);
        
        try
        {
            await ProcessOrderAsync(evt);
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

---

## Helper Methods

### RecordException

Records an exception on an activity with standard exception tags.

```csharp
public static void RecordException(Activity? activity, Exception exception)
```

**Tags Added:**

| Tag | Example |
|-----|---------|
| `exception.type` | `System.InvalidOperationException` |
| `exception.message` | `Payment gateway unavailable` |
| `exception.stacktrace` | (full stack trace) |

**Also Sets:** `activity.Status = ActivityStatusCode.Error`

**Example:**

```csharp
try
{
    await ProcessPaymentAsync(payment);
}
catch (Exception ex)
{
    GridActivitySource.RecordException(activity, ex);
    throw;
}
```

---

### SetSuccess

Marks an activity as successful.

```csharp
public static void SetSuccess(Activity? activity)
```

**Sets:** `activity.Status = ActivityStatusCode.Ok`

**Example:**

```csharp
var result = await ProcessPaymentAsync(payment);
GridActivitySource.SetSuccess(activity);
return result;
```

---

## OpenTelemetry Configuration

### Registering the ActivitySource

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "payment-service",
            serviceVersion: "2.1.0"))
    .WithTracing(tracing => tracing
        // Add HoneyDrunk Grid source
        .AddSource(GridActivitySource.SourceName)
        
        // Add ASP.NET Core instrumentation
        .AddAspNetCoreInstrumentation()
        
        // Add HTTP client instrumentation
        .AddHttpClientInstrumentation()
        
        // Export to console (dev)
        .AddConsoleExporter()
        
        // Export to OTLP (production)
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
        }));
```

---

## Trace Visualization

### Jaeger UI Example

```
TraceID: 4bf92f3577b34da6a3ce929d0e0e4736
Duration: 250ms

┌─ HTTP POST /api/orders (api-gateway) - 250ms
│  ├─ ProcessOrder (order-service) - 200ms
│  │  ├─ DB query Orders (order-service) - 15ms
│  │  ├─ Message OrderCreated (order-service) - 5ms
│  │  └─ HTTP POST /api/payments (payment-service) - 150ms
│  │     ├─ ProcessPayment (payment-service) - 145ms
│  │     │  ├─ DB query Payments (payment-service) - 10ms
│  │     │  └─ HTTP POST /charge (stripe-api) - 120ms
│  │     └─ Message PaymentCompleted (payment-service) - 5ms
│  └─ HTTP POST /notifications (notification-service) - 30ms

All spans share:
- hd.correlation_id: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
- hd.studio_id: honeycomb-prod
- hd.environment: production
```

### Query by Grid Context

```
# All traces for a specific correlation
hd.correlation_id="01HQXZ8K4TJ9X5B3N2YGF7WDCQ"

# All traces from a specific Node
hd.node_id="payment-service"

# All traces in an environment
hd.environment="production"

# All traces with specific baggage
hd.baggage.TenantId="01HQXZ7G5FH4C2B1N0XD5EWARP"
```

---

## Complete Example

### Service with Full Tracing

```csharp
public class OrderService(
    IGridContext gridContext,
    IOrderRepository repository,
    IPaymentGateway paymentGateway,
    IEventPublisher eventPublisher,
    ILogger<OrderService> logger)
{
    public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
    {
        using var activity = GridActivitySource.StartActivity(
            "CreateOrder",
            gridContext,
            tags: new[]
            {
                new KeyValuePair<string, object?>("order.items_count", request.Items.Count),
                new KeyValuePair<string, object?>("order.total_amount", request.TotalAmount)
            });
        
        try
        {
            logger.LogInformation("Creating order with {ItemCount} items", request.Items.Count);
            
            // 1. Save order to database
            Order order;
            using (var dbActivity = GridActivitySource.StartDatabaseActivity("command", "Orders", gridContext))
            {
                order = await repository.CreateAsync(request);
                GridActivitySource.SetSuccess(dbActivity);
            }
            
            activity?.SetTag("order.id", order.Id);
            
            // 2. Process payment
            PaymentResult payment;
            using (var paymentActivity = GridActivitySource.StartActivity("ProcessPayment", gridContext, ActivityKind.Client))
            {
                payment = await paymentGateway.ChargeAsync(order.TotalAmount);
                GridActivitySource.SetSuccess(paymentActivity);
            }
            
            // 3. Publish event
            using (var messageActivity = GridActivitySource.StartMessageActivity(
                "OrderCreated", "orders-queue", gridContext, ActivityKind.Producer))
            {
                await eventPublisher.PublishAsync(new OrderCreatedEvent
                {
                    OrderId = order.Id,
                    PaymentId = payment.Id
                });
                GridActivitySource.SetSuccess(messageActivity);
            }
            
            GridActivitySource.SetSuccess(activity);
            
            return new OrderResult { OrderId = order.Id, PaymentId = payment.Id };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order");
            GridActivitySource.RecordException(activity, ex);
            throw;
        }
    }
}
```

**Resulting Trace:**

```
CreateOrder (250ms) ✓
├─ DB command Orders (15ms) ✓
├─ ProcessPayment (150ms) ✓
└─ Message OrderCreated (5ms) ✓

Tags:
- hd.correlation_id: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
- hd.node_id: order-service
- order.items_count: 3
- order.total_amount: 299.99
- order.id: ORD-123
```

---

## Activity Naming Conventions

### Recommended Patterns

| Operation Type | Naming Pattern | Example |
|----------------|----------------|---------|
| **Business Operation** | `{Verb}{Noun}` | `CreateOrder`, `ProcessPayment` |
| **HTTP Request** | `HTTP {METHOD} {PATH}` | `HTTP POST /api/orders` |
| **Database Query** | `DB query {TABLE}` | `DB query Orders` |
| **Database Command** | `DB command {TABLE}` | `DB command Payments` |
| **Message Publish** | `Message {TYPE}` | `Message OrderCreated` |
| **External API** | `{SERVICE} {OPERATION}` | `Stripe ChargeCard` |

---

## Best Practices

### ✅ DO

```csharp
// Use 'using' to ensure disposal
using var activity = GridActivitySource.StartActivity("ProcessPayment", gridContext);

// Always call SetSuccess or RecordException
try
{
    var result = await ProcessAsync();
    GridActivitySource.SetSuccess(activity);
    return result;
}
catch (Exception ex)
{
    GridActivitySource.RecordException(activity, ex);
    throw;
}

// Add meaningful tags
activity?.SetTag("payment.amount", amount);
activity?.SetTag("payment.provider", "stripe");

// Use semantic conventions
activity?.SetTag("http.method", "POST");
activity?.SetTag("db.operation", "query");
```

### ❌ DON'T

```csharp
// Don't forget to dispose
var activity = GridActivitySource.StartActivity("...", gridContext);
// Missing using or .Dispose()!

// Don't forget to record outcome
var activity = GridActivitySource.StartActivity("...", gridContext);
await ProcessAsync();
// Missing SetSuccess or RecordException!

// Don't add PII to tags
activity?.SetTag("user.password", password); // ❌ Security risk!
activity?.SetTag("credit_card", cardNumber); // ❌ PCI violation!

// Don't use inconsistent naming
activity = GridActivitySource.StartActivity("payment_process", ...); // ❌ snake_case
activity = GridActivitySource.StartActivity("PROCESSPAYMENT", ...); // ❌ UPPERCASE
```

---

## Testing

### Unit Testing with Activities

```csharp
[Fact]
public async Task ProcessPayment_CreatesActivity()
{
    // Arrange
    var activityListener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == GridActivitySource.SourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
    };
    
    ActivitySource.AddActivityListener(activityListener);
    
    var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
    var service = new PaymentService(gridContext);
    
    // Act
    Activity? capturedActivity = null;
    activityListener.ActivityStarted = activity => capturedActivity = activity;
    
    await service.ProcessAsync(new PaymentRequest { Amount = 100 });
    
    // Assert
    Assert.NotNull(capturedActivity);
    Assert.Equal("ProcessPayment", capturedActivity.DisplayName);
    Assert.Equal("corr-123", capturedActivity.GetTagItem("hd.correlation_id"));
    Assert.Equal(ActivityStatusCode.Ok, capturedActivity.Status);
}
```

---

## Summary

| Component | Purpose | Returns |
|-----------|---------|---------|
| **GridActivitySource.Instance** | ActivitySource for Grid operations | `ActivitySource` |
| **StartActivity** | Create generic activity | `Activity?` |
| **StartHttpActivity** | Create HTTP activity | `Activity?` |
| **StartDatabaseActivity** | Create database activity | `Activity?` |
| **StartMessageActivity** | Create messaging activity | `Activity?` |
| **RecordException** | Mark activity as failed | `void` |
| **SetSuccess** | Mark activity as successful | `void` |

**Key Benefits:**
- ✅ Automatic Grid context enrichment
- ✅ OpenTelemetry-compatible (standard Activity API)
- ✅ Consistent activity naming
- ✅ Semantic tagging (HTTP, DB, messaging)
- ✅ Zero external dependencies
- ✅ Works with Jaeger, Zipkin, Grafana, Azure Monitor, etc.

**OpenTelemetry Exporters:**
- Console (development)
- OTLP (production - Jaeger, Tempo, etc.)
- Zipkin
- Azure Monitor
- AWS X-Ray
- Google Cloud Trace

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
