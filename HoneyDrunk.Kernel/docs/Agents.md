# 🤖 Agents - Agent Execution Framework

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IAgentDescriptor.cs](#iagentdescriptorcs)
- [IAgentExecutionContextFactory.cs](#iagentexecutioncontextfactorycs)
- [IAgentExecutionContext.cs](#iagentexecutioncontextcs)
- [IAgentCapability.cs](#iagentcapabilitycs)
- [AgentContextScope.cs](#agentcontextscopecs)
- [Complete Agent Execution Example](#complete-agent-execution-example)
- [AgentsInterop - Serialization and Context Marshaling](#agentsinterop---serialization-and-context-marshaling)
  - [AgentExecutionResult.cs](#agentexecutionresultcs)
  - [AgentResultSerializer.cs](#agentresultserializercs)
  - [GridContextSerializer.cs](#gridcontextserializercs)
  - [AgentContextProjection.cs](#agentcontextprojectioncs)
  - [Complete AgentsInterop Example](#complete-agentsinterop-example)
  - [Cross-Process Agent Flow](#cross-process-agent-flow)
- [Testing Patterns](#testing-patterns)
- [Summary](#summary)

---

## Overview

Agent abstractions enable AI assistants, automation scripts, and service accounts to operate within the Grid with scoped permissions, execution tracking, and capability-based security.

**Location:** `HoneyDrunk.Kernel.Abstractions/Agents/`

**Key Concepts:**
- **Agent Descriptor** - Identity, capabilities, and permissions
- **Agent Execution Context** - Scoped context and metadata tracking
- **Agent Capability** - Declarative permissions for actions
- **Agent Context Scope** - Fine-grained access control to Grid context

### Kernel vs AgentKit: Scope Boundary

**What lives in Kernel (this document):**
- ✅ **IAgentDescriptor** - Agent identity and capability declarations
- ✅ **IAgentExecutionContext** - Scoped execution context composition
- ✅ **IAgentExecutionContextFactory** - Context creation factory
- ✅ **AgentContextScope** - Security scoping for context visibility
- ✅ **Serializers** - Cross-process context and result marshaling (AgentResultSerializer, GridContextSerializer)
- ✅ **AgentContextProjection** - Internal primitive for context composition

**What lives in HoneyDrunk.AgentKit (future/external):**
- 🔮 **Scheduling** - Agent invocation timing and triggers
- 🔮 **Orchestration** - Multi-step workflows and coordination
- 🔮 **Memory** - Persistent conversation history and state
- 🔮 **Retry Logic** - Failure handling and exponential backoff
- 🔮 **Agent Runtime** - LLM integration, tool execution, planning

**Kernel's Role:** Provide the **execution primitives** that AgentKit and other Nodes build on. Kernel does not run agents - it defines how agents integrate with the Grid's context propagation, telemetry, and security model.

### Identity, Tenancy, and Security

Agent execution contexts inherit identity from Kernel's context primitives:

**Identity Sources:**
- **IGridContext** provides: `CorrelationId`, `CausationId`, `NodeId`, `StudioId`, `Environment`, `TenantId`, `ProjectId`, `Baggage`
- **IOperationContext** provides: `OperationId`, operation timing, outcome tracking, metadata
- **INodeContext** provides: Node identity, version, lifecycle stage

**Security Model (Two Layers):**

1. **AgentContextScope** - Controls which Grid context fields the agent can see
   - `None` → Fully isolated
   - `CorrelationOnly` → Tracing IDs only
   - `NodeAndCorrelation` → Node identity + tracing
   - `StudioAndNode` → Environment info + Node identity
   - `Standard` → All fields including TenantId/ProjectId; sensitive baggage filtered
   - `Full` → Complete access (trusted agents only)

2. **GridContextSerializer** - Filters sensitive baggage keys during serialization
   - Automatically removes keys containing: `"secret"`, `"password"`, `"token"`, `"key"`, `"credential"`
   - Use `includeFullBaggage: false` for untrusted agents (default recommended)
   - Use `includeFullBaggage: true` only for fully trusted internal agents

**What Kernel Does NOT Handle:**
- ❌ Vault integration (API keys, secrets) - handled by Auth/Configuration Nodes
- ❌ Token validation (JWT, OAuth) - handled by Auth Node
- ❌ Multi-tenant authorization policies - applications implement based on `TenantId`/`ProjectId`
- ❌ Row-level security - applications filter queries based on Grid context

**Design:** Kernel provides **identity rails** (TenantId, ProjectId, etc.) but does not enforce authorization. Downstream Nodes implement policies based on these identity attributes.

See [Identity.md](Identity.md) for strongly-typed ID primitives and [Context.md](Context.md) for context propagation patterns.

---

## IAgentDescriptor.cs

### What it is
Describes an agent's identity, capabilities, and access permissions within the Grid.

### Real-world analogy
Like a service account profile - defines who the agent is and what it's allowed to do.

### Properties

```csharp
public interface IAgentDescriptor
{
    string AgentId { get; }                              // Unique identifier
    string Name { get; }                                  // Human-readable name
    string AgentType { get; }                             // "llm-assistant", "automation-bot", "service-account"
    string Version { get; }                               // Agent version
    IReadOnlyList<IAgentCapability> Capabilities { get; } // What it can do
    AgentContextScope ContextScope { get; }               // What context it can see
    IReadOnlyDictionary<string, string> Metadata { get; } // Additional configuration
    bool HasCapability(string capabilityName);
}
```

### Usage Example

```csharp
public class OrderProcessingAgent : IAgentDescriptor
{
    public string AgentId => "order-processor-v2";
    public string Name => "Order Processing Automation";
    public string AgentType => "automation-bot";
    public string Version => "2.1.0";
    
    public IReadOnlyList<IAgentCapability> Capabilities => new[]
    {
        new ReadDatabaseCapability(),
        new InvokeApiCapability("payment-api"),
        new SendNotificationCapability()
    };
    
    // Agent can see Node and correlation data, but not full baggage
    public AgentContextScope ContextScope => AgentContextScope.NodeAndCorrelation;
    
    public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>
    {
        ["owner"] = "automation-team",
        ["cost-center"] = "engineering",
        ["max-concurrent-executions"] = "10"
    };
    
    public bool HasCapability(string capabilityName)
    {
        return Capabilities.Any(c => c.Name == capabilityName);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IAgentExecutionContextFactory.cs

### What it is
Primary entry point for creating scoped agent execution contexts with unified execution semantics across the Grid.

### Real-world analogy
Like a session factory in database ORM - creates a properly configured execution environment with automatic resource management.

### Location
**Interface:** `HoneyDrunk.Kernel.Abstractions/Agents/IAgentExecutionContextFactory.cs`  
**Implementation:** `HoneyDrunk.Kernel/AgentsInterop/AgentExecutionContextFactory.cs`

### Method

```csharp
public interface IAgentExecutionContextFactory
{
    IAgentExecutionContext Create(
        IAgentDescriptor agent,
        IGridContext gridContext,
        IOperationContext? operationContext = null,
        IReadOnlyDictionary<string, object?>? executionMetadata = null);
}
```

### What It Does

1. **Composes Context** - Unifies GridContext + OperationContext + AgentDescriptor
2. **Creates OperationContext** - Automatically creates one if not provided (uses IOperationContextFactory)
3. **Scoped Lifecycle** - Returned context tracks execution lifecycle (use with `using` pattern via OperationContext)
4. **Consistent Semantics** - Ensures all Nodes create agent contexts the same way

### Usage Example

```csharp
public class AgentExecutor(IAgentExecutionContextFactory contextFactory)
{
    public async Task<Result> ExecuteAgentAsync(
        IAgentDescriptor agent,
        IGridContext gridContext)
    {
        // Factory creates scoped execution context
        using var execContext = contextFactory.Create(
            agent: agent,
            gridContext: gridContext);
        
        try
        {
            // Track LLM usage
            execContext.AddMetadata("model", "gpt-4");
            execContext.AddMetadata("tokens_prompt", 150);
            
            // Check permissions before accessing resources
            if (!execContext.CanAccess("database", "orders"))
            {
                throw new UnauthorizedAccessException("Agent cannot access orders database");
            }
            
            var orders = await FetchOrdersAsync(execContext);
            
            // Track completion
            execContext.AddMetadata("orders_processed", orders.Count);
            execContext.AddMetadata("tokens_completion", 300);
            execContext.OperationContext.Complete();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            execContext.OperationContext.Fail($"Agent execution failed: {ex.Message}", ex);
            throw;
        }
    } // execContext disposed automatically
}
```

### When to Use

- ✅ **Always** - Use this factory to create agent execution contexts
- ✅ **Node implementations** - Consistent context creation across all Nodes
- ✅ **Testing** - Mock the factory for controlled test scenarios
- ✅ **Production** - Automatic operation tracking and telemetry

### Advanced Usage (Manual OperationContext)

```csharp
public class AdvancedAgentExecutor(
    IAgentExecutionContextFactory contextFactory,
    IOperationContextFactory operationFactory)
{
    public async Task ExecuteWithCustomOperationAsync(
        IAgentDescriptor agent,
        IGridContext gridContext)
    {
        // Create custom operation context with specific metadata
        using var operation = operationFactory.Create(
            operationName: $"CustomAgent:{agent.AgentId}",
            metadata: new Dictionary<string, object?>
            {
                ["priority"] = "high",
                ["source"] = "webhook"
            });
        
        // Pass custom operation to factory
        using var execContext = contextFactory.Create(
            agent: agent,
            gridContext: gridContext,
            operationContext: operation,
            executionMetadata: new Dictionary<string, object?>
            {
                ["temperature"] = 0.7,
                ["maxTokens"] = 2000
            });
        
        await ExecuteAgentLogicAsync(execContext);
    }
}
```

### Why It Matters

**Before (without factory):**
```csharp
// Every Node implements its own context creation
var context = new AgentExecutionContext(...); // Different patterns everywhere
```

**After (with factory):**
```csharp
// Consistent across entire Grid
using var context = contextFactory.Create(agent, grid);
```

**Benefits:**
- ✅ **Unified execution model** - Same semantics across all Nodes
- ✅ **Automatic tracking** - OperationContext created if not provided
- ✅ **Resource management** - Proper disposal via using pattern
- ✅ **Testable** - Easy to mock for unit tests
- ✅ **Maintainable** - Single place to update context composition logic

**Note on executionMetadata:** This parameter is intended for execution-level properties (temperature, max tokens, sampling settings), not the entire input payload. Input data should be passed separately to your agent execution logic.

### Registration

Registered by `AddHoneyDrunkNode()` during Node bootstrap:

```csharp
builder.Services.AddHoneyDrunkNode(options =>
{
    options.NodeId = Nodes.Core.Kernel;
    options.SectorId = Sectors.Core;
    options.EnvironmentId = Environments.Development;
    options.StudioId = "test-studio";
});
// IAgentExecutionContextFactory is now available via DI (scoped)
```

[↑ Back to top](#table-of-contents)

---

## IAgentExecutionContext.cs

### What it is
Execution context for agent operations with scoped permissions, tracking, and access control.

### Real-world analogy
Like a security context or session - carries identity, permissions, and tracks what the agent does.

### Properties

```csharp
public interface IAgentExecutionContext
{
    IAgentDescriptor Agent { get; }                      // Which agent is executing
    IGridContext GridContext { get; }                    // Scoped Grid context
    IOperationContext OperationContext { get; }          // Operation tracking
    DateTimeOffset StartedAtUtc { get; }                 // When execution started
    IReadOnlyDictionary<string, object?> ExecutionMetadata { get; } // Agent-specific metadata
    
    void AddMetadata(string key, object? value);
    bool CanAccess(string resourceType, string resourceId);
}
```

### Authorization Model

The `CanAccess` method provides a lightweight pre-flight check before accessing resources:

**What It Does:**
- Evaluates agent's capabilities (`IAgentCapability`)
- Optionally integrates with policy providers registered in DI
- Provides a guardrail for resource access attempts

**What It Is NOT:**
- ❌ Not a full authorization system replacement
- ❌ Not application-level permission enforcement
- ❌ Not row-level security
- ❌ Does not validate individual resource IDs - checks capability class only (e.g., `access:database`, not specific table names)

**Design:** `CanAccess` is a lightweight capability check at the resource type level. Applications should still enforce their own authorization policies based on Grid context (`TenantId`, `ProjectId`, user identity) when accessing actual resources.

**Typical Implementation:**
```csharp
public bool CanAccess(string resourceType, string resourceId)
{
    // Check if agent has capability for this resource type (class-level, not ID-level)
    var capabilityName = $"access:{resourceType}";
    return Agent.HasCapability(capabilityName);
}
```

### Usage Example

```csharp
public class AgentExecutor(IAgentExecutionContextFactory contextFactory)
{
    public async Task<Result> ExecuteAgentAsync(IAgentDescriptor agent, GridContext gridContext)
    {
        // Create scoped execution context
        using var execContext = contextFactory.Create(agent, gridContext);
        
        try
        {
            // Track LLM usage
            execContext.AddMetadata("model", "gpt-4");
            execContext.AddMetadata("tokens_prompt", 150);
            
            // Check permissions before accessing resources
            if (!execContext.CanAccess("database", "orders"))
            {
                throw new UnauthorizedAccessException("Agent cannot access orders database");
            }
            
            var orders = await FetchOrdersAsync(execContext);
            
            // Track completion
            execContext.AddMetadata("orders_processed", orders.Count);
            execContext.AddMetadata("tokens_completion", 300);
            execContext.OperationContext.Complete();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            execContext.OperationContext.Fail($"Agent execution failed: {ex.Message}", ex);
            throw;
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IAgentCapability.cs

### What it is
Declarative permission defining what an agent can do with validation and constraints.

### Real-world analogy
Like OAuth scopes or IAM policies - fine-grained permissions.

### Properties

```csharp
public interface IAgentCapability
{
    string Name { get; }                                  // Capability name
    string Description { get; }                            // Purpose
    string Category { get; }                               // Domain
    string PermissionLevel { get; }                        // "read", "write", "admin"
    IReadOnlyDictionary<string, string> Constraints { get; } // Rate limits, quotas
    
    bool ValidateParameters(
        IReadOnlyDictionary<string, object?> parameters, 
        out string? errorMessage);
}
```

### Usage Example

```csharp
public class InvokeApiCapability : IAgentCapability
{
    private readonly string _apiName;
    
    public InvokeApiCapability(string apiName)
    {
        _apiName = apiName;
    }
    
    public string Name => $"invoke-api:{_apiName}";
    public string Description => $"Invoke {_apiName} API endpoints";
    public string Category => "integration";
    public string PermissionLevel => "write";
    
    public IReadOnlyDictionary<string, string> Constraints => new Dictionary<string, string>
    {
        ["rateLimit"] = "100/minute",
        ["maxPayloadSize"] = "1MB",
        ["allowedMethods"] = "GET,POST"
    };
    
    public bool ValidateParameters(
        IReadOnlyDictionary<string, object?> parameters, 
        out string? errorMessage)
    {
        if (!parameters.ContainsKey("method"))
        {
            errorMessage = "Missing required parameter: method";
            return false;
        }
        
        var method = parameters["method"]?.ToString();
        if (method != "GET" && method != "POST")
        {
            errorMessage = $"Method {method} not allowed. Allowed: GET, POST";
            return false;
        }
        
        errorMessage = null;
        return true;
    }
}

public class ReadDatabaseCapability : IAgentCapability
{
    public string Name => "read-database";
    public string Description => "Read-only access to application databases";
    public string Category => "data-access";
    public string PermissionLevel => "read";
    
    public IReadOnlyDictionary<string, string> Constraints => new Dictionary<string, string>
    {
        ["maxRowsPerQuery"] = "1000",
        ["queryTimeout"] = "30s",
        ["allowedTables"] = "orders,customers,products"
    };
    
    public bool ValidateParameters(
        IReadOnlyDictionary<string, object?> parameters, 
        out string? errorMessage)
    {
        if (!parameters.ContainsKey("table"))
        {
            errorMessage = "Missing required parameter: table";
            return false;
        }
        
        var table = parameters["table"]?.ToString();
        var allowedTables = Constraints["allowedTables"].Split(',');
        
        if (!allowedTables.Contains(table))
        {
            errorMessage = $"Table {table} not accessible. Allowed: {string.Join(", ", allowedTables)}";
            return false;
        }
        
        errorMessage = null;
        return true;
    }
}
```

[↑ Back to top](#table-of-contents)

---

## AgentContextScope.cs

### What it is
Enum defining what portions of Grid context an agent can access.

### Real-world analogy
Like privacy levels - public, friends-only, private.

### Values

| Scope | Description | Access |
|-------|-------------|--------|
| `None` | Fully isolated | No context visible |
| `CorrelationOnly` | Basic tracing | CorrelationId, CausationId only |
| `NodeAndCorrelation` | Node identity + tracing | NodeId, CorrelationId, CausationId |
| `StudioAndNode` | Environment info | StudioId, Environment, NodeId, tracing |
| `Standard` | Non-sensitive data | All fields including TenantId/ProjectId; sensitive baggage filtered |
| `Full` | Complete access | All fields including TenantId/ProjectId and full baggage |

### Usage Example

```csharp
public class ContextScopeEnforcer
{
    public IGridContext CreateScopedContext(IGridContext fullContext, AgentContextScope scope)
    {
        return scope switch
        {
            AgentContextScope.None => new IsolatedGridContext(),
            
            AgentContextScope.CorrelationOnly => new ScopedGridContext
            {
                CorrelationId = fullContext.CorrelationId,
                CausationId = fullContext.CausationId
                // All other properties null/empty
            },
            
            AgentContextScope.NodeAndCorrelation => new ScopedGridContext
            {
                CorrelationId = fullContext.CorrelationId,
                CausationId = fullContext.CausationId,
                NodeId = fullContext.NodeId
            },
            
            AgentContextScope.StudioAndNode => new ScopedGridContext
            {
                CorrelationId = fullContext.CorrelationId,
                CausationId = fullContext.CausationId,
                NodeId = fullContext.NodeId,
                StudioId = fullContext.StudioId,
                Environment = fullContext.Environment
            },
            
            AgentContextScope.Standard => new ScopedGridContext
            {
                // All properties except sensitive baggage
                CorrelationId = fullContext.CorrelationId,
                CausationId = fullContext.CausationId,
                NodeId = fullContext.NodeId,
                StudioId = fullContext.StudioId,
                Environment = fullContext.Environment,
                Baggage = FilterSensitiveBaggage(fullContext.Baggage)
            },
            
            AgentContextScope.Full => fullContext, // No filtering
            
            _ => throw new ArgumentException($"Unknown scope: {scope}")
        };
    }
    
    private IReadOnlyDictionary<string, string> FilterSensitiveBaggage(
        IReadOnlyDictionary<string, string> baggage)
    {
        return baggage
            .Where(kvp => !kvp.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => !kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase))
            .Where(kvp => !kvp.Key.Contains("token", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Complete Agent Execution Example

```csharp
// 1. Define Agent with Capabilities
public class CustomerSupportAgent : IAgentDescriptor
{
    public string AgentId => "support-agent-gpt4";
    public string Name => "Customer Support AI Assistant";
    public string AgentType => "llm-assistant";
    public string Version => "1.0.0";
    
    public IReadOnlyList<IAgentCapability> Capabilities => new IAgentCapability[]
    {
        new ReadDatabaseCapability(),
        new InvokeApiCapability("crm-api"),
        new SendNotificationCapability()
    };
    
    public AgentContextScope ContextScope => AgentContextScope.Standard;
    
    public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>
    {
        ["model"] = "gpt-4",
        ["temperature"] = "0.7",
        ["maxTokens"] = "2000"
    };
    
    public bool HasCapability(string capabilityName) => 
        Capabilities.Any(c => c.Name == capabilityName);
}

// 2. Execute Agent with Factory
public class AgentExecutionService(
    IAgentExecutionContextFactory contextFactory,
    ILogger<AgentExecutionService> logger)
{
    public async Task<AgentResult> ExecuteAsync(
        IAgentDescriptor agent,
        IGridContext gridContext,
        Dictionary<string, object?> parameters)
    {
        // Use factory to create scoped execution context
        using var execContext = contextFactory.Create(
            agent: agent,
            gridContext: gridContext,
            executionMetadata: parameters);
        
        logger.LogInformation(
            "Starting agent {AgentId} execution with correlation {CorrelationId}",
            agent.AgentId,
            execContext.GridContext.CorrelationId);
        
        try
        {
            // Validate capabilities before execution
            // Note: In real Nodes, ValidateParameters is typically called only when 
            // invoking a capability, not for every capability at start of execution.
            foreach (var capability in agent.Capabilities)
            {
                if (!capability.ValidateParameters(parameters, out var error))
                {
                    throw new InvalidOperationException(
                        $"Parameter validation failed for {capability.Name}: {error}");
                }
            }
            
            // Execute agent logic
            var result = await InvokeAgentLogicAsync(execContext, parameters);
            
            // Track execution metrics
            execContext.AddMetadata("execution_time_ms", result.ExecutionTimeMs);
            execContext.AddMetadata("tools_invoked", result.ToolsInvoked.Count);
            execContext.OperationContext.Complete();
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent execution failed");
            execContext.OperationContext.Fail($"Agent failed: {ex.Message}", ex);
            throw;
        }
    } // execContext disposed automatically
}
```

[↑ Back to top](#table-of-contents)

---

## AgentsInterop - Serialization and Context Marshaling

**Location:** `HoneyDrunk.Kernel/AgentsInterop/`

The AgentsInterop subsystem provides serialization, context marshaling, and result projection for agents executing across process boundaries (LLMs, remote workers, sandboxed scripts).

---

### AgentExecutionResult.cs

#### What it is
Serializable record representing the outcome of an agent execution.

#### Properties

```csharp
public sealed record AgentExecutionResult
{
    public string? AgentId { get; init; }              // Agent identifier
    public string? CorrelationId { get; init; }        // Correlation for tracing
    public bool Success { get; init; }                  // Whether execution succeeded
    public JsonElement? Result { get; init; }           // Execution result (dynamic JSON)
    public string? ErrorMessage { get; init; }          // Error message if failed
    public DateTimeOffset StartedAtUtc { get; init; }  // Execution start time
    public DateTimeOffset CompletedAtUtc { get; init; } // Execution end time
    public Dictionary<string, JsonElement>? Metadata { get; init; } // Execution metadata
}
```

#### Usage Example

```csharp
public class AgentExecutor
{
    public async Task<AgentExecutionResult> ExecuteRemoteAgentAsync(
        IAgentDescriptor agent,
        IGridContext gridContext,
        object input)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            // Execute agent logic
            var result = await InvokeAgentAsync(agent, input);
            
            return new AgentExecutionResult
            {
                AgentId = agent.AgentId,
                CorrelationId = gridContext.CorrelationId,
                Success = true,
                Result = JsonSerializer.SerializeToElement(result),
                StartedAtUtc = startTime,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, JsonElement>
                {
                    ["tokensUsed"] = JsonSerializer.SerializeToElement(1500),
                    ["model"] = JsonSerializer.SerializeToElement("gpt-4")
                }
            };
        }
        catch (Exception ex)
        {
            return new AgentExecutionResult
            {
                AgentId = agent.AgentId,
                CorrelationId = gridContext.CorrelationId,
                Success = false,
                ErrorMessage = ex.Message,
                StartedAtUtc = startTime,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

### AgentResultSerializer.cs

#### What it is
Serializes agent execution results to JSON for Grid consumption.

#### Methods

```csharp
public sealed class AgentResultSerializer
{
    // Serialize execution result to JSON
    public static string SerializeResult(
        IAgentExecutionContext context,
        bool success,
        object? result = null,
        string? errorMessage = null);
    
    // Deserialize JSON to AgentExecutionResult
    public static AgentExecutionResult? DeserializeResult(string json);
}
```

#### Usage Example

```csharp
public class AgentService(IAgentExecutionContextFactory contextFactory)
{
    public async Task<string> ExecuteAndSerializeAsync(
        IAgentDescriptor agent,
        IGridContext gridContext,
        object input)
    {
        using var execContext = contextFactory.Create(agent, gridContext);
        
        try
        {
            var result = await ExecuteAgentLogicAsync(input);
            
            // Serialize success result
            return AgentResultSerializer.SerializeResult(
                context: execContext,
                success: true,
                result: result
            );
        }
        catch (Exception ex)
        {
            // Serialize failure result
            return AgentResultSerializer.SerializeResult(
                context: execContext,
                success: false,
                errorMessage: ex.Message
            );
        }
    }
    
    public AgentExecutionResult? ParseResult(string json)
    {
        return AgentResultSerializer.DeserializeResult(json);
    }
}
```

**Serialized Output:**
```json
{
  "agentId": "order-processor-v2",
  "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
  "success": true,
  "result": {
    "ordersProcessed": 150,
    "totalAmount": 45000.00
  },
  "errorMessage": null,
  "startedAtUtc": "2025-01-11T10:30:00Z",
  "completedAtUtc": "2025-01-11T10:30:45Z",
  "metadata": {
    "tokensUsed": 1500,
    "model": "gpt-4",
    "executionTimeMs": 45000
  }
}
```

[↑ Back to top](#table-of-contents)

---

### GridContextSerializer.cs

#### What it is
Serializes GridContext for agent consumption with automatic security filtering.

#### Methods

```csharp
public sealed class GridContextSerializer
{
    // Serialize GridContext to JSON (with optional full baggage)
    public static string Serialize(
        IGridContext context, 
        bool includeFullBaggage = false);
    
    // Deserialize JSON to GridContext
    public static IGridContext? Deserialize(string json);
}
```

#### Security Filtering

The serializer **automatically filters sensitive baggage keys** by default. **Only baggage keys are filtered** - all top-level fields (`TenantId`, `ProjectId`, `NodeId`, `StudioId`, `Environment`, `CorrelationId`, `CausationId`, `CreatedAtUtc`) are always included unless the agent's `AgentContextScope` reduces visibility.

**Filtered Baggage Keys:**
- Contains "secret"
- Contains "password"
- Contains "token"
- Contains "key"
- Contains "credential"

#### Usage Example

```csharp
public class AgentContextProvider
{
    public string CreateAgentContext(IGridContext gridContext, bool isTrustedAgent)
    {
        // For untrusted agents: filter sensitive baggage
        if (!isTrustedAgent)
        {
            return GridContextSerializer.Serialize(gridContext, includeFullBaggage: false);
        }
        
        // For trusted agents: include full context
        return GridContextSerializer.Serialize(gridContext, includeFullBaggage: true);
    }
    
    public IGridContext? RestoreContext(string json)
    {
        return GridContextSerializer.Deserialize(json);
    }
}
```

**Serialized Output (Filtered):**
```json
{
  "correlationId": "01HQXZ8K4TJ9X5B3N2YGF7WDCQ",
  "causationId": "01HQXY7J3SI8W4A2M1XE6UFBZP",
  "nodeId": "order-service",
  "studioId": "honeycomb-prod",
  "environment": "production",
  "createdAtUtc": "2025-01-11T10:30:00Z",
  "baggage": {
    "TenantId": "01HQXZ7G5FH4C2B1N0XD5EWARP",
    "UserId": "user-123"
    // "ApiToken" filtered out (contains "token")
    // "SecretKey" filtered out (contains "secret")
  }
}
```

[↑ Back to top](#table-of-contents)

---

### AgentContextProjection.cs

#### What it is
**Internal primitive** that projects GridContext + OperationContext + AgentDescriptor into a complete `IAgentExecutionContext`.

**Note:** In most cases, use `IAgentExecutionContextFactory` instead. This projection is the underlying primitive used by the factory and is available for advanced scenarios and testing.

#### Method

```csharp
public static class AgentContextProjection
{
    public static IAgentExecutionContext ProjectToAgentContext(
        IGridContext gridContext,
        IOperationContext operationContext,
        IAgentDescriptor agentDescriptor,
        IReadOnlyDictionary<string, object?>? executionMetadata = null);
}
```

#### When to Use

- ✅ **Testing** - Direct context creation without DI
- ✅ **Advanced scenarios** - When you need full control over context composition
- ⚠️ **Not recommended for production** - Use `IAgentExecutionContextFactory` instead

#### Usage Example

```csharp
// Testing scenario - direct projection without DI
public class AgentTests
{
    [Fact]
    public void TestAgentExecution()
    {
        // Arrange
        var gridContext = new GridContext(...);
        var operationContext = new OperationContext(...);
        var agent = new TestAgentDescriptor();
        
        // Act - direct projection
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            gridContext: gridContext,
            operationContext: operationContext,
            agentDescriptor: agent
        );
        
        // Assert
        Assert.Equal(agent.AgentId, agentContext.Agent.AgentId);
        Assert.Equal(gridContext.CorrelationId, agentContext.GridContext.CorrelationId);
    }
}

// Production scenario - use factory instead
public class AgentExecutor(IAgentExecutionContextFactory factory)
{
    public async Task ExecuteAsync(IAgentDescriptor agent, IGridContext grid)
    {
        // Preferred: Use factory
        using var context = factory.Create(agent, grid);
        await ExecuteAgentLogicAsync(context);
    }
}
```

### Testing Fixtures and Helpers

The examples above show unit-style testing with manual context creation. For more comprehensive testing scenarios, consider using **HoneyDrunk.Testing** fixtures:

**Recommended for Integration Tests:**
- **Fake IGridContext** - Pre-configured with consistent test data
- **Fake IOperationContext** - Deterministic timing and tracking
- **In-Memory Agent Runners** - Stub implementations for agent execution
- **Test Descriptors** - Reusable agent descriptors with known capabilities

**Example with Testing Package (Future):**
```csharp
[Fact]
public async Task ExecuteAgent_WithTestFixture()
{
    // Arrange
    using var fixture = new HoneyDrunkTestFixture();
    var agent = fixture.CreateTestAgent(
        capabilities: ["read-database", "invoke-api"]);
    var gridContext = fixture.CreateTestGridContext(
        nodeId: "test-node",
        environment: "test");
    
    // Act
    var result = await fixture.ExecuteAgentAsync(agent, gridContext);
    
    // Assert
    Assert.True(result.Success);
}
```

See **HoneyDrunk.Testing** documentation for available fixtures, builders, and test helpers.

[↑ Back to top](#table-of-contents)

---

## Summary

| Component | Purpose | Entry Point |
|-----------|---------|-------------|
| **IAgentDescriptor** | Agent identity & capabilities | Declarative permissions |
| **IAgentExecutionContextFactory** | Context composition | **Primary entry point** |
| **IAgentExecutionContext** | Execution tracking | Scoped Grid context |
| **IAgentCapability** | Fine-grained permissions | Parameter validation |
| **AgentContextScope** | Context visibility | Filtered baggage |
| **AgentExecutionResult** | Serializable outcome | JSON-safe |
| **AgentResultSerializer** | Result marshaling | Structured JSON |
| **GridContextSerializer** | Context marshaling | Automatic secret filtering |
| **AgentContextProjection** | Context composition (internal) | Testing/advanced scenarios only |

**Key Benefits:**
- ✅ **Unified execution model** - `IAgentExecutionContextFactory` ensures consistent context creation
- ✅ Agents operate with scoped permissions and context
- ✅ Grid context automatically filtered for security
- ✅ Structured serialization for cross-process communication
- ✅ Execution results traceable via CorrelationId
- ✅ Metadata tracking for LLM token usage, timing, etc.
- ✅ Type-safe projection from Grid primitives to Agent context

**Architecture:**
- **Factory Pattern:** `IAgentExecutionContextFactory` is the primary entry point (registered in DI)
- **Internal Primitive:** `AgentContextProjection` handles the actual projection logic
- **Composition:** Factory composes GridContext + OperationContext + AgentDescriptor + metadata
- **Resource Management:** Scoped contexts with automatic disposal

**Security Guidelines:**
- Use `GridContextSerializer` with `includeFullBaggage: false` for untrusted agents
- Always validate agent capabilities before execution
- Filter sensitive baggage keys (automatic in serializer)
- Use `AgentContextScope` to limit context visibility
- Track execution metadata for audit and billing

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

---

## Cross-Process Agent Flow

```plaintext
┌─────────────────────────────────────────────────────────────┐
│                    Orchestrator Node                         │
│  ┌─────────────────────────────────────────────────┐        │
│  │ 1. Create AgentExecutionContext                 │        │
│  │    - GridContext                                 │        │
│  │    - OperationContext                            │        │
│  │    - AgentDescriptor                             │        │
│  └──────────────────┬──────────────────────────────┘        │
│                     │                                         │
│  ┌─────────────────▼──────────────────────────────┐        │
│  │ 2. Serialize GridContext (filtered)             │        │
│  │    - GridContextSerializer.Serialize()          │        │
│  │    - Removes sensitive baggage                  │        │
│  └──────────────────┬──────────────────────────────┘        │
└────────────────────┼─────────────────────────────────────────┘
                     │
         ╔═══════════▼═══════════╗
         ║   HTTP POST /execute  ║
         ║   (JSON payload)      ║
         ╚═══════════╤═══════════╝
                     │
┌────────────────────▼─────────────────────────────────────────┐
│                    Agent Runner Node                          │
│  ┌─────────────────────────────────────────────────┐        │
│  │ 3. Deserialize GridContext                      │        │
│  │    - GridContextSerializer.Deserialize()        │        │
│  └──────────────────┬──────────────────────────────┘        │
│                     │                                         │
│  ┌─────────────────▼──────────────────────────────┐        │
│  │ 4. Execute Agent Logic                          │        │
│  │    - LLM invocation                             │        │
│  │    - Tool execution                             │        │
│  │    - Result generation                          │        │
│  └──────────────────┬──────────────────────────────┘        │
│                     │                                         │
│  ┌─────────────────▼──────────────────────────────┐        │
│  │ 5. Serialize Result                             │        │
│  │    - AgentResultSerializer.SerializeResult()    │        │
│  │    - Include metadata (tokens, model, timing)   │        │
│  └──────────────────┬──────────────────────────────┘        │
└────────────────────┼─────────────────────────────────────────┘
                     │
         ╔═══════════▼═══════════╗
         ║   HTTP 200 OK         ║
         ║   (AgentExecutionResult) ║
         ╚═══════════╤═══════════╝
                     │
┌────────────────────▼─────────────────────────────────────────┐
│                    Orchestrator Node                          │
│  ┌─────────────────────────────────────────────────┐        │
│  │ 6. Deserialize Result                           │        │
│  │    - AgentResultSerializer.DeserializeResult()  │        │
│  └──────────────────┬──────────────────────────────┘        │
│                     │                                         │
│  ┌─────────────────▼──────────────────────────────┐        │
│  │ 7. Complete OperationContext                    │        │
│  │    - Track metrics (duration, success)          │        │
│  │    - Log to Pulse                               │        │
│  └─────────────────────────────────────────────────┘        │
└──────────────────────────────────────────────────────────────┘
```

**Transport Adapters:** This diagram shows an HTTP-based agent runner for clarity. The same pattern applies to other transport mechanisms:

- **HTTP/REST** - Shown above (synchronous request/response)
- **Service Bus / Message Queues** - Context serialized into message properties via **HoneyDrunk.Transport**
- **Background Jobs** - Context serialized into job metadata via **HoneyDrunk.Jobs**  
- **gRPC** - Context in gRPC metadata headers

**Kernel Primitives Used Across All Transports:**
- `GridContextSerializer` - Serialize/deserialize context for transport
- `AgentResultSerializer` - Serialize/deserialize results
- `IAgentExecutionContextFactory` - Reconstruct execution context on orchestrator side

All transport adapters follow the same flow:
1. Serialize GridContext (filtered for security)
2. Execute remotely with appropriate transport
3. Return AgentExecutionResult
4. Track execution in OperationContext

See [Transport.md](Transport.md) for message-based agent invocation patterns and [Jobs.md](Jobs.md) for background agent execution.

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)


