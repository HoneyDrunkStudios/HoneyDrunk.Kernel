# 🧪 Testing - Test Patterns and Practices

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Test Structure](#test-structure)
- [Testing Best Practices](#testing-best-practices)
  - [Mocking Context](#mocking-context)
  - [Testing Context Behavior](#testing-context-behavior)
  - [Testing Lifecycle Components](#testing-lifecycle-components)
  - [Testing with Deterministic Time (Application Pattern)](#testing-with-deterministic-time-application-pattern)
  - [Integration Testing Patterns](#integration-testing-patterns)
- [Test Helpers and Utilities](#test-helpers-and-utilities)
- [Performance Testing](#performance-testing)
- [Summary](#summary)

---

## Overview

Testing guidance and patterns for HoneyDrunk.Kernel, including unit tests, integration tests, and testing best practices for Grid-aware applications.

**Location:** `HoneyDrunk.Kernel.Tests/`

**Test Framework:** xUnit + FluentAssertions

**Design Philosophy:** Test patterns match the actual v0.3 Kernel API - no fictional interfaces or behaviors. Examples use real types from `HoneyDrunk.Kernel.Abstractions` and `HoneyDrunk.Kernel`.

---

## Test Structure

### Context Tests
- **GridContextTests.cs** - Grid context behavior and causation chains
- **NodeContextTests.cs** - Node identity and lifecycle stage tracking
- **OperationContextTests.cs** - Operation tracking, timing, and outcome

### Lifecycle Tests
- **NodeLifecycleHostTests.cs** - Startup/shutdown orchestration
- **NodeLifecycleManagerTests.cs** - Health/readiness aggregation

### Diagnostics Tests
- **NodeContextReadinessContributorTests.cs** - Readiness checks
- **ConfigurationValidatorTests.cs** - Configuration validation

---

## Testing Best Practices

### Mocking Context

#### GridContext

```csharp
// Create test GridContext with known values
var testContext = new GridContext(
    correlationId: "test-corr-123",
    nodeId: "test-node",
    studioId: "test-studio",
    environment: "test",
    causationId: null,
    baggage: new Dictionary<string, string>
    {
        ["tenant_id"] = "test-tenant",
        ["user_id"] = "test-user"
    },
    createdAtUtc: DateTimeOffset.UtcNow);
```

#### NodeContext

```csharp
// Create test NodeContext (real implementation, not mock)
var testNodeContext = new NodeContext(
    nodeId: "test-node",
    version: "1.0.0-test",
    studioId: "test-studio",
    environment: "test",
    tags: new Dictionary<string, string>
    {
        ["region"] = "test-region"
    });

// NodeContext properties are plain strings at runtime (not value objects)
Assert.Equal("test-node", testNodeContext.NodeId);
Assert.Equal("test", testNodeContext.Environment);
```

#### OperationContext

```csharp
// Create test OperationContext
var testOperation = new OperationContext(
    gridContext: testContext,
    operationName: "TestOperation");

testOperation.AddMetadata("test_key", "test_value");
```

**Note:** `OperationContext` does not implement `IDisposable` in the current implementation. Call `Complete()` or `Fail()` explicitly to mark operation outcome.

---

### Testing Context Behavior

#### GridContext Causation Chains

```csharp
[Fact]
public void GridContext_CreateChildContext_PreservesCorrelation()
{
    // Arrange
    var parentContext = new GridContext(
        correlationId: "parent-123",
        nodeId: "parent-node",
        studioId: "test-studio",
        environment: "test",
        causationId: null);
    
    // Act
    var childContext = parentContext.CreateChildContext("child-node");
    
    // Assert
    // Child keeps same correlation (trace-id)
    Assert.Equal(parentContext.CorrelationId, childContext.CorrelationId);
    
    // Child's causation points to parent's correlation (parent-child link)
    Assert.Equal(parentContext.CorrelationId, childContext.CausationId);
    
    // Child has new node
    Assert.Equal("child-node", childContext.NodeId);
}

[Fact]
public void GridContext_BaggagePropagates_ToChildren()
{
    // Arrange
    var parentContext = new GridContext(
        correlationId: "test-123",
        nodeId: "parent-node",
        studioId: "test-studio",
        environment: "test",
        causationId: null,
        baggage: new Dictionary<string, string>
        {
            ["tenant_id"] = "tenant-abc",
            ["project_id"] = "project-xyz"
        });
    
    // Act
    var childContext = parentContext.CreateChildContext("child-node");
    
    // Assert - Baggage flows to child
    Assert.Equal("tenant-abc", childContext.Baggage["tenant_id"]);
    Assert.Equal("project-xyz", childContext.Baggage["project_id"]);
}
```

**Design Note:** GridContext does not have a `WithBaggage()` method in the current implementation. To test context with different baggage, construct a new instance with the desired baggage dictionary.

#### OperationContext Tracking

```csharp
[Fact]
public void OperationContext_Complete_SetsSuccessStatus()
{
    // Arrange
    var operation = new OperationContext(testContext, "TestOp");
    
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
    var operation = new OperationContext(testContext, "TestOp");
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
    var operation = new OperationContext(testContext, "TestOp");
    
    // Act
    operation.AddMetadata("key1", "value1");
    operation.AddMetadata("key2", 42);
    
    // Assert
    Assert.Equal("value1", operation.Metadata["key1"]);
    Assert.Equal(42, operation.Metadata["key2"]);
}

[Fact]
public void OperationContext_ConvenienceProperties_PassThroughToGridContext()
{
    // Arrange
    var gridContext = new GridContext(
        correlationId: "corr-123",
        nodeId: "test-node",
        studioId: "test-studio",
        environment: "test");
    
    var operation = new OperationContext(gridContext, "TestOp");
    
    // Assert - Convenience properties match GridContext
    Assert.Equal(gridContext.CorrelationId, operation.CorrelationId);
    Assert.Equal(gridContext.CausationId, operation.CausationId);
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
    
    var hook1 = new TestStartupHook("Hook1", priority: -100, 
        () => executionOrder.Add("Hook1"));
    var hook2 = new TestStartupHook("Hook2", priority: 0, 
        () => executionOrder.Add("Hook2"));
    var hook3 = new TestStartupHook("Hook3", priority: 100, 
        () => executionOrder.Add("Hook3"));
    
    var hooks = new[] { hook3, hook1, hook2 }; // Registered out of order
    
    // Act - Execute in priority order (lower first)
    foreach (var hook in hooks.OrderBy(h => h.Priority))
    {
        await hook.ExecuteAsync(CancellationToken.None);
    }
    
    // Assert
    Assert.Equal(new[] { "Hook1", "Hook2", "Hook3" }, executionOrder);
}

// Test implementation
public class TestStartupHook : IStartupHook
{
    private readonly Action _execute;
    
    public TestStartupHook(string name, int priority, Action execute)
    {
        Name = name;
        Priority = priority;
        _execute = execute;
    }
    
    public string Name { get; }
    public int Priority { get; }
    
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _execute();
        return Task.CompletedTask;
    }
}
```

#### Health Contributors

```csharp
[Fact]
public async Task NodeLifecycleHealthContributor_ReadyStage_ReturnsHealthy()
{
    // Arrange
    var nodeContext = new NodeContext(
        nodeId: "test-node",
        version: "1.0.0",
        studioId: "test-studio",
        environment: "test");
    
    // Set to Ready stage
    nodeContext.SetLifecycleStage(NodeLifecycleStage.Ready);
    
    var contributor = new NodeLifecycleHealthContributor(nodeContext);
    
    // Act
    var (status, message) = await contributor.CheckHealthAsync();
    
    // Assert
    Assert.Equal(HealthStatus.Healthy, status);
    Assert.Null(message);
}

[Fact]
public async Task NodeLifecycleHealthContributor_StoppingStage_ReturnsUnhealthy()
{
    // Arrange
    var nodeContext = new NodeContext(
        nodeId: "test-node",
        version: "1.0.0",
        studioId: "test-studio",
        environment: "test");
    
    nodeContext.SetLifecycleStage(NodeLifecycleStage.Stopping);
    
    var contributor = new NodeLifecycleHealthContributor(nodeContext);
    
    // Act
    var (status, message) = await contributor.CheckHealthAsync();
    
    // Assert
    Assert.Equal(HealthStatus.Unhealthy, status);
    Assert.Contains("stopping", message, StringComparison.OrdinalIgnoreCase);
}
```

#### Readiness Contributors

```csharp
[Fact]
public async Task NodeContextReadinessContributor_ReadyStage_ReturnsReady()
{
    // Arrange
    var nodeContext = new NodeContext(
        nodeId: "test-node",
        version: "1.0.0",
        studioId: "test-studio",
        environment: "test");
    
    nodeContext.SetLifecycleStage(NodeLifecycleStage.Ready);
    
    var contributor = new NodeContextReadinessContributor(nodeContext);
    
    // Act
    var (isReady, reason) = await contributor.CheckReadinessAsync();
    
    // Assert
    Assert.True(isReady);
    Assert.Null(reason);
}

[Fact]
public async Task NodeContextReadinessContributor_StartingStage_NotReady()
{
    // Arrange
    var nodeContext = new NodeContext(
        nodeId: "test-node",
        version: "1.0.0",
        studioId: "test-studio",
        environment: "test");
    
    nodeContext.SetLifecycleStage(NodeLifecycleStage.Starting);
    
    var contributor = new NodeContextReadinessContributor(nodeContext);
    
    // Act
    var (isReady, reason) = await contributor.CheckReadinessAsync();
    
    // Assert
    Assert.False(isReady);
    Assert.Contains("Starting", reason);
}
```

---

### Testing with Deterministic Time (Application Pattern)

**Note:** HoneyDrunk.Kernel uses BCL time primitives (`DateTime.UtcNow`, `DateTimeOffset.UtcNow`, `Stopwatch`) directly for performance. Kernel does **not** provide clock abstractions like `IClock` or `ISystemTime`.

For applications built on top of Kernel that need deterministic time in tests, implement your own clock abstraction at the application layer:

```csharp
// Application-level abstraction (NOT part of HoneyDrunk.Kernel)
public interface IAppClock
{
    DateTimeOffset UtcNow { get; }
}

public class SystemAppClock : IAppClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public class TestAppClock : IAppClock
{
    private DateTimeOffset _currentTime;
    
    public TestAppClock(DateTimeOffset startTime)
    {
        _currentTime = startTime;
    }
    
    public DateTimeOffset UtcNow => _currentTime;
    
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}

// Application service using clock abstraction
public class TimeSensitiveService
{
    private readonly IAppClock _clock;
    
    public TimeSensitiveService(IAppClock clock)
    {
        _clock = clock;
    }
    
    public bool IsExpired(DateTimeOffset expiresAt)
    {
        return _clock.UtcNow > expiresAt;
    }
}

// Test with deterministic time
[Fact]
public void Service_DetectsExpiration_WithTestClock()
{
    // Arrange
    var clock = new TestAppClock(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    var service = new TimeSensitiveService(clock);
    var expiresAt = new DateTimeOffset(2025, 1, 1, 0, 1, 0, TimeSpan.Zero);
    
    // Act & Assert - Not expired yet
    Assert.False(service.IsExpired(expiresAt));
    
    // Advance time past expiration
    clock.Advance(TimeSpan.FromMinutes(2));
    
    // Assert - Now expired
    Assert.True(service.IsExpired(expiresAt));
}
```

**Design Rationale:** Kernel avoids clock abstractions to keep runtime code simple and fast. Applications that need deterministic time for testing should implement their own clock interface and inject it into their services, not Kernel services.

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
        
        // Register Kernel services (v0.3)
        services.AddHoneyDrunkGrid(options =>
        {
            options.NodeId = "test-node";
            options.StudioId = "test-studio";
            options.Environment = "test";
            options.Version = "1.0.0-test";
        });
        
        // Register additional Kernel services
        services.AddSingleton<IGridContextAccessor, GridContextAccessor>();
        services.AddScoped<IOperationContextFactory, OperationContextFactory>();
        
        // Register test services
        services.AddScoped<IMyService, MyService>();
        
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Fact]
    public void Service_ReceivesNodeContext()
    {
        // Arrange & Act
        var nodeContext = _serviceProvider.GetRequiredService<INodeContext>();
        
        // Assert
        Assert.NotNull(nodeContext);
        Assert.Equal("test-node", nodeContext.NodeId);
        Assert.Equal("test-studio", nodeContext.StudioId);
    }
    
    [Fact]
    public void Service_ReceivesGridContext()
    {
        // Arrange
        var gridAccessor = _serviceProvider.GetRequiredService<IGridContextAccessor>();
        
        // Manually set Grid context (normally done by middleware)
        gridAccessor.GridContext = new GridContext(
            correlationId: "test-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test");
        
        // Act
        var gridContext = gridAccessor.GridContext;
        
        // Assert
        Assert.NotNull(gridContext);
        Assert.Equal("test-123", gridContext.CorrelationId);
    }
}
```

#### Testing Context Mappers

```csharp
[Fact]
public void HttpContextMapper_MapsHeaders_ToGridContext()
{
    // Arrange
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Headers[GridHeaderNames.CorrelationId] = "test-123";
    httpContext.Request.Headers[GridHeaderNames.CausationId] = "test-456";
    httpContext.Request.Headers[$"{GridHeaderNames.BaggagePrefix}tenant"] = "tenant-789";
    
    var nodeContext = new NodeContext(
        nodeId: "test-node",
        version: "1.0.0",
        studioId: "test-studio",
        environment: "test");
    
    var mapper = new HttpContextMapper();
    
    // Act
    var gridContext = mapper.MapFromHttpContext(httpContext, nodeContext);
    
    // Assert
    Assert.Equal("test-123", gridContext.CorrelationId);
    Assert.Equal("test-456", gridContext.CausationId);
    Assert.Equal("tenant-789", gridContext.Baggage["tenant"]);
    Assert.Equal(nodeContext.NodeId, gridContext.NodeId);
}
```

---

## Test Helpers and Utilities

### Test Builders

```csharp
// GridContext builder for test data
public class GridContextBuilder
{
    private string _correlationId = "test-corr-id";
    private string? _causationId = null;
    private string _nodeId = "test-node";
    private string _studioId = "test-studio";
    private string _environment = "test";
    private Dictionary<string, string> _baggage = new();
    private DateTimeOffset? _createdAtUtc = null;
    
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
    
    public GridContextBuilder WithNodeId(string nodeId)
    {
        _nodeId = nodeId;
        return this;
    }
    
    public GridContextBuilder WithBaggage(string key, string value)
    {
        _baggage[key] = value;
        return this;
    }
    
    public GridContextBuilder WithCreatedAtUtc(DateTimeOffset timestamp)
    {
        _createdAtUtc = timestamp;
        return this;
    }
    
    public IGridContext Build()
    {
        return new GridContext(
            correlationId: _correlationId,
            nodeId: _nodeId,
            studioId: _studioId,
            environment: _environment,
            causationId: _causationId,
            baggage: _baggage,
            createdAtUtc: _createdAtUtc ?? DateTimeOffset.UtcNow);
    }
}

// Usage
var context = new GridContextBuilder()
    .WithCorrelationId("my-test-id")
    .WithBaggage("tenant", "test-tenant")
    .WithCreatedAtUtc(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
    .Build();
```

---

## Performance Testing

### Benchmarking Context Operations

**Note:** Performance tests typically live in a separate `HoneyDrunk.Kernel.Benchmarks` project using BenchmarkDotNet, not in the unit test project.

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class ContextBenchmarks
{
    private GridContext _parentContext;
    
    [GlobalSetup]
    public void Setup()
    {
        _parentContext = new GridContext(
            correlationId: "bench-123",
            nodeId: "parent-node",
            studioId: "bench-studio",
            environment: "bench");
    }
    
    [Benchmark]
    public void CreateChildContext_Performance()
    {
        _ = _parentContext.CreateChildContext("child-node");
    }
    
    [Benchmark]
    public void OperationContext_CompleteFlow()
    {
        var operation = new OperationContext(_parentContext, "BenchOp");
        operation.AddMetadata("key", "value");
        operation.Complete();
    }
}
```

---

## Summary

| Test Category | Focus | Key Patterns |
|---------------|-------|--------------|
| **Unit Tests** | Individual components | Real implementations, not mocks |
| **Integration Tests** | Component interaction | DI containers, real dependencies |
| **Context Tests** | Causation chains | Parent-child relationships |
| **Lifecycle Tests** | Startup/shutdown | Priority ordering, state transitions |
| **Benchmarks** | Performance | Separate project with BenchmarkDotNet |

**Best Practices:**
- Use real Kernel implementations in tests (e.g., `GridContext`, `NodeContext`) instead of mocks where possible
- Test causation chains and context propagation
- Verify lifecycle stage transitions
- Use builders for complex test data
- Test with actual DI container for integration scenarios
- Deterministic time is an **application-level pattern**, not Kernel
- Performance benchmarks live in separate `*.Benchmarks` project

**v0.3 Alignment:**
- All examples use actual `GridOptions` (string-based, not value objects)
- `AddHoneyDrunkGrid()` for bootstrap (current method name)
- Context accessors registered explicitly
- No fictional interfaces (IHealthContributor with tuples matches actual API)
- Node identity uses plain strings at runtime (`NodeId` property is `string`, not struct)

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)


