// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;
using HoneyDrunk.Kernel.Context.Mappers;
using HoneyDrunk.Kernel.Tests.TestHelpers;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

public class MessagingContextMapperTests
{
    [Fact]
    public void ExtractFromMessage_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Act
        var act = () => MessagingContextMapper.ExtractFromMessage(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public void ExtractFromMessage_WithEmptyMetadata_ReturnsNullValues()
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.Should().NotBeNull();
        values.CorrelationId.Should().BeNull();
        values.CausationId.Should().BeNull();
        values.TenantId.Should().BeNull();
        values.ProjectId.Should().BeNull();
        values.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFromMessage_WithCorrelationId_ExtractsValue()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "test-correlation-123"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CorrelationId.Should().Be("test-correlation-123");
    }

    [Theory]
    [InlineData("CorrelationId")]
    [InlineData("correlation-id")]
    [InlineData("X-Correlation-Id")]
    public void ExtractFromMessage_WithDifferentCorrelationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-correlation"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CorrelationId.Should().Be("test-correlation");
    }

    [Fact]
    public void ExtractFromMessage_WithCausationId_ExtractsValue()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["CausationId"] = "test-causation-456"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CausationId.Should().Be("test-causation-456");
    }

    [Theory]
    [InlineData("CausationId")]
    [InlineData("causation-id")]
    [InlineData("X-Causation-Id")]
    public void ExtractFromMessage_WithDifferentCausationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-causation"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CausationId.Should().Be("test-causation");
    }

    [Fact]
    public void ExtractFromMessage_WithTenantId_ExtractsValue()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["TenantId"] = "test-tenant-789"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.TenantId.Should().Be("test-tenant-789");
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("tenant-id")]
    [InlineData("X-Tenant-Id")]
    public void ExtractFromMessage_WithDifferentTenantIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-tenant"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.TenantId.Should().Be("test-tenant");
    }

    [Fact]
    public void ExtractFromMessage_WithProjectId_ExtractsValue()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["ProjectId"] = "test-project-101"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.ProjectId.Should().Be("test-project-101");
    }

    [Theory]
    [InlineData("ProjectId")]
    [InlineData("project-id")]
    [InlineData("X-Project-Id")]
    public void ExtractFromMessage_WithDifferentProjectIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-project"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.ProjectId.Should().Be("test-project");
    }

    [Fact]
    public void ExtractFromMessage_WithBaggageItems_ExtractsBaggage()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.Baggage.Should().HaveCount(2);
        values.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        values.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void ExtractFromMessage_WithMixedCaseBaggagePrefix_ExtractsBaggage()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["Baggage-key2"] = "value2"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.Baggage.Should().HaveCount(2);
        values.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        values.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void ExtractFromMessage_WithNonBaggageItems_DoesNotIncludeInBaggage()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr-123",
            ["baggage-key1"] = "value1",
            ["OtherHeader"] = "other-value"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.Baggage.Should().HaveCount(1);
        values.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
    }

    [Fact]
    public void ExtractFromMessage_WithEmptyBaggageValue_IncludesInBaggage()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = string.Empty
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be(string.Empty);
    }

    [Fact]
    public void ExtractFromMessage_WithAllFields_ExtractsAllValues()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr-123",
            ["CausationId"] = "cause-456",
            ["TenantId"] = "tenant-789",
            ["ProjectId"] = "project-101",
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CorrelationId.Should().Be("corr-123");
        values.CausationId.Should().Be("cause-456");
        values.TenantId.Should().Be("tenant-789");
        values.ProjectId.Should().Be("project-101");
        values.Baggage.Should().HaveCount(2);
    }

    [Fact]
    public void ExtractFromMessage_PreservesMetadataOrder()
    {
        // Arrange - first key found should win
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "first",
            ["correlation-id"] = "second",
            ["X-Correlation-Id"] = "third"
        };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(metadata);

        // Assert
        values.CorrelationId.Should().Be("first");
    }

    [Fact]
    public void InitializeFromMessage_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        // Act
        var act = () => MessagingContextMapper.InitializeFromMessage(null!, metadata, CancellationToken.None);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void InitializeFromMessage_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();

        // Act
        var act = () => MessagingContextMapper.InitializeFromMessage(context, null!, CancellationToken.None);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public void InitializeFromMessage_WithEmptyMetadata_InitializesContextWithGeneratedCorrelationId()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized("test-node", "test-studio", "test-env");
        var metadata = new Dictionary<string, string>();

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.NodeId.Should().Be("test-node");
        context.StudioId.Should().Be("test-studio");
        context.Environment.Should().Be("test-env");
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.CausationId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.ProjectId.Should().BeNull();
        context.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void InitializeFromMessage_WithCorrelationId_UsesProvidedValue()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "test-correlation-123"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.CorrelationId.Should().Be("test-correlation-123");
    }

    [Fact]
    public void InitializeFromMessage_GeneratesUlidWhenNoCorrelationId()
    {
        // Arrange
        var context1 = GridContextTestHelper.CreateUninitialized();
        var context2 = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>();

        // Act
        MessagingContextMapper.InitializeFromMessage(context1, metadata, CancellationToken.None);
        MessagingContextMapper.InitializeFromMessage(context2, metadata, CancellationToken.None);

        // Assert
        context1.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context2.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context1.CorrelationId.Should().NotBe(context2.CorrelationId);
    }

    [Fact]
    public void InitializeFromMessage_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>();
        using var cts = new CancellationTokenSource();

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, cts.Token);

        // Assert
        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void InitializeFromMessage_WithAllFields_InitializesCompleteContext()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized("node", "studio", "production");
        var metadata = new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr-123",
            ["CausationId"] = "cause-456",
            ["TenantId"] = "tenant-789",
            ["ProjectId"] = "project-101",
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };
        using var cts = new CancellationTokenSource();

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, cts.Token);

        // Assert
        context.IsInitialized.Should().BeTrue();
        context.CorrelationId.Should().Be("corr-123");
        context.CausationId.Should().Be("cause-456");
        context.TenantId.Should().Be("tenant-789");
        context.ProjectId.Should().Be("project-101");
        context.NodeId.Should().Be("node");
        context.StudioId.Should().Be("studio");
        context.Environment.Should().Be("production");
        context.Baggage.Should().HaveCount(2);
        context.Cancellation.Should().Be(cts.Token);
    }

    [Theory]
    [InlineData("CorrelationId")]
    [InlineData("correlation-id")]
    [InlineData("X-Correlation-Id")]
    public void InitializeFromMessage_WithDifferentCorrelationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-correlation"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.CorrelationId.Should().Be("test-correlation");
    }

    [Theory]
    [InlineData("CausationId")]
    [InlineData("causation-id")]
    [InlineData("X-Causation-Id")]
    public void InitializeFromMessage_WithDifferentCausationIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-causation"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.CausationId.Should().Be("test-causation");
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("tenant-id")]
    [InlineData("X-Tenant-Id")]
    public void InitializeFromMessage_WithDifferentTenantIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-tenant"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.TenantId.Should().Be("test-tenant");
    }

    [Theory]
    [InlineData("ProjectId")]
    [InlineData("project-id")]
    [InlineData("X-Project-Id")]
    public void InitializeFromMessage_WithDifferentProjectIdKeys_ExtractsCorrectly(string key)
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            [key] = "test-project"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.ProjectId.Should().Be("test-project");
    }

    [Fact]
    public void InitializeFromMessage_WithBaggageItems_ExtractsBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["baggage-key2"] = "value2"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void InitializeFromMessage_WithMixedCaseBaggagePrefix_ExtractsBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = "value1",
            ["Baggage-key2"] = "value2"
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.Baggage.Should().HaveCount(2);
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        context.Baggage.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Fact]
    public void InitializeFromMessage_WithEmptyBaggageValue_IncludesInBaggage()
    {
        // Arrange
        var context = GridContextTestHelper.CreateUninitialized();
        var metadata = new Dictionary<string, string>
        {
            ["baggage-key1"] = string.Empty
        };

        // Act
        MessagingContextMapper.InitializeFromMessage(context, metadata, CancellationToken.None);

        // Assert
        context.Baggage.Should().ContainKey("key1").WhoseValue.Should().Be(string.Empty);
    }

    [Fact]
    public void MessageContextValues_Record_HasCorrectProperties()
    {
        // Arrange
        _ = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var values = MessagingContextMapper.ExtractFromMessage(new Dictionary<string, string>
        {
            ["CorrelationId"] = "corr",
            ["CausationId"] = "cause",
            ["TenantId"] = "tenant",
            ["ProjectId"] = "project",
            ["baggage-key"] = "value"
        });

        // Assert
        values.CorrelationId.Should().Be("corr");
        values.CausationId.Should().Be("cause");
        values.TenantId.Should().Be("tenant");
        values.ProjectId.Should().Be("project");
        values.Baggage.Should().ContainKey("key");
    }
}
