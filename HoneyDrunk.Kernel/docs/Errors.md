# ⚠️ Errors - Exception Hierarchy and Error Handling

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [HoneyDrunkException.cs](#honeydrunkexceptioncs)
- [Typed Exception Hierarchy](#typed-exception-hierarchy)
  - [ValidationException.cs](#validationexceptioncs)
  - [NotFoundException.cs](#notfoundexceptioncs)
  - [SecurityException.cs](#securityexceptioncs)
  - [ConcurrencyException.cs](#concurrencyexceptioncs)
  - [DependencyFailureException.cs](#dependencyfailureexceptioncs)
- [ErrorCode.cs](#errorcodecs)
- [ErrorClassification.cs](#errorclassificationcs)
- [IErrorClassifier.cs](#ierrorclassifiercs)
- [Complete Error Handling Example](#complete-error-handling-example)
- [Best Practices](#best-practices)
- [Error Code Taxonomy](#error-code-taxonomy)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

The Errors subsystem provides a structured exception hierarchy, error classification, and transport-friendly error mapping for distributed systems. Every exception carries Grid identity primitives (CorrelationId, NodeId, EnvironmentId) for cross-process correlation and debugging.

**Location:** `HoneyDrunk.Kernel.Abstractions/Errors/`

**Key Concepts:**
- **HoneyDrunkException** - Base exception with Grid identity metadata
- **Typed Exceptions** - Domain-specific exception types
- **ErrorCode** - Structured, taxonomy-based error identifiers
- **ErrorClassification** - Transport-agnostic error mapping
- **IErrorClassifier** - Service for classifying exceptions into HTTP status codes

---

## HoneyDrunkException.cs

### What it is
Base exception for all Kernel semantics. Carries Grid identity primitives for cross-process correlation and structured error codes.

### Real-world analogy
Like a medical record - every error has an ID, timestamp, and location where it occurred, making it traceable across the entire system.

### Properties

```csharp
public class HoneyDrunkException : Exception
{
    public CorrelationId? CorrelationId { get; }  // ULID of the failing operation
    public ErrorCode? ErrorCode { get; }           // Structured taxonomy code
    public NodeId? NodeId { get; }                 // Where error originated
    public EnvironmentId? EnvironmentId { get; }   // Environment (prod, staging, etc.)
}
```

**Design:** In most cases you should construct `HoneyDrunkException` from the current `IGridContext` and `INodeContext` so the error stays aligned with the active Grid context. **Avoid hardcoding NodeId/EnvironmentId as string literals** - pull them from Grid context.

### Constructor Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `message` | `string` | ✅ Yes | Human-readable error message |
| `correlationId` | `CorrelationId?` | ❌ No | Correlation ID for tracing (from `IGridContext`) |
| `errorCode` | `ErrorCode?` | ❌ No | Structured error code |
| `nodeId` | `NodeId?` | ❌ No | Node where error occurred (from `IGridContext` or `INodeContext`) |
| `environmentId` | `EnvironmentId?` | ❌ No | Environment identifier (from `IGridContext`) |
| `innerException` | `Exception?` | ❌ No | Underlying cause |

### Usage Example

```csharp
public class OrderService(IGridContext gridContext, INodeContext nodeContext)
{
    public async Task ProcessOrderAsync(Order order)
    {
        try
        {
            await ValidateAndSaveAsync(order);
        }
        catch (Exception ex)
        {
            // Pull Grid identity from context, not literals
            throw new HoneyDrunkException(
                message: "Order processing failed",
                correlationId: new CorrelationId(gridContext.CorrelationId),
                errorCode: new ErrorCode("order.processing.failed"),
                nodeId: new NodeId(gridContext.NodeId),
                environmentId: new EnvironmentId(gridContext.Environment),
                innerException: ex
            );
        }
    }
}
```

**Log Output:**
```json
{
  "level": "Error",
  "message": "Order processing failed",
  "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
  "errorCode": "order.processing.failed",
  "nodeId": "order-service",
  "environment": "production",
  "stackTrace": "..."
}
```

[↑ Back to top](#table-of-contents)

---

## Typed Exception Hierarchy

**When to Use:**
- **ValidationException** - Anything the caller can fix by changing input (invalid email, negative amount, missing required field)
- **NotFoundException** - Missing resources (order not found, user not found)
- **SecurityException** - Authentication/authorization problems (expired token, missing permission)
- **ConcurrencyException** - Optimistic concurrency failures, uniqueness violations, duplicate keys
- **DependencyFailureException** - External systems breaking (database timeout, API unavailable, queue full)

### ValidationException.cs

Thrown when input validation or business rule validation fails.

```csharp
public class ValidationException : HoneyDrunkException
{
    public ValidationException(string message, ErrorCode? errorCode = null) 
        : base(message, errorCode: errorCode ?? ErrorCode.WellKnown.ValidationInput) { }
}
```

**Usage:**
```csharp
if (string.IsNullOrWhiteSpace(order.CustomerId))
{
    throw new ValidationException(
        "CustomerId is required",
        ErrorCode.WellKnown.ValidationInput
    );
}

if (order.TotalAmount < 0)
{
    throw new ValidationException(
        "Order total cannot be negative",
        ErrorCode.WellKnown.ValidationBusiness
    );
}
```

**Transport Mapping:** HTTP 400 Bad Request

---

### NotFoundException.cs

Thrown when a requested resource does not exist.

```csharp
public class NotFoundException : HoneyDrunkException
{
    public NotFoundException(string message, ErrorCode? errorCode = null) 
        : base(message, errorCode: errorCode ?? ErrorCode.WellKnown.ResourceNotFound) { }
}
```

**Usage:**
```csharp
var order = await _repository.GetByIdAsync(orderId);
if (order is null)
{
    throw new NotFoundException(
        $"Order {orderId} not found",
        ErrorCode.WellKnown.ResourceNotFound
    );
}
```

**Transport Mapping:** HTTP 404 Not Found

---

### SecurityException.cs

Thrown when authorization or authentication fails.

```csharp
public class SecurityException : HoneyDrunkException
{
    public SecurityException(string message, ErrorCode? errorCode = null) 
        : base(message, errorCode: errorCode ?? ErrorCode.WellKnown.AuthorizationFailure) { }
}
```

**Usage:**
```csharp
// Authorization failure (user lacks permission)
if (!user.HasPermission("orders.delete"))
{
    throw new SecurityException(
        "User does not have permission to delete orders",
        ErrorCode.WellKnown.AuthorizationFailure
    );
}

// Authentication failure (user identity not verified)
if (!IsAuthenticated())
{
    throw new SecurityException(
        "User not authenticated",
        ErrorCode.WellKnown.AuthenticationFailure
    );
}
```

**Transport Mapping:** 
- HTTP 403 Forbidden (authorization failure - `authorization.failure`)
- HTTP 401 Unauthorized (authentication failure - `authentication.failure`)

**Note:** The default `IErrorClassifier` maps `SecurityException` to HTTP 403. For HTTP 401 responses, throw with `ErrorCode.WellKnown.AuthenticationFailure` and implement custom classifier logic to check the error code.

---

### ConcurrencyException.cs

Thrown when a resource conflict occurs (duplicate, optimistic concurrency failure, constraint violation).

```csharp
public class ConcurrencyException : HoneyDrunkException
{
    public ConcurrencyException(string message, ErrorCode? errorCode = null) 
        : base(message, errorCode: errorCode ?? ErrorCode.WellKnown.ResourceConflict) { }
}
```

**Usage:**
```csharp
try
{
    await _repository.SaveAsync(order);
}
catch (DbUpdateConcurrencyException ex)
{
    throw new ConcurrencyException(
        $"Order {order.Id} was modified by another user",
        ErrorCode.WellKnown.ResourceConflict,
        innerException: ex
    );
}

// Duplicate key
if (await _repository.ExistsAsync(order.OrderNumber))
{
    throw new ConcurrencyException(
        $"Order number {order.OrderNumber} already exists",
        ErrorCode.WellKnown.ResourceConflict
    );
}
```

**Transport Mapping:** HTTP 409 Conflict

---

### DependencyFailureException.cs

Thrown when an external dependency (database, API, queue) fails.

```csharp
public class DependencyFailureException : HoneyDrunkException
{
    public DependencyFailureException(string message, ErrorCode? errorCode = null) 
        : base(message, errorCode: errorCode ?? ErrorCode.WellKnown.DependencyUnavailable) { }
}
```

**Usage:**
```csharp
try
{
    var response = await _httpClient.GetAsync("https://api.stripe.com/charges");
    response.EnsureSuccessStatusCode();
}
catch (HttpRequestException ex)
{
    throw new DependencyFailureException(
        "Payment gateway unavailable",
        ErrorCode.WellKnown.DependencyUnavailable,
        innerException: ex
    );
}

try
{
    await _messageBus.PublishAsync(orderEvent, timeout: TimeSpan.FromSeconds(5));
}
catch (TimeoutException ex)
{
    throw new DependencyFailureException(
        "Message bus timeout",
        ErrorCode.WellKnown.DependencyTimeout,
        innerException: ex
    );
}
```

**Transport Mapping:** HTTP 502 Bad Gateway (dependency unavailable) or HTTP 504 Gateway Timeout (timeout)

---

## ErrorCode.cs

### What it is
Strongly-typed, structured error code following a taxonomy pattern (e.g., `validation.input`, `dependency.timeout`).

### Format Rules

- **Segments:** Dot-separated segments (`category.subcategory.detail`)
- **Characters:** Lowercase alphanumeric only (`a-z`, `0-9`)
- **Length:** Each segment 1-32 chars, total max 128 chars
- **Example:** `validation.input.required`, `dependency.timeout.database`

### Well-Known Error Codes

```csharp
ErrorCode.WellKnown.ValidationInput           // "validation.input"
ErrorCode.WellKnown.ValidationBusiness        // "validation.business"
ErrorCode.WellKnown.AuthenticationFailure     // "authentication.failure"
ErrorCode.WellKnown.AuthorizationFailure      // "authorization.failure"
ErrorCode.WellKnown.DependencyUnavailable     // "dependency.unavailable"
ErrorCode.WellKnown.DependencyTimeout         // "dependency.timeout"
ErrorCode.WellKnown.ResourceNotFound          // "resource.notfound"
ErrorCode.WellKnown.ResourceConflict          // "resource.conflict"
ErrorCode.WellKnown.ConfigurationInvalid      // "configuration.invalid"
ErrorCode.WellKnown.InternalError             // "internal.error"
ErrorCode.WellKnown.RateLimitExceeded         // "ratelimit.exceeded"
```

### Usage Example

```csharp
// Valid error codes
var code1 = new ErrorCode("validation.input.email");
var code2 = new ErrorCode("dependency.timeout.redis");
var code3 = new ErrorCode("authorization.role.missing");

// Validation - valid code
if (ErrorCode.IsValid("validation.input.email", out var error1))
{
    // Valid: all lowercase, alphanumeric, dot-separated
}

// Validation - invalid code
if (!ErrorCode.IsValid("VALIDATION.FAILED", out var error2))
{
    Console.WriteLine(error2); // "Segments must be lowercase alphanumeric only."
}

// TryParse
if (ErrorCode.TryParse("authentication.mfa.required", out var code))
{
    Console.WriteLine(code); // "authentication.mfa.required"
}

// Using well-known codes
throw new NotFoundException(
    "User not found",
    ErrorCode.WellKnown.ResourceNotFound
);
```

### Custom Error Codes

**Best Practice:** Each domain should centralize its error codes into a static class (e.g., `PaymentErrorCodes`) to avoid duplication and typos across the codebase.

```csharp
public static class PaymentErrorCodes
{
    public static readonly ErrorCode CardDeclined = new("payment.card.declined");
    public static readonly ErrorCode CardExpired = new("payment.card.expired");
    public static readonly ErrorCode InsufficientFunds = new("payment.card.insufficientfunds");
    public static readonly ErrorCode FraudSuspected = new("payment.fraud.suspected");
    public static readonly ErrorCode ProcessorUnavailable = new("payment.processor.unavailable");
}

// Usage
if (paymentResult.Declined)
{
    throw new ValidationException(
        "Payment declined by processor",
        PaymentErrorCodes.CardDeclined
    );
}
```

[↑ Back to top](#table-of-contents)

---

## ErrorClassification.cs

### What it is
Transport-specific error classification result used for mapping exceptions to HTTP status codes. **The default `IErrorClassifier` targets HTTP APIs.** Other transports (gRPC, messaging) can provide their own classifier implementations mapping the same exception types to transport-specific codes.

### Properties

```csharp
public sealed class ErrorClassification
{
    public int StatusCode { get; }      // HTTP status code (400, 404, 500, etc.)
    public string Title { get; }        // Short human-readable title
    public string? ErrorCode { get; }   // Structured error code
    public string? TypeUri { get; }     // Documentation URL
}
```

**Design Note:** `StatusCode` is interpreted as an HTTP status code by default. Non-HTTP transports can map this to equivalent semantics (e.g., gRPC status codes, messaging error envelopes).

### Usage Example

```csharp
var classification = new ErrorClassification(
    statusCode: 404,
    title: "Order not found",
    errorCode: "resource.notfound",
    typeUri: "https://docs.honeydrunk.io/errors/not-found"
);
```

[↑ Back to top](#table-of-contents)

---

## IErrorClassifier.cs

### What it is
Service for classifying exceptions into transport-friendly `ErrorClassification` results.

### Interface

```csharp
public interface IErrorClassifier
{
    ErrorClassification? Classify(Exception exception);
}
```

### Implementation: DefaultErrorClassifier

**Location:** `HoneyDrunk.Kernel/Errors/DefaultErrorClassifier.cs`

Maps Kernel exceptions to HTTP status codes:

| Exception Type | HTTP Status | Error Code | Docs URI |
|----------------|-------------|------------|----------|
| `ValidationException` | 400 | `validation.input` | `/errors/validation` |
| `NotFoundException` | 404 | `resource.notfound` | `/errors/not-found` |
| `SecurityException` | 403 | `authorization.failure` | `/errors/security` |
| `ConcurrencyException` | 409 | `resource.conflict` | `/errors/concurrency` |
| `DependencyFailureException` | 502 | `dependency.unavailable` | `/errors/dependency-failure` |
| `HoneyDrunkException` | 500 | `internal.error` | `/errors/internal` |
| `ArgumentException` | 400 | `validation.argument` | `/errors/validation-argument` |
| `TimeoutException` | 504 | `dependency.timeout` | `/errors/dependency-timeout` |

### Usage in Middleware

```csharp
public class ErrorHandlingMiddleware(
    IErrorClassifier errorClassifier,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            
            var classification = errorClassifier.Classify(ex);
            
            if (classification is not null)
            {
                context.Response.StatusCode = classification.StatusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = classification.TypeUri,
                    title = classification.Title,
                    status = classification.StatusCode,
                    errorCode = classification.ErrorCode,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
            }
            else
            {
                // Fallback to generic 500
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Internal Server Error",
                    status = 500,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
            }
        }
    }
}
```

**Response Example:**

```json
{
  "type": "https://docs.honeydrunk.io/errors/not-found",
  "title": "Order 12345 not found",
  "status": 404,
  "errorCode": "resource.notfound",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

[↑ Back to top](#table-of-contents)

---

## Complete Error Handling Example

### Global Middleware (Recommended)

**In most Nodes, global `ErrorHandlingMiddleware` handles exceptions automatically.** Controllers only need to catch specific exceptions when they want to override the default classification or add custom response metadata.

```csharp
public class ErrorHandlingMiddleware(
    IErrorClassifier errorClassifier,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            
            var classification = errorClassifier.Classify(ex);
            
            if (classification is not null)
            {
                context.Response.StatusCode = classification.StatusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = classification.TypeUri,
                    title = classification.Title,
                    status = classification.StatusCode,
                    errorCode = classification.ErrorCode,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
            }
            else
            {
                // Fallback to generic 500
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Internal Server Error",
                    status = 500,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier
                });
            }
        }
    }
}
```

**Response Example:**

```json
{
  "type": "https://docs.honeydrunk.io/errors/not-found",
  "title": "Order 12345 not found",
  "status": 404,
  "errorCode": "resource.notfound",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### Manual Handling (Special Cases)

Controllers can override default classification for special cases (e.g., custom retry logic, rate limit headers):

```csharp
public class OrderController(
    IOrderService orderService,
    IGridContext gridContext,
    ILogger<OrderController> logger) : ControllerBase
{
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(string orderId)
    {
        try
        {
            var order = await orderService.GetByIdAsync(orderId);
            return Ok(order);
        }
        catch (DependencyFailureException ex)
        {
            logger.LogError(ex, "Dependency failure while fetching order {OrderId}", orderId);
            
            // Custom handling: add Retry-After header
            return StatusCode(502, new
            {
                errorCode = ex.ErrorCode?.Value,
                message = "External service unavailable",
                retryAfter = 30
            });
        }
        // Other exceptions handled by global middleware
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Best Practices

### ✅ DO

```csharp
// Use typed exceptions for known failure modes
throw new NotFoundException("Order not found", ErrorCode.WellKnown.ResourceNotFound);

// Include Grid context for correlation
throw new HoneyDrunkException(
    "Processing failed",
    correlationId: gridContext.CorrelationId,
    nodeId: new NodeId("order-service")
);

// Use well-known error codes when applicable
throw new ValidationException("Invalid email", ErrorCode.WellKnown.ValidationInput);

// Wrap external exceptions with Grid context
catch (DbException ex)
{
    throw new DependencyFailureException(
        "Database unavailable",
        ErrorCode.WellKnown.DependencyUnavailable,
        innerException: ex
    );
}

// Log with structured context
logger.LogError(ex,
    "Operation failed for {OrderId} with {CorrelationId}",
    orderId,
    ex.CorrelationId);
```

### ❌ DON'T

```csharp
// Don't throw generic exceptions
throw new Exception("Something failed"); // ❌

// Don't lose Grid context
catch (HoneyDrunkException ex)
{
    throw new Exception(ex.Message); // ❌ Loses CorrelationId, NodeId
}

// Don't use string literals for error codes
throw new ValidationException("Failed", new ErrorCode("VALIDATION_FAILED")); // ❌ Wrong format

// Don't swallow inner exceptions
catch (Exception ex)
{
    throw new HoneyDrunkException("Failed"); // ❌ Missing innerException
}

// Don't expose stack traces to external clients
return Ok(new { error = ex.StackTrace }); // ❌ Security risk
```

**Note:** Internal logs can still capture full exception details (including stack traces) for debugging. Client responses should use error codes and correlation IDs only.

[↑ Back to top](#table-of-contents)

---

## Error Code Taxonomy

### Recommended Structure

```
category.subcategory.detail

Examples:
- validation.input.required
- validation.input.format
- validation.business.rule
- authentication.mfa.required
- authentication.token.expired
- authorization.role.missing
- authorization.permission.denied
- dependency.timeout.database
- dependency.unavailable.cache
- resource.notfound.order
- resource.conflict.duplicate
- ratelimit.exceeded.api
- configuration.invalid.setting
```

### Custom Taxonomy Example

```csharp
public static class PaymentErrorCodes
{
    public static readonly ErrorCode CardDeclined = new("payment.card.declined");
    public static readonly ErrorCode CardExpired = new("payment.card.expired");
    public static readonly ErrorCode InsufficientFunds = new("payment.card.insufficientfunds");
    public static readonly ErrorCode FraudSuspected = new("payment.fraud.suspected");
    public static readonly ErrorCode ProcessorUnavailable = new("payment.processor.unavailable");
}
```

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

```csharp
[Fact]
public void NotFoundException_IncludesErrorCode()
{
    // Arrange & Act
    var ex = new NotFoundException(
        "Order not found",
        ErrorCode.WellKnown.ResourceNotFound
    );
    
    // Assert
    Assert.Equal("Order not found", ex.Message);
    Assert.Equal("resource.notfound", ex.ErrorCode?.Value);
}

[Fact]
public void HoneyDrunkException_CarriesGridContext()
{
    // Arrange
    var correlationId = new CorrelationId(Ulid.NewUlid().ToString());
    var nodeId = new NodeId("test-node");
    
    // Act
    var ex = new HoneyDrunkException(
        "Test failure",
        correlationId: correlationId,
        nodeId: nodeId
    );
    
    // Assert
    Assert.Equal(correlationId, ex.CorrelationId);
    Assert.Equal(nodeId, ex.NodeId);
}

[Fact]
public void ErrorClassifier_MapsNotFoundException_To404()
{
    // Arrange
    var classifier = new DefaultErrorClassifier();
    var exception = new NotFoundException("Not found");
    
    // Act
    var classification = classifier.Classify(exception);
    
    // Assert
    Assert.NotNull(classification);
    Assert.Equal(404, classification.StatusCode);
    Assert.Equal("resource.notfound", classification.ErrorCode);
}

[Fact]
public void ErrorCode_ValidatesFormat()
{
    // Valid
    Assert.True(ErrorCode.IsValid("validation.input", out _));
    Assert.True(ErrorCode.IsValid("auth.mfa.required", out _));
    
    // Invalid
    Assert.False(ErrorCode.IsValid("VALIDATION.FAILED", out var error1)); // Uppercase
    Assert.False(ErrorCode.IsValid("validation-failed", out var error2)); // Hyphen in segment
    Assert.False(ErrorCode.IsValid("", out var error3)); // Empty
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

| Component | Purpose | Transport Mapping |
|-----------|---------|-------------------|
| **HoneyDrunkException** | Base exception with Grid context | 500 Internal Error |
| **ValidationException** | Input/business validation failure | 400 Bad Request |
| **NotFoundException** | Resource not found | 404 Not Found |
| **SecurityException** | Auth/authz failure | 401/403 Unauthorized/Forbidden |
| **ConcurrencyException** | Resource conflict | 409 Conflict |
| **DependencyFailureException** | External dependency failure | 502/504 Bad Gateway/Timeout |
| **ErrorCode** | Structured taxonomy identifier | Included in response body |
| **ErrorClassification** | Transport-agnostic mapping | Used by middleware |
| **IErrorClassifier** | Classification service | Registered in DI |

**Key Benefits:**
- ✅ Every error traceable via CorrelationId, NodeId, EnvironmentId
- ✅ Structured error codes for classification and analytics
- ✅ Transport-agnostic error mapping
- ✅ Consistent error responses across the Grid
- ✅ IDE-friendly with compile-time validation

**Best Practices:**
- Use typed exceptions for known failure modes
- Always include Grid context (CorrelationId, NodeId)
- Use `ErrorCode.WellKnown` values when applicable
- Wrap external exceptions with Grid-aware exceptions
- Log with structured context
- Never expose stack traces to external clients

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
