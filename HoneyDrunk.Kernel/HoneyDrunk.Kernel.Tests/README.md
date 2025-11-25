# HoneyDrunk.Kernel.Tests

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![xUnit](https://img.shields.io/badge/xUnit-3.0-blue)](https://xunit.net/)

> **Comprehensive test suite for HoneyDrunk.Kernel** - Ensuring reliability, correctness, and behavior validation across all Kernel components.

## ?? Overview

This project contains unit and integration tests for **HoneyDrunk.Kernel.Abstractions** and **HoneyDrunk.Kernel** runtime implementations. Tests follow xUnit conventions with FluentAssertions for readable assertions.

## ?? Testing Philosophy

### Test Structure
- **One behavior per test** - Each test validates a single scenario
- **AAA Pattern** - Arrange, Act, Assert structure
- **Clear naming** - `WhenCondition_ThenExpectedBehavior` convention
- **No test interdependencies** - Tests can run in any order or parallel

### Coverage Goals
- ? **Public API surface** - All public interfaces and classes
- ? **Edge cases** - Null handling, validation, boundaries
- ? **Error paths** - Exception scenarios and error codes
- ? **Integration points** - Context propagation, serialization
- ? **Behavior validation** - Not just code coverage, but meaningful tests

## ??? Test Structure

```
HoneyDrunk.Kernel.Tests/
??? AgentsInterop/                      # Agent serialization tests
?   ??? AgentContextProjectionTests.cs
?   ??? AgentExecutionResultTests.cs
?   ??? AgentResultSerializerTests.cs
?   ??? GridContextSerializerTests.cs
??? Configuration/                      # Configuration tests
?   ??? ConfigKeyTests.cs
?   ??? ConfigPathTests.cs
?   ??? ConfigScopeTypeTests.cs
?   ??? NodeRuntimeOptionsTests.cs
?   ??? StudioConfigurationTests.cs
??? Context/                            # Context tests
?   ??? GridContextTests.cs
?   ??? NodeContextTests.cs
?   ??? OperationContextTests.cs
?   ??? Mappers/
?       ??? HttpContextMapperTests.cs
??? Diagnostics/                        # Diagnostics tests
?   ??? ConfigurationValidatorTests.cs
?   ??? NodeContextReadinessContributorTests.cs
?   ??? NodeLifecycleHealthContributorTests.cs
?   ??? NoOpMetricsCollectorTests.cs
??? Health/                             # Health check tests
?   ??? CompositeHealthCheckTests.cs
?   ??? HealthStatusTests.cs
??? Identity/                           # Identity tests
?   ??? CorrelationIdTests.cs
?   ??? NodeIdTests.cs
?   ??? ProjectIdTests.cs
?   ??? RunIdTests.cs
?   ??? TenantIdTests.cs
??? Lifecycle/                          # Lifecycle tests
?   ??? NodeLifecycleManagerTests.cs
??? Config/                             # Secrets tests
?   ??? CompositeSecretsSourceTests.cs
??? Telemetry/                          # Telemetry tests
    ??? GridContextTraceEnricherTests.cs
    ??? TelemetryContextTests.cs
    ??? TelemetryLogScopeFactoryTests.cs
```

## ?? Test Categories

### Identity Tests
Validate strongly-typed identifier behavior, validation, and edge cases.

**Example:**
```csharp
[Fact]
public void NodeId_ValidKebabCase_Succeeds()
{
    // Arrange & Act
    var nodeId = new NodeId("payment-service");
    
    // Assert
    Assert.Equal("payment-service", nodeId.Value);
}

[Fact]
public void NodeId_InvalidCharacters_ThrowsArgumentException()
{
    // Act & Assert
    Assert.Throws<ArgumentException>(() => new NodeId("Payment_Service"));
}
```

### Context Tests
Validate context creation, propagation, and causality tracking.

**Example:**
```csharp
[Fact]
public void GridContext_CreateChildContext_PreservesCorrelationAndSetsCausation()
{
    // Arrange
    var parent = new GridContext("corr-123", "node-1", "studio", "prod");
    
    // Act
    var child = parent.CreateChildContext("node-2");
    
    // Assert
    Assert.Equal(parent.CorrelationId, child.CorrelationId); // Same correlation
    Assert.Equal(parent.CorrelationId, child.CausationId);  // Parent becomes cause
}
```

### AgentsInterop Tests
Validate serialization, security filtering, and context projection.

**Example:**
```csharp
[Fact]
public void GridContextSerializer_FiltersSecretBaggage()
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
            ["ApiToken"] = "secret-abc",         // Filtered
            ["SecretKey"] = "key-xyz"            // Filtered
        }
    );
    
    // Act
    var json = GridContextSerializer.Serialize(context, includeFullBaggage: false);
    var restored = GridContextSerializer.Deserialize(json);
    
    // Assert
    Assert.NotNull(restored);
    Assert.True(restored.Baggage.ContainsKey("TenantId"));
    Assert.False(restored.Baggage.ContainsKey("ApiToken"));   // Filtered
    Assert.False(restored.Baggage.ContainsKey("SecretKey")); // Filtered
}
```

### Lifecycle Tests
Validate startup/shutdown hook execution order and lifecycle stages.

**Example:**
```csharp
[Fact]
public async Task NodeLifecycleManager_StartAsync_ExecutesHooksInOrder()
{
    // Arrange
    var executionOrder = new List<int>();
    var hook1 = new TestStartupHook(order: 1, executionOrder);
    var hook2 = new TestStartupHook(order: 2, executionOrder);
    var manager = new NodeLifecycleManager(new[] { hook2, hook1 }, ...);
    
    // Act
    await manager.StartAsync(CancellationToken.None);
    
    // Assert
    Assert.Equal(new[] { 1, 2 }, executionOrder); // Ordered ascending
}
```

### Health Tests
Validate health check aggregation and status mapping.

**Example:**
```csharp
[Fact]
public void CompositeHealthCheck_AnyUnhealthy_ReturnsUnhealthy()
{
    // Arrange
    var check1 = new StaticHealthCheck(HealthStatus.Healthy);
    var check2 = new StaticHealthCheck(HealthStatus.Unhealthy);
    var composite = new CompositeHealthCheck(new[] { check1, check2 });
    
    // Act
    var result = await composite.CheckAsync();
    
    // Assert
    Assert.Equal(HealthStatus.Unhealthy, result);
}
```

## ?? Running Tests

### All Tests

```bash
dotnet test
```

### Specific Category

```bash
# Run only Identity tests
dotnet test --filter "FullyQualifiedName~Identity"

# Run only Context tests
dotnet test --filter "FullyQualifiedName~Context"

# Run only AgentsInterop tests
dotnet test --filter "FullyQualifiedName~AgentsInterop"
```

### With Coverage

```bash
# Install coverage tool (one-time)
dotnet tool install -g dotnet-coverage

# Run with coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

### Debug Single Test

```bash
dotnet test --filter "FullyQualifiedName=HoneyDrunk.Kernel.Tests.Context.GridContextTests.CreateChildContext_PreservesCorrelation"
```

## ?? Test Statistics (v0.3.0)

| Category | Test Count | Coverage |
|----------|------------|----------|
| **Identity** | 25+ | 100% |
| **Context** | 30+ | 100% |
| **AgentsInterop** | 15+ | 100% |
| **Configuration** | 20+ | 100% |
| **Lifecycle** | 10+ | 100% |
| **Health** | 8+ | 100% |
| **Telemetry** | 12+ | 100% |
| **Diagnostics** | 10+ | 100% |
| **Total** | **130+** | **~95%** |

## ?? Testing Patterns

### Test Naming Convention

```csharp
// Pattern: WhenCondition_ThenExpectedBehavior
[Fact]
public void WhenNodeIdIsValidKebabCase_ThenConstructorSucceeds() { }

[Fact]
public void WhenBaggageContainsSecrets_ThenSerializerFiltersSecrets() { }

[Fact]
public void WhenLifecycleStageIsRunning_ThenHealthCheckReturnsHealthy() { }
```

### Arrange-Act-Assert Pattern

```csharp
[Fact]
public void TestExample()
{
    // Arrange - Set up test data and dependencies
    var nodeId = "test-node";
    var context = new GridContext(correlationId, nodeId, studioId, environment);
    
    // Act - Execute the behavior under test
    var child = context.CreateChildContext("child-node");
    
    // Assert - Verify the expected outcome
    Assert.Equal(context.CorrelationId, child.CorrelationId);
    Assert.Equal(context.CorrelationId, child.CausationId);
}
```

### FluentAssertions Usage

```csharp
using FluentAssertions;

[Fact]
public void Context_Should_HaveExpectedProperties()
{
    // Arrange & Act
    var context = new GridContext("corr-123", "node-1", "studio", "prod");
    
    // Assert
    context.CorrelationId.Should().Be("corr-123");
    context.NodeId.Should().Be("node-1");
    context.Baggage.Should().BeEmpty();
}
```

### Exception Testing

```csharp
[Fact]
public void NodeId_InvalidFormat_ThrowsArgumentException()
{
    // Arrange
    var invalidNodeId = "Invalid_Format";
    
    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() => new NodeId(invalidNodeId));
    exception.Message.Should().Contain("kebab-case");
}
```

### Parameterized Tests

```csharp
[Theory]
[InlineData("valid-node-id")]
[InlineData("another-valid-node")]
[InlineData("node-123")]
public void NodeId_ValidInputs_Succeeds(string validInput)
{
    // Act
    var nodeId = new NodeId(validInput);
    
    // Assert
    Assert.Equal(validInput, nodeId.Value);
}

[Theory]
[InlineData("Invalid_Node")]     // Underscore
[InlineData("UPPERCASE")]         // Uppercase
[InlineData("node id")]           // Space
[InlineData("node@id")]           // Special char
public void NodeId_InvalidInputs_Throws(string invalidInput)
{
    // Act & Assert
    Assert.Throws<ArgumentException>(() => new NodeId(invalidInput));
}
```

## ?? Test Helpers

### Static Test Data

```csharp
public static class TestData
{
    public const string ValidNodeId = "test-node";
    public const string ValidStudioId = "test-studio";
    public const string ValidEnvironment = "test";
    
    public static GridContext CreateTestGridContext(
        string? correlationId = null,
        string? nodeId = null)
    {
        return new GridContext(
            correlationId: correlationId ?? Ulid.NewUlid().ToString(),
            nodeId: nodeId ?? ValidNodeId,
            studioId: ValidStudioId,
            environment: ValidEnvironment
        );
    }
}
```

### Mock Implementations

```csharp
public class TestStartupHook : IStartupHook
{
    private readonly List<int> _executionOrder;
    
    public TestStartupHook(int order, List<int> executionOrder)
    {
        Order = order;
        _executionOrder = executionOrder;
    }
    
    public int Order { get; }
    
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _executionOrder.Add(Order);
        return Task.CompletedTask;
    }
}
```

## ?? Related Documentation

- **[Testing.md](../docs/Testing.md)** - Test patterns and best practices
- **[FILE_GUIDE.md](../docs/FILE_GUIDE.md)** - Architecture documentation
- **[Context.md](../docs/Context.md)** - Context behavior specifications
- **[Identity.md](../docs/Identity.md)** - Identity validation rules

## ?? Best Practices

### ? DO

```csharp
// Keep tests focused on one behavior
[Fact]
public void CreateChildContext_PreservesCorrelation() { }

// Use descriptive test names
[Fact]
public void WhenNodeIdIsInvalid_ThenConstructorThrowsArgumentException() { }

// Test edge cases
[Fact]
public void NodeId_EmptyString_ThrowsArgumentException() { }

// Test public API surface
[Fact]
public void IGridContext_CreateChildContext_ReturnsNewContext() { }
```

### ? DON'T

```csharp
// Don't test multiple behaviors in one test
[Fact]
public void TestEverything() { } // ? Too broad

// Don't use magic numbers without explanation
[Fact]
public void Test() 
{
    var x = 42; // ? Why 42?
}

// Don't test private implementation details
[Fact]
public void PrivateMethod_DoesX() { } // ? Test public behavior

// Don't have test interdependencies
[Fact]
public void Test1() { }

[Fact]
public void Test2DependsOnTest1() { } // ? Fragile
```

## ?? Continuous Integration

Tests run automatically on:
- ? Every pull request
- ? Every commit to `main`
- ? Before package release

**Build Pipeline:**
```yaml
- name: Test
  run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"
  
- name: Code Coverage
  run: dotnet-coverage collect -f cobertura -o coverage.xml dotnet test --no-build
```

## ?? License

This project is licensed under the [MIT License](../LICENSE).

---

**Built with ?? by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) • [Documentation](../docs/FILE_GUIDE.md) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel/issues)
