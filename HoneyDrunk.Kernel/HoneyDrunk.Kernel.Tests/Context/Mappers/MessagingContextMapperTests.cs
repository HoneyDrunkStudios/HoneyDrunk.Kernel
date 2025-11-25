using FluentAssertions;
using HoneyDrunk.Kernel.Context.Mappers;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

public class MessagingContextMapperTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesMapper()
    {
        // Act
        var mapper = new MessagingContextMapper("node-id", "studio-id", "production");

        // Assert
        mapper.Should().NotBeNull();
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
        var act = () => new MessagingContextMapper(nodeId, studioId, environment);

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
        var act = () => new MessagingContextMapper(nodeId!, studioId!, environment!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapFromMessageMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");

        // Act
        var act = () => mapper.MapFromMessageMetadata(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public void MapFromMessageMetadata_WithEmptyMetadata_CreatesContextWithDefaults()
    {
        // Arrange
        var mapper = new MessagingContextMapper("test-node", "test-studio", "test-env");
        var metadata = new Dictionary<string, string>();

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.Should().NotBeNull();
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.CausationId.Should().BeNull();
        context.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void MapFromMessageMetadata_WithCorrelationId_UsesProvidedValue()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "test-correlation-123"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.CorrelationId.Should().Be("test-correlation-123");
    }

    [Theory]
    [InlineData("CorrelationId")]
    [InlineData("correlation-id")]
    [InlineData("X-Correlation-Id")]
    public void MapFromMessageMetadata_WithDifferentCorrelationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-correlation"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.CorrelationId.Should().Be("test-correlation");
    }

    [Fact]
    public void MapFromMessageMetadata_WithCausationId_UsesProvidedValue()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["CausationId"] = "test-causation-456"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.CausationId.Should().Be("test-causation-456");
    }

    [Theory]
    [InlineData("CausationId")]
    [InlineData("causation-id")]
    [InlineData("X-Causation-Id")]
    public void MapFromMessageMetadata_WithDifferentCausationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-causation"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.CausationId.Should().Be("test-causation");
    }

    [Fact]
    public void MapFromMessageMetadata_WithStudioIdInMetadata_UsesMetadataValue()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "default-studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["StudioId"] = "metadata-studio"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.StudioId.Should().Be("metadata-studio");
    }

    [Theory]
    [InlineData("StudioId")]
    [InlineData("studio-id")]
    [InlineData("X-Studio-Id")]
    public void MapFromMessageMetadata_WithDifferentStudioIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "default-studio", "env");
        var metadata = new Dictionary<string, string>
        {
            [key] = "metadata-studio"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.StudioId.Should().Be("metadata-studio");
    }

    [Fact]
    public void MapFromMessageMetadata_WithBaggageItems_ExtractsBaggage()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void MapFromMessageMetadata_WithMixedCaseBaggagePrefix_ExtractsBaggage()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["Baggage-key2"] = "value2"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void MapFromMessageMetadata_WithNonBaggageItems_DoesNotIncludeInBaggage()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr-123",
            ["baggage-key1"] = "value1",
            ["OtherHeader"] = "other-value"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.Baggage.Should().HaveCount(1);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
    }

    [Fact]
    public void MapFromMessageMetadata_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>();
        using var cts = new CancellationTokenSource();

        // Act
        var context = mapper.MapFromMessageMetadata(metadata, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void MapFromMessageMetadata_WithAllFields_CreatesCompleteContext()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "default-studio", "production");
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr-123",
            ["CausationId"] = "cause-456",
            ["StudioId"] = "metadata-studio",
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };
        using var cts = new CancellationTokenSource();

        // Act
        var context = mapper.MapFromMessageMetadata(metadata, cts.Token);

        // Assert
        context.CorrelationId.Should().Be("corr-123");
        context.CausationId.Should().Be("cause-456");
        context.NodeId.Should().Be("node");
        context.StudioId.Should().Be("metadata-studio");
        context.Environment.Should().Be("production");
        context.Baggage.Should().HaveCount(2);
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void MapFromMessageMetadata_GeneratesUlidWhenNoCorrelationId()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>();

        // Act
        var context1 = mapper.MapFromMessageMetadata(metadata);
        var context2 = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context1.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context2.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context1.CorrelationId.Should().NotBe(context2.CorrelationId);
    }

    [Fact]
    public void MapFromMessageMetadata_WithEmptyBaggageValue_IncludesInBaggage()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = string.Empty
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be(string.Empty);
    }

    [Fact]
    public void MapFromMessageMetadata_PreservesMetadataOrder()
    {
        // Arrange
        var mapper = new MessagingContextMapper("node", "studio", "env");
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "first",
            ["correlation-id"] = "second",
            ["X-Correlation-Id"] = "third"
        };

        // Act
        var context = mapper.MapFromMessageMetadata(metadata);

        // Assert
        context.CorrelationId.Should().Be("first");
    }
}
