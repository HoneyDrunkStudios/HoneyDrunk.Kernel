# Identity - Strongly-Typed Identifiers

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

All identity types in HoneyDrunk.Kernel enforce validation rules at construction time, ensuring identifiers are well-formed throughout the system. They use **readonly record structs** for efficient value semantics.

**Location:** `HoneyDrunk.Kernel.Abstractions/Identity/`

**Key Features:**
- Compile-time type safety
- Runtime validation
- Efficient value semantics (no heap allocations)
- Implicit string conversions
- Parse and TryParse support

---

## NodeId.cs

### What it is
Strongly-typed identifier for Nodes in the Grid.

### Real-world analogy
Like a DNS hostname - human-readable, unique, and validated.

### Validation Rules
- **Format:** `^[a-z0-9]+(-[a-z0-9]+)*$` (kebab-case)
- **Length:** 3-64 characters
- **Constraints:**
  - Lowercase letters and digits only
  - Hyphens allowed as separators
  - No consecutive hyphens
  - Cannot start or end with hyphens

### Examples
✅ Valid: `"payment-node"`, `"auth-gateway"`, `"notification-service"`, `"api-v2"`  
❌ Invalid: `"Payment-Node"` (uppercase), `"payment_node"` (underscore), `"p"` (too short), `"payment--node"` (consecutive hyphens)

### Usage

```csharp
// Construction (throws on invalid)
var nodeId = new NodeId("payment-node");

// Try parse (returns bool)
if (NodeId.TryParse("auth-gateway", out var parsed))
{
    Console.WriteLine($"Valid: {parsed}");
}

// Validation check
if (NodeId.IsValid("invalid_node", out var errorMessage))
{
    // Won't reach here
}
else
{
    Console.WriteLine(errorMessage); 
    // "Node ID must be kebab-case: lowercase letters, digits, and hyphens only..."
}

// Implicit string conversion
string nodeIdString = nodeId; // "payment-node"
```

### When to use
- Node identification in configuration
- Routing and service discovery
- Telemetry tagging
- Log correlation

### Why it matters
Type-safe, validated identifiers prevent typos and ensure consistent naming conventions across the entire Grid.

---

## CorrelationId.cs

### What it is
ULID-based identifier for tracking related operations across Nodes.

### Real-world analogy
Like a package tracking number that follows your shipment through multiple facilities.

### Format
**ULID** (Universally Unique Lexicographically Sortable Identifier)
- 128-bit unique identifier
- Chronologically sortable
- URL-safe Base32 encoding
- Example: `01ARZ3NDEKTSV4RRFFQ69G5FAV`

### Properties
- **Uniqueness:** Globally unique across all Nodes
- **Sortability:** Lexicographically sortable by creation time
- **Compactness:** 26-character string representation
- **Performance:** Faster generation than UUIDv4

### Usage

```csharp
// Generate new correlation ID
var correlationId = CorrelationId.NewId();
Console.WriteLine(correlationId); // "01HQXZ8K4TJ9X5B3N2YGF7WDCQ"

// Parse from string (incoming request)
if (CorrelationId.TryParse(headers["X-Correlation-ID"], out var id))
{
    // Use parsed ID
    _logger.LogInformation("Request correlation: {CorrelationId}", id);
}

// Convert to/from Ulid
Ulid ulid = correlationId.ToUlid();
var fromUlid = CorrelationId.FromUlid(ulid);

// Implicit conversions
string idString = correlationId;           // To string
Ulid ulidValue = correlationId;            // To Ulid
```

### When to use
- Every operation in the Grid has a CorrelationId
- Created once per user request
- Propagated through all downstream operations
- Attached to all log entries
- Included in all telemetry spans

### Why it matters
Enables distributed tracing - you can see the entire journey of a request across multiple services with a single ID.

### Request Flow Example

```
User Request (ID: 01HQXZ8K4TJ9X5B3N2YGF7WDCQ)
    ↓
API Gateway (logs with CorrelationId)
    ↓
Payment Service (receives & logs same CorrelationId)
    ↓
Database Query (tagged with CorrelationId)
    ↓
Message Published (CorrelationId in headers)
    ↓
Notification Service (receives & logs same CorrelationId)
    ↓
Email Sent (CorrelationId in tracking)
```

All logs across all services share the same CorrelationId → full trace reconstruction.

---

## TenantId.cs

### What it is
ULID-based identifier for multi-tenant isolation boundaries.

### Real-world analogy
Like an apartment building number - isolates one customer from another.

### Format
ULID (same format as CorrelationId)

### Usage

```csharp
// Generate new tenant ID
var tenantId = TenantId.NewId();

// Parse from storage
if (TenantId.TryParse(dbRecord.TenantIdString, out var parsed))
{
    // Use parsed tenant ID
}

// Store with data for isolation
var record = new CustomerRecord
{
    TenantId = tenantId,
    CustomerId = customerId,
    Data = sensitiveData
};
await _db.SaveAsync(record);

// Query filtered by tenant
var orders = await _db.Orders
    .Where(o => o.TenantId == tenantId)
    .ToListAsync();
```

### When to use
- Multi-tenant SaaS applications
- Customer data isolation
- Row-level security
- Authorization scoping
- Usage tracking and billing

### Why it matters
Enforces multi-tenancy at the type level - ensures Customer A can never access Customer B's data through compile-time checks and runtime validation.

### Multi-Tenancy Pattern

```csharp
public class OrderService(IGridContext context, IOrderRepository repository)
{
    public async Task<Order> GetOrderAsync(string orderId)
    {
        // TenantId extracted from context baggage
        var tenantId = GetTenantIdFromContext(context);
        
        // Query automatically scoped to tenant
        return await repository.GetOrderAsync(orderId, tenantId);
    }
}
```

---

## ProjectId.cs

### What it is
ULID-based identifier for projects/workspaces within a tenant.

### Real-world analogy
Like project folders within a company - organize work within a tenant boundary.

### Format
ULID (same format as CorrelationId/TenantId)

### Hierarchy
```
Tenant (Organization)
  └── Project A (Team 1's workspace)
  └── Project B (Team 2's workspace)
  └── Project C (Shared resources)
```

### Usage

```csharp
// Create new project
var projectId = ProjectId.NewId();
var project = new Project
{
    ProjectId = projectId,
    TenantId = tenantId,
    Name = "Q1 Marketing Campaign"
};

// Query resources by project
var resources = await _repository.GetResourcesAsync(tenantId, projectId);

// Multi-level scoping
public class ResourceService
{
    public async Task<Resource> GetAsync(TenantId tenantId, ProjectId projectId, string resourceId)
    {
        // Check tenant access
        if (!await _authService.HasTenantAccessAsync(tenantId))
            throw new UnauthorizedException();
        
        // Check project access within tenant
        if (!await _authService.HasProjectAccessAsync(tenantId, projectId))
            throw new UnauthorizedException();
        
        return await _repository.GetAsync(tenantId, projectId, resourceId);
    }
}
```

### When to use
- Multi-project scenarios within tenants
- Team-based workspace isolation
- Resource organization and permissions
- Project-level billing and quotas

### Why it matters
Enables hierarchical organization: **Tenant → Projects → Resources**, allowing fine-grained access control and resource management.

---

## RunId.cs

### What it is
ULID-based identifier for execution instances (workflows, jobs, operations).

### Real-world analogy
Like a receipt number for a specific transaction.

### Format
ULID (same format as other ID types)

### Usage

```csharp
// Start a workflow execution
var runId = RunId.NewId();
var execution = new WorkflowExecution
{
    RunId = runId,
    WorkflowId = workflowId,
    StartedAt = DateTimeOffset.UtcNow,
    Status = ExecutionStatus.Running
};
await _repository.SaveAsync(execution);

// Track execution progress
await _repository.UpdateExecutionAsync(runId, status: ExecutionStatus.Completed);

// Query execution history
var runs = await _repository.GetExecutionHistoryAsync(workflowId);
foreach (var run in runs.OrderByDescending(r => r.RunId)) // Sorted by time (ULID property)
{
    Console.WriteLine($"Run {run.RunId}: {run.Status} at {run.StartedAt}");
}

// Correlate logs with execution
_logger.LogInformation("Workflow step completed for run {RunId}", runId);
```

### When to use
- Long-running operations
- Background jobs
- Workflow executions
- Batch processes
- Scheduled tasks
- Audit trails

### Why it matters
Enables execution history, retry logic, and audit trails. Multiple runs of the same workflow/job are tracked independently.

### Execution Tracking Example

```csharp
public class WorkflowEngine
{
    public async Task<RunId> ExecuteWorkflowAsync(string workflowId, GridContext context)
    {
        var runId = RunId.NewId();
        
        using var operation = _operationFactory.Create("ExecuteWorkflow");
        operation.AddMetadata("workflow_id", workflowId);
        operation.AddMetadata("run_id", runId);
        operation.AddMetadata("correlation_id", context.CorrelationId);
        
        try
        {
            await ExecuteStepsAsync(workflowId, runId, context);
            operation.Complete();
            return runId;
        }
        catch (Exception ex)
        {
            operation.Fail($"Workflow execution failed: {ex.Message}", ex);
            throw;
        }
    }
}
```

---

## Common Patterns

### Identity Validation

All identity types follow the same validation pattern:

```csharp
// TryParse (safe, returns bool)
if (NodeId.TryParse(input, out var nodeId))
{
    // Use nodeId
}

// Parse (throws on invalid)
try
{
    var correlationId = new CorrelationId(input);
}
catch (ArgumentException ex)
{
    // Handle invalid format
}

// IsValid (check without constructing)
if (NodeId.IsValid(input, out var errorMessage))
{
    // Valid
}
else
{
    _logger.LogWarning("Invalid NodeId: {Error}", errorMessage);
}
```

### Implicit Conversions

All ULID-based IDs support implicit string/Ulid conversions:

```csharp
CorrelationId id = CorrelationId.NewId();

// Implicit to string
string idString = id;

// Implicit to Ulid
Ulid ulidValue = id;

// Explicit conversions
var fromString = new CorrelationId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
var fromUlid = CorrelationId.FromUlid(Ulid.NewUlid());
```

### Serialization

All identity types serialize as strings:

```json
{
  "nodeId": "payment-node",
  "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
  "tenantId": "01HQXZ8K4TJ9X5B3N2YGF7WDCR",
  "projectId": "01HQXZ8K4TJ9X5B3N2YGF7WDCS",
  "runId": "01HQXZ8K4TJ9X5B3N2YGF7WDCT"
}
```

Deserialization:
```csharp
var obj = JsonSerializer.Deserialize<MyObject>(json);
// Identity types automatically validated during deserialization
```

---

## Testing with Identity Types

```csharp
[Fact]
public void NodeId_RejectsInvalidFormat()
{
    // Arrange
    var invalidIds = new[] 
    { 
        "UPPERCASE",           // Must be lowercase
        "has_underscore",      // Only hyphens allowed
        "ab",                  // Too short
        "starts-with-hyphen",  // Cannot start with hyphen
        "ends-with-hyphen-",   // Cannot end with hyphen
        "double--hyphen"       // No consecutive hyphens
    };
    
    // Act & Assert
    foreach (var invalid in invalidIds)
    {
        Assert.False(NodeId.TryParse(invalid, out _));
        Assert.Throws<ArgumentException>(() => new NodeId(invalid));
    }
}

[Fact]
public void CorrelationId_MaintainsSortOrder()
{
    // Arrange
    var ids = new List<CorrelationId>();
    
    // Act
    for (int i = 0; i < 100; i++)
    {
        ids.Add(CorrelationId.NewId());
        await Task.Delay(1); // Ensure different timestamps
    }
    
    // Assert - IDs should be in chronological order
    var sorted = ids.OrderBy(id => id.ToString()).ToList();
    Assert.Equal(ids, sorted);
}
```

---

## Performance Considerations

### NodeId
- **Validation:** Regex compiled with `[GeneratedRegex]` for optimal performance
- **Storage:** Stack-allocated (value type), no heap allocations
- **Comparison:** String comparison overhead

### ULID-based IDs (CorrelationId, TenantId, ProjectId, RunId)
- **Generation:** O(1), faster than GUIDv4
- **Storage:** 16 bytes (same as GUID)
- **String representation:** 26 characters (vs 36 for GUID)
- **Sortability:** Chronological without database overhead

### Recommendations
- Use ULID-based IDs for high-throughput scenarios (orders, events, runs)
- Cache NodeId validation results if validating user input repeatedly
- Prefer value types avoid boxing in hot paths

---

## Summary

| Type | Format | Length | Use Case |
|------|--------|--------|----------|
| **NodeId** | Kebab-case | 3-64 chars | Node identification |
| **CorrelationId** | ULID | 26 chars | Request tracing |
| **TenantId** | ULID | 26 chars | Multi-tenancy isolation |
| **ProjectId** | ULID | 26 chars | Project/workspace organization |
| **RunId** | ULID | 26 chars | Execution tracking |

**Common Properties:**
- ✅ Type-safe
- ✅ Validated at construction
- ✅ Value semantics (stack-allocated)
- ✅ Implicit string conversion
- ✅ Parse/TryParse support
- ✅ JSON serialization friendly

---

[← Back to File Guide](FILE_GUIDE.md)

