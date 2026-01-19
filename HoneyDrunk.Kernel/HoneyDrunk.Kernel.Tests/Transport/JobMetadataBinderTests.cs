using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using HoneyDrunk.Kernel.Transport;

namespace HoneyDrunk.Kernel.Tests.Transport;

public class JobMetadataBinderTests
{
    [Fact]
    public void TransportType_ReturnsJob()
    {
        var binder = new JobMetadataBinder();

        binder.TransportType.Should().Be("job");
    }

    [Fact]
    public void CanBind_WithStringDictionary_ReturnsTrue()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();

        var result = binder.CanBind(metadata);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanBind_WithNonDictionary_ReturnsFalse()
    {
        var binder = new JobMetadataBinder();
        var notDictionary = new object();

        var result = binder.CanBind(notDictionary);

        result.Should().BeFalse();
    }

    [Fact]
    public void Bind_WithValidParameters_SetsJobMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey(GridHeaderNames.CorrelationId);
        metadata[GridHeaderNames.CorrelationId].Should().Be("corr-123");
        metadata.Should().ContainKey(GridHeaderNames.NodeId);
        metadata[GridHeaderNames.NodeId].Should().Be("test-node");
        metadata.Should().ContainKey(GridHeaderNames.StudioId);
        metadata[GridHeaderNames.StudioId].Should().Be("test-studio");
        metadata.Should().ContainKey(GridHeaderNames.Environment);
        metadata[GridHeaderNames.Environment].Should().Be("test-env");
        metadata.Should().ContainKey("CreatedAtUtc");
    }

    [Fact]
    public void Bind_WithCausationId_SetsCausationMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456");

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey(GridHeaderNames.CausationId);
        metadata[GridHeaderNames.CausationId].Should().Be("cause-456");
    }

    [Fact]
    public void Bind_WithoutCausationId_DoesNotSetCausationMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(metadata, gridContext);

        metadata.Should().NotContainKey(GridHeaderNames.CausationId);
    }

    [Fact]
    public void Bind_SetsCreatedAtUtcInIso8601Format()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey("CreatedAtUtc");
        var parsedDate = DateTimeOffset.Parse(metadata["CreatedAtUtc"], provider: System.Globalization.CultureInfo.InvariantCulture);
        parsedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Bind_WithBaggage_SetsBaggageMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var baggage = new Dictionary<string, string>
        {
            ["tenant_id"] = "tenant-123",
            ["user_id"] = "user-456"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: baggage);

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}tenant_id");
        metadata[$"{GridHeaderNames.BaggagePrefix}tenant_id"].Should().Be("tenant-123");
        metadata.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}user_id");
        metadata[$"{GridHeaderNames.BaggagePrefix}user_id"].Should().Be("user-456");
    }

    [Fact]
    public void Bind_WithEmptyBaggage_DoesNotSetBaggageMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(metadata, gridContext);

        var baggageMetadata = metadata.Keys.Where(k => k.StartsWith(GridHeaderNames.BaggagePrefix));
        baggageMetadata.Should().BeEmpty();
    }

    [Fact]
    public void Bind_WithNullEnvelope_ThrowsArgumentNullException()
    {
        var binder = new JobMetadataBinder();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var act = () => binder.Bind(null!, gridContext);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("envelope");
    }

    [Fact]
    public void Bind_WithNullGridContext_ThrowsArgumentNullException()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();

        var act = () => binder.Bind(metadata, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Bind_WithNonDictionary_ThrowsArgumentException()
    {
        var binder = new JobMetadataBinder();
        var notDictionary = new object();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var act = () => binder.Bind(notDictionary, gridContext);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("envelope")
            .WithMessage("*Expected IDictionary<string, string>*");
    }

    [Fact]
    public void Bind_MultipleTimes_OverwritesMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var gridContext1 = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "node-1",
            studioId: "studio",
            environment: "env");
        var gridContext2 = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-456",
            nodeId: "node-2",
            studioId: "studio",
            environment: "env");

        binder.Bind(metadata, gridContext1);
        binder.Bind(metadata, gridContext2);

        metadata[GridHeaderNames.CorrelationId].Should().Be("corr-456");
        metadata[GridHeaderNames.NodeId].Should().Be("node-2");
    }

    [Fact]
    public void Bind_WithMultipleBaggageItems_SetsAllMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>();
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: baggage);

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key1");
        metadata.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key2");
        metadata.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key3");
    }

    [Fact]
    public void Bind_PreservesExistingMetadata()
    {
        var binder = new JobMetadataBinder();
        var metadata = new Dictionary<string, string>
        {
            ["CustomMetadata"] = "custom-value"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(metadata, gridContext);

        metadata.Should().ContainKey("CustomMetadata");
        metadata["CustomMetadata"].Should().Be("custom-value");
    }

    [Fact]
    public void Bind_WithObjectDictionary_ReturnsFalseInCanBind()
    {
        var binder = new JobMetadataBinder();
        var objectDict = new Dictionary<string, object>();

        var result = binder.CanBind(objectDict);

        result.Should().BeFalse();
    }
}
