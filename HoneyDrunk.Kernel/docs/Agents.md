# 🤖 Agents - Agent Execution Framework

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [IAgentDescriptor.cs](#iagentdescriptorcs)
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
| `Standard` | Non-sensitive data | All except secret baggage |
| `Full` | Complete access | All Grid context including baggage |

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

// 2. Execute Agent with Context
public class AgentExecutionService(
    IAgentExecutionContextFactory contextFactory,
    ILogger<AgentExecutionService> logger)
{
    public async Task<AgentResult> ExecuteAsync(
        IAgentDescriptor agent,
        IGridContext gridContext,
        Dictionary<string, object?> parameters)
    {
        using var execContext = contextFactory.Create(agent, gridContext);
        
        logger.LogInformation(
            "Starting agent {AgentId} execution with correlation {CorrelationId}",
            agent.AgentId,
            execContext.GridContext.CorrelationId);
        
        try
        {
            // Validate capabilities before execution
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
    }
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

The serializer **automatically filters sensitive baggage keys** by default:

**Filtered Keys:**
- Contains "secret"
- Contains "password"
- Contains "token"
- Contains "key"
- Contains "credential"

#### Usage Example

```csharp
public class AgentContextProvider
{
    public string CreateAgentContext(IGridContext gridContext, bool trustLevel)
    {
        // For untrusted agents: filter sensitive baggage
        if (!trustLevel)
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
Projects GridContext + OperationContext + AgentDescriptor into a complete `IAgentExecutionContext`.

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

#### Usage Example

```csharp
public class AgentOrchestrator(
    IGridContext gridContext,
    IOperationContext operationContext)
{
    public IAgentExecutionContext CreateAgentContext(
        IAgentDescriptor agent,
        Dictionary<string, object?> metadata)
    {
        // Project Grid and Operation contexts into Agent context
        return AgentContextProjection.ProjectToAgentContext(
            gridContext: gridContext,
            operationContext: operationContext,
            agentDescriptor: agent,
            executionMetadata: metadata
        );
    }
}
```

[↑ Back to top](#table-of-contents)

---

### Complete AgentsInterop Example

```csharp
public class RemoteAgentExecutor(
    IGridContext gridContext,
    IOperationContext operationContext,
    IHttpClientFactory httpClientFactory,
    ILogger<RemoteAgentExecutor> logger)
{
    public async Task<AgentExecutionResult?> ExecuteRemoteAsync(
        IAgentDescriptor agent,
        object input)
    {
        // 1. Project context for agent
        var agentContext = AgentContextProjection.ProjectToAgentContext(
            gridContext: gridContext,
            operationContext: operationContext,
            agentDescriptor: agent,
            executionMetadata: new Dictionary<string, object?>
            {
                ["input"] = input,
                ["startTime"] = DateTimeOffset.UtcNow
            }
        );
        
        // 2. Serialize GridContext for agent (filtered)
        var contextJson = GridContextSerializer.Serialize(
            context: gridContext,
            includeFullBaggage: false
        );
        
        // 3. Call remote agent endpoint
        var client = httpClientFactory.CreateClient();
        var requestBody = new
        {
            agentId = agent.AgentId,
            context = contextJson,
            input = input
        };
        
        var response = await client.PostAsJsonAsync(
            "https://agent-runner.grid/execute",
            requestBody
        );
        
        response.EnsureSuccessStatusCode();
        
        // 4. Deserialize result
        var resultJson = await response.Content.ReadAsStringAsync();
        var result = AgentResultSerializer.DeserializeResult(resultJson);
        
        // 5. Track execution metadata
        if (result is not null)
        {
            agentContext.AddMetadata("executionDurationMs", 
                (result.CompletedAtUtc - result.StartedAtUtc).TotalMilliseconds);
            agentContext.AddMetadata("success", result.Success);
            
            if (result.Success)
            {
                operationContext.Complete();
            }
            else
            {
                operationContext.Fail($"Agent failed: {result.ErrorMessage}");
            }
        }
        
        logger.LogInformation(
            "Agent {AgentId} executed with correlation {CorrelationId}: {Success}",
            agent.AgentId,
            gridContext.CorrelationId,
            result?.Success ?? false
        );
        
        return result;
    }
}
```

[↑ Back to top](#table-of-contents)

---

### Cross-Process Agent Flow

```
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

[↑ Back to top](#table-of-contents)

---

## Testing Patterns

```csharp
[Fact]
public void GridContextSerializer_FiltersSecrets()
{
    // Arrange
    var context = new GridContext(
        correlationId: "corr-123",
        nodeId: "test-node",
        studioId: "test-studio",
        environment: "test",
        baggage: new Dictionary<string, string>
        {
            ["TenantId"] = "tenant-123",         // Safe
            ["ApiToken"] = "secret-token-abc",   // Filtered
            ["SecretKey"] = "key-xyz"            // Filtered
        }
    );
    
    // Act
    var json = GridContextSerializer.Serialize(context, includeFullBaggage: false);
    var restored = GridContextSerializer.Deserialize(json);
    
    // Assert
    Assert.NotNull(restored);
    Assert.True(restored.Baggage.ContainsKey("TenantId"));
    Assert.False(restored.Baggage.ContainsKey("ApiToken"));   // Filtered out
    Assert.False(restored.Baggage.ContainsKey("SecretKey")); // Filtered out
}

[Fact]
public void AgentResultSerializer_RoundTrip()
{
    // Arrange
    var agent = new TestAgentDescriptor();
    var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
    var operationContext = new OperationContext(gridContext, new NodeContext());
    var execContext = AgentContextProjection.ProjectToAgentContext(
        gridContext, operationContext, agent);
    
    // Act
    var json = AgentResultSerializer.SerializeResult(
        context: execContext,
        success: true,
        result: new { ordersProcessed = 10 }
    );
    
    var result = AgentResultSerializer.DeserializeResult(json);
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.Equal("corr-123", result.CorrelationId);
    Assert.Equal(10, result.Result?.GetProperty("ordersProcessed").GetInt32());
}

[Fact]
public void AgentContextProjection_CreatesExecutionContext()
{
    // Arrange
    var gridContext = new GridContext("corr-123", "test-node", "test-studio", "test");
    var operationContext = new OperationContext(gridContext, new NodeContext());
    var agent = new TestAgentDescriptor();
    
    // Act
    var execContext = AgentContextProjection.ProjectToAgentContext(
        gridContext, operationContext, agent);
    
    // Assert
    Assert.NotNull(execContext);
    Assert.Equal("test-agent", execContext.Agent.AgentId);
    Assert.Equal("corr-123", execContext.GridContext.CorrelationId);
    Assert.NotNull(execContext.OperationContext);
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

| Component | Purpose | Security |
|-----------|---------|----------|
| **IAgentDescriptor** | Agent identity & capabilities | Declarative permissions |
| **IAgentExecutionContext** | Execution tracking | Scoped Grid context |
| **IAgentCapability** | Fine-grained permissions | Parameter validation |
| **AgentContextScope** | Context visibility | Filtered baggage |
| **AgentExecutionResult** | Serializable outcome | JSON-safe |
| **AgentResultSerializer** | Result marshaling | Structured JSON |
| **GridContextSerializer** | Context marshaling | Automatic secret filtering |
| **AgentContextProjection** | Context composition | Combines Grid+Operation+Agent |

**Key Benefits:**
- ✅ Agents operate with scoped permissions and context
- ✅ Grid context automatically filtered for security
- ✅ Structured serialization for cross-process communication
- ✅ Execution results traceable via CorrelationId
- ✅ Metadata tracking for LLM token usage, timing, etc.
- ✅ Type-safe projection from Grid primitives to Agent context

**Security Guidelines:**
- Use `GridContextSerializer` with `includeFullBaggage: false` for untrusted agents
- Always validate agent capabilities before execution
- Filter sensitive baggage keys (automatic in serializer)
- Use `AgentContextScope` to limit context visibility
- Track execution metadata for audit and billing

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)

