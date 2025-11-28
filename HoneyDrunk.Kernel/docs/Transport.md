# 🚚 Transport - Context Propagation Across Boundaries

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ITransportEnvelopeBinder.cs](#itransportenvelopebindercs)
- [Built-in Implementations](#built-in-implementations)
  - [HttpResponseBinder](#httpresponsebinder)
  - [MessagePropertiesBinder](#messagepropertiesbinder)
  - [JobMetadataBinder](#jobmetadatabinder)
- [GridHeaderNames](#gridheadernames-updated)
- [Registration](#registration)
- [Custom Transport Binders](#custom-transport-binders)
- [Context Flow Example](#context-flow-example)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Transport abstractions enable automatic GridContext propagation across different transport mechanisms (HTTP, messaging, background jobs) without coupling to specific transport implementations.

**What Transport Does:** Kernel transport binders handle **outbound context propagation** (writing GridContext into envelopes). **Inbound extraction** is handled by context factories and middleware (see [Context.md](Context.md) and [Hosting.md](Hosting.md)).

**Location:** `HoneyDrunk.Kernel.Abstractions/Transport/`

**Key Concepts:**
- **Transport Envelope Binder** - Binds GridContext to transport-specific envelopes
- **Protocol-Agnostic** - Works with HTTP, gRPC, messaging, jobs, etc.
- **Automatic Propagation** - Context flows transparently across Node boundaries
- **Type-Safe** - Compile-time verification of envelope types
- **Outbound Only** - Binders write context; middleware/factories read it

[↑ Back to top](#table-of-contents)

---

## ITransportEnvelopeBinder.cs

### What it is
Interface for binding GridContext to transport-specific envelopes (HTTP responses, message properties, job metadata).

### Real-world analogy
Like an address label printer - automatically stamps correlation information onto any outgoing package, regardless of delivery method (mail, courier, drone).

### Interface Definition

```csharp
public interface ITransportEnvelopeBinder
{
    /// <summary>
    /// Gets the transport type this binder handles (e.g., "http", "grpc", "message", "job").
    /// </summary>
    string TransportType { get; }

    /// <summary>
    /// Binds GridContext to the transport envelope.
    /// </summary>
    void Bind(object envelope, IGridContext context);

    /// <summary>
    /// Determines if this binder can handle the given envelope type.
    /// </summary>
    bool CanBind(object envelope);
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `TransportType` | `string` | Identifies the transport mechanism ("http", "message", "job", "grpc") |

### Methods

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `Bind` | `envelope`, `context` | `void` | Writes GridContext data into the envelope |
| `CanBind` | `envelope` | `bool` | Checks if this binder supports the envelope type |

---

## Built-in Implementations

### HttpResponseBinder

**Location:** `HoneyDrunk.Kernel/Transport/HttpResponseBinder.cs`

Binds GridContext to HTTP response headers for client-side tracing.

```csharp
public class HttpResponseBinder : ITransportEnvelopeBinder
{
    public string TransportType => "http";
    
    public bool CanBind(object envelope) => envelope is HttpResponse;
    
    public void Bind(object envelope, IGridContext context)
    {
        var response = (HttpResponse)envelope;
        
        // Use canonical header names from GridHeaderNames
        response.Headers[GridHeaderNames.CorrelationId] = context.CorrelationId;
        response.Headers[GridHeaderNames.NodeId] = context.NodeId;
        
        if (context.CausationId is not null)
            response.Headers[GridHeaderNames.CausationId] = context.CausationId;
        
        // Baggage with standard prefix
        foreach (var (key, value) in context.Baggage)
            response.Headers[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
    }
}
```

**Headers Written:**

| Header | Source | Example |
|--------|--------|---------|
| `X-Correlation-Id` | `context.CorrelationId` | `01HQXZ8K4TJ9X5B3N2YGF7WDCQ` |
| `X-Causation-Id` | `context.CausationId` | `01HQXY7J3SI8W4A2M1XE6UFBZP` |
| `X-Node-Id` | `context.NodeId` | `payment-node` |
| `X-Baggage-{key}` | `context.Baggage[key]` | `X-Baggage-TenantId: 01HQXZ...` |

**Usage Example:**

```csharp
public class PaymentController(
    IGridContext gridContext,
    IEnumerable<ITransportEnvelopeBinder> binders)
{
    [HttpPost("/payments")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] PaymentRequest request,
        HttpContext httpContext)
    {
        var result = await _paymentService.ProcessAsync(request);
        
        // Automatically bind context to response
        var httpBinder = binders.First(b => b.TransportType == "http");
        httpBinder.Bind(httpContext.Response, gridContext);
        
        return Ok(result);
    }
}
```

**Client Receives:**

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
X-Node-Id: payment-node
X-Baggage-TenantId: 01HQXZ7G5FH4C2B1N0XD5EWARP

{ "paymentId": "PAY-123", "status": "completed" }
```

[↑ Back to top](#table-of-contents)

---

### MessagePropertiesBinder

**Location:** `HoneyDrunk.Kernel/Transport/MessagePropertiesBinder.cs`

Binds GridContext to message properties for event-driven architectures.

**Envelope Shape:** `IDictionary<string, object>` (AMQP-style headers used by RabbitMQ, Azure Service Bus, AWS SQS, etc.)

```csharp
public class MessagePropertiesBinder : ITransportEnvelopeBinder
{
    public string TransportType => "message";
    
    public bool CanBind(object envelope) => envelope is IDictionary<string, object>;
    
    public void Bind(object envelope, IGridContext context)
    {
        var properties = (IDictionary<string, object>)envelope;
        
        // Use canonical header names
        properties[GridHeaderNames.CorrelationId] = context.CorrelationId;
        properties[GridHeaderNames.NodeId] = context.NodeId;
        properties[GridHeaderNames.StudioId] = context.StudioId;
        properties[GridHeaderNames.Environment] = context.Environment;
        
        if (context.CausationId is not null)
            properties[GridHeaderNames.CausationId] = context.CausationId;
        
        foreach (var (key, value) in context.Baggage)
            properties[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
    }
}
```

**Example: RabbitMQ (AMQP):**

```csharp
public class OrderEventPublisher(
    IGridContext gridContext,
    ITransportEnvelopeBinder messageBinder,
    IConnection rabbitConnection)
{
    public async Task PublishOrderCreatedAsync(Order order)
    {
        using var channel = rabbitConnection.CreateModel();
        
        var properties = channel.CreateBasicProperties();
        var messageProps = new Dictionary<string, object>();
        
        // Bind Grid context to message properties (shape: IDictionary<string, object>)
        messageBinder.Bind(messageProps, gridContext);
        
        // Copy to RabbitMQ properties
        properties.Headers = messageProps;
        
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            OrderId = order.Id,
            CreatedAt = order.CreatedAtUtc
        });
        
        channel.BasicPublish(
            exchange: "orders",
            routingKey: "order.created",
            basicProperties: properties,
            body: body);
    }
}
```

**Message Properties:**

```json
{
  "headers": {
    "X-Correlation-Id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Causation-Id": "01HQXY7J3SI8W4A2M1XE6UFBZP",
    "X-Node-Id": "order-service",
    "X-Studio-Id": "honeycomb",
    "X-Environment": "production",
    "X-Baggage-TenantId": "01HQXZ7G5FH4C2B1N0XD5EWARP"
  },
  "body": {
    "orderId": "ORD-123",
    "createdAt": "2025-01-11T10:30:00Z"
  }
}
```

**Consumer Receives Context:**

```csharp
public class OrderEventConsumer(IGridContextAccessor contextAccessor)
{
    public async Task HandleAsync(BasicDeliverEventArgs ea)
    {
        // Extract context from message headers (inbound extraction, not handled by binder)
        var correlationId = ea.BasicProperties.Headers[GridHeaderNames.CorrelationId]?.ToString();
        var nodeId = ea.BasicProperties.Headers[GridHeaderNames.NodeId]?.ToString();
        // ... extract other properties
        
        var gridContext = new GridContext(correlationId, nodeId, studioId, environment);
        contextAccessor.GridContext = gridContext;
        
        // Process message with context available
        await ProcessOrderCreatedAsync(ea.Body);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### JobMetadataBinder

**Location:** `HoneyDrunk.Kernel/Transport/JobMetadataBinder.cs`

Binds GridContext to background job metadata for async processing.

**Envelope Shape:** `IDictionary<string, string>` (string metadata used by Hangfire, Quartz, Azure Functions, etc.)

```csharp
public class JobMetadataBinder : ITransportEnvelopeBinder
{
    public string TransportType => "job";
    
    public bool CanBind(object envelope) => envelope is IDictionary<string, string>;
    
    public void Bind(object envelope, IGridContext context)
    {
        var metadata = (IDictionary<string, string>)envelope;
        
        // Use canonical header names
        metadata[GridHeaderNames.CorrelationId] = context.CorrelationId;
        metadata[GridHeaderNames.NodeId] = context.NodeId;
        metadata[GridHeaderNames.StudioId] = context.StudioId;
        metadata[GridHeaderNames.Environment] = context.Environment;
        metadata["CreatedAtUtc"] = context.CreatedAtUtc.ToString("O");
        
        if (context.CausationId is not null)
            metadata[GridHeaderNames.CausationId] = context.CausationId;
        
        foreach (var (key, value) in context.Baggage)
            metadata[$"{GridHeaderNames.BaggagePrefix}{key}"] = value;
    }
}
```

**Example: Hangfire (Background Jobs):**

```csharp
public class ReportGenerator(
    IGridContext gridContext,
    ITransportEnvelopeBinder jobBinder,
    IBackgroundJobClient jobClient)
{
    public string ScheduleReportGeneration(string reportId)
    {
        var metadata = new Dictionary<string, string>();
        
        // Bind Grid context to job metadata (shape: IDictionary<string, string>)
        jobBinder.Bind(metadata, gridContext);
        
        var jobId = jobClient.Enqueue(() => 
            GenerateReportAsync(reportId, metadata));
        
        return jobId;
    }
    
    [Queue("reports")]
    public async Task GenerateReportAsync(
        string reportId,
        Dictionary<string, string> metadata)
    {
        // Reconstruct Grid context from metadata (inbound extraction)
        var correlationId = metadata[GridHeaderNames.CorrelationId];
        var nodeId = metadata[GridHeaderNames.NodeId];
        var studioId = metadata[GridHeaderNames.StudioId];
        var environment = metadata[GridHeaderNames.Environment];
        
        var baggage = metadata
            .Where(kvp => kvp.Key.StartsWith(GridHeaderNames.BaggagePrefix))
            .ToDictionary(
                kvp => kvp.Key.Replace(GridHeaderNames.BaggagePrefix, ""),
                kvp => kvp.Value);
        
        var gridContext = new GridContext(
            correlationId, nodeId, studioId, environment,
            baggage: baggage);
        
        // Process with context available
        await GenerateReportInternalAsync(reportId, gridContext);
    }
}
```

**Job Metadata:**

```json
{
  "jobId": "job-12345",
  "queue": "reports",
  "metadata": {
    "X-Correlation-Id": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Causation-Id": "01HQXY7J3SI8W4A2M1XE6UFBZP",
    "X-Node-Id": "report-service",
    "X-Studio-Id": "honeycomb",
    "X-Environment": "production",
    "CreatedAtUtc": "2025-01-11T10:30:00.0000000Z",
    "X-Baggage-TenantId": "01HQXZ7G5FH4C2B1N0XD5EWARP",
    "X-Baggage-ReportType": "sales-summary"
  }
}
```

[↑ Back to top](#table-of-contents)

---

## GridHeaderNames (Updated)

**Location:** `HoneyDrunk.Kernel.Abstractions/Context/GridHeaderNames.cs`

Standard header names for Grid context propagation. Updated in v0.3.0 to include baggage and environment headers.

```csharp
public static class GridHeaderNames
{
    /// <summary>
    /// Correlation identifier (ULID or external trace id).
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";
    
    /// <summary>
    /// Causation identifier referencing the parent correlation id.
    /// </summary>
    public const string CausationId = "X-Causation-Id";
    
    /// <summary>
    /// Studio identifier owning the execution.
    /// </summary>
    public const string StudioId = "X-Studio-Id";
    
    /// <summary>
    /// Node identifier executing the request.
    /// </summary>
    public const string NodeId = "X-Node-Id";
    
    /// <summary>
    /// Environment identifier (e.g., production, staging, development). [NEW in v0.3.0]
    /// </summary>
    public const string Environment = "X-Environment";
    
    /// <summary>
    /// W3C traceparent header (for interoperability).
    /// </summary>
    public const string TraceParent = "traceparent";
    
    /// <summary>
    /// W3C baggage header containing comma-separated key=value pairs.
    /// </summary>
    public const string Baggage = "baggage";
    
    /// <summary>
    /// Prefix for custom baggage headers (e.g., X-Baggage-TenantId). [NEW in v0.3.0]
    /// </summary>
    public const string BaggagePrefix = "X-Baggage-";
}
```

---

## Registration

Transport binders are automatically registered by `AddHoneyDrunkNode()`:

```csharp
public static IHoneyDrunkBuilder AddHoneyDrunkNode(
    this IServiceCollection services,
    Action<HoneyDrunkNodeOptions> configure)
{
    // ... other registrations ...
    
    // Transport envelope binders for context propagation
    services.AddSingleton<ITransportEnvelopeBinder, HttpResponseBinder>();
    services.AddSingleton<ITransportEnvelopeBinder, MessagePropertiesBinder>();
    services.AddSingleton<ITransportEnvelopeBinder, JobMetadataBinder>();
    
    return new HoneyDrunkBuilder(services);
}
```

---

## Custom Transport Binders

### Creating a Custom Binder

Example: gRPC metadata binder

```csharp
public class GrpcMetadataBinder : ITransportEnvelopeBinder
{
    public string TransportType => "grpc";
    
    public bool CanBind(object envelope) => envelope is Metadata;
    
    public void Bind(object envelope, IGridContext context)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(context);
        
        if (envelope is not Metadata metadata)
        {
            throw new ArgumentException(
                $"Expected Metadata but got {envelope.GetType().Name}",
                nameof(envelope));
        }
        
        // Add Grid context to gRPC metadata
        metadata.Add("x-correlation-id", context.CorrelationId);
        metadata.Add("x-node-id", context.NodeId);
        metadata.Add("x-studio-id", context.StudioId);
        metadata.Add("x-environment", context.Environment);
        
        if (context.CausationId is not null)
            metadata.Add("x-causation-id", context.CausationId);
        
        // Add baggage
        foreach (var (key, value) in context.Baggage)
            metadata.Add($"x-baggage-{key.ToLowerInvariant()}", value);
    }
}
```

**Register Custom Binder:**

```csharp
builder.Services.AddHoneyDrunkNode(options => { /* ... */ })
    .Services.AddSingleton<ITransportEnvelopeBinder, GrpcMetadataBinder>();
```

**Usage:**

```csharp
public class PaymentGrpcService(
    IGridContext gridContext,
    IEnumerable<ITransportEnvelopeBinder> binders) : Payment.PaymentBase
{
    public override async Task<PaymentResponse> ProcessPayment(
        PaymentRequest request,
        ServerCallContext context)
    {
        var result = await ProcessPaymentInternalAsync(request);
        
        // Bind Grid context to gRPC response metadata
        var grpcBinder = binders.First(b => b.TransportType == "grpc");
        grpcBinder.Bind(context.ResponseTrailers, gridContext);
        
        return result;
    }
}
```

---

## Context Flow Example

### End-to-End Propagation

```
User Request → API Gateway → Order Service → Payment Service → Notification Service
             ↓              ↓               ↓                 ↓
          [HTTP Headers] [Message Props] [Job Metadata]  [HTTP Headers]
             ↓              ↓               ↓                 ↓
         CorrelationId: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ (same across all)
```

**1. API Gateway receives request:**

```http
POST /orders HTTP/1.1
Host: api.example.com
Content-Type: application/json
```

**2. API Gateway creates GridContext and forwards:**

```http
POST /orders HTTP/1.1
Host: order-service
X-Correlation-ID: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
X-Node-ID: api-gateway
X-Studio-ID: honeycomb
X-Environment: production
```

**3. Order Service publishes event:**

```json
{
  "headers": {
    "X-Correlation-ID": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Causation-ID": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Node-ID": "order-service"
  },
  "body": { "orderId": "ORD-123" }
}
```

**4. Payment Service schedules background job:**

```json
{
  "metadata": {
    "X-Correlation-ID": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Causation-ID": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
    "X-Node-ID": "payment-service"
  }
}
```

**5. Notification Service calls external API:**

```http
POST /send HTTP/1.1
Host: notification-api
X-Correlation-ID: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
X-Causation-ID: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ
X-Node-ID: notification-service
```

**All logs share the same CorrelationId → Full distributed trace!**

---

## Testing Patterns

```csharp
[Fact]
public void HttpResponseBinder_BindsContext()
{
    // Arrange
    var binder = new HttpResponseBinder();
    var context = new GridContext("corr-123", "test-node", "test-studio", "dev");
    var response = new DefaultHttpContext().Response;
    
    // Act
    binder.Bind(response, context);
    
    // Assert
    Assert.Equal("corr-123", response.Headers["X-Correlation-ID"]);
    Assert.Equal("test-node", response.Headers["X-Node-ID"]);
}

[Fact]
public void MessagePropertiesBinder_CanBind_ReturnsTrueForDictionary()
{
    // Arrange
    var binder = new MessagePropertiesBinder();
    var envelope = new Dictionary<string, object>();
    
    // Act
    var canBind = binder.CanBind(envelope);
    
    // Assert
    Assert.True(canBind);
}

[Fact]
public void JobMetadataBinder_BindsCreatedAtUtc()
{
    // Arrange
    var binder = new JobMetadataBinder();
    var context = new GridContext("corr-123", "test-node", "test-studio", "dev");
    var metadata = new Dictionary<string, string>();
    
    // Act
    binder.Bind(metadata, context);
    
    // Assert
    Assert.Contains("CreatedAtUtc", metadata.Keys);
    Assert.True(DateTimeOffset.TryParse(metadata["CreatedAtUtc"], out _));
}
```

---

## Summary

| Binder | Transport | Envelope Type | Use Case |
|--------|-----------|---------------|----------|
| **HttpResponseBinder** | HTTP/REST | `HttpResponse` | API responses |
| **MessagePropertiesBinder** | Messaging | `IDictionary<string, object>` | RabbitMQ, Azure Service Bus, AWS SQS |
| **JobMetadataBinder** | Background Jobs | `IDictionary<string, string>` | Hangfire, Quartz, Azure Functions |

**Key Benefits:**
- Automatic context propagation across all transport types
- Protocol-agnostic design
- Easy to extend with custom binders
- Type-safe with compile-time validation
- Zero manual header/property management

**Best Practices:**
- Always bind context before sending responses/messages/jobs
- Use standard header names from `GridHeaderNames`
- Reconstruct context on the receiving end
- Include `CausationId` for proper causality chains
- Filter sensitive baggage before propagation

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
