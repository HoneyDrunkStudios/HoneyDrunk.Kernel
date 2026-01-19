using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using HoneyDrunk.Kernel.Transport;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Tests.Transport;

public class HttpResponseBinderTests
{
    [Fact]
    public void TransportType_ReturnsHttp()
    {
        var binder = new HttpResponseBinder();

        binder.TransportType.Should().Be("http");
    }

    [Fact]
    public void CanBind_WithHttpResponse_ReturnsTrue()
    {
        var binder = new HttpResponseBinder();
        var response = new DefaultHttpContext().Response;

        var result = binder.CanBind(response);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanBind_WithNonHttpResponse_ReturnsFalse()
    {
        var binder = new HttpResponseBinder();
        var notResponse = new object();

        var result = binder.CanBind(notResponse);

        result.Should().BeFalse();
    }

    [Fact]
    public void Bind_WithValidParameters_SetsResponseHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey(GridHeaderNames.CorrelationId);
        response.Headers[GridHeaderNames.CorrelationId].ToString().Should().Be("corr-123");
        response.Headers.Should().ContainKey(GridHeaderNames.NodeId);
        response.Headers[GridHeaderNames.NodeId].ToString().Should().Be("test-node");
    }

    [Fact]
    public void Bind_WithCausationId_SetsCausationHeader()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            causationId: "cause-456");

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey(GridHeaderNames.CausationId);
        response.Headers[GridHeaderNames.CausationId].ToString().Should().Be("cause-456");
    }

    [Fact]
    public void Bind_WithoutCausationId_DoesNotSetCausationHeader()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(response, gridContext);

        response.Headers.Should().NotContainKey(GridHeaderNames.CausationId);
    }

    [Fact]
    public void Bind_WithBaggage_SetsBaggageHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
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

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}tenant_id");
        response.Headers[$"{GridHeaderNames.BaggagePrefix}tenant_id"].ToString().Should().Be("tenant-123");
        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}user_id");
        response.Headers[$"{GridHeaderNames.BaggagePrefix}user_id"].ToString().Should().Be("user-456");
    }

    [Fact]
    public void Bind_WithEmptyBaggage_DoesNotSetBaggageHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(response, gridContext);

        var baggageHeaders = response.Headers.Where(h => h.Key.StartsWith(GridHeaderNames.BaggagePrefix));
        baggageHeaders.Should().BeEmpty();
    }

    [Fact]
    public void Bind_WithNullEnvelope_ThrowsArgumentNullException()
    {
        var binder = new HttpResponseBinder();
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
        var binder = new HttpResponseBinder();
        var response = new DefaultHttpContext().Response;

        var act = () => binder.Bind(response, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Bind_WithNonHttpResponse_ThrowsArgumentException()
    {
        var binder = new HttpResponseBinder();
        var notResponse = new object();
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var act = () => binder.Bind(notResponse, gridContext);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("envelope")
            .WithMessage("*Expected HttpResponse*");
    }

    [Fact]
    public void Bind_MultipleTimes_OverwritesHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
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

        binder.Bind(response, gridContext1);
        binder.Bind(response, gridContext2);

        response.Headers[GridHeaderNames.CorrelationId].ToString().Should().Be("corr-456");
        response.Headers[GridHeaderNames.NodeId].ToString().Should().Be("node-2");
    }

    [Fact]
    public void Bind_WithMultipleBaggageItems_SetsAllHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
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

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key1");
        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key2");
        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key3");
    }

    [Fact]
    public void Bind_PreservesExistingHeaders()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        response.Headers["X-Custom-Header"] = "custom-value";
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey("X-Custom-Header");
        response.Headers["X-Custom-Header"].ToString().Should().Be("custom-value");
    }

    [Fact]
    public void Bind_WithSpecialCharactersInBaggage_EncodesCorrectly()
    {
        var binder = new HttpResponseBinder();
        var httpContext = new DefaultHttpContext();
        var response = httpContext.Response;
        var baggage = new Dictionary<string, string>
        {
            ["key-with-spaces"] = "value with spaces",
            ["key-with-special"] = "value=with=equals"
        };
        var gridContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env",
            baggage: baggage);

        binder.Bind(response, gridContext);

        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key-with-spaces");
        response.Headers.Should().ContainKey($"{GridHeaderNames.BaggagePrefix}key-with-special");
    }

    [Fact]
    public void CanBind_WithNull_ReturnsFalse()
    {
        var binder = new HttpResponseBinder();

        var result = binder.CanBind(null!);

        result.Should().BeFalse();
    }
}
