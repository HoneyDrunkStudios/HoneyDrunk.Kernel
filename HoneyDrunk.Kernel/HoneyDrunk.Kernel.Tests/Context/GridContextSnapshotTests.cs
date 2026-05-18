using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Abstractions.Identity;

namespace HoneyDrunk.Kernel.Tests.Context;

public class GridContextSnapshotTests
{
    [Fact]
    public void Constructor_WithMinimalValues_CreatesInitializedInternalContext()
    {
        var context = new GridContextSnapshot(
            nodeId: "honeydrunk-transport",
            studioId: "honeydrunk",
            environment: "test");

        context.IsInitialized.Should().BeTrue();
        context.NodeId.Should().Be("honeydrunk-transport");
        context.StudioId.Should().Be("honeydrunk");
        context.Environment.Should().Be("test");
        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.TenantId.Should().Be(TenantId.Internal);
        context.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithAllValues_PreservesPropagationFields()
    {
        var tenantId = new TenantId("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        var baggage = new Dictionary<string, string>
        {
            ["source"] = "transport",
            ["purpose"] = "canary",
        };

        var context = new GridContextSnapshot(
            nodeId: "honeydrunk-data",
            studioId: "honeydrunk",
            environment: "local",
            correlationId: "corr-123",
            causationId: "cause-456",
            tenantId: tenantId,
            projectId: "project-789",
            baggage: baggage);

        context.CorrelationId.Should().Be("corr-123");
        context.CausationId.Should().Be("cause-456");
        context.TenantId.Should().Be(tenantId);
        context.ProjectId.Should().Be("project-789");
        context.Baggage.Should().BeEquivalentTo(baggage);
    }

    [Fact]
    public void AddBaggage_UpdatesSnapshotBaggage()
    {
        var context = new GridContextSnapshot(
            nodeId: "honeydrunk-transport",
            studioId: "honeydrunk",
            environment: "test");

        context.AddBaggage("key", "value");

        context.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }
}
