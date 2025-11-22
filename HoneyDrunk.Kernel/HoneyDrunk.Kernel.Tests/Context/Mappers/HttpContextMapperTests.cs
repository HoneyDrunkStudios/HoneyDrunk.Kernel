using FluentAssertions;
using HoneyDrunk.Kernel.Context.Mappers;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

public class HttpContextMapperTests
{
    private const string TestNodeId = "test-node";
    private const string TestStudioId = "test-studio";
    private const string TestEnvironment = "test-env";

    [Fact]
    public void Constructor_ValidParameters_CreatesMapper()
    {
        var act = () => new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("", "studio", "env")]
    [InlineData("node", "", "env")]
    [InlineData("node", "studio", "")]
    [InlineData(null, "studio", "env")]
    [InlineData("node", null, "env")]
    [InlineData("node", "studio", null)]
    public void Constructor_NullOrWhitespaceParameters_ThrowsArgumentException(
        string? nodeId,
        string? studioId,
        string? environment)
    {
        var act = () => new HttpContextMapper(nodeId!, studioId!, environment!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MapFromHttpContext_MinimalHeaders_CreatesGridContext()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Should().NotBeNull();
        gridContext.NodeId.Should().Be(TestNodeId);
        gridContext.StudioId.Should().Be(TestStudioId);
        gridContext.Environment.Should().Be(TestEnvironment);
        gridContext.CorrelationId.Should().NotBeNullOrWhiteSpace();
        gridContext.CausationId.Should().BeNull();
    }

    [Fact]
    public void MapFromHttpContext_WithCorrelationIdHeader_UsesProvidedCorrelationId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "test-correlation-123";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.CorrelationId.Should().Be("test-correlation-123");
    }

    [Fact]
    public void MapFromHttpContext_WithCausationIdHeader_UsesCausationId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Causation-Id"] = "test-causation-456";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.CausationId.Should().Be("test-causation-456");
    }

    [Fact]
    public void MapFromHttpContext_WithStudioIdHeader_UsesProvidedStudioId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Studio-Id"] = "custom-studio";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.StudioId.Should().Be("custom-studio");
    }

    [Fact]
    public void MapFromHttpContext_WithoutStudioIdHeader_UsesDefaultStudioId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.StudioId.Should().Be(TestStudioId);
    }

    [Fact]
    public void MapFromHttpContext_WithTraceParentHeader_ExtractsTraceId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.CorrelationId.Should().Be("0af7651916cd43dd8448eb211c80319c");
    }

    [Fact]
    public void MapFromHttpContext_WithBothCorrelationIdAndTraceParent_PrefersCorrelationId()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "explicit-correlation-id";
        httpContext.Request.Headers["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.CorrelationId.Should().Be("explicit-correlation-id");
    }

    [Fact]
    public void MapFromHttpContext_WithSimpleBaggage_ExtractsBaggage()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "userId=alice,sessionId=123";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userId"].Should().Be("alice");
        gridContext.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void MapFromHttpContext_WithBaggageProperties_IgnoresProperties()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "userId=alice;property1;property2,sessionId=123";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userId"].Should().Be("alice");
        gridContext.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void MapFromHttpContext_WithUrlEncodedBaggageValues_DecodesValues()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "userName=Alice%20Smith,email=alice%40example.com";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userName"].Should().Be("Alice Smith");
        gridContext.Baggage["email"].Should().Be("alice@example.com");
    }

    [Fact]
    public void MapFromHttpContext_WithEmptyBaggageValue_SkipsEntry()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "userId=alice,emptyKey=,sessionId=123";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage.Should().ContainKey("userId");
        gridContext.Baggage.Should().ContainKey("sessionId");
        gridContext.Baggage.Should().NotContainKey("emptyKey");
    }

    [Fact]
    public void MapFromHttpContext_WithMalformedBaggage_SkipsInvalidEntries()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "userId=alice,invalidentry,sessionId=123";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userId"].Should().Be("alice");
        gridContext.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void MapFromHttpContext_WithoutBaggageHeader_CreatesEmptyBaggage()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void MapFromHttpContext_WithWhitespaceBaggage_CreatesEmptyBaggage()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = "   ";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void MapFromHttpContext_PropagatesCancellationToken()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void MapFromHttpContext_NullHttpContext_ThrowsArgumentNullException()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);

        var act = () => mapper.MapFromHttpContext(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapFromHttpContext_WithAllHeaders_CreatesCompleteGridContext()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-123";
        httpContext.Request.Headers["X-Causation-Id"] = "cause-456";
        httpContext.Request.Headers["X-Studio-Id"] = "custom-studio";
        httpContext.Request.Headers["baggage"] = "userId=alice,sessionId=789";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.CorrelationId.Should().Be("corr-123");
        gridContext.CausationId.Should().Be("cause-456");
        gridContext.StudioId.Should().Be("custom-studio");
        gridContext.NodeId.Should().Be(TestNodeId);
        gridContext.Environment.Should().Be(TestEnvironment);
        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userId"].Should().Be("alice");
        gridContext.Baggage["sessionId"].Should().Be("789");
    }

    [Fact]
    public void MapFromHttpContext_WithBaggageContainingSpaces_TrimsSpaces()
    {
        var mapper = new HttpContextMapper(TestNodeId, TestStudioId, TestEnvironment);
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["baggage"] = " userId = alice , sessionId = 123 ";

        var gridContext = mapper.MapFromHttpContext(httpContext);

        gridContext.Baggage.Should().HaveCount(2);
        gridContext.Baggage["userId"].Should().Be("alice");
        gridContext.Baggage["sessionId"].Should().Be("123");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }
}
