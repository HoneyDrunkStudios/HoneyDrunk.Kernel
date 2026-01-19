using FluentAssertions;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;

namespace HoneyDrunk.Kernel.Tests.Context;

public class OperationContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesContext()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();

        // Act
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        opContext.GridContext.Should().BeSameAs(gridContext);
        opContext.OperationName.Should().Be("TestOperation");
        opContext.StartedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        opContext.CompletedAtUtc.Should().BeNull();
        opContext.IsSuccess.Should().BeNull();
        opContext.ErrorMessage.Should().BeNull();
        opContext.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMetadata_CreatesContextWithMetadata()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var metadata = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
        };

        // Act
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString(), metadata: metadata);

        // Assert
        opContext.Metadata.Should().HaveCount(2);
        opContext.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        opContext.Metadata.Should().ContainKey("key2").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void Constructor_NullGridContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OperationContext(null!, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_NullOrWhitespaceOperationName_ThrowsArgumentException(string? operationName)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();

        // Act
        var act = () => new OperationContext(gridContext, operationName!, Ulid.NewUlid().ToString());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_MarksOperationAsSuccessful()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.Complete();

        // Assert
        opContext.IsSuccess.Should().BeTrue();
        opContext.CompletedAtUtc.Should().NotBeNull();
        opContext.CompletedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        opContext.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Complete_CalledMultipleTimes_OnlyCompletesOnce()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.Complete();
        var firstCompletedAt = opContext.CompletedAtUtc;
        Thread.Sleep(10);
        opContext.Complete();

        // Assert
        opContext.CompletedAtUtc.Should().Be(firstCompletedAt);
    }

    [Fact]
    public void Fail_MarksOperationAsFailed()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.Fail("Something went wrong");

        // Assert
        opContext.IsSuccess.Should().BeFalse();
        opContext.CompletedAtUtc.Should().NotBeNull();
        opContext.ErrorMessage.Should().Be("Something went wrong");
    }

    [Fact]
    public void Fail_WithException_MarksOperationAsFailed()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());
        var exception = new InvalidOperationException("Test exception");

        // Act
        opContext.Fail("Operation failed", exception);

        // Assert
        opContext.IsSuccess.Should().BeFalse();
        opContext.ErrorMessage.Should().Be("Operation failed");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Fail_NullOrWhitespaceErrorMessage_ThrowsArgumentException(string errorMessage)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        var act = () => opContext.Fail(errorMessage);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fail_CalledMultipleTimes_OnlyFailsOnce()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.Fail("First error");
        var firstCompletedAt = opContext.CompletedAtUtc;
        var firstError = opContext.ErrorMessage;
        Thread.Sleep(10);
        opContext.Fail("Second error");

        // Assert
        opContext.CompletedAtUtc.Should().Be(firstCompletedAt);
        opContext.ErrorMessage.Should().Be(firstError);
    }

    [Fact]
    public void AddMetadata_AddsNewMetadata()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.AddMetadata("key1", "value1");
        opContext.AddMetadata("key2", 42);

        // Assert
        opContext.Metadata.Should().HaveCount(2);
        opContext.Metadata.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        opContext.Metadata.Should().ContainKey("key2").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void AddMetadata_UpdatesExistingMetadata()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());
        opContext.AddMetadata("key", "old-value");

        // Act
        opContext.AddMetadata("key", "new-value");

        // Assert
        opContext.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("new-value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMetadata_NullOrWhitespaceKey_ThrowsArgumentException(string key)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        var act = () => opContext.AddMetadata(key, "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dispose_WithoutCompletionOrFailure_AutomaticallyCompletes()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.Dispose();

        // Assert
        opContext.IsSuccess.Should().BeTrue();
        opContext.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_AfterCompletion_DoesNotChangeState()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());
        opContext.Complete();
        var completedAt = opContext.CompletedAtUtc;

        // Act
        opContext.Dispose();

        // Assert
        opContext.CompletedAtUtc.Should().Be(completedAt);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_IsSafe()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act & Assert - Should not throw
        opContext.Dispose();
        opContext.Dispose();
        opContext.Dispose();
    }

    [Fact]
    public void UsingPattern_AutomaticallyDisposesAndCompletes()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        OperationContext? opContext;

        // Act
        using (opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString()))
        {
            opContext.AddMetadata("processed", true);
        }

        // Assert
        opContext.IsSuccess.Should().BeTrue();
        opContext.CompletedAtUtc.Should().NotBeNull();
        opContext.Metadata.Should().ContainKey("processed");
    }

    [Fact]
    public void Constructor_WithNullMetadata_CreatesContextWithEmptyMetadata()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();

        // Act
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString(), metadata: null);

        // Assert
        opContext.Metadata.Should().NotBeNull();
        opContext.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void AddMetadata_WithNullValue_StoresNull()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        opContext.AddMetadata("nullable-key", null);

        // Assert
        opContext.Metadata.Should().ContainKey("nullable-key");
        opContext.Metadata["nullable-key"].Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    public void AddMetadata_NullKey_ThrowsArgumentException(string? key)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        var act = () => opContext.AddMetadata(key!, "value");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Fail_WithNullException_DoesNotThrow()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act & Assert - should not throw
        opContext.Fail("Error occurred", exception: null);

        opContext.IsSuccess.Should().BeFalse();
        opContext.ErrorMessage.Should().Be("Error occurred");
    }

    [Theory]
    [InlineData(null)]
    public void Fail_NullErrorMessage_ThrowsArgumentException(string? errorMessage)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Act
        var act = () => opContext.Fail(errorMessage!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NullOrWhitespaceOperationId_ThrowsArgumentException(string? operationId)
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();

        // Act
        var act = () => new OperationContext(gridContext, "TestOperation", operationId!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CorrelationId_ReturnsGridContextCorrelationId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "node",
            studioId: "studio",
            environment: "env");
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        opContext.CorrelationId.Should().Be("corr-123");
        opContext.CorrelationId.Should().Be(gridContext.CorrelationId);
    }

    [Fact]
    public void CausationId_ReturnsGridContextCausationId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            causationId: "cause-456");
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        opContext.CausationId.Should().Be("cause-456");
        opContext.CausationId.Should().Be(gridContext.CausationId);
    }

    [Fact]
    public void TenantId_ReturnsGridContextTenantId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            tenantId: "tenant-789");
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        opContext.TenantId.Should().Be("tenant-789");
        opContext.TenantId.Should().Be(gridContext.TenantId);
    }

    [Fact]
    public void ProjectId_ReturnsGridContextProjectId()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr",
            nodeId: "node",
            studioId: "studio",
            environment: "env",
            projectId: "project-012");
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());

        // Assert
        opContext.ProjectId.Should().Be("project-012");
        opContext.ProjectId.Should().Be(gridContext.ProjectId);
    }

    [Fact]
    public void Complete_AfterFailure_DoesNotChangeState()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());
        opContext.Fail("Initial failure");
        var failedAt = opContext.CompletedAtUtc;
        var errorMsg = opContext.ErrorMessage;

        // Act
        opContext.Complete();

        // Assert
        opContext.IsSuccess.Should().BeFalse();
        opContext.CompletedAtUtc.Should().Be(failedAt);
        opContext.ErrorMessage.Should().Be(errorMsg);
    }

    [Fact]
    public void Dispose_AfterFailure_DoesNotChangeState()
    {
        // Arrange
        var gridContext = GridContextTestHelper.CreateDefault();
        var opContext = new OperationContext(gridContext, "TestOperation", Ulid.NewUlid().ToString());
        opContext.Fail("Error occurred");
        var failedAt = opContext.CompletedAtUtc;

        // Act
        opContext.Dispose();

        // Assert
        opContext.IsSuccess.Should().BeFalse();
        opContext.CompletedAtUtc.Should().Be(failedAt);
    }
}
