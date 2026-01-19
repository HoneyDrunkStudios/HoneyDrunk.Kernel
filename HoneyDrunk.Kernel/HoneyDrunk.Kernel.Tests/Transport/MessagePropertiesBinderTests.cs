using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using HoneyDrunk.Kernel.Transport;

namespace HoneyDrunk.Kernel.Tests.Transport;

public class MessagePropertiesBinderTests
{
    [Fact]
    public void TransportType_ReturnsMessage()
    {
        var binder = new MessagePropertiesBinder();

        binder.TransportType.Should().Be("message");
    }

    [Fact]
    public void CanBind_WithDictionary_ReturnsTrue()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();

        var result = binder.CanBind(properties);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanBind_WithNonDictionary_ReturnsFalse()
    {
        var binder = new MessagePropertiesBinder();
        var notDictionary = new object();

        var result = binder.CanBind(notDictionary);

        result.Should().BeFalse();
    }

    [Fact]
    public void Bind_WithValidParameters_SetsMessageProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(properties, gridContext);

        properties.Should().ContainKey(GridHeaderNames.CorrelationId);
        properties[GridHeaderNames.CorrelationId].Should().Be("corr-123");
        properties.Should().ContainKey(GridHeaderNames.NodeId);
        properties[GridHeaderNames.NodeId].Should().Be("test-node");
        properties.Should().ContainKey(GridHeaderNames.StudioId);
        properties[GridHeaderNames.StudioId].Should().Be("test-studio");
        properties.Should().ContainKey(GridHeaderNames.Environment);
        properties[GridHeaderNames.Environment].Should().Be("test-env");
    }

    [Fact]
    public void Bind_WithCausationId_SetsCausationProperty()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456");

        binder.Bind(properties, gridContext);

        properties.Should().ContainKey(GridHeaderNames.CausationId);
        properties[GridHeaderNames.CausationId].Should().Be("cause-456");
    }

    [Fact]
    public void Bind_WithoutCausationId_DoesNotSetCausationProperty()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(properties, gridContext);

        properties.Should().NotContainKey(GridHeaderNames.CausationId);
    }

    [Fact]
    public void Bind_WithBaggage_SetsBaggageProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
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

        binder.Bind(properties, gridContext);

        properties.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}tenant_id");
        properties[$"{GridHeaderNames.BaggagePrefix}tenant_id"].Should().Be("tenant-123");
        properties.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}user_id");
        properties[$"{GridHeaderNames.BaggagePrefix}user_id"].Should().Be("user-456");
    }

    [Fact]
    public void Bind_WithEmptyBaggage_DoesNotSetBaggageProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(properties, gridContext);

        var baggageProps = properties.Keys.Where(k => k.StartsWith(GridHeaderNames.BaggagePrefix));
        baggageProps.Should().BeEmpty();
    }

    [Fact]
    public void Bind_WithNullEnvelope_ThrowsArgumentNullException()
    {
        var binder = new MessagePropertiesBinder();
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
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();

        var act = () => binder.Bind(properties, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Bind_WithNonDictionary_ThrowsArgumentException()
    {
        var binder = new MessagePropertiesBinder();
        var notDictionary = new object();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var act = () => binder.Bind(notDictionary, gridContext);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("envelope")
            .WithMessage("*Expected IDictionary<string, object>*");
    }

    [Fact]
    public void Bind_MultipleTimes_OverwritesProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
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

        binder.Bind(properties, gridContext1);
        binder.Bind(properties, gridContext2);

        properties[GridHeaderNames.CorrelationId].Should().Be("corr-456");
        properties[GridHeaderNames.NodeId].Should().Be("node-2");
    }

    [Fact]
    public void Bind_WithMultipleBaggageItems_SetsAllProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>();
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

        binder.Bind(properties, gridContext);

        properties.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key1");
        properties.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key2");
        properties.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key3");
    }

    [Fact]
    public void Bind_PreservesExistingProperties()
    {
        var binder = new MessagePropertiesBinder();
        var properties = new Dictionary<string, object>
        {
            ["CustomProperty"] = "custom-value"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(properties, gridContext);

        properties.Should().ContainKey("CustomProperty");
        properties["CustomProperty"].Should().Be("custom-value");
    }
}
