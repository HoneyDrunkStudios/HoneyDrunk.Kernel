using FluentAssertions;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;

namespace HoneyDrunk.Kernel.Tests.Context;

public class GridContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesUninitializedContext()
    {
        // Arrange & Act
        var context = new GridContext(
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        // Assert
        context.IsInitialized.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "studio", "env")]
    [InlineData("node", "", "env")]
    [InlineData("node", "studio", "")]
    public void Constructor_NullOrWhitespaceParameters_ThrowsArgumentException(
        string nodeId,
        string studioId,
        string environment)
    {
        // Act
        var act = () => new GridContext(nodeId, studioId, environment);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "studio", "env")]
    [InlineData("node", null, "env")]
    [InlineData("node", "studio", null)]
    public void Constructor_NullParameters_ThrowsArgumentException(
        string? nodeId,
        string? studioId,
        string? environment)
    {
        // Act
        var act = () => new GridContext(nodeId!, studioId!, environment!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsInitialized_BeforeInitialize_ReturnsFalse()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act & Assert
        context.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void IsInitialized_AfterInitialize_ReturnsTrue()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize("corr-123");

        // Assert
        context.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_ValidParameters_InitializesContext()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized(
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        // Act
        context.Initialize(correlationId: "corr-123");

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("corr-123");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
        context.CausationId.Should().BeNull();
        context.Baggage.Should().BeEmpty();
        context.Cancellation.Should().Be(default(CancellationToken));
        context.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Initialize_WithOptionalParameters_InitializesContext()
    {
        // Arrange
        var baggage = new Dictionary<string, string> { ["key1"] = "value1" };
        using var cts = new CancellationTokenSource();
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(
            correlationId: "corr-123",
            causationId: "cause-456",
            tenantId: "tenant-789",
            projectId: "proj-abc",
            baggage: baggage,
            cancellation: cts.Token);

        // Assert
        context.CorrelationId.Should().Be("corr-123");
        context.CausationId.Should().Be("cause-456");
        context.TenantId.Should().Be("tenant-789");
        context.ProjectId.Should().Be("proj-abc");
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void Initialize_WithMultipleBaggageItems_InitializesContextWithAllItems()
    {
        // Arrange
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(correlationId: "corr-123", baggage: baggage);

        // Assert
        context.Baggage.Should().HaveCount(3);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        context.Baggage.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
    }

    [Fact]
    public void Initialize_WithNullBaggage_InitializesContextWithEmptyBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(correlationId: "corr-123", baggage: null);

        // Assert
        context.Baggage.Should().NotBeNull();
        context.Baggage.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Initialize_NullOrWhitespaceCorrelationId_ThrowsArgumentException(string correlationId)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.Initialize(correlationId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Initialize_NullCorrelationId_ThrowsArgumentException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.Initialize(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        context.Initialize("corr-123");

        // Act
        var act = () => context.Initialize("corr-456");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been initialized*");
    }

    [Fact]
    public void Initialize_WithCancelledToken_StoresCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize("corr-123", cancellation: cts.Token);

        // Assert
        context.Cancellation.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void CorrelationId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.CorrelationId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void CausationId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.CausationId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void NodeId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.NodeId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void StudioId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.StudioId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void Environment_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.Environment;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void TenantId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.TenantId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void ProjectId_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.ProjectId;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void Cancellation_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.Cancellation;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void Baggage_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.Baggage;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void CreatedAtUtc_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.CreatedAtUtc;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void AddBaggage_AddsNewBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env");

        // Act
        context.AddBaggage("new-key", "new-value");

        // Assert
        context.Baggage.Should().ContainKey("new-key").WhoseValue.Should().Be("new-value");
    }

    [Fact]
    public void AddBaggage_UpdatesExistingBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            baggage: new Dictionary<string, string> { ["key"] = "old-value" });

        // Act
        context.AddBaggage("key", "new-value");

        // Assert
        context.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("new-value");
    }

    [Fact]
    public void AddBaggage_MutatesInPlace()
    {
        // Arrange
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env");

        // Act
        context.AddBaggage("key1", "value1");
        context.AddBaggage("key2", "value2");

        // Assert - Same context instance has both items
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void AddBaggage_PreservesExistingBaggageItems()
    {
        // Arrange
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            baggage: new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            });

        // Act
        context.AddBaggage("key3", "value3");

        // Assert
        context.Baggage.Should().HaveCount(3);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        context.Baggage.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("key", "")]
    public void AddBaggage_NullOrWhitespaceParameters_ThrowsArgumentException(string key, string value)
    {
        // Arrange
        var context = GridContextTestHelper.CreateDefault();

        // Act
        var act = () => context.AddBaggage(key, value);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "value")]
    [InlineData("key", null)]
    public void AddBaggage_NullParameters_ThrowsArgumentException(string? key, string? value)
    {
        // Arrange
        var context = GridContextTestHelper.CreateDefault();

        // Act
        var act = () => context.AddBaggage(key!, value!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddBaggage_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => context.AddBaggage("key", "value");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }

    [Fact]
    public void Baggage_ModifyingOriginalDictionary_DoesNotAffectContext()
    {
        // Arrange
        var baggageDict = new Dictionary<string, string> { ["key"] = "value" };
        var context = GridContextTestHelper.CreateUninitialized();
        context.Initialize(correlationId: "corr", baggage: baggageDict);

        // Act - Modify the original dictionary
        baggageDict["key"] = "modified";
        baggageDict["new-key"] = "new-value";

        // Assert - Context baggage should be unchanged
        context.Baggage.Should().HaveCount(1);
        context.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("value");
        context.Baggage.Should().NotContainKey("new-key");
    }

    [Fact]
    public void Initialize_WithTenantId_StoresTenantId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(
            correlationId: "corr-123",
            tenantId: "tenant-456");

        // Assert
        context.TenantId.Should().Be("tenant-456");
    }

    [Fact]
    public void Initialize_WithProjectId_StoresProjectId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(
            correlationId: "corr-123",
            projectId: "project-789");

        // Assert
        context.ProjectId.Should().Be("project-789");
    }

    [Fact]
    public void Initialize_WithTenantAndProject_StoresBoth()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        context.Initialize(
            correlationId: "corr-123",
            tenantId: "tenant-456",
            projectId: "project-789");

        // Assert
        context.TenantId.Should().Be("tenant-456");
        context.ProjectId.Should().Be("project-789");
    }

    [Fact]
    public void AddBaggage_DoesNotAffectTenantAndProjectIds()
    {
        // Arrange
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            tenantId: "tenant-123",
            projectId: "project-456");

        // Act
        context.AddBaggage("key", "value");

        // Assert
        context.TenantId.Should().Be("tenant-123");
        context.ProjectId.Should().Be("project-456");
    }

    [Fact]
    public void CreateInitialized_ReturnsInitializedContext()
    {
        // Act
        var context = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("corr-123");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
    }

    [Fact]
    public void CreateDefault_ReturnsInitializedContextWithDefaultValues()
    {
        // Act
        var context = GridContextTestHelper.CreateDefault();

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("test-correlation-id");
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
    }

    [Fact]
    public void CreateUninitialized_ReturnsUninitializedContext()
    {
        // Act
        var context = GridContextTestHelper.CreateUninitialized();

        // Assert
        context.IsInitialized.Should().BeFalse();
    }
}
