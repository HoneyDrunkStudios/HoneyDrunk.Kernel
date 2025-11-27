using FluentAssertions;
using HoneyDrunk.Kernel.Context;

namespace HoneyDrunk.Kernel.Tests.Context;

public class OperationContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesContext()
    {
        // Arrange
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");

        // Act
        var act = () => new OperationContext(gridContext, operationName!, Ulid.NewUlid().ToString());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Complete_MarksOperationAsSuccessful()
    {
        // Arrange
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
        var gridContext = new GridContext("corr", Ulid.NewUlid().ToString(), "node", "studio", "env");
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
}
