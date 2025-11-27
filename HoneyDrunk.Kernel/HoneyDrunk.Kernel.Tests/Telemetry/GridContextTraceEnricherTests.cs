using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Telemetry;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Telemetry;

namespace HoneyDrunk.Kernel.Tests.Telemetry;

public class GridContextTraceEnricherTests
{
    [Fact]
    public void Enrich_NullContext_ThrowsArgumentNullException()
    {
        var enricher = new GridContextTraceEnricher();
        var tags = new Dictionary<string, object?>();

        var act = () => enricher.Enrich(null!, tags);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_NullTags_ThrowsArgumentNullException()
    {
        var enricher = new GridContextTraceEnricher();
        var telemetryContext = CreateTestTelemetryContext();

        var act = () => enricher.Enrich(telemetryContext, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enrich_AddsStandardGridTags()
    {
        var enricher = new GridContextTraceEnricher();
        var telemetryContext = CreateTestTelemetryContext();
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags.Should().ContainKey(TelemetryTags.CorrelationId);
        tags.Should().ContainKey(TelemetryTags.NodeId);
        tags.Should().ContainKey(TelemetryTags.StudioId);
        tags.Should().ContainKey(TelemetryTags.Environment);
    }

    [Fact]
    public void Enrich_AddsCorrectValues()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext = new GridContext("corr-123", Ulid.NewUlid().ToString(), "test-node", "test-studio", "production");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags[TelemetryTags.CorrelationId].Should().Be("corr-123");
        tags[TelemetryTags.NodeId].Should().Be("test-node");
        tags[TelemetryTags.StudioId].Should().Be("test-studio");
        tags[TelemetryTags.Environment].Should().Be("production");
    }

    [Fact]
    public void Enrich_WithCausationId_AddsCausationTag()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext = new GridContext(
            "corr-123",
            "test-node",
            "test-studio",
            "production",
            causationId: "cause-456");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags.Should().ContainKey(TelemetryTags.CausationId);
        tags[TelemetryTags.CausationId].Should().Be("cause-456");
    }

    [Fact]
    public void Enrich_WithoutCausationId_DoesNotAddCausationTag()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext = new GridContext("corr-123", Ulid.NewUlid().ToString(), "test-node", "test-studio", "production");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags.Should().NotContainKey(TelemetryTags.CausationId);
    }

    [Fact]
    public void Enrich_WithBaggage_AddsBaggageWithPrefix()
    {
        var enricher = new GridContextTraceEnricher();
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var gridContext = new GridContext(
            "corr-123",
            Ulid.NewUlid().ToString(),
            "test-node",
            "test-studio",
            "production",
            baggage: baggage);
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags.Should().ContainKey("baggage.key1");
        tags.Should().ContainKey("baggage.key2");
        tags["baggage.key1"].Should().Be("value1");
        tags["baggage.key2"].Should().Be("value2");
    }

    [Fact]
    public void Enrich_WithoutBaggage_DoesNotAddBaggageTags()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext = new GridContext("corr-123", Ulid.NewUlid().ToString(), "test-node", "test-studio", "production");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext, tags);

        tags.Keys.Should().NotContain(key => key.StartsWith("baggage."));
    }

    [Fact]
    public void Enrich_ExistingTags_AreOverwritten()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext = new GridContext("corr-123", Ulid.NewUlid().ToString(), "test-node", "test-studio", "production");
        var telemetryContext = new TelemetryContext(gridContext, "trace-id", "span-id");
        var tags = new Dictionary<string, object?>
        {
            [TelemetryTags.NodeId] = "old-node"
        };

        enricher.Enrich(telemetryContext, tags);

        tags[TelemetryTags.NodeId].Should().Be("test-node");
    }

    [Fact]
    public void Enrich_PreservesOtherTags()
    {
        var enricher = new GridContextTraceEnricher();
        var telemetryContext = CreateTestTelemetryContext();
        var tags = new Dictionary<string, object?>
        {
            ["custom-tag"] = "custom-value"
        };

        enricher.Enrich(telemetryContext, tags);

        tags.Should().ContainKey("custom-tag");
        tags["custom-tag"].Should().Be("custom-value");
    }

    [Fact]
    public void Enrich_MultipleCalls_UpdatesTags()
    {
        var enricher = new GridContextTraceEnricher();
        var gridContext1 = new GridContext("corr-1", Ulid.NewUlid().ToString(), "node-1", "studio-1", "env-1");
        var telemetryContext1 = new TelemetryContext(gridContext1, "trace-1", "span-1");
        var gridContext2 = new GridContext("corr-2", Ulid.NewUlid().ToString(), "node-2", "studio-2", "env-2");
        var telemetryContext2 = new TelemetryContext(gridContext2, "trace-2", "span-2");
        var tags = new Dictionary<string, object?>();

        enricher.Enrich(telemetryContext1, tags);
        tags[TelemetryTags.CorrelationId].Should().Be("corr-1");

        enricher.Enrich(telemetryContext2, tags);
        tags[TelemetryTags.CorrelationId].Should().Be("corr-2");
    }

    private static TelemetryContext CreateTestTelemetryContext()
    {
        var gridContext = new GridContext("corr-123", "op-456", "test-node", "test-studio", "test");
        return new TelemetryContext(gridContext, "trace-id", "span-id");
    }
}
