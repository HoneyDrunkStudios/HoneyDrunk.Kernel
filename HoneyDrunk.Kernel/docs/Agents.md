# 🤖 Agents - Agent Execution Framework

[← Back to File Guide](FILE_GUIDE.md)

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

---

## Testing Patterns

```csharp
[Fact]
public void AgentDescriptor_HasCapability_ReturnsTrue()
{
    // Arrange
    var agent = new CustomerSupportAgent();
    
    // Act & Assert
    Assert.True(agent.HasCapability("read-database"));
    Assert.False(agent.HasCapability("delete-records"));
}

[Fact]
public void AgentCapability_ValidateParameters_ReturnsFalseForInvalidMethod()
{
    // Arrange
    var capability = new InvokeApiCapability("test-api");
    var parameters = new Dictionary<string, object?>
    {
        ["method"] = "DELETE" // Not allowed
    };
    
    // Act
    var isValid = capability.ValidateParameters(parameters, out var errorMessage);
    
    // Assert
    Assert.False(isValid);
    Assert.Contains("not allowed", errorMessage);
}

[Fact]
public void ContextScopeEnforcer_CorrelationOnly_FiltersContext()
{
    // Arrange
    var fullContext = new GridContext
    {
        CorrelationId = "corr-123",
        NodeId = "sensitive-node",
        Baggage = new Dictionary<string, string> { ["secret"] = "value" }
    };
    var enforcer = new ContextScopeEnforcer();
    
    // Act
    var scoped = enforcer.CreateScopedContext(fullContext, AgentContextScope.CorrelationOnly);
    
    // Assert
    Assert.Equal("corr-123", scoped.CorrelationId);
    Assert.Null(scoped.NodeId); // Filtered out
    Assert.Empty(scoped.Baggage); // Filtered out
}
```

---

## Summary

| Component | Purpose | Scope |
|-----------|---------|-------|
| **IAgentDescriptor** | Agent identity & permissions | Static definition |
| **IAgentExecutionContext** | Execution tracking & access control | Per-execution |
| **IAgentCapability** | Fine-grained permissions | Per-action |
| **AgentContextScope** | Context visibility control | Per-agent |

**Key Patterns:**
- Agents have declarative capabilities (not implicit permissions)
- Context scoping protects sensitive data
- Execution tracking enables observability and auditing
- Capability validation prevents unauthorized actions

**Security Guidelines:**
- Use `CorrelationOnly` or `NodeAndCorrelation` for untrusted agents
- Use `Standard` for most LLM assistants (filters secrets)
- Reserve `Full` scope for privileged automation
- Always validate capability parameters before execution

---

[← Back to File Guide](FILE_GUIDE.md)

