// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;
using HoneyDrunk.Kernel.Context.Mappers;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;

namespace HoneyDrunk.Kernel.Tests.Context.Mappers;

/// <summary>
/// Tests for the static <see cref="HttpContextMapper"/> class.
/// </summary>
/// <remarks>
/// HttpContextMapper provides two static methods:
/// ExtractFromHttpContext extracts values into a GridContextInitValues record.
/// InitializeFromHttpContext initializes an existing GridContext from HTTP headers.
/// </remarks>
public class HttpContextMapperTests
{
    private const string TestNodeId = "test-node";
    private const string TestStudioId = "test-studio";
    private const string TestEnvironment = "test-env";

    [Fact]
    public void ExtractFromHttpContext_WithCorrelationIdHeader_ExtractsCorrelationId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "test-correlation-123";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().Be("test-correlation-123");
    }

    [Fact]
    public void ExtractFromHttpContext_WithoutCorrelationId_GeneratesNewCorrelationId()
    {
        var httpContext = CreateHttpContext();

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExtractFromHttpContext_WithTraceParentHeader_ExtractsTraceId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().Be("0af7651916cd43dd8448eb211c80319c");
    }

    [Fact]
    public void ExtractFromHttpContext_WithBothCorrelationIdAndTraceParent_PrefersCorrelationId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "explicit-correlation-id";
        httpContext.Request.Headers.TraceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().Be("explicit-correlation-id");
    }

    [Fact]
    public void ExtractFromHttpContext_WithShortTraceParentFormat_ExtractsTraceId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.TraceParent = "00-shortid";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().Be("shortid");
    }

    [Fact]
    public void ExtractFromHttpContext_WithInvalidTraceParentFormat_GeneratesNewCorrelationId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.TraceParent = "invalid-format";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().NotBeNullOrWhiteSpace();
        values.CorrelationId.Should().NotBe("invalid-format");
    }

    [Fact]
    public void ExtractFromHttpContext_WithCausationIdHeader_ExtractsCausationId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Causation-Id"] = "test-causation-456";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CausationId.Should().Be("test-causation-456");
    }

    [Fact]
    public void ExtractFromHttpContext_WithoutCausationIdHeader_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CausationId.Should().BeNull();
    }

    [Fact]
    public void ExtractFromHttpContext_WithTenantIdHeader_ExtractsTenantId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-789";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.TenantId.Should().Be("tenant-789");
    }

    [Fact]
    public void ExtractFromHttpContext_WithoutTenantIdHeader_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.TenantId.Should().BeNull();
    }

    [Fact]
    public void ExtractFromHttpContext_WithProjectIdHeader_ExtractsProjectId()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Project-Id"] = "project-012";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.ProjectId.Should().Be("project-012");
    }

    [Fact]
    public void ExtractFromHttpContext_WithoutProjectIdHeader_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.ProjectId.Should().BeNull();
    }

    [Fact]
    public void ExtractFromHttpContext_WithTenantAndProjectHeaders_ExtractsBoth()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-789";
        httpContext.Request.Headers["X-Project-Id"] = "project-012";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.TenantId.Should().Be("tenant-789");
        values.ProjectId.Should().Be("project-012");
    }

    [Fact]
    public void ExtractFromHttpContext_WithSimpleBaggage_ExtractsBaggage()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "userId=alice,sessionId=123";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["userId"].Should().Be("alice");
        values.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void ExtractFromHttpContext_WithBaggageProperties_IgnoresProperties()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "userId=alice;property1;property2,sessionId=123";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["userId"].Should().Be("alice");
        values.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void ExtractFromHttpContext_WithUrlEncodedBaggageValues_DecodesValues()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "userName=Alice%20Smith,email=alice%40example.com";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["userName"].Should().Be("Alice Smith");
        values.Baggage["email"].Should().Be("alice@example.com");
    }

    [Fact]
    public void ExtractFromHttpContext_WithEmptyBaggageValue_SkipsEntry()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "userId=alice,emptyKey=,sessionId=123";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage.Should().ContainKey("userId");
        values.Baggage.Should().ContainKey("sessionId");
        values.Baggage.Should().NotContainKey("emptyKey");
    }

    [Fact]
    public void ExtractFromHttpContext_WithMalformedBaggage_SkipsInvalidEntries()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "userId=alice,invalidentry,sessionId=123";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["userId"].Should().Be("alice");
        values.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void ExtractFromHttpContext_WithoutBaggageHeader_ReturnsEmptyBaggage()
    {
        var httpContext = CreateHttpContext();

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFromHttpContext_WithWhitespaceBaggage_ReturnsEmptyBaggage()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "   ";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFromHttpContext_WithBaggageContainingSpaces_TrimsSpaces()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = " userId = alice , sessionId = 123 ";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["userId"].Should().Be("alice");
        values.Baggage["sessionId"].Should().Be("123");
    }

    [Fact]
    public void ExtractFromHttpContext_WithEmptyBaggageKey_SkipsEntry()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "=value,validKey=validValue";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(1);
        values.Baggage.Should().ContainKey("validKey");
    }

    [Fact]
    public void ExtractFromHttpContext_WithXBaggagePrefixHeaders_ExtractsBaggage()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Baggage-tenant-id"] = "tenant-123";
        httpContext.Request.Headers["X-Baggage-user-id"] = "user-456";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["tenant-id"].Should().Be("tenant-123");
        values.Baggage["user-id"].Should().Be("user-456");
    }

    [Fact]
    public void ExtractFromHttpContext_WithBothBaggageFormats_MergesBaggage()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Baggage = "w3c-key=w3c-value";
        httpContext.Request.Headers["X-Baggage-custom-key"] = "custom-value";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["w3c-key"].Should().Be("w3c-value");
        values.Baggage["custom-key"].Should().Be("custom-value");
    }

    [Fact]
    public void ExtractFromHttpContext_WithEmptyXBaggageValue_SkipsEntry()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Baggage-valid-key"] = "valid-value";
        httpContext.Request.Headers["X-Baggage-empty-key"] = string.Empty;

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(1);
        values.Baggage.Should().ContainKey("valid-key");
        values.Baggage.Should().NotContainKey("empty-key");
    }

    [Fact]
    public void ExtractFromHttpContext_WithWhitespaceXBaggageValue_SkipsEntry()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Baggage-valid-key"] = "valid-value";
        httpContext.Request.Headers["X-Baggage-whitespace-key"] = "   ";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(1);
        values.Baggage.Should().ContainKey("valid-key");
        values.Baggage.Should().NotContainKey("whitespace-key");
    }

    [Fact]
    public void ExtractFromHttpContext_WithCaseInsensitiveXBaggagePrefix_ExtractsBaggage()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["x-baggage-lowercase"] = "lower-value";
        httpContext.Request.Headers["X-BAGGAGE-UPPERCASE"] = "upper-value";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Baggage.Should().HaveCount(2);
        values.Baggage["lowercase"].Should().Be("lower-value");
        values.Baggage["UPPERCASE"].Should().Be("upper-value");
    }

    [Fact]
    public void ExtractFromHttpContext_PropagatesCancellationToken()
    {
        var httpContext = CreateHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void ExtractFromHttpContext_NullHttpContext_ThrowsArgumentNullException()
    {
        var act = () => HttpContextMapper.ExtractFromHttpContext(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpContext");
    }

    [Fact]
    public void ExtractFromHttpContext_WithAllHeaders_ExtractsAllValues()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-123";
        httpContext.Request.Headers["X-Causation-Id"] = "cause-456";
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-789";
        httpContext.Request.Headers["X-Project-Id"] = "project-012";
        httpContext.Request.Headers.Baggage = "userId=alice,sessionId=789";

        var values = HttpContextMapper.ExtractFromHttpContext(httpContext);

        values.CorrelationId.Should().Be("corr-123");
        values.CausationId.Should().Be("cause-456");
        values.TenantId.Should().Be("tenant-789");
        values.ProjectId.Should().Be("project-012");
        values.Baggage.Should().HaveCount(2);
        values.Baggage["userId"].Should().Be("alice");
        values.Baggage["sessionId"].Should().Be("789");
    }

    [Fact]
    public void InitializeFromHttpContext_InitializesGridContext()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-123";
        httpContext.Request.Headers["X-Causation-Id"] = "cause-456";
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-789";
        httpContext.Request.Headers["X-Project-Id"] = "project-012";
        httpContext.Request.Headers.Baggage = "userId=alice";

        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        HttpContextMapper.InitializeFromHttpContext(context, httpContext);

        context.CorrelationId.Should().Be("corr-123");
        context.CausationId.Should().Be("cause-456");
        context.TenantId.Should().Be("tenant-789");
        context.ProjectId.Should().Be("project-012");
        context.Baggage["userId"].Should().Be("alice");
        context.NodeId.Should().Be(TestNodeId);
        context.StudioId.Should().Be(TestStudioId);
        context.Environment.Should().Be(TestEnvironment);
    }

    [Fact]
    public void InitializeFromHttpContext_WithMinimalHeaders_InitializesWithGeneratedCorrelationId()
    {
        var httpContext = CreateHttpContext();
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        HttpContextMapper.InitializeFromHttpContext(context, httpContext);

        context.CorrelationId.Should().NotBeNullOrWhiteSpace();
        context.CausationId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.ProjectId.Should().BeNull();
        context.Baggage.Should().BeEmpty();
    }

    [Fact]
    public void InitializeFromHttpContext_PropagatesCancellationToken()
    {
        var httpContext = CreateHttpContext();
        using var cts = new CancellationTokenSource();
        httpContext.RequestAborted = cts.Token;
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        HttpContextMapper.InitializeFromHttpContext(context, httpContext);

        context.Cancellation.Should().Be(cts.Token);
    }

    [Fact]
    public void InitializeFromHttpContext_NullContext_ThrowsArgumentNullException()
    {
        var httpContext = CreateHttpContext();

        var act = () => HttpContextMapper.InitializeFromHttpContext(null!, httpContext);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void InitializeFromHttpContext_NullHttpContext_ThrowsArgumentNullException()
    {
        var context = GridContextTestHelper.CreateUninitialized(TestNodeId, TestStudioId, TestEnvironment);

        var act = () => HttpContextMapper.InitializeFromHttpContext(context, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpContext");
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }
}
