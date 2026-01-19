// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FluentAssertions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Context;
using HoneyDrunk.Kernel.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Kernel.Tests.Context;

/// <summary>
/// Tests for <see cref="GridContextAccessor"/>.
/// </summary>
/// <remarks>
/// GridContextAccessor retrieves the GridContext from HttpContext.RequestServices,
/// making it a read-only accessor that delegates to the DI container.
/// </remarks>
public class GridContextAccessorTests
{
    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new GridContextAccessor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void GridContext_WhenHttpContextIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var httpContextAccessor = new TestHttpContextAccessor { HttpContext = null };
        var accessor = new GridContextAccessor(httpContextAccessor);

        // Act
        var act = () => accessor.GridContext;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*outside of an HTTP request scope*");
    }

    [Fact]
    public void GridContext_WhenServiceNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var httpContextAccessor = new TestHttpContextAccessor { HttpContext = httpContext };

        var accessor = new GridContextAccessor(httpContextAccessor);

        // Act
        var act = () => accessor.GridContext;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not registered in the service container*");
    }

    [Fact]
    public void GridContext_WhenServiceRegistered_ReturnsContextFromRequestServices()
    {
        // Arrange
        var expectedContext = GridContextTestHelper.CreateInitialized(
            correlationId: "test-correlation",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var services = new ServiceCollection();
        services.AddSingleton<IGridContext>(expectedContext);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var httpContextAccessor = new TestHttpContextAccessor { HttpContext = httpContext };

        var accessor = new GridContextAccessor(httpContextAccessor);

        // Act
        var result = accessor.GridContext;

        // Assert
        result.Should().BeSameAs(expectedContext);
    }

    [Fact]
    public void GridContext_PreservesAllContextProperties()
    {
        // Arrange
        var baggage = new Dictionary<string, string> { ["key"] = "value" };
        var expectedContext = GridContextTestHelper.CreateInitialized(
            correlationId: "corr-123",
            nodeId: "node-1",
            studioId: "studio-1",
            environment: "production",
            causationId: "cause-456",
            tenantId: "tenant-1",
            projectId: "project-1",
            baggage: baggage);

        var services = new ServiceCollection();
        services.AddSingleton<IGridContext>(expectedContext);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var httpContextAccessor = new TestHttpContextAccessor { HttpContext = httpContext };

        var accessor = new GridContextAccessor(httpContextAccessor);

        // Act
        var result = accessor.GridContext;

        // Assert
        result.CorrelationId.Should().Be("corr-123");
        result.NodeId.Should().Be("node-1");
        result.StudioId.Should().Be("studio-1");
        result.Environment.Should().Be("production");
        result.CausationId.Should().Be("cause-456");
        result.TenantId.Should().Be("tenant-1");
        result.ProjectId.Should().Be("project-1");
        result.Baggage.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public void GridContext_MultipleAccesses_ReturnsSameInstance()
    {
        // Arrange
        var expectedContext = GridContextTestHelper.CreateInitialized(
            correlationId: "test-correlation",
            nodeId: "test-node",
            studioId: "test-studio",
            environment: "test-env");

        var services = new ServiceCollection();
        services.AddSingleton<IGridContext>(expectedContext);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var httpContextAccessor = new TestHttpContextAccessor { HttpContext = httpContext };

        var accessor = new GridContextAccessor(httpContextAccessor);

        // Act
        var result1 = accessor.GridContext;
        var result2 = accessor.GridContext;

        // Assert
        result1.Should().BeSameAs(result2);
    }

    /// <summary>
    /// Simple test double for IHttpContextAccessor.
    /// </summary>
    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
