using FluentAssertions;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Telemetry;
using HoneyDrunk.Kernel.Tests.TestHelpers;

namespace HoneyDrunk.Kernel.Tests.Telemetry;

public class TelemetryContextTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesTelemetryContext()
    {
        var gridContext = CreateTestGridContext();
        var traceId = "trace-123";
        var spanId = "span-456";

        var telemetryContext = new TelemetryContext(gridContext, traceId, spanId);

        telemetryContext.GridContext.Should().Be(gridContext);
        telemetryContext.TraceId.Should().Be(traceId);
        telemetryContext.SpanId.Should().Be(spanId);
        telemetryContext.ParentSpanId.Should().BeNull();
        telemetryContext.IsSampled.Should().BeTrue();
        telemetryContext.TelemetryBaggage.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesTelemetryContext()
    {
        var gridContext = CreateTestGridContext();
        var traceId = "trace-123";
        var spanId = "span-456";
        var parentSpanId = "parent-789";
        var baggage = new Dictionary<string, string> { ["key"] = "value" };

        var telemetryContext = new TelemetryContext(
            gridContext,
            traceId,
            spanId,
            parentSpanId,
            isSampled: false,
            telemetryBaggage: baggage);

        telemetryContext.GridContext.Should().Be(gridContext);
        telemetryContext.TraceId.Should().Be(traceId);
        telemetryContext.SpanId.Should().Be(spanId);
        telemetryContext.ParentSpanId.Should().Be(parentSpanId);
        telemetryContext.IsSampled.Should().BeFalse();
        telemetryContext.TelemetryBaggage.Should().ContainKey("key");
    }

    [Fact]
    public void Constructor_NullGridContext_ThrowsArgumentNullException()
    {
        var act = () => new TelemetryContext(null!, "trace", "span");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullTraceId_ThrowsArgumentNullException()
    {
        var gridContext = CreateTestGridContext();

        var act = () => new TelemetryContext(gridContext, null!, "span");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullSpanId_ThrowsArgumentNullException()
    {
        var gridContext = CreateTestGridContext();

        var act = () => new TelemetryContext(gridContext, "trace", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullTelemetryBaggage_CreatesEmptyBaggage()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            telemetryBaggage: null);

        telemetryContext.TelemetryBaggage.Should().NotBeNull();
        telemetryContext.TelemetryBaggage.Should().BeEmpty();
    }

    [Fact]
    public void TelemetryBaggage_IsReadOnly()
    {
        var gridContext = CreateTestGridContext();
        var baggage = new Dictionary<string, string> { ["key"] = "value" };

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            telemetryBaggage: baggage);

        telemetryContext.TelemetryBaggage.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
    }

    [Fact]
    public void IsSampled_DefaultValue_IsTrue()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(gridContext, "trace", "span");

        telemetryContext.IsSampled.Should().BeTrue();
    }

    [Fact]
    public void IsSampled_CanBeSetToFalse()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            isSampled: false);

        telemetryContext.IsSampled.Should().BeFalse();
    }

    [Fact]
    public void ParentSpanId_WhenNotProvided_IsNull()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(gridContext, "trace", "span");

        telemetryContext.ParentSpanId.Should().BeNull();
    }

    [Fact]
    public void ParentSpanId_WhenProvided_IsSet()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            parentSpanId: "parent-123");

        telemetryContext.ParentSpanId.Should().Be("parent-123");
    }

    [Fact]
    public void Constructor_EmptyTraceId_Accepts()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(gridContext, string.Empty, "span");

        telemetryContext.TraceId.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_EmptySpanId_Accepts()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(gridContext, "trace", string.Empty);

        telemetryContext.SpanId.Should().Be(string.Empty);
    }

    [Fact]
    public void Constructor_EmptyParentSpanId_AcceptsEmptyString()
    {
        var gridContext = CreateTestGridContext();

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            parentSpanId: string.Empty);

        telemetryContext.ParentSpanId.Should().Be(string.Empty);
    }

    [Fact]
    public void TelemetryBaggage_WithMultipleEntries_ContainsAll()
    {
        var gridContext = CreateTestGridContext();
        var baggage = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            telemetryBaggage: baggage);

        telemetryContext.TelemetryBaggage.Should().HaveCount(3);
        telemetryContext.TelemetryBaggage["key1"].Should().Be("value1");
        telemetryContext.TelemetryBaggage["key2"].Should().Be("value2");
        telemetryContext.TelemetryBaggage["key3"].Should().Be("value3");
    }

    [Fact]
    public void TelemetryBaggage_SharesReferenceWithConstructorParameter()
    {
        var gridContext = CreateTestGridContext();
        var baggage = new Dictionary<string, string> { ["key"] = "value" };

        var telemetryContext = new TelemetryContext(
            gridContext,
            "trace",
            "span",
            telemetryBaggage: baggage);

        // Modify original dictionary
        baggage["key"] = "modified";
        baggage["new-key"] = "new-value";

        // TelemetryContext shares the same reference
        telemetryContext.TelemetryBaggage.Should().HaveCount(2);
        telemetryContext.TelemetryBaggage["key"].Should().Be("modified");
        telemetryContext.TelemetryBaggage.Should().ContainKey("new-key");
    }

    private static GridContext CreateTestGridContext()
    {
        return GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test");
    }
}
