# Identity - Strongly-Typed Identifiers

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- **String-Based Identity Types**
  - [NodeId](#nodeidcs) - Node identification (kebab-case)
  - [EnvironmentId](#environmentidcs) - Deployment environment (kebab-case)
  - [SectorId](#sectoridcs) - Logical Node grouping (kebab-case)
  - [ErrorCode](#errorcodecs) - Hierarchical error classification (dot-separated)
- **ULID-Based Identity Types**
  - [CorrelationId](#correlationidcs) - Request tracing
  - [OperationId](#operationidcs) - Operation/span identification
  - [CausationId](#causationidcs) - Parent operation tracking
  - [TenantId](#tenantidcs) - Multi-tenancy isolation
  - [ProjectId](#projectidcs) - Project/workspace organization
  - [RunId](#runidcs) - Execution tracking
- [Common Patterns](#common-patterns)
- [Testing](#testing-with-identity-types)
- [Performance Considerations](#performance-considerations)
- [Summary](#summary)

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
- **Format:** Kebab-case (lowercase with optional hyphens)
- **Regex:** `^[a-z0-9]+(-[a-z0-9]+)*$`
- **Length:** 3-64 characters
- **Constraints:**
  - Lowercase letters and digits only
  - Hyphens act as separators (no underscores)
  - No consecutive hyphens
  - Cannot start or end with hyphens

### Examples
✅ Valid: `"kernel"`, `"transport"`, `"web-rest"`, `"pulse"`, `"arcadia"`, `"payment-service"`, `"api-v2"`

❌ Invalid: `"HoneyDrunk.Kernel"` (uppercase/dots), `"payment_node"` (underscore), `"p"` (too short), `"payment--node"` (consecutive hyphens)

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

**Registry Pattern:**
- **Infrastructure nodes** → Use `WellKnownNodes.Core.*` or `WellKnownNodes.Ops.*` (8 nodes)
- **Application nodes** → Define inline or in app constants (e.g., `new NodeId("arcadia")`)
- See [IdentityRegistries.md](IdentityRegistries.md) for details

### Why it matters
Type-safe, validated identifiers prevent typos and ensure consistent naming conventions across the entire Grid.

[↑ Back to top](#table-of-contents)

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

[↑ Back to top](#table-of-contents)

---

## OperationId.cs

### What it is
ULID-based identifier for a single operation (span) within a distributed trace.

### Real-world analogy
Like a unique transaction ID on a receipt - identifies this specific step in a larger journey.

### Format
**ULID** (same format as CorrelationId)

### Properties
- **Uniqueness:** Globally unique across all operations
- **Sortability:** Lexicographically sortable by creation time
- **Span Identity:** Maps to W3C traceparent span-id and OpenTelemetry span_id
- **Per-Operation:** Created fresh for each unit of work (HTTP request, message handler, job step)
- **Owned by IOperationContext:** Each `IOperationContext` instance generates and exposes its own `OperationId` to identify that span

### Usage

```csharp
// Generate new operation ID (for new operation/span)
var operationId = OperationId.NewId();
Console.WriteLine(operationId); // "01HQXZ8K4TJ9X5B3N2YGF7WDCQ"

// Parse from string (incoming trace context)
if (OperationId.TryParse(headers["X-Operation-ID"], out var id))
{
    _logger.LogInformation("Operation span: {OperationId}", id);
}

// Convert to/from Ulid
Ulid ulid = operationId.ToUlid();
var fromUlid = OperationId.FromUlid(ulid);

// Implicit conversions
string idString = operationId;           // To string
Ulid ulidValue = operationId;            // To Ulid
```

### When to use
- Every operation creates its own OperationId
- Identifies a unit of work within a larger trace
- Maps to span_id in distributed tracing systems
- Parent's OperationId becomes child's CausationId

### Why it matters
**Enables proper distributed tracing** - the missing piece that completes the three-ID model:
- **CorrelationId (trace-id)**: "All of these belong together"
- **OperationId (span-id)**: "This specific unit of work"
- **CausationId (parent-span-id)**: "Who called me"

Without OperationId, you can't distinguish between different operations in the same trace or build proper parent-child span relationships.

### Three-ID Model Example

```
User Request
  ├─ CorrelationId: ABC123 (constant - trace ID)
  ├─ OperationId: OP-001 (this operation - span ID)
  └─ CausationId: null (no parent - root span)
      │
      ├─ API Gateway
      │   ├─ CorrelationId: ABC123 (same trace)
      │   ├─ OperationId: OP-002 (new span)
      │   └─ CausationId: OP-001 (parent span)
      │       │
      │       ├─ Auth Service
      │       │   ├─ CorrelationId: ABC123 (same trace)
      │       │   ├─ OperationId: OP-003 (new span)
      │       │   └─ Causation Id: OP-002 (parent span)
      │       │
      │       └─ Payment Service
      │           ├─ CorrelationId: ABC123 (same trace)
      │           ├─ OperationId: OP-004 (new span)
      │           └─ CausationId: OP-002 (parent span)
```

**Result:** Perfect tree reconstruction with W3C/OpenTelemetry compatibility.

[↑ Back to top](#table-of-contents)

---

## CausationId.cs

### What it is
ULID-based identifier for tracking the direct parent operation that triggered the current operation.

### Real-world analogy
Like "Reply-To" in email threads - it points to the immediate message that caused this response, helping you trace back through the conversation tree.

### Format
**ULID** (same format as CorrelationId/OperationId)

### Properties
- **Uniqueness:** Globally unique across all operations
- **Sortability:** Lexicographically sortable by creation time
- **Causal Chain:** Forms a parent-child execution tree
- **Points to Parent's OperationId:** Not CorrelationId - points to the parent operation's span ID

### Usage

```csharp
// Incoming request (root operation - single CorrelationId for entire flow)
var correlationId = CorrelationId.NewId();
var operationId = OperationId.NewId();
var causationId = (CausationId?)null; // No parent

// First downstream operation (same CorrelationId, new OperationId, CausationId points to parent OperationId)
var operation1OperationId = OperationId.NewId();
var operation1CausationId = new CausationId(operationId); // Parent = original request's OperationId
_logger.LogInformation("Operation 1: Correlation={CorrelationId} Operation={OperationId} Causation={CausationId}", 
    correlationId, operation1OperationId, operation1CausationId);

// Second level operation (same CorrelationId, different OperationId/CausationId)
var operation2OperationId = OperationId.NewId();
var operation2CausationId = new CausationId(operation1OperationId); // Parent = operation1's OperationId
_logger.LogInformation("Operation 2: Correlation={CorrelationId} Operation={OperationId} Causation={CausationId}", 
    correlationId, operation2OperationId, operation2CausationId);

// Parse from message metadata
if (CausationId.TryParse(message.Properties["causation-id"], out var parsedCausationId))
{
    _logger.LogInformation("Processing message caused by {CausationId}", parsedCausationId);
}

// Convert to/from Ulid
Ulid ulid = causationId.ToUlid();
var fromUlid = CausationId.FromUlid(ulid);

// Implicit conversions
string idString = causationId;           // To string
Ulid ulidValue = causationId;            // To Ulid
```

### When to use
- Every operation that is triggered by another operation
- Building execution trees for complex workflows
- Debugging cascading failures
- Understanding request fan-out patterns
- Audit trails showing who-called-whom

### Why it matters
**Enables causal tracing** - while CorrelationId shows you all operations related to a user request, CausationId shows you the parent-child relationships by pointing to the parent's OperationId.

### Execution Tree Example

```
User Request
  ├─ Correlation: ABC123 (created once, propagated everywhere)
  ├─ Operation: OP-001 (this operation/span)
  └─ Causation: null
      │
      ├─ API Gateway
      │   ├─ Correlation: ABC123 (same)
      │   ├─ Operation: OP-002 (new)
      │   └─ Causation: OP-001 (points to User Request's OperationId)
      │       │
      │       ├─ Auth Service
      │       │   ├─ Correlation: ABC123 (same)
      │       │   ├─ Operation: OP-003 (new)
      │       │   └─ Causation: OP-002 (points to API Gateway's OperationId)
      │       │
      │       └─ Payment Service
      │           ├─ Correlation: ABC123 (same)
      │           ├─ Operation: OP-004 (new)
      │           └─ Causation: OP-002 (points to API Gateway's OperationId)
      │               │
      │               └─ Notification Service
      │                   ├─ Correlation: ABC123 (same)
      │                   ├─ Operation: OP-005 (new)
      │                   └─ Causation: OP-004 (points to Payment Service's OperationId)
```

**Result:** 
- **CorrelationId** is the same everywhere (`ABC123`) - links all operations to the original user request
- **OperationId** is unique per operation - identifies each individual span
- **CausationId** points to parent's OperationId - forms the tree structure

You can reconstruct the entire execution tree and see which operations spawned which children.

[↑ Back to top](#table-of-contents)

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
        // TenantId typically comes from IGridContext.TenantId (or baggage at external boundaries)
        var tenantId = context.TenantId;
        
        // Query automatically scoped to tenant
        return await repository.GetOrderAsync(orderId, tenantId);
    }
}
```

[↑ Back to top](#table-of-contents)

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

[↑ Back to top](#table-of-contents)

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

[↑ Back to top](#table-of-contents)

---

## EnvironmentId.cs

### What it is
Strongly-typed identifier for deployment environments (production, staging, development, etc.).

### Real-world analogy
Like the building floors in an office - each floor has different rules, access controls, and purposes (executive floor vs. cafeteria vs. warehouse).

### Validation Rules
- **Format:** `^[a-z0-9]+(-[a-z0-9]+)*$` (kebab-case)
- **Length:** 3-32 characters
- **Constraints:**
  - Lowercase letters and digits only
  - Single hyphens allowed as separators
  - No consecutive hyphens
  - Cannot start or end with hyphens
  - Low-cardinality (small set of well-known values)

### Examples
✅ Valid: `"production"`, `"staging"`, `"dev-alice"`, `"perf-test"`, `"integration"`  
❌ Invalid: `"Production"` (uppercase), `"dev_alice"` (underscore), `"pr"` (too short), `"dev--alice"` (consecutive hyphens)

### Usage

```csharp
// Construction (throws on invalid)
var envId = new EnvironmentId("production");

// Try parse (returns bool)
if (EnvironmentId.TryParse("staging", out var parsed))
{
    Console.WriteLine($"Valid environment: {parsed}");
}

// Well-known environments
var prod = EnvironmentId.WellKnown.Production;        // "production"
var staging = EnvironmentId.WellKnown.Staging;        // "staging"
var dev = EnvironmentId.WellKnown.Development;        // "development"
var test = EnvironmentId.WellKnown.Testing;           // "testing"
var perf = EnvironmentId.WellKnown.Performance;       // "performance"
var integration = EnvironmentId.WellKnown.Integration; // "integration"
var local = EnvironmentId.WellKnown.Local;            // "local"

// Validation check
if (EnvironmentId.IsValid("dev-sandbox", out var errorMessage))
{
    var customEnv = new EnvironmentId("dev-sandbox");
}
else
{
    Console.WriteLine(errorMessage);
}

// Implicit string conversion
string envString = envId; // "production"
```

### When to use
- Configuration scoping (different settings per environment)
- Telemetry partitioning (separate dashboards per environment)
- Access control (production vs. non-production permissions)
- Resource naming (database names, storage accounts)
- Deployment validation (prevent prod deployments from staging)
- Feature flags (enable features only in certain environments)

### Why it matters
Type-safe environment identification prevents catastrophic mistakes like deploying to production instead of staging or querying production data from a development environment.

### Environment-Scoped Configuration Example

```csharp
public class ConfigurationService
{
    public async Task<TConfig> GetConfigAsync<TConfig>(EnvironmentId environment, string key)
    {
        // Environment-specific config lookup
        var configKey = $"{environment}/{key}";
        return await _vaultClient.GetSecretAsync<TConfig>(configKey);
    }
}

// Usage
var dbConfig = await _configService.GetConfigAsync<DatabaseConfig>(
    EnvironmentId.WellKnown.Production, 
    "database/connection-string"
);
```

[↑ Back to top](#table-of-contents)

---

## SectorId.cs

### What it is
Strongly-typed identifier for logical groupings of Nodes within the Grid (e.g., core, ai, ops, creator, market).

### Real-world analogy
Like departments in a company - Marketing, Engineering, Sales, HR. Each sector groups related services together.

### Validation Rules
- **Format:** `^[a-z0-9]+(-[a-z0-9]+)*$` (kebab-case)
- **Length:** 2-32 characters
- **Constraints:**
  - Lowercase letters and digits only
  - Single hyphens allowed as separators
  - No consecutive hyphens
  - Cannot start or end with hyphens
  - Low-cardinality (intentionally coarse grouping)

### Examples
✅ Valid: `"core"`, `"ai"`, `"ops"`, `"creator"`, `"market"`, `"honeyplay"`, `"cyberware"`  
❌ Invalid: `"AI"` (uppercase), `"data_services"` (underscore), `"a"` (too short), `"data--services"` (consecutive hyphens)

### Usage

```csharp
// Construction (throws on invalid)
var sectorId = new SectorId("ai");

// Try parse (returns bool)
if (SectorId.TryParse("creator", out var parsed))
{
    Console.WriteLine($"Valid sector: {parsed}");
}

// Well-known sectors for the HoneyDrunk Grid
var core = SectorId.WellKnown.Core;             // "core" - foundational primitives
var ops = SectorId.WellKnown.Ops;               // "ops" - CI/CD, monitoring
var ai = SectorId.WellKnown.AI;                 // "ai" - agents, orchestration
var creator = SectorId.WellKnown.Creator;       // "creator" - content tools
var market = SectorId.WellKnown.Market;         // "market" - public SaaS products
var honeyplay = SectorId.WellKnown.HoneyPlay;   // "honeyplay" - gaming, media
var cyberware = SectorId.WellKnown.Cyberware;   // "cyberware" - robotics, hardware
var honeynet = SectorId.WellKnown.HoneyNet;     // "honeynet" - security, defense
var meta = SectorId.WellKnown.Meta;             // "meta" - registries, documentation

// Alternative: Use the Sectors static class
var coreSector = Sectors.Core;                   // Same as SectorId.WellKnown.Core

// Validation check
if (SectorId.IsValid("custom-sector", out var errorMessage))
{
    var customSector = new SectorId("custom-sector");
}
else
{
    Console.WriteLine(errorMessage);
}

// Implicit string conversion
string sectorString = sectorId; // "ai"
```

### When to use
- Service discovery (find all Nodes in a sector)
- Resource organization (group related Nodes)
- Deployment planning (deploy entire sectors)
- Observability grouping (dashboards per sector)
- Access control (sector-level permissions)
- Network policies (isolate sectors)
- Cost allocation (billing per sector)

### Why it matters
Provides a **coarse-grained taxonomy** for organizing hundreds of Nodes without creating a complex hierarchy. Keeps the Grid manageable and discoverable.

### HoneyDrunk Grid Sectors

| Sector | Description | Examples |
|--------|-------------|----------|
| **Core** | Foundational primitives | Kernel, Data, Transport |
| **Ops** | CI/CD and observability | Deployments, monitoring, logging |
| **AI** | Agents and orchestration | LLM services, agent lifecycle, memory |
| **Creator** | Content tools | Content intelligence, amplification |
| **Market** | Public SaaS products | Customer-facing applications |
| **HoneyPlay** | Gaming and media | Worlds, leagues, narrative experiences |
| **Cyberware** | Robotics and hardware | Simulation, servos, embodied agents |
| **HoneyNet** | Security and defense | Breach simulations, secure SDKs |
| **Meta** | Knowledge systems | Registries, documentation, schemas |

### Sector-Based Service Discovery Example

```csharp
public class ServiceDiscoveryClient
{
    public async Task<IEnumerable<NodeDescriptor>> GetNodesInSectorAsync(SectorId sector)
    {
        // Find all nodes in the specified sector
        return await _registry.QueryAsync(n => n.SectorId == sector);
    }
}

// Usage - find all AI services
var aiNodes = await _discovery.GetNodesInSectorAsync(SectorId.WellKnown.AI);
foreach (var node in aiNodes)
{
    Console.WriteLine($"AI Node: {node.NodeId} at {node.Endpoint}");
}

// Usage - find all Creator tools
var creatorNodes = await _discovery.GetNodesInSectorAsync(Sectors.Creator);
foreach (var node in creatorNodes)
{
    Console.WriteLine($"Creator Node: {node.NodeId}");
}
```

[↑ Back to top](#table-of-contents)

---

## ErrorCode.cs

### What it is
Structured error code using dot-separated segments for hierarchical error classification.

### Real-world analogy
Like HTTP status codes, but more detailed: HTTP gives you `404`, ErrorCode gives you `resource.notfound.customer.order`.

### Validation Rules
- **Format:** Dot-separated segments: `segment1.segment2.segment3`
- **Segment Rules:**
  - Each segment: 1-32 characters
  - Lowercase alphanumeric and hyphens (kebab-case within segments)
  - At least one segment required
- **Overall Length:** Maximum 128 characters
- **Purpose:** Hierarchical error taxonomy

### Examples
✅ Valid: `"validation.input"`, `"authentication.token-expired"`, `"state.version-conflict"`, `"rate-limit.exceeded"`  
❌ Invalid: `"Validation.Input"` (uppercase), `"validation_input"` (underscore), `"validation."` (trailing dot), `""` (empty)

### Usage

```csharp
// Construction (throws on invalid)
var errorCode = new ErrorCode("validation.input.missing");

// Try parse (returns bool)
if (ErrorCode.TryParse("authentication.failure", out var parsed))
{
    Console.WriteLine($"Valid error code: {parsed}");
}

// Well-known error codes (Grid-native categories)

// Validation
var validationInput = ErrorCode.WellKnown.ValidationInput;           // "validation.input"
var validationBusiness = ErrorCode.WellKnown.ValidationBusiness;     // "validation.business"

// Authentication & Authorization
var authFailure = ErrorCode.WellKnown.AuthenticationFailure;         // "authentication.failure"
var authTokenExpired = ErrorCode.WellKnown.AuthenticationTokenExpired; // "authentication.token-expired"
var authzFailure = ErrorCode.WellKnown.AuthorizationFailure;         // "authorization.failure"

// Context (Grid-specific)
var contextMissing = ErrorCode.WellKnown.ContextMissing;             // "context.missing"
var contextInvalid = ErrorCode.WellKnown.ContextInvalid;             // "context.invalid"
var tenantInactive = ErrorCode.WellKnown.TenantInactive;             // "tenant.inactive"
var projectInactive = ErrorCode.WellKnown.ProjectInactive;           // "project.inactive"

// Resource
var notFound = ErrorCode.WellKnown.ResourceNotFound;                 // "resource.notfound"
var conflict = ErrorCode.WellKnown.ResourceConflict;                 // "resource.conflict"

// Operation & State (Distributed system)
var stateConflict = ErrorCode.WellKnown.StateVersionConflict;        // "state.version-conflict"
var idempotentReplay = ErrorCode.WellKnown.OperationIdempotentReplay; // "operation.idempotent-replay"
var opTimeout = ErrorCode.WellKnown.OperationTimeout;                // "operation.timeout"

// Contract (Transport/Envelope)
var contractInvalid = ErrorCode.WellKnown.ContractInvalid;           // "contract.invalid"
var contractUnsupported = ErrorCode.WellKnown.ContractUnsupportedVersion; // "contract.unsupported-version"
var contractMissing = ErrorCode.WellKnown.ContractMissingField;      // "contract.missing-field"

// Feature & Quota (Runtime gating)
var featureDisabled = ErrorCode.WellKnown.FeatureDisabled;           // "feature.disabled"
var featureNotAllowed = ErrorCode.WellKnown.FeatureNotAllowed;       // "feature.not-allowed"
var quotaExceeded = ErrorCode.WellKnown.QuotaExceeded;               // "quota.exceeded"

// Dependency
var depUnavailable = ErrorCode.WellKnown.DependencyUnavailable;      // "dependency.unavailable"
var depTimeout = ErrorCode.WellKnown.DependencyTimeout;              // "dependency.timeout"

// Configuration
var configInvalid = ErrorCode.WellKnown.ConfigurationInvalid;        // "configuration.invalid"

// System
var internalError = ErrorCode.WellKnown.InternalError;               // "internal.error"
var serviceUnavailable = ErrorCode.WellKnown.ServiceUnavailable;     // "service.unavailable"
var rateLimitExceeded = ErrorCode.WellKnown.RateLimitExceeded;       // "ratelimit.exceeded"

// Validation check
if (ErrorCode.IsValid("custom.error.type", out var errorMessage))
{
    var customError = new ErrorCode("custom.error.type");
}
else
{
    Console.WriteLine(errorMessage);
}

// Implicit string conversion
string errorString = errorCode; // "validation.input.missing"
```

### When to use
- Structured error responses in APIs
- Error classification for monitoring and alerting
- Alert routing (different alerts for different error types)
- Error documentation (generate docs from error codes)
- Client error handling (programmatic error detection)
- Internationalization (map codes to localized messages)
- SLA tracking (track error types vs. targets)
- **Grid-specific scenarios:**
  - Context validation failures (`context.missing`, `context.invalid`)
  - Multi-tenant isolation errors (`tenant.inactive`, `project.inactive`)
  - Distributed state conflicts (`state.version-conflict`, `operation.idempotent-replay`)
  - Envelope/contract validation (`contract.invalid`, `contract.missing-field`)
  - Feature gating (`feature.disabled`, `quota.exceeded`)

### Why it matters
Enables **machine-readable error classification** across the entire Grid. Unlike HTTP status codes (which only provide `404`, `403`, `500`), ErrorCodes provide Grid-native context like `authentication.token-expired` or `state.version-conflict`. Clients can handle errors programmatically, monitoring systems can alert on specific error patterns, and documentation can be auto-generated.

**Grid-Native vs REST-Only:**
- ❌ REST-only: `401 Unauthorized` (generic auth failure)
- ✅ Grid-native: `authentication.token-expired` (specific reason, actionable)
- ❌ REST-only: `409 Conflict` (generic conflict)
- ✅ Grid-native: `state.version-conflict` (optimistic concurrency, retry logic clear)

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
  "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCR",
  "projectId": "01HQXZ8K4TJ9X5B3N2YGF7WDCS",
  "runId": "01HQXZ8K4TJ9X5B3N2YGF7WDCT"
}
```

Deserialization:
```csharp
var obj = JsonSerializer.Deserialize<MyObject>(json);
// Identity types automatically validated during deserialization
```

[↑ Back to top](#table-of-contents)

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
public async Task CorrelationId_MaintainsSortOrder()
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

[Fact]
public void CausationId_LinksOperations()
{
    // Arrange
    var parentOperationId = OperationId.NewId();
    var causationId = new CausationId(parentOperationId);
    
    // Act
    var reconstructed = causationId.ToUlid();
    
    // Assert
    Assert.Equal(parentOperationId.ToUlid(), reconstructed);
}

[Fact]
public void EnvironmentId_WellKnownValuesAreValid()
{
    // Arrange & Act
    var wellKnown = new[]
    {
        EnvironmentId.WellKnown.Production,
        EnvironmentId.WellKnown.Staging,
        EnvironmentId.WellKnown.Development,
        EnvironmentId.WellKnown.Testing
    };
    
    // Assert
    foreach (var env in wellKnown)
    {
        Assert.True(EnvironmentId.IsValid(env, out _));
    }
}

[Fact]
public void SectorId_RejectsTooShort()
{
    // Arrange
    var tooShort = "a";
    
    // Act & Assert
    Assert.False(SectorId.TryParse(tooShort, out _));
    Assert.Throws<ArgumentException>(() => new SectorId(tooShort));
}

[Fact]
public void ErrorCode_SupportsHierarchicalStructure()
{
    // Arrange
    var errorCode = new ErrorCode("validation.input.missing.required");
    
    // Act
    var segments = errorCode.ToString().Split('.');
    
    // Assert
    Assert.Equal(4, segments.Length);
    Assert.Equal("validation", segments[0]);
    Assert.Equal("input", segments[1]);
    Assert.Equal("missing", segments[2]);
    Assert.Equal("required", segments[3]);
}
```

[↑ Back to top](#table-of-contents)

---

## Performance Considerations

### NodeId, EnvironmentId, SectorId
- **Validation:** Regex compiled with `[GeneratedRegex]` for optimal performance
- **Storage:** Stack-allocated (value type), no heap allocations
- **Comparison:** String comparison overhead

### ULID-based IDs (CorrelationId, OperationId, CausationId, TenantId, ProjectId, RunId)
- **Generation:** O(1), faster than GUIDv4
- **Storage:** 16 bytes (same as GUID)
- **String representation:** 26 characters (vs 36 for GUID)
- **Sortability:** Chronological without database overhead

### ErrorCode
- **Validation:** Character-by-character check (no regex)
- **Storage:** String value (heap-allocated)
- **Comparison:** String equality (consider interning for hot paths)

### Recommendations
- Use ULID-based IDs for high-throughput scenarios (orders, events, runs)
- Cache NodeId/EnvironmentId/SectorId validation results if validating user input repeatedly
- Prefer value types; avoid boxing in hot paths
- Intern common ErrorCode values for faster comparison

---

## Summary

| Type | Format | Length | Use Case |
|------|--------|--------|----------|
| **NodeId** | Kebab-case | 3-64 chars | Node identification |
| **CorrelationId** | ULID | 26 chars | Request tracing (trace-id - constant per request) |
| **OperationId** | ULID | 26 chars | Operation/span identification (span-id - unique per operation) |
| **CausationId** | ULID | 26 chars | Parent operation tracking (parent-span-id - points to parent OperationId) |
| **TenantId** | ULID | 26 chars | Multi-tenancy isolation |
| **ProjectId** | ULID | 26 chars | Project/workspace organization |
| **RunId** | ULID | 26 chars | Execution instance tracking |
| **EnvironmentId** | Kebab-case | 3-32 chars | Environment identification |
| **SectorId** | Kebab-case | 2-32 chars | Logical Node grouping |
| **ErrorCode** | Dot-separated kebab-case segments | 1-128 chars | Grid-native error classification |

**Format Rules:**
- **Kebab-case** = lowercase + hyphens (`kernel`, `payment-service`, `rate-limit.exceeded`)
- **ULID** = chronologically sortable, 26-char Base32 (`01HQXZ8K...`)

**Three-ID Tracing Model (W3C/OpenTelemetry Compatible):**
- **CorrelationId (trace-id)** = Constant across entire request tree
- **OperationId (span-id)** = Unique per operation/span
- **CausationId (parent-span-id)** = Points to parent's OperationId

**Common Properties:**
- ✅ Type-safe
- ✅ Validated at construction
- ✅ Value semantics (stack-allocated for kebab-case types)
- ✅ Implicit string conversion
- ✅ Parse/TryParse support
- ✅ JSON serialization friendly

**Grid-Native Design:**
- **Context propagation** - CorrelationId stays constant, OperationId uniquely identifies each span, CausationId points to parent
- **Multi-tenancy** - TenantId, ProjectId enforce isolation at type level
- **Distributed state** - ErrorCode includes `state.version-conflict`, `operation.idempotent-replay`
- **Runtime gating** - ErrorCode includes `feature.disabled`, `quota.exceeded`
- **Contract validation** - ErrorCode includes `contract.invalid`, `contract.missing-field`

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

