# 🧪 Testing - Test Patterns and Practices

[← Back to File Guide](FILE_GUIDE.md)

---

## Overview

Testing guidance and patterns for HoneyDrunk.Kernel, including unit tests, integration tests, and testing best practices for Grid-aware applications.

**Location:** `HoneyDrunk.Kernel.Tests/`

**Test Framework:** xUnit + FluentAssertions

---

## Test Structure

### Context Tests
- **GridContextTests.cs** - Grid context behavior and causation chains
- **NodeContextTests.cs** - Node identity and lifecycle stage tracking
- **OperationContextTests.cs** - Operation tracking, timing, and outcome

### Identity Tests
- **NodeIdTests.cs** - NodeId validation rules and parsing
- **TenantIdTests.cs** - TenantId ULID behavior and conversions

---

## Testing Best Practices

### Mocking Context

#### GridContext

```csharp
// Create test GridContext with known values
var testContext = new GridContext(
    correlationId: "test-corr-123",
    causationId: null,
    nodeId: "test-node",
    studioId: "test-studio",
    environment: "test",
    baggage: new Dictionary<string, string>
    {
        ["tenant_id"] = "test-tenant",
        ["user_id"] = "test-user"
    },
    cancellation: CancellationToken.None);
```

#### NodeContext

```csharp
// Mock NodeContext for testing
var mockNodeContext = new Mock<INodeContext>();
mockNodeContext.Setup(c => c.NodeId).Returns("test-node");
mockNodeContext.Setup(c => c.Version).Returns("1.0.0-test");
mockNodeContext.Setup(c => c.StudioId).Returns("test-studio");
mockNodeContext.Setup(c => c.Environment).Returns("test");
mockNodeContext.Setup(c => c.LifecycleStage).Returns(NodeLifecycleStage.Running);
mockNodeContext.Setup(c => c.StartedAtUtc).Returns(DateTimeOffset.UtcNow.AddMinutes(-5));
```

#### OperationContext

```csharp
// Create test OperationContext
using var testOperation = new OperationContext(
    gridContext: testContext,
    operationName: "TestOperation",
    metadata: new Dictionary<string, object?>
    {
        ["test_key"] = "test_value"
    });
```

---

### Testing Identity Types

#### NodeId Validation

```csharp
[Theory]
[InlineData("valid-node-id", true)]
[InlineData("payment-node", true)]
[InlineData("api-v2", true)]
[InlineData("Invalid_ID", false)] // Underscore not allowed
[InlineData("UPPERCASE", false)] // Must be lowercase
[InlineData("ab", false)] // Too short
[InlineData("starts-", false)] // Cannot end with hyphen
[InlineData("-starts", false)] // Cannot start with hyphen
[InlineData("double--hyphen", false)] // No consecutive hyphens
public void NodeId_Validation(string value, bool expected)
{
    var isValid = NodeId.TryParse(value, out _);
    Assert.Equal(expected, isValid);
}

[Fact]
public void NodeId_InvalidFormat_ThrowsException()
{
    var ex = Assert.Throws<ArgumentException>(() => new NodeId("Invalid_ID"));
    Assert.Contains("kebab-case", ex.Message);
}
```

#### ULID-based IDs

```csharp
[Fact]
public void CorrelationId_NewId_GeneratesUniqueIds()
{
    var id1 = CorrelationId.NewId();
    var id2 = CorrelationId.NewId();
    
    Assert.NotEqual(id1.ToString(), id2.ToString());
}

[Fact]
public void CorrelationId_MaintainsSortOrder()
{
    var ids = new List<CorrelationId>();
    
    for (int i = 0; i < 10; i++)
    {
        ids.Add(CorrelationId.NewId());
        Thread.Sleep(1); // Ensure different timestamps
    }
    
    var sorted = ids.OrderBy(id => id.ToString()).ToList();
    Assert.Equal(ids, sorted);
}

[Fact]
public void TenantId_ToFromUlid_RoundTrips()
{
    var original = TenantId.NewId();
    var ulid = original.ToUlid();
    var roundTripped = TenantId.FromUlid(ulid);
    
    Assert.Equal(original, roundTripped);
}
```

---

### Testing Context Behavior

#### GridContext Causation Chains

```csharp
[Fact]
public void GridContext_CreateChildContext_SetsCausationId()
{
    // Arrange
    var parentContext = new GridContext(
        correlationId: "parent-123",
        causationId: null,
        nodeId: "parent-node",
        studioId: "test-studio",
        environment: "test",
        baggage: new Dictionary<string, string>(),
        cancellation: CancellationToken.None);
    
    // Act
    var childContext = parentContext.CreateChildContext("child-node");
    
    // Assert
    Assert.NotEqual(parentContext.CorrelationId, childContext.CorrelationId);
    Assert.Equal(parentContext.CorrelationId, childContext.CausationId);
    Assert.Equal("child-node", childContext.NodeId);
}

[Fact]
public void GridContext_WithBaggage_AddsMetadata()
{
    // Arrange
    var context = new GridContext(
        correlationId: "test-123",
        causationId: null,
        nodeId: "test-node",
        studioId: "test-studio",
        environment: "test",
        baggage: new Dictionary<string, string>(),
        cancellation: CancellationToken.None);
    
    // Act
    var enrichedContext = context
        .WithBaggage("key1", "value1")
        .WithBaggage("key2", "value2");
    
    // Assert
    Assert.Equal("value1", enrichedContext.Baggage["key1"]);
    Assert.Equal("value2", enrichedContext.Baggage["key2"]);
}
```

#### OperationContext Tracking

```csharp
[Fact]
public void OperationContext_Complete_SetsSuccessStatus()
{
    // Arrange
    using var operation = new OperationContext(testContext, "TestOp");
    
    // Act
    operation.Complete();
    
    // Assert
    Assert.True(operation.IsSuccess);
    Assert.NotNull(operation.CompletedAtUtc);
    Assert.Null(operation.ErrorMessage);
}

[Fact]
public void OperationContext_Fail_SetsErrorStatus()
{
    // Arrange
    using var operation = new OperationContext(testContext, "TestOp");
    var exception = new InvalidOperationException("Test error");
    
    // Act
    operation.Fail("Operation failed", exception);
    
    // Assert
    Assert.False(operation.IsSuccess);
    Assert.NotNull(operation.CompletedAtUtc);
    Assert.Contains("Operation failed", operation.ErrorMessage);
}

[Fact]
public void OperationContext_AddMetadata_StoresValues()
{
    // Arrange
    using var operation = new OperationContext(testContext, "TestOp");
    
    // Act
    operation.AddMetadata("key1", "value1");
    operation.AddMetadata("key2", 42);
    
    // Assert
    Assert.Equal("value1", operation.Metadata["key1"]);
    Assert.Equal(42, operation.Metadata["key2"]);
}
```

---

### Testing Lifecycle Components

#### Startup Hooks

```csharp
[Fact]
public async Task StartupHook_ExecutesInPriorityOrder()
{
    // Arrange
    var executionOrder = new List<string>();
    
    var hook1 = new TestStartupHook("Hook1", -100, () => executionOrder.Add("Hook1"));
    var hook2 = new TestStartupHook("Hook2", 0, () => executionOrder.Add("Hook2"));
    var hook3 = new TestStartupHook("Hook3", 100, () => executionOrder.Add("Hook3"));
    
    var hooks = new[] { hook3, hook1, hook2 }; // Registered out of order
    
    // Act
    foreach (var hook in hooks.OrderBy(h => h.Priority))
    {
        await hook.ExecuteAsync(CancellationToken.None);
    }
    
    // Assert
    Assert.Equal(new[] { "Hook1", "Hook2", "Hook3" }, executionOrder);
}
```

#### Health Contributors

```csharp
[Fact]
public async Task HealthContributor_Critical_Unhealthy_FailsNode()
{
    // Arrange
    var contributor = new TestHealthContributor("Database", isCritical: true)
    {
        Status = HealthStatus.Unhealthy,
        Message = "Database unreachable"
    };
    
    // Act
    var (status, message) = await contributor.CheckHealthAsync();
    
    // Assert
    Assert.Equal(HealthStatus.Unhealthy, status);
    Assert.Contains("Database unreachable", message);
}

[Fact]
public async Task HealthContributor_NonCritical_Unhealthy_DoesNotFailNode()
{
    // Arrange
    var contributor = new TestHealthContributor("Cache", isCritical: false)
    {
        Status = HealthStatus.Degraded
    };
    
    // Act
    var (status, _) = await contributor.CheckHealthAsync();
    
    // Assert - Non-critical can be degraded without failing Node
    Assert.Equal(HealthStatus.Degraded, status);
}
```

---

### Testing Agent Components

```csharp
[Fact]
public void AgentDescriptor_HasCapability_ReturnsCorrectResult()
{
    // Arrange
    var agent = new TestAgentDescriptor
    {
        Capabilities = new[]
        {
            new TestCapability("read-database"),
            new TestCapability("invoke-api")
        }
    };
    
    // Act & Assert
    Assert.True(agent.HasCapability("read-database"));
    Assert.True(agent.HasCapability("invoke-api"));
    Assert.False(agent.HasCapability("delete-records"));
}

[Fact]
public void AgentCapability_ValidateParameters_RejectsInvalid()
{
    // Arrange
    var capability = new InvokeApiCapability("test-api");
    var invalidParams = new Dictionary<string, object?>
    {
        ["method"] = "DELETE" // Not allowed by capability
    };
    
    // Act
    var isValid = capability.ValidateParameters(invalidParams, out var errorMessage);
    
    // Assert
    Assert.False(isValid);
    Assert.Contains("not allowed", errorMessage);
}
```

---

### Testing with Deterministic Time

```csharp
// Use a test clock for deterministic time-based tests
public class TestClock : IClock
{
    private DateTimeOffset _currentTime;
    
    public TestClock(DateTimeOffset startTime)
    {
        _currentTime = startTime;
    }
    
    public DateTimeOffset UtcNow => _currentTime;
    
    public long GetTimestamp() => _currentTime.Ticks;
    
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}

[Fact]
public async Task Operation_Timeout_DetectedWithTestClock()
{
    // Arrange
    var clock = new TestClock(DateTimeOffset.UtcNow);
    var operation = new OperationContext(testContext, "TestOp", clock);
    
    // Act
    clock.Advance(TimeSpan.FromSeconds(30));
    operation.Complete();
    
    // Assert
    var duration = operation.CompletedAtUtc.Value - operation.StartedAtUtc;
    Assert.Equal(TimeSpan.FromSeconds(30), duration);
}
```

---

### Integration Testing Patterns

#### Testing with Dependency Injection

```csharp
public class ServiceTests
{
    private readonly ServiceProvider _serviceProvider;
    
    public ServiceTests()
    {
        var services = new ServiceCollection();
        
        // Register Kernel services
        services.AddHoneyDrunkCore(options =>
        {
            options.NodeId = "test-node";
            options.StudioId = "test-studio";
            options.Environment = "test";
        });
        
        // Register test services
        services.AddSingleton<IMyService, MyService>();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void Service_ReceivesGridContext()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<IMyService>();
        
        // Act & Assert
        Assert.NotNull(service);
        // Service should have IGridContext injected
    }
}
```

#### Testing Context Mappers

```csharp
[Fact]
public void HttpContextMapper_MapsHeaders()
{
    // Arrange
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Headers["X-Correlation-ID"] = "test-123";
    httpContext.Request.Headers["X-Causation-ID"] = "test-456";
    httpContext.Request.Headers["X-Baggage-tenant"] = "tenant-789";
    
    var mapper = new HttpContextMapper();
    
    // Act
    var gridContext = mapper.Map(httpContext);
    
    // Assert
    Assert.Equal("test-123", gridContext.CorrelationId);
    Assert.Equal("test-456", gridContext.CausationId);
    Assert.Equal("tenant-789", gridContext.Baggage["tenant"]);
}
```

---

## Test Helpers and Utilities

### Test Builders

```csharp
public class GridContextBuilder
{
    private string _correlationId = "test-corr-id";
    private string? _causationId = null;
    private string _nodeId = "test-node";
    private Dictionary<string, string> _baggage = new();
    
    public GridContextBuilder WithCorrelationId(string id)
    {
        _correlationId = id;
        return this;
    }
    
    public GridContextBuilder WithCausationId(string? id)
    {
        _causationId = id;
        return this;
    }
    
    public GridContextBuilder WithBaggage(string key, string value)
    {
        _baggage[key] = value;
        return this;
    }
    
    public IGridContext Build()
    {
        return new GridContext(
            _correlationId,
            _causationId,
            _nodeId,
            "test-studio",
            "test",
            _baggage,
            CancellationToken.None);
    }
}

// Usage
var context = new GridContextBuilder()
    .WithCorrelationId("my-test-id")
    .WithBaggage("tenant", "test-tenant")
    .Build();
```

---

## Performance Testing

### Benchmarking Identity Types

```csharp
[Benchmark]
public void NodeId_Validation_Performance()
{
    for (int i = 0; i < 1000; i++)
    {
        NodeId.TryParse("payment-node", out _);
    }
}

[Benchmark]
public void CorrelationId_Generation_Performance()
{
    for (int i = 0; i < 1000; i++)
    {
        CorrelationId.NewId();
    }
}
```

---

## Summary

| Test Category | Focus | Key Patterns |
|---------------|-------|--------------|
| **Unit Tests** | Individual components | Mocking, builders, deterministic time |
| **Integration Tests** | Component interaction | DI containers, real dependencies |
| **Identity Tests** | Validation rules | Theory tests, edge cases |
| **Context Tests** | Causation chains | Parent-child relationships |
| **Lifecycle Tests** | Startup/shutdown | Priority ordering, state transitions |
| **Performance Tests** | Benchmarking | BenchmarkDotNet, profiling |

**Best Practices:**
- Use builders for complex test data
- Test edge cases and validation rules
- Use Theory tests for parameterized testing
- Mock external dependencies
- Use deterministic time for time-based tests
- Test causation chains and context propagation
- Verify lifecycle stage transitions

---

[← Back to File Guide](FILE_GUIDE.md)


